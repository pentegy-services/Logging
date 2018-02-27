namespace PentegyServices.Logging.Core.Web
{
	/// <summary>Rule type defines if the rule allows or disallows logging.</summary>
	public enum LoggingRuleType
	{
		/// <summary>If a request matches the rule it will be logged.</summary>
		Save,

		/// <summary>If a request matches the rule it will not be logged.</summary>
		Skip
	}
}
