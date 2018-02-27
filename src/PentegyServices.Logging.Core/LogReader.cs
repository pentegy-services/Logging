using PentegyServices.Logging.Core;
using System;
using System.ServiceModel;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Default implementation of <see cref="ILogReader"/>.
	/// The service depends on <see cref="ILoggingConfiguration"/> and <see cref="ILogReaderRepository"/> interfaces to abstract 
	/// from specific implementation details.
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, 
		Namespace = Namespace.Service)]
    public class LogReader
		: ILogReader
    {
        /// <summary>Dependency <see cref="ILogReaderRepository"/> to read entries from.</summary>
        protected readonly ILogReaderRepository LogReaderRepository;

        /// <summary>Dependency <see cref="ILoggingConfiguration"/> to parameterize the service.</summary>
        protected readonly ILoggingConfiguration Configuration;

        /// <summary>
        /// Creates an instance of <see cref="LogReader"/>.
        /// </summary>
        /// <param name="configuration">Instance of <see cref="ILoggingConfiguration"/> used to parameterize the service.</param>
        /// <param name="logReaderRepository">Instance of <see cref="ILogReaderRepository"/> used to read entries.</param>
        public LogReader(ILoggingConfiguration configuration, ILogReaderRepository logReaderRepository)
        {
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			if (logReaderRepository == null)
			{
				throw new ArgumentNullException("logReaderRepository");
			}

            Configuration = configuration;
            LogReaderRepository = logReaderRepository;
        }

        #region ILogReader Members

        /// <summary>
        /// Returns entries using <see cref="LogReaderRepository"/> according to the specified filter parameters.
        /// </summary>
        /// <param name="filter">Filter parameters.</param>
        /// <returns>Event entries that satisfy the filter parameters.</returns>
        public LogEntry[] GetEventLog(LogFilter filter)
        {
			if (filter == null)
			{
				filter = new LogFilter();
			}

            LogEntry[] result = LogReaderRepository.ReadBatch(filter);
            return result;
        }

        /// <summary>
        /// Returns a single <see cref="LogEntry"/> for the given key using <see cref="LogReaderRepository"/>.
        /// </summary>
        /// <param name="id">Event log entry key.</param>
        /// <returns>An instance of <see cref="LogEntry"/> that corresponds to the given key.</returns>
        public LogEntry GetEventLogEntry(Int64 id)
        {
            LogEntry result = LogReaderRepository.ReadEntry(id);
            return result;
        }

        #endregion
    }
}
