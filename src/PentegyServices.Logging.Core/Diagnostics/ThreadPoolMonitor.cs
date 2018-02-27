using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace PentegyServices.Logging.Core.Diagnostics
{
	/// <summary>
	/// Helper class to monitor .NET <see cref="System.Threading.ThreadPool"/>.
	/// Based on http://msdn.microsoft.com/en-us/library/ff650682.aspx.
	/// </summary>
	public class ThreadPoolMonitor // TODO: Finish me please
	{
		const String PeriodSettingKey = "threadPoolMonitor";
		static Int32 monitorPeriod;
		static Timer monitorTimer = null;

		static ThreadPoolMonitor()
		{
			String monitorPeriodString = ConfigurationManager.AppSettings[PeriodSettingKey];
			Int32.TryParse(monitorPeriodString, out monitorPeriod);

			if (monitorPeriod > 0) // check if enabled
			{
				Trace.TraceInformation("Enabling ThreadPoolMonitor with {0} period...", monitorPeriod);
				CreateCounters();
				monitorTimer = new Timer(SetCounters, null, 0, monitorPeriod);
			}
		}

		//public const string ConfigurationKey = "threadPoolMonitor";

		/// <summary>Performance counter category name used to register <see cref="ThreadPoolMonitor"/> counters.</summary>
		public const String CounterCategoryName = ".NetThreadPoolCounters";

		const String CounterAvailableWorkerThreads = "Available Worker Threads";
		const String CounterAvailableIOThreads = "Available IO Threads";
		const String CounterMaxWorkerThreads = "Max Worker Threads";
		const String CounterMaxIOThreads = "Max IO Threads";
		const String CounterMinWorkerThreads = "Min Worker Threads";
		const String CounterMinIOThreads = "Min IO Threads";

		static PerformanceCounterCategory CounterCategory = null;

		static CounterCreationData CreateCounter(String name, String description)
		{
			var counterData = new CounterCreationData
			{
				CounterName = name,
				CounterHelp = description,
				CounterType = PerformanceCounterType.NumberOfItems32
			};
			return counterData;
		}

		/// <summary>
		/// Creates the performance counters and registers them in <see cref="CounterCategoryName"/> category.
		/// </summary>
		internal static void CreateCounters()
		{
			var counters = new CounterCreationDataCollection
			{
				CreateCounter(CounterAvailableWorkerThreads, "The difference between the maximum number of thread pool worker threads and the number currently active."),
				CreateCounter(CounterAvailableIOThreads, "The difference between the maximum number of thread pool IO threads and the number currently active."),

				CreateCounter(CounterMaxWorkerThreads, "The number of requests to the thread pool that can be active concurrently. All requests above that number remain queued until thread pool worker threads become available."),
				CreateCounter(CounterMaxIOThreads, "The number of requests to the thread pool that can be active concurrently. All requests above that number remain queued until thread pool IO threads become available."),

				CreateCounter(CounterMinWorkerThreads, "The minimum number of worker threads that the thread pool creates on demand."),
				CreateCounter(CounterMinIOThreads, "The minimum number of asynchronous I/O threads that the thread pool creates on demand.")
			};

			// delete the category if it already exists
			if (PerformanceCounterCategory.Exists(CounterCategoryName))
			{
				PerformanceCounterCategory.Delete(CounterCategoryName);
			}

			// bind the counters to the PerformanceCounterCategory
			CounterCategory = PerformanceCounterCategory.Create(CounterCategoryName, "", PerformanceCounterCategoryType.MultiInstance, counters);
		}

		static void  SetCounter(String counterName, Int64 rawValue)
		{
			var counter = new PerformanceCounter
			{
				CategoryName = CounterCategoryName,
				CounterName = counterName
			};
			Process process = Process.GetCurrentProcess();
			counter.InstanceName = process.ProcessName;
			counter.MachineName = process.MachineName;
			counter.ReadOnly = false;
			counter.RawValue = rawValue;
			counter.Close();
		}

		static void SetCounters(Object state)
		{
			// use ThreadPool to get the current status
			Int32 availableWorker, availableIO;
			Int32 maxWorker, maxIO, minWorker, minIO;

			ThreadPool.GetAvailableThreads(out availableWorker, out availableIO);
			ThreadPool.GetMaxThreads(out maxWorker, out maxIO);
			ThreadPool.GetMinThreads(out minWorker, out minIO);

			SetCounter(CounterAvailableWorkerThreads, availableWorker);
			SetCounter(CounterAvailableIOThreads, availableIO);

			SetCounter(CounterMaxWorkerThreads, maxWorker);
			SetCounter(CounterMaxIOThreads, maxIO);

			SetCounter(CounterMinWorkerThreads, minWorker);
			SetCounter(CounterMinIOThreads, minIO);
		}

	}
}
