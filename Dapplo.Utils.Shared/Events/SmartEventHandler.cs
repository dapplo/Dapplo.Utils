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

namespace Dapplo.Utils.Events
{
	/// <summary>
	/// This is the implementation of the smart event handler
	/// </summary>
	public class SmartEventHandler<TEventArgs> : ISmartEventHandler<TEventArgs>
	{
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
			if (Action != null)
			{
				throw new InvalidOperationException("can't register when after do.");
			}
			Predicate = predicate;
			return this;
		}
	}
}
