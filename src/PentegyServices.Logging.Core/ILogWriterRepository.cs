namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Repository interface that abstracts storage-related code (DAL) for <see cref="ILogWriter"/>.
	/// </summary>
	public interface ILogWriterRepository
	{
		/// <summary>
		/// Persists the given batch into the underlaying storage.
		/// </summary>
		/// <param name="batch">An array of <see cref="LogEntry"/> to persist.</param>
		void WriteBatch(LogEntry[] batch);
	}
}
