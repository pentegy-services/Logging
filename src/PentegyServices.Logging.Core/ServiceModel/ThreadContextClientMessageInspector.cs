using log4net;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Adds specified properties 
	/// from log4net <see cref="ThreadContext"/> to <see cref="System.ServiceModel.Channels.MessageHeaders"/> collection
	/// of all outgoing messages for endpoints where this message inspector is applied.
	/// To specify property names use either <see cref="ThreadContextClientMessageInspector(string[])"/> ctor (in the code) or
	/// <see cref="ThreadContextClientMessageInspectorElement"/> extension element (in the configuration file).
	/// <para>Here is a sample of an outgoing message header after the inspector added <c>sessionID</c> and <c>loggingID</c> properties:
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
	/// Note, <see cref="ThreadContext"/> may contain objects of any type. However, even if they are serializable 
	/// we cannot use .NET serialization because we have to keep SOAP compatibility. That means <see cref="ThreadContextClientMessageInspector"/>
	/// always performs <see cref="Object.ToString()"/> before putting property values into headers and <see cref="ThreadContextServiceMessageInspector"/>
	/// always restores them as <see cref="String"/>.
	/// </para>
	/// 
	/// <seealso cref="ThreadContextServiceMessageInspector"/>
	/// </summary>
	public class ThreadContextClientMessageInspector
		: ThreadContextMessageInspectorBase, IClientMessageInspector, IEndpointBehavior
	{
		/// <summary>
		/// Creates an instance of <see cref="ThreadContextClientMessageInspector"/> with the given list of properties to use.
		/// </summary>
		/// <param name="properties">An array of property names to extract from log4net <see cref="ThreadContext"/>.</param>
		public ThreadContextClientMessageInspector(String[] properties)
		{
			if (properties == null)
			{
				throw new ArgumentNullException();
			}
			Properties = properties;
		}

		#region IClientMessageInspector Members

		/// <summary>Does nothing.</summary>
		/// <param name="reply"></param>
		/// <param name="correlationState"></param>
		public void AfterReceiveReply(ref Message reply, Object correlationState)
		{ }

		/// <summary>
		/// Creates <see cref="MessageHeader"/> for any property in <see cref="ThreadContextMessageInspectorBase.Properties"/>
		/// collection and adds them to <see cref="System.ServiceModel.Channels.Message.Headers"/> collection of <paramref name="request"/>.
		/// </summary>
		/// <param name="request">The message to be sent to the service.</param>
		/// <param name="channel">The client object channel.</param>
		/// <returns><see cref="Guid.NewGuid()"/> value.</returns>
		public Object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			foreach (String propName in Properties)
			{
				Object propValue = ThreadContext.Properties[propName];
				if (propValue != null)
				{
					MessageHeader customHeader = MessageHeader.CreateHeader(propName, HeaderNamespace, propValue.ToString());
					request.Headers.Add(customHeader);
				}
			}

			return Guid.NewGuid();
		}

		#endregion

		#region IEndpointBehavior Members

		/// <summary>Does nothing.</summary>
		/// <param name="endpoint"></param>
		/// <param name="bindingParameters"></param>
		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{ }

		/// <summary>
		/// Adds the instance to the <see cref="ClientRuntime.MessageInspectors"/> collection.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="clientRuntime"></param>
		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			clientRuntime.MessageInspectors.Add(this);
		}

		/// <summary>Does nothing.</summary>
		/// <param name="endpoint"></param>
		/// <param name="endpointDispatcher"></param>
		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{ }

		/// <summary>Does nothing (the behavior can be applied to any endpoint, there is no specific validation here).</summary>
		/// <param name="endpoint"></param>
		public void Validate(ServiceEndpoint endpoint)
		{ }

		#endregion
	}
}
