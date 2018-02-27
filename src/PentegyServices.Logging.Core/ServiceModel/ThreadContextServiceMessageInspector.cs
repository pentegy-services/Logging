using log4net;
using PentegyServices.Logging.Core.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Adds specified properties to log4net <see cref="ThreadContext"/> from <see cref="System.ServiceModel.Channels.MessageHeaders"/> collection
	/// of all incoming messages for endpoints where this message inspector is applied.
	/// To specify property names use either <see cref="ThreadContextServiceMessageInspector(String[])"/> ctor (in the code) or
	/// <see cref="ThreadContextServiceMessageInspectorElement"/> extension element (in the configuration file).
	/// <para>Here is a sample of an incoming message header:
	/// <code>
	/// &lt;s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope" xmlns:a="http://www.w3.org/2005/08/addressing"&gt;
	///		&lt;s:Header&gt;
	///			&lt;a:Action s:mustUnderstand="1"&gt;http://pentegyservices.com/logging/ISampleService&lt;/a:Action&gt;
	///			&lt;a:MessageID&gt;urn:uuid:0ac46ba4-7800-4448-b861-ebe2e3a393d7&lt;/a:MessageID&gt;
	///			&lt;a:ReplyTo&gt;
	///				&lt;a:Address&gt;http://www.w3.org/2005/08/addressing/anonymous&lt;/a:Address&gt;
	///			&lt;/a:ReplyTo&gt;
	///			&lt;loggingID xmlns="log4net"&gt;2e3c04ee-0c79-43e6-b8bc-ab4fe1f295fb&lt;/loggingID&gt;
	///			&lt;sessionID xmlns="log4net"&gt;6f5a56b0&lt;/sessionID&gt;
	///			&lt;a:To s:mustUnderstand="1"&gt;net.tcp://localhost:2000/SampleService&lt;/a:To&gt;
	///		&lt;/s:Header&gt;
	///		....
	///	&lt;s:Envelope&gt;
	/// </code>
	/// </para>
	/// <para>
	/// If the inspector is configured to process <c>sessionID</c> and <c>loggingID</c> properties the values from the header
	/// will be automatically put to log4net <see cref="ThreadContext"/>.
	/// </para>
	/// <para>
	/// Note, <see cref="ThreadContext"/> may contain objects of any type. However, even if they are serializable 
	/// we cannot use .NET serialization because we have to keep SOAP compatibility. That means <see cref="ThreadContextClientMessageInspector"/>
	/// always performs <see cref="Object.ToString()"/> before putting property values into headers and <see cref="ThreadContextServiceMessageInspector"/>
	/// always restores them as <see cref="String"/>.
	/// </para>
	/// <seealso cref="ThreadContextClientMessageInspector"/>
	/// </summary>
	public class ThreadContextServiceMessageInspector
		: ThreadContextMessageInspectorBase, IDispatchMessageInspector, IServiceBehavior
	{
		//TODO: Ideally we need a map between incoming message headers and ThreadContext props so that we can define, for example, that "LOGID" header goes to ThreadContext.Properties[LogProp.LoggingID]

		/// <summary>
		/// Creates an instance of <see cref="ThreadContextServiceMessageInspector"/> with the given list of properties to use.
		/// </summary>
		/// <param name="properties">An array of property names to extract from log4net <see cref="ThreadContext"/>.</param>
		public ThreadContextServiceMessageInspector(String[] properties)
		{
			if (properties == null)
			{
				throw new ArgumentNullException();
			}
			Properties = properties;
		}

		#region IDispatchMessageInspector members

		/// <summary>
		/// Extracts properties specified by <see cref="ThreadContextMessageInspectorBase.Properties"/> from inbound message headers and puts them into log4net <see cref="ThreadContext"/>.
		/// In addition, populates <see cref="LogProp.RequestAddress"/> property with a value of <see cref="RemoteEndpointMessageProperty.Address"/>
		/// to have IP address of a machine where the request coming from (useful in cluster environment).
		/// </summary>
		public Object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
		{
			OperationContext context = OperationContext.Current;
			MessageHeaders headers = context.IncomingMessageHeaders;
			Debug.Assert(headers == request.Headers);

			ThreadContext.Properties.Remove(LogProp.LoggingID); // clear possible garbage

			// extract properties from incoming headers into log4net context
			foreach (String propName in Properties)
			{
				if (headers.FindHeader(propName, HeaderNamespace) >= 0)
				{
					String propValue = headers.GetHeader<String>(propName, HeaderNamespace);
					ThreadContext.Properties[propName] = propValue;
				}
				else
				{
					ThreadContext.Properties.Remove(propName); // clear possible garbage
				}
			}

			String loggingID = ThreadContext.Properties[LogProp.LoggingID] as String;
			// if no LoggingID prop is passed we need a new ID to have something in logs
			// the best candidate would be standard MessageId
			if (String.IsNullOrEmpty(loggingID))
			{
				Guid id = Guid.Empty;
				if (headers != null && headers.MessageId != null)
				{
					headers.MessageId.TryGetGuid(out id);
				}

				if (id == Guid.Empty)
				{
					id = Guid.NewGuid(); // best we can do
				}

				ThreadContext.Properties[LogProp.LoggingID] = id.ToString();
			}

			Guid systemActivityId = Trace.CorrelationManager.ActivityId;
			Debug.WriteLine(String.Format("ThreadContextServiceMessageInspector: ActivityId: {0}", systemActivityId));
			Debug.WriteLine(String.Format("ThreadContextServiceMessageInspector: Thread.CurrentPrincipal: {0}", Thread.CurrentPrincipal.FormatString()));

			// Other request information
			MessageProperties messageProperties = context.IncomingMessageProperties;
			var endpointProperty = (RemoteEndpointMessageProperty)messageProperties[RemoteEndpointMessageProperty.Name];
			ThreadContext.Properties[LogProp.RequestAddress] = endpointProperty.Address;
			
			return null;
		}

		/// <summary>Does nothing.</summary>
		/// <param name="reply"></param>
		/// <param name="correlationState"></param>
		public void BeforeSendReply(ref Message reply, Object correlationState)
		{ }

		#endregion

		#region IServiceBehavior Members

		/// <summary>Does nothing.</summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		/// <param name="endpoints"></param>
		/// <param name="bindingParameters"></param>
		public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, 
			Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{ }

		/// <summary>
		/// Adds the instance to <see cref="System.ServiceModel.Dispatcher.DispatchRuntime.MessageInspectors"/> collection for all
		/// endpoints in <paramref name="serviceHostBase"/>.
		/// </summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			foreach (ChannelDispatcher chDisp in serviceHostBase.ChannelDispatchers)
			{
				foreach (EndpointDispatcher epDisp in chDisp.Endpoints)
				{
					epDisp.DispatchRuntime.MessageInspectors.Add(this);
				}
			}
		}

		/// <summary>Does nothing.</summary>
		/// <param name="serviceDescription"></param>
		/// <param name="serviceHostBase"></param>
		public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{ }

		#endregion
	}
}
