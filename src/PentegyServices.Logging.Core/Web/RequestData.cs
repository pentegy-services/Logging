using System;
using System.Text;
using System.Web;

namespace PentegyServices.Logging.Core.Web
{
	/// <summary>
	/// Helper structure to contain data from HttpContext to be used by <see cref="LoggingRule"/>.
	/// </summary>
	public class RequestData
	{
		/// <summary>Constructs new instance based on <see cref="HttpContext"/> provided.</summary>
		public RequestData(HttpContext httpContext)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException();
			}

			ContentType = httpContext.Response.ContentType;
			IP = httpContext.Request.UserHostAddress;
			Method = httpContext.Request.RequestType;
			Status = httpContext.Response.StatusCode.ToString();
			Url = httpContext.Request.Url.ToString();
		}

		/// <summary>Constructs new instance based on parameters (all optional) provided.</summary>
		public RequestData(String contentType, String ip, String method, String status, String url)
		{
			ContentType = contentType;
			IP = ip;
			Method = method;
			Status = status;
			Url = url;
		}

		/// <summary>HTTP request method.</summary>
		public String Method;

		/// <summary>HTTP request url.</summary>
		public String Url;

		/// <summary>HTTP request incoming address.</summary>
		public String IP;

		/// <summary>HTTP response status code.</summary>
		public String Status;

		/// <summary>HTTP response content type.</summary>
		public String ContentType;

		/// <summary>Converts the instance into text representation.</summary>
		public override String ToString()
		{
			var sb = new StringBuilder();
			sb.Append("Method: " + Method);
			sb.Append("\tStatus: " + Status);
			sb.Append("\tIP: " + IP);
			sb.Append("\tContentType: " + ContentType);
			sb.Append("\tUrl: " + Url);
			return sb.ToString();
		}
	}
}
