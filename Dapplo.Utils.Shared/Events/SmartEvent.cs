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
		/// <param name="eventName">The name of the event, e.g. via nameof(object.event), used for logging and finding the ISmartEvent</param>
		/// <param name="registeredSmartEvents">If you want to keep track of the ISmartEvent registrations, you can pass a list here</param>
		public static ISmartEvent<TEventArgs> FromEventHandler<TEventArgs>(ref EventHandler<TEventArgs> eventHandler, string eventName = null, IList<ISmartEvent> registeredSmartEvents = null) where TEventArgs : EventArgs
		{
			var smartEvent = new SmartEvent<TEventArgs>(ref eventHandler, eventName);
			registeredSmartEvents?.Add(smartEvent);
			return smartEvent;
		}

		/// <summary>
		/// Create a SmartEvent from the event which can be find in the object by the specified event name.
		/// </summary>
		/// <typeparam name="TEventArgs">Typeof the event arguments</typeparam>
		/// <param name="targetObject">object which defines the event</param>
		/// <param name="eventName">nameof(object.event)</param>
		/// <param name="registeredSmartEvents">If you want to keep track of the ISmartEvent registrations, you can pass a list here</param>
		/// <returns>ISmartEvent</returns>
		public static ISmartEvent<TEventArgs> FromReflection<TEventArgs>(object targetObject, string eventName, IList<ISmartEvent> registeredSmartEvents = null) where TEventArgs : EventArgs
		{
			// Create the SmartEvent, the Reflection is in there.
			var smartEvent = new SmartEvent<TEventArgs>(targetObject, eventName);
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
		private static readonly BindingFlags DefaultBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private bool _disposedValue; // To detect redundant calls
		private readonly bool _useEventHandler;
		private EventHandler<TEventArgs> _eventHandler;
		private readonly object _targetObject;
		private readonly EventInfo _eventInfo;
		private readonly Delegate _handleEventDelegate;
		private bool _isRegistered;
		private readonly IList<ISmartEventHandler<TEventArgs>> _eventHandlers = new List<ISmartEventHandler<TEventArgs>>();

		/// <summary>
		/// The name of the underlying event, might be null if not supplied
		/// </summary>
		public string EventName { get; }

		/// <summary>
		/// Constructor for the FieldInfo
		/// </summary>
		/// <param name="targetObject">Object containing the event field/property</param>
		/// <param name="eventName">The name of the event field / property</param>
		internal SmartEvent(object targetObject, string eventName)
		{
			_eventInfo = targetObject.GetType().GetEvent(eventName, DefaultBindingFlags);
			if (_eventInfo == null)
			{
				throw new ArgumentException($"The event {eventName} cannot be bound by FromReflection as it does not exist in the supplied object.", nameof(eventName));
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
		/// Constructor for the EventHandler ref
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
		/// This will handle the event in the case where the event handler only has one argument
		/// </summary>
		/// <param name="eventArgs">TEventArgs</param>
		private void HandleEventWithoutSender(TEventArgs eventArgs)
		{
			HandleEvent(_targetObject, eventArgs);
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
				throw new ObjectDisposedException(nameof(SmartEvent), $"Can't be handling events for {EventName} after dispose.");
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
						if (smartEventHandler.NeedsUi && !UiContext.HasUiAccess)
						{
							UiContext.RunOn(() => smartEventHandler.Action.Invoke(sender, eventArgs)).Wait();
						}
						else
						{
							smartEventHandler.Action(sender, eventArgs);
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, $"An exception occured while processing event {EventName}");
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
				throw new ObjectDisposedException(nameof(SmartEvent), $"No registrations for {EventName} after dispose.");
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
					_eventInfo.AddMethod.Invoke(_targetObject, new object[] { _handleEventDelegate });
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
					// Unregister the _handleEventDelegate via reflection
					_eventInfo.RemoveMethod.Invoke(_targetObject, new object[] { _handleEventDelegate });
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
						raiseMethodInfo.Invoke(_targetObject, new[] { sender, eventArgs });
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
		/// Register a SmartEventHandler which helps to process the event
		/// </summary>
		/// <param name="smartEventHandler">ISmartEventHandler</param>
		internal void Register(ISmartEventHandler<TEventArgs> smartEventHandler)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(SmartEvent), $"Can't register {EventName} after dispose.");
			}

			lock (_eventHandlers)
			{
				_eventHandlers.Add(smartEventHandler);
			}
			RegisterHandleEvent();
		}

		/// <summary>
		/// Unregister the SmartEventhandler
		/// </summary>
		/// <param name="smartEventHandler">ISmartEventHandler</param>
		internal void Unregister(ISmartEventHandler<TEventArgs> smartEventHandler)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(SmartEvent), $"Can't Unregister {EventName} after dispose.");
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
