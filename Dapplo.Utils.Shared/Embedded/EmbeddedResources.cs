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
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Utils.Embedded
{
	/// <summary>
	///     Utilities for embedded resources
	/// </summary>
	public static partial class EmbeddedResources
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// Get the stream for a assembly manifest resource based on the filePath
		/// It will automatically wrapped as GZipStream if the file-ending is .gz
		/// Note: a GZipStream is not seekable, this might cause issues.
		/// </summary>
		/// <param name="filePath">string with the filepath to find</param>
		/// <param name="assembly">Assembly to look into</param>
		/// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
		/// <returns>Stream for the filePath, or null if not found</returns>
		public static Stream GetEmbeddedResourceAsStream(this Assembly assembly, string filePath, bool ignoreCase = true)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException(nameof(assembly));
			}
			if (string.IsNullOrEmpty(filePath))
			{
				throw new ArgumentNullException(nameof(filePath));
			}
			// Resources don't have directory separators, they use ., fix this before creating a regex
			var resourcePath = filePath.Replace(Path.DirectorySeparatorChar, '.');
			// First get the extension to build the regex
			// TODO: this doesn't work 100% for double extensions like .png.gz etc but I will only fix this as soon as it's really needed
			var extensions = new[] {Path.GetExtension(resourcePath) };
			// Than get the filename without extension
			var filename = Path.GetFileNameWithoutExtension(resourcePath);
			// Assembly resources CAN have a prefix with the type namespace, use this instead of the default
			var prefix = $@"^({assembly.GetName().Name.Replace(".", @"\.")}\.)?";
			// build the regex
			var filePathRegex = FileTools.FilenameToRegex(filename, extensions, true, prefix);

			// Find the regex
			var resourceName = assembly.FindEmbeddedResources(filePathRegex).FirstOrDefault();
			if (resourceName != null)
			{
				Log.Verbose().WriteLine("Requested stream for path {0}, using resource {1} from assembly {2}", filePath, resourceName, assembly.FullName);
				var resultStream = assembly.GetManifestResourceStream(resourceName);
				if (resultStream != null && resourceName.EndsWith(".gz"))
				{
					resultStream = new GZipStream(resultStream, CompressionMode.Decompress);
				}
				if (resultStream == null)
				{
					Log.Warn().WriteLine("Couldn't get the resource stream for {0} from assembly {1}", resourceName, assembly.FullName);
				}
				return resultStream;
			}
			Log.Warn().WriteLine("Couldn't find the resource stream for path {0}, using regex pattern {1} from assembly {2}", filePath, filePathRegex, assembly.FullName);
			return null;
		}

		/// <summary>
		///     Scan the manifest of the supplied Assembly with a regex pattern for embedded resources
		/// </summary>
		/// <param name="assembly">Assembly to scan</param>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
		/// <returns>IEnumerable with matching resource names</returns>
		public static IEnumerable<string> FindEmbeddedResources(this Assembly assembly, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
		{
			return assembly.FindEmbeddedResources(new Regex(regexPattern, regexOptions));
		}

		/// <summary>
		///     Scan the manifest of the supplied Assembly with a regex pattern for embedded resources
		/// </summary>
		/// <param name="assembly">Assembly to scan</param>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <returns>IEnumerable with matching resource names</returns>
		public static IEnumerable<string> FindEmbeddedResources(this Assembly assembly, Regex regexPattern)
		{
			return from resourceName in assembly.GetManifestResourceNames()
				   where regexPattern.IsMatch(resourceName)
				   select resourceName;
		}

		/// <summary>
		///     Scan the manifest of the supplied Assembly elements with a regex pattern for embedded resources
		/// </summary>
		/// <param name="assemblies">IEnumerable with Assembly elements to scan</param>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
		/// <returns>IEnumerable with matching resource names</returns>
		public static IEnumerable<Tuple<Assembly, string>> FindEmbeddedResources(this IEnumerable<Assembly> assemblies, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
		{
			return assemblies.FindEmbeddedResources(new Regex(regexPattern, regexOptions));
		}

		/// <summary>
		///     Scan the manifest of the supplied Assembly elements with a regex pattern for embedded resources
		/// </summary>
		/// <param name="assemblies">IEnumerable with Assembly elements to scan</param>
		/// <param name="regex">Regex to scan for</param>
		/// <returns>IEnumerable with matching resource names</returns>
		public static IEnumerable<Tuple<Assembly, string>> FindEmbeddedResources(this IEnumerable<Assembly> assemblies, Regex regex)
		{
			return from assembly in assemblies
				   from resourceName in assembly.GetManifestResourceNames()
				   where regex.IsMatch(resourceName)
				   select new Tuple<Assembly, string>(assembly, resourceName);
		}
	}
}