using System;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Simplifies configuration by allowing to apply <see cref="ThreadContextServiceMessageInspector"/> 
	/// in the configuration file instead of code:
	/// <code>
	/// 
	/// &lt;system.serviceModel&gt;
	///		&lt;extensions&gt;
	///			&lt;behaviorExtensions&gt;
	///				&lt;add name="ThreadContextServiceMessageInspector" type="Core.Logging.ServiceModel.ThreadContextServiceMessageInspectorElement, Core.Logging" /&gt;
	///			&lt;/behaviorExtensions&gt;
	///		&lt;/extensions&gt;
	///
	///		&lt;behaviors&gt;
	///			&lt;serviceBehaviors&gt;
	///				&lt;behavior name="LoggingBehavior"&gt;
	///					&lt;ThreadContextServiceMessageInspector&gt;
	///						&lt;properties&gt;
	///							&lt;add name="sessionID"/&gt;
	///							&lt;add name="loggingID"/&gt;
	///						&lt;/properties&gt;
	///					&lt;/ThreadContextServiceMessageInspector&gt;
	///	 			&lt;/behavior&gt;
	///			&lt;/serviceBehaviors&gt;
	///		&lt;/behaviors&gt;
	///	&lt;/system.serviceModel&gt;
	/// </code>
	/// In the sample above properties <c>sessionID</c> and <c>loggingID</c> will 
	/// be extracted from <see cref="System.ServiceModel.Channels.MessageHeader"/> of the incoming messages and put into <see cref="log4net.ThreadContext"/>.
	/// </summary>
	public class ThreadContextServiceMessageInspectorElement
		: ThreadContextMessageInspectorElementBase
	{
		/// <summary>Returns type of <see cref="ThreadContextClientMessageInspector"/>.</summary>
		public override Type BehaviorType
		{
			get
			{
				return typeof(ThreadContextServiceMessageInspector);
			}
		}

		/// <summary>
		/// Create an instance of <see cref="ThreadContextServiceMessageInspector"/>
		/// with property names defined in the current configuration.
		/// </summary>
		/// <returns>An instance of <see cref="ThreadContextServiceMessageInspector"/>.</returns>
		protected override Object CreateBehavior()
		{
			return new ThreadContextServiceMessageInspector(PropertyNames);
		}
	}
}
