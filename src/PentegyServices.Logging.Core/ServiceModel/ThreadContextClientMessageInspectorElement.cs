using System;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Simplifies configuration by allowing to apply <see cref="ThreadContextClientMessageInspector"/> 
	/// in the configuration file instead of code:
	/// <code>
	/// 
	/// &lt;system.serviceModel&gt;
	///		&lt;extensions&gt;
	///			&lt;behaviorExtensions&gt;
	///				&lt;add name="ThreadContextClientMessageInspector" type="Core.Logging.ServiceModel.ThreadContextClientMessageInspectorElement, Core.Logging" /&gt;
	///			&lt;/behaviorExtensions&gt;
	///		&lt;/extensions&gt;
	///
	///		&lt;behaviors&gt;
	///			&lt;endpointBehaviors&gt;
	///				&lt;behavior name="LoggingBehavior"&gt;
	///					&lt;ThreadContextClientMessageInspector&gt;
	///						&lt;properties&gt;
	///							&lt;add name="sessionID"/&gt;
	///							&lt;add name="loggingID"/&gt;
	///						&lt;/properties&gt;
	///					&lt;/ThreadContextClientMessageInspector&gt;
	///	 			&lt;/behavior&gt;
	///			&lt;/endpointBehaviors&gt;
	///		&lt;/behaviors&gt;
	///	&lt;/system.serviceModel&gt;
	/// </code>
	/// In the sample above properties <c>sessionID</c> and <c>loggingID</c> from <see cref="log4net.ThreadContext"/> will 
	/// be added to <see cref="System.ServiceModel.Channels.MessageHeader"/> of all outgoing messages.
	/// </summary>
	public class ThreadContextClientMessageInspectorElement
		: ThreadContextMessageInspectorElementBase
	{
		/// <summary>Returns type of <see cref="ThreadContextClientMessageInspector"/>.</summary>
		public override Type BehaviorType
		{
			get
			{
				return typeof(ThreadContextClientMessageInspector);
			}
		}

		/// <summary>
		/// Create an instance of <see cref="ThreadContextClientMessageInspector"/>
		/// with property names defined in the current configuration.
		/// </summary>
		/// <returns>An instance of <see cref="ThreadContextClientMessageInspector"/>.</returns>
		protected override Object CreateBehavior()
		{
			return new ThreadContextClientMessageInspector(PropertyNames);
		}
	}
}
