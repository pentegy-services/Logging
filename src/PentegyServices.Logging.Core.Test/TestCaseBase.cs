using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PentegyServices.Logging.Core.Test
{
	/// <summary>
	/// Contains helper functionality common to most tests.
	/// </summary>
	public class TestCaseBase
	{
		static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>Shortcut for <see cref="Random"/> instance which is often required in tests</summary>
		protected static readonly Random Rnd = new Random();

		static TestCaseBase()
		{
			log4net.Config.XmlConfigurator.Configure();
			AppDomain.CurrentDomain.UnhandledException += (_, e) => logger.FatalFormat("AppDomain unhandled exception", e.ExceptionObject as Exception);
			// The following two lines aren't necessary, ServiceAppender will initialize MachineAddress (always)
			// and Application (if latter is defined in its config properties)
			//GlobalContext.Properties[LogProp.Application] = "TEST";
			//GlobalContext.Properties[LogProp.MachineAddress] = SysUtil.GetMachineName();
		}

		/// <summary>
		/// Spawns a set of <see cref="Thread"/> per each <see cref="Action"/> (using <see cref="ThreadPool"/>)
		/// in <paramref name="actions"/> then waits for them to terminate.
		/// </summary>
		/// <param name="actions">A collection of <see cref="Action"/> to execute.</param>
		protected static void SpawnAndWait(IEnumerable<Action> actions)
		{
			var list = actions.ToList();
			var handles = new ManualResetEvent[actions.Count()];
			for (var i = 0; i < list.Count; i++)
			{
				handles[i] = new ManualResetEvent(false);
				var currentAction = list[i];
				var currentHandle = handles[i];
				Action wrappedAction = () => { try { currentAction(); } finally { currentHandle.Set(); } };
				ThreadPool.QueueUserWorkItem(x => wrappedAction());
			}

			WaitHandle.WaitAll(handles);
		}

		/// <summary>
		/// Spawns the specified number of <see cref="Thread"/> for the action (using <see cref="ThreadPool"/>)
		/// then waits for them to terminate.
		/// </summary>
		/// <param name="action"><see cref="Action"/> to execute.</param>
		/// <param name="numberOfThreads">Number of <see cref="Thread"/> to spawn</param>
		protected static void SpawnAndWait(Action action, int numberOfThreads = 15)
		{
			SpawnAndWait(Enumerable.Repeat(action, numberOfThreads));
		}

		/// <summary>
		/// Often you need to compare two instances in your tests.
		/// Please organize them to minimize duplication by using inheritance/overloading.
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="copy"></param>
		protected virtual void Compare(object origin, object copy)
		{
			Assert.That(origin, Is.Not.Null, "'origin' cannot be null to compare");
			Assert.That(copy, Is.Not.Null, "'copy' cannot be null to compare");
		}
	}
}
