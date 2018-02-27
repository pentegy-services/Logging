using System;
using System.ServiceModel;

namespace PentegyServices.Logging.Core.Test.CustomImpl
{
	/// <summary>
	/// Combined implementation 'cause <see cref="ServiceTestCaseBase{I,T}"/> supports only one service.
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, Namespace = Namespace.Service)]
	public class CustomLogService
		: ILogService
	{
		LogReader reader;
		LogWriter writer;

		public CustomLogService(ILoggingConfiguration configuration, CustomLogRepository repository)
		{
			reader = new LogReader(configuration, repository);
			writer = new LogWriter(configuration, repository);
		}

		#region ILogWriter Members

		public Boolean Write(LogEntry[] batch)
		{
			return writer.Write(batch);
		}

		#endregion

		#region ILogReader Members

		public LogEntry[] GetEventLog(LogFilter filter)
		{
			return reader.GetEventLog(filter);
		}

		public LogEntry GetEventLogEntry(Int64 id)
		{
			return reader.GetEventLogEntry(id);
		}

		#endregion
	}
}
