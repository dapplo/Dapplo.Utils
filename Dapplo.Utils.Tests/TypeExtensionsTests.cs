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
using Dapplo.Utils.Extensions;
using Dapplo.Utils.Tests.Logger;
using Xunit;
using Xunit.Abstractions;
using System;

#endregion

namespace Dapplo.Utils.Tests
{
	public class TypeExtensionsTests
	{
		public TypeExtensionsTests(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
		}

		[Fact]
		public void TestConvertOrCastValueToType_StringToListOfString()
		{
			var listOfStringType = typeof(List<string>);

			var listOfStringsObject = listOfStringType.ConvertOrCastValueToType("1,2,3");
			Assert.NotNull(listOfStringsObject);
			var listOfStrings = listOfStringsObject as IList<string>;
			Assert.NotNull(listOfStrings);

			Assert.NotNull(listOfStrings?.Count == 3);
		}

		[Fact]
		public void TestConvertOrCastValueToType_StringToListOfInt()
		{
			var listOfStringType = typeof(List<int>);

			var listOfIntssObject = listOfStringType.ConvertOrCastValueToType("1,2,3");
			Assert.NotNull(listOfIntssObject);
			var listOfInts = listOfIntssObject as IList<int>;
			Assert.NotNull(listOfInts);

			Assert.NotNull(listOfInts?.Count == 3);
		}

		[Fact]
		public void TestConvertOrCastValueToType_StringToIListOfInt()
		{
			var listOfStringType = typeof(IList<int>);

			var listOfIntssObject = listOfStringType.ConvertOrCastValueToType("1,2,3");
			Assert.NotNull(listOfIntssObject);
			var listOfInts = listOfIntssObject as IList<int>;
			Assert.NotNull(listOfInts);

			Assert.NotNull(listOfInts?.Count == 3);
		}

		[Fact]
		public void TestDefault()
		{
			var defaultBool = typeof(bool).Default();
			Assert.Equal(false, defaultBool);
			var defaultInt = typeof(int).Default();
			Assert.Equal(0, defaultInt);
			var defaultString = typeof(string).Default();
			Assert.Equal(null, defaultString);
			var defaultListOfString = typeof(List<string>).Default();
			Assert.Equal(null, defaultListOfString);
		}

		[Fact]
		public void TestFriendlyName()
		{
			Assert.Equal("string", typeof(string).FriendlyName());
			Assert.Equal("int[]", typeof(int[]).FriendlyName());
			Assert.Equal("int[][]", typeof(int[][]).FriendlyName());
			Assert.Equal("KeyValuePair<int, string>", typeof(KeyValuePair<int, string>).FriendlyName());
			Assert.Equal("Tuple<int, string>", typeof(Tuple<int, string>).FriendlyName());
			Assert.Equal("Tuple<KeyValuePair<object, long>, string>", typeof(Tuple<KeyValuePair<object, long>, string>).FriendlyName());
			Assert.Equal("List<Tuple<int, string>>", typeof(List<Tuple<int, string>>).FriendlyName());
			Assert.Equal("Tuple<short[], string>", typeof(Tuple<short[], string>).FriendlyName());
		}
	}
}