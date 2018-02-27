using System;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Repository interface that abstracts storage-related code (DAL) for <see cref="ILogReader"/>.
	/// </summary>
	public interface ILogReaderRepository
	{
		/// <summary>
		/// Returns an instance of <see cref="LogEntry"/> from the underlaying storage by its ID.
		/// </summary>
		/// <param name="id">Event log entry key.</param>
		/// <returns>An instance of <see cref="LogEntry"/> that corresponds to the given key.</returns>
		LogEntry ReadEntry(Int64 id);

		/// <summary>
		/// Returns entries from the underlaying storage according to the specified filter parameters.
		/// </summary>
		/// <param name="filter">Filter parameters.</param>
		/// <returns>Event entries that satisfy the filter parameters. The result is ordered by <see cref="LogEntry.CreatedOn"/>.
		/// </returns>
		LogEntry[] ReadBatch(LogFilter filter);
	}
}
