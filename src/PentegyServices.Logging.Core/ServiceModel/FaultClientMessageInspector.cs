using log4net;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Tries to extract fault detail (derived from <see cref="Exception"/>) from within typed <see cref="FaultException{T}"/> 
	/// and then rethrows it (thus original stack trace is not preserved; it's ok because we don't want server stack trace on the client side because of security considerations).
	/// That allows completely transparent "throw on a server - catch on a client" scenario while still maintaining 
	/// SOAP compatibility (as long as you have appropriate <see cref="FaultContractAttribute"/> defined for your exceptions).
	/// Clients have no need now to catch <see cref="FaultException{T}"/> which allows to utilize inheritance (!) in <c>try/catch</c> blocks
	/// which is not possible with <see cref="FaultException{T}"/>.
	/// 
	/// <seealso cref="FaultServiceErrorHandler"/>
	/// </summary>
	public class FaultClientMessageInspector
		: IClientMessageInspector, IEndpointBehavior
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>Indicates whether to put original stack trace into the reply.</summary>
		protected readonly Boolean PreserveServerStack;

		/// <summary>
		/// Creates the instance.
		/// </summary>
		/// <param name="preserveServerStack"><c>true</c> to put original stack trace into the reply, <c>false</c> - to clear it.</param>
		public FaultClientMessageInspector(Boolean preserveServerStack)
		{
			PreserveServerStack = preserveServerStack;
		}

		/// <summary>
		/// http://stackoverflow.com/questions/57383/in-c-how-can-i-rethrow-innerexception-without-losing-stack-trace
		/// </summary>
		/// <param name="e"></param>
		static void PreserveStackTrace(Exception e)
		{
			var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
			var mgr = new ObjectManager(null, ctx);
			var si = new SerializationInfo(e.GetType(), new FormatterConverter());

			e.GetObjectData(si, ctx);
			mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
			mgr.DoFixups(); // ObjectManager calls SetObjectData

			// voila, e is unmodified save for _remoteStackTraceString
		}

		#region IClientMessageInspector Members

		/// <summary>
		/// Tries to deserialize <see cref="Exception"/> from the reply and throws it.
		/// </summary>
		public void AfterReceiveReply(ref Message reply, Object correlationState)
		{
			if (reply.IsFault)
			{
				MessageBuffer buffer = reply.CreateBufferedCopy(Int32.MaxValue); // Create a copy of the original reply to allow default WCF processing
				Message copy = buffer.CreateMessage();	// Create a copy to work with
				reply = buffer.CreateMessage();			// Restore the original message 

				MessageFault fault = MessageFault.CreateFault(copy, Int32.MaxValue);
				if (fault.HasDetail)
				{
					XmlDictionaryReader reader = fault.GetReaderAtDetailContents();
					Type faultType = null;
					// assume it's a known fault and try to locate the corresponding type
					try
					{
						Type[] faultTypes = AppDomain.CurrentDomain.GetAssemblies()
							.SelectMany(x => x.GetTypes())
							.Where(x => x.Name == reader.Name && typeof(Exception).IsAssignableFrom(x))
							.ToArray();
						faultType = faultTypes.FirstOrDefault();
						if (faultTypes.Length > 1)
						{
							logger.WarnFormat("Fault detail '{0}' matches many types: {1}", reader.Name,
								String.Join(", ", faultTypes.Select(x => x.AssemblyQualifiedName).ToArray()));
						}
						else if (!faultTypes.Any())
						{
							logger.WarnFormat("Fault detail '{0}' does no match any type.", reader.Name);
						}
						else
						{
							logger.DebugFormat("Fault detail '{0}' maps to type {1}", reader.Name, faultType.FullName);
						}
					}
					catch (Exception ex)
					{
						logger.Fatal("Cannot locate corresponding fault type", ex);
						throw;
					}

					if (faultType != null) // if found - extract the instance by trying to deserialize it
					{
						Exception faultDetail = null;
						try
						{
							MethodInfo method = fault.GetType().GetMethod("GetDetail", new Type[0]);
							MethodInfo generic = method.MakeGenericMethod(faultType);
							faultDetail = generic.Invoke(fault, null) as Exception;
						}
						catch (Exception ex)
						{
							logger.ErrorFormat("Cannot extract fault detail of {0}: {1}", faultType.FullName, ex);
						}

						if (faultDetail != null)
						{
							if (PreserveServerStack)
							{
								PreserveStackTrace(faultDetail);
							}
							throw faultDetail; // extracted original exception! throw it now!
						}
					}
				}
			}
		}

		/// <summary>Does nothing.</summary>
		public Object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			return null;
		}

		#endregion

		#region IEndpointBehavior Members

		/// <summary>Does nothing.</summary>
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
		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{ }

		/// <summary>Does nothing.</summary>
		public void Validate(ServiceEndpoint endpoint)
		{
			// can be applied to any endpoint, no specific validation
		}

		#endregion
	}
}
