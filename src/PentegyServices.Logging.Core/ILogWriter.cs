using System;
using System.ServiceModel;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Log writer service interface used by <see cref="ServiceAppender"/>.
	/// Your back-end should expose the service implementing <see cref="ILogWriter"/> to all the components that use <see cref="ServiceAppender"/>.
	/// <seealso cref="ILogReader"/>.
	/// </summary>
	[ServiceContract(Namespace = Namespace.ServiceContract)]
	public interface ILogWriter
	{
		/// <summary>
		/// Writes batch of <see cref="LogEntry"/> to the underlaying storage.
		/// </summary>
		/// <param name="batch">An array of <see cref="LogEntry"/> instances.</param>
		/// <returns><c>true</c> if the batch has been persisted successfully, otherwise <c>false</c>.</returns>
		[OperationContract]
		Boolean Write(LogEntry[] batch);
	}
}
