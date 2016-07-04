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
	/// This is the interface to a smart event.
	/// 
	/// </summary>
	/// <typeparam name="TEventArgs">type for the underlying EventHandler</typeparam>
    public interface ISmartEvent<TEventArgs> : IDisposable
	{
		/// <summary>
		/// Triggers the event, thus calling all registered delegates (also those which are no SmartEventHandlers)
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="eventArgs">TEventArgs</param>
		void Trigger(object sender, TEventArgs eventArgs);

		/// <summary>
		/// Create a ISmartEventHandler which is running the action if the underlying event triggers
		/// </summary>
		/// <param name="action"></param>
		/// <returns>ISmartEventHandler</returns>
		ISmartEventHandler<TEventArgs> On(Action<object, TEventArgs> action);

		/// <summary>
		/// Register a smartEventHandler to the ISmartEvent
		/// </summary>
		/// <param name="smartEventHandler">ISmartEventHandler</param>
		/// <returns>this</returns>
		ISmartEvent<TEventArgs> Register(ISmartEventHandler<TEventArgs> smartEventHandler);

		/// <summary>
		/// Unregister a smartEventHandler from the ISmartEvent
		/// </summary>
		/// <param name="smartEventHandler">ISmartEventHandler</param>
		/// <returns>this</returns>
		ISmartEvent<TEventArgs> Unregister(ISmartEventHandler<TEventArgs> smartEventHandler);
	}
}
