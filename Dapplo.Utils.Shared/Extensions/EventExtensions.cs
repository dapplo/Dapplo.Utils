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
		/// <returns>number of removed event handlers</returns>
		public static int RemoveEventHandlers(this IHasEvents hasEvents, string regExPattern = null)
		{
			return EventObservable.RemoveEventHandlers(hasEvents, regExPattern);
		}

		/// <summary>
		/// This gives an IEnumerable of IEventObservable for the specified EventArgs type. 
		/// </summary>
		/// <param name="hasEvents"></param>
		/// <typeparam name="TEventArgs">Type of the EventArgs, use EventArgs to get most events</typeparam>
		/// <returns>IEnumerable with IEventObservable</returns>
		public static IEnumerable<IEventObservable<TEventArgs>> EventsIn<TEventArgs>(this IHasEvents hasEvents)
		{
			return EventObservable.EventsIn<TEventArgs>(hasEvents);
		}
	}
}