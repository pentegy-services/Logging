using PentegyServices.Logging.Core;
using System;
using System.ServiceModel;

namespace PentegyServices.Logging.Wcf
{
	/// <summary>
	///		Implementation of <see cref="ILogReader"/> that creates WCF client proxy (lazy initialization) and calls it.
	///		This is default implementation for the event viewer. You can use it as the reference to create your own implementation
	///		(for example, if you want the event viewer to display data directly from the database).
	///		The configuration file should contain the client endpoint named 'ILogReader':
	/// <code>
	///&lt;system.serviceModel&gt;
	///		&lt;client&gt;
	///			&lt;endpoint name="ILogReader" binding="netTcpBinding" contract="PentegyServices.Logging.Core.ILogReader" address="net.tcp://localhost:2000/Logging/Reader" /&gt;
	///		&lt;/client&gt;
	///&lt;/system.serviceModel&gt;
	/// </code>
	///		If you want to use this implementation with Ninject make sure you register <see cref="WcfLogReaderModule"/>.
	/// </summary>
	public class WcfLogReader
		: ILogReader
    {
        static readonly Object Sync = new Object();

        static ChannelFactory<ILogReader> _serviceFactory;

        static ChannelFactory<ILogReader> ServiceFactory
        {
            get
            {
				lock (Sync)
				{
					if (_serviceFactory == null)
					{
						_serviceFactory = new ChannelFactory<ILogReader>(typeof(ILogReader).Name);
					}
				}
                return _serviceFactory;
            }
        }

        #region ILogReader Members

        /// <summary>
        ///		Creates the new channel and calls the corresponding method.
        /// </summary>
        public LogEntry[] GetEventLog(LogFilter filter)
        {
            ILogReader service = ServiceFactory.CreateChannel();

            try
            {
                return service.GetEventLog(filter);
            }
            finally
            {
                ((ICommunicationObject)service).Dispose();
            }
        }

        /// <summary>
        ///		Creates the new channel and calls the corresponding method.
        /// </summary>
        public LogEntry GetEventLogEntry(Int64 id)
        {
            ILogReader service = ServiceFactory.CreateChannel();

            try
            {
                return service.GetEventLogEntry(id);
            }
            finally
            {
                ((ICommunicationObject)service).Dispose();
            }
        }

        #endregion
    }
}
