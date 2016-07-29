#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Utils
// 
// Dapplo.Utils is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Utils is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Utils. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Text.RegularExpressions;

#endregion

namespace Dapplo.Utils
{
	/// <summary>
	///     Util class for working with a file pattern
	/// </summary>
	public static class FilePattern
	{
		private const string NonDotCharacters = @"[^.]*";
		private static readonly Regex HasQuestionMarkRegEx = new Regex(@"\?", RegexOptions.Compiled);
		private static readonly Regex IllegalCharactersRegex = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
		private static readonly Regex CatchExtentionRegex = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);

		/// <summary>
		///     Helper method to convert a file pattern e.g. *.txt to a regexp
		/// </summary>
		/// <param name="pattern">file pattern</param>
		/// <param name="ignoreCase">true to ignore the case, this is default</param>
		/// <returns>Regex</returns>
		public static Regex FilePatternToRegex(string pattern, bool ignoreCase = true)
		{
			if (pattern == null)
			{
				throw new ArgumentNullException();
			}
			pattern = pattern.Trim();
			if (pattern.Length == 0)
			{
				throw new ArgumentException("Pattern is empty.");
			}
			if (IllegalCharactersRegex.IsMatch(pattern))
			{
				throw new ArgumentException("Pattern contains illegal characters.");
			}
			var hasExtension = CatchExtentionRegex.IsMatch(pattern);
			var matchExact = false;
			if (HasQuestionMarkRegEx.IsMatch(pattern))
			{
				matchExact = true;
			}
			else if (hasExtension)
			{
				matchExact = CatchExtentionRegex.Match(pattern).Groups[1].Length != 3;
			}
			var regexString = Regex.Escape(pattern);
			regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
			regexString = Regex.Replace(regexString, @"\\\?", ".");
			if (!matchExact && hasExtension)
			{
				regexString += NonDotCharacters;
			}
			regexString += "$";
			return new Regex(regexString, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
		}
	}
}