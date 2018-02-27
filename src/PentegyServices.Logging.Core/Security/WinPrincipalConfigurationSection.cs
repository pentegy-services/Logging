using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.Security
{
	/// <summary>
	/// Represents a section within configuration file for <see cref="WinPrincipal"/> class:
	/// <code>
	/// &lt;configSections&gt;
	///		&lt;section name="winPrincipal" type="Core.Security.WinPrincipalConfigurationSection, Core" /&gt;
	///	&lt;/configSections&gt;
	///	
	///&lt;winPrincipal domain="TEST"&gt;
	///	&lt;mapping&gt;
	///		&lt;map role="Chief" group="GroupBoss"/&gt;
	///		&lt;map role="Boss" group=""/&gt;
	///		&lt;map role="Regular" group="BUILTIN\Users"/&gt;
	///	&lt;/mapping&gt;
	///&lt;/winPrincipal&gt;
	/// </code>
	/// The configuration above defines that "Boss" entry will be skipped (empty definition), members of "TEST\GroupBoss" Windows group will have "Chief" role and
	/// members of "BUILTIN\Users" Windows group will have "Regular" role, i.e.
	/// <code>
	///	var winPrincipal = new WinPrincipal(WindowsIdentity.GetCurrent());
	/// bool hasBoth = winPrincipal.IsInRole("Chief") &amp;&amp; winPrincipal("Regular");
	/// </code>
	/// The code above will produce <c>true</c> if the current principal is a member of both "TEST\GroupBoss" and "BUILTIN\Users" Windows groups.
	///
	/// <seealso cref="WinPrincipalMapCollection"/>.
	/// <seealso cref="WinPrincipalMapItem"/>.
	/// </summary>
	public class WinPrincipalConfigurationSection
		: ConfigurationSection
	{
		static WinPrincipalConfigurationSection settings = (WinPrincipalConfigurationSection)ConfigurationManager.GetSection("winPrincipal");

		/// <summary>Current (or default) settings from the configuration file.</summary>
		public static WinPrincipalConfigurationSection Settings
		{
			get
			{
				return settings;
			}
		}

		const String DomainProp = "domain";

		/// <summary>Property to specify Windows domain name.</summary>
		[ConfigurationProperty(DomainProp, DefaultValue = "false", IsRequired = false)]
		public String Domain
		{
			get
			{
				return (String)base[DomainProp];
			}
			set
			{
				base[DomainProp] = value;
			}
		}

		/// <summary>Collection of rules to determine what requests must be logged.</summary>
		[ConfigurationProperty("mapping", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
		public WinPrincipalMapCollection Mapping
		{
			get
			{
				return ((WinPrincipalMapCollection)(base["mapping"]));
			}
			set
			{
				base["mapping"] = value;
			}
		}  
	}
}
