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
using System.ComponentModel;
using System.Text.RegularExpressions;
using Dapplo.Log.Facade;
using Dapplo.Utils.Events;

#endregion

namespace Dapplo.Utils.Extensions
{
	/// <summary>
	///     Extensions to simplify the usage of INotifyPropertyChanged and INotifyPropertyChanging
	/// </summary>
	public static class NotifyPropertyExtensions
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// Shortcut to create an EventObservable for a INotifyPropertyChanged
		/// </summary>
		/// <param name="notifyPropertyChanged">INotifyPropertyChanged</param>
		/// <returns>IEventObservable for PropertyChangedEventArgs</returns>
		public static IEventObservable<PropertyChangedEventArgs> ToObservable(this INotifyPropertyChanged notifyPropertyChanged)
		{
			if (notifyPropertyChanged == null)
			{
				throw new ArgumentNullException(nameof(notifyPropertyChanged));
			}
			return EventObservable.From(notifyPropertyChanged);
		}

		/// <summary>
		///     Automatically call the update action when the INotifyPropertyChanged fires
		///     If the is called on a DI object, make sure it's available.
		///     When using MEF, it would be best to call this from IPartImportsSatisfiedNotification.OnImportsSatisfied
		/// </summary>
		/// <param name="notifyPropertyChangedEventObservable">EventObservable for PropertyChangedEventArgs</param>
		/// <param name="notifyAction">Action to call on active and update, the argument is the property name</param>
		/// <param name="pattern">Optional Regex pattern to match the property name in the event against, null matches everything</param>
		/// <param name="run">specify if the action also needs to be run, true is the default, this might be needed to make sure the property or properties are updated</param>
		/// <returns>an IDisposable, calling Dispose on this will stop everything</returns>
		public static IDisposable OnPropertyChanged(this IEventObservable<PropertyChangedEventArgs> notifyPropertyChangedEventObservable, Action<string> notifyAction, string pattern = null, bool run = true)
		{
			if (notifyPropertyChangedEventObservable == null)
			{
				throw new ArgumentNullException(nameof(notifyPropertyChangedEventObservable));
			}
			if (notifyAction == null)
			{
				throw new ArgumentNullException(nameof(notifyAction));
			}

			// Test if we need to run the action now, this might be needed to make sure the property or properties are updated
			if (run)
			{
				Log.Verbose().WriteLine("Running your action, as run is true.");
				notifyAction("*");
			}

			// Create predicate
			Func<IEventData<PropertyChangedEventArgs>, bool> predicate = null;
			if (!string.IsNullOrEmpty(pattern))
			{
				predicate = propertyChangedEventArgs =>
				{
					try
					{
						var propertyName = propertyChangedEventArgs.Args.PropertyName;
						return string.IsNullOrEmpty(propertyName) || propertyName == "*" || string.IsNullOrEmpty(pattern) || Regex.IsMatch(propertyName, pattern);
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex, "Error in predicate for OnPropertyChanged");
					}
					return false;
				};
			}

