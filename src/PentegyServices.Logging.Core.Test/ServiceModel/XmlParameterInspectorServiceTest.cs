using NUnit.Framework;
using PentegyServices.Logging.Core.ServiceModel;
using System;
using System.Globalization;
using System.ServiceModel;
using System.Xml.Linq;

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	[TestFixture]
    public class XmlParameterInspectorServiceTest
		: XmlParameterInspectorTestBase
    {
        const String InspectorName = "testservice";

        protected override void ApplyServiceBehavior(System.ServiceModel.ServiceHost serviceHost)
        {
            var settings = new XmlParamInspectorSettings
            {
                Name = InspectorName
            };

            serviceHost.Description.Behaviors.Add(new XmlParameterInspector(settings)); // apply XmlParameterInspector as service behavior
        }

        [Test]
        public void ServiceBehavior()
        {
            String expectedLoggedTypeName = typeof(SampleService).FullName;

            String param1 = Rnd.Next().ToString(CultureInfo.InvariantCulture);
            String param2 = "Hello, world!";
            String result = service.TestXmlParameters(param1, param2);

            // so now we must have 2 log entries written by the inspector
            log4net.Core.LoggingEvent[] events = appender.GetEvents();
            Assert.AreEqual(2, events.Length);

            // the first one is "before"
            XElement xml1 = XElement.Parse(events[0].MessageObject.ToString());
            TestBeforeElement(xml1, InspectorName, XmlParameterInspector.EventLocation.Service, expectedLoggedTypeName, "TestXmlParameters", new Object[] { param1, param2 });

            // the second one is "after"
            XElement xml2 = XElement.Parse(events[1].MessageObject.ToString());
            TestAfterElement(xml2, InspectorName, XmlParameterInspector.EventLocation.Service, expectedLoggedTypeName, "TestXmlParameters", result);

            // ensure the performance if logged
            XAttribute ms = xml2.Attribute("ms");
            Assert.IsNotNull(ms);
            Assert.GreaterOrEqual(Int32.Parse(ms.Value), 0);
        }

        [Test]
        public void ServiceBehavior_Exception()
        {
            String expectedLoggedTypeName = typeof(SampleService).FullName;

            String param1 = Rnd.Next().ToString(CultureInfo.InvariantCulture);
            var ex = Assert.Throws<FaultException<InvalidOperationException>>(() => service.ThrowException(param1, "Shit happens."));

            // so now we must have 2 log entries written by the inspector
            log4net.Core.LoggingEvent[] events = appender.GetEvents();
            Assert.AreEqual(2, events.Length);

            // the first one is "before"
            XElement xml1 = XElement.Parse(events[0].MessageObject.ToString());
            TestBeforeElement(xml1, InspectorName, XmlParameterInspector.EventLocation.Service, expectedLoggedTypeName, "ThrowException", new Object[] { param1, "Shit happens." });

            // the second one is "after"
            XElement xml2 = XElement.Parse(events[1].MessageObject.ToString());
            TestAfterElement(xml2, InspectorName, XmlParameterInspector.EventLocation.Service, expectedLoggedTypeName, "ThrowException", ex.ToString());

            // ensure the performance if logged
            XAttribute ms = xml2.Attribute("ms");
            Assert.IsNotNull(ms);
            Assert.GreaterOrEqual(Int32.Parse(ms.Value), 0);
        }
    }
}
