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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapplo.Utils.Embedded;
using Dapplo.Utils.Extensions;
using Dapplo.Log.Facade;
using System.Text.RegularExpressions;
using System.IO.Compression;

#endregion

namespace Dapplo.Utils.Resolving
{
	/// <summary>
	///     This is a static Assembly resolver and Assembly loader
	///     It doesn't use a logger or other dependencies outside the Dapplo.Utils dll to make it possible to only have this DLL in the output directory
	/// </summary>
	public static class AssemblyResolver
	{
		private static readonly LogSource Log = new LogSource();
		private const string AssemblyEndingRegexPattern = @"(\.exe|\.exe\.gz|\.dll|\.dll\.gz)$";
		private static readonly ISet<string> AppDomainRegistrations = new HashSet<string>();
		private static readonly ISet<string> ResolveDirectories = new HashSet<string>();
		private static readonly IDictionary<string, Assembly> AssembliesByName = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
		private static readonly IDictionary<string, Assembly> AssembliesByPath = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Setup and Register some of the default assemblies in the assembly cache
		/// </summary>
		static AssemblyResolver()
		{
			Register(Assembly.GetCallingAssembly());
			Register(Assembly.GetEntryAssembly());
			Register(Assembly.GetExecutingAssembly());
			AddDirectory(".");
		}

		/// <summary>
		/// Directories which this AssemblyResolver uses to find assemblies
		/// </summary>
		public static IEnumerable<string> Directories => ResolveDirectories;

		/// <summary>
		/// Add the specified directory, by converting it to an absolute directory
		/// </summary>
		/// <param name="directory">Directory to add for resolving</param>
		public static void AddDirectory(string directory)
		{
			lock (ResolveDirectories)
			{
				foreach (var absoluteDirectory in FileLocations.DirectoriesFor(directory))
				{
					ResolveDirectories.Add(absoluteDirectory);
				}
			}
		}

		/// <summary>
		/// Extension to register an assembly to the AssemblyResolver, this is used for resolving embedded assemblies
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="filePath">Path to assembly, or null if it isn't loaded from the file system</param>
		public static void Register(this Assembly assembly, string filePath = null)
		{
			if (assembly == null)
			{
				Log.Verbose().WriteLine("Register was callled with null.");
				return;
			}
			lock (AssembliesByName)
			{
				if (!AssembliesByName.ContainsKey(assembly.GetName().Name))
				{
					AssembliesByName[assembly.GetName().Name] = assembly;
				}
			}
			lock (AssembliesByPath)
			{
				filePath = filePath ?? assembly.Location;
				if (!string.IsNullOrEmpty(filePath) && !AssembliesByPath.ContainsKey(filePath))
				{
					AssembliesByPath[filePath] = assembly;
				}
			}
		}

		/// <summary>
		///     IEnumerable with all cached assemblies
		/// </summary>
		public static IEnumerable<Assembly> AssemblyCache => AssembliesByName.Values;

		/// <summary>
		///     Defines if the resolving is first loading internal files, if nothing was found check the file system
		///     There might be security reasons for not doing this.
		/// </summary>
		public static bool ResolveEmbeddedBeforeFiles { get; set; } = true;

		/// <summary>
		///     Register the AssemblyResolve event for the specified AppDomain
		///     This can be called multiple times, it detect this.
		/// </summary>
		/// <returns>IDisposable, when disposing this the event registration is removed</returns>
		public static IDisposable RegisterAssemblyResolve(this AppDomain appDomain)
		{
			lock (AppDomainRegistrations)
			{
				if (!AppDomainRegistrations.Contains(appDomain.FriendlyName))
				{
					AppDomainRegistrations.Add(appDomain.FriendlyName);
					appDomain.AssemblyResolve += ResolveEventHandler;
				}
				return Disposable.Create(() => UnegisterAssemblyResolve(appDomain));
			}
		}

		/// <summary>
		///     Register AssemblyResolve on the current AppDomain
		/// </summary>
		/// <returns>IDisposable, when disposing this the event registration is removed</returns>
		public static IDisposable RegisterAssemblyResolve()
		{
			return AppDomain.CurrentDomain.RegisterAssemblyResolve();
		}

