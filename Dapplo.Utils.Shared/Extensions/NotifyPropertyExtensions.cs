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
		///     Automatically call the update action when the INotifyPropertyChanged fires
		///     If the is called on a DI object, make sure it's available.
		///     When using MEF, it would be best to call this from IPartImportsSatisfiedNotification.OnImportsSatisfied
		/// </summary>
		/// <param name="notifyPropertyChanged">INotifyPropertyChanged</param>
		/// <param name="updateAction">Action to call on active and update, the argument is the property name</param>
		/// <param name="pattern">Optional Regex pattern to match the property name in the event against, null matches everything</param>
		/// <returns>an IDisposable, calling Dispose on this will stop everything</returns>
		public static IDisposable OnPropertyChanged(this INotifyPropertyChanged notifyPropertyChanged, Action<string> updateAction, string pattern = null)
		{
			if (notifyPropertyChanged == null)
			{
				throw new ArgumentNullException(nameof(notifyPropertyChanged));
			}
			if (updateAction == null)
			{
				throw new ArgumentNullException(nameof(updateAction));
			}

			// Create the action
			var notifyAction = WrapNotifyProperty(updateAction, pattern, nameof(OnPropertyChanged));
			// Always run the action once, to make sure the property or properties are updated
			notifyAction("*");
			return EventObservable.From(notifyPropertyChanged).OnEach(pce => notifyAction(pce.Args.PropertyName));
		}

		/// <summary>
		///     Automatically call the update action when the INotifyPropertyChanging fires
		///     If the is called on a DI object, make sure it's available.
		///     When using MEF, it would be best to call this from IPartImportsSatisfiedNotification.OnImportsSatisfied
		/// </summary>
		/// <param name="notifyPropertyChanging">INotifyPropertyChanging</param>
		/// <param name="updateAction">Action to call on active and update, the argument is the property name</param>
		/// <param name="pattern">Optional Regex pattern to match the property name in the event against, null matches everything</param>
		/// <returns>an IDisposable, calling Dispose on this will stop everything</returns>
		public static IDisposable OnPropertyChanging(this INotifyPropertyChanging notifyPropertyChanging, Action<string> updateAction, string pattern = null)
		{
			if (notifyPropertyChanging == null)
			{
				throw new ArgumentNullException(nameof(notifyPropertyChanging));
			}
			if (updateAction == null)
			{
				throw new ArgumentNullException(nameof(updateAction));
			}
			var notifyAction = WrapNotifyProperty(updateAction, pattern, nameof(OnPropertyChanging));
			return EventObservable.From(notifyPropertyChanging).OnEach(pce => notifyAction(pce.Args.PropertyName));
		}

		/// <summary>
		/// Create a wrapping action, which handles the pattern check and logsexceptions
		/// </summary>
		/// <param name="notifyAction">Action which was passed by the caller</param>
		/// <param name="pattern">Pattern which was called by the caller</param>
		/// <param name="source">string with OnPropertyChanged or OnPropertyChanging</param>
		/// <returns>Action which accepts a string</returns>
		private static Action<string> WrapNotifyProperty(Action<string> notifyAction, string pattern, string source)
		{
			return (propertyName) =>
			{
				try
				{
					if (!string.IsNullOrEmpty(propertyName) && propertyName != "*" && !string.IsNullOrEmpty(pattern) && !Regex.IsMatch(propertyName, pattern))
					{
						return;
					}
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex);
				}
				try
				{
					notifyAction(propertyName);
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "An error occured while calling updateAction from {0}", source);
				}
			};
		}
	}
}