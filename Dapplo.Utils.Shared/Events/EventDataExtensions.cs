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

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     Helper extension for IEventData
	/// </summary>
	public static class EventDataExtensions
	{
		/// <summary>
		///     Use this to flatten the event data to only the TEventArgs
		/// </summary>
		/// <param name="eventEnumerable">IEnumerable with IEventData</param>
		/// <typeparam name="TEventArgs">Type of the event arguments</typeparam>
		/// <returns>IEnumerable with TEventArgs</returns>
		public static IEnumerable<TEventArgs> Flatten<TEventArgs>(this IEnumerable<IEventData<TEventArgs>> eventEnumerable)
			where TEventArgs : class
		{
			return eventEnumerable.Select(x => x.Args);
		}
	}
}