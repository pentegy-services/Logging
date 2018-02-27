using PentegyServices.Logging.Core;
using System;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Simplifies configuration by allowing to apply <see cref="XmlParameterInspector"/> 
	/// in the configuration file instead of code:
	/// <code>
	/// &lt;system.serviceModel&gt;
	///		&lt;extensions&gt;
	///			&lt;behaviorExtensions&gt;
	///				&lt;add name="XmlParameterInspector" type="Core.Logging.ServiceModel.XmlParameterInspectorElement, Core.Logging" /&gt;
	///			&lt;/behaviorExtensions&gt;
	///		&lt;/extensions&gt;
	///
	///		&lt;behaviors&gt;
	///			&lt;endpoInt32Behaviors&gt;
	///				&lt;behavior name="MyBehavior"&gt;
	///					&lt;XmlParameterInspector name="TestClient" log-before="false" log-after="true" log-inputs="true" log-outputs="true" /&gt;
	///	 			&lt;/behavior&gt;
	///			&lt;/endpoInt32Behaviors&gt;
	///		&lt;/behaviors&gt;
	///	&lt;/system.serviceModel&gt;
	///	</code>
	///	In the sample above all operation parameters (along with result) will be logged on the client side. <see cref="XmlParameterInspectorElement"/>
	///	can be applied to service behaviors as well.
	/// </summary>
	public class XmlParameterInspectorElement
		: BehaviorExtensionElement
	{
		const string NameProp = "name";
		const string LogBeforeProp = "log-before";
		const string LogAfterProp = "log-after";
		const string LogInputsProp = "log-inputs";
		const string LogOutputsProp = "log-outputs";
		const string XDepthLimitProp = "x-depth-limit";
		const string XSizeLimitProp = "x-size-limit";
		const string XLengthLimitProp = "x-length-limit";

		/// <summary>
		/// Allows to specify inspector name in the configuration file as 'name' attribute to the behavior element.
		/// </summary>
		[ConfigurationProperty(NameProp, DefaultValue = "", IsRequired = false)]
		public String Name
		{
			get
			{
				return (String)base[NameProp];
			}
			set
			{
				base[NameProp] = value;
			}
		}

		/// <summary>
		/// Allows to specify if the inspector should log "before" entry based on 'log-before' attribute of the behavior element.
		/// </summary>
		[ConfigurationProperty(LogBeforeProp, DefaultValue = "true", IsRequired = false)]
		public Boolean LogBefore
		{
			get
			{
				return (Boolean)base[LogBeforeProp];
			}
			set
			{
				base[LogBeforeProp] = value;
			}
		}

		/// <summary>
		/// Allows to specify if the inspector should log "after" entry based on 'log-after' attribute of the behavior element.
		/// </summary>
		[ConfigurationProperty(LogAfterProp, DefaultValue = "true", IsRequired = false)]
		public Boolean LogAfter
		{
			get
			{
				return (Boolean)base[LogAfterProp];
			}
			set
			{
				base[LogAfterProp] = value;
			}
		}

		/// <summary>
		/// Allows to specify if the inspector should log inputs parameters based on 'log-inputs' attribute of the behavior element.
		/// </summary>
		[ConfigurationProperty(LogInputsProp, DefaultValue = "true", IsRequired = false)]
		public Boolean LogInputs
		{
			get
			{
				return (Boolean)base[LogInputsProp];
			}
			set
			{
				base[LogInputsProp] = value;
			}
		}

		/// <summary>
		/// Allows to specify if the inspector should log output parameters (result) based on 'log-outputs' attribute of the behavior element.
		/// </summary>
		[ConfigurationProperty(LogOutputsProp, DefaultValue = "true", IsRequired = false)]
		public Boolean LogOutputs
		{
			get
			{
				return (Boolean)base[LogOutputsProp];
			}
			set
			{
				base[LogOutputsProp] = value;
			}
		}

		/// <summary>Allows to specify a value to configure <see cref="XSerializer.DepthLimit"/> parameter based on 'x-depth-limit' attribute of the behavior element.</summary>
		[ConfigurationProperty(XDepthLimitProp, DefaultValue = XSerializer.DefaultDepthLimit, IsRequired = false)]
		[IntegerValidator(MinValue = 1, MaxValue = Int32.MaxValue)]
		public Int32 XDepthLimit
		{
			get
			{
				return (Int32)base[XDepthLimitProp];
			}
			set
			{
				base[XDepthLimitProp] = value;
			}
		}

		/// <summary>Allows to specify a value to configure <see cref="XSerializer.SizeLimit"/> parameter based on 'x-size-limit' attribute of the behavior element.</summary>
		[ConfigurationProperty(XSizeLimitProp, DefaultValue = XSerializer.DefaultSizeLimit, IsRequired = false)]
		[IntegerValidator(MinValue = 1, MaxValue = Int32.MaxValue)]
		public Int32 XSizeLimit
		{
			get
			{
				return (Int32)base[XSizeLimitProp];
			}
			set
			{
				base[XSizeLimitProp] = value;
			}
		}

		/// <summary>Allows to specify a value to configure <see cref="XSerializer.LengthLimit"/> parameter based on 'x-length-limit' attribute of the behavior element.</summary>
		[ConfigurationProperty(XLengthLimitProp, DefaultValue = XSerializer.DefaultLengthLimit, IsRequired = false)]
		[IntegerValidator(MinValue = 1, MaxValue = Int32.MaxValue)]
		public Int32 XLengthLimit
		{
			get
			{
				return (Int32)base[XLengthLimitProp];
			}
			set
			{
				base[XLengthLimitProp] = value;
			}
		}

		/// <summary>Returns type of <see cref="XmlParameterInspector"/>.</summary>
		public override Type BehaviorType
		{
			get
			{
				return typeof(XmlParameterInspector);
			}
		}

		/// <summary>
		/// Create an instance of <see cref="XmlParameterInspector"/> with parameters specified in the confiduration file
		/// (see <see cref="Name"/>, <see cref="LogInputs"/>, <see cref="LogOutputs"/>).
		/// </summary>
		/// <returns>An instance of <see cref="XmlParameterInspector"/>.</returns>
		protected override Object CreateBehavior()
		{
			var settings = new XmlParamInspectorSettings
			{
				Name = Name,
				LogBefore = LogBefore,
				LogAfter = LogAfter,
				LogInputs = LogInputs,
				LogOutputs = LogOutputs,
				XDepthLimit= XDepthLimit,
				XLengthLimit = XLengthLimit,
				XSizeLimit = XSizeLimit
			};

			//return new XmlParameterInspector(Name, LogBefore, LogAfter, LogInputs, LogOutputs);
			return new XmlParameterInspector(settings);
		}
	}
}
