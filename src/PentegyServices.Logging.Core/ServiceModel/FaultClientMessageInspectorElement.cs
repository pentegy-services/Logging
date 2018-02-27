using System;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Simplifies configuration be allowing to apply <see cref="FaultClientMessageInspector"/> in the configuration file
	/// rather than as the attribute:
	/// 
	/// <code>
	/// &lt;system.serviceModel&gt;
	///		&lt;extensions&gt;
	///			&lt;behaviorExtensions&gt;
	///				&lt;add name="FaultClientMessageInspector" type="Core.ServiceModel.FaultClientMessageInspectorElement, Core" /&gt;
	///			&lt;/behaviorExtensions&gt;
	///		&lt;/extensions&gt;
	///
	///		&lt;behaviors&gt;
	///			&lt;endpointBehaviors&gt;
	///				&lt;behavior name="FaultBehavior"&gt;
	///					&lt;FaultClientMessageInspector/&gt;
	///	 			&lt;/behavior&gt;
	///			&lt;/endpointBehaviors&gt;
	///		&lt;/behaviors&gt;
	///	&lt;/system.serviceModel&gt; 
	/// </code>
	/// </summary>
	public class FaultClientMessageInspectorElement
		: FaultProcessingElementBase
	{
		/// <summary>Returns type of <see cref="FaultClientMessageInspector"/>.</summary>
		public override Type BehaviorType
		{
			get
			{
				return typeof(FaultClientMessageInspector);
			}
		}

		/// <summary>
		/// Creates new instance of <see cref="FaultClientMessageInspector"/>.
		/// </summary>
		/// <returns></returns>
		protected override Object CreateBehavior()
		{
			return new FaultClientMessageInspector(CanPreserveServerStack);
		}
	}
}
