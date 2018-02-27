using System;
using System.ServiceModel;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Log reader service interface that can be used to retrieve previosly persisted <see cref="LogEntry"/> instances.
	/// <seealso cref="ILogWriter"/>.
	/// </summary>
	[ServiceContract(Namespace = Namespace.ServiceContract)]
    public interface ILogReader
    {
        /// <summary>
        /// Returns entries from the event log according to the specified filter parameters.
        /// </summary>
        /// <param name="filter">Filter parameters.</param>
        /// <returns>Event entries that satisfy the filter parameters. The result is ordered by <see cref="LogEntry.CreatedOn"/>.
        /// </returns>
        [OperationContract]
        LogEntry[] GetEventLog(LogFilter filter);

        /// <summary>
        /// Returns a single <see cref="LogEntry"/> for the given key.
        /// </summary>
        /// <param name="id">Event log entry key.</param>
        /// <returns>An instance of <see cref="LogEntry"/> that corresponds to the given key.</returns>
        [OperationContract]
        LogEntry GetEventLogEntry(Int64 id);
    }
}
