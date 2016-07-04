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
using System.Collections.Generic;
using System.Linq;
using Dapplo.Log.Facade;
using System.Reflection;

namespace Dapplo.Utils.Events
{
	/// <summary>
	/// Static methods to create a generic SmartEvent
	/// </summary>
	public static class SmartEvent
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly BindingFlags DefaultBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |BindingFlags.NonPublic;

		/// <summary>
		/// Dispose all ISmartEvents in the list
		/// </summary>
		/// <param name="smartEvents">IList with smart events</param>
		public static void DisposeAll(IList<ISmartEvent> smartEvents)
		{
			foreach (var smartEvent in smartEvents)
			{
				smartEvent.Dispose();
			}
		}

		/// <summary>
		/// Create a SmartEvent for the referenced EventHandler
		/// </summary>
		/// <typeparam name="TEventArgs">Type for the event</typeparam>
		/// <param name="eventHandler">EventHandler</param>
		/// <param name="registeredSmartEvents">If you want to keep track of the ISmartEvent registrations, you can pass a list here</param>
		public static ISmartEvent<TEventArgs> FromEventHandler<TEventArgs>(ref EventHandler<TEventArgs> eventHandler, IList<ISmartEvent> registeredSmartEvents = null) where TEventArgs : EventArgs
		{
			var smartEvent = new SmartEvent<TEventArgs>(ref eventHandler);
			registeredSmartEvents?.Add(smartEvent);
			return smartEvent;
		}

