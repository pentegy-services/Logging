using PentegyServices.Logging.Core;
using System;
using System.Diagnostics;
using System.ServiceModel;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Default implementation of <see cref="ILogWriter"/>.
	/// The service depends on <see cref="ILoggingConfiguration"/> and <see cref="ILogWriterRepository"/> interfaces to abstract 
	/// from specific implementation details.
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, 
		Namespace = Namespace.Service)]
	public class LogWriter
		: ILogWriter
	{
		/// <summary>Dependency <see cref="ILogWriterRepository"/> to write entries to.</summary>
		protected readonly ILogWriterRepository LogWriterRepository;

		/// <summary>Dependency <see cref="ILoggingConfiguration"/> to parameterize the service.</summary>
		protected readonly ILoggingConfiguration Configuration;

		/// <summary>
		/// Creates an instance of <see cref="LogWriter"/>.
		/// </summary>
		/// <param name="configuration">Instance of <see cref="ILoggingConfiguration"/> used to parameterize the service.</param>
		/// <param name="logWriterRepository">Instance of <see cref="ILogWriterRepository"/> used to write entries.</param>
		public LogWriter(ILoggingConfiguration configuration, ILogWriterRepository logWriterRepository)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			if (logWriterRepository == null)
			{
				throw new ArgumentNullException("logWriterRepository");
			}

			Configuration = configuration;
			LogWriterRepository = logWriterRepository;
		}

		#region ILogWriter Members

		/// <summary>
		/// Persists the given batch using <see cref="LogWriterRepository"/>.
		/// </summary>
		/// <param name="batch">An array of <see cref="LogEntry"/> to persist.</param>
		/// <returns><c>true</c> if the batch has been persisted successfully, otherwise <c>false</c> (catches all the exceptions).</returns>
		public Boolean Write(LogEntry[] batch)
		{
			if (batch == null)
			{
				return true; // nothing to process
			}

			Trace.TraceInformation("Got batch: {0}", batch.Length);
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			try
			{
				LogWriterRepository.WriteBatch(batch);
				stopwatch.Stop();
				Trace.TraceInformation("Dumped {0} entries in {1}ms", batch.Length, stopwatch.Elapsed.TotalMilliseconds);
				return true;
			}
			catch (Exception ex)
			{
				Trace.TraceError("Dumping to {0} failed: {1}", LogWriterRepository.GetType().Name, ex);
			}
			return false;
		}

		#endregion
	}
}
