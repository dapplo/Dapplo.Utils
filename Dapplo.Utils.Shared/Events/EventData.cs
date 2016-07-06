using System;
using System.Collections.Generic;
using System.Text;

namespace Dapplo.Utils.Events
{
	/// <summary>
	/// Interface for the event data
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	public interface IEventData<out TEvent>
	{
		/// <summary>
		/// Who sent the event
		/// </summary>
		object Sender { get; }

		/// <summary>
		/// Name of the event
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Arguments of the event
		/// </summary>
		TEvent Args { get; }
	}

	/// <summary>
	/// Non mutable container for the event values
	/// </summary>
	public class EventData
	{
		/// <summary>
		/// Factory method
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
	/// Non mutable container for the event values
	/// </summary>
	/// <typeparam name="TEvent">The underlying event type</typeparam>
	public class EventData<TEvent> : IEventData<TEvent>
	{
		/// <summary>
		/// Constructor
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
		/// Who sent the event
		/// </summary>
		public object Sender { get; }

		/// <summary>
		/// Name of the event
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Arguments of the event
		/// </summary>
		public TEvent Args { get; }

	}

}
