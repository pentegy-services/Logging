using log4net;
using System;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// A base class for message inspectors that work with log4net <see cref="ThreadContext"/> properties.
	/// </summary>
	public abstract class ThreadContextMessageInspectorBase
	{
		/// <summary>
		/// Namespace to be used for <see cref="System.ServiceModel.Channels.MessageHeader"/> being added.
		/// Defaults to <c>typeof(log4net.ThreadContext).Namespace</c>.
		/// </summary>
		public virtual string HeaderNamespace
		{
			get
			{
				return typeof(ThreadContext).Namespace;
			}
		}

		/// <summary>
		/// A list of property names to add.
		/// </summary>
		public String[] Properties { get; protected set; }
	}
}
