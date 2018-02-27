using log4net.Appender;
using NUnit.Framework;
using PentegyServices.Logging.Core.ServiceModel;
using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	/// <summary>
	/// Common functionality to test <see cref="XmlParameterInspector"/> functionality on client and service sides separately.
	/// </summary>
	public abstract class XmlParameterInspectorTestBase
		: ServiceTestCaseBase<ISampleService, SampleService>
	{
		protected MemoryAppender appender;

		public override void Init()
		{
			base.Init();

			appender = new MemoryAppender
			{
				Layout = new log4net.Layout.SimpleLayout()
			};
			appender.ActivateOptions();

			// reconfigure logging engine to use only MemoryAppender
			log4net.LogManager.GetRepository().ResetConfiguration();
			log4net.Config.BasicConfigurator.Configure(appender);

			CollectionAssert.IsEmpty(appender.GetEvents()); // ensure it's empty
		}

		public override void Down()
		{
			// restore logging configuration from the config file
			log4net.Config.XmlConfigurator.Configure();

			base.Down();
		}

		protected void TestBeforeElement(XElement xml, String inspectorName, String where, String typeName, String methodName, Object[] inputs)
		{
			TestElement(xml, "b", inspectorName, where, typeName, methodName);
			Assert.AreEqual(inputs.ToXml().ToString(), xml.Element("inputs").Nodes().Single().ToString());
		}

		protected void TestAfterElement(XElement xml, String inspectorName, String where, String typeName, String methodName, Object returnValue)
		{
			TestElement(xml, "a", inspectorName, where, typeName, methodName);
			var outputs = new Object[0]; // we don't have output parameters
			Assert.AreEqual(outputs.ToXml().ToString(), xml.Element("outputs").Nodes().Single().ToString());
			Assert.AreEqual(returnValue.ToXml().ToString(), xml.Element("returnValue").Nodes().Single().ToString());
		}

		protected void TestElement(XElement xml, String when, String inspectorName, String where, String typeName, String methodName)
		{
			Trace.TraceInformation("{0}", xml);
			Assert.AreEqual(inspectorName, xml.Attribute("inspector").Value);
			Assert.AreEqual(when, xml.Attribute("when").Value);
			Assert.AreEqual(where, xml.Attribute("where").Value);
			Assert.AreEqual(typeName, xml.Attribute("type").Value);
			Assert.AreEqual(methodName, xml.Attribute("method").Value);
		}
	}
}
