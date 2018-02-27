using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Allows to converts an arbitrary object graph Int32o xml representation using the following rules:
	/// <list type="bullet">
	///		<item><term>All public fields, all non-public fields with <see cref="DataMemberAttribute"/> applied are affected.</term></item>
	///		<item><term>All public properties, all non-public properties with <see cref="DataMemberAttribute"/> applied that have getters and are not indexers are affected.</term></item>
	///		<item><term>If any of the above has <see cref="MaskAttribute"/> applied the value will be masked.</term></item>
	///		<item><term><see cref="MaskAttribute"/> behavior is inherited, i.e. if applied to a collection of some class, any members affected will be masked even if they don't have explict <see cref="MaskAttribute"/> applied.</term></item>
	///		<item><term>Mask is always applied to the result of <see cref="Object.ToString()"/> or <see cref="IFormattable.ToString(string, IFormatProvider)"/> (if implemented).</term></item>
	///		<item><term>Element names in the output xml are escaped.</term></item>
	///		<item><term>Cycling graphs are detected.</term></item>
	/// </list>
	/// <para>
	/// The serializer respects <see cref="DepthLimit"/>, <see cref="SizeLimit"/>, <see cref="LengthLimit"/> adding 
	/// an XML comment when the corresponding limit is exceeded. The processing is then terminated.
	/// </para>
	/// You can use it for logging or debugging purposes, especially when you need masking. Note, the routine does not use
	/// any of standard .NET serializers so its output is not equivalent to, say, one from <see cref="DataContractSerializer"/>.
	/// Instead, it uses recursive tree traversal with reflection (so it maybe slow for time critical tasks).
	/// <seealso cref="MaskAttribute"/>	
	/// </summary>
	public class XSerializer
	{
		delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5); // this is only defined in .NET 4 
		delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6); // this is only defined in .NET 4 

		struct Result
		{
			public XElement Element;
			public Boolean Terminate;

			public override String ToString()
			{
				return Element != null ? Element.ToString() : "(null)";
			}
		}

		/// <summary>Default maximum allowed graph depth (stack size).</summary>
		public const Int32 DefaultDepthLimit = 16;
		/// <summary>Default maximum allowed number of objects in the graph.</summary>
		public const Int32 DefaultSizeLimit = 1000;
		/// <summary>Default maximum allowed graph text length.</summary>
		public const Int32 DefaultLengthLimit = Int32.MaxValue;

		/// <summary>Current maximum allowed graph depth (stack size) used by the serializer.</summary>
		public readonly Int32 DepthLimit;
		/// <summary>Current maximum allowed number of objects in the graph used by the serializer.</summary>
		public readonly Int32 SizeLimit;
		/// <summary>
		/// Current maximum allowed graph text length used by the serializer. Note, that this is not exact value because 
		/// the root element always has some length (so the value of, say 3, makes no sense). Or, the warning 
		/// comment has its own length too, so if the limit is, for example 1000 and the serializer stopped processing at 983,
		/// the comment added may increse it to ~1050-1300.
		/// </summary>
		public readonly Int32 LengthLimit;


		/// <summary>
		/// Constructs new instance of <see cref="XSerializer"/>.
		/// </summary>
		/// <param name="depthLimit">Maximum allowed graph depth. When omitted <see cref="DefaultDepthLimit"/> is used.</param>
		/// <param name="sizeLimit">Maximum allowed number of objects in the graph. When omitted <see cref="DefaultSizeLimit"/> is used.</param>
		/// <param name="lengthLimit">Maximum allowed graph text length. When omitted <see cref="DefaultLengthLimit"/> is used.</param>
		public XSerializer(
			Int32 depthLimit = DefaultDepthLimit,
			Int32 sizeLimit = DefaultSizeLimit,
			Int32 lengthLimit = DefaultLengthLimit
			) // length limit
		{
			if (depthLimit <= 0)
			{
				throw new ArgumentOutOfRangeException("depthLimit", "Must be non-negative.");
			}
			if (sizeLimit <= 0)
			{
				throw new ArgumentOutOfRangeException("sizeLimit", "Must be non-negative.");
			}
			if (lengthLimit <= 0)
			{
				throw new ArgumentOutOfRangeException("lengthLimit", "Must be non-negative.");
			}

			DepthLimit = depthLimit;
			SizeLimit = sizeLimit;
			LengthLimit = lengthLimit;
		}

		static XComment FormatGraphDepthLimitComment(Int32 limit, String elemPath, Object obj)
		{
			var result = new XComment(String.Format(" maximum graph depth exceeded {0}. Element '{1}' of type {2} ",
				limit, elemPath, obj != null ? obj.GetType().FullName : "(null)"));
			return result;
		}

		static XComment FormatGraphLengthLimitComment(Int32 limit, String elemPath, Object obj)
		{
			var result = new XComment(String.Format(" total output length exceeded {0} chars. Element '{1}' of type {2} ",
				limit, elemPath, obj != null ? obj.GetType().FullName : "(null)"));
			return result;
		}

		static XComment FormatGraphSizeLimitComment(Int32 limit, String elemPath, Object obj)
		{
			var result = new XComment(String.Format(" total graph size exceeded {0} object limit. Element '{1}' of type {2} ",
				limit, elemPath, obj != null ? obj.GetType().FullName : "(null)"));

			return result;
		}

		static String EscapeElementName(String elemName)
		{
			var sb = new StringBuilder(elemName);
			sb.Replace('.', '-');
			sb.Replace('+', '_');
			sb.Replace("[]", "Array");
			sb.Replace("`", "-"); // generic type args

			String name = sb.ToString();
			name = System.Xml.XmlConvert.EncodeName(name);
			return name;
		}

		/// <summary>
		/// Performs serialization of the graph specified.
		/// </summary>
		/// <param name="graph">Object graph to process.</param>
		/// <returns><see cref="XElement"/> instance that represents the given graph. Does not have any namespaces, just plain tree.</returns>
		public XElement Serialize(Object graph)
		{
			// reset limit counters
			size = 0;
			root = null;

			Result result = Serialize(graph, 0, "null", "", false);
			return result.Element;
		}

		Int32 size;  // accumulated size
		XElement root = null;
		BindingFlags publicFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
		BindingFlags nonPublicFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

		Result Serialize(Object obj,
			Int32 depth, // accumulated (depth
			String elemName,
			String elemPath, // accumulated element path (for diagnostics)
			Boolean stop,
			MaskAttribute parentMaskAttr = null)
		{
			var result = new Result
			{
				Element = new XElement(EscapeElementName(elemName))
			};
			if (root == null)
			{
				root = result.Element; // remember root to calculate total length later
			}
			if (obj == null)
			{
				return result;
			}
			Type type = obj.GetType();
			if (elemName == "null")
			{
				elemName = type.Name; // root non null
			}
			result.Element.Name = EscapeElementName(elemName);

			// returns <c>true</c> to indicate if the processing must be terminated
			Func<Object, Int32, String, String, Boolean, MaskAttribute, Boolean> addElementRecursive = (_graph, _depth, _elemName, _elemPath, _stop, _maskAttr) =>
			{
				if (depth >= DepthLimit)
				{
					result.Element.Add(FormatGraphDepthLimitComment(DepthLimit, _elemPath, _graph));
					return true;
				}

				size++;
				if (size >= SizeLimit)
				{
					result.Element.Add(FormatGraphSizeLimitComment(SizeLimit, _elemPath, _graph));
					return true;
				}

				Result innerResult = Serialize(_graph, _depth + 1, _elemName, _elemPath, _stop, _maskAttr); // recursive call

				result.Element.Add(innerResult.Element); // try to add to the result to have correct total length
				if (result.ToString().Length >= LengthLimit)
				{
					innerResult.Element.Remove(); // cannot afford it
					result.Element.Add(FormatGraphLengthLimitComment(LengthLimit, _elemPath, _graph));
					return true;
				}
				
				result.Terminate = innerResult.Terminate;
				return result.Terminate;
			};

			// process collections first
			if (!(obj is String) && obj is IEnumerable)
			{
				if (IsPrimitiveArray(obj))
				{
					ProcessArray((Array)obj, result.Element, parentMaskAttr);
				}
				else
				{
					foreach (var child in (IEnumerable)obj)
					{
						if (result.Terminate = addElementRecursive(child, depth, "item", elemPath + "/item", stop, parentMaskAttr))
						{
							break;
						}
					}
				}
				return result;
			}

			FieldInfo[] fields = GetFields(type);
			PropertyInfo[] props = GetProperties(type);
			Boolean noMembers = fields.Length == 0 && props.Length == 0;

			// then check if leaf, not leaf but no members, or need not to dig deeper (stop)
			Boolean leaf = !type.IsGenericType && (obj is String || obj is DateTime || type.IsPrimitive || type.IsEnum);
			if (stop || leaf || noMembers)
			{
				// ok, this is leaf, convert it to string
				String objText = null;
				try // ToString() may throw exceptions
				{
					objText = obj is IFormattable ? ((IFormattable)obj).ToString(null, CultureInfo.InvariantCulture) : obj.ToString();
				}
				catch (Exception ex) 
				{
					objText = "ToString[" + ex.GetType() + ": " + ex.Message + "]";
				}

				if (objText != null)
				{
					// sanitize special chars
					objText = XmlTextEncoder.Encode(objText);

					// apply mask if any
					if (parentMaskAttr != null)
					{
						objText = parentMaskAttr.Apply(objText);
					}
				}
				else
				{
					objText = "(null)";
				}

				result.Element.AddFirst(objText);
				Int32 overflow = LengthLimit - result.ToString().Length;

				if (overflow < 0)
				{
					Int32 newLength = objText.Length + overflow;
					if (newLength >= 0)
					{
						objText = objText.Substring(0, newLength);
						result.Element.Value = objText;
					}
					result.Element.Add(FormatGraphLengthLimitComment(LengthLimit, "/", obj));
				}

				if (stop)
				{
					result.Element.Add(new XComment(" loop detected "));
				}
				return result;
			}

			// now process all fields
			foreach (FieldInfo field in fields)
			{
				Object value = field.GetValue(obj);

				Boolean loop = (field.FieldType == type);

				var maskAttr = GetMaskAttr(parentMaskAttr, field);
				if (result.Terminate = addElementRecursive(value, depth, field.Name, elemPath + "/" + field.Name, loop, maskAttr))
				{
					return result; // terminate
				}
			}

			// finally, process all properties
			foreach (PropertyInfo prop in props)
			{
				Object value = null;
				try // getters may throw exceptions
				{
					value = ((PropertyInfo)prop).GetValue(obj, null);
				}
				catch (TargetInvocationException ex)
				{
					Exception innerEx = ex.InnerException;
					value = innerEx != null ? "Get[" + ex.InnerException.GetType() + ": " + ex.InnerException.Message + "]" : ex.Message;
				}

				var maskAttr = GetMaskAttr(parentMaskAttr, prop);
				Boolean loop = prop.PropertyType == type;
				; // singleton or direct loop
				if (result.Terminate = addElementRecursive(value, depth, prop.Name, elemPath + "/" + prop.Name, loop, maskAttr))
				{
					return result; // terminate
				}
			}

			return result;
		}

		static MaskAttribute GetMaskAttr(MaskAttribute parentMaskAttr, MemberInfo member)
		{
			var maskAttr = (MaskAttribute)member.GetCustomAttributes(typeof(MaskAttribute), true).FirstOrDefault() ?? parentMaskAttr;
			return maskAttr;
		}

		PropertyInfo[] GetProperties(Type type)
		{
			PropertyInfo[] result = type.GetProperties(publicFlags | BindingFlags.GetProperty) // all public properties with getters
				.Concat( // plus all non-public properties with DataMember applied
					type.GetProperties(nonPublicFlags | BindingFlags.GetProperty).Where(x => Attribute.IsDefined(x, typeof(DataMemberAttribute), true)))
				.Distinct()
				.Where(p => !p.GetIndexParameters().Any()) // we don't need any indexers
				.ToArray();
			return result;
		}

		FieldInfo[] GetFields(Type type)
		{
			FieldInfo[] result = type.GetFields(publicFlags) // all public fields
				.Concat( // plus all non-public with DataMember applied
					type.GetFields(nonPublicFlags).Where(x => Attribute.IsDefined(x, typeof(DataMemberAttribute), true)))
				.Distinct()
				.ToArray();
			return result;
		}

		Boolean IsPrimitiveArray(Object obj)
		{
			Array array = obj as Array;
			Boolean result = array != null
				&& array.Rank == 1 // one dimensional only
				&& array.GetType().GetElementType().IsPrimitive; // only Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, Single.
			
			// note, Decimal is not primitive type, for example
			
			return result;
		}

		void ProcessArray(Array array, XElement result, MaskAttribute mask)
		{
			var sb = new StringBuilder();
			
			for (Int32 i = 0; i < array.Length -1; i++)
			{
				Object value = array.GetValue(i);

				if (mask == null)
				{
					if (value is Byte)
					{
						sb.AppendFormat("{0:X2}", value);
						continue; // no separator
					}
					else if (value is Char)
					{
						sb.AppendFormat("'{0}'", value);
					}
					else
					{
						sb.Append(value.ToString()); // ToString() on primitive types never fails
					}
				}
				else
				{
					sb.AppendFormat("*"); // no partial or full masking here to save space
				}

				if (i < array.Length - 1)
				{
					sb.Append(',');
				}
			}
			result.Value = sb.ToString();
		}
	}
}
