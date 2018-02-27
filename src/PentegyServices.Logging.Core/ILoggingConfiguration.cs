using System;

namespace PentegyServices.Logging.Core
{
    /// <summary>
    /// Contains configuration parameters required for logging subsystem to work.
    /// </summary>
    public interface ILoggingConfiguration
    {
        /// <summary>A maximum number of rows to return when querying the event log via <see cref="ILogReader"/>.</summary>
        Int32 MaxRowsToQueryEventLog { get; }

        /// <summary>Default number of rows for displaying purposes. Must be less or equal to <see cref="MaxRowsToQueryEventLog"/>.</summary>
        Int32 DefaultPageSize { get; }
    }
}
