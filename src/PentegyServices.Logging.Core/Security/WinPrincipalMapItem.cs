using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.Security
{
	/// <summary>
	///		Represents a configuration element of a single map definition within <see cref="WinPrincipalMapCollection"/> 
	///		for <see cref="WinPrincipal"/>.
	/// </summary>
	public class WinPrincipalMapItem 
		: ConfigurationElement
	{
		/// <summary>Logical role to map Windows group to.</summary>
		[ConfigurationProperty("role")]
		public String Role
		{
			get
			{
				return (String)base["role"];
			}
			set
			{
				base["role"] = value;
			}
		}

		/// <summary>Windows group name (without domain) to map from.</summary>
		[ConfigurationProperty("group")]
		public String Group
		{
			get
			{
				return (String)base["group"];
			}
			set
			{
				base["group"] = value;
			}
		}

		/// <summary>Windows group SID to map from.</summary>
		[ConfigurationProperty("sid")]
		public String SID
		{
			get
			{
				return (String)base["sid"];
			}
			set
			{
				base["sid"] = value;
			}
		}
	}  
}
