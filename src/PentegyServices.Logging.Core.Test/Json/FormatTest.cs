using NUnit.Framework;
using PentegyServices.Logging.Core.Json;
using System;
using System.Diagnostics;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.Json
{
	[TestFixture]
	public class FormatTest
	{
		[Test]
		public void FormatString_Performance(
			[Values(5000)]
			Int32 iterations,
			[Values(true, false)]
			Boolean indent
			)
		{
			var sample = new
			{
				Name = "Test",
				Age = 88,
				Child = new
				{
					Name = "Bobby",
					Age = 11
				}
			};

			var stopWatch = new Stopwatch();
			stopWatch.Start();
			String output = null;
			try
			{
				for (Int32 i = 0; i < iterations; i++)
				{
					output = Format.FormatString(sample, indent);
				}
			}
			finally
			{
				stopWatch.Stop();
				Trace.TraceInformation("Finished {0} iterations in {1}ms:", iterations, stopWatch.ElapsedMilliseconds);
				Trace.TraceInformation(output);
			}

		}
	}
}
