using log4net;
using System;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Defines a property ("PreserveServerStack") that can be specified in the configuration file.
	/// </summary>
	public abstract class FaultProcessingElementBase
		: BehaviorExtensionElement
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		const String NameProp = "preserveServerStack";

		/// <summary>
		/// Allows to specify whether to preserve a server stack trace in the configuration file as 'preserveServerStack' attribute to the behavior element.
		/// </summary>
		[ConfigurationProperty(NameProp, DefaultValue = "false", IsRequired = false)]
		public String PreserveServerStack
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
		/// Tries to parse <see cref="PreserveServerStack"/> value into <see cref="Boolean"/> and writes a warning message if it cannot.
		/// </summary>
		protected Boolean CanPreserveServerStack
		{
			get
			{
				Boolean preserveServerStack = false;
				if (!Boolean.TryParse(PreserveServerStack, out preserveServerStack))
				{
					logger.WarnFormat("Cannot parse '{0}' parameter as boolean: '{1}'. Assuming false.", NameProp, PreserveServerStack);
				}
				return preserveServerStack;
			}
		}
	}
}
