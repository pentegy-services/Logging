using Newtonsoft.Json;
using System;

namespace PentegyServices.Logging.Core.Json
{
	/// <summary>
	/// Extension to <see cref="JsonConverter"/> that trims byte arrays to specified length.
	/// <example><code>
	///   {"Data":/*First 5 bytes of total 4000*/"AAAAAAA="}
	/// </code></example>
	/// </summary>
	public class ByteArrayJsonConverter
		: JsonConverter
	{
		/// <summary>Default length limit is 256 bytes.</summary>
		public const Int32 DefaultLengthLimit = 256;

		readonly Int32 lengthLimit = DefaultLengthLimit;

		/// <summary>Constructs new instance of <see cref="ByteArrayJsonConverter"/>.</summary>
		public ByteArrayJsonConverter(Int32 lengthLimit = DefaultLengthLimit)
		{
			this.lengthLimit = lengthLimit;
		}

		/// <summary>
		/// Indicates if a type can be converted. Returns <c>true</c> for byte arrays.
		/// </summary>
		/// <param name="objectType"></param>
		/// <returns>Always <c>true</c> for byte arrays.</returns>
		public override Boolean CanConvert(Type objectType)
		{
			return objectType == typeof(Byte[]);
		}

		/// <summary>
		/// Throws <see cref="NotImplementedException"/> because trimming is one way transformation.
		/// </summary>
		public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs byte array trimming if its length exceeds the limit.
		/// </summary>
		public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			Byte[] data = (Byte[])value;
			if (data.Length > lengthLimit)
			{
				Byte[] copy = new Byte[lengthLimit];
				Buffer.BlockCopy(data, 0, copy, 0, lengthLimit);

				writer.WriteComment(String.Format("First {0} bytes of total {1}", lengthLimit, data.Length));
				writer.WriteValue(copy);
			}
			else
			{
				writer.WriteValue(data);
			}
		}
	}
}
