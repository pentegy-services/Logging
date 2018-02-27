using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace PentegyServices.Logging.Core.Security
{
	/// <summary>
	/// Extends standard <see cref="WindowsPrincipal"/> with ability to map Windows groups to logical roles
	/// according to the mapping defined in the configuration file (see <see cref="WinPrincipalConfigurationSection"/>).
	/// This can be useful in intranet scenarios when your code uses role based security and relies on <see cref="System.Security.Permissions.PrincipalPermission"/>.
	/// 
	/// <para>Warning! Indirect group inclusion is not supported. For example, if you have Windows group 'A' 
	/// and this group is a member of group 'B' and there is a mapping defined between 'B' and one of your logical roles,
	/// users of 'A' will not get those roles.</para>
	/// </summary>
	public class WinPrincipal 
		: WindowsPrincipal
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		static String domainName = null;
		static Dictionary<String, String> groupMap = null;

		/// <summary>Combined original Windows groups plus mapped logical roles for the identity.</summary>
		public readonly IList<String> Roles;

		/// <summary>
		/// Refined Windows group &lt;-&gt; logical role mapping (from <see cref="WinPrincipalMapCollection"/>).
		/// </summary>
		protected internal static IDictionary<String, String> GroupMap 
		{
			get
			{
				if (groupMap == null)
				{
					if (WinPrincipalConfigurationSection.Settings != null && WinPrincipalConfigurationSection.Settings.Mapping != null)
					{
						groupMap = WinPrincipalConfigurationSection.Settings.Mapping
							.OfType<WinPrincipalMapItem>()
							.Where(x => x != null)
							.Where(x => !String.IsNullOrEmpty(x.Role) &&
								(!String.IsNullOrEmpty(x.Group) || !String.IsNullOrEmpty(x.SID)))
							.Select(x => new {
								x.Role,
								Group = !String.IsNullOrEmpty(x.SID) ?
									ResolveSidtoName(x.SID) : (String.IsNullOrEmpty(DomainName) || x.Group.Contains("\\") ? x.Group : DomainName + "\\" + x.Group),
							})
							.GroupBy(x => x.Role)
							.ToDictionary(x => x.Key, x => x.First().Group);
					}

					if (groupMap == null)
					{
						groupMap = new Dictionary<String, String>();
						logger.Info("No mapping defined in the configuration file.");
					}
					else
					{
						String mapping = String
							.Join(Environment.NewLine, groupMap.Select(x => String.Format("\t'{0}' -> '{1}'.", x.Key, x.Value))
							.ToArray());
						logger.InfoFormat("Domain to use: {0}. BackEndRole to windows group mapping:\r\n {1}", DomainName, mapping);
					}
				}
				return groupMap;
			}
		}

		/// <summary>
		/// Domain name to use (from <see cref="WinPrincipalConfigurationSection.Domain"/>).
		/// </summary>
		public static String DomainName
		{
			get
			{
				if (domainName == null)
				{
					if (WinPrincipalConfigurationSection.Settings != null)
					{
						domainName = WinPrincipalConfigurationSection.Settings.Domain;
					}

					// Determine domain name to use
					if (String.IsNullOrEmpty(domainName))
					{
						try
						{
							domainName = Environment.MachineName;
						}
						catch (InvalidOperationException ex)
						{
							domainName = "."; // Nothing we can do
							logger.Warn("Cannot determine host name. Domain name is defaulted to '.'", ex);
						}
					}
				}
				return domainName;
			}
		}

		/// <summary>
		/// Constructs the instance and populates <see cref="Roles"/> collection.
		/// </summary>
		/// <param name="ntIdentity">The <see cref="WindowsIdentity"/> object from which to construct the new instance of <see cref="WinPrincipal"/>.</param>
		public WinPrincipal(WindowsIdentity ntIdentity) 
			: base(ntIdentity)
		{
			String[] windowsGroups = ntIdentity.Groups
				.Translate(typeof(NTAccount))
				.Select(x => x.Value)
				.ToArray();

			// Map back-end roles from windows groups
			String[] roles = GroupMap
				.Where(x => windowsGroups.Contains(x.Value, StringComparer.OrdinalIgnoreCase))
				.Select(x => x.Key)
				.ToArray();

			Trace.TraceInformation("Account {0}({1}/'{2}'): {3} mapped to {4}.", 
				ntIdentity.Name, ntIdentity.IsAuthenticated, ntIdentity.AuthenticationType,
				String.Join(", ", windowsGroups), String.Join(", ", roles));

			Roles = windowsGroups.Concat(roles).ToList().AsReadOnly();
		}

		/// <summary>
		/// Overrides default <see cref="WindowsPrincipal.IsInRole(String)"/> by first looking into <see cref="Roles"/> collection 
		/// and then calling the base implementation (logical OR).
		/// </summary>
		/// <param name="role">The name of the role for which to check membership.</param>
		/// <returns><c>true</c> if the current principal is a member of the specified role; otherwise, <c>false</c>.</returns>
		public override Boolean IsInRole(String role)
		{
			return Roles.Contains(role) || base.IsInRole(role);
		}

		protected static String ResolveSidtoName(String sid)
		{
			try
			{
				return (new SecurityIdentifier(sid)).Translate(typeof(NTAccount)).Value;
			}
			catch (Exception)
			{
				return sid;
			}
		}
	}
}
