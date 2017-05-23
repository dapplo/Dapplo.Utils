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
using System.Reflection;
using System.Text.RegularExpressions;

#endregion

namespace Dapplo.Utils.Extensions
{
	/// <summary>
	///     Extensions for IHaveEvents and IEventObservable
	/// </summary>
	public static class EventExtensions
	{
		private const BindingFlags AllBindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>
		///     Removes all the event handlers on a IHaveEvents
		///     This is usefull to do internally, after a clone is made, to prevent memory leaks
		/// </summary>
		/// <param name="haveEvents">IHaveEvents instance</param>
		/// <param name="regExPattern">Regular expression to match the even names, null for alls</param>
		/// <returns>number of removed event handlers</returns>
		public static int RemoveEventHandlers(this IHaveEvents haveEvents, string regExPattern = null)
		{
			if (haveEvents == null)
			{
				throw new ArgumentNullException(nameof(haveEvents));
			}
			return RemoveEventHandlersFromObject(haveEvents, regExPattern);
		}

		/// <summary>
		///     Removes all the event handlers from the defined events in an object
		///     This is usefull to do internally, after a MemberwiseClone is made, to prevent memory leaks
		/// </summary>
		/// <param name="instance">object instance where events need to be removed</param>
		/// <param name="regExPattern">Regular expression to match the even names, null for alls</param>
		/// <returns>number of removed events</returns>
		public static int RemoveEventHandlersFromObject(object instance, string regExPattern = null)
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
	}
}