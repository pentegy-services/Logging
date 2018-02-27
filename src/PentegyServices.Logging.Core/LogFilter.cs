using PentegyServices.Logging.Core;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Filter structure used by <see cref="ILogReader.GetEventLog(LogFilter)"/>.
	/// The following levels are included by default: "DEBUG", "INFO", "WARN", "ERROR", "FATAL" (see <see cref="DefaultLevels"/>).
	/// The rest of properties have their default values.
	/// </summary>
	[DataContract(Namespace = Namespace.DataContract)]
	[Serializable]
	public class LogFilter
	{
		/// <summary>Default logging levels.</summary>
		public static String[] DefaultLevels = new String[] { "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };

		/// <summary>Initializes default values.</summary>
		public LogFilter()
		{
			From = DateTime.Today;
			To = To ?? DateTime.Now;
			Levels = DefaultLevels;
			LoggersInclude = new String[0];
			LoggersExclude = new String[0];
		}

		/// <summary>
		/// Merges the filter provided into the instance using logical OR.
		/// </summary>
		/// <param name="source">A source filter to merge from.</param>
		public void Merge(LogFilter source)
		{
			if (source == null)
			{
				throw new ArgumentNullException();
			}

			if (!String.IsNullOrEmpty(source.Application))
			{
				Application = source.Application;
			}
			if (source.From.HasValue)
			{
				From = source.From;
			}
			if (source.To.HasValue)
			{
				To = source.To;
			}
			if (!String.IsNullOrEmpty(source.LoggingID))
			{
				LoggingID = source.LoggingID;
			}
			if (!String.IsNullOrEmpty(source.SessionID))
			{
				SessionID = source.SessionID;
			}
			if (!String.IsNullOrEmpty(source.Message))
			{
				Message = source.Message;
			}
			if (!String.IsNullOrEmpty(source.MachineAddress))
			{
				MachineAddress = source.MachineAddress;
			}
			if (!String.IsNullOrEmpty(source.RequestAddress))
			{
				RequestAddress = source.RequestAddress;
			}
			if (PageSize == 0)
			{
				PageSize = source.PageSize;
			}
			if (Page >= 0)
			{
				Page = source.Page;
			}
			if (!String.IsNullOrEmpty(source.User))
			{
				User = source.User;
			}

			String[] levels1 = Levels ?? new String[0];
			String[] levels2 = source.Levels ?? new String[0];
			Levels = levels1.Union(levels2).ToArray();

			String[] loggersEx1 = LoggersExclude ?? new String[0];
			String[] loggersEx2 = source.LoggersExclude ?? new String[0];
			LoggersExclude = loggersEx1.Union(loggersEx2).ToArray();

			String[] loggersIn1 = LoggersInclude ?? new String[0];
			String[] loggersIn2 = source.LoggersInclude ?? new String[0];
			LoggersInclude = loggersIn1.Union(loggersIn2).ToArray();
		}

		/// <summary>Event log application (sub-system) (optional).</summary>
		[DataMember]
		public String Application { get; set; }

		/// <summary>From date (optional, inclusive).</summary>
		[DataMember]
		public DateTime? From { get; set; }

		/// <summary>To date (optional, inclusive).</summary>
		[DataMember]
		public DateTime? To { get; set; }

		/// <summary>Logging identifier or its starting part (optional).</summary>
		[DataMember]
		public String LoggingID { get; set; }

		/// <summary>Session identifier or its starting part (optional).</summary>
		[DataMember]
		public String SessionID { get; set; }

		/// <summary>User identity (optional).</summary>
		[DataMember]
		public String User { get; set; }

		/// <summary>Machine adrress where the request was processed (optional).</summary>
		[DataMember]
		public String MachineAddress { get; set; }

		/// <summary>Incoming request address (optional)</summary>
		[DataMember]
		public String RequestAddress { get; set; }

		/// <summary>Part (any) of log message (optional).</summary>
		[DataMember]
		public String Message { get; set; }

		/// <summary>A list of loggers to include in the result (optional).</summary>
		[DataMember]
		public String[] LoggersInclude { get; set; }

		/// <summary>A list of loggers to exclude from the result (optional).</summary>
		[DataMember]
		public String[] LoggersExclude { get; set; }

		/// <summary>Log levels to include in the result (optional, empty means all).</summary>
		[DataMember]
		public String[] Levels { get; set; }

		/// <summary>Page to return. <seealso cref="PageSize"/></summary>
		[DataMember]
		public Int32 Page { get; set; }

		/// <summary>Page size. When zero or negative the actual size is determined by specific implementation. <see cref="Page"/></summary>
		[DataMember]
		public Int32 PageSize { get; set; }

		/// <summary>
		/// Converts current instance into human readable string.
		/// </summary>
		/// <returns>Human readable string.</returns>
		public override String ToString()
		{
			String result = SerializationUtil.SerializeJson(this);
			return result;
		}
	}
}
