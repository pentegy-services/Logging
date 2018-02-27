using System;
using System.Diagnostics;

namespace PentegyServices.Logging.Core
{
	/// <summary>
	/// This is a marker attribute to be used when sensitive data need to be masked (mostly when logging).
	/// Supports full or partial masking. The former is useful for most sensitive data (like passwords, sometimes client names;
	/// replaces the origin with a fixed mask string, i.e. does not preserve original length),
	/// the latter is useful for numerical values like card or account numbers (preserves original length).
	/// <para>Note, this attribute does not enforce any rules and works in conjunction with classes that care of it.
	/// That means it's up to you to handle masking when <see cref="Object.ToString()"/> or other mechanisms are used.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, 
		AllowMultiple = false)]
	public class MaskAttribute
		: Attribute
	{
		/// <summary>Used to replace characters in the source string when <see cref="Partial"/> is <c>true</c>.</summary>
		public const Char CharMask = '*';

		/// <summary>Used to replace the whole source string when <see cref="Partial"/> is <c>false</c>.</summary>
		public const String DefaultMask = "****";

		/// <summary>
		/// Indicates whether to apply <see cref="DefaultMask"/> regardless the source length (<c>false</c>)
		/// or partial mask (<c>true</c>) when only middle of the sequence are masked.
		/// </summary>
		public Boolean Partial = false;

		/// <summary>
		/// Creates the instance of <see cref="MaskAttribute"/>. <see cref="Partial"/> value will default to <c>false</c>.
		/// </summary>
		public MaskAttribute()
		{ }

		/// <summary>
		/// Applies the mask to the given string.
		/// </summary>
		/// <param name="value"><see cref="String"/> value to be masked.</param>
		/// <returns>Masked <see cref="String"/> if <paramref name="value"/> was not <c>null</c>.</returns>
		public String Apply(String value)
		{
			String result = Apply(value, Partial, CharMask, DefaultMask);
			return result;
		}

		/// <summary>
		/// Applies the mask to the given string.
		/// </summary>
		/// <param name="value"><see cref="String"/> value to be masked.</param>
		/// <param name="partial"><c>true</c> to perform partial masking rather than full replacement.</param>
		/// <param name="charMask">A character used for partial replacement.</param>
		/// <param name="mask">Default (full) mask ro replace <paramref name="value"/>.</param>
		/// <returns>Masked <see cref="String"/> if <paramref name="value"/> was not <c>null</c>.</returns>
		public static String Apply(String value, Boolean partial, Char charMask, String mask)
		{
			String result = null;
			if (value != null)
			{
				Debug.Assert(mask.Length > 0);
				if (partial)
				{
					if (value.Length <= mask.Length * 2)
					{
						result = DefaultMask; // too short for partial masking
					}
					else
					{
						Char[] seq = value.ToCharArray();
						for (Int32 i = mask.Length; i < seq.Length - mask.Length; i++)
						{
							if (!Char.IsWhiteSpace(seq[i]))
							{
								seq[i] = charMask;
							}
						}
						result = new String(seq);
					}
				}
				else
				{
					result = mask;
				}
			}
			return result;
		}
	}
}
