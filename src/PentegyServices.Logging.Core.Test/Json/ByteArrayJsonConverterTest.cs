using Newtonsoft.Json;
using NUnit.Framework;
using PentegyServices.Logging.Core.Json;
using System;
using System.Diagnostics;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.Json
{
	[TestFixture]
	public class ByteArrayJsonConverterTest
	{
		class Sample
		{
			public Byte[] Data = new Byte[4000];
		}

		[Test]
		public void JsonSerialize()
		{
			var sample = new Sample();

			String json = JsonConvert.SerializeObject(sample, new ByteArrayJsonConverter(5) );
			Trace.TraceInformation(json);
			Assert.AreEqual("{\"Data\":/*First 5 bytes of total 4000*/\"AAAAAAA=\"}", json);
		}
	}
}
