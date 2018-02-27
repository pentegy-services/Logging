using log4net;
using log4net.Appender;
using log4net.Core;
using PentegyServices.Logging.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Asynchronous, high-performance, WCF-enabled logger built on top of log4net:
	/// <list type="bullet">
	///		<item>
	///			<description>Tries to write to <see cref="ILogWriter"/> WCF service or falls back to inherited <see cref="RollingFileAppender"/>.</description>
	///		</item>
	///		<item>
	///			<description>Dumping is triggered either by exceeding <see cref="BufferThreshold"/> or <see cref="TimeThresholdInMilliseconds"/> depending on what happens sooner.</description>
	///		</item>
	///		<item>
	///			<description>The buffer is <see cref="Queue{T}"/> with configurable capacity.</description>
	///		</item>
	///		<item>
	///			<description><see cref="ThreadPool"/> is used for worker threads.</description>
	///		</item>
	///		<item>
	///			<description>Checks for maximum message length (<see cref="MaxEntryLength"/>). When exceeded trims the message but also writes full entries to file.</description>
	///		</item>
	///		<item>
	///			<description>Adds an alert entry if it needs to fall back to a file. If fall back fails writes an error message to standard <see cref="Trace"/>.</description>
	///		</item>
	/// </list>
	/// 
	/// <para>
	/// The appender adds a few public properties to configure its behavior:
	/// <code>
	///	&lt;appender name="ServiceAppender" type="Core.Logging.ServiceAppender, Core.Logging"&gt;
	///		&lt;!-- ServiceAppender settings start --&gt;
	///		&lt;bufferCapacity value="8192" /&gt;
	///		&lt;bufferThreshold value="4096" /&gt;
	///		&lt;timeThresholdInMilliseconds value="2000" /&gt;
	///		&lt;serviceEndpoInt32Name value="ILogWriter" /&gt;
	///		&lt;maxEntryLength value="4096" /&gt;
	///		&lt;applicationName value="Core" /&gt;
	///		&lt;!-- ServiceAppender settings end --&gt;
	///		&lt;file value="Core.Logging.Web.UI.service-fallback.log" /&gt;
	///		&lt;!-- The rest of log4net RollingFileAppender settings here --&gt;
	///	&lt;/appender&gt;
	/// </code>
	/// </para>
	/// </summary>
	public class ServiceAppender
		: RollingFileAppender
	{
		static ServiceAppender()
		{
			GlobalContext.Properties[LogProp.MachineAddress] = SysUtil.GetMachineName();
		}

		const Int32 BufferCapacityDefault = 512; // the recommended capacity value is 2 or 4 times greater than the threshold
		const Int32 BufferThresholdDefault = 128;
		const Int32 TimeThresholdInMillisecondsDefault = 1000;
		const Int32 MaxEntryLengthDefault = 1024 * 8;

		/// <summary>
		/// Initial buffer capacity (when a number of items in buffer is less that this value, adding a new item is O(1)).
		/// Should be a bit greater than <see cref="BufferThreshold"/> so <see cref="List{T}.Add(T)"/> don't need to reallocate memory often.
		/// Default value is <c>512</c>.
		/// </summary>
		public Int32 BufferCapacity { get; set; }

		/// <summary>How many items should be in buffer before starting to dump.
		/// Default value is <c>128</c>.
		/// </summary>
		public Int32 BufferThreshold { get; set; }

		/// <summary>How long to sleep between dumping. 
		/// This ensures that if the buffer does not fill or fill very slowly (for example, the system is idle) it still will be flushed within this period.
		/// Default value is <c>1000</c>.
		/// </summary>
		public Int32 TimeThresholdInMilliseconds { get; set; }

		/// <summary>Service endpoInt32 name to use (system.serviceModel/client/endpoInt32).
		/// Default value is name of <see cref="ILogWriter"/> Int32erface.
		/// </summary>
		public String ServiceEndpointName { get; set; }

		/// <summary>
		/// <see cref="ServiceEndpoint"/> to use.
		/// </summary>
		public ServiceEndpoint ServiceEndpoint { get; set; }

		/// <summary>Maximum entry length (in characters). 
		/// If entry length exceeds the value it's trimmed but the original (full) entry is still written to the file.
		/// Default value is <c>1024 * 8</c>.
		/// </summary>
		public Int32 MaxEntryLength { get; set; }

		/// <summary>Name of an application (component or sub-system) the appender belongs to. If not configured will default to the process name.</summary>
		public String ApplicationName { get; set; }

		ChannelFactory<ILogWriter> factory = null;

		/// <summary>
		/// We have to store <see cref="LogEntry"/> converted from original log4net <see cref="LoggingEvent"/> (because log4net 
		/// context properties must be resolved in a caller thread) along with originals (to be able to fallback if we cannot write to the service).
		/// That's why we need <see cref="KeyValuePair{K,V}"/>. A <see cref="Queue{T}"/> is needed to have fixed batch size when writing 
		/// to the service: to not exceed WCF binding/serializer quotas and guarantee (more or less) order of events (in case when 
		/// their number is greater than the service can process).
		/// </summary>
		Queue<KeyValuePair<LoggingEvent, LogEntry>> buffer = null;

		/// <summary>
		/// Timer is required to flush non completely filled buffer which is useful when the system is at idle.
		/// </summary>
		Timer timer = null;
		String application = Process.GetCurrentProcess().ProcessName;
		Object bufferSync = new Object(); // sync object required for exclusive access to the buffer
		/// <summary>A number of scheduled/non finished work items.</summary>
		protected Int64 WorkersCount = 0; 

		/// <summary>Default ctor.</summary>
		public ServiceAppender()
		{
			// initialize properties in case if the appender is created manually at run-time
			BufferCapacity = BufferCapacityDefault;
			BufferThreshold = BufferThresholdDefault;
			TimeThresholdInMilliseconds = TimeThresholdInMillisecondsDefault;
			MaxEntryLength = MaxEntryLengthDefault;
			ServiceEndpointName = typeof(ILogWriter).Name;
		}

		/// <summary>Returns current number of items in the buffer. This is a locking operation so do not call it too often.</summary>
		public Int32 ItemsInBuffer
		{
			get
			{
				lock (bufferSync)
				{
					return buffer != null ? buffer.Count : 0;
				}
			}
		}

		void ResetTimer()
		{
			Int64 time = TimeThresholdInMilliseconds <= 0 ? TimeThresholdInMillisecondsDefault : TimeThresholdInMilliseconds;
			timer.Change(time, time);
		}

		void AppendToBuffer(Action<Queue<KeyValuePair<LoggingEvent, LogEntry>>> addAction)
		{
			Boolean dump;
			lock (bufferSync)
			{
				if (buffer == null)
				{
					buffer = new Queue<KeyValuePair<LoggingEvent, LogEntry>>(BufferCapacity <= 0 ? BufferCapacityDefault : BufferCapacity);
				}
				addAction(buffer);
				dump = buffer.Count >= BufferThreshold;
			}
			if (dump)
			{
				if (!StartProcessing())
				{
					return;
				}

				ResetTimer(); // if need to dump when appending - no need for the timer to trigger
			}
		}

		Boolean StartProcessing()
		{
			try
			{
				ThreadPool.QueueUserWorkItem(ProcessBuffer);
				Interlocked.Increment(ref WorkersCount);
			}
			catch (Exception ex)
			{
				Trace.TraceError("ServiceAppender: ThreadPool.QueueUserWorkItem failed: {0}", ex);
				return false;
			}
			return true;
		}

		void ProcessBuffer(Object state)
		{
			try
			{
				IEnumerable<KeyValuePair<LoggingEvent, LogEntry>> bufferToDump = null;
				lock (bufferSync)
				{
					if (buffer != null && buffer.Count > 0)
					{
						if (buffer.Count <= BufferThreshold) // if less than the threshold - grab the whole buffer
						{
							Trace.TraceInformation("ServiceAppender: {0} entries to dump", buffer.Count);
							bufferToDump = buffer; // copy the poInt32er (as fast as possible)
							buffer = null; // and reset the original so the next call to AppendToBuffer() will recreate it
										   // Queue{T} has very aggressive capacity growth. resetting it to null will help to reduce occupied memory (when the load is lowering)
						}
						else // otherwise get BufferThreshold first items in order of arrival
						{
							Trace.TraceInformation("ServiceAppender: {0} entries of {1} to dump", BufferThreshold, buffer.Count);
							var list = new List<KeyValuePair<LoggingEvent, LogEntry>>(BufferThreshold);
							for (Int32 i = 0; i < BufferThreshold; i++)
							{
								list.Add(buffer.Dequeue());
							}
							bufferToDump = list;
						}
					}
				}

				if (bufferToDump != null)
				{
					Dump(bufferToDump);
				}
			}
			finally
			{
				Interlocked.Decrement(ref WorkersCount);
			}
		}


		void Dump(IEnumerable<KeyValuePair<LoggingEvent, LogEntry>> entries)
		{
			// Split the collection to entries that exceed MaxEntrySize and ones that don't

			var fileEntries = new List<KeyValuePair<LoggingEvent, LogEntry>>();
			var serviceEntries = new List<KeyValuePair<LoggingEvent, LogEntry>>();
			Int32 maxLength = MaxEntryLength > 0 ? MaxEntryLength : MaxEntryLengthDefault;
			var message = string.Format("...[trimmed to {0} chars]", maxLength);

			foreach (var entry in entries)
			{
				if (entry.Value.Message != null && entry.Value.Message.Length > maxLength && entry.Value.Message.Length > message.Length)
				{
					Trace.TraceWarning("ServiceAppender: Entry length {0} exceeds {1} limit", entry.Value.Message, maxLength);
					entry.Value.Message = entry.Value.Message.Substring(0, maxLength - message.Length) + message;
					fileEntries.Add(entry);
				}
				serviceEntries.Add(entry);
			}

			// try to write entries that don't exceed the limit
			try
			{
				Trace.TraceInformation("ServiceAppender: Writing batch of {0}/{1} entries to the service (workers: {2})...", serviceEntries.Count, entries.Count(), Interlocked.Read(ref WorkersCount));
				WriteToService(serviceEntries.Select(x => x.Value).ToArray());
			}
			catch (Exception ex)
			{
				Trace.TraceError("ServiceAppender: Cannot write {0} entries to the service: {1}", entries.Count(), ex);

				var data = new LoggingEventData
				{
					Level = Level.Alert,
					LoggerName = GetType().Name + "-fallback",
					Message = String.Format("Cannot write {0} entries to the service", entries.Count()),
					ThreadName = Thread.CurrentThread.Name,
					TimeStamp = DateTime.Now,
					Identity = System.Security.Principal.WindowsIdentity.GetCurrent().Name,
					ExceptionString = ex.ToString()
				};
				var errorEvent = new LoggingEvent(data);

				// add alert entry
				Fallback(new LoggingEvent[] { errorEvent });
				// now failed entries
				Fallback(serviceEntries.Select(x => x.Key).ToArray());
			}

			// Now write trimmed entries
			if (fileEntries.Count > 0)
			{
				Trace.TraceInformation("ServiceAppender: Writing batch of {0}/{1} entries to the file...", fileEntries.Count, entries.Count());
				Fallback(fileEntries.Select(x => x.Key).ToArray());
			}
		}

		Object fallbackSync = new Object();

		void Fallback(LoggingEvent[] entries)
		{
			// without the lock we get weird log4net 'System.ArgumentOutOfRangeException: Index and count must refer to a location within the buffer.' in the trace output
			lock (fallbackSync)
			{
				try
				{
					base.Append(entries);
				}
				catch (Exception ex)
				{
					Trace.TraceError("ServiceAppender: Fallback failed, entries lost! {0}. ", ex);
				}
			}
		}

		/// <summary>
		/// Initializes Int32ernal structures (timer and service factory) that depend on configuration properties.
		/// </summary>
		public override void ActivateOptions()
		{
			base.ActivateOptions();

			// appender's properties are set, can access ServiceEndpointName and others now

			if (GlobalContext.Properties[LogProp.Application] == null) // if the hosting process forgot to set it 
			{
				GlobalContext.Properties[LogProp.Application] = ApplicationName;
			}

			// seems like log4net creates one instance of appenders per domain so we do not need to lock
			if (ServiceEndpoint == null)
			{
				String endpointName = String.IsNullOrEmpty(ServiceEndpointName) ?
					typeof(ILogWriter).Name : ServiceEndpointName;
				factory = new ChannelFactory<ILogWriter>(endpointName);
			}
			else
			{
				factory = new ChannelFactory<ILogWriter>(ServiceEndpoint);
			}

			// create a timer that will dump non-empty buffer when triggered
			timer = new Timer(new TimerCallback(_ =>
			{
				Int32 dump = 0;
				lock (bufferSync)
				{
					if (buffer != null)
					{
						dump = buffer.Count;
					}
				}

				if (dump > 0) // and there is at least one entry - dump it
				{
					Trace.TraceInformation("ServiceAppender: Timer threshold exceeded: {0}", dump);
					if (!StartProcessing())
					{
						return;
					}
				}
			}));
			ResetTimer(); // start it
		}

		/// <summary>
		/// Converts log4net entry to <see cref="LogEntry"/> and stores both in the buffer for later processing.
		/// </summary>
		/// <param name="loggingEvent"></param>
		protected override void Append(LoggingEvent loggingEvent)
		{
			// We have to resolve context properties in the calling thread (here), not in a working thread.
			AppendToBuffer(x => x.Enqueue(new KeyValuePair<LoggingEvent, LogEntry>(loggingEvent, Convert(loggingEvent))));
		}

		/// <summary>
		/// Converts an array of log4net entries to <see cref="LogEntry"/> and stores both in the buffer for later processing.
		/// </summary>
		/// <param name="loggingEvents"></param>
		protected override void Append(LoggingEvent[] loggingEvents)
		{
			// We have to resolve context properties in the calling thread (here), not in a working thread.
			AppendToBuffer(x =>
			{
				foreach (var loggingEvent in loggingEvents)
				{
					x.Enqueue(new KeyValuePair<LoggingEvent, LogEntry>(loggingEvent, Convert(loggingEvent)));
				}
			});
		}

		/// <summary>
		/// Override this method if you need to provide additional data in <see cref="LogEntry.CustomData"/> collection.
		/// </summary>
		/// <param name="e">log4net <see cref="LoggingEvent"/> instance to convert.</param>
		/// <returns>Instance of <see cref="LogEntry"/> converted from the source provided.</returns>
		protected virtual LogEntry Convert(LoggingEvent e)
		{
			Func<String> formatMessage = () =>
				{
					String result = e.MessageObject != null ? e.RenderedMessage : "";
					if (e.MessageObject != null && e.ExceptionObject != null)
					{
						result += Environment.NewLine;
					}
					result += e.ExceptionObject != null ? e.GetExceptionString() : "";
					return result;
				};

			return new LogEntry()
			{
				Application = (GlobalContext.Properties[LogProp.Application] ?? application).ToString(),
				CreatedOn = e.TimeStamp,
				Level = e.Level.DisplayName,
				Logger = e.LoggerName,
				Message = formatMessage(),
				LoggingID = (e.LookupProperty(LogProp.LoggingID) ?? "").ToString(),
				SessionID = (e.LookupProperty(LogProp.SessionID) ?? "").ToString(),
				ThreadID = Thread.CurrentThread.ManagedThreadId.ToString(),
				MachineAddress = (GlobalContext.Properties[LogProp.MachineAddress] ?? "").ToString(),
				RequestAddress = (e.LookupProperty(LogProp.RequestAddress) ?? "").ToString(),
				UserIdentity = (e.LookupProperty(LogProp.UserIdentity) ?? "").ToString()
			};
		}

		/// <summary>
		/// Tries to flush the buffer and disposes the resources
		/// </summary>
		protected override void OnClose()
		{
			if (timer != null) // stop the timer first so it won't trigger during closing
			{
				timer.Dispose();
				timer = null;
			}

			ProcessBuffer(null); // try to flush if anything left

			base.OnClose();
		}

		/// <summary>
		/// Disposes service factory.
		/// </summary>
		~ServiceAppender()
		{
			if (factory != null)
			{
				((ICommunicationObject)factory).Dispose();
				factory = null;
			}
		}

		/// <summary>
		/// Performs service call to <see cref="ILogWriter"/> to write the entries.
		/// </summary>
		/// <param name="batch">An array of <see cref="LogEntry"/> to write.</param>
		protected void WriteToService(LogEntry[] batch)
		{
			ILogWriter service = factory.CreateChannel();
			try
			{
				Boolean success = service.Write(batch);
				if (!success)
				{
					throw new InvalidOperationException("Cannot write to the logging service. Check the service log.");
				}
			}
			finally
			{
				((ICommunicationObject)service).Dispose();
			}
		}

		/// <summary>
		/// Blocking method that returns when the buffer is empty.
		/// <seealso cref="PentegyServices.Logging.Core.SysUtil.FlushLogBuffers()"/>
		/// </summary>
		/// <param name="pollInterval">Polling Int32erval in milliseconds.</param>
		public void WaitForFinish(Int32 pollInterval = 1000)
		{
			Trace.TraceInformation("ServiceAppender: Waiting for ServiceAppender to finish...");
			// lets wait till buffer is empty, otherwise it may affect subsequent tests
			// also, we'll be able to see the trace output of the background workers
			while (ItemsInBuffer != 0)
			{
				Trace.TraceWarning("ServiceAppender: ItemsInBuffer: {0}. WorkersCount: {1}. Waiting for {2}s more...", ItemsInBuffer, Interlocked.Read(ref WorkersCount), pollInterval / 1000);
				Thread.Sleep(pollInterval);
			}

			// the buffer is empty but the workers can still be running
			
			Int64 workersCount;
			do
			{
				workersCount = Interlocked.Read(ref WorkersCount);
				if (workersCount > 0)
				{
					Trace.TraceWarning("ServiceAppender: still has {0} workers running! Waiting for {1}s more...", workersCount, pollInterval / 1000);
					Thread.Sleep(pollInterval);
				}
			} while (workersCount > 0);
		}
	}
}
