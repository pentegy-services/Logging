using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace PentegyServices.Logging.Core.Json
{
	/// <summary>
	/// The example of JSON.NET usage.
	/// </summary>
	public static class Format
	{
		/// <summary>
		/// Converts the given object to JSON-like string. Intended to be used in <see cref="object.ToString()"/> overloads
		/// for visual object representation and logging purposes only. The resulting string is not valid JSON and cannot be deserialized.
		/// <example><code>
		/// {
		///   Type: 'Core.Security.WinPrincipal',
		///   Identity: {
		///     Type: 'System.Security.Principal.WindowsIdentity',
		///     Name: 'dm6\\Sergey',
		///     IsAuthenticated: true,
		///     AuthenticationType: 'NTLM',
		///     WindowsIdentity: {
		///       Type: 'System.Security.Principal.WindowsIdentity',
		///       ImpersonationLevel: 'None'
		///     }
		///   }
		/// }
		/// </code></example>
		/// </summary>
		/// <param name="graph">An object to convert. Note, cycled graphs are not persisted.</param>
		/// <param name="indent"><c>true</c> to indent the output, <c>false</c> to not (default).</param>
		/// <returns>JSON-like string.</returns>
		public static string FormatString(this Object graph, Boolean indent = false)
		{
			JsonSerializerSettings settings = indent ? settingsIndent : settingsNoIndent;
			JsonSerializer jsonSerializer = JsonSerializer.Create(settings);

			StringBuilder sb = new StringBuilder(256);
			StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);
			using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
			{
				jsonWriter.Formatting = settings.Formatting;
				jsonWriter.QuoteName = false;
				jsonWriter.QuoteChar = '\'';
				jsonSerializer.Serialize(jsonWriter, graph);
			}

			String json = sw.ToString();
			return json;
		}

		static JsonSerializerSettings settingsIndent = new JsonSerializerSettings
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // no need to process cycled graphs
			Formatting = Formatting.Indented,
			Converters = new JsonConverter[] {
				new StringEnumConverter(), // output enum member names, instead of values
				new ByteArrayJsonConverter(), // trim large buffers
			},
			ContractResolver = new MaskContractResolver() // will attach MaskJsonConverter
		};

		static JsonSerializerSettings settingsNoIndent = new JsonSerializerSettings
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // no need to process cycled graphs
			Formatting = Formatting.None,
			Converters = new JsonConverter[] {
				new StringEnumConverter(), // output enum member names, instead of values
				new ByteArrayJsonConverter(), // trim large buffers
			},
			ContractResolver = new MaskContractResolver() // will attach MaskJsonConverter
		};
	}
}
