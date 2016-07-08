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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     Static factory methods to create a EventObservable
	/// </summary>
	public static class EventObservable
	{
		/// <summary>
		///     Default BindingFlags for finding events and methods via reflection
		/// </summary>
		public const BindingFlags AllBindings = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>
		///     Dispose all IEventObservable in the list
		/// </summary>
		/// <param name="eventObservables">IList with IEventObservable</param>
		public static void DisposeAll(IEnumerable<IEventObservable> eventObservables)
		{
			if (eventObservables != null)
			{
				foreach (var eventObservable in eventObservables)
				{
					eventObservable.Dispose();
				}
			}
		}

		/// <summary>
		///     Removes all the event handlers from the defined events in an object
		///     This is usefull to do internally, after a MemberwiseClone is made, to prevent memory leaks
		/// </summary>
		/// <param name="instance">object instance where events need to be removed</param>
		/// <param name="regExPattern">Regular expression to match the even names, null for alls</param>
		public static void RemoveEventHandlers(object instance, string regExPattern = null)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}
			Regex regex = null;
			if (!string.IsNullOrEmpty(regExPattern))
			{
				regex = new Regex(regExPattern);
			}
			var typeWithEvents = instance.GetType();
			foreach (var eventInfo in typeWithEvents.GetEvents(AllBindings))
			{
				if (regex != null && !regex.IsMatch(eventInfo.Name))
				{
					continue;
				}
				var fieldInfo = typeWithEvents.GetField(eventInfo.Name, AllBindings);
				if (fieldInfo == null)
				{
					continue;
				}
				var eventDelegate = fieldInfo.GetValue(instance) as Delegate;
				var removeMethod = eventInfo.GetRemoveMethod(true);
				removeMethod?.Invoke(instance, new object[] {eventDelegate});
			}
		}

		/// <summary>
		///     Register the supplied action to all events in the targe class.
		///     Although this is somewhat restrictive, it might be usefull for logs
		/// </summary>
		/// <param name="targetObject"></param>
		/// <param name="predicate"></param>
		/// <param name="action">Action for the registration</param>
		/// <returns>IList of IEventObservable which can be dispose with DisposeAll</returns>
		public static IList<IEventObservable> RegisterEvents<TEventArgs>(object targetObject, Action<IEventData<TEventArgs>> action, Func<string, bool> predicate = null)
		{
			var eventObservables = new List<IEventObservable>();

			foreach (var eventInfo in targetObject.GetType().GetEvents(AllBindings))
			{
				// Skip if predicate is defined and returns false
				if (predicate?.Invoke(eventInfo.Name) == false)
				{
					continue;
				}
				var eventHandlerInvokeDelegate = eventInfo.EventHandlerType.GetMethod("Invoke");
				var eventType = eventHandlerInvokeDelegate.GetParameters().Last().ParameterType;
				var constructedType = typeof(EventObservable<>).MakeGenericType(eventType);
				var args = new[] {targetObject, eventInfo.Name};
				var eventObservable = (IEventObservable) Activator.CreateInstance(constructedType, AllBindings, null, args, null, null);
				var onEachMethodInfo = eventObservable.GetType().GetMethod("OnEach", AllBindings);
				onEachMethodInfo.Invoke(eventObservable, new object[] {action, predicate});
				eventObservables.Add(eventObservable);
			}

			return eventObservables;
		}

		/// <summary>
		///     Create a Parent for the referenced EventHandler
		/// </summary>
		/// <typeparam name="TEventArgs">Type for the event</typeparam>
		/// <param name="eventHandler">EventHandler</param>
		/// <param name="eventName">
		///     The name of the event, e.g. via nameof(object.event), used for logging and finding the IEventObservable
		/// </param>
		public static IEventObservable<TEventArgs> From<TEventArgs>(ref EventHandler<TEventArgs> eventHandler, string eventName = null)
			where TEventArgs : EventArgs
		{
			return new EventObservable<TEventArgs>(ref eventHandler, eventName);
		}

		/// <summary>
		///     Create a Parent from the event which can be find in the object by the specified event name.
		/// </summary>
		/// <typeparam name="TEventArgs">Typeof the event arguments</typeparam>
		/// <param name="targetObject">object which defines the event</param>
		/// <param name="eventName">nameof(object.event)</param>
		/// <returns>IEventObservable</returns>
		public static IEventObservable<TEventArgs> From<TEventArgs>(object targetObject, string eventName)
			where TEventArgs : EventArgs
		{
			// Create the Parent, the Reflection is in there.
			return new EventObservable<TEventArgs>(targetObject, eventName);
		}
	}

	/// <summary>
	///     This creates a EventObservable for the supplied event information.
	/// </summary>
	/// <typeparam name="TEventArgs">the underlying type for the EventHandler</typeparam>
	public class EventObservable<TEventArgs> : IEventObservable<TEventArgs>
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly LogSource Log = new LogSource();
		private readonly EventInfo _eventInfo;
		private readonly Delegate _handleEventDelegate;

		private readonly IList<IObserver<IEventData<TEventArgs>>> _observers = new List<IObserver<IEventData<TEventArgs>>>();
		private readonly object _targetObject;
		private readonly bool _useEventHandler;
		private bool _disposedValue; // To detect redundant calls
		private EventHandler<TEventArgs> _eventHandler;
		private bool _subscribedToEvents;

		/// <summary>
		///     Constructor for the FieldInfo
		/// </summary>
		/// <param name="targetObject">Object containing the event field/property</param>
		/// <param name="eventName">The name of the event field / property</param>
		internal EventObservable(object targetObject, string eventName)
		{
			_eventInfo = targetObject.GetType().GetEvent(eventName, EventObservable.AllBindings);
			if (_eventInfo == null)
			{
				throw new ArgumentException($"The event {eventName} cannot be bound by FromReflection as it does not exist in the supplied object.",
					nameof(eventName));
			}
			EventName = eventName;

			_targetObject = targetObject;
			_useEventHandler = false;

			// Sometimes the event handler only uses a single argument, for this check the parameter count of the delegate
			var eventHandlerInvokeDelegate = _eventInfo.EventHandlerType.GetMethod("Invoke");
			var useWithSender = true;
			if (eventHandlerInvokeDelegate != null)
			{
				useWithSender = eventHandlerInvokeDelegate.GetParameters().Length >= 2;
			}
			// Now decide on the handler, in the end both will function the same as we store the target and pass this as sender.
			var eventHandleMethodName = useWithSender ? nameof(HandleEvent) : nameof(HandleEventWithoutSender);
			var handleEventMethodInfo = GetType().GetMethod(eventHandleMethodName, BindingFlags.Instance | BindingFlags.NonPublic);
			_handleEventDelegate = handleEventMethodInfo.CreateDelegate(_eventInfo.EventHandlerType, this);
		}

		/// <summary>
		///     Constructor for the EventHandler ref
		/// </summary>
		/// <param name="eventHandler"></param>
		/// <param name="eventName">Name of the event, can be used to find EventObservable's in a list and logging</param>
		internal EventObservable(ref EventHandler<TEventArgs> eventHandler, string eventName = null)
		{
			EventName = eventName;
			_eventHandler = eventHandler;
			_useEventHandler = true;
		}

		/// <summary>
		///     The name of the underlying event, might be null if not supplied
		/// </summary>
		public string EventName { get; }

		/// <summary>
		///     Triggers an event
		/// </summary>
		/// <param name="eventData">IEventData</param>
		public void Trigger(IEventData<EventArgs> eventData)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(EventObservable), $"Can't trigger {EventName} after dispose.");
			}

			try
			{
				if (_useEventHandler)
				{
					_eventHandler?.Invoke(eventData.Sender, (TEventArgs) (object) eventData.Args);
				}
				else
				{
					// Raise via reflection
					var raiseMethodInfo = _eventInfo.RaiseMethod;
					if (raiseMethodInfo != null)
					{
						raiseMethodInfo.Invoke(_targetObject, new[] {eventData.Sender, eventData.Args});
					}
					else
					{
						var fieldInfo = _targetObject.GetType().GetField(_eventInfo.Name, EventObservable.AllBindings);
						if (fieldInfo != null)
						{
							var eventDelegate = (Delegate) fieldInfo.GetValue(_targetObject);
							eventDelegate?.DynamicInvoke(eventData.Sender, eventData.Args);
						}
						else
						{
							throw new NotSupportedException($"Can't trigger the event {_eventInfo.Name}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "An exception occured while triggering an event.");
			}
		}

		/// <summary>
		///     Call the supplied action on each event.
		/// </summary>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <returns>IEventHandler</returns>
		public IDisposable OnEach(Action<IEventData<TEventArgs>> action, Func<IEventData<TEventArgs>, bool> predicate = null)
		{
			var handler = new DirectObserver<IEventData<TEventArgs>>(this);
			handler.Where(predicate);
			handler.Do(action);
			return handler;
		}

		/// <summary>
		///     Process events (IEnumerable with IEventData) in a background task, the task will only finish on an
		///     exception or if the function returns
		/// </summary>
		/// <typeparam name="TResult">Type of the result</typeparam>
		/// <param name="processFunc">Function which will process the IEnumerable</param>
		/// <returns>Task with the result of the function</returns>
		public Task<TResult> ProcessAsync<TResult>(Func<IEnumerable<IEventData<TEventArgs>>, TResult> processFunc)
		{
			// Start the registration inside the current thread
			var enumerable = From;
			return Task.Run(() => processFunc(enumerable));
		}

		/// <summary>
		///     Process events (IEnumerable with IEventData) in a background task, the task will only finish on an
		///     exception
		/// </summary>
		/// <param name="processAction">Action which will process the IEnumerable</param>
		/// <returns>Task</returns>
		public Task ProcessAsync(Action<IEnumerable<IEventData<TEventArgs>>> processAction)
		{
			// Start the registration inside the current thread
			var enumerable = From;
			return Task.Run(() => processAction(enumerable));
		}

		/// <summary>
		///     Create a EnumerableObserver for handling the sender and the eventArgs
		/// </summary>
		/// <returns>IEnumerable with a IEventData</returns>
		public IEnumerable<IEventData<TEventArgs>> From
		{
			get
			{
				var handler = new EnumerableObserver<IEventData<TEventArgs>>(this);
				return handler.GetEnumerable;
			}
		}

		/// <summary>
		///     Implement IDisposable.Dispose()
		/// </summary>
		void IDisposable.Dispose()
		{
			UnsubscribeAllHandlers();
			_disposedValue = true;
		}

		/// <summary>
		///     IObservable.Subscript implementation
		/// </summary>
		/// <param name="observer">IObserver which wants to subscribe</param>
		/// <returns>IDisposable which needs to be disposed to unsubscribe</returns>
		public IDisposable Subscribe(IObserver<IEventData<TEventArgs>> observer)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(EventObservable), $"Can't subscribe to {EventName} after the EventObservable was disposed.");
			}

			lock (_observers)
			{
				if (!_observers.Contains(observer))
				{
					_observers.Add(observer);
				}
			}
			if (!_subscribedToEvents)
			{
				_subscribedToEvents = true;
				// Start the processing in the background by registering the event
				if (_useEventHandler)
				{
					_eventHandler += HandleEvent;
				}
				else
				{
					_eventInfo.AddMethod.Invoke(_targetObject, new object[] {_handleEventDelegate});
				}
			}
			return Disposable.Create(() => Unsubscribe(observer));
		}

		/// <summary>
		///     Unsubscribe the IObserver, this is used internally
		/// </summary>
		/// <param name="observer">IObserver</param>
		private void Unsubscribe(IObserver<IEventData<TEventArgs>> observer)
		{
			lock (_observers)
			{
				if (_observers.Remove(observer))
				{
					// Unsubscribe the HandleEvent from the event as no-one is interested
					if (_observers.Count == 0 && _subscribedToEvents)
					{
						_subscribedToEvents = false;
						// We finished adding events, so the processing can stop
						if (_useEventHandler && _eventHandler != null)
						{
							// ReSharper disable once DelegateSubtraction
							_eventHandler -= HandleEvent;
						}
						else if (!_useEventHandler)
						{
							// Unsubscribe the _handleEventDelegate via reflection
							_eventInfo.RemoveMethod.Invoke(_targetObject, new object[] {_handleEventDelegate});
						}
					}
					// signal that it was removed by telling that there will be no more data
					observer.OnCompleted();
				}
			}
		}

		/// <summary>
		///     This will handle the event in the case where the event handler only has one argument.
		///     It will take the target object, where the event resides, as the sender.
		/// </summary>
		/// <param name="eventArgs">TEventArgs</param>
		private void HandleEventWithoutSender(TEventArgs eventArgs)
		{
			HandleEvent(_targetObject, eventArgs);
		}

		/// <summary>
		///     This handles the actual event bye placing it into the BlockingCollection
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="eventArgs">TEventArgs</param>
		private void HandleEvent(object sender, TEventArgs eventArgs)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(EventObservable), $"Can't be handling events for {EventName} after dispose.");
			}
			IList<IObserver<IEventData<TEventArgs>>> eventHandlers;
			lock (_observers)
			{
				eventHandlers = _observers.ToList();
			}
			// Loop over all event handlers, and add the item to their BlockingCollection
			foreach (var eventHandler in eventHandlers)
			{
				try
				{
					eventHandler.OnNext(EventData.Create(sender, eventArgs, EventName));
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex);
				}
			}
		}

		/// <summary>
		///     Remove all registered IEventHandler
		/// </summary>
		private void UnsubscribeAllHandlers()
		{
			IList<IObserver<IEventData<TEventArgs>>> eventHandlers;
			lock (_observers)
			{
				eventHandlers = _observers.ToList();
			}
			// Loop over all event handlers, and add the item to their BlockingCollection
			foreach (var eventHandler in eventHandlers)
			{
				Unsubscribe(eventHandler);
			}
		}
	}
}