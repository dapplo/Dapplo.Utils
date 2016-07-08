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

using System;
using System.Collections.Generic;

#endregion

namespace Dapplo.Utils.Enumerable
{
	/// <summary>
	///     Simple IEnumerable extensions.
	///     These are in a different package so it doesn't hinder other IEnumerable extensions.
	/// </summary>
	public static class EnumerableExtensions
	{
		/// <summary>
		///     As the BCL doesn't include a ForEach on the IEnumerable, this extension was added to help a few border cases
		/// </summary>
		/// <typeparam name="T">Type of the IEnumerable</typeparam>
		/// <param name="source">The IEnumerable</param>
		/// <param name="action">Action to call for each item</param>
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			foreach (var item in source)
			{
				action(item);
			}
		}
	}
}