		/// <summary>
		///     Unegister the AssemblyResolve event for the specified AppDomain
		///     This can be called multiple times, it detect this.
		/// </summary>
		public static void UnegisterAssemblyResolve(this AppDomain appDomain)
		{
			lock (AppDomainRegistrations)
			{
				if (AppDomainRegistrations.Contains(appDomain.FriendlyName))
				{
					AppDomainRegistrations.Remove(appDomain.FriendlyName);
					appDomain.AssemblyResolve -= ResolveEventHandler;
				}
			}
		}

		/// <summary>
		///     Unregister AssemblyResolve from the current AppDomain
		/// </summary>
		public static void UnegisterAssemblyResolve()
		{
			AppDomain.CurrentDomain.UnegisterAssemblyResolve();
		}

		/// <summary>
		///     A resolver which takes care of loading DLL's which are referenced from AddOns but not found
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="resolveEventArgs">ResolveEventArgs</param>
		/// <returns>Assembly</returns>
		private static Assembly ResolveEventHandler(object sender, ResolveEventArgs resolveEventArgs)
		{
			var assemblyName = new AssemblyName(resolveEventArgs.Name);
			Log.Verbose().WriteLine("Resolve event for {0}", assemblyName.FullName);
			return FindAssembly(assemblyName.Name);
		}

