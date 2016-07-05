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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log.Facade;

namespace Dapplo.Utils.Events
{
	/// <summary>
	/// This is the implementation of the smart event handler
	/// </summary>
	public class SmartEventHandler<TEventArgs> : ISmartEventHandler<TEventArgs>
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly LogSource Log = new LogSource();
		private readonly SmartEvent<TEventArgs> _parent;

		/// <summary>
		/// The registered "when" Predicate
		/// </summary>
		public Func<object, TEventArgs, bool> Predicate { get; private set; } = (o, args) => true;

		/// <summary>
		/// The registered "do" action
		/// </summary>
		public Action<object, TEventArgs> Action { get; private set; }

		internal SmartEventHandler(SmartEvent<TEventArgs> parent)
		{
			_parent = parent;
		}

		/// <summary>
		/// Start the event handling by registering this ISmartEventHandler to the parent SmartEvent.
		/// If the SmartEvent didn't register the event yet, it will do so now.
		/// </summary>
		public ISmartEvent<TEventArgs> Start()
		{
			if (Action == null)
			{
				throw new ArgumentNullException(nameof(Action), "No action defined, nothing to do.");
			}
			_parent.Register(this);
			return _parent;
		}

		/// <summary>
		/// Pauses the event handling by unregistering this ISmartEventHandler to the parent
		/// This might cause the event registration to be removed all together, but this should not matter.
		/// </summary>
		public ISmartEvent<TEventArgs> Pause()
		{
			_parent.Unregister(this);
			return _parent;
		}

		/// <summary>
		/// React to first event only, unregister when a match was found
		/// </summary>
		public bool First { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="doAction"></param>
		/// <returns>ISmartEventHandler (this)</returns>
		public ISmartEventHandler<TEventArgs> Do(Action<object, TEventArgs> doAction)
		{
			Action = doAction;
			return this;
		}

		/// <summary>
		/// Set the predicate which decides if the event handler needs to react.
		/// </summary>
		/// <param name="predicate">function which returns a bool depending on the passed sender and event args</param>
		/// <returns>ISmartEventHandler (this)</returns>
		public ISmartEventHandler<TEventArgs> When(Func<object, TEventArgs, bool> predicate)
		{
			Predicate = predicate;
			return this;
		}

		/// <summary>
		/// This allows you to await an event, it's important that only a ISmartEventHandler created with First is allowed.
		/// The When Predicate, if specified, specifies if the await is "finished".
		/// The Do Action, if specified, will also be called (usefull or not)
		/// </summary>
		/// <param name="timeout">optional TimeSpan</param>
		/// <param name="cancellationToken">optional CancellationToken</param>
		/// <returns>Task to await for</returns>
		public Task WaitForAsync(TimeSpan? timeout = null, CancellationToken? cancellationToken = null)
		{
			// We actually just reuse the WaitForAsync with the function, capture the action before so we can call it.
			return WaitForAsync((o, args) => true, timeout, cancellationToken);
		}

		/// <summary>
		/// This allows you to await an event, it's important that only a ISmartEventHandler created with First is allowed.
		/// The When Predicate, if specified, specifies if the await is "finished".
		/// The Do Action, if specified, will also be called (usefull or not)
		/// </summary>
		/// <param name="func">Function which is called when the event passes the When Predicate, the result is returned in the awaiting Task</param>
		/// <param name="timeout">optional TimeSpan</param>
		/// <param name="cancellationToken">optional CancellationToken</param>
		/// <returns>Task to await for</returns>
		public Task<TResult> WaitForAsync<TResult>(Func<object, TEventArgs, TResult> func, TimeSpan? timeout = null, CancellationToken? cancellationToken = null)
		{
			// Test arguments
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}

			// Test state
			if (!First)
			{
				throw new InvalidOperationException(nameof(WaitForAsync) + " only works if First was specified.");
			}
			var taskCompletionSource = new TaskCompletionSource<TResult>();
			IList<CancellationTokenRegistration> cancellationTokenRegistrations = new List<CancellationTokenRegistration>();
			Action<IList<CancellationTokenRegistration>> cleanupAction = registrations =>
			{
				foreach (var tokenRegistration in registrations)
				{
					tokenRegistration.Dispose();
				}
			};

			// Add timeout logic
			if (timeout.HasValue)
			{
				var cancellationTokenSource = new CancellationTokenSource(timeout.Value);

				// Register the timeout
				var cancellationTokenRegistration = cancellationTokenSource.Token.Register(() =>
				{
					cleanupAction(cancellationTokenRegistrations);
					string message = $"Timeout awaiting event";
					Log.Error().WriteLine(message);
					taskCompletionSource.TrySetException(new TimeoutException(message));
				}, false);
				cancellationTokenRegistrations.Add(cancellationTokenRegistration);
			}

			// Add cancel logic
			cancellationToken?.Register(() =>
			{
				cleanupAction(cancellationTokenRegistrations);
				string message = $"Cancel while waiting for event";
				Log.Error().WriteLine(message);
				taskCompletionSource.SetCanceled();
			});

			// Store Action, in case the caller has set a do
			var storedAction = Action;
			Action = (sender, args) =>
			{
				Log.Info().WriteLine($"Event awating action called.");
				cleanupAction(cancellationTokenRegistrations);
				try
				{
					var result = func(sender, args);

					// Call the Action if there was any
					storedAction?.Invoke(sender, args);

					// Restore the state before, just in case
					Action = storedAction;
					taskCompletionSource.SetResult(result);
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetException(ex);
				}
			};

			// Register the event, so the await will work.
			Start();

			return taskCompletionSource.Task;
		}
	}
}
