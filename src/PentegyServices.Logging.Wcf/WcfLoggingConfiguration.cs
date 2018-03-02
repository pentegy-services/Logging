using PentegyServices.Logging.Core;
using System;
using System.Configuration;

namespace PentegyServices.Logging.Wcf
{
	/// <summary>
	///		For simplicity we read it from the appSettings section.
	///		In the production it can be a special service.
	/// </summary>
	public class WcfLoggingConfiguration
		: ILoggingConfiguration
    {
        #region ILoggingConfiguration Members

        /// <summary>
		///		A maximum number of rows to return when querying the event log via <see cref="ILogReader"/>.
		///	</summary>
        public Int32 MaxRowsToQueryEventLog
        {
            get
			{
				return Convert.ToInt32(ConfigurationManager.AppSettings["Logging.MaxRowsToQueryEventLog"]);
			}
        }

        /// <summary>
		///		Default number of rows for displaying purposes. Must be less or equal to <see cref="MaxRowsToQueryEventLog"/>.
		/// </summary>
        public Int32 DefaultPageSize
        {
            get
			{
				return Convert.ToInt32(ConfigurationManager.AppSettings["Logging.DefaultPageSize"]);
			}
        }

        #endregion
    }
}
