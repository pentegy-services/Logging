using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.ServiceModel
{
	/// <summary>Custom exception to test inner exceptions scenario</summary>
	[Serializable]
	[KnownType(typeof(ArgumentNullException))] // otherwise we'll get SerializationException
	public class CustomException
		: Exception
	{
		public CustomException(String message, Exception innerException)
			: base(message, innerException)
		{ }

		protected CustomException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
	}

	[DataContract]
	public class CustomFault { }

	/// <summary>
	/// Sample service interface required for the test.
	/// </summary>
	[ServiceContract]
	public interface IFaultingService
	{
		[OperationContract]
		void ThrowKeyNotFoundException_No_FaultContract();

		[OperationContract]
		[FaultContract(typeof(KeyNotFoundException))]
		void ThrowKeyNotFoundException();

		[OperationContract]
		[FaultContract(typeof(CustomException))]
		void ThrowCustomException_With_Inner_ArgumentNullException();

		[OperationContract]
		[FaultContract(typeof(CustomFault))]
		void ThrowCustomFault();
	}
	
	/// <summary>
	/// <see cref="IFaultingService"/> implementation.
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
	public class FaultingService
		: IFaultingService
	{
		public const String FaultMessage = "Shit happens.";

		#region IFaultingService Members

		public void ThrowKeyNotFoundException_No_FaultContract()
		{
			throw new KeyNotFoundException(FaultMessage);
		}

		public void ThrowKeyNotFoundException()
		{
			throw new KeyNotFoundException(FaultMessage);
		}
		
		public void ThrowCustomException_With_Inner_ArgumentNullException()
		{
			var inner = new ArgumentNullException("param");
			throw new CustomException(FaultMessage, inner);
		}

		public void ThrowCustomFault()
		{
			var detail = new CustomFault();
			throw new FaultException<CustomFault>(detail, FaultMessage); // will not be affected neither by FaultServiceErrorHandler nor FaultClientMessageInspector
		}

		#endregion
	}
}
