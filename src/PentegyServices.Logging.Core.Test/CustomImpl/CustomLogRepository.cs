using System;
using System.Collections.Generic;
using System.Linq;

namespace PentegyServices.Logging.Core.Test.CustomImpl
{
	/// <summary>
	/// Implements trivial in-memory storage repository.
	/// </summary>
	public class CustomLogRepository
		: ILogReaderRepository, ILogWriterRepository
	{
		readonly Object sync = new Object();
		readonly Dictionary<Int64, LogEntry> log = new Dictionary<Int64, LogEntry>();
		readonly ILoggingConfiguration Configuration;
		Int64 id;

		public CustomLogRepository(ILoggingConfiguration configuration)
		{
			Configuration = configuration;
		}

		#region ILogReaderRepository Members

		public LogEntry ReadEntry(Int64 id)
		{
			lock (sync)
			{
				return log[id];
			}
		}

		public LogEntry[] ReadBatch(LogFilter filter)
		{
			lock (sync)
			{
				return log.Values.Take(Configuration.MaxRowsToQueryEventLog).ToArray();
			}
		}

		#endregion

		#region ILogWriterRepository Members

		public void WriteBatch(LogEntry[] batch)
		{
			foreach (var entry in batch)
			{
				lock (sync)
				{
					entry.ID = id++;
					log.Add(entry.ID, entry);
				}
			}
		}

		#endregion
	}
}
