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

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     This is the implementation of the DirectObserver,  the implementation does nothing more than call a supplied action
	///     when OnNext is called.
	///     Another way of doing this, woulb be EventObservable.Next.ForEachAsync(action).
	/// </summary>
	internal class DirectObserver<TValue> : IObserver<TValue>, IDisposable
	{
		private readonly Action<TValue> _action;
		private readonly Func<TValue, bool> _predicate = e => true;
		private readonly IDisposable _subscription;

		public DirectObserver(IObservable<TValue> observable, Action<TValue> action, Func<TValue, bool> predicate)
		{
			if (observable == null)
			{
				throw new ArgumentNullException(nameof(observable));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			_action = action;
			if (predicate != null)
			{
				_predicate = predicate;
			}
			_subscription = observable.Subscribe(this);
		}

		/// <summary>
		///     Dispose this IEventHandler, this will cancel the subscription
		/// </summary>
		public void Dispose()
		{
			_subscription.Dispose();
		}

		/// <summary>
		///     IObserver.OnCompleted
		/// </summary>
		public void OnCompleted()
		{
			// Do nothing
		}

		/// <summary>
		///     IObserver.OnError
		/// </summary>
		/// <param name="error">Exception</param>
		public void OnError(Exception error)
		{
			// Ignore
		}

		/// <summary>
		///     IObserver.OnNext
		/// </summary>
		/// <param name="value">TValue</param>
		public void OnNext(TValue value)
		{
			if (_predicate(value))
			{
				_action?.Invoke(value);
			}
		}
	}
}