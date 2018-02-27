using NUnit.Framework;
using PentegyServices.Logging.Core.Diagnostics;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.Diagnostics
{
	[TestFixture]
	public class ThreadPoolMonitorTest
		: TestCaseBase
	{
		[Test]
		public void CreateCounters()
		{
			ThreadPoolMonitor.CreateCounters();
		}
	}
}
