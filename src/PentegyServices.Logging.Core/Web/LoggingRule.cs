using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// Represents a single rule <see cref="HttpLoggingModule"/> uses to determine if a request must be logged.
	/// When two or more properties of <see cref="LoggingRule"/> are specified they are matched using logical AND.
	/// For example, when <see cref="IP"/> is set to '192\.168\.2\.14' and <see cref="Status"/> is set to '302' 
	/// the rule will match any request from '192.168.2.14' address with response code '302'.
	/// </summary>
	public class LoggingRule
	{
		/// <summary>
		/// Represents a result of matching <see cref="LoggingRule"/> against <see cref="RequestData"/>.
		/// </summary>
		public class MatchResult
		{
			/// <summary>Constructs new instance.</summary>
			public MatchResult(Boolean isLog)
				: this(isLog, null)
			{ }

			/// <summary>Constructs new instance.</summary>
			public MatchResult(Boolean isLog, Int32? ruleIndex)
			{
				IsLog = isLog;
				RuleIndex = ruleIndex;
			}

			/// <summary><c>true</c> - the request must be logged.</summary>
			public readonly Boolean IsLog;

			/// <summary>Optional index of a rule that matched first.</summary>
			public readonly Int32? RuleIndex;
		}

		/// <summary>
		/// Constructs a new rule based on <paramref name="type"/> and a set of <see cref="Regex"/> patterns.
		/// </summary>
		public LoggingRule(LoggingRuleType type, String patternContentType, String patternIP, String patternMethod, String patternStatus, String patternUrl)
		{
			Type = type;

			var options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

			if (!String.IsNullOrEmpty(patternContentType))
			{
				ContentType = new Regex(patternContentType, options);
			}

			if (!String.IsNullOrEmpty(patternIP))
			{
				IP = new Regex(patternIP, options);
			}

			if (!String.IsNullOrEmpty(patternMethod))
			{
				Method = new Regex(patternMethod, options);
			}

			if (!String.IsNullOrEmpty(patternStatus))
			{
				Status = new Regex(patternStatus, options);
			}

			if (!String.IsNullOrEmpty(patternUrl))
			{
				Url = new Regex(patternUrl, options);
			}
		}

		/// <summary>Rule type defines if the rule allows or disallows logging.</summary>
		public readonly LoggingRuleType Type;

		/// <summary><see cref="Regex"/> to match HTTP request method.</summary>
		public readonly Regex Method;

		/// <summary><see cref="Regex"/> to match HTTP request url.</summary>
		public readonly Regex Url;

		/// <summary><see cref="Regex"/> to match HTTP request incoming address.</summary>
		public readonly Regex IP;

		/// <summary><see cref="Regex"/> to match HTTP response status code.</summary>
		public readonly Regex Status;

		/// <summary><see cref="Regex"/> to match HTTP response content type.</summary>
		public readonly Regex ContentType;

		/// <summary>Determines if the data provided match the rule.</summary>
		public Boolean IsMatch(RequestData requestData)
		{
			if (requestData == null)
			{
				throw new ArgumentNullException();
			}

			Boolean result = true;

			if (ContentType != null && requestData.ContentType != null)
			{
				result &= ContentType.IsMatch(requestData.ContentType);
			}

			if (IP != null && requestData.IP != null)
			{
				result &= IP.IsMatch(requestData.IP);
			}

			if (Method != null && requestData.Method != null)
			{
				result &= Method.IsMatch(requestData.Method);
			}

			if (Status != null && requestData.Status != null)
			{
				result &= Status.IsMatch(requestData.Status);
			}

			if (Url != null && requestData.Url != null)
			{
				result &= Url.IsMatch(requestData.Url);
			}

			return result;
		}

		/// <summary>
		/// Determines if a request represented by <see cref="RequestData"/> must be logged.
		/// </summary>
		/// <param name="rules">Rule collection to use.</param>
		/// <param name="requestData">Request parameters.</param>
		/// <returns>An instance of <see cref="MatchResult"/>.</returns>
		public static MatchResult IsLog(IList<LoggingRule> rules, RequestData requestData)
		{
			if (rules == null)
			{
				throw new ArgumentNullException("rules");
			}
			if (requestData == null)
			{
				throw new ArgumentNullException("requestData");
			}

			for (Int32 i = 0; i < rules.Count; i++)
			{
				if (rules[i].IsMatch(requestData))
				{
					return new MatchResult(rules[i].Type == LoggingRuleType.Save, i);
				}
			}

			// no matches found, return result based on whether all rules are of same type
			Boolean onlySave = rules.All(x => x.Type == LoggingRuleType.Save);
			if (onlySave)
			{
				return new MatchResult(false);
			}

			Boolean onlySkip = rules.All(x => x.Type == LoggingRuleType.Skip);
			if (onlySkip)
			{
				return new MatchResult(true);
			}

			// mixed set
			return new MatchResult(!rules.Any()); // if collection is empty - log it, otherwise - do not
		}
	}
}