		/// <summary>
		/// Create a SmartEVent from the event which can be find in the object by the specified event name.
		/// </summary>
		/// <typeparam name="TEventArgs">Typeof the event arguments</typeparam>
		/// <param name="objectContainingEvent">object which defines the event</param>
		/// <param name="eventName">nameof(object.event)</param>
		/// <param name="registeredSmartEvents">If you want to keep track of the ISmartEvent registrations, you can pass a list here</param>
		/// <returns>ISmartEvent</returns>
		public static ISmartEvent<TEventArgs> FromReflection<TEventArgs>(object objectContainingEvent, string eventName, IList<ISmartEvent> registeredSmartEvents = null) where TEventArgs : EventArgs
		{
			// Use reflection to get the EventInfo object for the Event
			var objectType = objectContainingEvent.GetType();
			var eventInfo = objectType.GetEvent(eventName, DefaultBindingFlags);

			// Use reflection to get the FieldInfo object for the Event
			var eventField = objectType.GetField(eventName, DefaultBindingFlags);
			if (eventInfo == null || eventField == null)
			{
				throw new ArgumentException($"The event {eventName} does not exist in the supplied object.", nameof(eventName));
			}

			var smartEvent = new SmartEvent<TEventArgs>(
				action =>
				{
					var currentDelegates = eventField.GetValue(objectContainingEvent) as Delegate;
					var newDelegateList = Delegate.Combine(currentDelegates, action);
					if (newDelegateList != null && !eventField.FieldType.IsInstanceOfType(newDelegateList))
					{
						Log.Warn().WriteLine("Can't assign {0} to {1}", newDelegateList.GetType(), eventField.FieldType);
					}
					eventField.SetValue(objectContainingEvent, newDelegateList);
				},
				//action => removeMethod.Invoke(objectContainingEvent, new object[] { (Delegate)action }),
				action =>
				{
					var currentDelegates = eventField.GetValue(objectContainingEvent) as Delegate;
					var newDelegateList = Delegate.Remove(currentDelegates, action);
					if (newDelegateList != null && !eventField.FieldType.IsInstanceOfType(newDelegateList))
					{
						Log.Warn().WriteLine("Can't assign {0} to {1}", newDelegateList.GetType(), eventField.FieldType);
					}
					eventField.SetValue(objectContainingEvent, newDelegateList);
				},
				(o, eventArgs) =>
				{
					var eventDelegate = (Delegate)eventField.GetValue(objectContainingEvent);
					eventDelegate?.DynamicInvoke(o, eventArgs);
				});

			registeredSmartEvents?.Add(smartEvent);
			return smartEvent;
		}
	}

	/// <summary>
	/// This creates a smart event for the supplied event.
	/// </summary>
	/// <typeparam name="TEventArgs">the underlying type for the EventHandler</typeparam>
	public class SmartEvent<TEventArgs> : ISmartEvent<TEventArgs>
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly LogSource Log = new LogSource();
		private bool _disposedValue; // To detect redundant calls
		private readonly bool _useEventHandler;
		private EventHandler<TEventArgs> _eventHandler;
		private readonly Action<EventHandler<TEventArgs>> _addAction;
		private readonly Action<EventHandler<TEventArgs>> _removeAction;
		private readonly Action<object, TEventArgs> _invokeAction;
		private bool _isRegistered;
		private readonly IList<ISmartEventHandler<TEventArgs>> _eventHandlers = new List<ISmartEventHandler<TEventArgs>>();

		/// <summary>
		/// Constructor for the EventHandler ref
		/// </summary>
		/// <param name="addAction">Action to add the delegate</param>
		/// <param name="removeAction">Action to remove the delegate</param>
		/// <param name="invokeAction">Action to invoke the event</param>
		internal SmartEvent(Action<EventHandler<TEventArgs>> addAction, Action<EventHandler<TEventArgs>> removeAction, Action<object, TEventArgs> invokeAction)
		{
			_addAction = addAction;
			_removeAction = removeAction;
			_invokeAction = invokeAction;
			_useEventHandler = false;
		}

		/// <summary>
		/// Constructor for the EventHandler ref
		/// </summary>
		/// <param name="eventHandler"></param>
		internal SmartEvent(ref EventHandler<TEventArgs> eventHandler)
		{
			_eventHandler = eventHandler;
			_useEventHandler = true;
		}

		/// <summary>
		/// This handles the actual event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void HandleEvent(object sender, TEventArgs eventArgs)
		{
			if (_disposedValue)
			{
				throw new InvalidOperationException("Can't be handling events after disposing.");
			}
			IList<ISmartEventHandler<TEventArgs>> eventHandlers;
			lock (_eventHandlers)
			{
				eventHandlers = _eventHandlers.ToList();
			}
			// Loop over all event handlers
			foreach (var smartEventHandler in eventHandlers)
			{
				try
				{
					// Call the predicate to decide if the action needs to be called
					if (smartEventHandler.Predicate(sender, eventArgs))
					{
						if (smartEventHandler.First)
						{
							Unregister(smartEventHandler);
						}
						smartEventHandler.Action(sender, eventArgs);
					}
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "An exception occured while processing an event.");
				}
			}
		}

		/// <summary>
		/// Register from the underlying event
		/// </summary>
		private void RegisterHandleEvent()
		{
			if (_disposedValue)
			{
				throw new InvalidOperationException("Can't be register after being disposed.");
			}
			if (!_isRegistered)
			{
				_isRegistered = true;
				if (_useEventHandler)
				{
					_eventHandler += HandleEvent;
				}
				else
				{
					_addAction?.Invoke(HandleEvent);
				}
			}
		}


		/// <summary>
		/// Register to the underlying event
		/// </summary>
		private void UnregisterHandleEvent()
		{
			if (_isRegistered)
			{
				_isRegistered = false;
				if (_useEventHandler && _eventHandler != null)
				{
					// ReSharper disable once DelegateSubtraction
					_eventHandler -= HandleEvent;
				}
				else if (!_useEventHandler)
				{
					_removeAction?.Invoke(HandleEvent);
				}
			}
		}

		/// <summary>
		/// Triggers the event
		/// </summary>
		/// <param name="sender">the sender of the event</param>
		/// <param name="eventArgs">TEventArgs</param>
		public void Trigger(object sender, TEventArgs eventArgs)
		{
			if (_disposedValue)
			{
				throw new InvalidOperationException("Can't trigger after being disposed.");
			}

			try
			{
				if (_useEventHandler)
				{
					_eventHandler?.Invoke(sender, eventArgs);
				}
				else
				{
					_invokeAction(sender, eventArgs);
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "An exception occured while triggering an event.");
			}
		}

		/// <summary>
		/// Register a SmartEventHandler which helps to process the event
		/// </summary>
		/// <param name="smartEventHandler">ISmartEventHandler</param>
		internal void Register(ISmartEventHandler<TEventArgs> smartEventHandler)
		{
			if (_disposedValue)
			{
				throw new InvalidOperationException("Can't register after being disposed.");
			}

			RegisterHandleEvent();
			lock (_eventHandlers)
			{
				_eventHandlers.Add(smartEventHandler);
			}
		}

		/// <summary>
		/// Unregister the SmartEventhandler
		/// </summary>
		/// <param name="smartEventHandler">ISmartEventHandler</param>
		internal void Unregister(ISmartEventHandler<TEventArgs> smartEventHandler)
		{
			if (_disposedValue)
			{
				throw new InvalidOperationException("Can't Unregister after being disposed.");
			}
			lock (_eventHandlers)
			{
				_eventHandlers.Remove(smartEventHandler);
			}
			if (_eventHandlers.Count == 0)
			{
				UnregisterHandleEvent();
			}
		}

		#region SmartEventHandler
		/// <summary>
		/// Create a ISmartEventHandler which responds to every "matching" event
		/// </summary>
		/// <returns>ISmartEventHandler</returns>
		public ISmartEventHandler<TEventArgs> Every
		{
			get
			{
				var smartEventHandler = new SmartEventHandler<TEventArgs>(this)
				{
					First = false
				};
				return smartEventHandler;
			}
		}

		/// <summary>
		/// Create a ISmartEventHandler which responds to the first event
		/// </summary>
		/// <returns>ISmartEventHandler</returns>
		public ISmartEventHandler<TEventArgs> First
		{
			get
			{
				var smartEventHandler = new SmartEventHandler<TEventArgs>(this)
				{
					First = true
				};
				return smartEventHandler;
			}
		}
		#endregion

		/// <summary>
		/// Implement IDisposable.Dispose()
		/// </summary>
		void IDisposable.Dispose()
		{
			UnregisterHandleEvent();
			_disposedValue = true;
		}
	}
}
