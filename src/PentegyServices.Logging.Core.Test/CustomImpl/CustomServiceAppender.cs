using System;
using System.ServiceModel.Description;

namespace PentegyServices.Logging.Core.Test.CustomImpl
{
	/// <summary>
	/// Example of how to extend <see cref="ServiceAppender"/> to store additional data.
	/// </summary>
	public class CustomServiceAppender
		: ServiceAppender
	{
		public const String StackTraceProp = "stackTrace";
		public const String CustomDataProp = "customData";

		public CustomServiceAppender(ServiceEndpoint endpoint)
		{
			ServiceEndpoint = endpoint;
		}

		protected override LogEntry Convert(log4net.Core.LoggingEvent e)
		{
			LogEntry result = base.Convert(e);
			if (e.Level.Name.Equals("FATAL", StringComparison.OrdinalIgnoreCase) ||
				e.Level.Name.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
			{
				var ex = e.ExceptionObject as Exception;
				if (ex != null)
				{
					result.CustomData.Add(StackTraceProp, ex.StackTrace);
				}
			}

			var customData = e.LookupProperty(CustomDataProp);
			if (customData != null)
			{
				result.CustomData.Add(CustomDataProp, customData.ToString());

			}

			return result;
		}

		protected override void Append(log4net.Core.LoggingEvent loggingEvent)
		{
			base.Append(loggingEvent);
		}
	}
}
