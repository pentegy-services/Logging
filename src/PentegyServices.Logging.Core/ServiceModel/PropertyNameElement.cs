using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Represents a configuration element within <see cref="PropertyElementCollection"/>.
	/// </summary>
	public class PropertyNameElement
		: ConfigurationElement
	{
		static ConfigurationProperty property = new ConfigurationProperty("name", typeof(String), null, ConfigurationPropertyOptions.IsKey);

		static ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection { property };

		/// <summary>Gets the collection of properties.</summary>
		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				return properties;
			}
		}

		/// <summary>Default ctor.</summary>
		public PropertyNameElement()
		{ }

		/// <summary>Creates new instance with specified property name.</summary>
		/// <param name="propertyName"></param>
		public PropertyNameElement(String propertyName)
			: this()
		{
			PropertyName = propertyName;
		}

		/// <summary>Property name value as defined in the configuration.</summary>
		public String PropertyName
		{
			get
			{
				return this[property] as String;
			}
			set
			{
				this[property] = value;
			}
		}
	}
}
