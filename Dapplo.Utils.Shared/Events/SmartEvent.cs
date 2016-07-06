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
using System.Threading.Tasks;
using Dapplo.Log.Facade;
using Dapplo.Utils.Tasks;

#endregion

namespace Dapplo.Utils.Events
{
	/// <summary>
	///     Static methods to create a generic Parent
	/// </summary>
	public static class SmartEvent
	{
		private const BindingFlags AllBindings = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>
		///     Dispose all ISmartEvent in the list
		/// </summary>
		/// <param name="smartEvents">IList with ISmartEvent</param>
		public static void DisposeAll(IEnumerable<ISmartEvent> smartEvents)
		{
			foreach (var smartEvent in smartEvents)
			{
				smartEvent.Dispose();
			}
		}

		/// <summary>
		///     Register the supplied action to all events in the targe class.
		///     Although this is somewhat restrictive, it might be usefull for logs
		/// </summary>
		/// <param name="targetObject"></param>
		/// <param name="predicate"></param>
		/// <param name="action">Action for the registration</param>
		/// <returns>IList of ISmartEvent which can be dispose with DisposeAll</returns>
		public static IList<ISmartEvent> RegisterEvents<TEventArgs>(object targetObject, Action<object, TEventArgs> action,
			Func<string, bool> predicate = null)
		{
			var smartEvents = new List<ISmartEvent>();

			foreach (var eventInfo in targetObject.GetType().GetEvents(AllBindings))
			{
				// Skip if predicate is defined and returns false
				if (predicate?.Invoke(eventInfo.Name) == false)
				{
					continue;
				}
				var eventHandlerInvokeDelegate = eventInfo.EventHandlerType.GetMethod("Invoke");
				var eventType = eventHandlerInvokeDelegate.GetParameters().Last().ParameterType;
				var constructedType = typeof(SmartEvent<>).MakeGenericType(eventType);
				var args = new[] {targetObject, eventInfo.Name};
				var smartEvent = (ISmartEvent) Activator.CreateInstance(constructedType, AllBindings, null, args, null, null);
				smartEvent.OnEach(action);
				smartEvents.Add(smartEvent);
			}

			return smartEvents;
		}

		/// <summary>
		///     Create a Parent for the referenced EventHandler
		/// </summary>
		/// <typeparam name="TEventArgs">Type for the event</typeparam>
		/// <param name="eventHandler">EventHandler</param>
		/// <param name="eventName">
		///     The name of the event, e.g. via nameof(object.event), used for logging and finding the
		///     ISmartEvent
		/// </param>
		/// <param name="registeredSmartEvents">
		///     If you want to keep track of the ISmartEvent registrations, you can pass a list
		///     here
		/// </param>
		public static ISmartEvent<TEventArgs> From<TEventArgs>(ref EventHandler<TEventArgs> eventHandler, string eventName = null,
			IList<ISmartEvent> registeredSmartEvents = null) where TEventArgs : EventArgs
		{
			var smartEvent = new SmartEvent<TEventArgs>(ref eventHandler, eventName);
			registeredSmartEvents?.Add(smartEvent);
			return smartEvent;
		}

		/// <summary>
		///     Create a Parent from the event which can be find in the object by the specified event name.
		/// </summary>
		/// <typeparam name="TEventArgs">Typeof the event arguments</typeparam>
		/// <param name="targetObject">object which defines the event</param>
		/// <param name="eventName">nameof(object.event)</param>
		/// <param name="registeredSmartEvents">
		///     If you want to keep track of the ISmartEvent registrations, you can pass a list
		///     here
		/// </param>
		/// <returns>ISmartEvent</returns>
		public static ISmartEvent<TEventArgs> From<TEventArgs>(object targetObject, string eventName, IList<ISmartEvent> registeredSmartEvents = null)
			where TEventArgs : EventArgs
		{
			// Create the Parent, the Reflection is in there.
			var smartEvent = new SmartEvent<TEventArgs>(targetObject, eventName);
			registeredSmartEvents?.Add(smartEvent);
			return smartEvent;
		}
	}

	/// <summary>
	///     This creates a smart event for the supplied event.
	/// </summary>
	/// <typeparam name="TEventArgs">the underlying type for the EventHandler</typeparam>
	public class SmartEvent<TEventArgs> : IInternalSmartEvent<TEventArgs>
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly LogSource Log = new LogSource();

		private static readonly BindingFlags DefaultBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |
																	BindingFlags.NonPublic;

