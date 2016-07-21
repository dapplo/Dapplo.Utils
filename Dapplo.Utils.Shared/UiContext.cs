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
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Utils
{
	/// <summary>
	/// This helps to make sure Tasks can have access to the UI 
	/// </summary>
	public static class UiContext
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// The TaskScheduler for the UI
		/// </summary>
		public static TaskScheduler UiTaskScheduler
		{
			get;
			internal set;
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
				Log.Warn().WriteLine("No current SynchronizationContent, creating one.");
				SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			}

			UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		}

		/// <summary>
		/// Checks if there is UI access possible
		/// </summary>
		public static bool HasUiAccess => HasScheduler && TaskScheduler.Current == UiTaskScheduler;

		/// <summary>
		/// Checks if there is UI scheduler
		/// </summary>
		public static bool HasScheduler => UiTaskScheduler != null;

		/// <summary>
		///     Run your action on the UI, if needed.
		///     Initialize() should be called once, otherwise the taskpool is used.
		/// </summary>
		/// <param name="function">Action to run</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task of TResult</returns>
		public static Task<TResult> RunOn<TResult>(Func<TResult> function, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (HasScheduler && !HasUiAccess)
			{
				return Task.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, UiTaskScheduler);
			}
			return Task.Run(function, cancellationToken);
		}

		/// <summary>
		///     Run your action on the UI, if needed.
		///     Initialize() should be called once, otherwise the taskpool is used.
		/// </summary>
		/// <param name="function">Function to run</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task of TResult</returns>
		public static Task<TResult> RunOn<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (HasScheduler && !HasUiAccess)
			{
				return Task.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, UiTaskScheduler).Unwrap();
			}
			return Task.Run(function, cancellationToken);
		}

		/// <summary>
		///     Run your action on the UI, if needed.
		///     Initialize() should be called once, otherwise the taskpool is used.
		/// </summary>
		/// <param name="action">Action to run</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public static Task RunOn(Action action, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (HasScheduler && !HasUiAccess)
			{
				return Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.DenyChildAttach, UiTaskScheduler);
			}
			return Task.Run(action, cancellationToken);
		}
	}
}