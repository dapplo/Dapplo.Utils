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
	///     Base interface for the event data
	/// </summary>
	public interface IEventData
	{
		/// <summary>
		///     Who sent the event
		/// </summary>
		object Sender { get; }

		/// <summary>
		///     Name of the event
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// The event arguments
		/// </summary>
		EventArgs Args { get; }
	}

	/// <summary>
	///     Interface for the event data
	/// </summary>
	/// <typeparam name="TEventArgs"></typeparam>
	public interface IEventData<out TEventArgs> : IEventData
		where TEventArgs : class
	{
		/// <summary>
		///     Arguments of the event
		/// </summary>
		new TEventArgs Args { get; }
	}

	/// <summary>
	///     Non mutable container for the event values
	/// </summary>
	public static class EventData
	{
		/// <summary>
		///     Factory method
		/// </summary>
		/// <typeparam name="TEventArgs">Type for the event data</typeparam>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		/// <param name="name"></param>
		/// <returns>IEventData</returns>
		public static IEventData<TEventArgs> Create<TEventArgs>(object sender, TEventArgs args, string name = null)
			where TEventArgs : class
		{
			return new EventData<TEventArgs>(sender, args, name);
		}

		/// <summary>
		///     Factory method for an empty argument
		/// </summary>
		/// <typeparam name="TEventArgs">Type for the event data</typeparam>
		/// <returns>IEventData</returns>
		public static IEventData<TEventArgs> Create<TEventArgs>(object sender = null, string name = null) where TEventArgs : class, new()
		{
			return new EventData<TEventArgs>(sender, new TEventArgs(), name);
		}
	}

	/// <summary>
	///     Non mutable container for the event values
	/// </summary>
	/// <typeparam name="TEventArgs">The underlying event type</typeparam>
	public class EventData<TEventArgs> : IEventData<TEventArgs>
		where TEventArgs : class
	{
		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="sender">Who initiated the event</param>
		/// <param name="name">Name of the event, if available</param>
		/// <param name="args">TEventArgs</param>
		public EventData(object sender, TEventArgs args, string name = null)
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
		///     Name of the event, will be automatically filled if this was null and is used in Trigger
		/// </summary>
		public string Name { get; set; }

		EventArgs IEventData.Args => Args as EventArgs;

		/// <summary>
		///     Arguments of the event
		/// </summary>
		public TEventArgs Args { get; }
	}
}