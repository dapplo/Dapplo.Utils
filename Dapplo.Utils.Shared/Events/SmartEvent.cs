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
		private static readonly BindingFlags DefaultBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |BindingFlags.NonPublic;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventHandler">EventHandler</param>
		/// <typeparam name="TEventArgs"></typeparam>
		public static ISmartEvent<TEventArgs> FromEvent<TEventArgs>(ref EventHandler<TEventArgs> eventHandler)
		{
			return new SmartEvent<TEventArgs>(ref eventHandler);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TEventArgs">Typeof the event arguments</typeparam>
		/// <param name="objectContainingEvent">object which defines the event</param>
		/// <param name="eventName">nameof(object.event)</param>
		/// <returns>ISmartEvent</returns>
		public static ISmartEvent<TEventArgs> FromReflection<TEventArgs>(object objectContainingEvent, string eventName)
		{
			var objectType = objectContainingEvent.GetType();
			var eventInfo = objectType.GetEvent(eventName, DefaultBindingFlags);

			var eventField = objectType.GetField(eventName, DefaultBindingFlags);
			if (eventInfo == null || eventField == null)
			{
				throw new ArgumentException($"The event {eventName} does not exist in the supplied object.", nameof(eventName));
			}
			Type delegateType = eventInfo.EventHandlerType;
			MethodInfo invokeMethod = delegateType.GetMethod("Invoke", DefaultBindingFlags);
			if (invokeMethod == null)
			{
				throw new ArgumentException($"Couldn't find the invoke for the {eventName} event.");
			}

			var currentDelegates = eventField.GetValue(objectContainingEvent) as Delegate;
			return new SmartEvent<TEventArgs>(
				action =>
				{
					var newDelegateList = Delegate.Combine(currentDelegates, action);
					eventField.SetValue(objectContainingEvent, newDelegateList);
				},
				//action => removeMethod.Invoke(objectContainingEvent, new object[] { (Delegate)action }),
				action =>
				{
					var newDelegateList = Delegate.Remove(currentDelegates, action);
					eventField.SetValue(objectContainingEvent, newDelegateList);
				},
				(o, eventArgs) =>
				{
					var eventDelegate = (Delegate)eventField.GetValue(objectContainingEvent);
					eventDelegate?.DynamicInvoke(o, eventArgs);
				});
		}
	}

	/// <summary>
	/// This creates a smart event for the supplied event.
	/// </summary>
	/// <typeparam name="TEventArgs">the underlying type for the EventHandler</typeparam>
	public class SmartEvent<TEventArgs> : ISmartEvent<TEventArgs>
	{
		private readonly LogSource _log = new LogSource();
		private bool _disposedValue; // To detect redundant calls
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
		}

		/// <summary>
		/// Constructor for the EventHandler ref
		/// </summary>
		/// <param name="eventHandler"></param>
		internal SmartEvent(ref EventHandler<TEventArgs> eventHandler)
		{
			_eventHandler = eventHandler;
		}

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
			foreach (var smartEventHandler in eventHandlers)
			{
				try
				{
					if (smartEventHandler.Predicate(sender, eventArgs))
					{
						smartEventHandler.Action(sender, eventArgs);
					}
				}
				catch (Exception ex)
				{
					_log.Error().WriteLine(ex, "An exception occured while processing an event.");
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
				if (_addAction == null)
				{
					_eventHandler += HandleEvent;
				}
				else
				{
					_addAction(HandleEvent);
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
				if (_removeAction == null && _eventHandler != null)
				{
					// ReSharper disable once DelegateSubtraction
					_eventHandler -= HandleEvent;
				}
				else
				{
					_removeAction(HandleEvent);
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

			if (_eventHandler != null)
			{
				_eventHandler(sender, eventArgs);
			}
			else
			{
				_invokeAction(sender, eventArgs);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="smartEventHandler"></param>
		/// <returns></returns>
		public ISmartEvent<TEventArgs> Register(ISmartEventHandler<TEventArgs> smartEventHandler)
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
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="smartEventHandler"></param>
		/// <returns></returns>
		public ISmartEvent<TEventArgs> Unregister(ISmartEventHandler<TEventArgs> smartEventHandler)
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
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ISmartEventHandler<TEventArgs> On(Action<object, TEventArgs> action)
		{
			var smartEventHandler = new SmartEventHandler<TEventArgs>(this);
			smartEventHandler.Do(action);
			return smartEventHandler;
		}

		/// <summary>
		/// Create an ISmartEventHandler with a predicate, don't forget to register a do
		/// </summary>
		/// <returns>ISmartEventHandler</returns>
		public ISmartEventHandler<TEventArgs> When(Func<object, TEventArgs, bool> predicate)
		{
			var smartEventHandler = new SmartEventHandler<TEventArgs>(this);
			smartEventHandler.When(predicate);
			return smartEventHandler;
		}

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
