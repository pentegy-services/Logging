using log4net;
using NUnit.Framework;
using PentegyServices.Logging.Core.ServiceModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	/// <summary>
	/// The test ensures that <see cref="ThreadContextClientMessageInspector"/>
	/// and <see cref="ThreadContextServiceMessageInspector"/> work as expected.
	/// We define a sample <see cref="ISampleService"/> service configured with custom behavior (<see cref="ThreadContextServiceMessageInspectorElement"/>),
	/// then use a client proxy also configured with custom behavior (<see cref="ThreadContextClientMessageInspectorElement"/>.
	/// </summary>
	[TestFixture]
	public class ThreadContextInspectorTest
		: ServiceTestCaseBase<ISampleService, SampleService>
	{
		String[] clientProps = new String[] {"prop1", "prop2", "prop3"};
		String[] serviceProps = new String[] { "prop1", "prop2" };

		protected override void ApplyClientBehavior(ChannelFactory<ISampleService> factory)
		{
			factory.Endpoint.Behaviors.Add(new ThreadContextClientMessageInspector(clientProps));
		}

		protected override void ApplyServiceBehavior(System.ServiceModel.ServiceHost serviceHost)
		{
			serviceHost.Description.Behaviors.Add(new ThreadContextServiceMessageInspector(serviceProps));
		}

		[Test]
		public void PassProperties()
		{
			// put test values into log4net.ThreadContext
			foreach (var prop in TestProps)
			{
				ThreadContext.Properties[prop.Key] = prop.Value;
				Trace.TraceInformation("Added property '{0}' to ThreadContext: {1}", prop.Key, prop.Value);
			}

			// now call the service.
			// ThreadContextClientMessageInspector applied in the config file should pass 'prop1', 'prop2' and 'prop3' only
			service.TestThreadContext(TestProps, serviceProps);
		}

		protected static Dictionary<String, Object> TestProps = new Dictionary<String, Object>()
		{
			{"prop1", Rnd.Next()},
			{"prop2", true },
			{"prop3", "bla-bla"},
			{"prop4", Rnd.Next()}
		};
	}
}
