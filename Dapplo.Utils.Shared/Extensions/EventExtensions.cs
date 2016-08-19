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
using Dapplo.Utils.Events;

#endregion

namespace Dapplo.Utils.Extensions
{
	/// <summary>
	///     Extensions for IHasEvents and IEventObservable
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
			if (hasEvents == null)
			{
				throw new ArgumentNullException(nameof(hasEvents));
			}
			return EventObservable.RemoveEventHandlers(hasEvents, regExPattern);
		}

		/// <summary>
		///     This gives an IEnumerable of IEventObservable for the specified EventArgs type.
		/// </summary>
		/// <param name="hasEvents"></param>
		/// <typeparam name="TEventArgs">Type of the EventArgs, use EventArgs to get most events</typeparam>
		/// <returns>IEnumerable with IEventObservable</returns>
		public static IEnumerable<IEventObservable<TEventArgs>> EventsIn<TEventArgs>(this IHasEvents hasEvents)
			where TEventArgs : class
		{
			if (hasEvents == null)
			{
				throw new ArgumentNullException(nameof(hasEvents));
			}
			return EventObservable.EventsIn<TEventArgs>(hasEvents);
		}

		/// <summary>
		///     Test if an IEventObservable is for the specified event args type
		/// </summary>
		/// <param name="eventObservable"></param>
		/// <typeparam name="TEventArgs">Type of the event arguments</typeparam>
		/// <returns>bool</returns>
		public static bool Is<TEventArgs>(this IEventObservable eventObservable)
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return eventObservable.EventArgumentType == typeof(TEventArgs);
		}

		/// <summary>
		///     Does a where and is on the IEnumerable with IEventObservable to return the ones that handle events for the
		///     specified event types
		/// </summary>
		/// <param name="eventObservables">IEnumerable with IEventObservable</param>
		/// <typeparam name="TEventArgs">Type for the event args</typeparam>
		/// <returns>IEnumerable of typed IEventObservable</returns>
		public static IEnumerable<IEventObservable<TEventArgs>> For<TEventArgs>(this IEnumerable<IEventObservable> eventObservables)
			where TEventArgs : class
		{
			if (eventObservables == null)
			{
				throw new ArgumentNullException(nameof(eventObservables));
			}
			return eventObservables.Where(eo => eo.Is<TEventArgs>()).Cast<IEventObservable<TEventArgs>>();
		}

		/// <summary>
		///     Dispose all IEventObservable in the list
		/// </summary>
		/// <param name="eventObservables">IList with IEventObservable</param>
		public static void DisposeAll(this IEnumerable<IEventObservable> eventObservables)
		{
			if (eventObservables == null)
			{
				throw new ArgumentNullException(nameof(eventObservables));
			}
			foreach (var eventObservable in eventObservables)
			{
				eventObservable.Dispose();
			}
		}

		/// <summary>
		///     Call the supplied action on each event.
		/// </summary>
		/// <param name="eventObservable">IObservable</param>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <returns>IEventHandler</returns>
		public static IDisposable OnEach<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable,
			Action<IEventData<TEventArgs>> action, Func<IEventData<TEventArgs>, bool> predicate = null)
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return new DirectObserver<IEventData<TEventArgs>>(eventObservable, action, predicate);
		}


		/// <summary>
		///     This subscribes an EnumerableObserver (which implements IEnumerable) and returns this
		/// </summary>
		/// <returns>IEnumerator for IEventData of TEventArgs</returns>
		public static IEnumerable<IEventData<TEventArgs>> Subscribe<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable)
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return new EnumerableObserver<IEventData<TEventArgs>>(eventObservable);
		}

		/// <summary>
		/// Trigger, but also create the EventData
		/// </summary>
		/// <param name="eventObservable">IEventObservable for TEventArgs</param>
		/// <param name="eventArgs">TEventArgs</param>
		/// <param name="sender">Optional sender</param>
		/// <param name="name"></param>
		/// <typeparam name="TEventArgs">Type of the EventArgs</typeparam>
		public static bool Trigger<TEventArgs>(this IEventObservable<TEventArgs> eventObservable, TEventArgs eventArgs, object sender = null, string name = null) where TEventArgs : EventArgs
		{
			var eventData = EventData.Create(sender, eventArgs, name);
			return eventObservable.Trigger(eventData);
		}

		/// <summary>
		/// Trigger, but also create the EventData with an empty TEventArgs (must have a default constructor)
		/// </summary>
		/// <param name="eventObservable">IEventObservable for TEventArgs</param>
		/// <param name="sender">Optional sender</param>
		/// <param name="name">Optional Name of the event, will be set if null and available in the IEventObservable</param>
		/// <typeparam name="TEventArgs">Type of the EventArgs</typeparam>
		public static bool Trigger<TEventArgs>(this IEventObservable<TEventArgs> eventObservable, object sender = null, string name = null) where TEventArgs : EventArgs, new()
		{
			var eventData = EventData.Create(sender, name);
			return eventObservable.Trigger(eventData);
		}
	}
}