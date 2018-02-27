using System;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Basic properties used in logging contexts (see <see cref="log4net.ThreadContext"/>, <see cref="log4net.GlobalContext"/>).
	/// Use these constants to put/retrieve the appropriate values into/from log4net contexts (to not use strings).
	/// <seealso cref="LogEntry"/>.
	/// </summary>
	public sealed class LogProp
	{
		/// <summary>Application (or sub-system) identifier. This is global-context property.</summary>
		public const String Application = "application";

		/// <summary>Incoming request address. This is thread-context property.</summary>
		public const String RequestAddress = "requestAddr";

		/// <summary>Current (processing) machine address (cluster node name). This is global-context property.</summary>
		public const String MachineAddress = "machineAddr";

		/// <summary>Identity of a user the current request is processed under. This is thread-context property.</summary>
		public const String UserIdentity = "user";

		/// <summary>Unique ID to flow throw system layers unmodified (for tracking purposes). This is thread-context property.</summary>
		public const String LoggingID = "loggingID";

		/// <summary>User agent string from the originating http request. The value will be null if the request is non-browser (for example, from intranet service). This is thread-context property.</summary>
		public const String UserAgent = "userAgent";

		/// <summary>Incoming user address from the originating http request. The value will be null if the request is non-browser (for example, from intranet service; unlike <see cref="RequestAddress"/>). This is thread-context property.</summary>
		public const String UserAddress = "userAddr";

		/// <summary>UI (for example, ASP.NET) user session ID. This is thread-context property.</summary>
		public const String SessionID = "sessionID";
	}
}
