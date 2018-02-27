using log4net;
using System;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Intercepts all exceptions happened in the service and converts all non-fault exceptions to <see cref="FaultException{T}"/>.
	/// See <a href="http://weblogs.asp.net/pglavich/archive/2008/10/16/wcf-ierrorhandler-and-propagating-faults.aspx">this article</a> for more details.
	/// Additionally, writes all exceptions to log4net.
	/// <para>
	/// Here is a sample of how the fault reply looks like after applying this handler:
	/// </para>
	/// <code>
	///	&lt;s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"&gt;
	///	   &lt;s:Body&gt;
	///		  &lt;s:Fault&gt;
	///			 &lt;faultcode&gt;s:Client&lt;/faultcode&gt;
	///			 &lt;faultstring xml:lang="en-US"&gt;Parameter key: fff&lt;/faultstring&gt;
	///			 &lt;detail&gt;
	///				&lt;KeyNotFoundException xmlns="http://schemas.datacontract.org/2004/07/System.Collections.Generic" xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns:x="http://www.w3.org/2001/XMLSchema"&gt;
	///				   &lt;ClassName i:type="x:string" xmlns=""&gt;System.Collections.Generic.KeyNotFoundException&lt;/ClassName&gt;
	///				   &lt;Message i:type="x:string" xmlns=""&gt;Parameter key: fff&lt;/Message&gt;
	///					&lt;Data i:nil="true" xmlns=""/&gt;
	///				   &lt;InnerException i:nil="true" xmlns=""/&gt;
	///				   &lt;HelpURL i:nil="true" xmlns=""/&gt;
	///				   &lt;StackTraceString i:type="x:string" xmlns=""&gt;at Core.Configuration.Impl.SqlServer.SqlConfigurationManager.GetSystemParameter(String key) in C:\Projects\Core\src\Core.Configuration.Impl.SqlServer\SqlConfigurationManager.cs:line 68
	///	   at SyncInvokeGetSystemParameter(Object , Object[] , Object[] )
	///	   at System.ServiceModel.Dispatcher.SyncMethodInvoker.Invoke(Object instance, Object[] inputs, Object[]&amp; outputs)
	///	   at System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin(MessageRpc&amp; rpc)
	///	   at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage5(MessageRpc&amp; rpc)
	///	   at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage31(MessageRpc&amp; rpc)
	///	   at System.ServiceModel.Dispatcher.MessageRpc.Process(Boolean isOperationContextSet)&lt;/StackTraceString&gt;
	///				   &lt;RemoteStackTraceString i:nil="true" xmlns=""/&gt;
	///				   &lt;RemoteStackIndex i:type="x:int" xmlns=""&gt;0&lt;/RemoteStackIndex&gt;
	///				   &lt;ExceptionMethod i:type="x:string" xmlns=""&gt;8
	///	GetSystemParameter
	///	Core.Configuration.Impl.SqlServer, Version=0.0.0.0, Culture=neutral, PublicKeyToken=1d3c536c71c466aa
	///	Core.Configuration.Impl.SqlServer.SqlConfigurationManager
	///	Core.Configuration.SysParamInfo GetSystemParameter(System.String)&lt;/ExceptionMethod&gt;
	///				   &lt;HResult i:type="x:int" xmlns=""&gt;-2146232969&lt;/HResult&gt;
	///				   &lt;Source i:type="x:string" xmlns=""&gt;Core.Configuration.Impl.SqlServer&lt;/Source&gt;
	///				   &lt;WatsonBuckets i:nil="true" xmlns=""/&gt;
	///				&lt;/KeyNotFoundException&gt;
	///			 &lt;/detail&gt;
	///		  &lt;/s:Fault&gt;
	///	   &lt;/s:Body&gt;
	///	&lt;/s:Envelope&gt;
	/// </code>
	/// 
	/// <seealso cref="FaultClientMessageInspector"/>
	/// </summary>
	public class FaultServiceErrorHandler
		: IErrorHandler, IServiceBehavior
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>Indicates whether to put original stack trace into the reply.</summary>
		protected readonly Boolean PreserveServerStack;

		/// <summary>
		/// Creates the instance.
		/// </summary>
		/// <param name="preserveServerStack"><c>true</c> to put original stack trace into the reply, <c>false</c> - to clear it.</param>
		public FaultServiceErrorHandler(Boolean preserveServerStack)
		{
			PreserveServerStack = preserveServerStack;
		}

		#region IErrorHandler

		/// <summary>
		/// Writes the exception to log4net.
		/// </summary>
		/// <param name="error">The exception thrown during processing.</param>
		/// <returns><c>true</c> if should not abort the session (if there is one) and instance context if the instance context is not <see cref="InstanceContextMode.Single"/>;
		/// otherwise, <c>false</c>. The default is <c>false</c>.</returns>
		public Boolean HandleError(Exception error)
		{
			logger.Debug(error);

			return true; // the error is handled
		}

		/// <summary>
		/// Enables the creation of a custom <see cref="FaultException{TDetail}"/> that is returned from an exception in the course of a service method. 
		/// </summary>
		/// <param name="error">The Exception object thrown in the course of the service operation. </param>
		/// <param name="version">The SOAP version of the message.</param>
		/// <param name="fault">The <see cref="System.ServiceModel.Channels.Message"/> object that is returned to the client, or service, in the duplex case. </param>
		public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
		{
			if (error is FaultException)
			{
				logger.DebugFormat("Skipping {0}", error.GetType().FullName);
				return; // already good
			}

			logger.DebugFormat("Providing fault for {0}", error.GetType().FullName);
			//PreserveStackTrace(error);

			// This hack sets the _data field to null, see http://www.woutware.com/blog/post/Implementing-IErrorHandler-and-working-around-SerializationException.aspx
			try
			{
				FieldInfo fieldInfo = typeof(Exception).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
				Exception inner = error;
				while (inner != null) // do it for all the chain of inner exceptions
				{
					fieldInfo.SetValue(inner, null); // otherwise we'll get SerializationException
					inner = inner.InnerException;
				}

				// now wrap the exception into FaultException<T>
				Type wrappedFaultType = typeof(FaultException<>).MakeGenericType(error.GetType());
				var wrappedFault = (FaultException)Activator.CreateInstance(wrappedFaultType, new Object[] { error, error.Message });
				// generate reply
				fault = Message.CreateMessage(version, wrappedFault.CreateMessageFault(), wrappedFault.Action);
			}
			catch (Exception ex)
			{
				logger.Fatal(ex);
				throw;
			}
		}

		#endregion IErrorHandler

		#region IServiceBehavior

		/// <summary>Does nothing.</summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		/// <param name="endpoints"></param>
		/// <param name="bindingParameters"></param>
		public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
		{ }

		/// <summary>
		/// Adds the instance to <see cref="System.ServiceModel.Dispatcher.DispatchRuntime.MessageInspectors"/> collection for all
		/// endpoints in <paramref name="serviceHostBase"/>.
		/// </summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			foreach (ChannelDispatcher channDisp in serviceHostBase.ChannelDispatchers)
			{
				channDisp.ErrorHandlers.Add(this);
			}
		}

		/// <summary>Does nothing.</summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{ }

		#endregion IServiceBehavior
	}
}
