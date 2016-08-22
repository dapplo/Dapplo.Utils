//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2015-2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Utils
// 
//  Dapplo.Utils is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Utils is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Utils. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Linq;
using Dapplo.Log.Facade;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Extensions;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	/// Test EnumerableExtensions
	/// </summary>
	public class EnumerableExtensionsTests
	{
		public EnumerableExtensionsTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public void Test_SkipLast_SingleItem()
		{
			Assert.True(new[] {1, 2, 3, 4}.SkipLast().Last() == 3);
		}

		[Fact]
		public void Test_SkipLast_Count()
		{
			Assert.True(new[] { 1, 2, 3, 4 }.SkipLast().Count() == 3);
		}

		[Fact]
		public void Test_SkipLastN_SingleItem()
		{
			Assert.True(new[] { 1, 2, 3, 4 }.SkipLast(2).Last() == 2);
		}

		[Fact]
		public void Test_SkipLastN_Count()
		{
			Assert.True(new[] { 1, 2, 3, 4 }.SkipLast(2).Count() == 2);
		}

		[Fact]
		public void Test_SkipLastN_MoreThanAvailable()
		{
			Assert.False(new[] { 1, 2, 3, 4 }.SkipLast(8).Any());
		}

		[Fact]
		public void Test_SkipLastN_Zero()
		{
			Assert.True(new[] { 1, 2, 3, 4 }.SkipLast(0).Count() == 4);
		}

		[Fact]
		public void Test_SkipLastN_WrongArgument()
		{
			Assert.Throws<ArgumentException>(() => new[] { 1, 2, 3, 4 }.SkipLast(-8).Any());
		}
	}
}