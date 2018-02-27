using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test
{
	[TestFixture]
	public class SysUtilTest
		: TestCaseBase
	{
		[Test, TestCaseSource("HexSamples_Valid")]
		public void ToStringHex(KeyValuePair<String, Byte[]> pair)
		{
			String hex = pair.Value.ToStringHex();
			Trace.TraceInformation("'{0}' converted to: '{1}'", BitConverter.ToString(pair.Value).Replace("-", ""), hex);
			StringAssert.AreEqualIgnoringCase(pair.Key, hex);
		}

		[Test]
		public void ToStringHex_Performance()
		{
			var data = new Byte[1024 * 1024 * 16];
			Rnd.NextBytes(data);

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			for (Int32 i = 0; i < 10; i++)
			{
				String str = data.ToStringHex();
			}
			stopwatch.Stop();
			Trace.TraceInformation("Finished in {0}ms", stopwatch.ElapsedMilliseconds);
		}

		[Test, ExpectedException(typeof(ArgumentNullException))]
		public void ToStringHex_Bad_Input()
		{
			Byte[] data = null;
			data.ToStringHex();
		}

		[Test, ExpectedException(typeof(ArgumentException)), TestCaseSource("HexSamples_Bad_Length")]
		public void ParseHex_Bad_Length(
			[Values()]
			String input)
		{
			input.ParseHex();
		}

		[Test, ExpectedException(typeof(FormatException)), TestCaseSource("HexSamples_Bad_Symbol")]
		public void ParseHex_Bad_Symbol(String input)
		{
			input.ParseHex();
		}

		[Test, TestCaseSource("HexSamples_Valid")]
		public void ParseHex(KeyValuePair<string, Byte[]> pair)
		{
			Byte[] result = pair.Key.ParseHex();
			Trace.TraceInformation("'{0}' parsed to: '{1}'", pair.Key, BitConverter.ToString(result).Replace("-", ""));
			Assert.AreEqual(pair.Value, result);
		}

		static String[] HexSamples_Bad_Length = new String[] { (String)null, "", "1", "000", "aaAAA" };
		static String[] HexSamples_Bad_Symbol = new String[] { "A@", "0g", "00-FF-11" };
		
		static IDictionary<String, Byte[]> HexSamples_Valid = new Dictionary<String, Byte[]>()
		{
			{"00", new Byte[] {0x00}},
			{"BAADF00D", new Byte[] {0xBA, 0xAD, 0xF0, 0x0D}},
			{"0001fF", new Byte[] {0x00, 0x01, 0xFF}},
			{"0123456789abcdef", new Byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}},
			{"0123456789ABCDEF", new Byte[] {0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF}}
		};
	}
}
