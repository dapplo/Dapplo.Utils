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

using Dapplo.Log.Facade;
using Dapplo.Utils.Extensions;
using Xunit;
using Xunit.Abstractions;
using System.ComponentModel;
using System.Reflection;
using Dapplo.Utils.Tests.TestEntities;
using Dapplo.Log.XUnit;

#endregion

namespace Dapplo.Utils.Tests
{
	public class PropertyInfoExtensionsTests
	{
		private readonly PropertyInfo _propertyInfoName = typeof(PropertiesClass).GetProperty("Name");
		private readonly PropertyInfo _propertyInfoName2 = typeof(PropertiesClass).GetProperty("Name2");

		public PropertyInfoExtensionsTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public void TestCategory()
		{
			var category = _propertyInfoName.GetCategory();
			Assert.Equal("Dapplo", category);
		}

		[Fact]
		public void TestDataMemberName()
		{
			var dataMemberName = _propertyInfoName.GetDataMemberName();
			Assert.Equal("name", dataMemberName);
		}

		[Fact]
		public void TestDataMemberName_Null()
		{
			// Request the DataMemberName for a not annotated property
			var dataMemberName = _propertyInfoName2.GetDataMemberName();
			Assert.Null(dataMemberName);
		}

		[Fact]
		public void TestDefaultValue()
		{
			var defaultValue = _propertyInfoName.GetDefaultValue();
			Assert.Equal("Robin", defaultValue);
		}

		[Fact]
		public void TestDefaultValue_ValueTypeWithoutAnnotation()
		{
			var defaultValue = typeof(PropertiesClass).GetProperty("Age").GetDefaultValue();
			Assert.Null(defaultValue);
		}

		[Fact]
		public void TestDefaultValue_ReferenceTypeStringWithoutAnnotation()
		{
			var defaultValue = _propertyInfoName2.GetDefaultValue();
			Assert.Null(defaultValue);
		}

		[Fact]
		public void TestDescription()
		{
			var description = _propertyInfoName.GetDescription();
			Assert.Equal("This is a description", description);
			// Test display attribute
			var description2 = _propertyInfoName2.GetDescription();
			Assert.Equal("This is also a description", description2);
		}

		[Fact]
		public void TestEmitDefaultValue()
		{
			var emitDefaultValue = _propertyInfoName.GetEmitDefaultValue();
			Assert.True(emitDefaultValue);
		}

		[Fact]
		public void TestEmitDefaultValue_NoAnnotation()
		{
			var emitDefaultValue = _propertyInfoName2.GetEmitDefaultValue();
			Assert.False(emitDefaultValue);
		}

		[Fact]
		public void TestReadOnly()
		{
			var readOnly = _propertyInfoName.GetReadOnly();
			Assert.True(readOnly);
		}

		[Fact]
		public void TestTypeConverter()
		{
			var typeConverter = _propertyInfoName.GetTypeConverter();
			Assert.Equal(typeof(StringConverter), typeConverter.GetType());
		}

		[Fact]
		public void TestTypeConverter_NotSpecified_Create()
		{
			var typeConverter = _propertyInfoName2.GetTypeConverter(true);
			Assert.Equal(typeof(StringConverter), typeConverter.GetType());
		}

		[Fact]
		public void TestTypeConverter_NotSpecified()
		{
			var typeConverter = _propertyInfoName2.GetTypeConverter();
			Assert.Null(typeConverter);
		}
	}
}