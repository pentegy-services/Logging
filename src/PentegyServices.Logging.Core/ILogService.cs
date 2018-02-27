using System.ServiceModel;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// Combined interface to simplify some scenarios.
	/// </summary>
	[ServiceContract(Namespace = Namespace.ServiceContract)]
	public interface ILogService
		: ILogWriter, ILogReader
	{ }
}
