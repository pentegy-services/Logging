using log4net;
using System;
using System.IO;
using System.Text;

namespace PentegyServices.Logging.Core.Diagnostics
{
	/// <summary>
	/// Helper class to redirect all <see cref="TextWriter"/> input to log4net.
	/// The entries are logged with "DEBUG" level.
	/// The class can be used for logging all Linq 2 Sql operations, for example:
	/// <code>
	/// </code>
	/// </summary>
	public class Log4netTextWriter
		: TextWriter
	{
		/// <summary>Underlaying log4net logger to redirect to.</summary>
		public readonly ILog Logger;

		/// <summary><see cref="Encoding"/> in which the output is written. The default value is <see cref="System.Text.Encoding.Default"/>.</summary>
		public override Encoding Encoding
		{
			get
			{
				return Encoding.Default;
			}
		}

		/// <summary>
		/// Constructs new instance from the given log4net logger.
		/// </summary>
		/// <param name="logger"></param>
		public Log4netTextWriter(ILog logger)
		{
			if (logger == null)
			{
				throw new ArgumentNullException();
			}

			Logger = logger;
		}

		/// <summary>
		/// Writes a string to the logger.
		/// </summary>
		/// <param name="value"></param>
		public override void Write(String value)
		{
			Logger.Debug(value);
			//System.Diagnostics.Debug.Write(value); // Also, write to standard debug output for easier debugging (Output window in VS)
			// Cannot write to System.Diagnostics here - that will lead to endless loop!
		}

		/// <summary>
		/// Converts the array into the string, then calls to <see cref="Write(string)"/>.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		public override void Write(Char[] buffer, Int32 index, Int32 count)
		{
			Write(new String(buffer, index, count));
		}
	}
}
