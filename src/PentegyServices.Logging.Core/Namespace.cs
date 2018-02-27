using System;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Namespaces to be used by core WCF services. The goal is to get rid of 'http://tempuri.org' 
	/// in WSDLs (not acceptable for production use).
	/// See <a href="http://www.pluralsight-training.net/community/blogs/kirillg/archive/2006/06/18/28380.aspx">this article</a> 
	/// for more details on WCF namespaces.
	/// </summary>
	public class Namespace
	{
		/// <summary>Base url for all namespaces.</summary>
		protected const String Base = "http://pentegyservices.com/logging";

		/// <summary>The target namespace of the root WSDL. Apply this using <see cref="System.ServiceModel.ServiceBehaviorAttribute"/> to the service implementation.</summary>
		public const String Service = Base;

		/// <summary>Contract (wsdl:port element) namespace. Apply this using <see cref="System.ServiceModel.ServiceContractAttribute"/> to the service contract.</summary>
		public const String ServiceContract = Base + "";

		/// <summary>Binding (wsdl:binding element) namespace. Add this value to 'endpoint' element's 'bindingNamespace' attribute in the service configuration.
		/// Alternatively, use the following snippet to set it in the code:
		/// <code>
		/// foreach (var ep in host.Description.Endpoints)
		///		ep.Binding.Namespace = Namespace.Binding;
		/// </code>
		/// </summary>
		public const String Binding = Base + "/binding";

		/// <summary>Schema (xsd) namespace where types/data contracts are defined. Apply this using <see cref="System.Runtime.Serialization.DataContractAttribute"/> to your structures.</summary>
		public const String DataContract = Base + "/types";
	}
}
