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
using Dapplo.Log;
using Dapplo.Utils.Extensions;
using Xunit;
using Xunit.Abstractions;
using System;
using System.ComponentModel;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Tests.TestEntities;

#endregion

namespace Dapplo.Utils.Tests
{
    public class TypeExtensionsTests
    {
        public TypeExtensionsTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void TestAdd()
        {
            // Added to get better coverage
            TypeExtensions.AddDefaultConverter(typeof(TypeExtensionsTests), null);
        }

        /// <summary>
        /// This tests if we can convert an array of string to an array of uris, this doesn't work yet.
        /// </summary>
        //[Fact]
        public void TestConvertOrCastValueToType_StringArrayToUriArray()
        {
            var result = typeof(Uri[]).ConvertOrCastValueToType(new [] { "http://1.dapplo.net" , "http://2.dapplo.net" }) as Uri[];
            Assert.NotNull(result);
            Assert.Contains(new Uri("http://1.dapplo.net"), result);
            Assert.Equal(2, result.Length);
        }


        [Fact]
        public void TestConvertOrCastValueToType_NullAndEmpty()
        {
            var result = typeof(string).ConvertOrCastValueToType(null);
            Assert.Null(result);
            // Added to get better coverage
            result = TypeExtensions.ConvertOrCastValueToType<string>(null);
            Assert.Null(result);

            var stringResult = typeof(string).ConvertOrCastValueToType("", new StringConverter()) as string;
            Assert.NotNull(stringResult);
            Assert.True(stringResult.Length == 0);

            stringResult = typeof(string).ConvertOrCastValueToType("") as string;
            Assert.NotNull(stringResult);
            Assert.True(stringResult.Length == 0);
        }

        [Fact]
        public void TestConvertOrCastValueToType_StringToListOfString()
        {
            var listOfStringType = typeof(List<string>);

            var listOfStringsObject = listOfStringType.ConvertOrCastValueToType("1,2,3");
            Assert.NotNull(listOfStringsObject);
            var listOfStrings = listOfStringsObject as IList<string>;
            Assert.NotNull(listOfStrings);

            Assert.Equal(3, listOfStrings.Count);
        }

        [Fact]
        public void TestConvertOrCastValueToType_StringToListOfInt()
        {
            var listOfStringType = typeof(List<int>);

            var listOfIntssObject = listOfStringType.ConvertOrCastValueToType("1,2,3");
            Assert.NotNull(listOfIntssObject);
            var listOfInts = listOfIntssObject as IList<int>;
            Assert.NotNull(listOfInts);

            Assert.Equal(3, listOfInts.Count);
        }

        [Fact]
        public void TestConvertOrCastValueToType_StringToIListOfInt()
        {
            var listOfStringType = typeof(IList<int>);

            var listOfIntssObject = listOfStringType.ConvertOrCastValueToType("1,2,3");
            Assert.NotNull(listOfIntssObject);
            var listOfInts = listOfIntssObject as IList<int>;
            Assert.NotNull(listOfInts);

            Assert.Equal(3, listOfInts.Count);
        }

        [Fact]
        public void TestDefault()
        {
            var defaultBool = typeof(bool).Default();
            Assert.False((bool)defaultBool);
            var defaultInt = typeof(int).Default();
            Assert.Equal(0, defaultInt);
            var defaultString = typeof(string).Default();
            Assert.Null(defaultString);
            var defaultListOfString = typeof(List<string>).Default();
            Assert.Null(defaultListOfString);
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

        [Fact]
        public void TestDefaultValue()
        {
            var classWithDefaultValue = new HaveDefaultValue();
            var defaultValue = classWithDefaultValue.GetType().GetProperty(nameof(classWithDefaultValue.MyValue)).GetDefaultValue();
            Assert.Equal("CorrectValue", defaultValue);
        }
    }
}