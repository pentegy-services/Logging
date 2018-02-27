using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	[ServiceContract]
	public interface ISampleService
	{
		[OperationContract]
		void TestThreadContext(IDictionary<String, Object> testProps, String[] expectedPropNames);

		[OperationContract]
		String TestXmlParameters(String param1, String param2);

		[OperationContract]
		[FaultContract(typeof(InvalidOperationException))]
		void ThrowException(Object input, String exceptionMessage);
	}
}
