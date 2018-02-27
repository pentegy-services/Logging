using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// Represents a configuration collection of rules for <see cref="HttpLoggingModule"/>.
	/// The rule order is important! The first matching rule in the collection wins.
	/// <seealso cref="HttpLoggingModuleRuleItem"/>.
	/// </summary>
	[ConfigurationCollection(typeof(HttpLoggingModuleRuleItem), AddItemName = "save,skip", CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
	public class HttpLoggingModuleRuleCollection
		: ConfigurationElementCollection
	{
		/// <summary>This collection is of <see cref="ConfigurationElementCollectionType.BasicMapAlternate"/> type.</summary>
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMapAlternate;
			}
		}

		/// <summary>Always empty string (see <see cref="IsElementName(String)"/>).</summary>
		protected override String ElementName
		{
			get
			{
				return String.Empty;
			}
		}

		/// <summary>Determines what child nodes belong to collection items ("save" and "skip").</summary>
		protected override Boolean IsElementName(String elementName)
		{
			return (elementName == "save" || elementName == "skip");
		}

		/// <summary>Gets the element key for a specified configuration element.</summary>
		protected override Object GetElementKey(ConfigurationElement element)
		{
			return element;
		}

		/// <summary>Creates new instance of <see cref="HttpLoggingModuleRuleItem"/>.</summary>
		protected override ConfigurationElement CreateNewElement()
		{
			return new HttpLoggingModuleRuleItem();
		}

		/// <summary>Creates new <see cref="HttpLoggingModuleRuleItem"/> based on <paramref name="elementName"/>.</summary>
		protected override ConfigurationElement CreateNewElement(String elementName)
		{
			var rule = new HttpLoggingModuleRuleItem();
			if (elementName == "save")
			{
				rule.Type = LoggingRuleType.Save;
			}
			else if (elementName == "skip")
			{
				rule.Type = LoggingRuleType.Skip;
			}
			return rule;
		}

		/// <summary>Gets a value indicating whether the <see cref="ConfigurationElement"/> object is read-only.</summary>
		public override Boolean IsReadOnly()
		{
			return false;
		}
	}
}
