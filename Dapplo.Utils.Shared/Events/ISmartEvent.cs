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

		/// <summary>
		///     Non generic action, which can be used to subscribe to unknown types.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="predicate"></param>
		/// <returns>IEventHandler</returns>
		IEventHandler OnEach<TEventArgs>(Action<object, TEventArgs> action, Func<object, TEventArgs, bool> predicate = null);
	}

	/// <summary>
	///     This is the interface to a SmartEvent.
	/// </summary>
	/// <typeparam name="TEventArgs">type for the underlying EventHandler</typeparam>
	public interface ISmartEvent<TEventArgs> : ISmartEvent
	{
		/// <summary>
		///     Get an IEnumerable with eventargs for the underlying event
		/// </summary>
		/// <returns>IEnumerable with eventargs</returns>
		IEnumerable<TEventArgs> From { get; }

		/// <summary>
		///     Get an IEnumerable for the underlying event
		/// </summary>
		/// <returns>IEnumerable with tuple sender,eventargs</returns>
		IEnumerable<Tuple<object, TEventArgs>> FromExtended { get; }

		/// <summary>
		///     Trigger the underlying event
		/// </summary>
		/// <param name="sender">object for sender</param>
		/// <param name="eventArgs">TEventArgs</param>
		void Trigger(object sender, TEventArgs eventArgs);

		/// <summary>
		///     Call the supplied action on each event.
		/// </summary>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <returns>IEventHandler</returns>
		IEventHandler OnEach(Action<TEventArgs> action, Func<TEventArgs, bool> predicate = null);

		/// <summary>
		///     Call the supplied action on each event.
		/// </summary>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <returns>IEventHandler</returns>
		IEventHandler OnEach(Action<object, TEventArgs> action, Func<object, TEventArgs, bool> predicate = null);

		/// <summary>
		///     Process events (IEnumerable with tuple sender,eventargs) in a background task, the task will only finish on an
		///     exception or if the function returns
		/// </summary>
		/// <typeparam name="TResult">Type of the result</typeparam>
		/// <param name="processFunc">Function which will process the IEnumerable</param>
		/// <returns>Task with the result of the function</returns>
		Task<TResult> ProcessExtendedAsync<TResult>(Func<IEnumerable<Tuple<object, TEventArgs>>, TResult> processFunc);

		/// <summary>
		///     Process events (IEnumerable with eventargs) in a background task, the task will only finish on an exception or if
		///     the function returns
		/// </summary>
		/// <typeparam name="TResult">Type of the result</typeparam>
		/// <param name="processFunc">Function which will process the IEnumerable</param>
		/// <returns>Task with the result of the function</returns>
		Task<TResult> ProcessAsync<TResult>(Func<IEnumerable<TEventArgs>, TResult> processFunc);

		/// <summary>
		///     Process events (IEnumerable with tuple sender,eventargs) in a background task, the task will only finish on an
		///     exception
		/// </summary>
		/// <param name="processAction">Action which will process the IEnumerable</param>
		/// <returns>Task</returns>
		Task ProcessExtendedAsync(Action<IEnumerable<Tuple<object, TEventArgs>>> processAction);

		/// <summary>
		///     Process events (IEnumerable with eventargs) in a background task, the task will only finish on an exception
		/// </summary>
		/// <param name="processAction">Action which will process the IEnumerable</param>
		/// <returns>Task</returns>
		Task ProcessAsync(Action<IEnumerable<TEventArgs>> processAction);
	}

	/// <summary>
	///     The interface which is used internally
	/// </summary>
	/// <typeparam name="TEventArgs"></typeparam>
	public interface IInternalSmartEvent<TEventArgs> : ISmartEvent<TEventArgs>
	{
		/// <summary>
		///     The IInternalEventHandler want to subscribe
		/// </summary>
		/// <param name="eventHandler"></param>
		void Subscribe(IInternalEventHandler<TEventArgs> eventHandler);

		/// <summary>
		///     The IInternalEventHandler wants to unsubscribe
		/// </summary>
		/// <param name="eventHandler"></param>
		void Unsubscribe(IInternalEventHandler<TEventArgs> eventHandler);
	}
}