using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Represents a configuration element containing a collection of child <see cref="PropertyNameElement"/> elements.
	/// </summary>
	public class PropertyElementCollection
		: ConfigurationElementCollection
	{
		static ConfigurationPropertyCollection properties;

		/// <summary>Default ctor.</summary>
		public PropertyElementCollection()
		{
			properties = new ConfigurationPropertyCollection();
		}

		/// <summary>Gets the collection of properties.</summary>
		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				return properties;
			}
		}

		/// <summary>Gets the type of the <see cref="PropertyElementCollection"/>.</summary>
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMap;
			}
		}

		/// <summary>Gets the name used to identify this collection of elements in the configuration file. The overriden value is 'add'.</summary>
		protected override String ElementName
		{
			get
			{
				return "add";
			}
		}

		/// <summary>Creates new instance of <see cref="PropertyNameElement"/>.</summary>
		/// <returns></returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new PropertyNameElement();
		}

		/// <summary>
		/// Gets the element key for a specified <see cref="PropertyNameElement"/>.
		/// </summary>
		/// <param name="element"></param>
		/// <returns><see cref="PropertyNameElement.PropertyName"/> if instance of <see cref="PropertyNameElement"/> is passed.</returns>
		protected override object GetElementKey(ConfigurationElement element)
		{
			var propNameElement = element as PropertyNameElement;
			if (propNameElement == null)
			{
				throw new ArgumentNullException();
			}
			return propNameElement.PropertyName;
		}
	}
}
