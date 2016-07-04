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
	/// 
	/// </summary>
	public interface ISmartEventHandler<TEventArgs>
	{
		/// <summary>
		/// Set the action which is executed on an event, if a when predicate is set this will be checked first
		/// </summary>
		/// <param name="doAction">action which is passed the sender and event arguments</param>
		/// <returns>ISmartEventHandler (this)</returns>
		ISmartEventHandler<TEventArgs> Do(Action<object, TEventArgs> doAction);

		/// <summary>
		/// Set the predicate which decides if the event handler needs to react.
		/// </summary>
		/// <param name="predicate">function which returns a bool depending on the passed sender and event arguments</param>
		/// <returns>ISmartEventHandler (this)</returns>
		ISmartEventHandler<TEventArgs> When(Func<object, TEventArgs, bool> predicate);

		/// <summary>
		/// Start the ISmartEventHandler by registering the underlying event
		/// </summary>
		void Start();

		/// <summary>
		/// Test if the DoAtion can be called
		/// </summary>
		Func<object, TEventArgs, bool> Predicate { get; }

		/// <summary>
		/// The registered Action, this does whatever needs to be done when the event triggers
		/// </summary>
		Action<object, TEventArgs> Action { get; }
	}
}
