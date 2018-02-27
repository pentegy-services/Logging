using NUnit.Framework;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace PentegyServices.Logging.Core.Test
{
	/// <summary>
	/// Allows to host arbitrary WCF service using 'net.tcp' protocol.
	/// </summary>
	/// <typeparam name="I">Service interface.</typeparam>
	/// <typeparam name="T">Concrete implementaion of <typeparamref name="I"/> interface to host. Must have default ctor.</typeparam>
	public abstract class ServiceTestCaseBase<I, T>
		: TestCaseBase where T : I
	{
		/// <summary>Address of the endpoint used to host the test service.</summary>
		protected static readonly String EndpointAddress = "net.tcp://localhost:9394/Test/" + typeof(I).Name; // move to app.config for better flexibility?

		/// <summary>Host of the test service.</summary>
		protected ServiceHost serviceHost;
		/// <summary>Client channel factory of the test service.</summary>
		protected ChannelFactory<I> factory;
		/// <summary>Client proxy of the test service.</summary>
		protected I service;

		/// <summary>
		/// Creates the test service instance, its host and the client channel factory.
		/// </summary>
		[TestFixtureSetUp]
		public void InitFixture()
		{
			Trace.TraceInformation("InitFixture");
			var binding = CreateBinding();
			// host the test service
			serviceHost = CreateServiceHost();
			serviceHost.AddServiceEndpoint(typeof(I), binding, EndpointAddress);
			ApplyServiceBehavior(serviceHost);
			serviceHost.Open();

			factory = new ChannelFactory<I>(binding, EndpointAddress); // create client proxy factory
			factory.Endpoint.Name = typeof(I).Name;
			ApplyClientBehavior(factory);
		}

		/// <summary>
		/// Closes the service host and client channel factory created in <see cref="InitFixture()"/>.
		/// </summary>
		[TestFixtureTearDown]
		public virtual void DownFixture()
		{
			Trace.TraceInformation("DownFixture");
			((ICommunicationObject)factory).Dispose();
			serviceHost.Close();
		}

		/// <summary>
		/// Creates a new client proxy from <see cref="factory"/>.
		/// </summary>
		[SetUp]
		public virtual void Init()
		{
			Trace.TraceInformation("Init");
			service = factory.CreateChannel(); // create client proxy
		}

		/// <summary>
		/// Closes the client proxy created in <see cref="Init()"/>.
		/// </summary>
		[TearDown]
		public virtual void Down()
		{
			Trace.TraceInformation("Down");
			((ICommunicationObject)service).Dispose();
		}

		/// <summary>
		/// Instantiates the service for the test. 
		/// By default it uses <see cref="Activator.CreateInstance{T}()"/> but
		/// you can override which is useful if your service has dependencies.
		/// </summary>
		/// <returns>The service instane to host.</returns>
		protected virtual T CreateService()
		{
			Trace.TraceInformation("CreateService");
			return Activator.CreateInstance<T>();
		}

		/// <summary>
		/// Instantiates the test service and its host (<see cref="Core.ServiceModel.ServiceHost"/>).
		/// </summary>
		/// <returns><see cref="ServiceHost"/> instance constructed but not yet opened.</returns>
		protected virtual ServiceHost CreateServiceHost()
		{
			Trace.TraceInformation("CreateServiceHost");
			return new Core.ServiceModel.ServiceHost(CreateService());
		}

		/// <summary>Allows to create a custom binding for the test service and the client factory.</summary>
		/// <returns></returns>
		protected virtual NetTcpBinding CreateBinding()
		{
			Trace.TraceInformation("CreateBinding");
			return new NetTcpBinding();
		}

		/// <summary>Allows descendant tests to apply any service behaviors.</summary>
		/// <param name="serviceHost"><see cref="ServiceHost"/> instance to apply behaviors to.</param>
		protected virtual void ApplyServiceBehavior(ServiceHost serviceHost)
		{
			Trace.TraceInformation("ApplyServiceBehavior");
			// enable detailed exceptions
			var debugBehavior = serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
			if (debugBehavior == null)
			{
				debugBehavior = new ServiceDebugBehavior();
				serviceHost.Description.Behaviors.Add(debugBehavior);
			}
			debugBehavior.IncludeExceptionDetailInFaults = true;
		}

		/// <summary>Allows descendant tests to apply any client endpoint behaviors.</summary>
		/// <param name="factory">Client channel factory to apply behaviors to.</param>
		protected virtual void ApplyClientBehavior(ChannelFactory<I> factory)
		{
			Trace.TraceInformation("ApplyClientBehavior");
		}
	}
}
