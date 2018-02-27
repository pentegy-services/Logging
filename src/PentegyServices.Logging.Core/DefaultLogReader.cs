using System;
using System.Collections.Generic;
using System.Linq;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Naive, in-memory implementation of <see cref="ILogReader"/> that uses LINQ for filtering.
	/// The implementation is case insensitive (see <see cref="StringComparison.InvariantCultureIgnoreCase"/>).
	/// </summary>
	public class DefaultLogReader
		: ILogReader
	{
		readonly IEnumerable<LogEntry> source;
		readonly ILoggingConfiguration configuration;

		/// <summary>
		/// Creates a new instance from the source sequence provided.
		/// </summary>
		/// <param name="source">Source log sequence to use.</param>
		/// <param name="configuration">Configuration to use.</param>
		public DefaultLogReader(IEnumerable<LogEntry> source, ILoggingConfiguration configuration)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			this.source = source;
			this.configuration = configuration;
		}

		#region ILogReader Members

		/// <summary>
		/// Returns entries from the event log according to the specified filter parameters.
		/// </summary>
		/// <param name="filter">Filter parameters.</param>
		/// <returns>Event entries that satisfy the filter parameters. The result is ordered by <see cref="LogEntry.CreatedOn"/>.
		/// </returns>
		public LogEntry[] GetEventLog(LogFilter filter)
		{
			if (filter == null)
			{
				filter = new LogFilter();
			}

			var query = source;

			if (!String.IsNullOrEmpty(filter.Application))
			{
				query = query.Where(x => x.Application.StartsWith(filter.Application, StringComparison.InvariantCultureIgnoreCase));
			}

			if (filter.From.HasValue)
			{
				query = query.Where(x => x.CreatedOn >= filter.From);
			}

			if (filter.To.HasValue)
			{
				query = query.Where(x => x.CreatedOn <= filter.To);
			}

			if (filter.Levels != null && filter.Levels.Length > 0)
			{
				query = query.Where(x => filter.Levels.Contains(x.Level, StringComparer.InvariantCultureIgnoreCase));
			}

			if (!String.IsNullOrEmpty(filter.LoggingID))
			{
				query = query.Where(x => x.LoggingID.StartsWith(filter.LoggingID, StringComparison.InvariantCultureIgnoreCase));
			}

			if (!String.IsNullOrEmpty(filter.SessionID))
			{
				query = query.Where(x => x.SessionID.StartsWith(filter.SessionID, StringComparison.InvariantCultureIgnoreCase));
			}

			if (!String.IsNullOrEmpty(filter.User))
			{
				query = query.Where(x => x.UserIdentity.Equals(filter.User, StringComparison.InvariantCultureIgnoreCase));
			}

			if (!String.IsNullOrEmpty(filter.MachineAddress))
			{
				query = query.Where(x => x.MachineAddress.Equals(filter.MachineAddress, StringComparison.InvariantCultureIgnoreCase));
			}

			if (!String.IsNullOrEmpty(filter.RequestAddress))
			{
				query = query.Where(x => x.RequestAddress.Equals(filter.RequestAddress, StringComparison.InvariantCultureIgnoreCase));
			}

			if (!String.IsNullOrEmpty(filter.Message))
			{
				query = query.Where(x => x.Message.IndexOf(filter.Message, StringComparison.InvariantCultureIgnoreCase) >= 0);
			}

			if (filter.LoggersExclude != null && filter.LoggersExclude.Length > 0)
			{
				query = query.Where(x => !filter.LoggersExclude.Contains(x.Logger, StringComparer.InvariantCultureIgnoreCase));
			}

			if (filter.LoggersInclude != null && filter.LoggersInclude.Length > 0)
			{
				query = query.Where(x => filter.LoggersInclude.Contains(x.Logger, StringComparer.InvariantCultureIgnoreCase));
			}

			Int32 rowsToQuery = filter.PageSize > 0 ? filter.PageSize : configuration.MaxRowsToQueryEventLog;
			Int32 startRow = filter.Page * rowsToQuery;

			LogEntry[] result = query
				.OrderByDescending(x => x.CreatedOn)
				.Skip(startRow)
				.Take(rowsToQuery)
				.ToArray();

			return result;
		}

		/// <summary>
		/// Returns a single <see cref="LogEntry"/> for the given key.
		/// </summary>
		/// <param name="id">Event log entry key.</param>
		/// <returns>An instance of <see cref="LogEntry"/> that corresponds to the given key.</returns>
		public LogEntry GetEventLogEntry(Int64 id)
		{
			LogEntry entry = source.SingleOrDefault(x => x.ID == id);
			return entry;
		}

		#endregion
	}
}
