using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using NUnit.Framework;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test
{
	[TestFixture]
	public class XSerializerTest
		: TestCaseBase
	{
		protected class PersonName
		{
			[DataMember]
			public String FirstName { get; set; }

			[Mask]
			public String LastName { get; set; }

			[DataMember]
			internal String MiddleName;

#pragma warning disable 0169 // is never used
			private String Prefix; // should not be processed as does not have [DataMember] and is not public
#pragma warning restore 0169
		}

		protected class Person
		{
			[DataMember][Mask]
			public PersonName Name { get; set; } // [Mask] here must apply recursively to all public properties of PersonName, or private ones with [DataContract] explicitly added

			[Mask]
			public String Password { get; set; }
		}

		class Card
		{
			[Mask(Partial = true)]
			public String CardNumber;

			[DataMember][Mask(Partial = true)]
			public String[] AssociatedAccounts { get; set; }

			[DataMember][Mask]
			protected internal String CVV { get; set; }
		}

		protected class Node
		{
			public Node Linked;

			[DataMember]
			public String Name;

			public static String StaticName = "stat-base";
		}

		class NodeB
		{
			public NodeA A = null;
		}

		class NodeA
		{
			public NodeB B = null;

			[Mask]
			public Int32[] MArray = new Int32[] { 1, 2, 3, 4, 5, 6, 7 };

			[Mask]
			public List<Boolean> BList = new List<Boolean>() { true, false };
		}


		public class BadSingleton
		{
			static BadSingleton inner = new BadSingleton();

			public static BadSingleton Current
			{
				get
				{
					return inner;
				}
			} // statics must not be processed
			
			public BadSingleton New
			{
				get
				{
					return new BadSingleton();
				}
			}
		}

		public struct BadSingletonStruct
		{
			static BadSingletonStruct inner = new BadSingletonStruct { Name = "cool" };

			public static BadSingletonStruct Current { get { return inner; } } // statics must not be processed

			public String Name;
		}

		protected class NewNode
			: Node
		{
			public NewNode()
			{
				Name = "new";
				base.Name = "base";
			}

			public new String Name;

			public new static String StaticName = "stat-new"; // statics must not be processed
		}

		protected class BadGetterNode
		{
			public string Name
			{
				get
				{
					throw new InvalidOperationException("Shit happens");
				}
			}
		}

		protected struct BadStruct
		{
			public override String ToString()
			{
				throw new InvalidOperationException("Shit happens");
			}
		}

		protected struct SNode<T>
		{
			public T ID;

			[Mask]
			public String Name;
		}

		public struct BNode
		{
			[DataMember]
			public String Name { get; set; }
		}

		[Flags]
		enum TestEnum
			: Byte
		{
			First = 1,
			Second = 8
		}

		class CNode
		{
			public TestEnum A = TestEnum.First | TestEnum.Second;
			public TestEnum B = TestEnum.Second;
		}

		[Test, TestCaseSource("Samples")]
		public void RegularGraph(Object graph, String expectedString)
		{
			XElement xml = graph.ToXml();
			String xmlString = xml.ToString();
			Trace.TraceInformation("Instance of {1}:{0}{2}", Environment.NewLine, graph != null ? graph.GetType().Name : "(null)", xmlString);

			expectedString = XElement.Parse(expectedString).ToString(); // parse and convert back to string to eliminate difference in space characters
			Assert.AreEqual(expectedString, xmlString);
		}

		[Test]
		public void Cycled_Directly() 
		{
			var node1 = new Node() { Name = "1" };
			var node2 = new Node() { Name = "2" };
			node1.Linked = node2;
			node2.Linked = node1;

			var graph = node1;
			XElement xml = graph.ToXml();
			String xmlString = xml.ToString();
			Trace.TraceInformation(xmlString);

			String expectedString = "<Node><Linked>PentegyServices.Logging.Core.Test.XSerializerTest+Node<!-- loop detected --></Linked><Name>1</Name></Node>";
			expectedString = XElement.Parse(expectedString).ToString(); // parse and convert back to string to eliminate difference in space characters

			Assert.AreEqual(expectedString, xmlString);
		}

		[Test]
		public void Depth_Limit_In_Cycled_Indirectly(
			[Values(1, 4, 12, 50)]
			Int32 limit
			) // not part of RegularGraph test because of non-trivial graph construction
		{
			var nodeA = new NodeA();
			var nodeB = new NodeB();
			nodeA.B = nodeB;
			nodeB.A = nodeA;

			var graph = nodeA;
			XElement xml = graph.ToXml(limit);
			Trace.TraceInformation(xml.ToString());

			XComment comment = xml.DescendantNodes().OfType<XComment>().Single();
			String commentMessage = String.Format(" maximum graph depth exceeded {0}.", limit);
			StringAssert.StartsWith(commentMessage, comment.Value);
		}

		[Test]
		public void Exception_In_Getter()
		{
			var graph = new BadGetterNode();
			XElement xml = graph.ToXml(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
			Trace.TraceInformation(xml.ToString());

			XElement item = xml.Descendants().First();
			Assert.AreEqual("Get[System.InvalidOperationException: Shit happens]", item.Value);
		}

		[Test]
		public void Exception_In_ToString()
		{
			var graph = new[] { new BadStruct(), new BadStruct() };
			XElement xml = graph.ToXml(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
			Trace.TraceInformation(xml.ToString());

			foreach (var item in xml.Descendants())
			{
				Assert.AreEqual("ToString[System.InvalidOperationException: Shit happens]", item.Value);
			}
		}

		const Int32 MinLengthLimit = 24;

		void ValidateSizeLimit(Object graph, Int32 sizeLimit)
		{
			XElement xml = graph.ToXml(Int32.MaxValue, Int32.MaxValue, sizeLimit);

			Int32 Objects = xml.DescendantNodes().OfType<XElement>().Count() + 1;
			Trace.TraceInformation("Got {0} elements with max {1} allowed:\n\r{2}", Objects, sizeLimit, xml);
			Assert.LessOrEqual(Objects, sizeLimit);
		}

		void ValidateLengthLimit(Object graph, Int32 lengthLimit)
		{
			XElement xml = graph.ToXml(Int32.MaxValue, Int32.MaxValue, lengthLimit);

			String xmlString = xml.ToString();
			Trace.TraceInformation("Got {0} chars with max {1} allowed:\n\r{2}", xmlString.Length, lengthLimit, xmlString);

			String commentMessage = String.Format(" total output length exceeded {0} chars.", lengthLimit);

			// ensure there is the comment
			XComment comment = xml.DescendantNodes().OfType<XComment>().Single();
			StringAssert.StartsWith(commentMessage, comment.Value);

			// remove it to get "clean" length
			comment.Remove();
			String strippedXmlString = xml.ToString();
			Trace.TraceInformation("Removed the comment ({0}):\n\r{1}", strippedXmlString.Length, strippedXmlString);

			Assert.LessOrEqual(strippedXmlString.Length - MinLengthLimit, lengthLimit);
		}

		[Test]
		public void Size_Limit_Array(
			[Values(1, 5, 50)]
			Int32 sizeLimit
			)
		{
			String[] graph = Enumerable.Repeat("test-item", sizeLimit).ToArray();

			ValidateSizeLimit(graph, sizeLimit);
		}

		[Test]
		public void Size_Limit_LinkedList(
			[Values(1, 5, 50)]
			Int32 sizeLimit
			)
		{
			var graph = new Node { Name = "ROOOOOOOT" };

			Node b = graph;
			for (Int32 i = 0; i < sizeLimit * 2; i++)
			{
				b.Linked = new Node { Name = "Very long string to increase the total length" + Rnd.Next() };
				b = b.Linked;
			}

			ValidateSizeLimit(graph, sizeLimit);
		}


		[Test]
		public void Length_Limit_String(
			[Values(1, 5, 50, 1000)]
			Int32 maxLength
			)
		{
			String graph = "0".PadRight(maxLength, '1'); // +1 to the limit
			ValidateLengthLimit(graph, maxLength);
		}

		[Test]
		public void Length_Limit_Collection(
			[Values(1, 5, 50, 79, 244, 1000)]
			Int32 maxLength
			)
		{
			String[] graph = Enumerable.Repeat("test-item", 100).ToArray(); // some large graph
			ValidateLengthLimit(graph, maxLength);
		}

		[Test, TestCaseSource("ComplexSamples")]
		public void ComplexGraph(Object graph) // test on real complex graphs like system singletons, delegates
		{
			XElement xml = graph.ToXml();
			Trace.TraceInformation(xml.ToString());
		}

		static Object[] ComplexSamples =
		{
			System.ServiceModel.Channels.MessageVersion.Default,
			System.AppDomain.CurrentDomain,
			System.Threading.Thread.CurrentThread,
			System.Threading.Thread.CurrentContext,
			System.Console.Out,
			new System.CrossAppDomainDelegate(() => {}),
			(Func<Int32, Int32>)(x => x*3), "d",
			typeof(string),
		};

		static Object[] Samples =
		{
			new Object[] { (String)null, "<null />" },
			new Object[] { "", "<String></String>" },
			new Object[] { "Hello world", "<String>Hello world</String>" },

			new Object[] { new Object(), "<Object>System.Object</Object>" },
			new Object[] { Int32.MinValue, "<Int32>-2147483648</Int32>" },
			new Object[] { (Int32?)null, "<null />" },
			new Object[] { decimal.MinValue, "<Decimal>-79228162514264337593543950335</Decimal>" },
			new Object[] { new DateTime(1889, 4, 20), "<DateTime>04/20/1889 00:00:00</DateTime>" },

			new Object[] { new Boolean?[] {true, (Boolean?)null }, "<Nullable-1Array><item>True</item><item /></Nullable-1Array>" },
			new Object[] { new byte[] {0x01, 0xAA, 0x00, 0x77, 0x8F, 0x3E }, "<ByteArray>01AA00778F</ByteArray>"},
			new Object[] { new byte[] {0x01, 0xAA, 0x00, 0x77, 0x8F, 0x3E }, "<ByteArray>01AA00778F</ByteArray>"},
			new Object[] { new short[] {0xFF, 0xAA, 0x00, 0x77, 0x8F, 0x3E }, "<Int16Array>255,170,0,119,143,</Int16Array>"},
			new Object[] { new char[] {'a', 'Ж', ',', '+' }, "<CharArray>'a','Ж',',',</CharArray>"},
			new Object[] { new NodeA(), "<NodeA><B /><MArray>*,*,*,*,*,*,</MArray><BList><item>****</item><item>****</item></BList></NodeA>"},
			new Object[] { new Dictionary<Int32, Boolean>() {{1, true}}, "<Dictionary-2><item><Key>1</Key><Value>True</Value></item></Dictionary-2>" },
			new Object[] { new Dictionary<Int32, Node>() {{1, new Node{Name = "name"}}}, "<Dictionary-2><item><Key>1</Key><Value><Linked/><Name>name</Name></Value></item></Dictionary-2>" },
			new Object[] { new List<SNode<Int32>>() { new SNode<Int32> { ID = 14, Name = "test" } }, "<List-1><item><ID>14</ID><Name>****</Name></item></List-1>"},
			new Object[] { new List<BNode>() { new BNode { Name = "test" } }, "<List-1><item><Name>test</Name></item></List-1>"},
			new Object[] { new LinkedList<Int32>( new Int32[] { 1,2 } ), "<LinkedList-1><item>1</item><item>2</item></LinkedList-1>"},

			new Object[] { new PersonName() { FirstName = "John", LastName = "Doe", MiddleName = "D."}, "<PersonName><MiddleName>D.</MiddleName><FirstName>John</FirstName><LastName>****</LastName></PersonName>" },
			new Object[] { new Person() { Password = "P@ssw0rd!", Name = new PersonName() { FirstName = "John", LastName = "Doe", MiddleName = "D."} }, "<Person><Name><MiddleName>****</MiddleName><FirstName>****</FirstName><LastName>****</LastName></Name><Password>****</Password></Person>" },
			new Object[] { new Card() { CardNumber = "10018889992222", CVV = "726", AssociatedAccounts = new String[] { "26250000111199", "26250000222288" }}, "<Card><CardNumber>1001******2222</CardNumber><AssociatedAccounts><item>2625******1199</item><item>2625******2288</item></AssociatedAccounts><CVV>****</CVV></Card>" },

			new Object[] { TestEnum.First, "<TestEnum>First</TestEnum>" },
			new Object[] { new CNode(), "<CNode><A>First, Second</A><B>Second</B></CNode>" },
		};
	}
}

