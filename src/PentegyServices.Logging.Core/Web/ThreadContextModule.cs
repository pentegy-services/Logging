using log4net;
using PentegyServices.Logging.Core;
using System;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// ASP.NET helper module that initializes logging context (log4net) for each HTTP request.
	/// The application that uses this module must initialize log4net configuration itself (for example, 
	/// by calling <see cref="log4net.Config.XmlConfigurator.Configure()"/>). The properties initialized (<see cref="log4net.ThreadContext"/> scope):
	/// <list type="bullet">
	///		<item>
	///			<term><see cref="LogProp.SessionID"/></term>
	///		</item>
	///		<item>
	///			<term><see cref="LogProp.LoggingID"/></term>
	///		</item>
	///		<item>
	///			<term><see cref="LogProp.UserIdentity"/></term>
	///		</item>
	///		<item>
	///			<term><see cref="LogProp.RequestAddress"/></term>
	///		</item>
	///		<item>
	///			<term><see cref="LogProp.UserAgent"/></term>
	///		</item>
	///		<item>
	///			<term><see cref="LogProp.UserAddress"/></term>
	///		</item>
	/// </list>
	/// Note, the module does not initialize properties in global context (<see cref="GlobalContext"/>) except <see cref="LogProp.MachineAddress"/> - it's up to the consuming site.
	/// <para>Configure the module as this:
	/// <code>
	/// &lt;httpModules&gt;
	///		&lt;add name="ThreadContextModule" type="Core.Logging.Web.ThreadContextModule, Core.Logging" /&gt;
	///	&lt;/httpModules&gt;
	/// </code>
	/// This example is for IIS6, IIS7 classic mode and Cassini.
	///	Don't forget to add the same modules to system.webServer/modules section for IIS7+ integration mode.
	///	See <a href="http://msdn.microsoft.com/en-us/library/46c5ddfy.aspx">http://msdn.microsoft.com/en-us/library/46c5ddfy.aspx</a> for more information.
	/// </para>
	/// <para>Note, the order of modules in web.config is important. Place "ThreadContextModule" entry as the very first one.</para>
	/// <para>In addition, the module add <see cref="LogProp.LoggingID"/> value as a response HTTP header for easier debugging in browsers.</para>
	/// </summary>
	public class ThreadContextModule
		: IHttpModule
	{
		static readonly Object sync = new Object();

		static Boolean initialized = false;

		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		HttpApplication app;

		/// <summary>
		/// Creates an instance of <see cref="ThreadContextModule"/>. Normally, you don't use it directly.
		/// Instead, you configure the module in <c>web.config</c> and ASP.NET runtime instantiates it itself.
		/// </summary>
		public ThreadContextModule()
		{ }

		static ThreadContextModule()
		{
			GlobalContext.Properties[LogProp.MachineAddress] = SysUtil.GetMachineName();
		}

		#region IHttpModule Members

		/// <summary>
		/// Disposes of the resources (other than memory) used by the module that implements <see cref="IHttpModule"/>.
		/// <see cref="ThreadContextModule"/> does not allocate any resources so this implementation does nothing.
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// Initializes a module and prepares it to handle requests.
		/// </summary>
		/// <param name="context">An <see cref="HttpApplication"/> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application.</param>
		public void Init(HttpApplication context)
		{
			this.app = context;

			this.app.BeginRequest += new EventHandler(app_BeginRequest);
			this.app.PostAuthenticateRequest += new EventHandler(app_PostAuthenticateRequest);
			this.app.PostMapRequestHandler += new EventHandler(app_PostMapRequestHandler);
			this.app.PostAcquireRequestState += new EventHandler(app_PostAcquireRequestState);
			this.app.PreRequestHandlerExecute += new EventHandler(app_PreRequestHandlerExecute);
			
			this.app.EndRequest += new EventHandler(app_EndRequest);

			lock (sync)
			{
				if (!initialized)
				{
					initialized = true;
					logger.Info("Attached the module");
				}
			}
		}

		#endregion

		void app_PostMapRequestHandler(Object sender, EventArgs e)
		{
			HttpApplication app = (HttpApplication)sender;

			if (app.Context.Handler is IReadOnlySessionState || app.Context.Handler is IRequiresSessionState)
			{
				return; // no need to replace the current handler
			}

			// swap the current handler
			app.Context.Handler = new HelperHttpHandler(app.Context.Handler);
			SetThreadContextProperties();
		}

		void app_PreRequestHandlerExecute(Object sender, EventArgs e)
		{
			HttpApplication app = (HttpApplication)sender;
			HelperHttpHandler resourceHttpHandler = HttpContext.Current.Handler as HelperHttpHandler;

			if (resourceHttpHandler != null)
			{
				// set the original handler back
				HttpContext.Current.Handler = resourceHttpHandler.OriginalHandler;
			}

			// -> at this point session state should be available
			try // accessing HttpContext.Session may throw exceptions so we'd better catch it
			{
				ThreadContext.Properties[LogProp.SessionID] = app.Session.SessionID;
			}
			catch
			{
				ThreadContext.Properties[LogProp.SessionID] = null;
			}
			SetThreadContextProperties();
		}

		void app_PostAcquireRequestState(Object sender, EventArgs e)
		{
			HttpApplication app = (HttpApplication)sender;
			HelperHttpHandler resourceHttpHandler = HttpContext.Current.Handler as HelperHttpHandler;

			if (resourceHttpHandler != null)
			{
				// set the original handler back
				HttpContext.Current.Handler = resourceHttpHandler.OriginalHandler;
			}

			// -> at this point session state should be available
			try // accessing HttpContext.Session may throw exceptions so we'd better catch it
			{
				ThreadContext.Properties[LogProp.SessionID] = app.Session.SessionID;
			}
			catch
			{
				ThreadContext.Properties[LogProp.SessionID] = null;
			}
			SetThreadContextProperties();
		}

		void app_PostAuthenticateRequest(Object sender, EventArgs e)
		{
			SetThreadContextProperties();
		}

		void app_BeginRequest(Object sender, EventArgs e)
		{
			// Generate an unique identifier to flow through all layers (for logging/diagnosing)
			var id = Guid.NewGuid();

			// Attach it to standard .NET tracing infrastructure
			// If you have propagateActivity="true" for "System.ServiceMode" sources in system.diagnostics section
			// you can correlate IB logging with WCF diagnostics
			Trace.CorrelationManager.ActivityId = id;

			// Attach it to the current logging context
			ThreadContext.Properties[LogProp.LoggingID] = id.ToString();

			// If http context is present add useful request data to the logging context
			SetThreadContextProperties();
		}

		void app_EndRequest(Object sender, EventArgs e)
		{
			var context = HttpContext.Current;
			var request = HttpContext.Current.Request;
			if (context != null && request != null && request.Url != null)
			{

				String url = request.Url.ToString();
				// add current logging ID excluding Content of the site as a response header for easier debugging in browsers
				Object loggingID = ThreadContext.Properties[LogProp.LoggingID];
				if (loggingID != null && !String.IsNullOrEmpty(url) && !url.Contains("Content"))
				{
					HttpResponse response = context.Response;
					Debug.Assert(response != null);

					response.AppendHeader(LogProp.LoggingID, loggingID.ToString());
				}
			}
			SetThreadContextProperties();
		}

		private void SetThreadContextProperties()
		{
			var context = HttpContext.Current;
			if (context != null)
			{
				String identityName = "null";
				if (context.User != null && context.User.Identity != null && context.User.Identity.Name != null)
				{
					identityName = context.User.Identity.Name;
				}
				ThreadContext.Properties[LogProp.UserIdentity] = identityName;
				// If http context is present add useful request data to the logging context
				HttpRequest request = context.Request;
				if (request != null)
				{
					ThreadContext.Properties[LogProp.RequestAddress] = request.UserHostAddress;
					ThreadContext.Properties[LogProp.UserAddress] = request.UserHostAddress;
					ThreadContext.Properties[LogProp.UserAgent] = request.UserAgent;
				}
			}
		}
		
		/// <summary>
		/// A temp handler used to force the SessionStateModule to load session state
		/// See <a href="http://stackoverflow.com/questions/276355/can-i-access-session-state-from-an-httpmodule">this article</a> for details.
		/// </summary>
		public class HelperHttpHandler
			: IHttpHandler, IRequiresSessionState
		{
			internal readonly IHttpHandler OriginalHandler;

			/// <summary>
			/// Constructs an instance of <see cref="HelperHttpHandler"/> from the given <see cref="IHttpHandler"/>.
			/// </summary>
			/// <param name="originalHandler"></param>
			public HelperHttpHandler(IHttpHandler originalHandler)
			{
				OriginalHandler = originalHandler;
			}

			/// <summary>
			/// Processes HTTP web request. Should not be called.
			/// </summary>
			/// <param name="context"></param>
			public void ProcessRequest(HttpContext context)
			{
				// do not worry, ProcessRequest() will not be called, but let's be safe
				throw new InvalidOperationException("MyHttpHandler cannot process requests.");
			}

			/// <summary>
			/// Gets a value indicating whether another request can use the <see cref="IHttpHandler"/> instance.
			/// </summary>
			public Boolean IsReusable
			{
				// IsReusable must be set to false since class has a member!
				get
				{
					return false;
				}
			}
		}
	}
}
