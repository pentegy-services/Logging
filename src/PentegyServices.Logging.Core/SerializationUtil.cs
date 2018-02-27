using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Xml;
using System.Xml.Serialization;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Provides a set of routines to serialize and deserialize objects using all known serialization methods.
	/// <c>String</c> is used as a serialization context wherever possible. Can be very useful in tests 
	/// to ensure that your classes can be (de)serialized with a specific serializer.
	/// </summary>
	public static class SerializationUtil
	{
		#region XmlSerializer

		/// <summary>
		/// Serializes a given object graph using <c>XmlSerializer</c>.
		/// </summary>
		/// <param name="graph">Object graph to serialize</param>
		/// <returns>Serialized content as <c>String</c></returns>
		public static String SerializeXml(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			XmlSerializer serializer = new XmlSerializer(graph.GetType());

			using (StringWriter sw = new StringWriter())
			{
				serializer.Serialize(sw, graph);
				return sw.ToString();
			}
		}

		/// <summary>
		/// Deserializes an object graph of type <typeparamref name="T"/> using <c>XmlSerializer</c>.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize</typeparam>
		/// <param name="content">Previously serialized content</param>
		/// <returns>Restored object graph</returns>
		public static T DeserializeXml<T>(String content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			XmlSerializer serializer = new XmlSerializer(typeof(T));

			using (StringReader sr = new StringReader(content))
			{
				return (T)serializer.Deserialize(sr);
			}
		}

		#endregion

		#region DataContractSerializer

		/// <summary>
		/// Serializes the given object graph using the specified instance of <c>DataContractSerializer</c>.
		/// </summary>
		/// <param name="serializer">The instance of <see cref="DataContractSerializer"/> to use.</param>
		/// <param name="graph">Object graph to serialize.</param>
		/// <returns>Serialized content as <c>String</c>.</returns>
		public static String SerializeDataContract(DataContractSerializer serializer, Object graph)
		{
			if (serializer == null)
			{
				throw new ArgumentNullException("serializer");
			}
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			using (StringWriter sw = new StringWriter())
			{
				using (XmlTextWriter xw = new XmlTextWriter(sw))
				{
					serializer.WriteObject(xw, graph);
				}
				return sw.ToString();
			}
		}

		/// <summary>
		/// Serializes a given object graph using <c>DataContractSerializer</c>.
		/// </summary>
		/// <param name="graph">Object graph to serialize.</param>
		/// <returns>Serialized content as <c>String</c>.</returns>
		public static String SerializeDataContract(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			return SerializeDataContract(new DataContractSerializer(graph.GetType()), graph);
		}

		/// <summary>
		/// Deserializes an object graph of type <typeparamref name="T"/> using <c>DataContractSerializer</c>.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize.</typeparam>
		/// <param name="serializer">The instance of <see cref="DataContractSerializer"/> to use.</param>
		/// <param name="content">Previously serialized content.</param>
		/// <returns>Restored object graph.</returns>
		public static T DeserializeDataContract<T>(DataContractSerializer serializer, String content)
		{
			if (serializer == null)
			{
				throw new ArgumentNullException("serializer");
			}
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			using (StringReader sr = new StringReader(content))
			{
				using (XmlTextReader xr = new XmlTextReader(sr))
				{
					return (T)serializer.ReadObject(xr);
				}
			}
		}
	
		/// <summary>
		/// Deserializes an object graph of type <typeparamref name="T"/> using <c>DataContractSerializer</c>.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize.</typeparam>
		/// <param name="content">Previously serialized content.</param>
		/// <returns>Restored object graph.</returns>
		public static T DeserializeDataContract<T>(String content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			return DeserializeDataContract<T>(new DataContractSerializer(typeof(T)), content);
		}

		#endregion

		#region NetDataContractSerializer

		/// <summary>
		/// Serializes a given object graph using <c>NetDataContractSerializer</c>.
		/// </summary>
		/// <param name="graph">Object graph to serialize</param>
		/// <returns>Serialized content as <c>String</c></returns>
		public static String SerializeNetDataContract(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			NetDataContractSerializer serializer = new NetDataContractSerializer();

			using (StringWriter sw = new StringWriter())
			{
				using (XmlTextWriter xw = new XmlTextWriter(sw))
				{
					serializer.WriteObject(xw, graph);
				}
				return sw.ToString();
			}
		}

		/// <summary>
		/// Deserializes an object graph of type <typeparamref name="T"/> using <c>NetDataContractSerializer</c>.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize</typeparam>
		/// <param name="content">Previously serialized content</param>
		/// <returns>Restored object graph</returns>
		public static T DeserializeNetDataContract<T>(String content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			NetDataContractSerializer serializer = new NetDataContractSerializer();

			using (StringReader sr = new StringReader(content))
			{
				using (XmlTextReader xr = new XmlTextReader(sr))
				{
					return (T)serializer.ReadObject(xr);
				}
			}
		}

		#endregion

		#region BinaryFormatter

		/// <summary>
		/// Serializes a given object graph using <c>BinaryFormatter</c>.
		/// </summary>
		/// <param name="graph">Object graph to serialize</param>
		/// <returns>Serialized content as an array of <c>Byte</c></returns>
		public static Byte[] SerializeBinary(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			BinaryFormatter formatter = new BinaryFormatter();

			using (MemoryStream stream = new MemoryStream())
			{
				formatter.Serialize(stream, graph);
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Deserializes an object graph of type <typeparamref name="T"/> using <c>BinaryFormatter</c>.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize</typeparam>
		/// <param name="content">Previously serialized content</param>
		/// <returns>Restored object graph</returns>
		public static T DeserializeBinary<T>(Byte[] content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			BinaryFormatter formatter = new BinaryFormatter();

			using (MemoryStream stream = new MemoryStream(content, true))
			{
				stream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(stream);
			}
		}

		#endregion

		#region SoapFormatter

		/// <summary>
		/// Serializes a given object graph using <c>SoapFormatter</c>.
		/// </summary>
		/// <param name="graph">Object graph to serialize</param>
		/// <returns>Serialized content as <c>String</c></returns>
		public static String SerializeSoap(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			SoapFormatter formatter = new SoapFormatter();

			using (MemoryStream stream = new MemoryStream())
			{
				formatter.Serialize(stream, graph);
				stream.Seek(0, SeekOrigin.Begin);
				// Although documentation does not state what encoding SoapFormatter uses
				// we know from reflector that internally it uses System.Text.UTF8Encoding.
				using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
				{
					return sr.ReadToEnd();
				}
			}
		}

		/// <summary>
		/// Deserializes an object graph of type <typeparamref name="T"/> using <c>SoapFormatter</c>.
		/// </summary>
		/// <typeparam name="T">Type of object to deserialize</typeparam>
		/// <param name="content">Previously serialized content</param>
		/// <returns>Restored object graph</returns>
		public static T DeserializeSoap<T>(String content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			SoapFormatter formatter = new SoapFormatter();

			using (MemoryStream stream = new MemoryStream())
			{
				using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8))
				{
					sw.Write(content);
					sw.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					return (T)formatter.Deserialize(stream);
				}
			}
		}

		#endregion

		#region LosFormatter

		/// <summary>Serializes a given object graph using <c>LosFormatter</c>.</summary>
		/// <param name="graph">Object graph to serialize</param>
		/// <returns>Serialized content as <c>String</c></returns>
		public static String SerializeLos(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			LosFormatter formatter = new LosFormatter();

			using (StringWriter sw = new StringWriter())
			{
				formatter.Serialize(sw, graph);
				return sw.ToString();
			}
		}

		/// <summary>Deserializes an object graph of type <typeparamref name="T"/> using <c>LosFormatter</c>.</summary>
		/// <typeparam name="T">Type of object to deserialize.</typeparam>
		/// <param name="content">Previously serialized content.</param>
		/// <returns>Restored object graph.</returns>
		public static T DeserializeLos<T>(String content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			LosFormatter formatter = new LosFormatter();

			using (StringReader sr = new StringReader(content))
			{
				return (T)formatter.Deserialize(sr);
			}
		}

		#endregion

		#region DataContractJsonSerializer

		/// <summary>Serializes a given object graph using <c>DataContractJsonSerializer</c>.
		/// <para>Note, the serializer outputs <see cref="DateTime"/> types like "Date(1351285200000+0300)" crap.</para>
		/// </summary>
		/// <param name="graph">Object graph to serialize.</param>
		/// <returns>Serialized content as <c>String</c>.</returns>
		public static String SerializeJson(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			var serializer = new DataContractJsonSerializer(graph.GetType());
			String result = null;
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, graph);
				result = Encoding.UTF8.GetString(stream.ToArray(), 0, (Int32)stream.Length);
			}
			return result;
		}

		/// <summary>Deserializes an object graph of type <typeparamref name="T"/> using <c>DataContractJsonSerializer</c>.</summary>
		/// <typeparam name="T">Type of object to deserialize</typeparam>
		/// <param name="content">Previously serialized content.</param>
		/// <returns>Restored object graph.</returns>
		public static T DeserializeJson<T>(String content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			var serializer = new DataContractJsonSerializer(typeof(T));
			T result = default(T);

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
			{
				result = (T)serializer.ReadObject(stream);
			}

			return result;
		}

		#endregion

		#region JavaScriptSerializer

		/// <summary>Serializes a given object graph using <c>JavaScriptSerializer</c>.
		/// <para>Note, the serializer outputs <see cref="DateTime"/> types like "Date(1351285200000+0300)" crap.</para>
		/// </summary>
		/// <param name="graph">Object graph to serialize.</param>
		/// <returns>Serialized content as <c>String</c>.</returns>
		public static String SerializeJavaScript(Object graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}

			var serializer = new JavaScriptSerializer();
			string result = serializer.Serialize(graph);
			return result;
		}

		/// <summary>Deserializes an object graph of type <typeparamref name="T"/> using <c>JavaScriptSerializer</c>.</summary>
		/// <typeparam name="T">Type of object to deserialize</typeparam>
		/// <param name="content">Previously serialized content.</param>
		/// <returns>Restored object graph.</returns>
		public static T DeserializeJavaScript<T>(String content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			var serializer = new JavaScriptSerializer();
			T result = serializer.Deserialize<T>(content);
			return result;
		}

		#endregion
	}
}
