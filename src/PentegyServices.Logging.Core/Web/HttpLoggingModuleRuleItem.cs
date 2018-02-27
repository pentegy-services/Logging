using System;
using System.Configuration;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// Represents a configuration element of a single rule within <see cref="HttpLoggingModuleRuleCollection"/> for <see cref="HttpLoggingModule"/>.
	/// <seealso cref="HttpLoggingModuleRuleItem"/>.
	/// </summary>
	public class HttpLoggingModuleRuleItem
		: ConfigurationElement
	{
		/// <summary>Rule type defines if the rule allows or disallows logging.</summary>
		public LoggingRuleType Type { get; set; }

		/// <summary>
		/// Regular expression pattern for HTTP request method.
		/// Example: "POST|GET|DELETE".
		/// </summary>
		[ConfigurationProperty("method")]
		public String Method
		{
			get
			{
				return (String)base["method"];
			}
			set
			{
				base["method"] = value;
			}
		}

		/// <summary>
		/// Regular expression pattern for HTTP request url.
		/// Example: "/delete/\d+".
		/// </summary>
		[ConfigurationProperty("url")]
		public String Url
		{
			get
			{
				return (String)base["url"];
			}
			set
			{
				base["url"] = value;
			}
		}

		/// <summary>
		/// Regular expression pattern for HTTP request incoming address.
		/// Example: "10\.12\.0\.\d+".
		/// </summary>
		[ConfigurationProperty("ip")]
		public String IP
		{
			get
			{
				return (String)base["ip"];
			}
			set
			{
				base["ip"] = value;
			}
		}

		/// <summary>
		/// Regular expression pattern for HTTP response status code.
		/// Example: "20\d".
		/// </summary>
		[ConfigurationProperty("status")]
		public String Status
		{
			get
			{
				return (String)base["status"];
			}
			set
			{
				base["status"] = value;
			}
		}

		/// <summary>
		/// Regular expression pattern for HTTP response content type.
		/// Example: "html/text".
		/// </summary>
		[ConfigurationProperty("contentType")]
		public String ContentType
		{
			get
			{
				return (String)base["contentType"];
			}
			set
			{
				base["contentType"] = value;
			}
		}
	}  
}
