using PentegyServices.Logging.Core;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Represents an event entry.
	/// <see cref="ServiceAppender"/> uses it to pass to WCF services implementing <see cref="ILogWriter"/>.
	/// </summary>
	[DataContract(Namespace = Namespace.DataContract)]
	[Serializable]
	public class LogEntry
		: IComparable<LogEntry>
	{
		/// <summary>Default ctor.</summary>
		public LogEntry()
		{
			CustomData = new Dictionary<String, String>();
		}

		/// <summary>
		/// Unique event identifier in the underlaying storage.
		/// </summary>
		[DataMember]
		public Int64 ID { get; set; }

		/// <summary>
		/// Identifier that distinguishes the log if it's shared by multiple applications or sub-systems.
		/// </summary>
		[DataMember]
		public String Application { get; set; }

		/// <summary>
		/// <see cref="DateTime"/> value when an event happened. Note, that <see cref="DateTime"/> 
		/// has a precision so it's possible to have a few events with the same value and one cannnot tell in which order they happended.
		/// Also, your underlaying storage may have its own precision for corresponding type (for example, precision of <c>datetime</c>
		/// type in Sql Server is 3.33 milliseconds which is worse than .NET type).
		/// </summary>
		[DataMember]
		public DateTime CreatedOn { get; set; }

		/// <summary>
		/// log4net event level (INFO, WARN, ERROR, DEBUG, FATAL).
		/// </summary>
		[DataMember]
		public String Level { get; set; }

		/// <summary>
		/// log4net logger name.
		/// </summary>
		[DataMember]
		public String Logger { get; set; }

		/// <summary>
		/// A special code that is used for end-to-end logging.
		/// Usually originated in UI applications and not changed when the processing flows through the layers
		/// so it's easy to correlate events on different layers of your system that belong to the same request.
		/// Also, you can use it as a service code to display to users when errors happen (users can tell it to support).
		/// </summary>
		[DataMember]
		public String LoggingID { get; set; }

		/// <summary>
		/// User session identifier. For example, if an event is raised in a web application this can be ASP.NET session ID.
		/// </summary>
		[DataMember]
		public String SessionID { get; set; }

		/// <summary>
		/// Managed thread ID. Useful to diagnose concurrency issues.
		/// </summary>
		[DataMember]
		public String ThreadID { get; set; }

		/// <summary>
		/// Human readable event message.
		/// </summary>
		[DataMember]
		public String Message { get; set; }

		/// <summary>
		/// IP address of a machine where request described by this event is originated from.
		/// For example, if an event is raised in a web application this can be IP address of a client browser.
		/// If, then, the application makes a call to the back-end and writes another event, the address will be
		/// IP address of the machine running the web application.
		/// </summary>
		[DataMember]
		public String RequestAddress { get; set; }

		/// <summary>
		/// Network name of the machine where an event raised. Very useful in cluster environment.
		/// </summary>
		[DataMember]
		public String MachineAddress { get; set; }

		/// <summary>
		/// User identity (usually from thread principal).
		/// </summary>
		[DataMember]
		public String UserIdentity { get; set; }

		/// <summary>
		/// Any additional data you want to store.
		/// </summary>
		[DataMember]
		public IDictionary<String, String> CustomData { get; protected set; }

		#region IComparable<LogEntry> Members

		/// <summary>
		/// Compares two instances of <see cref="LogEntry"/> based on <see cref="CreatedOn"/> property.
		/// </summary>
		/// <param name="other">The object to compare to the current instance.</param>
		/// <returns>Compares the value of this instance to a specified <see cref="LogEntry"/> value and indicates whether this instance is earlier than, the same as, or later than the specified <see cref="LogEntry"/> value.</returns>
		public Int32 CompareTo(LogEntry other)
		{
			if (other == null)
			{
				throw new ArgumentNullException();
			}
			Int32 result = CreatedOn.CompareTo(other.CreatedOn);
			return result;
		}

		#endregion

		/// <summary>
		/// String representation of an instance.
		/// </summary>
		/// <returns>Returns <see cref="Message"/> property if not null, otherwise base implementation.</returns>
		public override String ToString()
		{
			return Message ?? base.ToString();
		}
	}
}