		/// <summary>
		///     Simple method to load an assembly from a file path (or returned a cached version).
		///     If it was loaded new, it will be added to the cache
		/// </summary>
		/// <param name="filepath">string with the path to the file</param>
		/// <returns>Assembly</returns>
		public static Assembly LoadAssemblyFromFile(string filepath)
		{
			if (string.IsNullOrEmpty(filepath))
			{
				throw new ArgumentNullException(nameof(filepath));
			}
			var assembly = AssembliesByName.Values.FirstOrDefault(x => x.Location == filepath);

			if (assembly == null)
			{
				lock (AssembliesByPath)
				{
					AssembliesByPath.TryGetValue(filepath, out assembly);
				}
			}

			if (assembly == null)
			{
				if (filepath.EndsWith(".gz"))
				{
					// Gzipped file, needs to be loaded via a stream, and change to a 
					byte[] assemblyBytes;
					using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite))
					using (var stream = new GZipStream(fileStream, CompressionMode.Decompress))
					{
						assemblyBytes = stream.ToByteArray();
					}
					assembly = Assembly.Load(assemblyBytes);
				}
				else
				{
					assembly = Assembly.LoadFile(filepath);
				}
				// Make sure the directory of the file is known to the resolver
				// this takes care of dlls which are in the same directory as this assembly.
				// It only makes sense if this method was called directly, but as the ResolveDirectories is a set, it doesn't hurt.
				var assemblyDirectory = Path.GetDirectoryName(filepath);
				if (!string.IsNullOrEmpty(assemblyDirectory))
				{
					lock (ResolveDirectories)
					{
						ResolveDirectories.Add(assemblyDirectory);
					}
				}
				// Register the assembly in the cache, by name and by path
				Register(assembly, filepath);
			}
			return assembly;
		}

		/// <summary>
		///     Simple method to load an assembly from a stream.
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <returns>Assembly or null when the stream is null</returns>
		public static Assembly LoadAssemblyFromStream(Stream stream)
		{
			if (stream == null)
			{
				return null;
			}
			// Load the assembly, unfortunately this only works via a byte array
			var assembly = Assembly.Load(stream.ToByteArray());
			Register(assembly);
			return assembly;
		}

		/// <summary>
		///     Find the specified assembly from a manifest resource or from the file system.
		///     It is possible to use wildcards but the first match will be loaded!
		/// </summary>
		/// <param name="assemblyName">string with the assembly name, e.g. from AssemblyName.Name, do not specify an extension</param>
		/// <returns>Assembly or null</returns>
		public static Assembly FindAssembly(string assemblyName)
		{
			Assembly assembly;
			// Do not use the cache if a wildcard was used.
			if (!assemblyName.Contains("*"))
			{
				lock (AssembliesByName)
				{
					// Try the cache
					if (AssembliesByName.TryGetValue(assemblyName, out assembly))
					{
						return assembly;
					}
				}
			}

			// Loading order depends on ResolveEmbeddedBeforeFiles
			if (ResolveEmbeddedBeforeFiles)
			{
				assembly = LoadEmbeddedAssembly(assemblyName) ?? LoadAssemblyFromFileSystem(assemblyName);
			}
			else
			{
				assembly = LoadAssemblyFromFileSystem(assemblyName) ?? LoadEmbeddedAssembly(assemblyName);
			}

			return assembly;
		}

		/// <summary>
		/// Create a regex to find the specified assembly
		/// </summary>
		/// <param name="assemblyName">
		/// string with the name of the assembly.
		/// A file pattern like Dapplo.* is allowed, and would be converted to Dapplo\..*(\.exe|\.exe\.gz|\.dll|\.dll\.gz)$
		/// </param>
		/// <param name="ignoreCase">default is true and makes sure the case is ignored</param>
		/// <returns>Regex</returns>
		public static Regex AssemblyNameToRegex(string assemblyName, bool ignoreCase = true)
		{
			if (assemblyName == null)
			{
				throw new ArgumentNullException(nameof(assemblyName));
			}
			string regex = assemblyName.Replace(".", @"\.").Replace("*", ".*");
			return new Regex($"{regex}{AssemblyEndingRegexPattern}", ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
		}

		/// <summary>
		///     Load the specified assembly from a manifest resource, or return null
		/// </summary>
		/// <param name="assemblyName">string</param>
		/// <returns>Assembly</returns>
		public static Assembly LoadEmbeddedAssembly(string assemblyName)
		{
			var assemblyRegex = AssemblyNameToRegex(assemblyName);
			try
			{
				var resourceTuple = AssemblyCache.FindEmbeddedResources(assemblyRegex).FirstOrDefault();
				if (resourceTuple != null)
				{
					return resourceTuple.Item1.LoadEmbeddedAssembly(resourceTuple.Item2);
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine("Error loading {0} from manifest resources: {1}", assemblyName, ex.Message);
			}
			return null;
		}

		/// <summary>
		///     Load the specified assembly from an embedded (manifest) resource, or return null
		/// </summary>
		/// <param name="assembly">Assembly to load the resource from</param>
		/// <param name="resourceName">Name of the embedded resource for the assembly to load</param>
		/// <returns>Assembly</returns>
		public static Assembly LoadEmbeddedAssembly(this Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetEmbeddedResourceAsStream(resourceName))
			{
				return LoadAssemblyFromStream(stream);
			}
		}

		/// <summary>
		///     Load the specified assembly from the ResolveDirectories, or return null
		/// </summary>
		/// <param name="assemblyName">string with the name without path</param>
		/// <returns>Assembly</returns>
		public static Assembly LoadAssemblyFromFileSystem(string assemblyName)
		{
			return LoadAssemblyFromFileSystem(ResolveDirectories, assemblyName);
		}

		/// <summary>
		///     Load the specified assembly from the specified directories, or return null
		/// </summary>
		/// <param name="directories">IEnumerable with directories</param>
		/// <param name="assemblyName">string with the name without path</param>
		/// <returns>Assembly</returns>
		public static Assembly LoadAssemblyFromFileSystem(IEnumerable<string> directories, string assemblyName)
		{
			var assemblyRegex = AssemblyNameToRegex(assemblyName);
			var filepath = FileLocations.Scan(directories, assemblyRegex).Select(x => x.Item1).FirstOrDefault();
			if (!string.IsNullOrEmpty(filepath) && File.Exists(filepath))
			{
				try
				{
					return LoadAssemblyFromFile(filepath);
				}
				catch (Exception ex)
				{
					// don't log with other libraries as this might cause issues / recurse resolving
					Log.Error().WriteLine("Error loading assembly from file {0}: {1}", filepath, ex.Message);
				}
			}
			return null;
		}
	}
}