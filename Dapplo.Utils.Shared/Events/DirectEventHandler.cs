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
		private Action<object, TEventArgs> _action;
		private Func<object, TEventArgs, bool> _predicate = (s, e) => true;

		internal DirectEventHandler(IInternalSmartEvent<TEventArgs> parent)
		{
			_parent = parent;
		}

		/// <summary>
		///     This is called when an event occurs, and will call the registered action
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="eventArgs">TEventArgs</param>
		public void Handle(object sender, TEventArgs eventArgs)
		{
			if (_predicate(sender, eventArgs))
			{
				_action?.Invoke(sender, eventArgs);
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
		/// <param name="predicate">Func which gets an object and a TEventArgs, and returns a bool</param>
		/// <returns>IEventHandler</returns>
		public IEventHandler Where(Func<object, TEventArgs, bool> predicate)
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
		/// <param name="action">Action which gets an object and TEventArgs</param>
		/// <returns>IEventHandler</returns>
		public IEventHandler Do(Action<object, TEventArgs> action)
		{
			_action = action;
			Subscribe();
			return this;
		}
	}
}