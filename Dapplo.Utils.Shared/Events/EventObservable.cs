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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
		///     Removes all the event handlers from the defined events in an object
		///     This is usefull to do internally, after a MemberwiseClone is made, to prevent memory leaks
		/// </summary>
		/// <param name="instance">object instance where events need to be removed</param>
		/// <param name="regExPattern">Regular expression to match the even names, null for alls</param>
		/// <returns>number of removed events</returns>
		public static int RemoveEventHandlers(object instance, string regExPattern = null)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}
			var count = 0;
			Regex regex = null;
			if (!string.IsNullOrEmpty(regExPattern))
			{
				regex = new Regex(regExPattern);
			}
			var typeWithEvents = instance.GetType();
			foreach (var eventInfo in typeWithEvents.GetEvents(AllBindings))
			{
				if ((regex != null) && !regex.IsMatch(eventInfo.Name))
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
				if ((eventDelegate == null) || (removeMethod == null))
				{
					continue;
				}
				count += eventDelegate.GetInvocationList().Length;
				removeMethod.Invoke(instance, new object[] {eventDelegate});
			}
			return count;
		}

		/// <summary>
		///     Register the supplied action to all events in the targe class.
		///     Although this is somewhat restrictive, it might be usefull for logs
		/// </summary>
		/// <param name="targetObject"></param>
		/// <returns>IList of IEventObservable which can be dispose with DisposeAll</returns>
		public static IEnumerable<IEventObservable<TEventArgs>> EventsIn<TEventArgs>(object targetObject)
			where TEventArgs : class
		{
			foreach (var eventInfo in targetObject.GetType().GetEvents(AllBindings))
			{
				var eventHandlerInvokeDelegate = eventInfo.EventHandlerType.GetMethod("Invoke");
				var eventType = eventHandlerInvokeDelegate.GetParameters().Last().ParameterType;
				if (!typeof(TEventArgs).IsAssignableFrom(eventType))
				{
					continue;
				}
				var constructedType = typeof(EventObservable<>).MakeGenericType(eventType);
				var args = new[] {targetObject, eventInfo.Name};
				var eventObservable = (IEventObservable<TEventArgs>) Activator.CreateInstance(constructedType, AllBindings, null, args, null, null);
				yield return eventObservable;
			}
		}

		/// <summary>
		///     Create a EventObservable from the event which can be find in the object by the specified event name.
		/// </summary>
		/// <typeparam name="TEventArgs">Typeof the event arguments</typeparam>
		/// <param name="targetObject">object which defines the event</param>
		/// <param name="eventName">nameof(object.event)</param>
		/// <returns>IEventObservable</returns>
		public static IEventObservable<TEventArgs> From<TEventArgs>(object targetObject, string eventName)
			where TEventArgs : EventArgs
		{
			// Create the EventObservable, the Reflection is in there.
			return new EventObservable<TEventArgs>(targetObject, eventName);
		}

		/// <summary>
		///     Create IEventObservable for the supplied INotifyPropertyChanged
		/// </summary>
		/// <param name="notifyPropertyChanged">INotifyPropertyChanged</param>
		/// <returns>IEventObservable for PropertyChangedEventArgs</returns>
		public static IEventObservable<PropertyChangedEventArgs> From(INotifyPropertyChanged notifyPropertyChanged)
		{
			return new EventObservable<PropertyChangedEventArgs>(notifyPropertyChanged, nameof(INotifyPropertyChanged.PropertyChanged));
		}

		/// <summary>
		///     Create IEventObservable for the supplied INotifyPropertyChanging
		/// </summary>
		/// <param name="notifyPropertyChanging">INotifyPropertyChanging</param>
		/// <returns>IEventObservable for PropertyChangingEventArgs</returns>
		public static IEventObservable<PropertyChangingEventArgs> From(INotifyPropertyChanging notifyPropertyChanging)
		{
			return new EventObservable<PropertyChangingEventArgs>(notifyPropertyChanging, nameof(INotifyPropertyChanging.PropertyChanging));
		}
	}

	/// <summary>
	///     This creates a EventObservable for the supplied event information.
	/// </summary>
	/// <typeparam name="TEventArgs">the underlying type for the EventHandler</typeparam>
	public class EventObservable<TEventArgs> : IEventObservable<TEventArgs>
		where TEventArgs : class
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly LogSource Log = new LogSource();
		private readonly MethodInfo _addMethod;
		private readonly EventInfo _eventInfo;
		private readonly Delegate _handleEventDelegate;
		private readonly MethodInfo _invokeMethod;

		private readonly IList<IObserver<IEventData<TEventArgs>>> _observers = new List<IObserver<IEventData<TEventArgs>>>();
		private readonly MethodInfo _removeMethod;
		private readonly WeakReference _targetObject;
		private bool _disposedValue; // To detect redundant calls
		private bool _subscribedToEvents;

		/// <summary>
		///     Constructor for the FieldInfo
		/// </summary>
		/// <param name="targetObject">Object containing the event field/property</param>
		/// <param name="eventName">The name of the event field / property</param>
		internal EventObservable(object targetObject, string eventName)
		{
			var targetType = targetObject.GetType();
			_addMethod = targetType.GetMethod($"add_{eventName}", EventObservable.AllBindings);
			_removeMethod = targetType.GetMethod($"remove_{eventName}", EventObservable.AllBindings);
			_invokeMethod = targetType.GetMethod($"invoke_{eventName}", EventObservable.AllBindings);
			_eventInfo = targetType.GetEvent(eventName, EventObservable.AllBindings);
			EventName = eventName;
			EventArgumentType = typeof(TEventArgs);

			_targetObject = new WeakReference(targetObject);

			// Sometimes the event handler only uses a single argument, for this check the parameter count of the delegate
			var useWithSender = true;
			var eventHandlerType = _eventInfo?.EventHandlerType ?? _addMethod?.GetParameters()[0].ParameterType;
			var eventHandlerInvokeDelegate = eventHandlerType?.GetMethod("Invoke", EventObservable.AllBindings);
			if (eventHandlerInvokeDelegate != null)
			{
				useWithSender = eventHandlerInvokeDelegate.GetParameters().Length >= 2;
			}
			// Now decide on the handler, in the end both will function the same as we store the target and pass this as sender.
			var eventHandleMethodName = useWithSender ? nameof(HandleEvent) : nameof(HandleEventWithoutSender);
			var handleEventMethodInfo = GetType().GetMethod(eventHandleMethodName, EventObservable.AllBindings);
			_handleEventDelegate = handleEventMethodInfo.CreateDelegate(eventHandlerType, this);
		}

		/// <summary>
		///     The name of the underlying event, might be null if not supplied.
		///     Can be used to find an event using LINQ on EventObservable.EventsIn
		/// </summary>
		public string EventName { get; }

		/// <summary>
		///     The object which contains the event, could be null depending on how the event was registered
		/// </summary>
		public object Source => _targetObject?.Target;

		/// <summary>
		///     The type for the Argument
		///     Can be used to find an event using LINQ on EventObservable.EventsIn
		/// </summary>
		public Type EventArgumentType { get; }

		/// <summary>
		///     Triggers an event, this will try different techniques
		/// </summary>
		/// <param name="eventData">IEventData</param>
		/// <returns>true if the trigger call actually did something</returns>
		public bool Trigger(IEventData eventData)
		{
			if (_disposedValue)
			{
				throw new ObjectDisposedException(nameof(EventObservable), $"Can't trigger {EventName} after being disposed.");
			}

			// Set the event name, if not done already
			if (eventData.Name == null)
			{
				eventData = EventData.Create(eventData.Sender, eventData.Args as TEventArgs, EventName);
				eventData.Name = EventName;
			}

			try
			{
				var targetObject = _targetObject?.Target;
				// Do nothing if the target object was garbage collected
				if (targetObject == null)
				{
					Log.Warn().WriteLine($"Target object to trigger {EventName} was garbage collected.");
					return false;
				}
				// Raise via reflection
				var raiseMethodInfo = _eventInfo?.RaiseMethod;
				if (raiseMethodInfo != null)
				{
					raiseMethodInfo.Invoke(targetObject, new[] {eventData.Sender, eventData.Args});
					return true;
				}
				// Invoke by retrieving the delegate of the field
				var fieldInfo = targetObject.GetType().GetField(EventName, EventObservable.AllBindings);
				if (fieldInfo != null)
				{
					var eventDelegate = (Delegate) fieldInfo.GetValue(targetObject);
					eventDelegate?.DynamicInvoke(eventData.Sender, eventData.Args);
					// Return, even if eventDelegate was null.. it might have been empty!!
					return eventDelegate != null;
				}
				// Use a special invoke method, created by Dapplo.InterfaceImpl
				if (_invokeMethod != null)
				{
					_invokeMethod.Invoke(targetObject, new[] {eventData.Sender, eventData.Args});
					return true;
				}
				Log.Warn().WriteLine("Can't trigger the event {0}", EventName);
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, $"An exception occured while trying to trigger {EventName}");
			}
			return false;
		}

		/// <summary>
		///     Implement IDisposable.Dispose()
		/// </summary>
		public void Dispose()
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
			if (observer == null)
			{
				throw new ArgumentNullException(nameof(observer));
			}
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
				// Check weak reference, throw exception (as the called SHOULD know this)
				var targetObject = _targetObject?.Target;
				if (targetObject == null)
				{
					var message = $"Target object with containing {EventName} event was garbage collected.";
					Log.Error().WriteLine(message);
					throw new ObjectDisposedException(EventName, message);
				}
				if (_eventInfo != null)
				{
					_eventInfo.AddMethod.Invoke(targetObject, new object[] {_handleEventDelegate});
				}
				else
				{
					_addMethod.Invoke(targetObject, new object[] {_handleEventDelegate});
				}
			}
			// TODO: Check if we need to return a Disposable when the subscribe didn't do anything (e.g. when it was already subscribed)
			return Disposable.Create(() => Unsubscribe(observer));
		}

		/// <summary>
		///     Unsubscribe the IObserver, this is used internally
		/// </summary>
		/// <param name="observer">IObserver</param>
		private void Unsubscribe(IObserver<IEventData<TEventArgs>> observer)
		{
			if (observer == null)
			{
				throw new ArgumentNullException(nameof(observer));
			}

			lock (_observers)
			{
				if (!_observers.Remove(observer))
				{
					return;
				}
				// Unsubscribe the HandleEvent from the event as no-one is interested
				if ((_observers.Count == 0) && _subscribedToEvents)
				{
					_subscribedToEvents = false;
					// There are no more subscriptions, so the event registration can be removed.
					// Check weak reference, do nothing if the object is garbage collected
					var targetObject = _targetObject?.Target;
					if (targetObject != null)
					{
						if (_eventInfo != null)
						{
							_eventInfo.RemoveMethod.Invoke(targetObject, new object[] {_handleEventDelegate});
						}
						else
						{
							_removeMethod.Invoke(targetObject, new object[] {_handleEventDelegate});
						}
					}
				}
				// signal that it was removed by telling that there will be no more data
				observer.OnCompleted();
			}
		}

		/// <summary>
		///     This will handle the event in the case where the event handler only has one argument.
		///     It will take the target object, where the event resides, as the sender.
		/// </summary>
		/// <param name="eventArgs">TEventArgs</param>
		private void HandleEventWithoutSender(TEventArgs eventArgs)
		{
			// Check weak reference, if the target was disposed null will be passed
			var targetObject = _targetObject?.Target;
			HandleEvent(targetObject, eventArgs);
		}

		/// <summary>
		///     This handles the actual event by placing it into the BlockingCollection
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