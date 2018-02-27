using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
	public class SampleService
		: ISampleService
	{
		#region ISampleService Members

		public void TestThreadContext(IDictionary<String, Object> testProps, String[] expectedPropNames)
		{
			// here ThreadContextServiceMessageInspector should restore 'prop1' and 'prop2' only

			foreach (var prop in testProps) // visualize
			{
				Object propValue = ThreadContext.Properties[prop.Key];
				if (propValue != null)
				{
					Trace.TraceInformation("Property '{0}' found in ThreadContext: {1}", prop.Key, propValue);
				}
				else
				{
					Trace.TraceInformation("Property '{0}' not found in ThreadContext", prop.Key);
				}
			}

			foreach (String propName in expectedPropNames)
			{
				Object propValue = ThreadContext.Properties[propName];
				Assert.IsNotNull(propValue);
				Assert.AreEqual(testProps[propName].ToString(), propValue);
			}
		}

		public String TestXmlParameters(String param1, String param2)
		{
			Trace.TraceInformation("Got param1: {0} param2: {1}", param1, param2);
			// here XmlParameterInspector should already have logged "before call" (input) parameters

			String result = param1 ?? "" + param2 ?? ""; // just concatenate
			return result;
		}

		public void ThrowException(Object input, String exceptionMessage)
		{
			var ex = new InvalidOperationException(exceptionMessage);
			throw new FaultException<InvalidOperationException>(ex);
		}

		#endregion
	}
}
