using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Text;

namespace PentegyServices.Logging.Core.Data
{
	/// <summary>
	/// Simple CSV export (taken from <a href="http://stackoverflow.com/questions/2422212/simple-c-csv-excel-export-class">http://stackoverflow.com/questions/2422212/simple-c-csv-excel-export-class</a>).
	/// Example:
	/// <code>
	///   CsvExport myExport = new CsvExport();
	///
	///   myExport.AddRow();
	///   myExport["Region"] = "New York, USA";
	///   myExport["Sales"] = 100000;
	///   myExport["Date Opened"] = new DateTime(2003, 12, 31);
	///
	///   myExport.AddRow();
	///   myExport["Region"] = "Sydney \"in\" Australia";
	///   myExport["Sales"] = 50000;
	///   myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);
	///</code>
	/// Then you can do any of the following three output options:
	/// <code>
	///   string myCsv = myExport.Export();
	///   myExport.ExportToFile("Somefile.csv");
	///   byte[] myCsvData = myExport.ExportToBytes();
	///   </code>
	/// </summary>
	public class CsvExport
	{
		/// <summary>
		/// To keep the ordered list of column names
		/// </summary>
		List<String> fields = new List<String>();

		/// <summary>
		/// The list of rows
		/// </summary>
		List<Dictionary<String, Object>> rows = new List<Dictionary<String, Object>>();

		/// <summary>
		/// The current row
		/// </summary>
		Dictionary<String, Object> currentRow
		{
			get
			{
				return rows[rows.Count - 1];
			}
		}

		String separator = String.Empty;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public CsvExport()
			: this(",")
		{ }

		/// <summary>
		/// CsvExport may use different separator.
		/// </summary>
		/// <param name="separator">separator symbol</param>
		public CsvExport(String separator) 
		{
			this.separator = separator;
		}

		/// <summary>
		/// Set a value on this column
		/// </summary>
		public Object this[String field]
		{
			set
			{
				// Keep track of the field names, because the dictionary loses the ordering
				if (!fields.Contains(field))
				{
					fields.Add(field);
				}
				currentRow[field] = value;
			}
		}

		/// <summary>
		/// Call this before setting any fields on a row
		/// </summary>
		public void AddRow()
		{
			rows.Add(new Dictionary<String, Object>());
		}

		/// <summary>
		/// Converts a value to how it should output in a csv file
		/// If it has a comma, it needs surrounding with double quotes
		/// Eg Sydney, Australia -> "Sydney, Australia"
		/// Also if it contains any double quotes ("), then they need to be replaced with quad quotes[sic] ("")
		/// Eg "Dangerous Dan" McGrew -> """Dangerous Dan"" McGrew"
		/// </summary>
		String MakeValueCsvFriendly(Object value)
		{
			if (value == null)
			{
				return "";
			}

			if (value is INullable && ((INullable)value).IsNull)
			{
				return "";
			}

			if (value is DateTime)
			{
				if (((DateTime)value).TimeOfDay.TotalSeconds == 0)
				{
					return ((DateTime)value).ToString("yyyy-MM-dd");
				}

				return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
			}

			String output = value.ToString();
			if (output.Contains(",") || output.Contains("\""))
			{
				output = '"' + output.Replace("\"", "\"\"") + '"';
			}

			// Fields with embedded line breaks must be enclosed within double-quote characters
			if (output.Contains("\n") || output.Contains("\r"))
			{
				output = "\"\"" + output.Replace("\n", "").Replace("\r", "") + "\"\"";
			}

			return output;
		}

		/// <summary>
		/// Output all rows as a CSV returning a string
		/// </summary>
		public String Export()
		{
			StringBuilder sb = new StringBuilder();

			// The header
			foreach (String field in fields)
			{
				sb.Append(field).Append(separator);
			}
			sb.AppendLine();

			// The rows
			foreach (Dictionary<String, Object> row in rows)
			{
				foreach (String field in fields)
				{
					sb.Append(MakeValueCsvFriendly(row[field])).Append(separator);
				}
				sb.AppendLine();
			}

			return sb.ToString();
		}

		/// <summary>
		/// Exports to a file
		/// </summary>
		public void ExportToFile(String path)
		{
			File.WriteAllText(path, Export());
		}

		/// <summary>
		/// Exports as raw UTF8 bytes with preamble.
		/// </summary>
		public Byte[] ExportToBytes()
		{
			Byte[] preamble = Encoding.UTF8.GetPreamble(); // required to correctly handle non-latin chars in Excel
			Byte[] output = Encoding.UTF8.GetBytes(Export());

			Byte[] result = new Byte[preamble.Length + output.Length];
			Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
			Buffer.BlockCopy(output, 0, result, preamble.Length, output.Length);

			return result;
		}
	}
}
