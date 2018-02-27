using System;
using System.Configuration;
using System.Linq;
using System.ServiceModel.Configuration;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Base class for all extension elements that have a collection of property names defined in configuration.
	/// </summary>
	public abstract class ThreadContextMessageInspectorElementBase
		: BehaviorExtensionElement
	{
		static ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection();

		static ThreadContextMessageInspectorElementBase()
		{
			properties.Add(new ConfigurationProperty("properties", typeof(PropertyElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection));
		}

		/// <summary>Returns a collection of all configuration properties.</summary>
		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				return properties;
			}
		}

		/// <summary>Returns a collection of property names defined in the configuration.</summary>
		public String[] PropertyNames
		{
			get
			{
				var propertyNames = new String[0];
				var contextProperties = this["properties"] as PropertyElementCollection;
				if (contextProperties != null)
				{
					propertyNames = contextProperties.OfType<PropertyNameElement>()
						.Select(x => x.PropertyName)
						.Where(x => !String.IsNullOrEmpty(x))
						.ToArray();
				}
				return propertyNames;
			}
		}
	}
}
