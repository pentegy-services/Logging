using NUnit.Framework;
using PentegyServices.Logging.Core.Security;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test.Security
{
	[TestFixture]
	public class WinPrincipalTest
	{
		[Test]
		public void ConfigurationSection()
		{
			var settings = WinPrincipalConfigurationSection.Settings;
			Assert.IsNotNull(settings, "'winPrincipal' must be defined in app.config");
			Assert.IsNotNull(settings.Mapping, "mapping");
			CollectionAssert.AllItemsAreNotNull(settings.Mapping);
			CollectionAssert.AllItemsAreInstancesOfType(settings.Mapping, typeof(WinPrincipalMapItem));

			Assert.AreEqual("TEST", settings.Domain);
			Assert.AreEqual(5, settings.Mapping.Count);

			foreach (var map in settings.Mapping.OfType<WinPrincipalMapItem>())
			{
				Trace.TraceInformation("Role: '{0}'. Group: '{1}'.", map.Role, map.Group);
			}
		}

		[Test]
		public void ConfigurationInitialization()
		{
			Assert.AreEqual("TEST", WinPrincipal.DomainName);
			Assert.IsNotNull(WinPrincipal.GroupMap);

			foreach (var map in WinPrincipal.GroupMap)
			{
				Trace.TraceInformation("Role: '{0}'. Group: '{1}'.", map.Key, map.Value);
				if (!map.Value.Contains("\\"))
				{
					StringAssert.StartsWith(WinPrincipal.DomainName + "\\", map.Value, "Domain name must be prefixed");
				}
			}

			Assert.AreEqual(3, WinPrincipal.GroupMap.Count, "Empty entries must have been skipped");
			CollectionAssert.AllItemsAreUnique(WinPrincipal.GroupMap.Keys);
		}

		[Test]
		public void CurrentPrincipal()
		{
			WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
			var winPrincipal = new WinPrincipal(currentIdentity);
			Assert.AreSame(currentIdentity, winPrincipal.Identity);

			foreach (String role in winPrincipal.Roles)
			{
				Trace.TraceInformation(role);
			}

			var translatedAuthenticatedUsers = (new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null))
				.Translate(typeof(NTAccount)).Value;

			CollectionAssert.Contains(winPrincipal.Roles, translatedAuthenticatedUsers);
			CollectionAssert.Contains(winPrincipal.Roles, "U");
			Assert.IsTrue(winPrincipal.IsInRole(translatedAuthenticatedUsers));
			Assert.IsTrue(winPrincipal.IsInRole("U"));
		}
	}
}
