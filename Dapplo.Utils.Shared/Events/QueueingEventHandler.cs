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
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     This is the implementation of the QueueingEventHandler
	/// </summary>
	public class QueueingEventHandler<TEventArgs> : BlockingCollection<Tuple<object, TEventArgs>>, IInternalEventHandler<TEventArgs>
	{
		/// <summary>
		///     The logger for this class
		/// </summary>
		protected static readonly LogSource Log = new LogSource();

		private readonly IInternalSmartEvent<TEventArgs> _parent;

		internal QueueingEventHandler(IInternalSmartEvent<TEventArgs> parent)
		{
			_parent = parent;
			Subscribe();
		}

		/// <summary>
		///     Create a consuming IEnumerable
		/// </summary>
		/// <returns>IEnumerable with a tuple with object (sender) and TEventArgs</returns>
		public IEnumerable<Tuple<object, TEventArgs>> GetEnumerable
		{
			get
			{
				try
				{
					while (!IsCompleted)
					{
						Tuple<object, TEventArgs> item;
						while (TryTake(out item, TimeSpan.FromSeconds(1)))
						{
							// Only yield the event arguments
							yield return item;
						}
					}
				}
				finally
				{
					// "Caller" finished, unregister
					Unsubscribe();
				}
			}
		}

		/// <summary>
		///     Add the passed event information to the Blocking collection
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="eventArgs">TEventArgs</param>
		public void Handle(object sender, TEventArgs eventArgs)
		{
			Add(new Tuple<object, TEventArgs>(sender, eventArgs));
		}

		/// <summary>
		///     Signal the enumerator that it has been unsubscribed, and no longer get any new events
		/// </summary>
		public void Unsubscribed()
		{
			CompleteAdding();
		}

		/// <summary>
		///     Signal the enumerator that it subscribed, and will be passed events
		/// </summary>
		public void Subscribed()
		{
			// do nothing
		}

		/// <summary>
		///     Add this to the parent
		/// </summary>
		public void Subscribe()
		{
			_parent.Subscribe(this);
		}

		/// <summary>
		///     Remove this from the parent
		/// </summary>
		public void Unsubscribe()
		{
			_parent.Unsubscribe(this);
		}
	}
}