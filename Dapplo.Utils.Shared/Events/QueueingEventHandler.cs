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
using System.Collections.Concurrent;
using System.Collections.Generic;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     This is the implementation of the QueueingEventHandler
	/// </summary>
	internal class QueueingEventHandler<TEventArgs> : IEventHandler, IObserver<IEventData<TEventArgs>>
	{
		private readonly BlockingCollection<IEventData<TEventArgs>> _events = new BlockingCollection<IEventData<TEventArgs>>();
		private readonly IDisposable _subscription;

		internal QueueingEventHandler(IObservable<IEventData<TEventArgs>> parent)
		{
			_subscription = parent.Subscribe(this);
		}

		/// <summary>
		///     Create a consuming IEnumerable
		/// </summary>
		/// <returns>IEnumerable with a tuple with object (sender) and TEventArgs</returns>
		public IEnumerable<IEventData<TEventArgs>> GetEnumerable
		{
			get
			{
				try
				{
					while (!_events.IsCompleted)
					{
						IEventData<TEventArgs> item;
						while (_events.TryTake(out item, TimeSpan.FromSeconds(1)))
						{
							// Only yield the event arguments
							yield return item;
						}
					}
				}
				finally
				{
					// "Caller" finished, unregister
					_subscription.Dispose();
					_events.Dispose();
				}
			}
		}

		public void Dispose()
		{
			_subscription.Dispose();
		}

		/// <summary>
		///     Add the passed event information to the Blocking collection
		/// </summary>
		/// <param name="eventData">IEventData</param>
		public void OnNext(IEventData<TEventArgs> eventData)
		{
			_events.Add(eventData);
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
			_events.CompleteAdding();
		}
	}
}