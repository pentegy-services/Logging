using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// Represents a section within configuration file for <see cref="HttpLoggingModule"/>:
	/// <code>
	/// &lt;configSections&gt;
	///		&lt;section name="httpLogging" type="Core.Logging.Web.HttpLoggingModuleConfigurationSection, Core.Logging" /&gt;
	///	&lt;/configSections&gt;
	///	
	///	&lt;httpLogging debug="true" headers="false" cookies="true" form="true" maxHeaderLength="128" maxCookieLength="200" maxFormLength="400" &gt; 
	///		&lt;rules&gt;
	///			&lt;skip url="\.(gif|png|js)$" /&gt;
	///			&lt;skip status="302" ip="10\.55\.24\.8(0|1)" url="https://10\.55\.24\.69/" /&gt;
	///			&lt;save method="GET" /&gt;
	///		&lt;/rules&gt;
	///	&lt;/httpLogging&gt;
	/// </code>
	/// The configuration above defines to skip all requests for images and java script files and log GET requests only.
	/// The log messages will be written with "DEBUG" level, will contain cookies but no headers.
	/// <seealso cref="HttpLoggingModuleRuleCollection"/>.
	/// <seealso cref="HttpLoggingModuleRuleItem"/>.
	/// </summary>
	public class HttpLoggingModuleConfigurationSection
		: ConfigurationSection
	{
		static HttpLoggingModuleConfigurationSection settings = (HttpLoggingModuleConfigurationSection)ConfigurationManager.GetSection("httpLogging");

		/// <summary>Current (or default) settings from the configuration file.</summary>
		public static HttpLoggingModuleConfigurationSection Settings
		{
			get
			{
				return settings;
			}
		}

		/// <summary>When <c>true</c> - requests will be logged with "DEBUG" level, otherwise - with "INFO" level.</summary>
		[ConfigurationProperty("debug", DefaultValue = false, IsRequired = false)]
		public Boolean Debug
		{
			get
			{
				return (Boolean)this["debug"];
			}
			set
			{
				this["debug"] = value;
			}
		}

		/// <summary>When <c>true</c> request/response headers will be logged.</summary>
		[ConfigurationProperty("headers", DefaultValue = true, IsRequired = false)]
		public Boolean Headers
		{
			get
			{
				return (Boolean)this["headers"];
			}
			set
			{
				this["headers"] = value;
			}
		}

		/// <summary>When <c>true</c> request/response cookies will be logged.</summary>
		[ConfigurationProperty("cookies", DefaultValue = true, IsRequired = false)]
		public Boolean Cookies
		{
			get
			{
				return (Boolean)this["cookies"];
			}
			set
			{
				this["cookies"] = value;
			}
		}

		/// <summary>When <c>true</c> request form collection will be logged.</summary>
		[ConfigurationProperty("form", DefaultValue = true, IsRequired = false)]
		public Boolean Form
		{
			get
			{
				return (Boolean)this["form"];
			}
			set
			{
				this["form"] = value;
			}
		}

		/// <summary>Maximum HTTP header length (in characters) to log. Longer values will be trimmed.</summary>
		[ConfigurationProperty("maxHeaderLength", DefaultValue = 32, IsRequired = false)]
		[IntegerValidator]
		public Int32 MaxHeaderLength
		{
			get
			{
				return (Int32)this["maxHeaderLength"];
			}
			set
			{
				this["maxHeaderLength"] = value;
			}
		}

		/// <summary>Maximum HTTP cookie length (in characters) to log. Longer values will be trimmed.</summary>
		[ConfigurationProperty("maxCookieLength", DefaultValue = 32, IsRequired = false)]
		[IntegerValidator]
		public Int32 MaxCookieLength
		{
			get
			{
				return (Int32)this["maxCookieLength"];
			}
			set
			{
				this["maxCookieLength"] = value;
			}
		}

		/// <summary>Maximum HTTP form length (in characters) to log. Longer values will be trimmed.</summary>
		[ConfigurationProperty("maxFormLength", DefaultValue = 128, IsRequired = false)]
		[IntegerValidator]
		public Int32 MaxFormLength
		{
			get
			{
				return (Int32)this["maxFormLength"];
			}
			set
			{
				this["maxFormLength"] = value;
			}
		}
		/// <summary>Collection of rules to determine what requests must be logged.</summary>
		[ConfigurationProperty("rules", IsRequired = false, IsKey = false, IsDefaultCollection = false)]
		public HttpLoggingModuleRuleCollection Rules
		{
			get
			{
				return ((HttpLoggingModuleRuleCollection)(base["rules"]));
			}
			set
			{
				base["rules"] = value;
			}
		}  
	}
}
