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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Utils.Events
{
	/// <summary>
	/// 
	/// </summary>
	public interface ISmartEventHandler<TEventArgs>
	{
		/// <summary>
		/// Should only the first match be handled?
		/// </summary>
		bool First { get;}

		/// <summary>
		/// Make sure the Do Action is run on the UI thread
		/// </summary>
		ISmartEventHandler<TEventArgs> OnUi { get; }

		/// <summary>
		/// Does the Do Action need to run on the UI thread
		/// </summary>
		bool NeedsUi { get; }

		/// <summary>
		/// Set the action which is executed on an event, if a when predicate is set this will be checked first
		/// </summary>
		/// <param name="doAction">action which is passed the sender and event arguments</param>
		/// <returns>ISmartEventHandler (this)</returns>
		ISmartEventHandler<TEventArgs> Do(Action<object, TEventArgs> doAction);

		/// <summary>
		/// Set the predicate which decides if the event handler needs to react.
		/// </summary>
		/// <param name="predicate">function which returns a bool depending on the passed sender and event arguments</param>
		/// <returns>ISmartEventHandler (this)</returns>
		ISmartEventHandler<TEventArgs> When(Func<object, TEventArgs, bool> predicate);

		/// <summary>
		/// Start the ISmartEventHandler by registering the underlying event
		/// </summary>
		/// <returns>ISmartEvent (parent)</returns>
		ISmartEvent<TEventArgs> Start();

		/// <summary>
		/// Test if the DoAtion can be called
		/// </summary>
		Func<object, TEventArgs, bool> Predicate { get; }

		/// <summary>
		/// The registered Action, this does whatever needs to be done when the event triggers
		/// </summary>
		Action<object, TEventArgs> Action { get; }

		/// <summary>
		/// This allows you to await an event, it's important that only a ISmartEventHandler created with First is allowed.
		/// The When Predicate and Do Action, if specified, will be used.
		/// </summary>
		/// <param name="timeout">optional TimeSpan</param>
		/// <param name="cancellationToken">optional CancellationToken</param>
		/// <returns>Task to await for</returns>
		Task WaitForAsync(TimeSpan? timeout = null, CancellationToken? cancellationToken = null);
		
		/// <summary>
		/// This allows you to await an event, it's important that only a ISmartEventHandler created with First is allowed.
		/// The When predicate, if specified, will define if the event is "finished".
		/// </summary>
		/// <param name="func">Function which is called when the event passes the When Predicate, the result is returned in the awaiting Task</param>
		/// <param name="timeout">optional TimeSpan</param>
		/// <param name="cancellationToken">optional CancellationToken</param>
		/// <returns>Task to await for</returns>
		Task<TResult> WaitForAsync<TResult>(Func<object, TEventArgs, TResult> func, TimeSpan? timeout = null, CancellationToken? cancellationToken = null);
	}
}
