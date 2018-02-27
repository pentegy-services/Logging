using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.Security
{
	/// <summary>
	/// Represents a configuration collection of role to group mappings for <see cref="WinPrincipal"/>.
	/// <seealso cref="WinPrincipalMapItem"/>.
	/// </summary>
	[ConfigurationCollection(typeof(WinPrincipalMapItem), AddItemName = "map", CollectionType = ConfigurationElementCollectionType.BasicMap)]
	public class WinPrincipalMapCollection
		: ConfigurationElementCollection
	{
		/// <summary>This collection is of <see cref="ConfigurationElementCollectionType.BasicMap"/> type.</summary>
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMap;
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

		/// <summary>Determines what child nodes belong to collection items ("map" only).</summary>
		protected override Boolean IsElementName(String elementName)
		{
			return (elementName == "map");
		}

		/// <summary>Gets the element key for a specified configuration element.</summary>
		protected override Object GetElementKey(ConfigurationElement element)
		{
			return element;
		}

		/// <summary>Creates new instance of <see cref="WinPrincipalMapItem"/>.</summary>
		protected override ConfigurationElement CreateNewElement()
		{
			return new WinPrincipalMapItem();
		}

		/// <summary>Creates new <see cref="WinPrincipalMapItem"/> based on <paramref name="elementName"/>.</summary>
		protected override ConfigurationElement CreateNewElement(String elementName)
		{
			var rule = new WinPrincipalMapItem();
			return rule;
		}

		/// <summary>Gets a value indicating whether the <see cref="ConfigurationElement"/> object is read-only.</summary>
		public override Boolean IsReadOnly()
		{
			return false;
		}
	}
}
