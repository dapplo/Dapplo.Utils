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

using System.ComponentModel;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

#endregion

namespace Dapplo.Utils.Tests.TestEntities
{
	/// <summary>
	/// Class used for testing the PropertyInfoExtensions
	/// </summary>
	public class PropertiesClass
	{
		[Category("Dapplo")]
		[DataMember(Name = "name", EmitDefaultValue = true)]
		[DefaultValue("Robin")]
		[Description("This is a description")]
		[ReadOnly(true)]
		[TypeConverter(typeof(StringConverter))]
		public string Name { get; set; }

		[Display(Description = "This is also a description")]
		public string Name2 { get; set; }

		public int Age { get; set; }
	}
}