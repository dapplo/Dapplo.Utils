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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Utils.Extensions;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     Extensions for IHaveEvents and IEventObservable
	/// </summary>
	public static class EventExtensions
	{
		/// <summary>
		///     Removes all the event handlers on a IHaveEvents
		///     This is usefull to do internally, after a clone is made, to prevent memory leaks
		/// </summary>
		/// <param name="haveEvents">IHaveEvents instance</param>
		/// <param name="regExPattern">Regular expression to match the even names, null for alls</param>
		/// <returns>number of removed event handlers</returns>
		public static int RemoveEventHandlers(this IHaveEvents haveEvents, string regExPattern = null)
		{
			if (haveEvents == null)
			{
				throw new ArgumentNullException(nameof(haveEvents));
			}
			return EventObservable.RemoveEventHandlers(haveEvents, regExPattern);
		}

		/// <summary>
		///     This gives an IEnumerable of IEventObservable for the specified EventArgs type.
		/// </summary>
		/// <param name="haveEvents"></param>
		/// <typeparam name="TEventArgs">Type of the EventArgs, use EventArgs to get most events</typeparam>
		/// <returns>IEnumerable with IEventObservable</returns>
		public static IEnumerable<IEventObservable<TEventArgs>> EventsIn<TEventArgs>(this IHaveEvents haveEvents)
			where TEventArgs : class
		{
			if (haveEvents == null)
			{
				throw new ArgumentNullException(nameof(haveEvents));
			}
			return EventObservable.EventsIn<TEventArgs>(haveEvents);
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
		///     Filters the elements of an IEnumerable with IEventObservable based on a specified type.
		/// </summary>
		/// <param name="eventObservables">IEnumerable with IEventObservable</param>
		/// <typeparam name="TEventArgs">Type for the event args</typeparam>
		/// <returns>IEnumerable of typed IEventObservable with TEventArgs</returns>
		public static IEnumerable<IEventObservable<TEventArgs>> OfType<TEventArgs>(this IEnumerable<IEventObservable> eventObservables)
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
		/// <param name="eventObservables">IEnumerable with IEventObservable</param>
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
		///     This creates an DirectObserver, and registers this to the passed IObservable.
		///     The DirectObserver will use the optional predicate on each "message" from the IObservable, and call your action.
		///     This continues as long as the IObservable lives or until you dispose the IDisposable, or cancel the CancellationToken
		/// </summary>
		/// <param name="eventObservable">IObservable</param>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <param name="cancellationToken">CancellationToken to cancel the processing</param>
		/// <returns>IDisposable, call dispose to stop the registration.</returns>
		public static IDisposable ForEach<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable, Action<IEventData<TEventArgs>> action, Func<IEventData<TEventArgs>, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return new DirectObserver<IEventData<TEventArgs>>(eventObservable, action, predicate, cancellationToken);
		}

		/// <summary>
		///     This subscribes an EnumerableObserver (which implements IEnumerable), to the passed IObservable, and processes the results via ForEachAsync
		///     When the iteration of the IEnumerable is stopped, in this case via the CancellationToken, it will automatically unsubscribe.
		/// </summary>
		/// <param name="eventObservable">IObservable</param>
		/// <param name="action">Action to call for each (matching) item</param>
		/// <param name="predicate">optional predicate Func</param>
		/// <param name="cancellationToken">CancellationToken, can be used to cancel the ForEach processing</param>
		/// <returns>IEnumerator for IEventData of TEventArgs</returns>
		public static async Task ForEachSync<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable, Action<IEventData<TEventArgs>> action, Func<IEventData<TEventArgs>, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			await new EnumerableObserver<IEventData<TEventArgs>>(eventObservable, cancellationToken).ForEachAsync(action, predicate, cancellationToken);
		}

		/// <summary>
		///     This subscribes an EnumerableObserver (which implements IEnumerable), to the passed IObservable, and returns this.
		///     When the iteration of the IEnumerable is stopped, it will automatically unsubscribe.
		/// </summary>
		/// <returns>IEnumerator for IEventData of TEventArgs</returns>
		public static IEnumerable<IEventData<TEventArgs>> Subscribe<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return new EnumerableObserver<IEventData<TEventArgs>>(eventObservable, cancellationToken);
		}

		/// <summary>
		///     Determines whether any event satisfies a condition, or when the predicate is left out, is created at all.
		///     This subscribes an IObservable and waits until an event happens, optionally it needs to match the predicate.
		///     As soon as the function returns, the subscription is removed automatically.
		/// </summary>
		/// <returns>true if there was a matching event</returns>
		public static Task<bool> AnyAsync<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable, Func<IEventData<TEventArgs>, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return eventObservable.Subscribe(cancellationToken).AnyAsync(predicate, cancellationToken);
		}

		/// <summary>
		///     This subscribes an IObservable and returns the first event matching the optional predicate.
		///     As soon as the function returns, the subscription is removed automatically.
		/// </summary>
		/// <returns>IEventData with TEventArgs</returns>
		public static Task<IEventData<TEventArgs>> FirstAsync<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable, Func<IEventData<TEventArgs>, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return eventObservable.Subscribe(cancellationToken).FirstAsync(predicate, cancellationToken);
		}

		/// <summary>
		///     This subscribes an IObservable and returns the count of the events.
		///     As soon as the function returns, or cancelled, the subscription is removed automatically.
		/// </summary>
		/// <returns>int</returns>
		public static Task<int> CountAsync<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable, Func<IEventData<TEventArgs>, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return eventObservable.Subscribe(cancellationToken).CountAsync(predicate, cancellationToken);
		}

		/// <summary>
		///     Use this to flatten the event data to only the TEventArgs
		/// </summary>
		/// <param name="eventObservable">IObservable with IEventData</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <typeparam name="TEventArgs">Type of the event arguments</typeparam>
		/// <returns>IEnumerable with TEventArgs</returns>
		public static IEnumerable<TEventArgs> Flatten<TEventArgs>(this IObservable<IEventData<TEventArgs>> eventObservable, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			return eventObservable.Subscribe(cancellationToken).Flatten();
		}

		/// <summary>
		/// Projects each element of a sequence, coming from an IObservable, into a new form.
		/// </summary>
		/// <param name="eventObservable">IEnumerable of type TSource</param>
		/// <param name="selector">Func to project the IEnumerable iitems in TResult</param>
		/// <param name="cancellationToken">optional CancellationToken</param>
		/// <typeparam name="TResult">The type for the result</typeparam>
		/// <typeparam name="TEventArgs">The type of the IEnumerable</typeparam>
		/// <returns>IEnumerable of TResult</returns>
		public static IEnumerable<TResult> Select<TEventArgs, TResult>(this IObservable<IEventData<TEventArgs>> eventObservable, Func<IEventData<TEventArgs>, TResult> selector, CancellationToken cancellationToken = default(CancellationToken))
			where TEventArgs : class
		{
			if (eventObservable == null)
			{
				throw new ArgumentNullException(nameof(eventObservable));
			}
			return eventObservable.Subscribe(cancellationToken).Select(selector);
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
			var eventData = EventData.Create<TEventArgs>(sender, name);
			return eventObservable.Trigger(eventData);
		}
	}
}