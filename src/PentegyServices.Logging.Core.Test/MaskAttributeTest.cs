using NUnit.Framework;
using System;
using System.Diagnostics;
#pragma warning disable 1591 // Missing XML comment

namespace PentegyServices.Logging.Core.Test
{
	[TestFixture]
	public class MaskAttributeTest
		: TestCaseBase
	{
		public const String CardNumberMask = @"(?<=^\d{4})\d{8}(?=\d{4}$)"; // card number is 16 digits without spaces

		public const String AccountMask = @"(?<=^\d{4})\d{1,6}(?=\d{0,4}$)"; // account is 5 to 14 digits without spaces

		[Test, TestCaseSource("Samples")]
		public void ApplyPartialMask(String sourceValue, String expectedMaskedValue)
		{
			var attr = new MaskAttribute() { Partial = true };
			DoApply(attr, sourceValue, expectedMaskedValue);
		}

		[Test, TestCaseSource("Samples")]
		public void ApplyFullMask(String sourceValue, String expectedMaskedValue)
		{
			var attr = new MaskAttribute() { Partial = false };
			DoApply(attr, sourceValue, MaskAttribute.DefaultMask);
		}

		protected static void DoApply(MaskAttribute attr, String sourceValue, String expectedMaskedValue)
		{
			String maskedValue = attr.Apply(sourceValue);

			Trace.TraceInformation("Applying mask to '{0}': '{1}'", sourceValue ?? "(null)", maskedValue ?? "(null)");
			if (sourceValue != null)
			{
				Assert.AreEqual(expectedMaskedValue, maskedValue);
			}
			else
			{
				Assert.IsNull(maskedValue);
			}
		}

		public Object[] Samples =
		{
			new Object[] { (String)null, (String)null},
			new Object[] { "", "****"},
			new Object[] { "a", "****"},
			new Object[] { "ab", "****"},
			new Object[] { "abc", "****"},
			new Object[] { "abcd", "****"},
			new Object[] { "abcde", "****"},
			new Object[] { "abcdef", "****"},
			new Object[] { "abcdefg", "****"},
			new Object[] { "abcdefgh", "****"},
			new Object[] { "abcd fghi", "abcd fghi"},
			new Object[] { "abcdefghi", "abcd*fghi"},
			new Object[] { "Loan 212/1104 repayment from 26054000918881 to 29021000093302", "Loan ******** ********* **** ************** ** **********3302"},

			new Object[] { "0000111188882222", "0000********2222"},
			new Object[] { "0000 1111 8888 2222", "0000 **** **** 2222"},
			new Object[] { "0000abcd 8888", "0000**** 8888"},
			new Object[] { "0000a8888", "0000*8888"},
			new Object[] { "0000 8888", "0000 8888"},
			new Object[] { "26209001800330", "2620******0330"}
		};
	}
}
