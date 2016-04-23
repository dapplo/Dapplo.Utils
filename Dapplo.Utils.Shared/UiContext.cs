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

#region using

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Utils
{
	/// <summary>
	/// </summary>
	public static class UiContext
	{
		/// <summary>
		/// The TaskScheduler for the UI
		/// </summary>
		public static TaskScheduler UiTaskScheduler
		{
			get;
			private set;
		}

		/// <summary>
		///     Initialize to get the UI TaskScheduler
		/// </summary>
		public static void Initialize()
		{
			if (UiTaskScheduler != null)
			{
				return;
			}

			if (SynchronizationContext.Current == null)
			{
				SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			}

			UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		}

		/// <summary>
		///     Run your action on the UI, if needed
		/// </summary>
		/// <param name="action">Action to run</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public static Task RunOn(Action action, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (TaskScheduler.Current != UiTaskScheduler)
			{
				return Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.None, UiTaskScheduler);
			}
			return Task.Run(action, cancellationToken);
		}
	}
}