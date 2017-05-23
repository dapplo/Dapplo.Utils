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
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;

#endregion

namespace Dapplo.Utils.Notify
{
	/// <summary>
	///     Extensions for IHaveEvents and IEventObservable
	/// </summary>
	public static class HaveEventsExtensions
	{
		private const BindingFlags AllBindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>
		///     This gives an IEnumerable of IObservable for the specified EventArgs type.
		/// </summary>
		/// <param name="haveEvents"></param>
		/// <typeparam name="TEventArgs">Type of the EventArgs, use EventArgs to get most events</typeparam>
		/// <returns>IEnumerable with IObservable</returns>
		public static IEnumerable<IObservable<EventPattern<TEventArgs>>> EventsIn<TEventArgs>(this IHaveEvents haveEvents)
			where TEventArgs : class
		{
			if (haveEvents == null)
			{
				throw new ArgumentNullException(nameof(haveEvents));
			}
			return EventsInObject<TEventArgs>(haveEvents);
		}

		/// <summary>
		/// Create an IEnumerable with IObservable for every event in the target object
		/// </summary>
		/// <param name="targetObject">object</param>
		/// <returns>IList of IObservable which can be dispose with DisposeAll</returns>
		public static IEnumerable<IObservable<EventPattern<TEventArgs>>> EventsInObject<TEventArgs>(object targetObject) where TEventArgs : class
		{
			return targetObject.GetType().GetEvents(AllBindings).Select(eventInfo => Observable.FromEventPattern<TEventArgs>(targetObject, eventInfo.Name));
		}
	}
}