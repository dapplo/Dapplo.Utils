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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     This is the implementation of the EnumerableObserver
	/// </summary>
	internal class EnumerableObserver<TValue> : IObserver<TValue>, IEnumerable<TValue>, IDisposable
	{
		private readonly BlockingCollection<TValue> _values = new BlockingCollection<TValue>();
		private readonly IDisposable _subscription;
		private readonly CancellationToken _cancellationToken;
		private bool _disposed;
		private readonly object _lock = new object();
		private const int TimeoutMs = 300;

		/// <summary>
		/// Create an EnumerableObserver and subscribe it to the parent, this can be used to get a IEnumerable for events
		/// </summary>
		/// <param name="parent">IObservable</param>
		/// <param name="cancellationToken">CancellationToken when cancel requested this subscription is stopped</param>
		public EnumerableObserver(IObservable<TValue> parent, CancellationToken cancellationToken = default(CancellationToken))
		{
			_subscription = parent.Subscribe(this);
			_cancellationToken = cancellationToken;
			if (cancellationToken != CancellationToken.None)
			{
				// Dispose when the cancellationToken is cancelled
				cancellationToken.Register(Dispose);
			}
		}

		/// <summary>
		///     Create a consuming TValue IEnumerable
		/// </summary>
		/// <returns>IEnumerable with a TValue</returns>
		public IEnumerable<TValue> GetEnumerable
		{
			get
			{
				try
				{
					// Check if the Observable is ready with suplying values, or cancel was requested
					while (!_values.IsCompleted && !_cancellationToken.IsCancellationRequested)
					{
						TValue item;
						// Try getting a value, this blocks for some time  unless a value is available and also monitors the CancellationToken
						if (_values.TryTake(out item, TimeoutMs))
						{
							// Just yield the value
							yield return item;
						}
					}
				}
				finally
				{
					// "Caller" finished, unregister
					_subscription.Dispose();
					// We no longer access the values, so safely dispose them
					_values.Dispose();
				}
			}
		}

		public void Dispose()
		{
			lock (_lock)
			{
				if (_disposed)
				{
					return;
				}
				_disposed = true;
				_subscription.Dispose();
			}
		}

		/// <summary>
		///     Add the value to the Blocking collection
		/// </summary>
		/// <param name="value">TValue</param>
		public void OnNext(TValue value)
		{
			if (!_cancellationToken.IsCancellationRequested && !_values.IsAddingCompleted)
			{
				// ReSharper disable once MethodSupportsCancellation
				_values.Add(value);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="error"></param>
		public void OnError(Exception error)
		{
			// Ignore for now
		}

		/// <summary>
		///     The IObservable is finished
		/// </summary>
		public void OnCompleted()
		{
			if (!_values.IsAddingCompleted)
			{
				_values.CompleteAdding();
			}
		}

		/// <summary>
		/// Implementation of the generic IEnumerable.GetEnumerator
		/// </summary>
		/// <returns>IEnumerator with TValue</returns>
		public IEnumerator<TValue> GetEnumerator()
		{
			return GetEnumerable.GetEnumerator();
		}

		/// <summary>
		/// Implementation of the IEnumerable.GetEnumerator
		/// </summary>
		/// <returns>IEnumerator</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerable.GetEnumerator();
		}
	}
}