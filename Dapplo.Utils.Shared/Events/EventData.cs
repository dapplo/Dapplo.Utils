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

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     Interface for the event data
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	public interface IEventData<out TEvent>
	{
		/// <summary>
		///     Who sent the event
		/// </summary>
		object Sender { get; }

		/// <summary>
		///     Name of the event
		/// </summary>
		string Name { get; }

		/// <summary>
		///     Arguments of the event
		/// </summary>
		TEvent Args { get; }
	}

	/// <summary>
	///     Non mutable container for the event values
	/// </summary>
	public static class EventData
	{
		/// <summary>
		///     Factory method
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static IEventData<TArgs> Create<TArgs>(object sender, TArgs args, string name = null)
		{
			return new EventData<TArgs>(sender, args, name);
		}
	}

	/// <summary>
	///     Non mutable container for the event values
	/// </summary>
	/// <typeparam name="TEvent">The underlying event type</typeparam>
	public class EventData<TEvent> : IEventData<TEvent>
	{
		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="sender">Who initiated the event</param>
		/// <param name="name">Name of the event, if available</param>
		/// <param name="args">TEventArgs</param>
		public EventData(object sender, TEvent args, string name = null)
		{
			Sender = sender;
			Args = args;
			Name = name;
		}

		/// <summary>
		///     Who sent the event
		/// </summary>
		public object Sender { get; }

		/// <summary>
		///     Name of the event
		/// </summary>
		public string Name { get; }

		/// <summary>
		///     Arguments of the event
		/// </summary>
		public TEvent Args { get; }
	}
}