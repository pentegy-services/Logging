using Newtonsoft.Json;
using NUnit.Framework;
using PentegyServices.Logging.Core.Json;
using System;
using System.Diagnostics;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.Json
{
	[TestFixture]
	public class MaskJsonConverterTest
	{
		class Sample
		{
			[Mask(Partial = false)]
			public string Name1;
			[Mask(Partial = true)]
			public string Name2;

			public string Name3;
		}

		[Test]
		public void JsonSerialize()
		{
			var sample = new Sample { Name1 = "Hello,world", Name2 = "Hello,world", Name3 = "Hello,world" };

			var settings = new JsonSerializerSettings
			{
				ContractResolver = new MaskContractResolver()
			};

			String json = JsonConvert.SerializeObject(sample, settings);
			Trace.TraceInformation(json);
			Assert.AreEqual("{\"Name1\":\"****\",\"Name2\":\"Hell***orld\",\"Name3\":\"Hello,world\"}", json);
		}
	}
}
