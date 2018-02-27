using log4net;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Reflection;

namespace PentegyServices.Logging.Core.Test.CustomImpl
{
	/// <summary>
	/// This test shows how to create your own storage repository, extend <see cref="ServiceAppender"/>
	/// to add additional context processing and register it in run-time.
	/// </summary>
	[TestFixture]
    public class CustomLoggingServiceTest
		: ServiceTestCaseBase<ILogService, CustomLogService>
    {
        public class CustomConfiguration
			: ILoggingConfiguration
        {
            public Int32 MaxRowsToQueryEventLog
			{
				get
				{
					return 100;
				}
			}

            public Int32 DefaultPageSize
			{
				get
				{
					return 50;
				}
			}
        }

        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        CustomServiceAppender _appender;
        readonly ILoggingConfiguration _configuration = new CustomConfiguration();

        protected override CustomLogService CreateService()
        {
            var repository = new CustomLogRepository(_configuration);
            var logservice = new CustomLogService(_configuration, repository);
            return logservice;
        }

        public override void Init()
        {
            base.Init();

			_appender = new CustomServiceAppender(factory.Endpoint)
			{
				Layout = new log4net.Layout.SimpleLayout(),
				File = "CustomServiceAppender-fallback.log"
			};
			_appender.ActivateOptions();

            // reconfigure logging engine to use only CustomServiceAppender
            LogManager.GetRepository().ResetConfiguration();
            log4net.Config.BasicConfigurator.Configure(_appender);
        }

        public override void Down()
        {
            _appender.WaitForFinish();

            // restore logging configuration from the config file
            log4net.Config.XmlConfigurator.Configure();

            base.Down();
        }

        [Test]
        public void Generate()
        {
            const String messagePrefix = "Random test entry ";
            const Int32 maxEntries = 250;

            // initialize logging context
            String loggingID = Guid.NewGuid().ToString();
            String customData = Rnd.Next().ToString(CultureInfo.InvariantCulture);
            ThreadContext.Properties[LogProp.LoggingID] = loggingID;
            ThreadContext.Properties[CustomServiceAppender.CustomDataProp] = customData;

            // ServiceAppender should always write associated exception chain regardless the layout, lets check this too
            var ex = new InvalidOperationException("outer-exception", new ArgumentException("inner-exception"));

            for (Int32 i = 0; i < maxEntries; i++)
            {
                String message = messagePrefix + Rnd.Next();
                Logger.Info(message, ex);
            }

            //SysUtil.FlushLogBuffers();
            _appender.WaitForFinish();

            LogEntry[] log = service.GetEventLog(null);
            Assert.IsNotNull(log);
            Assert.AreEqual(_configuration.MaxRowsToQueryEventLog, log.Length, "GetEventLog(null)");

            foreach (LogEntry entry in log)
            {
                Assert.AreEqual(loggingID, entry.LoggingID);
                Assert.AreEqual(Logger.Logger.Name, entry.Logger);
                StringAssert.StartsWith(messagePrefix, entry.Message);

                // check if all the exception chain has been persisted
                StringAssert.Contains("InvalidOperationException", entry.Message);
                StringAssert.Contains("ArgumentException", entry.Message);
                StringAssert.Contains("outer-exception", entry.Message);
                StringAssert.Contains("inner-exception", entry.Message);

                // check if custom context data have been persisted by the logger
                Assert.IsNotNull(entry.CustomData);
                Assert.AreEqual(customData, entry.CustomData[CustomServiceAppender.CustomDataProp]);
            }
        }
    }
}
