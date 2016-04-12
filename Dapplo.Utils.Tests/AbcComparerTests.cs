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

using System.Threading.Tasks;
using Dapplo.LogFacade;
using Dapplo.Utils.Tests.Logger;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	/// Test the AbcComparer
	/// </summary>
	public class AbcComparerTests
	{
		private static readonly LogSource Log = new LogSource();

		public AbcComparerTests(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
		}

		[Fact]
		public void TestAbcComparerCompare()
		{
			var abcComparer = new AbcComparer();
			Assert.True(abcComparer.Compare("abc123", "__AbC_123!") == 0);
			Assert.True(abcComparer.Compare(null, null) == 0);
			Assert.True(abcComparer.Compare("abc123", "bcd123") < 0);
			Assert.True(abcComparer.Compare(null, "bcd123") < 0);
			Assert.True(abcComparer.Compare("bcd123", "abc123") > 0);
			Assert.True(abcComparer.Compare("bcd123", null) > 0);
		}

		[Fact]
		public void TestAbcComparerEquals()
		{
			var abcComparer = new AbcComparer();
			Assert.True(abcComparer.Equals("abc123", "__AbC_123!"));
			Assert.True(abcComparer.Equals(null, null));
			Assert.False(abcComparer.Equals(null, "bcd123"));
			Assert.False(abcComparer.Equals("abc123", null));
			Assert.False(abcComparer.Equals("abc123", "bcd123"));
			Assert.False(abcComparer.Equals("bcd123", "abc123"));
		}

		[Fact]
		public void TestAbcComparerGetHashCode()
		{
			var abcComparer = new AbcComparer();
			var hc1 = abcComparer.GetHashCode("abc123");
			Assert.Equal(hc1, abcComparer.GetHashCode("abc123"));
			Assert.Equal(hc1, abcComparer.GetHashCode("__abc123!! --§"));
			Assert.NotEqual(hc1, abcComparer.GetHashCode("bcd123"));
		}
	}
}