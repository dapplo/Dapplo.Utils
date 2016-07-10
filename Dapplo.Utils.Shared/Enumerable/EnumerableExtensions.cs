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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Utils.Enumerable
{
	/// <summary>
	///     Simple IEnumerable extensions.
	///     These are in a different package so it doesn't hinder other IEnumerable extensions.
	/// </summary>
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Convert a linq query to an task which can be awaited.
		/// </summary>
		/// <param name="source">IEnumerable of type TResult</param>
		/// <param name="cancellationToken">optional CancellationToken</param>
		/// <typeparam name="TResult">The type for the result</typeparam>
		/// <returns>Task with an IEnumerable of type TResult</returns>
		public static Task<IEnumerable<TResult>> ToTask<TResult>(this IEnumerable<TResult> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<IEnumerable<TResult>>();

			Task.Run(() =>
			{
				try
				{
					// Evaluate the IEnumerable, so it can be returned
					taskCompletionSource.TrySetResult(source.ToList());
				}
				catch (Exception ex)
				{
					taskCompletionSource.TrySetException(ex);
				}
			}, cancellationToken);
			return taskCompletionSource.Task;
		}

		/// <summary>
		/// Convert a linq query to an task which can be awaited.
		/// The function will process the source IEnumeration and returns a result (which could be an enumeration itself).
		/// </summary>
		/// <param name="source">IEnumerable of type TSource</param>
		/// <param name="resultFunc">Func to process the IEnumerable and returns a TResult</param>
		/// <param name="cancellationToken">optional CancellationToken</param>
		/// <typeparam name="TResult">The type for the result</typeparam>
		/// <typeparam name="TSource">The type of the IEnumerable</typeparam>
		/// <returns>Task with TResult</returns>
		public static Task<TResult> ToTask<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, TResult> resultFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<TResult>();

			Task.Run(() =>
			{
				try
				{
					taskCompletionSource.TrySetResult(resultFunc(source));
				}
				catch (Exception ex)
				{
					taskCompletionSource.TrySetException(ex);
				}
			}, cancellationToken);
			return taskCompletionSource.Task;
		}

		/// <summary>
		///     As the BCL doesn't include a ForEach on the IEnumerable, this extension was added to help a few border cases
		/// </summary>
		/// <typeparam name="T">Type of the IEnumerable</typeparam>
		/// <param name="source">The IEnumerable</param>
		/// <param name="action">Action to call for each item</param>
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			foreach (var item in source)
			{
				action(item);
			}
		}

		/// <summary>
		///     As the BCL doesn't include a ForEach on the IEnumerable, this extension was added to help a few border cases
		/// </summary>
		/// <typeparam name="T">Type of the IEnumerable</typeparam>
		/// <param name="source">The IEnumerable</param>
		/// <param name="action">Action to call for each item</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task to await for</returns>
		public static async Task ForEachAsync<T>(this IEnumerable<T> source, Action<T> action, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			// Process inside the task
			await Task.Run(() =>
			{
				foreach (var item in source)
				{
					action(item);
				}
			}, cancellationToken);
		}
	}
}