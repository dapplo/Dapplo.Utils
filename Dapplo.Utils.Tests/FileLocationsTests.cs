﻿#region Dapplo 2016 - GNU Lesser General Public License

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

using System.Text.RegularExpressions;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	public class FileLocationsTests
	{
		public FileLocationsTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public void TestRoamingAppData()
		{
			var roamingAppDataDirectory = FileLocations.RoamingAppDataDirectory("Dapplo");
			Assert.EndsWith(@"AppData\Roaming\Dapplo", roamingAppDataDirectory);
		}

		[Fact]
		public void TestScan()
		{
			var startupDirectory = FileLocations.StartupDirectory;
			var files = FileLocations.Scan(new[] {startupDirectory}, "*.xml");
			Assert.Contains(files, file => file.EndsWith("Dapplo.Utils.xml"));
		}

		[Fact]
		public void TestScanRegex()
		{
			var startupDirectory = FileLocations.StartupDirectory;
			var files = FileLocations.Scan(new[] {startupDirectory}, new Regex(@".*\.xml"));
			Assert.Contains(files, file => file.Item1.EndsWith("Dapplo.Utils.xml"));
		}
	}
}