using log4net;
using Ninject.Modules;
using PentegyServices.Logging.Core;

namespace PentegyServices.Logging.Wcf
{
	/// <summary>
	///		Ninject module that rebinds <see cref="ILogReader"/> to <see cref="WcfLogReader"/>.
	///		This allows you to just put this project output to any host process that requires <see cref="ILogReader"/> 
	///		implementation and is able to auto load Ninject dependencies.
	/// </summary>
	public class WcfLogReaderModule
		: NinjectModule
    {
        static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        ///		Rebinds <see cref="ILogReader"/> to <see cref="WcfLogReader"/>
        ///		and <see cref="ILoggingConfiguration"/> to <see cref="WcfLoggingConfiguration"/>.
        /// </summary>
        public override void Load()
        {
            logger.DebugFormat("Rebinding dependency {0} to {1}", typeof(ILogReader).FullName, typeof(WcfLogReader).FullName);

            Unbind<ILogReader>();
            Bind<ILogReader>().To<WcfLogReader>();
            Unbind<ILoggingConfiguration>();
            Bind<ILoggingConfiguration>().To<WcfLoggingConfiguration>();
        }
    }
}
