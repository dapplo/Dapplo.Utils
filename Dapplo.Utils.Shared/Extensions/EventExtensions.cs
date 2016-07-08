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

using Dapplo.Utils.Events;

#endregion

namespace Dapplo.Utils.Extensions
{
	/// <summary>
	/// Extensions for IHasEvents
	/// </summary>
	public static class EventExtensions
	{
		/// <summary>
		///     Removes all the event handlers on a IHasEvents
		///     This is usefull to do internally, after a clone is made, to prevent memory leaks
		/// </summary>
		/// <param name="hasEvents">IHasEvents instance</param>
		/// <param name="regExPattern">Regular expression to match the even names, null for alls</param>
		public static void RemoveEventHandlers(this IHasEvents hasEvents, string regExPattern = null)
		{
			EventObservable.RemoveEventHandlers(hasEvents, regExPattern);
		}
	}
}