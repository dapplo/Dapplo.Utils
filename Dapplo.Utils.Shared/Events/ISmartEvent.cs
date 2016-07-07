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
using System.Threading.Tasks;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     Base marker interface for the SmartEvent
	/// </summary>
	public interface ISmartEvent : IDisposable
	{
		/// <summary>
		///     The name of the underlying event, might be null if not supplied
		/// </summary>
		string EventName { get; }
	}

	/// <summary>
	///     This is the interface to a SmartEvent.
	/// </summary>
	/// <typeparam name="TEventArgs">type for the underlying EventHandler</typeparam>
	public interface ISmartEvent<out TEventArgs> : ISmartEvent, IObservable<IEventData<TEventArgs>>
	{
		/// <summary>
		///     Get an IEnumerable for the underlying event
		/// </summary>
		/// <returns>IEnumerable with IEventData</returns>
		IEnumerable<IEventData<TEventArgs>> From { get; }

		/// <summary>
		///     Trigger the underlying event
		/// </summary>
		/// <param name="eventData">IEventData with all the data about the event</param>
		void Trigger(IEventData<EventArgs> eventData);

		/// <summary>
		///     Call the supplied action on each event.
		/// </summary>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <returns>IEventHandler</returns>
		IEventHandler OnEach(Action<IEventData<TEventArgs>> action, Func<IEventData<TEventArgs>, bool> predicate = null);

		/// <summary>
		///     Process events (IEnumerable with tuple sender,eventargs) in a background task, the task will only finish on an
		///     exception or if the function returns
		/// </summary>
		/// <typeparam name="TResult">Type of the result</typeparam>
		/// <param name="processFunc">Function which will process the IEnumerable</param>
		/// <returns>Task with the result of the function</returns>
		Task<TResult> ProcessAsync<TResult>(Func<IEnumerable<IEventData<TEventArgs>>, TResult> processFunc);

		/// <summary>
		///     Process events (IEnumerable with tuple sender,eventargs) in a background task, the task will only finish on an
		///     exception
		/// </summary>
		/// <param name="processAction">Action which will process the IEnumerable</param>
		/// <returns>Task</returns>
		Task ProcessAsync(Action<IEnumerable<IEventData<TEventArgs>>> processAction);
	}
}