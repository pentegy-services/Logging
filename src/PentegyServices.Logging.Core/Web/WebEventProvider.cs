using log4net;
using System;
using System.Collections.Specialized;
using System.Web.Management;
using System.Xml.Linq;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// Simple web event provider that redirects all the events into log4net engine in xml format.
	/// Based on <a href="http://msdn.microsoft.com/en-us/library/ms227676.aspx">Microsoft's sample</a>.
	/// </summary>
	public class WebEventProvider
		: BufferedWebEventProvider
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes the provider.
		/// </summary>
		public override void Initialize(String name, NameValueCollection config)
		{
			base.Initialize(name, config);
		}

		/// <summary>
		/// Processes the incoming events.This method performs custom processing and, if
		/// buffering is enabled, it calls the base.ProcessEvent() to buffer the event information.
		/// </summary>
		public override void ProcessEvent(WebBaseEvent eventRaised)
		{
			if (UseBuffering)
			{
				// Buffering enabled, call the base event to buffer event information.
				base.ProcessEvent(eventRaised);
			}
			else
			{
				WriteEvent(eventRaised);
			}
		}

		/// <summary>
		/// Processes the messages that have been buffered.
		/// It is called by the ASP.NET when the flushing of the buffer is required according to the parameters 
		/// defined in the &lt;bufferModes&gt; element of the &lt;healthMonitoring&gt; configuration section.
		/// </summary>
		public override void ProcessEventFlush(WebEventBufferFlushInfo flushInfo)
		{
			// Read each buffered event and send it to the log.
			foreach (WebBaseEvent webEvent in flushInfo.Events)
			{
				WriteEvent(webEvent);
			}
		}

		/// <summary>
		/// Performs standard shutdown.
		/// </summary>
		public override void Shutdown()
		{
			Flush(); // Flush the buffer, if needed.
		}

		/// <summary>
		/// Writes the event specified into log4net engine.
		/// </summary>
		protected void WriteEvent(WebBaseEvent webEvent)
		{
			var root = new XElement("webEvent",
				new XAttribute("code", webEvent.EventCode),
				new XAttribute("detailCode", webEvent.EventDetailCode),
				new XAttribute("id", webEvent.EventID),
				new XAttribute("occurrence", webEvent.EventOccurrence),
				new XAttribute("sequence", webEvent.EventSequence),
				new XAttribute("source", webEvent.EventSource != null ? webEvent.EventSource.ToString() : ""),
				new XAttribute("time", webEvent.EventTime),
				new XAttribute("timeUTC", webEvent.EventTimeUtc)
			)
			{
				Value = webEvent.Message
			};

			logger.Info(root);
		}
	}
}
