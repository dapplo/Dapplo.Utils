using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dapplo.Utils
{
	/// <summary>
	/// Workaround for Observable.FromEvent not supporting single parameter event handlers
	/// </summary>
	public static class EventObservable
	{
		private const BindingFlags AllBindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
				removeMethod.Invoke(instance, new object[] { eventDelegate });
			}
			return count;
		}

		/// <summary>
		/// Create an IEnumerable with IObservable for every event in the target object
		/// </summary>
		/// <param name="targetObject">object</param>
		/// <returns>IList of IObservable which can be dispose with DisposeAll</returns>
		public static IEnumerable<IObservable<TEventArgs>> EventsIn<TEventArgs>(object targetObject) where TEventArgs : class
		{
			return targetObject.GetType().GetEvents(AllBindings).Select(eventInfo => FromEvent<TEventArgs>(targetObject, eventInfo.Name));
		}

		/// <summary>
		/// A workaround for Observable.FromEvent not working with standard events
		/// </summary>
		/// <typeparam name="TEventArgs">Type of the event</typeparam>
		/// <param name="targetObject">object</param>
		/// <param name="eventName">string with the name of the event</param>
		/// <returns>IObservable of TEventArgs</returns>
		public static IObservable<TEventArgs> FromEvent<TEventArgs>(object targetObject, string eventName) where TEventArgs : class
		{
			if (targetObject == null)
			{
				throw new ArgumentNullException(nameof(targetObject));
			}
			// If the event is standard conform, throw exception
			if (targetObject.GetType().GetEvent(eventName, AllBindings)?.EventHandlerType.GetMethod("Invoke", AllBindings)?.GetParameters().Length != 1)
			{
				throw new ArgumentException($"Event {eventName} doesn't exist or is not standard conform (use Observable.FromEvent)", nameof(eventName));
			}

			// Used to make sure the remove handler uses the same value as the add
			var delegateStore = new object[1];

			return Observable.FromEvent<TEventArgs>(
				// Add handler action
				handler =>
				{
					var addMethod = targetObject.GetType().GetMethod($"add_{eventName}", AllBindings);
					// ReSharper disable once PossibleNullReferenceException (this was already checked above!)
					var eventHandlerType = targetObject.GetType().GetEvent(eventName, AllBindings).EventHandlerType;
					delegateStore[0] = Delegate.CreateDelegate(eventHandlerType, handler.Target, handler.Method);
					addMethod.Invoke(targetObject, new[] { delegateStore[0] });
				},
				// Remove handler action
				handler =>
				{
					targetObject.GetType().GetMethod($"remove_{eventName}", AllBindings).Invoke(targetObject, new[] { delegateStore[0] });
				});
		}
	}
}
