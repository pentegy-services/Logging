using PentegyServices.Logging.Core;
using System;

namespace PentegyServices.Logging.Core.ServiceModel
{
	/// <summary>
	/// <see cref="XmlParameterInspector"/> configuration settings extracted by <see cref="XmlParameterInspectorElement"/>.
	/// </summary>
	public class XmlParamInspectorSettings
	{
		/// <summary>
		/// Creates new instance of <see cref="XmlParamInspectorSettings"/> with default settings.
		/// </summary>
		public XmlParamInspectorSettings()
		{
			Name = "unspecified";
			LogBefore = true;
			LogAfter = true;
			LogInputs = true;
			LogOutputs = true;
			XDepthLimit = XSerializer.DefaultDepthLimit;
			XSizeLimit = XSerializer.DefaultSizeLimit;
			XLengthLimit = XSerializer.DefaultLengthLimit;
		}

		/// <summary>
		/// Inspector name.
		/// </summary>
		public String Name { get; set; }

		/// <summary>
		/// <c>true</c> if the inspector should log "before" entry.
		/// </summary>
		public Boolean LogBefore { get; set; }

		/// <summary>
		/// <c>true</c>if the inspector should log "after" entry. If this property is <c>true</c> and service call faults no entry will be logged.
		/// </summary>
		public Boolean LogAfter { get; set; }

		/// <summary>
		/// <c>true</c> if the inspector should log input parameters.
		/// </summary>
		public Boolean LogInputs { get; set; }

		/// <summary>
		/// <c>true</c> if the inspector should log output parameters (result).
		/// </summary>
		public Boolean LogOutputs { get; set; }

		/// <summary>Value to configure <see cref="XSerializer.DepthLimit"/> parameter.</summary>
		public Int32 XDepthLimit { get; set; }

		/// <summary>Value to configure <see cref="XSerializer.SizeLimit"/> parameter.</summary>
		public Int32 XSizeLimit { get; set; }

		/// <summary>Value to configure <see cref="XSerializer.LengthLimit"/> parameter.</summary>
		public Int32 XLengthLimit { get; set; }

	}
}
