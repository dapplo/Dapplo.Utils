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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Dapplo.Log.Facade;
using Dapplo.Utils.Resolving;

#endregion

namespace Dapplo.Utils.Embedded
{
	/// <summary>
	///     Utilities for embedded resources
	/// </summary>
	public static partial class EmbeddedResources
	{
		private static readonly Regex PackRegex = new Regex(@"/(?<assembly>[a-zA-Z\.]+);component/(?<path>.*)", RegexOptions.Compiled);

		/// <summary>
		/// Helper method to create a regex match for the supplied Pack uri
		/// </summary>
		/// <param name="packUri">Uri</param>
		/// <returns>Match</returns>
		public static Match PackUriMatch(this Uri packUri)
		{
			if (packUri == null)
			{
				throw new ArgumentNullException(nameof(packUri));
			}
			if (!"pack".Equals(packUri.Scheme))
			{
				throw new ArgumentException("Scheme is not pack:", nameof(packUri));
			}
			if (!"application:,,,".Equals(packUri.Host))
			{
				throw new ArgumentException("pack uri is not for application", nameof(packUri));
			}
			var match = PackRegex.Match(packUri.AbsolutePath);
			if (!match.Success)
			{
				throw new ArgumentException("pack uri isn't correctly formed.", nameof(packUri));
			}
			return match;
		}

		/// <summary>
		/// Test if there is an embedded resourcefor the Pack-Uri
		/// This is work in progress, as most of the times the files are compiled from xaml to baml, and won't be recognized when you specify a pack uri ending on .xaml
		/// </summary>
		/// <param name="packUri">Uri</param>
		/// <param name="ignoreCase">true to ignore the case</param>
		/// <returns>Stream</returns>
		public static bool EmbeddedResourceExists(this Uri packUri, bool ignoreCase = true)
		{
			var match = packUri.PackUriMatch();

			var assemblyName = match.Groups["assembly"].Value;
			
			var assembly = AssemblyResolver.FindAssembly(assemblyName);
			if (assembly == null)
			{
				return false;
			}

			var path = match.Groups["path"].Value;

			var resourceRegex = assembly.ResourceRegex(path, ignoreCase);

			if (assembly.HasResource(resourceRegex))
			{
				return true;
			}
			return assembly.HasEmbeddedDotResourcesResource(path);
		}

		/// <summary>
		/// check if there is any resource in the specified assembly's .g.resources which matches the Regex
		/// This is work in progress, as most of the times the files are compiled from xaml to baml, and won't be recognized when you specify .xaml
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="filePath">filePath</param>
		/// <param name="ignoreCase">true to ignore the case</param>
		/// <returns>bool with true if there is a matching resource</returns>
		public static bool HasEmbeddedDotResourcesResource(this Assembly assembly, string filePath, bool ignoreCase = true)
		{
			var resourceNames = assembly.GetManifestResourceNames();
			foreach (var resourcesFile in resourceNames.Where(x => x.EndsWith(".g.resources")))
			{
				Log.Verbose().WriteLine("Resource not directly found, trying {0}", resourcesFile);
				using (var resourceStream = assembly.GetEmbeddedResourceAsStream(resourcesFile))
				{
					if (resourceStream != null)
					{

						using (var resourceReader = new ResourceReader(resourceStream))
						{
							// Check if it contains the filename
							return resourceReader.OfType<DictionaryEntry>().Select(x => x.Key as string).Any(x => string.Equals(x, filePath, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture));
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the embedded resource, as specified in the Pack-Uri as a stream.
		/// This currently doesn't go into the embedded .g.resources files, this might be added later
		/// </summary>
		/// <param name="packUri">Uri</param>
		/// <returns>Stream</returns>
		public static Stream GetEmbeddedResourceAsStream(this Uri packUri)
		{
			var match = packUri.PackUriMatch();

			var assemblyName = match.Groups["assembly"].Value;
			var assembly = AssemblyResolver.FindAssembly(assemblyName);
			if (assembly == null)
			{
				throw new ArgumentException($"Pack uri references unknown assembly {assemblyName}.", nameof(packUri));
			}
			var path = match.Groups["path"].Value;
			return assembly.GetEmbeddedResourceAsStream(path);
		}

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

		/// <summary>
		///     Scan the manifest of the Assembly of the supplied Type with a regex pattern for embedded resources
		/// </summary>
		/// <param name="type">Type is used to get the assembly </param>
		/// <param name="regex">Regex to scan for</param>
		/// <returns>IEnumerable with matching resource names</returns>
		public static IEnumerable<string> FindEmbeddedResources(this Type type, Regex regex)
		{
			return type.Assembly.FindEmbeddedResources(regex);
		}
	}
}