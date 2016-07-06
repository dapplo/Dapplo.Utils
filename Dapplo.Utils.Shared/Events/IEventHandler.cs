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

using System;

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     IEventHandler is what is exposed to the "outside"
	/// </summary>
	public interface IEventHandler
	{
		/// <summary>
		///     Make the IEventHandler start handling events
		/// </summary>
		void Subscribe();

		/// <summary>
		///     Make the IEventHandler stop handling events
		/// </summary>
		void Unsubscribe();
	}

	/// <summary>
	///     IInternalEventHandler, used from the SmartEvent
	/// </summary>
	public interface IInternalEventHandler<in TEventArgs> : IEventHandler
	{
		/// <summary>
		///     This needs to be implemented to make sure the event data is processed
		/// </summary>
		/// <param name="eventData">IEventData has all data about the event</param>
		void Handle(IEventData<TEventArgs> eventData);

		/// <summary>
		///     This is called when the IEventHandler unsubscribed from it'S parent
		/// </summary>
		void Unsubscribed();

		/// <summary>
		///     This is called when the IEventHandler subscribed to it'S parent
		/// </summary>
		void Subscribed();
	}
}