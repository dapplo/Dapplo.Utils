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

			var dllName = @"Dapplo.Utils.Tests.TestAssembly.dll";
			// Make sure the dll is NOT available on the file system, otherwise the test won't go into the resolver
			if (File.Exists(dllName))
			{
				File.Delete(dllName);
			}


			// Register OUR AssemblyResolver, not the one of ILBundle.
			using (AssemblyResolver.RegisterAssemblyResolve())
			{
				// Check that the DLL is there
				var dll = GetType().FindEmbeddedResources(dllName);
				Assert.True(dll.Any());

				// Now force the usage of the other assembly, this should load it and call the method
				ThisForcesDelayedLoadingOfAssembly();
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
			var file_noMatch = @"C:\LocalData\CSharpProjects\Dapplo.Addons\Dapplo.Addons.Tests\bin\Debug\xunit.execution.desktop.dll";
			var regex = AssemblyResolver.FilenameToRegex("Dapplo*");
			Assert.False(regex.IsMatch(file_noMatch));
		}
	}
}