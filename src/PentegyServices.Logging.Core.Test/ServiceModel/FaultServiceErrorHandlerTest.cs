using NUnit.Framework;
using PentegyServices.Logging.Core.ServiceModel;
using System;
using System.Collections.Generic;
using System.ServiceModel;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	[TestFixture]
	public class FaultServiceErrorHandlerTest
		: ServiceTestCaseBase<IFaultingService, FaultingService>
	{
		protected override void ApplyServiceBehavior(System.ServiceModel.ServiceHost serviceHost)
		{
			serviceHost.Description.Behaviors.Add(new FaultServiceErrorHandler(false)); // apply FaultServiceErrorHandler as service behavior with preserveServerStack disabled
		}

		[Test]
		public void ThrowKeyNotFoundException_No_FaultContract()
		{
			const String StackTraceStart = "\r\nServer stack trace: \r\n   at Core.Test.ServiceModel.FaultingService.ThrowKeyNotFoundException_No_FaultContract()";

			var ex = Assert.Throws<FaultException>(() => service.ThrowKeyNotFoundException_No_FaultContract());

			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			StringAssert.DoesNotStartWith(StackTraceStart, ex.StackTrace, "Original server stack trace must NOT be preserved");
		}

		[Test]
		public void ThrowKeyNotFoundException_FaultContract()
		{
			const String StackTraceStart = "\r\nServer stack trace: \r\n   at Core.Test.ServiceModel.FaultingService.ThrowKeyNotFoundException()";

			var ex = Assert.Throws<FaultException<KeyNotFoundException>>(() => service.ThrowKeyNotFoundException());

			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			StringAssert.DoesNotStartWith(StackTraceStart, ex.StackTrace, "Original server stack trace must NOT be preserved");
		}

		[Test]
		public void ThrowCustomException_With_Inner_ArgumentNullException_FaultContract()
		{
			const String StackTraceStart = "\r\nServer stack trace: \r\n   at Core.Test.ServiceModel.FaultingService.ThrowCustomException_With_Inner_ArgumentNullException()";

			var ex = Assert.Throws<FaultException<CustomException>>(() => service.ThrowCustomException_With_Inner_ArgumentNullException());
			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			Assert.AreEqual(FaultingService.FaultMessage, ex.Detail.Message);
			StringAssert.DoesNotStartWith(StackTraceStart, ex.StackTrace, "Original server stack trace must NOT be preserved");

			// Although the service successfully serialized the inner exception the client side ignores it in the reply :(
			Assert.IsNull(ex.InnerException);
		}

		[Test]
		public void ThrowCustomFault()
		{
			const String StackTraceStart = "\r\nServer stack trace: \r\n   at Core.Test.ServiceModel.FaultingService.ThrowCustomFault()";

			var ex = Assert.Throws<FaultException<CustomFault>>(() => service.ThrowCustomFault());

			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			StringAssert.DoesNotStartWith(StackTraceStart, ex.StackTrace, "Original server stack trace must NOT be preserved as FaultException's are not processed by FaultServiceErrorHandler");
		}
	}
}
