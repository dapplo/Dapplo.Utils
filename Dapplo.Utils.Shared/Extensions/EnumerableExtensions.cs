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
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Utils.Extensions
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
		public static async Task<IList<TResult>> ToListAsync<TResult>(this IEnumerable<TResult> source, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Do not pass the CancellationToken, as this would case an OperationCanceledException
			// ReSharper disable once MethodSupportsCancellation
			return await Task.Run(() =>
			{
				var results = new List<TResult>();
				if (cancellationToken.IsCancellationRequested)
				{
					return results;
				}
				foreach (var item in source)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
					results.Add(item);
				}
				return results;
			}).ConfigureAwait(false);
		}

		/// <summary>
		///     An async version of the ForEach extension, with an optional predicate
		/// </summary>
		/// <typeparam name="T">Type of the IEnumerable</typeparam>
		/// <param name="source">The IEnumerable</param>
		/// <param name="action">Action to call for each item</param>
		/// <param name="predicate">Predicate func</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task to await for</returns>
		public static async Task ForEachAsync<T>(this IEnumerable<T> source, Action<T> action, Func<T, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
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
			// Do not pass the CancellationToken, as this would case an OperationCanceledException
			// ReSharper disable once MethodSupportsCancellation
			await Task.Run(() =>
			{
				source.ForEach(action, predicate, cancellationToken);
			}).ConfigureAwait(false);
		}

		/// <summary>
		///     A simple foreach, with an optional predicate
		/// </summary>
		/// <typeparam name="T">Type of the IEnumerable</typeparam>
		/// <param name="source">The IEnumerable</param>
		/// <param name="action">Action to call for each item</param>
		/// <param name="predicate">Predicate func</param>
		/// <param name="cancellationToken">CancellationToken, which breaks the enumeration</param>
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action, Func<T, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			foreach (var item in source)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				if (predicate != null && !predicate(item))
				{
					continue;
				}
				action(item);
			}
		}


		/// <summary>
		/// Returns if there is any (matching) value in the source
		/// </summary>
		/// <typeparam name="T">Type for the IEnumerable</typeparam>
		/// <param name="source">IEnumerable of T</param>
		/// <param name="predicate">Func which takes a T and returns a bool, can be null</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>true if there was a value, false if not</returns>
		public static async Task<bool> AnyAsync<T>(this IEnumerable<T> source, Func<T, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			// Do not pass the CancellationToken, as this would case an OperationCanceledException
			// ReSharper disable once MethodSupportsCancellation
			return await Task.Run(() =>
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return false;
				}
				foreach (var item in source)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						return false;
					}
					if (predicate != null && !predicate(item))
					{
						continue;
					}
					return true;
				}
				return false;
			}).ConfigureAwait(false);
		}

		/// <summary>
		/// Returns the first, blocks until there is something, throws an InvalidOperationException when cancelled or when there are no items left
		/// </summary>
		/// <typeparam name="T">Type for the IEnumerable</typeparam>
		/// <param name="source">IEnumerable of T</param>
		/// <param name="predicate">Func which takes a T and returns a bool</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>first T in the IEnumerable</returns>
		public static async Task<T> FirstAsync<T>(this IEnumerable<T> source, Func<T, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			return await Task.Run(() =>
			{
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var item in source)
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (predicate != null && !predicate(item))
					{
						continue;
					}
					return item;
				}
				throw new InvalidOperationException("No elements found.");
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Returns the count of the items in the source
		/// </summary>
		/// <param name="source">IEnumerable of T</param>
		/// <param name="predicate">Func which takes a T and returns a bool</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>first T in the IEnumerable</returns>
		public static async Task<int> CountAsync<T>(this IEnumerable<T> source, Func<T, bool> predicate = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			// Do not pass the CancellationToken, as this would case an OperationCanceledException
			// ReSharper disable once MethodSupportsCancellation
			return await Task.Run(() =>
			{
				int count = 0;
				if (!cancellationToken.IsCancellationRequested)
				{
					foreach (var item in source)
					{
						if (cancellationToken.IsCancellationRequested)
						{
							break;
						}
						if (predicate != null && !predicate(item))
						{
							continue;
						}
						count++;
					}
				}
				return count;
			}).ConfigureAwait(false);
		}

		/// <summary>
		/// Skip the last n elements of an IEnumerable
		/// </summary>
		/// <typeparam name="T">Type for the IEnumerable</typeparam>
		/// <param name="source"></param>
		/// <param name="skipN">the number of elements to skip, default is 1 and should be positive (0 skips nothing)</param>
		/// <returns>IEnumerable</returns>
		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int skipN = 1)
		{
			// Check arguments
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (skipN < 0)
			{
				throw new ArgumentException("Cannot be less than 0", nameof(skipN));
			}
			// Do not add logic when skipN == 0, just return the source
			if (skipN == 0)
			{
				return source;
			}
			// No do the real skip last
			return InternalSkipLast(source, skipN);
		}

		/// <summary>
		/// Internal method to skip the last n elements of an IEnumerable
		/// </summary>
		/// <typeparam name="T">Type for the IEnumerable</typeparam>
		/// <param name="source"></param>
		/// <param name="skipN">the number of elements to skip, default is 1 and should be positive (0 skips nothing)</param>
		/// <returns>IEnumerable</returns>
		private static IEnumerable<T> InternalSkipLast<T>(this IEnumerable<T> source, int skipN = 1)
		{
			using (var enumerator = source.GetEnumerator())
			{
				bool hasRemainingItems;
				var cache = new Queue<T>(skipN + 1);

				do
				{
					hasRemainingItems = enumerator.MoveNext();
					if (!hasRemainingItems)
					{
						continue;
					}
					cache.Enqueue(enumerator.Current);
					if (cache.Count > skipN)
					{
						yield return cache.Dequeue();
					}
				} while (hasRemainingItems);
			}
		}
	}
}