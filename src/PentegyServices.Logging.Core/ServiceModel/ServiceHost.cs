using log4net;
using System;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// Extends default <see cref="System.ServiceModel.ServiceHost"/> to apply specific namespaces endpoint bindings 
	/// and the service host (this happens in <see cref="OnOpened()"/>).
	/// This is to ensure we have proper namespaces for bindings (alternative is to define them directly in the configuration file - endpoint[bindingNamespace]).
	/// </summary>
	public class ServiceHost
		: System.ServiceModel.ServiceHost
	{
		static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceHost"/> class. 
		/// </summary>
		protected ServiceHost()
		{ }

		void ApplyNamespaces()
		{
			String bindingNamespace = GetBindingNamespace();
			if (!String.IsNullOrEmpty(bindingNamespace) && bindingNamespace.Trim().Length > 0)
			{
				logger.DebugFormat("Applying binding namespace '{0}' to {1} endpoints of {2}.", bindingNamespace, Description.Endpoints.Count, Description.ServiceType.FullName);
				foreach (var ep in Description.Endpoints)
				{
					ep.Binding.Namespace = bindingNamespace;
				}
			}

			String serviceNamespace = GetServiceNamespace();
			if (!String.IsNullOrEmpty(serviceNamespace))
			{
				logger.DebugFormat("Applying service namespace '{0}' to {1}.", serviceNamespace, Description.ServiceType.FullName);
				Description.Namespace = serviceNamespace;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceHost"/> class with the instance of the service and its base addresses specified.
		/// </summary>
		/// <param name="singletonInstance">The instance of the hosted service.</param>
		/// <param name="baseAddresses">An array of type <see cref="Uri"/> that contains the base addresses for the hosted service.</param>
		public ServiceHost(Object singletonInstance, params Uri[] baseAddresses)
			: base(singletonInstance, baseAddresses)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceHost"/> class with the type of service and its base addresses specified.
		/// </summary>
		/// <param name="serviceType">The type of hosted service.</param>
		/// <param name="baseAddresses">An array of type <see cref="Uri"/> that contains the base addresses for the hosted service.</param>
		public ServiceHost(Type serviceType, params Uri[] baseAddresses)
			: base(serviceType, baseAddresses)
		{ }

		/// <summary>
		/// Writes base address to the log before trying to open the host.
		/// </summary>
		protected override void OnOpening()
		{
			logger.InfoFormat("Opening host for {0} with {1} base addresses:{2}", 
				Description.ServiceType.FullName, BaseAddresses.Count, GetBasesAddressesInfo());
			base.OnOpening();
		}

		/// <summary>
		/// Writes service description along with endpoint information into the log.
		/// </summary>
		protected override void OnOpened()
		{
			ApplyNamespaces();
			base.OnOpened();
			logger.InfoFormat("Opened host for {0}:{1}{2}", Description.ServiceType.FullName, GetServiceInfo(), GetEndpointsInfo());
		}

		#region Diagnostics formatting

		String GetServiceInfo(Uri[] baseAddresses)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine("Name: " + Description.Name ?? "(null)");
			sb.AppendLine("Namespace: " + Description.Namespace ?? "(null)");
			sb.AppendLine("ConfigurationName: " + Description.ConfigurationName ?? "(null)");

			sb.AppendFormat("Service behaviors ({0}):", Description.Behaviors.Count);
			sb.AppendLine();
			foreach (var x in Description.Behaviors)
			{
				sb.AppendLine("\t" + x.GetType().FullName);
			}

			sb.AppendFormat("Base addresses ({0}):", baseAddresses != null ? baseAddresses.Length.ToString() : "none");
			sb.AppendLine();
			if (baseAddresses != null)
			{
				foreach (var x in baseAddresses)
				{
					sb.AppendLine("\t" + x);
				}
			}

			sb.AppendFormat("Endpoints ({0}):", Description.Endpoints.Count);
			sb.AppendLine();
			foreach (var ep in Description.Endpoints)
			{
				sb.AppendLine(GetEndpointInfo(ep));
			}

			return sb.ToString();
		}

		String GetServiceInfo()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine("Name: " + Description.Name ?? "(null)");
			sb.AppendLine("Namespace: " + Description.Namespace ?? "(null)");
			sb.AppendLine("ConfigurationName: " + Description.ConfigurationName ?? "(null)");

			sb.AppendFormat("Service behaviors applied ({0}):", Description.Behaviors.Count);
			sb.AppendLine();
			foreach (var x in Description.Behaviors)
			{
				sb.AppendLine("\t" + x.GetType().FullName);
			}

			return sb.ToString();
		}

		String GetBasesAddressesInfo()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine();
			sb.AppendLine();
			foreach (var address in BaseAddresses)
			{
				sb.AppendLine("\t" + address);
			}
			return sb.ToString();
		}

		String GetEndpointsInfo()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendFormat("Endpoints ({0}):", Description.Endpoints.Count);
			sb.AppendLine();
			foreach (var ep in Description.Endpoints)
			{
				sb.AppendLine(GetEndpointInfo(ep));
			}

			return sb.ToString();
		}

		String GetEndpointInfo(System.ServiceModel.Description.ServiceEndpoint ep)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("\tName: " + ep.Name);
			sb.AppendLine("\tAddress: " + ep.Address);
			if (ep.Address != null)
			{
				sb.AppendLine("\tAddress.IsAnonymous: " + ep.Address.IsAnonymous);
				sb.AppendLine("\tAddress.IsNone: " + ep.Address.IsNone);
				if (ep.Address.Identity != null && ep.Address.Identity.IdentityClaim != null)
				{
					System.IdentityModel.Claims.Claim claim = ep.Address.Identity.IdentityClaim;
					sb.AppendLine("\tAddress.Identity.IdentityClaim.ClaimType: " + claim.ClaimType);
					sb.AppendLine("\tAddress.Identity.IdentityClaim.Resource: " + claim.Resource);
					sb.AppendLine("\tAddress.Identity.IdentityClaim.Right: " + claim.Right);
				}
			}

			sb.AppendLine("\tListenUri: " + ep.ListenUri);
			sb.AppendLine("\tListenUriMode: " + ep.ListenUriMode);
			sb.AppendLine();
			sb.AppendLine("\tContract.Name: " + ep.Contract.Name);
			sb.AppendLine("\tContract.Namespace: " + ep.Contract.Namespace);
			sb.AppendLine("\tContract.ConfigurationName: " + ep.Contract.ConfigurationName);
			sb.AppendLine("\tContract.ProtectionLevel: " + ep.Contract.ProtectionLevel);
			sb.AppendLine();
			sb.AppendLine("\tBinding.Name: " + ep.Binding.Name);
			sb.AppendLine("\tBinding.Namespace: " + ep.Binding.Namespace);
			sb.AppendLine("\tBinding.MessageVersion: " + ep.Binding.MessageVersion);
			sb.AppendLine("\tBinding.Scheme: " + ep.Binding.Scheme);
			sb.AppendLine("\tBinding.OpenTimeout:\t" + ep.Binding.OpenTimeout);
			sb.AppendLine("\tBinding.CloseTimeout:\t" + ep.Binding.CloseTimeout);
			sb.AppendLine("\tBinding.SendTimeout:\t" + ep.Binding.SendTimeout);
			sb.AppendLine("\tBinding.ReceiveTimeout:\t" + ep.Binding.ReceiveTimeout);
			sb.AppendLine();

			sb.AppendFormat("\tEndpoint behaviors applied ({0}):", ep.Behaviors.Count);
			sb.AppendLine();
			foreach (var x in ep.Behaviors)
			{
				sb.AppendLine("\t\t" + x.GetType().FullName);
			}

			sb.AppendLine("\t---");
			return sb.ToString();
		}

		#endregion

		/// <summary>
		/// Returns namespace to be used as endpoints binding namespace.
		/// </summary>
		/// <returns>Namespace to be used. The default value is <see cref="Namespace.Binding"/>.</returns>
		protected virtual String GetBindingNamespace()
		{
			return Namespace.Binding;
		}

		/// <summary>
		/// Returns namespace to be used as service namespace (to apply to <see cref="System.ServiceModel.Description.ServiceDescription.Namespace"/> ).
		/// </summary>
		/// <returns>Namespace to be used. The default value is <see cref="Namespace.Service"/>.</returns>
		protected virtual String GetServiceNamespace()
		{
			return Namespace.Service;
		}
	}
}
