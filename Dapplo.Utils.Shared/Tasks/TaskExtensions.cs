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
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Utils.Tasks
{
	/// <summary>
	///     A few simple task helpers, these could be simplified.
	///     The code was based, but slightly modified, upon the blog entry <a href="http://blogs.msdn.com/b/pfxteam/archive/2011/11/10/10235834.aspx">here</a>
	/// </summary>
	public static class TaskExtensions
	{
		/// <summary>
		///     Create a task which timeouts after the specified timeout
		/// </summary>
		/// <typeparam name="TResult">Type for the result</typeparam>
		/// <param name="task">Task</param>
		/// <param name="timeout">optional TimeSpan</param>
		/// <returns>Task with result</returns>
		public static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan? timeout = null)
		{
			return WithTimeoutInternal<TResult>(task, timeout) as Task<TResult>;
		}

		/// <summary>
		///     Create a task which timeouts after the specified timeout
		/// </summary>
		/// <param name="task">Task</param>
		/// <param name="timeout">optional TimeSpan</param>
		/// <returns>Task</returns>
		public static Task WithTimeout(this Task task, TimeSpan? timeout = null)
		{
			return WithTimeoutInternal<VoidTypeStruct>(task, timeout);
		}

		/// <summary>
		///     Internally used, to solve the generic issue
		/// </summary>
		/// <typeparam name="TResult">Type for the result</typeparam>
		/// <param name="task"></param>
		/// <param name="timeout"></param>
		/// <returns>Task</returns>
		private static Task WithTimeoutInternal<TResult>(Task task, TimeSpan? timeout = null)
		{
			// Short-circuit #1: no/negative timeout or task already completed
			if (task.IsCompleted || !timeout.HasValue || Timeout.InfiniteTimeSpan.Equals(timeout))
			{
				// Either the task has already completed or timeout will never occur.
				// No proxy necessary.
				return task;
			}

			// tcs.Task will be returned as a proxy to the caller
			var taskCompletionSource = new TaskCompletionSource<TResult>();

			// Short-circuit #2: zero timeout
			if ((long) timeout.Value.TotalMilliseconds == 0)
			{
				// We've already timed out.
				taskCompletionSource.SetException(new TimeoutException($"The timeout of {timeout.Value} has expired."));
				return taskCompletionSource.Task;
			}

			// Set up a timer to complete after the specified timeout period
			var timer = new Timer(state =>
			{
				// Recover your state information
				var innerTaskCompletionSource = (TaskCompletionSource<TResult>) state;

				// Fault our proxy with a TimeoutException
				innerTaskCompletionSource.TrySetException(new TimeoutException($"The timeout of {timeout.Value} has expired."));
			}, taskCompletionSource, timeout.Value, Timeout.InfiniteTimeSpan);

			// Wire up the logic for what happens when source task completes
			task.ContinueWith((antecedent, state) =>
				{
					// Recover our state data
					var tuple = (Tuple<Timer, TaskCompletionSource<TResult>>) state;

					// Cancel the Timer
					tuple.Item1.Dispose();

					// Marshal results to proxy
					MarshalTaskResults(antecedent, tuple.Item2);
				},
				Tuple.Create(timer, taskCompletionSource), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			return taskCompletionSource.Task;
		}

		/// <summary>
		///     This passes the result of a task to a TaskCompletionSource
		/// </summary>
		/// <typeparam name="TResult">Result type for the TaskCompletionSource</typeparam>
		/// <param name="source">task</param>
		/// <param name="taskCompletionSource"></param>
		private static void MarshalTaskResults<TResult>(Task source, TaskCompletionSource<TResult> taskCompletionSource)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			switch (source.Status)
			{
				case TaskStatus.Faulted:
					if (source.Exception == null)
					{
						taskCompletionSource.TrySetException(new NullReferenceException("Task faulted without exception."));
					}
					else
					{
						taskCompletionSource.TrySetException(source.Exception);
					}
					break;
				case TaskStatus.Canceled:
					taskCompletionSource.TrySetCanceled();
					break;
				case TaskStatus.RanToCompletion:
					var castedSource = source as Task<TResult>;
					taskCompletionSource.TrySetResult(castedSource == null
						?
						// source is a Task
						default(TResult)
						:
						// source is a Task<TResult>
						castedSource.Result);
					break;
			}
		}

		// Simulate a Task<void> to satisfy the WithTimeoutInternal Type argument
		private struct VoidTypeStruct
		{
		}
	}
}