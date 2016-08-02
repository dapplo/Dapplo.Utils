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

using System.IO;
using System.Linq;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Embedded;
using Dapplo.Utils.Resolving;
using Dapplo.Utils.Tests.TestAssembly;
using Dapplo.Utils.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	/// This tests the Assembly resolve functionality.
	/// </summary>
	public class AssemblyResolveTests
	{
		public AssemblyResolveTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public void TestResolving()
		{
			// Tests don't have an entry assembly, force it with this helper
			AssemblyUtils.SetEntryAssembly(GetType().Assembly);

			var assemblyName = "Dapplo.Utils.Tests.TestAssembly";
			var dllName = assemblyName + ".dll";
			// Make sure the dll is NOT available on the file system, otherwise the test won't go into the resolver
			if (File.Exists(dllName))
			{
				File.Delete(dllName);
			}


			// Register OUR AssemblyResolver, not the one of ILBundle.
			using (AssemblyResolver.RegisterAssemblyResolve())
			{
				var regex = AssemblyResolver.FilenameToRegex(assemblyName);

				// Check that the assembly can be found in the embedded resources
				var assemblyFiles = GetType().FindEmbeddedResources(regex);
				Assert.True(assemblyFiles.Any());

				// Now force the usage of the other assembly, this should load it and call the method
				ThisForcesDelayedLoadingOfAssembly();

				// TODO: Test i the cache-hit works, so I don't need to check the logs :)
				// Load it again, to see if the Name-Cache kicks in and it's not loaded 2x
				AssemblyResolver.LoadEmbeddedAssembly(assemblyName);
				// Load it again, to see if the cache kicks in and it's not loaded 2x
				AssemblyResolver.CheckEmbeddedResourceNameAgainstCache = false;
				AssemblyResolver.LoadEmbeddedAssembly(assemblyName);
			}
		}

		private void ThisForcesDelayedLoadingOfAssembly()
		{
			var helloWorld = ExternalClass.HelloWord();
			Assert.Equal(nameof(ExternalClass.HelloWord), helloWorld);
		}

		[Fact]
		public void Test_AssemblyNameToRegex()
		{
			var file_noMatch = @"C:\Project\Dapplo.Addons\Dapplo.Addons.Tests\bin\Debug\xunit.execution.desktop.dll";
			var file_match = @"C:\Project\blub\bin\Debug\Dapplo.something.dll";
			var regex = AssemblyResolver.FilenameToRegex("Dapplo*");
			Assert.False(regex.IsMatch(file_noMatch));
			Assert.True(regex.IsMatch(file_match));
		}
	}
}