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

		public EnumerableObserver(IObservable<TValue> parent)
		{
			_subscription = parent.Subscribe(this);
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
					// Check if the Observable is ready with suplying values
					while (!_values.IsCompleted)
					{
						TValue item;
						// Try getting a value, this blocks for one second unless a value is available.
						while (_values.TryTake(out item, TimeSpan.FromSeconds(1)))
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
					_values.Dispose();
				}
			}
		}

		public void Dispose()
		{
			_values.CompleteAdding();
			_subscription.Dispose();
		}

		/// <summary>
		///     Add the value to the Blocking collection
		/// </summary>
		/// <param name="value">TValue</param>
		public void OnNext(TValue value)
		{
			_values.Add(value);
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
			_values.CompleteAdding();
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