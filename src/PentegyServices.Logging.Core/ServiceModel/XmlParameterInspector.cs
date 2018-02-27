using log4net;
using PentegyServices.Logging.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml.Linq;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Allows all WCF operation parameters to be logged into log4net engine in xml format.
	/// <see cref="XmlParameterInspector"/> relies on <see cref="XSerializer"/> so all its formatting and masking rules apply.
	/// The inspector can be attached to both client and service sides using corresponding behaviors.
	/// See <see cref="XmlParameterInspectorElement"/>
	/// <para>
	/// The component was tested on synchronous calls only.
	/// </para>
	/// </summary>
	public class XmlParameterInspector
		: IParameterInspector, IOperationBehavior, IEndpointBehavior, IServiceBehavior, IErrorHandler, IClientMessageInspector
	{
		internal static class EventLocation
		{
			public static String Client = "c"; // client (proxy) side
			public static String Service = "s"; // service side
		}

		/// <summary>
		/// Used to keep state to log "after" events.
		/// On service side we use IExtension{InstanceContext} implementation to catch faulted calls
		/// while on client side hope that thread static clientCorrelationState field will do the same job.
		/// </summary>
		class CorrelationState
			: IExtension<InstanceContext>
		{
			public XElement Input;
			public Stopwatch Stopwatch;
			public String OperationName;
			public String Type;
			public String Where;

			#region IExtension<InstanceContext> Members

			public void Attach(InstanceContext owner) {}

			public void Detach(InstanceContext owner) {}

			#endregion
		}

		static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>Full contract type name to add as "type" attribute.</summary>
		protected readonly String TypeName = "";

		/// <summary>Location where the logging happens (client-side or server-side).</summary>
		protected readonly String Where = "";

		/// <summary>Settings for the inspector and underlaying <see cref="XSerializer"/>.</summary>
		protected readonly XmlParamInspectorSettings Settings = new XmlParamInspectorSettings();

		readonly XSerializer serializer;

		/// <summary>
		/// The idea here is that IClientMessageInspector.AfterReceiveReply() will be called on the same thread as IParameterInspector.BevoreCall().
		/// At least for synchronous calls, so this can only be proved by stress testing :(
		/// </summary>
		[ThreadStatic]
		static CorrelationState clientCorrelationState;

		/// <summary>Default ctor.</summary>
		public XmlParameterInspector()
		{ }

		/// <summary>
		/// Creates an instance of <see cref="XmlParameterInspector"/> with specified settings.
		/// </summary>
		/// <param name="settings">Settings for the inspector and underlaying <see cref="XSerializer"/>.</param>
		public XmlParameterInspector(XmlParamInspectorSettings settings)
			: this(settings, null, null)
		{ }

		/// <summary>
		/// To be used by the class itself.
		/// </summary>
		XmlParameterInspector(XmlParamInspectorSettings settings, String where, String typeName)
		{
			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}

			Settings = settings;
			Where = where;
			TypeName = typeName;

			serializer = new XSerializer(Settings.XDepthLimit, settings.XSizeLimit, settings.XLengthLimit);
		}

		#region IParameterInspector Members

		/// <summary>
		/// Formats output call arguments (using <see cref="XSerializer"/>) and then logs it to log4net.
		/// Note, if an exception is thrown during the processing of the call (e.g., the server returned a fault), AfterCall will not be called on the client.
		/// </summary>
		public void AfterCall(String operationName, Object[] outputs, Object returnValue, Object correlationState)
		{
			var _correlationState = (CorrelationState)correlationState;

			LogAfterCall(_correlationState, outputs, returnValue, false);
		}

		void LogAfterCall(CorrelationState correlationState, Object[] outputs, Object returnValue, Boolean error)
		{
			correlationState.Stopwatch.Stop();

			if (Settings.LogAfter)
			{
				var root = new XElement("wcf",
					new XAttribute("inspector", Settings.Name ?? ""),
					new XAttribute("when", "a"),
					new XAttribute("where", correlationState.Where),
					new XAttribute("type", correlationState.Type),
					new XAttribute("method", correlationState.OperationName),
					new XAttribute("ms", correlationState.Stopwatch.ElapsedMilliseconds.ToString().PadLeft(6, '0'))
				);

				if (Settings.LogBefore)
				{
					root.Add(new XAttribute("correlationState", correlationState.Stopwatch.GetHashCode().ToString())); // correlation state is needed only if both LogAfter and LogBefore are true
				}

				if (Settings.LogInputs && !Settings.LogBefore && correlationState.Input != null)
				{
					root.Add(correlationState.Input); // inputs are only need to be logged in 'after' entry if not logged in 'before' entry
				};

				if (Settings.LogOutputs)
				{
					try
					{
						root.Add(new XElement("outputs", serializer.Serialize(outputs)));
						var returnValueElement = new XElement("returnValue", serializer.Serialize(returnValue));
						if (error)
						{
							returnValueElement.Add(new XAttribute("exception", true));
						}
						root.Add(returnValueElement);
					}
					catch (Exception ex)
					{
						logger.Fatal(ex);
					}
				};
	
				logger.Info(root);
			}

			// let's clear the context stored for IErrorHandler
			if (OperationContext.Current != null && OperationContext.Current.InstanceContext != null && OperationContext.Current.InstanceContext.Extensions != null)
			{
				CorrelationState _correlationState = OperationContext.Current.InstanceContext.Extensions.Find<CorrelationState>();
				if (_correlationState != null)
				{
					OperationContext.Current.InstanceContext.Extensions.Remove(_correlationState);
				}
			}
			// same for client reference
			clientCorrelationState = null;
		}

		/// <summary>
		/// Formats input call arguments (using <see cref="XSerializer"/>) and then logs it to log4net.
		/// </summary>
		public Object BeforeCall(String operationName, Object[] inputs)
		{
			var stopwatch = new Stopwatch();
			XElement inputElement = null;

			if ((Settings.LogBefore || Settings.LogAfter) && Settings.LogInputs)
			{
				try
				{
					inputElement = new XElement("inputs", serializer.Serialize(inputs));
				}
				catch (Exception ex)
				{
					logger.Fatal(ex);
				}
			};

			var root = new XElement("wcf",
				new XAttribute("inspector", Settings.Name ?? ""),
				new XAttribute("when", "b"),
				new XAttribute("where", Where ?? ""),
				new XAttribute("type", TypeName ?? ""),
				new XAttribute("method", operationName ?? "")
			);

			if (Settings.LogBefore)
			{
				if (Settings.LogAfter)
				{
					root.Add(new XAttribute("correlationState", stopwatch.GetHashCode().ToString()));  // correlation state is needed only if both LogAfter and LogBefore are true
				}

				if (Settings.LogInputs)
				{
					root.Add(inputElement);
				};
				logger.Info(root);
			}

			var correlationState = new CorrelationState
			{
				OperationName = operationName,
				Input = inputElement,
				Stopwatch = stopwatch,
				Where = Where ?? "",
				Type = TypeName ?? ""
			};

			if (Where == EventLocation.Service)
			{
				// the problem: on the service side if the method call faults IParameterInspector.AfterCall() will not be called
				// thus if we have LogAfter true and need to store the inputs they will be lost
				// to overcome the limitation we have IErrorHandle but we cannot get the correlation state outside of IParameterInspector
				// it appears the best place to store it is OperationContext.Current.InstanceContext.Extensions

				if (OperationContext.Current != null && OperationContext.Current.InstanceContext != null && OperationContext.Current.InstanceContext.Extensions != null)
				{
					OperationContext.Current.InstanceContext.Extensions.Add(correlationState); // remember for IErrorHandler if the method call fails
				}
			}

			if (Where == EventLocation.Client)
			{
				clientCorrelationState = correlationState;
			}

			correlationState.Stopwatch.Start(); // start measuring call time

			return correlationState; // return for AfterCall() (if the method call succeeds)
		}

		#endregion

		#region IOperationBehavior Members

		/// <summary>Does nothing.</summary>
		/// <param name="operationDescription"></param>
		/// <param name="bindingParameters"></param>
		public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
		{ }

		/// <summary>
		/// Adds the instance to <see cref="ClientOperation.ParameterInspectors"/> collection.
		/// </summary>
		/// <param name="operationDescription"></param>
		/// <param name="clientOperation"></param>
		public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
		{
			clientOperation.ParameterInspectors.Add(this);
		}

		/// <summary>
		/// Adds the instance to <see cref="DispatchOperation.ParameterInspectors"/> collection.
		/// </summary>
		/// <param name="operationDescription"></param>
		/// <param name="dispatchOperation"></param>
		public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
		{
			dispatchOperation.ParameterInspectors.Add(this);
		}

		/// <summary>Does nothing.</summary>
		/// <param name="operationDescription"></param>
		public void Validate(OperationDescription operationDescription)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEndpointBehavior Members

		/// <summary>Does nothing.</summary>
		/// <param name="endpoint"></param>
		/// <param name="bindingParameters"></param>
		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{ }

		/// <summary>
		/// Adds the instance to the <see cref="ClientOperation.ParameterInspectors"/> collection for all operations in <paramref name="clientRuntime"/>.
		/// </summary>
		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			foreach (ClientOperation clientOperation in clientRuntime.Operations)
			{
				clientOperation.ParameterInspectors.Add(
					new XmlParameterInspector(Settings, EventLocation.Client, endpoint.Contract.ContractType.FullName));
			}

			// attach IClientMessageInspector so we can detect faulted events on client side
			// we use Insert() instead of Add() to inject it as the very first 
			// because other IClientMessageInspector attached (like FaultClientMessageInspector that rethrows faults)
			// may stop the chain and AfterReceiveReply() will never be hit.
			clientRuntime.MessageInspectors.Insert(0, this); 
		}

		/// <summary>
		/// Adds the instance to the <see cref="DispatchOperation.ParameterInspectors"/> collection for all operations in <paramref name="endpointDispatcher"/>.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="endpointDispatcher"></param>
		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
			foreach (DispatchOperation dispatchOperation in endpointDispatcher.DispatchRuntime.Operations)
			{
				dispatchOperation.ParameterInspectors.Add(
					new XmlParameterInspector(Settings, EventLocation.Service, endpoint.Contract.ContractType.FullName));
			}
		}

		/// <summary>Does nothing (the behavior can be applied to any endpoint, there is no specific validation here).</summary>
		/// <param name="endpoint"></param>
		public void Validate(ServiceEndpoint endpoint)
		{ }

		#endregion

		#region IServiceBehavior Members

		/// <summary>Does nothing.</summary>
		public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{ }

		/// <summary>
		/// Adds the instance to <see cref="System.ServiceModel.Dispatcher.DispatchRuntime.MessageInspectors"/> collection for all
		/// endpoints in <paramref name="serviceHostBase"/>.
		/// </summary>
		public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			var inspector = new XmlParameterInspector(Settings, EventLocation.Service, serviceDescription.ServiceType.FullName);

			foreach (ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers)
			{
				foreach (EndpointDispatcher endpoint in dispatcher.Endpoints)
				{
					foreach (DispatchOperation op in endpoint.DispatchRuntime.Operations)
					{
						op.ParameterInspectors.Add(inspector);
					}
				}

				// attach IErrorHandler so we can catch "after" events when service call faults
				dispatcher.ErrorHandlers.Add(this);
			}
		}

		/// <summary>Does nothing.</summary>
		public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{ }

		#endregion

		#region IErrorHandler Members

		/// <summary>Does nothing.</summary>
		public Boolean HandleError(Exception error)
		{
			return false;
		}

		/// <summary>Allows to log "after" events in case of errors.</summary>
		public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
		{
			// MSDN:
			// All ProvideFault implementations are called first, prior to sending a response message. When all ProvideFault implementations have been called and return, and if fault is non-null, it is sent back to the client according to the operation contract. If fault is null after all implementations have been called, the response message is controlled by the ServiceBehaviorAttribute.IncludeExceptionDetailInFaults property value.

			// do nothing except logging because other error handlers may be attached, let them do their work

			if (OperationContext.Current != null && OperationContext.Current.InstanceContext != null && OperationContext.Current.InstanceContext.Extensions != null)
			{
				CorrelationState correlationState = OperationContext.Current.InstanceContext.Extensions.Find<CorrelationState>();
				if (correlationState != null)
				{
					String errorToLog = error != null ? error.ToString() : "{null}";
					LogAfterCall(correlationState, new Object[0], errorToLog, true);
				}
				else
				{
					Trace.TraceWarning("Expected CorrelationState to be present in OperationContext.Current.InstanceContext.Extensions but it was {null}.");
				}
			}
		}

		#endregion

		#region IClientMessageInspector Members

		/// <summary>A message to write on client side when a remote call faults.</summary>
		protected internal const String ClientFaultMessage = "No fault details available at this point. See subsequent log events.";

		/// <summary>Allows to log "after" events in case of errors.</summary>
		public void AfterReceiveReply(ref Message reply, Object correlationState)
		{
			if (reply.IsFault && clientCorrelationState != null)
			{
				// we don't try to deserialize it (not smart idea), instead we just log the event
				LogAfterCall(clientCorrelationState, new Object[0], ClientFaultMessage, true);
			}
		}

		/// <summary>Does nothing.</summary>
		public Object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			return null;
		}

		#endregion
	}
}
