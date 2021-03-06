﻿//  Dapplo - building blocks for desktop applications
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


namespace Dapplo.Utils
{
	/// <summary>
	/// The interface for the ShallowClone method.
	/// </summary>
	public interface IShallowCloneable
	{
		/// <summary>
		/// Make a memberwise clone of the object, this is "shallow".
		/// </summary>
		/// <returns>"Shallow" Cloned instance</returns>
		object ShallowClone();
	}

	/// <summary>
	/// The interface for the generic ShallowClone method.
	/// </summary>
	/// <typeparam name="T">Type of the copy which is returned</typeparam>
	public interface IShallowCloneable<out T> where T : class
	{
		/// <summary>
		/// Make a memberwise clone of the object, this is "shallow".
		/// </summary>
		/// <returns>"Shallow" Cloned instance of type T</returns>
		T ShallowClone();
	}
}