			return notifyPropertyChangedEventObservable.ForEach(pce => notifyAction(pce.Args.PropertyName), predicate);
		}

		/// <summary>
		///     Automatically call the update action when the INotifyPropertyChanged fires
		///     If the is called on a DI object, make sure it's available.
		///     When using MEF, it would be best to call this from IPartImportsSatisfiedNotification.OnImportsSatisfied
		/// </summary>
		/// <param name="notifyPropertyChanged">INotifyPropertyChanged</param>
		/// <param name="updateAction">Action to call on active and update, the argument is the property name</param>
		/// <param name="pattern">Optional Regex pattern to match the property name in the event against, null matches everything</param>
		/// <param name="run">specify if the action also needs to be run, true is the default, this might be needed to make sure the property or properties are updated</param>
		/// <returns>an IDisposable, calling Dispose on this will stop everything</returns>
		public static IDisposable OnPropertyChanged(this INotifyPropertyChanged notifyPropertyChanged, Action<string> updateAction, string pattern = null, bool run = true)
		{
			return notifyPropertyChanged.ToObservable().OnPropertyChanged(updateAction, pattern, run);
		}

		/// <summary>
		/// Shortcut to create an EventObservable for a INotifyPropertyChanging
		/// </summary>
		/// <param name="notifyPropertyChanging">INotifyPropertyChanging</param>
		/// <returns>IEventObservable for PropertyChangingEventArgs</returns>
		public static IEventObservable<PropertyChangingEventArgs> ToObservable(this INotifyPropertyChanging notifyPropertyChanging)
		{
			if (notifyPropertyChanging == null)
			{
				throw new ArgumentNullException(nameof(notifyPropertyChanging));
			}
			return EventObservable.From(notifyPropertyChanging);
		}

		/// <summary>
		///     Automatically call the update action when the INotifyPropertyChanging fires
		///     If the is called on a DI object, make sure it's available.
		///     When using MEF, it would be best to call this from IPartImportsSatisfiedNotification.OnImportsSatisfied
		/// </summary>
		/// <param name="notifyPropertyChangedEventObservable">IEventObservable for PropertyChangingEventArgs</param>
		/// <param name="notifyAction">Action to call on active and update, the argument is the property name</param>
		/// <param name="pattern">Optional Regex pattern to match the property name in the event against, null matches everything</param>
		/// <param name="run">specify if the action also needs to be run, true is the default, this might be needed to make sure the property or properties are updated</param>
		/// <returns>an IDisposable, calling Dispose on this will unsubscribe the event handler</returns>
		public static IDisposable OnPropertyChanging(this IEventObservable<PropertyChangingEventArgs> notifyPropertyChangedEventObservable, Action<string> notifyAction, string pattern = null, bool run = true)
		{
			if (notifyPropertyChangedEventObservable == null)
			{
				throw new ArgumentNullException(nameof(notifyPropertyChangedEventObservable));
			}
			if (notifyAction == null)
			{
				throw new ArgumentNullException(nameof(notifyAction));
			}

			// Test if we need to run the action now, this might be needed to make sure the property or properties are updated
			if (run)
			{
				Log.Verbose().WriteLine("Running your action, as run is true.");
				notifyAction("*");
			}

			// Create predicate
			Func<IEventData<PropertyChangingEventArgs>, bool> predicate = null;
			if (!string.IsNullOrEmpty(pattern))
			{
				predicate = propertyChangingEventArgs =>
				{
					try
					{
						var propertyName = propertyChangingEventArgs.Args.PropertyName;
						return string.IsNullOrEmpty(propertyName) || propertyName == "*" || string.IsNullOrEmpty(pattern) || Regex.IsMatch(propertyName, pattern);
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex, "Error in predicate for OnPropertyChanging");
					}
					return false;
				};
			}
			return notifyPropertyChangedEventObservable.ForEach(pce => notifyAction(pce.Args.PropertyName), predicate);
		}

		/// <summary>
		///     Automatically call the update action when the INotifyPropertyChanging fires
		///     If the is called on a DI object, make sure it's available.
		///     When using MEF, it would be best to call this from IPartImportsSatisfiedNotification.OnImportsSatisfied
		/// </summary>
		/// <param name="notifyPropertyChanging">INotifyPropertyChanging</param>
		/// <param name="notifyAction">Action to call on active and update, the argument is the property name</param>
		/// <param name="pattern">Optional Regex pattern to match the property name in the event against, null matches everything</param>
		/// <param name="run">specify if the action also needs to be run, true is the default, this might be needed to make sure the property or properties are updated</param>
		/// <returns>an IDisposable, calling Dispose on this will unsubscribe the event handler</returns>
		public static IDisposable OnPropertyChanging(this INotifyPropertyChanging notifyPropertyChanging, Action<string> notifyAction, string pattern = null, bool run = true)
		{
			return notifyPropertyChanging.ToObservable().OnPropertyChanging(notifyAction, pattern, run);
		}
	}
}