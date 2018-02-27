using log4net;
using log4net.Appender;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Transactions;
using System.Web;
using System.Xml.Linq;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Contains global system-related utilities.
	/// </summary>
	public static class SysUtil
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Returns name of the current machine by probing first DNS then NETBIOS.
		/// </summary>
		/// <returns>Name of the current machine or "unknown" if it's cannot be obtained for some reason.</returns>
		public static String GetMachineName()
		{
			String machineName = "unknown";
			try // to get DNS name first
			{
				machineName = Dns.GetHostName();
			}
			catch (Exception ex)
			{
				logger.WarnFormat("Cannot obtain DNS name: {0}.\nTrying Environment.MachineName...", ex);
				try // to get NETBIOS name (is it always uppercased?)
				{
					machineName = Environment.MachineName;
				}
				catch (Exception ex2)
				{
					logger.WarnFormat("Cannot obtain machine name: {0}", ex2);
				}
			}
			return machineName;
		}

		/// <summary>
		/// Returns the application assembly which is different for different application types and hosting options.
		/// The snippet is from <a href="http://stackoverflow.com/questions/756031/using-the-web-application-version-number-from-an-assembly-asp-net-c">here</a>.
		/// </summary>
		/// <returns><see cref="Assembly"/> that represents the current application.</returns>
		public static Assembly GetApplicationAssembly()
		{
			const String AspNetNamespace = "ASP";

			// Try the EntryAssembly, this doesn't work for ASP.NET classic pipeline (untested on integrated)
			var assembly = Assembly.GetEntryAssembly();

			var ctx = HttpContext.Current; // Look for web application assembly
			if (ctx != null)
			{
				IHttpHandler handler = ctx.CurrentHandler;
				if (handler != null)
				{

					Type type = handler.GetType();
					while (type != null && type != typeof(Object) && type.Namespace == AspNetNamespace)
					{
						type = type.BaseType;
					}

					assembly = type.Assembly;
				}
			}
        
			return assembly ?? Assembly.GetExecutingAssembly(); // Fallback to executing assembly
		}

		/// <summary>
		/// Creates a new <see cref="TransactionScope"/> instance with non-default <see cref="TransactionOptions"/>
		/// that is considered harmful. See <a href="http://blogs.msdn.com/b/dbrowne/archive/2010/05/21/using-new-transactionscope-considered-harmful.aspx">this article</a>
		/// for more details.
		/// </summary>
		/// <param name="isolationLevel"><see cref="IsolationLevel"/> to use. The default is <see cref="IsolationLevel.ReadCommitted"/>.</param>
		/// <param name="transactionScopeOption"><see cref="TransactionScopeOption"/> to use. The default is <see cref="TransactionScopeOption.Required"/>.</param>
		/// <returns><see cref="TransactionScope"/> instance with isolation level set to <paramref name="isolationLevel"/> in its <see cref="TransactionOptions"/>.</returns>
		public static TransactionScope CreateTransactionScope(
			IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
			TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required)
		{
			var transactionOptions = new TransactionOptions
			{
				IsolationLevel = isolationLevel,
				Timeout = TransactionManager.MaximumTimeout // this will use machine.config value
			};
			if (Transaction.Current != null)
			{
				// there is an ambient transaction
				// the scope we're about to return will most likely be used as an inner transaction and we'll get "System.ArgumentOutOfRangeException: Time-out interval must be less than 2^32-2"
				transactionOptions.Timeout.Add(-new TimeSpan(0, 0, 1));
				// Note: looks like you cannot have 2 connections to the same DB in the scope getting System.Transactions.TransactionException: The operation is not valid for the state of the transaction. :(
			}
			return new TransactionScope(transactionScopeOption, transactionOptions);
		}


		/// <summary>
		/// Performs safe disposal of <see cref="ICommunicationObject"/> instances.
		/// WCF has invalid <see cref="IDisposable"/> implementation for client proxies.
		/// See <a href="http://geekswithblogs.net/DavidBarrett/archive/2007/11/22/117058.aspx">http://geekswithblogs.net/DavidBarrett/archive/2007/11/22/117058.aspx</a>
		/// for more information.
		/// </summary>
		/// <param name="commObj"><see cref="ICommunicationObject"/> instance to dispose.</param>
		/// <returns>Any <see cref="Exception"/> that might occured during disposing. Can be used for logging purposes.</returns>
		public static Exception Dispose(this ICommunicationObject commObj)
		{
			Exception result = null;
			if (commObj != null)
			{
				try
				{
					if (commObj.State == CommunicationState.Opened)
					{
						try
						{
							commObj.Close();
						}
						catch (Exception ex)
						{
							Trace.TraceError("Cannot close {0}: {1}", commObj.GetType().Name, ex);
							commObj.Abort();
						}
					}
					else
					{
						commObj.Abort();
					}
					((IDisposable)commObj).Dispose();
				}
				catch (Exception ex)
				{
					Trace.TraceError("Cannot dispose {0}: {1}", commObj.GetType().Name, ex);
					result = ex;
				}
			}
			return result;
		}

		/// <summary>
		/// Converts an arbitrary object graph into xml representation using the following rules:
		/// <list type="bullet">
		///		<item><term>All public fields or non-public fields with <see cref="DataMemberAttribute"/> applied are affected.</term></item>
		///		<item><term>All public properties or non-public properties with <see cref="DataMemberAttribute"/> applied that have getters and are not indexers are affected.</term></item>
		///		<item><term>If any of the above has <see cref="MaskAttribute"/> applied the value will be masked.</term></item>
		///		<item><term><see cref="MaskAttribute"/> behavior is inherited, i.e. if applied to a collection of some class, any members affected will be masked even if they don't have explict <see cref="MaskAttribute"/> applied.</term></item>
		///		<item><term>Mask is always applied to the result of <see cref="Object.ToString()"/>.</term></item>
		///		<item><term>Element names in the output xml are escaped.</term></item>
		///		<item><term>Cycling graphs are detected (<see cref="InvalidOperationException"/> is thrown).</term></item>
		///		<item><term>Value types are considered leafs, sorry (please override <see cref="object.ToString()"/>).</term></item>
		/// </list>
		/// You can use it for logging or debugging purposes, especially when you need masking. Note, the routine does not use
		/// any of standard .NET serializers so its output is not equivalent to, say, one from <see cref="DataContractSerializer"/>.
		/// Instead, it uses recursive tree traversal with reflection (so it maybe slow for time critical tasks).
		/// <seealso cref="MaskAttribute"/>
		/// </summary>
		/// <param name="obj">An object graph to process.</param>
		/// <param name="depthLimit">Maximum graph depth allowed.</param>
		/// <param name="sizeLimit">Maximum number of objects in the.</param>
		/// <param name="lengthLimit">Maximum graph text length allowed.</param>
		/// <returns><see cref="XElement"/> instance that represents the given graph. Does not have any namespaces, just plain tree.</returns>
		public static XElement ToXml(this Object obj,
			Int32 depthLimit = XSerializer.DefaultDepthLimit,
			Int32 sizeLimit = XSerializer.DefaultSizeLimit,
			Int32 lengthLimit = XSerializer.DefaultLengthLimit
			)
		{
			var serializer = new XSerializer(depthLimit, sizeLimit, lengthLimit);
			XElement result = serializer.Serialize(obj);
			return result;
		}

		/// <summary>
		/// Returns various system information about the current process.
		/// To be used for easier problem diagnosis.
		/// </summary>
		/// <param name="calculateCollections"><c>true</c> to calculate collection content size (default is <c>false</c>).</param>
		/// <returns>System information as string dictionary with keys organized in namespace like manner.</returns>
		public static IDictionary<String, String> GatherSystemInfo(Boolean calculateCollections = false)
		{
			var result = new Dictionary<String, String>();

			Func<Func<String>, String> safeGetValue = valueProvider =>
			{
				try
				{
					return valueProvider();
				}
				catch (Exception ex)
				{
					return ex.GetType() + ": " + ex.Message;
				}
			};
			Action<String, Func<String>> safeAddValue = (key, valueProvider) => { result.Add(key, safeGetValue(valueProvider)); };
			Action<String, String> addValue = (key, value) => { result.Add(key, value ?? "(null)"); };
			Func<IEnumerable, String> calculateCollectionSize = items =>
			{
				if (!calculateCollections)
				{
					return "disabled!";
				}

				if (items == null)
				{
					return "null";
				}

				return String.Join(", ", items.Cast<Object>().Select(x =>
				{
					String size;
					try
					{
						size = SerializationUtil.SerializeBinary(x).Length.ToString();
					}
					catch (Exception ex)
					{
						size = ex.Message;
					}
					return String.Format("{0}: {1}", x.GetType().FullName, size);
					}).ToArray());
				};

			// Process
			Process process = Process.GetCurrentProcess();
			safeAddValue("Process.ProcessName", () => process.ProcessName);
			safeAddValue("Process.ProcessorAffinity", () => process.ProcessorAffinity.ToString());
			safeAddValue("Process.PriorityClass", () => process.PriorityClass.ToString());
			safeAddValue("Process.BasePriority", () => process.BasePriority.ToString());
			safeAddValue("Process.MachineName", () => process.MachineName.ToString());
			safeAddValue("Process.Handle", () => process.Handle.ToString());
			safeAddValue("Process.HandleCount", () => process.HandleCount.ToString());
			safeAddValue("Process.UserProcessorTime.TotalSeconds", () => process.UserProcessorTime.TotalSeconds.ToString());

			safeAddValue("Process.NonpagedSystemMemorySize64", () => process.NonpagedSystemMemorySize64.ToString());
			safeAddValue("Process.PagedMemorySize64", () => process.PagedMemorySize64.ToString());
			safeAddValue("Process.PagedSystemMemorySize64", () => process.PagedSystemMemorySize64.ToString());
			safeAddValue("Process.PeakPagedMemorySize64", () => process.PeakPagedMemorySize64.ToString());
			safeAddValue("Process.PeakVirtualMemorySize64", () => process.PeakVirtualMemorySize64.ToString());
			safeAddValue("Process.PeakWorkingSet64", () => process.PeakWorkingSet64.ToString());
			safeAddValue("Process.PrivateMemorySize64", () => process.PrivateMemorySize64.ToString());
			safeAddValue("Process.VirtualMemorySize64", () => process.VirtualMemorySize64.ToString());
			safeAddValue("Process.WorkingSet64", () => process.WorkingSet64.ToString());

			// AppDomain
			AppDomain domain = AppDomain.CurrentDomain;
			addValue("AppDomain.CurrentDomain.Id", domain.Id.ToString());
			addValue("AppDomain.CurrentDomain.FriendlyName", domain.FriendlyName);
			addValue("AppDomain.CurrentDomain.BaseDirectory", domain.BaseDirectory);
#if NET4
			addValue("AppDomain.CurrentDomain.IsFullyTrusted", domain.IsFullyTrusted.ToString());
			safeAddValue("AppDomain.MonitoringIsEnabled", () => AppDomain.MonitoringIsEnabled.ToString());
			safeAddValue("AppDomain.MonitoringSurvivedProcessMemorySize", () => AppDomain.MonitoringSurvivedProcessMemorySize.ToString());
#endif
			safeAddValue("AppDomain.CurrentDomain.SetupInformation.ApplicationTrust", () => domain.SetupInformation.ApplicationTrust.ToString());
			safeAddValue("AppDomain.CurrentDomain.SetupInformation.CachePath", () => domain.SetupInformation.CachePath);
			safeAddValue("AppDomain.CurrentDomain.SetupInformation.ConfigurationFile", () => domain.SetupInformation.ConfigurationFile);
			safeAddValue("AppDomain.CurrentDomain.SetupInformation.ShadowCopyDirectories", () => domain.SetupInformation.ShadowCopyDirectories);
			safeAddValue("AppDomain.CurrentDomain.SetupInformation.ShadowCopyFiles", () => domain.SetupInformation.ShadowCopyFiles);

			// Assembly.GetCallingAssembly()
			Assembly assembly = Assembly.GetCallingAssembly();
			if (assembly != null)
			{
				addValue("Assembly.GetCallingAssembly.CodeBase", assembly.CodeBase);
				if (assembly.EntryPoint != null)
				{
					safeAddValue("Assembly.GetCallingAssembly.EntryPoint.Name", () => assembly.EntryPoint.Name);
				}
				addValue("Assembly.GetCallingAssembly.FullName", assembly.FullName);
				addValue("Assembly.GetCallingAssembly.Location", assembly.Location);
			}
			else
			{
				addValue("Assembly.GetCallingAssembly()", "null");
			}

			// Assembly.GetEntryAssembly()
			assembly = Assembly.GetEntryAssembly();
			if (assembly != null)
			{
				addValue("Assembly.GetEntryAssembly.CodeBase", assembly.CodeBase);
				if (assembly.EntryPoint != null)
				{
					safeAddValue("Assembly.GetEntryAssembly.EntryPoint.Name", () => assembly.EntryPoint.Name);
				}
				addValue("Assembly.GetEntryAssembly.FullName", assembly.FullName);
				addValue("Assembly.GetEntryAssembly.Location", assembly.Location);
			}
			else addValue("Assembly.GetEntryAssembly()", "null");

			// Assembly.GetExecutingAssembly()
			assembly = Assembly.GetExecutingAssembly();
			if (assembly != null)
			{
				addValue("Assembly.GetExecutingAssembly.CodeBase", assembly.CodeBase);
				if (assembly.EntryPoint != null)
				{
					safeAddValue("Assembly.GetExecutingAssembly.EntryPoint.Name", () => assembly.EntryPoint.Name);
				}
				addValue("Assembly.GetExecutingAssembly.FullName", assembly.FullName);
				addValue("Assembly.GetExecutingAssembly.Location", assembly.Location);
			}
			else
			{
				addValue("Assembly.GetExecutingAssembly()", "null");
			}

			// Threadpool
			Int32 workerThreads;
			Int32 completionPortThreads;
			ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
			addValue("ThreadPool.GetMinThreads.workerThreads", workerThreads.ToString());
			addValue("ThreadPool.GetMinThreads.completionPortThreads", completionPortThreads.ToString());
			ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
			addValue("ThreadPool.GetMaxThreads.workerThreads", workerThreads.ToString());
			addValue("ThreadPool.GetMaxThreads.completionPortThreads", completionPortThreads.ToString());

			// Environment
			safeAddValue("Environment.CurrentDirectory", () => Environment.CurrentDirectory);
			safeAddValue("Environment.MachineName", () => Environment.MachineName);
			safeAddValue("Environment.OSVersion", () => Environment.OSVersion.ToString());
			safeAddValue("Environment.UserDomainName", () => Environment.UserDomainName);
			addValue("Environment.CommandLine", Environment.CommandLine);
#if NET4
			addValue("Environment.Is64BitOperatingSystem", Environment.Is64BitOperatingSystem.ToString());
			addValue("Environment.Is64BitProcess", Environment.Is64BitProcess.ToString());
#endif
			addValue("Environment.ProcessorCount", Environment.ProcessorCount.ToString());
			addValue("Environment.SystemDirectory", Environment.SystemDirectory);
			addValue("Environment.UserInteractive", Environment.UserInteractive.ToString());
			addValue("Environment.UserName", Environment.UserName);
			addValue("Environment.Version", Environment.Version.ToString());

			// Environment variables
			IDictionary variables = Environment.GetEnvironmentVariables();
			foreach (var key in variables.Keys)
			{
				safeAddValue("Environment.Variables." + key, () => variables[key].ToString());
			}

			// System.IO.Path
			addValue("Path.GetTempPath", System.IO.Path.GetTempPath());
			addValue("Path.DirectorySeparatorChar", System.IO.Path.DirectorySeparatorChar.ToString());
			addValue("Path.AltDirectorySeparatorChar", System.IO.Path.AltDirectorySeparatorChar.ToString());
			addValue("Path.PathSeparator", System.IO.Path.PathSeparator.ToString());
			addValue("Path.VolumeSeparatorChar", System.IO.Path.VolumeSeparatorChar.ToString());

			// WindowsIdentity
			safeAddValue("WindowsIdentity.GetCurrent.Name", () => WindowsIdentity.GetCurrent().Name);
			safeAddValue("WindowsIdentity.GetCurrent.ImpersonationLevel", () => WindowsIdentity.GetCurrent().ImpersonationLevel.ToString());
			safeAddValue("WindowsIdentity.GetCurrent.IsSystem", () => WindowsIdentity.GetCurrent().IsSystem.ToString());
			safeAddValue("WindowsIdentity.GetCurrent.AuthenticationType", () => WindowsIdentity.GetCurrent().AuthenticationType);

			// HttpContext
			HttpContext http = HttpContext.Current;
			if (http != null)
			{
				addValue("HttpContext.Current.Application.Count", http.Application.Count.ToString());
				addValue("HttpContext.Current.Application.Contents.Count", http.Application.Contents.Count.ToString());
				addValue("HttpContext.Current.Application.Keys", String.Join(", ", http.Application.Keys.Cast<String>().ToArray()));
				addValue("HttpContext.Current.Application", calculateCollectionSize(http.Application));

				addValue("HttpContext.Current.Cache.Count", http.Cache.Count.ToString());
				addValue("HttpContext.Current.Cache.EffectivePercentagePhysicalMemoryLimit", http.Cache.EffectivePercentagePhysicalMemoryLimit.ToString());
				addValue("HttpContext.Current.Cache.EffectivePrivateBytesLimit", http.Cache.EffectivePrivateBytesLimit.ToString());
				addValue("HttpContext.Current.Cache", calculateCollectionSize(http.Cache));

				safeAddValue("HttpContext.Current.User.Identity.AuthenticationType", () => http.User.Identity.AuthenticationType.ToString());
				safeAddValue("HttpContext.Current.User.Identity.IsAuthenticated", () => http.User.Identity.IsAuthenticated.ToString());
				safeAddValue("HttpContext.Current.User.Identity.Name", () => http.User.Identity.Name.ToString());

				addValue("HttpContext.Current.IsCustomErrorEnabled", http.IsCustomErrorEnabled.ToString());
				addValue("HttpContext.Current.IsDebuggingEnabled", http.IsDebuggingEnabled.ToString());
				safeAddValue("HttpContext.Current.IsPostNotification", () => http.IsPostNotification.ToString());

				safeAddValue("HttpContext.Current.Server.MachineName", () => http.Server.MachineName.ToString());
				safeAddValue("HttpContext.Current.Server.ScriptTimeout", () => http.Server.ScriptTimeout.ToString());

				HttpApplication app = http.ApplicationInstance;
				if (app != null)
				{
					safeAddValue("HttpContext.Current.ApplicationInstance.Modules", () => String.Join(",", app.Modules.AllKeys));
				}
				else
				{
					addValue("HttpContext.Current.ApplicationInstance", "null");
				}
			}
			else
			{
				addValue("HttpContext.Current", "null");
			}

			return result;
		}

		/// <summary>
		/// Returns various system information about the current process.
		/// To be used for easier problem diagnosis.
		/// </summary>
		/// <returns></returns>
		public static String GetSystemInfo()
		{
			IDictionary<String, String> info = GatherSystemInfo();
			var sb = new StringBuilder();
			foreach (var param in info)
			{
				sb.AppendFormat("{0}: {1}", param.Key, param.Value);
				sb.AppendLine();
			}
			return sb.ToString();
		}

		/// <summary>
		/// Flashes all buffering appenders in log4net default repository. This is useful to ensure all log messages are written, for example, 
		/// within <see cref="AppDomain.UnhandledException"/>.
		/// </summary>
		public static void FlushLogBuffers()
		{
			Trace.TraceInformation("Flushing log4net buffered appenders...");
			BufferingAppenderSkeleton[] bufferedAppenders = LogManager.GetRepository().GetAppenders().OfType<BufferingAppenderSkeleton>().ToArray();
			try
			{
				foreach (var appender in bufferedAppenders)
				{
					appender.Flush();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("{0}: {1}", ex.GetType().FullName, ex.Message);
			}
		}

		/// <summary>
		/// A helper over <see cref="BitConverter.ToString(Byte[])"/> that removes '-' separators.
		/// Note, it's very inefficient on large buffers.
		/// <seealso cref="ParseHex(String)"/>
		/// </summary>
		/// <param name="data">Byte array to convert into hexadecimal string.</param>
		/// <returns>Converted string (for example, "04D7A945B").</returns>
		public static String ToStringHex(this Byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException();
			}
			//string result = BitConverter.ToString(data).Replace("-", ""); // 5862ms
			//return result;
			const String Hex = "0123456789ABCDEF";

			var sb = new StringBuilder(2 * data.Length);
			for (Int32 i = 0; i < data.Length; i++)
			{
				Byte b = data[i];
				sb.Append(Hex[b >> 4]);
				sb.Append(Hex[b & 0x0F]); //4455ms
				//sb.Append(data[i].ToString("X2")); // 19178ms
			}
			String result = sb.ToString(); 
			return result;
		}

		/// <summary>
		/// Function opposite to <see cref="BitConverter.ToString(Byte[])"/> is not in BCL.
		/// <seealso cref="ToStringHex(Byte[])"/>
		/// </summary>
		/// <param name="hex">Hexadecimal string (for example, "04D7A945B"). Letters can be in any case.</param>
		/// <returns>Byte array corresponding to the source string.</returns>
		public static Byte[] ParseHex(this String hex)
		{
			if (String.IsNullOrEmpty(hex))
			{
				throw new ArgumentException("Cannot be null or empty.");
			}
			if ((hex.Length % 2) != 0)
			{
				throw new ArgumentException("The length must be even.");
			}

			const String alphabet = "0123456789ABCDEF";

			hex = hex.ToUpperInvariant();
			Byte[] data = new Byte[hex.Length >> 1];
			for (Int32 i = 0; i < data.Length; i++)
			{
				Int32 hi = alphabet.IndexOf(hex[i << 1]);
				Int32 lo = alphabet.IndexOf(hex[i << 1 | 0x01]);

				if (hi < 0 || lo < 0)
				{
					throw new FormatException();
				}

				data[i] = (Byte)(hi << 4 | lo);
			}
			return data;
		}

		/// <summary>
		/// Figure out if string value contains digits
		/// </summary>
		/// <param name="val">string value, e.g. "2345110"</param>
		/// <returns>True if all symbols are digit</returns>
		public static Boolean IsNumber(String val)
		{
			return !String.IsNullOrEmpty(val) && val.Length > 0 && val.All(c => Char.IsDigit(c));
		}
	}
}
