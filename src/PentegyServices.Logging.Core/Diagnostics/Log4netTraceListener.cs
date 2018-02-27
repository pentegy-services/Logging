using log4net;
using System;
using System.Diagnostics;
using System.Reflection;

namespace PentegyServices.Logging.Core.Diagnostics
{
	/// <summary>
	/// Forwards all trace messages (<see cref="System.Diagnostics"/>) to log4net engine.
	/// This is useful if you want to store, for example, WCF tracing into your logs:
	/// <code>
	/// &lt;system.diagnostics&gt;
	///		&lt;sources&gt;
	/// 		&lt;source name="System.ServiceModel" switchValue="Error,Critical,Warning" propagateActivity="false"&gt;
	/// 			&lt;listeners&gt;
	/// 				&lt;add name="log4net" type="Core.Logging.Diagnostics.Log4netTraceListener, Core.Logging" initializeData="System.ServiceModel.Redirect" /&gt;
	/// 			&lt;/listeners&gt;
	/// 		&lt;/source&gt;
	///		&lt;/sources&gt;
	/// &lt;/system.diagnostics&gt;
	/// </code>
	/// In this sample all WCF errors and warnings will be redirected to log4net with a logger named "System.ServiceModel.Redirect".
	/// Note, that if you need to mask or skip sensitive data in logs <see cref="Log4netTraceListener"/> is not the thing you want to use,
	/// especially in production environment.
	/// <para>
	/// The listener respects original <see cref="TraceEventType"/> and maps it to the corresponding log4net levels.
	/// The listener does not respect other event properties like <see cref="TraceEventCache"/> so information like thread id is not persisted.
	/// </para>
	/// <seealso cref="Core.Logging.ServiceModel.XmlParameterInspector"/>
	/// </summary>
	public class Log4netTraceListener
		: TraceListener
	{
		ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs an instance with default log4net logger named "System.Diagnostics.Redirection".
		/// </summary>
		public Log4netTraceListener()
		{ }

		/// <summary>
		/// Constructs an instance with specified name.
		/// Without this constructor .NET cannot instantiate the listener specified in the configuration file.
		/// </summary>
		/// <param name="name">The name of the <see cref="Log4netTraceListener"/>.</param>
		public Log4netTraceListener(String name)
			: base(name)
		{
			if (!String.IsNullOrEmpty(name))
			{
				logger = LogManager.GetLogger(name); // recreate log4net logger with the specified name
			}
		}

		/// <summary>
		/// Maps original event levels to log4net levels.
		/// </summary>
		/// <param name="eventCache"></param>
		/// <param name="source"></param>
		/// <param name="eventType"></param>
		/// <param name="id"></param>
		/// <param name="data"></param>
		public override void TraceData(TraceEventCache eventCache, String source, TraceEventType eventType, Int32 id, Object data)
		{
			if (data == null)
			{
				return;
			}

			switch (eventType)
			{
				case TraceEventType.Critical:
					logger.Fatal(data); break;
				case TraceEventType.Error:
					logger.Error(data); break;
				case TraceEventType.Warning:
					logger.Warn(data); break;
				case TraceEventType.Information:
					logger.Info(data); break;
				default:
					logger.Debug(data); break;
			}
		}

		/// <summary>
		/// This is must-implement method because it's abstract in <see cref="TraceListener"/>.
		/// Writes the message to log4net logger with "INFO" level.
		/// </summary>
		/// <param name="message">A message to write.</param>
		public override void Write(String message)
		{
			logger.Info(message);
		}

		/// <summary>
		/// This is must-implement method because it's abstract in <see cref="TraceListener"/>.
		/// Writes the message to log4net logger with "INFO" level.
		/// </summary>
		/// <param name="message">A message to write.</param>
		public override void WriteLine(String message)
		{
			logger.Info(message);
		}
	}
}
