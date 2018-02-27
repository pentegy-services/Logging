using log4net;
using System;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Simplifies configuration by allowing to apply <see cref="FaultServiceErrorHandler"/> behavior in the configuration file
	/// rather than as the attribute:
	/// 
	/// <code>
	/// &lt;system.serviceModel&gt;
	///		&lt;extensions&gt;
	///			&lt;behaviorExtensions&gt;
	///				&lt;add name="FaultServiceErrorHandler" type="Core.ServiceModel.FaultServiceErrorHandlerElement, Core" /&gt;
	///			&lt;/behaviorExtensions&gt;
	///		&lt;/extensions&gt;
	///
	///		&lt;behaviors&gt;
	///			&lt;serviceBehaviors&gt;
	///				&lt;behavior name="FaultBehavior"&gt;
	///					&lt;FaultServiceErrorHandler/&gt;
	///				&lt;/behavior&gt;
	///			&lt;/serviceBehaviors&gt;
	///		&lt;/behaviors&gt;
	///	&lt;/system.serviceModel&gt; 
	/// </code>
	/// </summary>
	public class FaultServiceErrorHandlerElement
		: FaultProcessingElementBase
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>Returns type of <see cref="FaultServiceErrorHandler"/>.</summary>
		public override Type BehaviorType
		{
			get
			{
				return typeof(FaultServiceErrorHandler);
			}
		}

		/// <summary>
		/// Creates new instance of <see cref="FaultServiceErrorHandler"/>.
		/// </summary>
		/// <returns></returns>
		protected override Object CreateBehavior()
		{
			return new FaultServiceErrorHandler(CanPreserveServerStack);
		}
	}
}
