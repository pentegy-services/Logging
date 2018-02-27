using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;

namespace PentegyServices.Logging.Core.Json
{
	/// <summary>
	/// Extension to <see cref="DefaultContractResolver"/> that attaches <see cref="MaskJsonConverter"/> to properties marked with <see cref="MaskAttribute"/>.
	/// </summary>
	public class MaskContractResolver
		: DefaultContractResolver
	{
		/// <summary>
		/// Overrides the method to attach <see cref="MaskJsonConverter"/> to <see cref="JsonProperty"/>.
		/// </summary>
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			JsonProperty property = base.CreateProperty(member, memberSerialization);

			MaskAttribute attr = member.GetCustomAttributes(typeof(MaskAttribute), false).Cast<MaskAttribute>().FirstOrDefault();
			if (attr != null)
			{
				property.Converter = new MaskJsonConverter(attr);
			}

			return property;
		}
	}
}
