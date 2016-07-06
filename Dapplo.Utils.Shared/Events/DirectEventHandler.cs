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
	///     This is the implementation of the DirectEventHandler
	/// </summary>
	public class DirectEventHandler<TEventArgs> : IInternalEventHandler<TEventArgs>
	{
		private readonly IInternalSmartEvent<TEventArgs> _parent;
		private Action<IEventData<TEventArgs>> _action;
		private Func<IEventData<TEventArgs>, bool> _predicate = e => true;

		internal DirectEventHandler(IInternalSmartEvent<TEventArgs> parent)
		{
			_parent = parent;
		}

		/// <summary>
		///     This is called when an event occurs, and will call the registered action
		/// </summary>
		/// <param name="eventData">IEventData</param>
		public void Handle(IEventData<TEventArgs> eventData)
		{
			if (_predicate(eventData))
			{
				_action?.Invoke(eventData);
			}
		}

		/// <summary>
		///     Signal the enumerator that it has been unsubscribed, and no longer get any new events
		/// </summary>
		public void Unsubscribed()
		{
			// Nothing to do, we just aren't called
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

		/// <summary>
		///     Filter the events with a predicate
		/// </summary>
		/// <param name="predicate">Func which gets IEventData, and returns a bool</param>
		/// <returns>IEventHandler</returns>
		public IEventHandler Where(Func<IEventData<TEventArgs>, bool> predicate)
		{
			if (predicate != null)
			{
				_predicate = predicate;
			}
			return this;
		}

		/// <summary>
		///     Register an action which is called on every event.
		/// </summary>
		/// <param name="action">Action which gets IEventData</param>
		/// <returns>IEventHandler</returns>
		public IEventHandler Do(Action<IEventData<TEventArgs>> action)
		{
			_action = action;
			Subscribe();
			return this;
		}
	}
}