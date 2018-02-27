using NUnit.Framework;
using PentegyServices.Logging.Core.ServiceModel;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text.RegularExpressions;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	[TestFixture]
	public class FaultClientMessageInspectorTest
		: ServiceTestCaseBase<IFaultingService, FaultingService>
	{
		protected override void ApplyServiceBehavior(System.ServiceModel.ServiceHost serviceHost)
		{
			serviceHost.Description.Behaviors.Add(new FaultServiceErrorHandler(true)); // apply FaultServiceErrorHandler as service behavior with preserveServerStack enabled
		}

		protected override void ApplyClientBehavior(ChannelFactory<IFaultingService> factory)
		{
			factory.Endpoint.Behaviors.Add(new FaultClientMessageInspector(true)); // apply FaultClientMessageInspector as client behavior with preserveServerStack enabled
		}

		[Test]
		public void ThrowKeyNotFoundException_No_FaultContract()
		{
			const String StackTraceStartPatern = @"^\r\nServer stack trace: \r\n   ([A-zА-я]+) PentegyServices.Logging.Core.Test.ServiceModel.FaultingService.ThrowKeyNotFoundException_No_FaultContract";

			// Note, that we don't even need FaultContract defined!
			var ex = Assert.Throws<KeyNotFoundException>(() => service.ThrowKeyNotFoundException_No_FaultContract());

			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			Assert.IsTrue(
				Regex.IsMatch(ex.StackTrace, StackTraceStartPatern, RegexOptions.Compiled | RegexOptions.IgnoreCase),
				"Original server stack trace must be preserved");
		}

		[Test]
		public void ThrowKeyNotFoundException_FaultContract()
		{
			const String StackTraceStartPatern = @"^\r\nServer stack trace: \r\n   ([A-zА-я]+) PentegyServices.Logging.Core.Test.ServiceModel.FaultingService.ThrowKeyNotFoundException";

			var ex = Assert.Throws<KeyNotFoundException>(() => service.ThrowKeyNotFoundException());

			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			Assert.IsTrue(
				Regex.IsMatch(ex.StackTrace, StackTraceStartPatern, RegexOptions.Compiled | RegexOptions.IgnoreCase),
				"Original server stack trace must be preserved");
		}

		[Test]
		public void ThrowCustomException_With_Inner_ArgumentNullException_FaultContract()
		{
			const String StackTraceStartPatern = @"^\r\nServer stack trace: \r\n   ([A-zА-я]+) PentegyServices.Logging.Core.Test.ServiceModel.FaultingService.ThrowCustomException_With_Inner_ArgumentNullException";

			var ex = Assert.Throws<CustomException>(() => service.ThrowCustomException_With_Inner_ArgumentNullException());

			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			Assert.IsTrue(
				Regex.IsMatch(ex.StackTrace, StackTraceStartPatern, RegexOptions.Compiled | RegexOptions.IgnoreCase),
				"Original server stack trace must be preserved");

			// In contrast to the same test in FaultServiceErrorHandlerTest the inner exception is restored by FaultServiceErrorHandler!
			Assert.IsNotNull(ex.InnerException);
			Assert.IsInstanceOf<ArgumentNullException>(ex.InnerException);
			Assert.AreEqual("param", ((ArgumentNullException)ex.InnerException).ParamName);
		}

		[Test]
		public void ThrowCustomFault()
		{
			const String StackTraceStartPatern = @"^\r\nServer stack trace: \r\n   ([A-zА-я]+) PentegyServices.Logging.Core.Test.ServiceModel.FaultingService.ThrowCustomFault";

			var ex = Assert.Throws<FaultException<CustomFault>>(() => service.ThrowCustomFault());

			Assert.AreEqual(FaultingService.FaultMessage, ex.Message);
			Assert.IsFalse(
				Regex.IsMatch(ex.StackTrace, StackTraceStartPatern, RegexOptions.Compiled | RegexOptions.IgnoreCase),
				"Original server stack trace must NOT be preserved as FaultException's are not processed by FaultServiceErrorHandler");
		}
	}
}
