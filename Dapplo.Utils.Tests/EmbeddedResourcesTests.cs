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

using System.Linq;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Embedded;
using Dapplo.Utils.Tests.Cache;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	public class EmbeddedResourcesTests
	{
		public EmbeddedResourcesTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		/// <summary>
		/// Test if resources can be found
		/// </summary>
		[Fact]
		public void Test_FindEmbeddedResources()
		{
			var resources = GetType().FindEmbeddedResources(@"embedded-dapplo.png");
			Assert.True(resources.Any());
		}

		/// <summary>
		/// Test if finding and loading from the manifest works
		/// </summary>
		[Fact]
		public void Test_GetEmbeddedResourceAsStream()
		{
			
			using (var stream = GetType().Assembly.GetEmbeddedResourceAsStream(@"TestFiles\embedded-dapplo.png"))
			{
				var bitmap = stream.ImageFromStream();
				Assert.NotNull(bitmap.Width);
				Assert.True(bitmap.Width > 0);
			}
		}

		/// <summary>
		/// Test if gunzip works
		/// </summary>
		[Fact]
		public void Test_GetEmbeddedResourceAsStream_GZ()
		{

			using (var stream = GetType().Assembly.GetEmbeddedResourceAsStream(@"TestFiles\embedded-dapplo.png.gz"))
			{
				var bitmap = stream.ImageFromStream();
				Assert.NotNull(bitmap.Width);
				Assert.True(bitmap.Width > 0);
			}
		}
	}
}