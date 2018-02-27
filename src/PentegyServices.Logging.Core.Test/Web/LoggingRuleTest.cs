using NUnit.Framework;
using PentegyServices.Logging.Core.Web;
using System;
using System.Collections.Generic;

namespace PentegyServices.Logging.Core.Test.Web
{
	[CLSCompliant(false)]
	[TestFixture]
	public class LoggingRuleTest
	{
		static LoggingRule EmptyRule = new LoggingRule(LoggingRuleType.Save, null, null, null, null, null);
		static LoggingRule FullRule = new LoggingRule(LoggingRuleType.Save, @"html", @"192\.168\.", "GET|POST", "200|302|304", "localhost/test");

		[TestCase(null, null, null, null, null, Result = true)]
		[TestCase("", "", "", "", "", Result = true)]
		[TestCase("html/text", "", "", "", "", Result = true)]
		[TestCase("html/text", "", "POST", "", "", Result = true)]
		public bool Empty_Rule_Matches_Any_Request(String contentType, String ip, String method, String status, String url)
		{
			var requestData = new RequestData(contentType, ip, method, status, url);

			return EmptyRule.IsMatch(requestData);
		}

		[TestCase(null, null, null, null, null, Result = true)]
		[TestCase("", "", "", "", "", Result = false)]
		[TestCase("html/text", "", "", "", "", Result = false)]
		[TestCase("html/text", "", "POST", "", "", Result = false)]
		[TestCase("html/text", "192.168.1.2", "POST", "200", "http://localhost/test/welcome", Result = true)]
		[TestCase("xhtml", "192.168.1.1", "GET", "302", "http://localhost/test", Result = true)]
		[TestCase("xhtml", "192.168.1.1", "get", "302", "http://localhost/test", Result = true)] // case insensitive
		[TestCase("xhtml", "192.168.1.1", "GET", "500", "http://localhost/test", Result = false)]
		public bool Rule_Uses_Logical_AND(String contentType, String ip, String method, String status, String url)
		{
			var requestData = new RequestData(contentType, ip, method, status, url);
			return FullRule.IsMatch(requestData);
		}

		static LoggingRule SkipRule = new LoggingRule(LoggingRuleType.Skip, "xml", "", "", "", "");
		static LoggingRule SaveRule = new LoggingRule(LoggingRuleType.Save, "", "", "POST", "", "");

		[TestCase("xml", "", "POST", "", "", Result = false)]
		[TestCase("xml", "", "GET", "", "", Result = false)]
		[TestCase("", "", "POST", "", "", Result = true)]
		[TestCase("", "", "POST", "", "www", Result = true)]
		[TestCase("", "", "GET", "", "", Result = false)]
		public bool Skip_Rule_Is_First(String contentType, String ip, String method, String status, String url)
		{
			var requestData = new RequestData(contentType, ip, method, status, url);

			var rules = new List<LoggingRule>() { SkipRule, SaveRule };

			return LoggingRule.IsLog(rules, requestData).IsLog;
		}

		[TestCase("xml", "", "POST", "", "", Result = true)]
		[TestCase("xml", "", "GET", "", "", Result = false)]
		[TestCase("", "", "POST", "", "", Result = true)]
		[TestCase("", "", "POST", "", "www", Result = true)]
		[TestCase("", "", "GET", "", "", Result = false)]
		public bool Save_Rule_Is_First(String contentType, String ip, String method, String status, String url)
		{
			var requestData = new RequestData(contentType, ip, method, status, url);

			var rules = new List<LoggingRule>() { SaveRule, SkipRule };

			return LoggingRule.IsLog(rules, requestData).IsLog;
		}
	}
}
