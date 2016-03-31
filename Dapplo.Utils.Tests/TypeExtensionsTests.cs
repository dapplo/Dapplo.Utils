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

using System.Collections.Generic;
using Dapplo.LogFacade;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	public class TypeExtensionsTests
	{
		private static readonly LogSource Log = new LogSource();

		public TypeExtensionsTests(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
		}

		[Fact]
		public void TestConvertOrCastValueToType_StringToListOfString()
		{
			var listOfStringType = typeof(List<string>);

			var listOfStringsObject = listOfStringType.ConvertOrCastValueToType("1,2,3", null);
			Assert.NotNull(listOfStringsObject);
			var listOfStrings = listOfStringsObject as IList<string>;
			Assert.NotNull(listOfStrings);

			Assert.NotNull(listOfStrings.Count == 3);
		}

		[Fact]
		public void TestConvertOrCastValueToType_StringToListOfInt()
		{
			var listOfStringType = typeof(List<int>);

			var listOfIntssObject = listOfStringType.ConvertOrCastValueToType("1,2,3", null);
			Assert.NotNull(listOfIntssObject);
			var listOfInts = listOfIntssObject as IList<int>;
			Assert.NotNull(listOfInts);

			Assert.NotNull(listOfInts.Count == 3);
		}

		[Fact]
		public void TestConvertOrCastValueToType_StringToIListOfInt()
		{
			var listOfStringType = typeof(IList<int>);

			var listOfIntssObject = listOfStringType.ConvertOrCastValueToType("1,2,3", null);
			Assert.NotNull(listOfIntssObject);
			var listOfInts = listOfIntssObject as IList<int>;
			Assert.NotNull(listOfInts);

			Assert.NotNull(listOfInts.Count == 3);
		}
	}
}