		private readonly IList<IInternalEventHandler<TEventArgs>> _eventHandlers = new List<IInternalEventHandler<TEventArgs>>();
		private readonly EventInfo _eventInfo;
		private readonly Delegate _handleEventDelegate;
		private readonly object _targetObject;
		private readonly bool _useEventHandler;
		private bool _disposedValue; // To detect redundant calls
		private EventHandler<TEventArgs> _eventHandler;
		private bool _isRegistered;

		/// <summary>
		///     Constructor for the FieldInfo
		/// </summary>
		/// <param name="targetObject">Object containing the event field/property</param>
		/// <param name="eventName">The name of the event field / property</param>
		internal SmartEvent(object targetObject, string eventName)
		{
			_eventInfo = targetObject.GetType().GetEvent(eventName, DefaultBindingFlags);
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
		/// <param name="eventName">Name of the event, can be used to find smart events in a list and logging</param>
		internal SmartEvent(ref EventHandler<TEventArgs> eventHandler, string eventName = null)
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
		/// <param name="sender">the sender of the event</param>
		/// <param name="eventArgs">TEventArgs</param>
		public void Trigger(object sender, TEventArgs eventArgs)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(SmartEvent), $"Can't trigger {EventName} after dispose.");
			}

			try
			{
				if (_useEventHandler)
				{
					_eventHandler?.Invoke(sender, eventArgs);
				}
				else
				{
					// Raise via reflection
					var raiseMethodInfo = _eventInfo.RaiseMethod;
					if (raiseMethodInfo != null)
					{
						raiseMethodInfo.Invoke(_targetObject, new[] {sender, eventArgs});
					}
					else
					{
						var fieldInfo = _targetObject.GetType().GetField(_eventInfo.Name, DefaultBindingFlags);
						if (fieldInfo != null)
						{
							var eventDelegate = (Delegate) fieldInfo.GetValue(_targetObject);
							eventDelegate?.DynamicInvoke(sender, eventArgs);
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
		///     Subscribe an IEventHandler
		/// </summary>
		/// <param name="eventHandler">IEventHandler</param>
		public void Subscribe(IInternalEventHandler<TEventArgs> eventHandler)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(SmartEvent), $"Can't register {EventName} after dispose.");
			}

			lock (_eventHandlers)
			{
				if (!_eventHandlers.Contains(eventHandler))
				{
					_eventHandlers.Add(eventHandler);
				}
			}
			Subscribe();
		}

		/// <summary>
		///     Unsubscribe the IEventHandler
		/// </summary>
		/// <param name="eventHandler">IEventHandler</param>
		public void Unsubscribe(IInternalEventHandler<TEventArgs> eventHandler)
		{
			lock (_eventHandlers)
			{
				if (_eventHandlers.Remove(eventHandler))
				{
					if (_eventHandlers.Count == 0)
					{
						Unsubscribe();
						// signal removed
						eventHandler.Unsubscribed();
					}
				}
			}
		}

		/// <summary>
		///     Call the supplied action on each event.
		/// </summary>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <returns>IEventHandler</returns>
		public IEventHandler OnEach(Action<TEventArgs> action, Func<TEventArgs, bool> predicate = null)
		{
			if (predicate == null)
			{
				return OnEach((o, args) => action(args));
			}
			return OnEach((o, args) => action(args), (o, args) => predicate(args));
		}

		/// <summary>
		///     Call the supplied action on each event.
		/// </summary>
		/// <param name="action">Action to call</param>
		/// <param name="predicate">Predicate, deciding on if the action needs to be called</param>
		/// <returns>IEventHandler</returns>
		public IEventHandler OnEach(Action<object, TEventArgs> action, Func<object, TEventArgs, bool> predicate = null)
		{
			var handler = new DirectEventHandler<TEventArgs>(this);
			handler.Where(predicate);
			handler.Do(action);
			return handler;
		}

		/// <summary>
		///     Process events (IEnumerable with tuple sender,eventargs) in a background task, the task will only finish on an
		///     exception or if the function returns
		/// </summary>
		/// <typeparam name="TResult">Type of the result</typeparam>
		/// <param name="processFunc">Function which will process the IEnumerable</param>
		/// <param name="timeout">Optional TimeSpan for a timeout</param>
		/// <returns>Task with the result of the function</returns>
		public Task<TResult> ProcessExtendedAsync<TResult>(Func<IEnumerable<Tuple<object, TEventArgs>>, TResult> processFunc, TimeSpan? timeout = null)
		{
			// Start the registration inside the current thread
			var enumerable = FromExtended;
			if (timeout.HasValue)
			{
				return AsyncHelper.RunWithTimeout(() => processFunc(enumerable), timeout.Value);
			}
			return Task.Run(() => processFunc(enumerable));
		}

		/// <summary>
		///     Process events (IEnumerable with eventargs) in a background task, the task will only finish on an exception or if
		///     the function returns
		/// </summary>
		/// <typeparam name="TResult">Type of the result</typeparam>
		/// <param name="processFunc">Function which will process the IEnumerable</param>
		/// <param name="timeout">Optional TimeSpan for a timeout</param>
		/// <returns>Task with the result of the function</returns>
		public Task<TResult> ProcessAsync<TResult>(Func<IEnumerable<TEventArgs>, TResult> processFunc, TimeSpan? timeout = null)
		{
			// Start the registration inside the current thread
			var enumerable = From;
			if (timeout.HasValue)
			{
				return AsyncHelper.RunWithTimeout(() => processFunc(enumerable), timeout.Value);
			}
			return Task.Run(() => processFunc(enumerable));
		}

		/// <summary>
		///     Process events (IEnumerable with tuple sender,eventargs) in a background task, the task will only finish on an
		///     exception
		/// </summary>
		/// <param name="processAction">Action which will process the IEnumerable</param>
		/// <param name="timeout">Optional TimeSpan for a timeout</param>
		/// <returns>Task</returns>
		public Task ProcessExtendedAsync(Action<IEnumerable<Tuple<object, TEventArgs>>> processAction, TimeSpan? timeout = null)
		{
			// Start the registration inside the current thread
			var enumerable = FromExtended;
			if (timeout.HasValue)
			{
				return AsyncHelper.RunWithTimeout(() => processAction(enumerable), timeout.Value);
			}
			return Task.Run(() => processAction(enumerable));
		}

		/// <summary>
		///     Process events (IEnumerable with eventargs) in a background task, the task will only finish on an exception
		/// </summary>
		/// <param name="processAction">Action which will process the IEnumerable</param>
		/// <param name="timeout">Optional TimeSpan for a timeout</param>
		/// <returns>Task</returns>
		public Task ProcessAsync(Action<IEnumerable<TEventArgs>> processAction, TimeSpan? timeout = null)
		{
			// Start the registration inside the current thread
			var enumerable = From;
			return Task.Run(() => processAction(enumerable));
		}

		/// <summary>
		///     Create a QueueingEventHandler for handling only the eventArgs
		/// </summary>
		/// <returns>IEnumerable with TEventArgs</returns>
		public IEnumerable<TEventArgs> From
		{
			get { return FromExtended.Select(tuple => tuple.Item2); }
		}

		/// <summary>
		///     Create a QueueingEventHandler for handling the sender and the eventArgs
		/// </summary>
		/// <returns>IEnumerable with a tuple with object (sender) and TEventArgs</returns>
		public IEnumerable<Tuple<object, TEventArgs>> FromExtended
		{
			get
			{
				var handler = new QueueingEventHandler<TEventArgs>(this);
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
		///     This implements the OnEach in the non generic interface
		/// </summary>
		/// <typeparam name="TEventArgs2">The type for this method</typeparam>
		/// <param name="action">Action</param>
		/// <param name="predicate">Func</param>
		/// <returns>IEventHandler</returns>
		public IEventHandler OnEach<TEventArgs2>(Action<object, TEventArgs2> action, Func<object, TEventArgs2, bool> predicate = null)
		{
			return OnEach((o, e) => action(o, (TEventArgs2) (object) e));
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
				throw new ObjectDisposedException(nameof(SmartEvent), $"Can't be handling events for {EventName} after dispose.");
			}
			IList<IInternalEventHandler<TEventArgs>> eventHandlers;
			lock (_eventHandlers)
			{
				eventHandlers = _eventHandlers.ToList();
			}
			// Loop over all event handlers, and add the item to their BlockingCollection
			foreach (var eventHandler in eventHandlers)
			{
				try
				{
					eventHandler.Handle(sender, eventArgs);
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex);
				}
			}
		}

		/// <summary>
		///     Subscribe to the underlying event
		/// </summary>
		private void Subscribe()
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(SmartEvent), $"No registrations for {EventName} after dispose.");
			}
			if (!_isRegistered)
			{
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
		}

		/// <summary>
		///     Unsubscribe to the underlying event
		/// </summary>
		private void Unsubscribe()
		{
			if (_isRegistered)
			{
				_isRegistered = false;
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
		}

		/// <summary>
		///     Remove all registered IEventHandler
		/// </summary>
		private void UnsubscribeAllHandlers()
		{
			IList<IInternalEventHandler<TEventArgs>> eventHandlers;
			lock (_eventHandlers)
			{
				eventHandlers = _eventHandlers.ToList();
			}
			// Loop over all event handlers, and add the item to their BlockingCollection
			foreach (var eventHandler in eventHandlers)
			{
				Unsubscribe(eventHandler);
			}
		}
	}
}