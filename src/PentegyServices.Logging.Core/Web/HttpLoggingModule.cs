using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// Logs HTTP requests/response information to log4net logging engine along with execution time.
	/// The information is written in xml format to make it easier to parse/analyze:
	/// <code>
	///	&lt;http ms="000524"&gt;
	///		&lt;request requestType="GET" contentEncoding="Unicode (UTF-8)" contentLength="0" contentType="" url="http://localhost:22038/Content/custom.css" urlReferrer="http://localhost:22038/" userAgent="Mozilla/5.0 (Windows NT 6.1; WOW64)" totalBytes="0" isAuthenticated="true" isLocal="true" isSecureConnection="false"&gt;
	///			&lt;cookies&gt;
	///				&lt;__RequestVerificationToken_Lw__ expires="" domain="" httpOnly="false" path="/" secure="false" lenth="172"&gt;19+qS1wBvGxu/khcOfQDSr9EhS3yhcd8...&lt;/__RequestVerificationToken_Lw__&gt;
	///				&lt;ASP.NET_SessionId expires="" domain="" httpOnly="false" path="/" secure="false" lenth="24"&gt;yncvpg2ncfzd3yjmxtt3zw1q&lt;/ASP.NET_SessionId&gt;
	///			&lt;/cookies&gt;
	///			&lt;headers&gt;
	///				&lt;Referer len="23"&gt;http://localhost:22038/&lt;/Referer&gt;
	///			&lt;/headers&gt;
	///		&lt;/request&gt;
	///		&lt;response contentEncoding="Unicode (UTF-8)" contentType="text/css" expires="0" expiresAbsolute="" isClientConnected="true" redirectLocation="" statusCode="200" statusDescription="OK" suppressContent="false" trySkipIisCustomErrors="false"&gt;
	///			&lt;cookies /&gt;
	///		&lt;/response&gt;
	///	&lt;/http&gt;
	/// </code>
	/// The module supports flexible configuration based on rules (see <see cref="HttpLoggingModuleConfigurationSection"/>).
	/// 
	/// To enable the module put it into your web.config (IIS 7.x integrated mode):
	/// <code>
	/// &lt;system.webServer&gt;
	///		&lt;modules runAllManagedModulesForAllRequests="true"&gt;
	///			&lt;add name="HttpLoggingModule" type="Core.Logging.Web.HttpLoggingModule, Core.Logging" /&gt;
	///		&lt;/modules&gt;
	/// &lt;/system.webServer&gt;
	/// </code>
	/// For Cassini, IIS 6 or IIS 7.x classic mode put the module into <c>system.web/httpModules</c> section.
	/// </summary>
	public class HttpLoggingModule
		: IHttpModule
	{
		static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		static readonly String ContextItemKey = typeof(HttpLoggingModule).Name; // used to correlate begin_request and end_request
		static List<LoggingRule> Rules = null;

		#region Static privates

		private static volatile bool applicationStarted = false;
		private static object applicationStartLock = new object();

		#endregion

		HttpLoggingModuleConfigurationSection Settings // just a shortcut
		{
			get
			{
				return HttpLoggingModuleConfigurationSection.Settings;
			}
		}

		#region IHttpModule Members

		/// <summary>Does nothing.</summary>
		public void Dispose()
		{ }

		/// <summary>
		/// Initializes the specified module.
		/// Subscribes to <see cref="HttpApplication.BeginRequest"/> and <see cref="HttpApplication.EndRequest"/>
		/// and initializes the rules.
		/// </summary>
		/// <param name="context">The application context that instantiated and will be running this module.</param>
		public void Init(HttpApplication context)
		{
			if (!applicationStarted)
			{
				lock (applicationStartLock) // http://stackoverflow.com/questions/1140915/httpmodule-init-method-is-called-several-times-why
				{
					if (!applicationStarted)
					{
						applicationStarted = true;
						// this will run only once per application start
						OnStart(context);
					}
				}
			}
			// this will run on every HttpApplication initialization in the application pool
			OnInit(context);
		}

		#endregion

		/// <summary>Initializes any data/resources on application start.</summary>
		/// <param name="context">The application context that instantiated and will be running this module.</param>
		public virtual void OnStart(HttpApplication context) 
		{
			if (Rules == null)
			{
				LoggingRule[] rules = null;
				try
				{
					rules = Settings.Rules
					   .OfType<HttpLoggingModuleRuleItem>()
					   .Select(x => new LoggingRule(x.Type, x.ContentType, x.IP, x.Method, x.Status, x.Url))
					   .ToArray();
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException("Cannot initialize logging rules", ex);
				}
				Rules = new List<LoggingRule>(rules);
				logger.InfoFormat("Attached the module ({0} rules)", Rules.Count);
			}
		}

		/// <summary>Initializes any data/resources on HTTP module start.</summary>
		/// <param name="context">The application context that instantiated and will be running this module.</param>
		public virtual void OnInit(HttpApplication context)
		{
			context.BeginRequest += app_BeginRequest;
			context.EndRequest += app_EndRequest;
		}

		Boolean IsEnabled
		{
			get
			{
				return logger.IsDebugEnabled || logger.IsInfoEnabled;
			}
		}

		void app_BeginRequest(Object source, EventArgs e)
		{
			HttpApplication app = (HttpApplication)source;
			if (IsEnabled)
			{
				var stopwatch = new Stopwatch();
				app.Context.Items[ContextItemKey] = stopwatch;
				stopwatch.Start();
			}
		}

		void app_EndRequest(Object source, EventArgs e)
		{
			HttpApplication app = (HttpApplication)source;
			if (IsEnabled)
			{
				Int64 elapsedMilliseconds = 0;
				try
				{
					var stopwatch = (Stopwatch)app.Context.Items[ContextItemKey];
					stopwatch.Stop(); // pure request time
					elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
				}
				catch (Exception ex)
				{
					Trace.TraceError(ex.ToString());
				}

				// determine if need to log
				var requestData = new RequestData(app.Context);
				var result = LoggingRule.IsLog(Rules, requestData);
				Trace.TraceInformation("{0}: {1}/{2}\t[{3}]", GetType().Name, result.IsLog, result.RuleIndex, requestData);

				if (result.IsLog)
				{
					try
					{
						var root = new XElement("http",
							new XAttribute("ms", elapsedMilliseconds.ToString().PadLeft(6, '0')),
							FormatRequest(app.Context.Request),
							FormatResponse(app.Context.Response)
						);

						if (Settings.Debug)
						{
							logger.Debug(root);
						}
						else
						{
							logger.Info(root);
						}
					}
					catch (Exception ex)
					{
						logger.Fatal(ex);
					}
				}
			}
		}

		XElement FormatRequest(HttpRequest request)
		{
			var root = new XElement("request");
			if (request == null)
			{
				return root;
			}

			root.Add(new XAttribute("requestType", request.RequestType ?? ""));
			root.Add(new XAttribute("contentEncoding", request.ContentEncoding.EncodingName));
			root.Add(new XAttribute("contentLength", request.ContentLength));
			root.Add(new XAttribute("contentType", request.ContentType ?? ""));
			root.Add(new XAttribute("url", request.Url.ToString()));
			root.Add(new XAttribute("urlReferrer", request.UrlReferrer != null ? request.UrlReferrer.ToString() : ""));
			root.Add(new XAttribute("userAgent", request.UserAgent ?? ""));
			root.Add(new XAttribute("totalBytes", request.TotalBytes));
			root.Add(new XAttribute("isAuthenticated", request.IsAuthenticated));
			root.Add(new XAttribute("isLocal", request.IsLocal));
			root.Add(new XAttribute("isSecureConnection", request.IsSecureConnection));

			if (Settings.Headers)
			{
				AddHeaders(request.Headers, root);
			}
			if (Settings.Cookies)
			{
				AddCookies(request.Cookies, root);
			}
			if (Settings.Form)
			{
				AddForm(request.Form, root);
			}

			return root;
		}

		XElement FormatResponse(HttpResponse response)
		{
			var root = new XElement("response");
			if (response == null)
			{
				return root;
			}

			root.Add(new XAttribute("contentEncoding", response.ContentEncoding.EncodingName));
			root.Add(new XAttribute("contentType", response.ContentType ?? ""));
			root.Add(new XAttribute("expires", response.Expires));
			root.Add(new XAttribute("expiresAbsolute", response.ExpiresAbsolute != default(DateTime) ? response.ExpiresAbsolute.ToString("o") : ""));
			root.Add(new XAttribute("isClientConnected", response.IsClientConnected));
			root.Add(new XAttribute("redirectLocation", response.RedirectLocation ?? ""));
			root.Add(new XAttribute("statusCode", response.StatusCode));
			try
			{
				root.Add(new XAttribute("subStatusCode", response.SubStatusCode));
			}
			catch (PlatformNotSupportedException)
			{ }
			try
			{
				root.Add(new XAttribute("statusDescription", response.StatusDescription ?? ""));
			}
			catch (ArgumentOutOfRangeException)
			{ }
			root.Add(new XAttribute("suppressContent", response.SuppressContent));
			root.Add(new XAttribute("trySkipIisCustomErrors", response.TrySkipIisCustomErrors));

			if (Settings.Headers)
			{
				try
				{
					AddHeaders(response.Headers, root);
				}
				catch (PlatformNotSupportedException)
				{ }
			}

			if (Settings.Cookies)
			{
				AddCookies(response.Cookies, root);
			}

			return root;
		}

		void AddCookies(HttpCookieCollection cookiesCollection, XElement parent)
		{
			var cookies = new XElement("cookies");
			parent.Add(cookies);

			if (cookiesCollection != null && cookiesCollection.AllKeys != null)
			{
				cookies.Add(new XAttribute("count", cookiesCollection.Count));
				foreach (String key in cookiesCollection.AllKeys.OrderBy(x => x))
				{
					HttpCookie cookie = cookiesCollection[key];
					String val = cookie.Value;
					String valToLog = val.Length > Settings.MaxCookieLength ? val.Substring(0, Settings.MaxCookieLength) + "..." : val;

					var cookieElem = new XElement(XmlConvert.EncodeName(key),
						new XAttribute("expires", cookie.Expires != default(DateTime) ? cookie.Expires.ToString("o") : ""),
						new XAttribute("domain", cookie.Domain ?? ""),
						new XAttribute("httpOnly", cookie.HttpOnly),
						new XAttribute("path", cookie.Path ?? ""),
						new XAttribute("secure", cookie.Secure),
						new XAttribute("lenth", val.Length)
					)
					{
						Value = valToLog
					};
					cookies.Add(cookieElem);
				}
			}
		}

		void AddHeaders(NameValueCollection headersCollection, XElement parent)
		{
			var headers = new XElement("headers");
			parent.Add(headers);

			if (headersCollection != null && headersCollection.AllKeys != null)
			{
				headers.Add(new XAttribute("count", headersCollection.Count));
				foreach (String key in headersCollection.AllKeys.OrderBy(x => x))
				{
					String val = headersCollection[key] ?? "";
					String valToLog = val.Length > Settings.MaxHeaderLength ? val.Substring(0, Settings.MaxHeaderLength) + "..." : val;

					var headerElem = new XElement(XmlConvert.EncodeName(key), new XAttribute("len", val.Length))
					{
						Value = valToLog
					};
					headers.Add(headerElem);
				}
			}
		}

		void AddForm(NameValueCollection formCollection, XElement parent)
		{
			var form = new XElement("form");
			parent.Add(form);

			if (formCollection != null && formCollection.AllKeys != null)
			{
				form.Add(new XAttribute("count", formCollection.Count));
				foreach (String key in formCollection.AllKeys.Where(x => x != null).OrderBy(x => x))
				{
					String val = formCollection[key] ?? "";
					String valToLog = val.Length > Settings.MaxFormLength ? val.Substring(0, Settings.MaxFormLength) + "..." : val;

					var formElem = new XElement(XmlConvert.EncodeName(key), new XAttribute("len", val.Length))
					{
						Value = valToLog
					};
					form.Add(formElem);
				}
			}
		}
	}
}
