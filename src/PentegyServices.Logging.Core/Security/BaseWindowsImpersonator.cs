using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace PentegyServices.Logging.Core.Security
{
	/// <summary>
	/// Impersonates a given Windows user using <see cref="IDisposable"/> pattern.
	/// Supports nested calls per thread (properly restores previous identity and <see cref="Thread.CurrentPrincipal"/>).	
	/// </summary>
	/// <remarks>	
	/// See http://support.microsoft.com/default.aspx?scid=kb;en-us;Q306158
	/// </remarks>
	public abstract class BaseWindowsImpersonator
		: IDisposable
	{
		#region Externals

		private const int Logon32LogonInteractive = 2;
		private const int Logon32ProviderDefault = 0;

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern int LogonUser(string lpszUserName, string lpszDomain, string lpszPassword, int dwLogonType,
		                                    int dwLogonProvider, ref IntPtr phToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int DuplicateToken(IntPtr hToken, int impersonationLevel, ref IntPtr hNewToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool RevertToSelf();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern bool CloseHandle(IntPtr handle);

		#endregion

		[ThreadStatic] private static Stack<IPrincipal> _impersonationChain; // keeps history of nested calls per thread
		[ThreadStatic] private static WindowsImpersonationContext _impersonationContext; // current impersonation

		/// <summary>
		/// Impersonates the user with the specified account credentials.
		/// </summary>
		/// <param name="userName">User name</param>
		/// <param name="domainName">Domain name (empty for local machine)</param>
		/// <param name="password">Password (clear text).</param>
		protected BaseWindowsImpersonator(String userName, String domainName, String password)
		{
			ImpersonateValidUser(userName, domainName, password);
		}

		/// <summary>
		/// Impersonates the user with specified <c>NetworkCredential</c>.
		/// </summary>
		/// <param name="accountCredentials">Account credentials</param>
		protected BaseWindowsImpersonator(NetworkCredential accountCredentials)
			: this(accountCredentials.UserName, accountCredentials.Domain, accountCredentials.Password)
		{ }

		private static Stack<IPrincipal> ImpersonationChain // initialization helper property
		{
			get
			{
				return _impersonationChain ?? (_impersonationChain = new Stack<IPrincipal>());
			}
		}

		#region IDisposable Members

		/// <summary>
		/// Reverts impersonation and restores previous context.
		/// </summary>
		public void Dispose()
		{
			UndoImpersonation();
		}

		#endregion

		private void ImpersonateValidUser(String userName, String domain, String password)
		{
			IntPtr token = IntPtr.Zero;
			IntPtr tokenDuplicate = IntPtr.Zero;

			try
			{
				if (RevertToSelf())
				{
					if (LogonUser(userName, domain, password, Logon32LogonInteractive, Logon32ProviderDefault, ref token) != 0)
					{
						if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
						{
							DoImpersonation(tokenDuplicate);
							return;
						}
					}
				}
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			finally
			{
				if (token != IntPtr.Zero)
				{
					CloseHandle(token);
				}
				if (tokenDuplicate != IntPtr.Zero)
				{
					CloseHandle(tokenDuplicate);
				}
			}
		}

		private void DoImpersonation(IntPtr token)
		{
			ImpersonationChain.Push(Thread.CurrentPrincipal);
			var identity = new WindowsIdentity(token);
			//Trace.TraceInformation("Thread.CurrentPrincipal: {0}. WindowsIdentity.GetCurrent(): {1}. Impersonating {2}...",
			//    Thread.CurrentPrincipal.Identity.Name, WindowsIdentity.GetCurrent().Name, identity.Name);

			_impersonationContext = identity.Impersonate();
			SetContext(CreatePrincipal(identity));

			Trace.Indent();
		}

		///<summary>
		/// Create principal by identity
		///</summary>
		///<param name="identity"></param>
		///<returns></returns>
		public abstract IPrincipal CreatePrincipal(IIdentity identity);

		///<summary>
		///</summary>
		///<param name="principal"></param>
		public virtual void SetContext(IPrincipal principal)
		{
			Thread.CurrentPrincipal = principal;
		}

		private void UndoImpersonation()
		{
			Trace.Unindent();
			//Trace.TraceInformation("Thread.CurrentPrincipal: {0}. WindowsIdentity.GetCurrent(): {1}.",
			//    Thread.CurrentPrincipal.Identity.Name, WindowsIdentity.GetCurrent().Name);

			if (_impersonationContext != null)
			{
				//Trace.TraceInformation("Undoing impersonation...");
				_impersonationContext.Undo();
				_impersonationContext = null;
			}

			if (ImpersonationChain.Count > 0)
			{
				IPrincipal prevPrincipal = ImpersonationChain.Pop();
				//Trace.TraceInformation("Previous Principal: {0}. WindowsIdentity.GetCurrent(): {1}.",
				//    prevPrincipal.Identity.Name, WindowsIdentity.GetCurrent().Name);
				if (prevPrincipal is WindowsPrincipal && ImpersonationChain.Count > 0)
				{
					//Trace.TraceInformation("Reimpersonating {0}...", prevPrincipal.Identity.Name);
					_impersonationContext = ((WindowsIdentity)prevPrincipal.Identity).Impersonate();
				}
				SetContext(prevPrincipal);
			}
			else
			{
				SetContext(CreatePrincipal(WindowsIdentity.GetCurrent()));
			}
		}
	}
}
