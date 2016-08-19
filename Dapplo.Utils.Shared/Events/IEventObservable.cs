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
	///     Base marker interface for the IEventObservable
	/// </summary>
	public interface IEventObservable : IDisposable
	{
		/// <summary>
		///     The name of the underlying event, might be null if not supplied
		/// </summary>
		string EventName { get; }

		/// <summary>
		///     The object which contains the event, could be null depending on how the event was registered.
		///     The backing type is a Weak-Reference, so it COULD be garbage collected and than return null
		/// </summary>
		object Source { get; }

		/// <summary>
		///     The type for the Argument
		/// </summary>
		Type EventArgumentType { get; }
	}

	/// <summary>
	///     This is the interface to a EventObservable.
	/// </summary>
	/// <typeparam name="TEventArgs">type for the underlying EventHandler</typeparam>
	public interface IEventObservable<out TEventArgs> : IEventObservable, IObservable<IEventData<TEventArgs>>
		where TEventArgs : class
	{
		/// <summary>
		///     Trigger the underlying event
		/// </summary>
		/// <param name="eventData">IEventData with all the data about the event, use EventData.Create for this.</param>
		bool Trigger(IEventData eventData);
	}
}