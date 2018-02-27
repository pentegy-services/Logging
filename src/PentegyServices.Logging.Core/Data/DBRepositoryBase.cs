using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.Data
{
	/// <summary>
	/// Contains functionality common to all repositories working with a database.
	/// </summary>
	public abstract class DBRepositoryBase
	{
		/// <summary>Database connection string key that must be present in the application configuration file.</summary>
		public const String DB_CONN_STRING = "core.db";

		private static String _connectionString;

		/// <summary>
		/// Extracts connection string from the configuration by <see cref="DB_CONN_STRING"/> key.
		/// </summary>
		public static String ConnectionString
		{
			get
			{
				if (!String.IsNullOrEmpty(_connectionString))
				{
					return _connectionString;
				}
				var setting = ConfigurationManager.ConnectionStrings[DB_CONN_STRING];
				if (setting == null || String.IsNullOrEmpty(setting.ConnectionString))
				{
					throw new InvalidOperationException("No connection string defined in the current configuration: " + DB_CONN_STRING);
				}
				_connectionString =  setting.ConnectionString;
				return _connectionString;
			}
		}
	}
}
