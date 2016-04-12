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

using Dapplo.LogFacade;
using Dapplo.Utils.Extensions;
using Dapplo.Utils.Tests.Logger;
using Xunit;
using Xunit.Abstractions;
using System.ComponentModel;
using System.Reflection;
using Dapplo.Utils.Tests.TestEntities;

#endregion

namespace Dapplo.Utils.Tests
{
	public class PropertyInfoExtensionsTests
	{
		private readonly PropertyInfo _propertyInfo = typeof(PropertiesClass).GetProperty("Name");

		public PropertyInfoExtensionsTests(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
		}

		[Fact]
		public void TestCategory()
		{
			var category = _propertyInfo.GetCategory();
			Assert.Equal("Dapplo", category);
		}

		[Fact]
		public void TestDataMemberName()
		{
			var dataMemberName = _propertyInfo.GetDataMemberName();
			Assert.Equal("name", dataMemberName);
		}

		[Fact]
		public void TestDefaultValue()
		{
			var defaultValue = _propertyInfo.GetDefaultValue();
			Assert.Equal("Robin", defaultValue);
		}

		[Fact]
		public void TestDescription()
		{
			var description = _propertyInfo.GetDescription();
			Assert.Equal("This is a description", description);
		}

		[Fact]
		public void TestEmitDefaultValue()
		{
			var emitDefaultValue = _propertyInfo.GetEmitDefaultValue();
			Assert.True(emitDefaultValue);
		}

		[Fact]
		public void TestReadOnly()
		{
			var readOnly = _propertyInfo.GetReadOnly();
			Assert.True(readOnly);
		}

		[Fact]
		public void TestTypeConverter()
		{
			var typeConverter = _propertyInfo.GetTypeConverter();
			Assert.Equal(typeof(StringConverter), typeConverter.GetType());
		}
	}
}