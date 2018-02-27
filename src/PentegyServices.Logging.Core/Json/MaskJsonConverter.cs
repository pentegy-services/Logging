using System;
using Newtonsoft.Json;

namespace PentegyServices.Logging.Core.Json
{
	/// <summary>
	/// Extension to <see cref="JsonConverter"/> that supports <see cref="MaskAttribute"/>.
	/// <example><code>
	///   {"Name1":"****","Name2":"Hell***orld"}
	/// </code></example>
	/// </summary>
	public class MaskJsonConverter
		: JsonConverter
	{
		/// <summary>Flag indicating whether to perform partial or full masking.</summary>
		readonly MaskAttribute maskAttribute;

		/// <summary>Constructs new instance of <see cref="MaskJsonConverter"/>.</summary>
		/// <param name="maskAttribute"><see cref="MaskAttribute"/> instance to use when converting.</param>
		public MaskJsonConverter(MaskAttribute maskAttribute)
		{
			this.maskAttribute = maskAttribute;
		}

		/// <summary>
		/// Indicates if a type can be converted. Always returns <c>true</c> as the convertor relies on <see cref="Object.ToString()"/>.
		/// </summary>
		/// <param name="objectType"></param>
		/// <returns>Always <c>true</c>.</returns>
		public override Boolean CanConvert(Type objectType)
		{
			return true;
		}

		/// <summary>
		/// Throws <see cref="NotImplementedException"/> because masking is one way transformation.
		/// </summary>
		public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs masking of the value.
		/// </summary>
		public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			String masked = maskAttribute.Apply(value.ToString());

			writer.WriteValue(masked);
		}
	}
}
