using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// A helper HTTP handler to output process diagnostics information gathered by <see cref="SysUtil.GatherSystemInfo(Boolean)"/>.
	/// Register it as usual:
	/// <code>
	/// &lt;httpHandlers&gt;
	///		&lt;add verb="GET" path="ProcDiag" type="Core.Web.ProcessDiagnosticsHandler, Core"/&gt;
	///	&lt;/httpHandlers&gt;
	/// </code>
	/// For ASP.NET MVC projects you also need to ignore the route:
	/// <code>
	/// 	routes.IgnoreRoute("ProcDiag");
	/// </code>
	/// </summary>
	public class ProcessDiagnosticsHandler
		: IHttpHandler, IRequiresSessionState 
	{
		#region IHttpHandler Members

		/// <summary>
		/// Gets a value indicating whether another request can use the <see cref="IHttpHandler"/> instance. 
		/// </summary>
		public Boolean IsReusable
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Processes the request.
		/// </summary>
		/// <param name="context">An <see cref="HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests. </param>
		public void ProcessRequest(HttpContext context)
		{
			var watch = new Stopwatch();
			watch.Start();

			Boolean calculateCollections = context.Request.QueryString["calculateCollections"] != null;

			IDictionary<String, String> systemInfo = SysUtil.GatherSystemInfo(calculateCollections);
			context.Response.Write("<h1>Process Diagnostics</h1>");

			String[] groups = systemInfo.GroupBy(x => Regex.Match(x.Key, @"^[^\.]+(?=\.)").Value).Select(x => x.Key).ToArray();


			context.Response.Write("<table style='font-size: .75em; font-family: Verdana, Helvetica, Sans-Serif; color: dimGray; border: solid 1px #E8EEF4; '>");
			foreach (var group in groups)
			{
				context.Response.Write(String.Format("<tr><td colspan='2' style='padding:0.5em; font-size: x-large; color: black;'><strong>{0}</strong></td></tr>", HttpUtility.HtmlEncode(group)));
				foreach (var data in systemInfo.Where(x => x.Key.StartsWith(group)))
				{
					context.Response.Write(String.Format("<tr><td>{0}</td><td>{1}</td></tr>", HttpUtility.HtmlEncode(data.Key), HttpUtility.HtmlEncode(data.Value)));
				}
			}
			context.Response.Write("</table>");

			watch.Stop();
			context.Response.Write(String.Format("<p style='font-size:x-small; color: dimGray;'>Generated at {0:o} in {1} seconds.</p>", DateTime.Now, watch.Elapsed.TotalSeconds));
		}

		#endregion
	}
}
