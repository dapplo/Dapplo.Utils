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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

#endregion

namespace Dapplo.Utils.Embedded
{
	/// <summary>
	///     Utilities for embedded resources
	/// </summary>
	public static partial class EmbeddedResources
	{
		/// <summary>
		///     Get the stream for the calling assembly from the manifest resource based on the filePath
		/// </summary>
		/// <param name="filePath">string with the filepath to find</param>
		/// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
		/// <returns>Stream for the filePath, or null if not found</returns>
		public static Stream GetEmbeddedResourceAsStream(string filePath, bool ignoreCase = true)
		{
			return Assembly.GetCallingAssembly().GetEmbeddedResourceAsStream(filePath, ignoreCase);
		}

		/// <summary>
		/// Scan the manifest of the calling Assembly with a regex pattern for embedded resources
		/// </summary>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
		/// <returns>IEnumerable with matching resource names</returns>
		public static IEnumerable<string> FindEmbeddedResources(string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
		{
			var assembly = Assembly.GetCallingAssembly();
			return assembly.FindEmbeddedResources(regexPattern, regexOptions);
		}

		/// <summary>
		/// Scan the manifest of all assemblies in the AppDomain with a regex pattern for embedded resources
		/// Usually this would be used with AppDomain.Current
		/// </summary>
		/// <param name="appDomain">AppDomain to scan</param>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
		/// <returns>IEnumerable with matching assembly resource name tuples</returns>
		public static IEnumerable<Tuple<Assembly, string>> FindEmbeddedResources(this AppDomain appDomain, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
		{
			return appDomain.GetAssemblies().FindEmbeddedResources(regexPattern, regexOptions);
		}

		/// <summary>
		///     Scan the manifest of the Assembly of the supplied Type with a regex pattern for embedded resources
		/// </summary>
		/// <param name="type">Type is used to get the assembly </param>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
		/// <returns>IEnumerable with matching resource names</returns>
		public static IEnumerable<string> FindEmbeddedResources(this Type type, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
		{
			return type.Assembly.FindEmbeddedResources(regexPattern, regexOptions);
		}
	}
}