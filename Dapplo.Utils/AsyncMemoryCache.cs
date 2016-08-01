﻿#region Dapplo 2016 - GNU Lesser General Public License

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
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Utils
{
	/// <summary>
	///     This abstract class builds a base for a simple async memory cache.
	/// </summary>
	/// <typeparam name="TKey">Type for the key</typeparam>
	/// <typeparam name="TResult">Type for the stored value</typeparam>
	public abstract class AsyncMemoryCache<TKey, TResult> where TResult : class
	{
		private static readonly Task<TResult> EmptyValueTask = Task.FromResult<TResult>(null);
		private readonly AsyncLock _asyncLock = new AsyncLock();
		private readonly MemoryCache _cache = new MemoryCache(Guid.NewGuid().ToString());
		private readonly LogSource _log = new LogSource();

		/// <summary>
		///     Set the timespan for items to expire.
		/// </summary>
		public TimeSpan? ExpireTimeSpan { get; set; }

		/// <summary>
		///     Set the timespan for items to slide.
		/// </summary>
		public TimeSpan? SlidingTimeSpan { get; set; }

		/// <summary>
		///     Specifies if the RemovedCallback needs to be called
		///     If this is active, ActivateUpdateCallback should be false
		/// </summary>
		protected bool ActivateRemovedCallback { get; set; } = true;

		/// <summary>
		///     Specifies if the UpdateCallback needs to be called.
		///     If this is active, ActivateRemovedCallback should be false
		/// </summary>
		protected bool ActivateUpdateCallback { get; set; } = false;

		/// <summary>
		/// Implement this method, it should create an instance of TResult via the supplied TKey.
		/// </summary>
		/// <param name="key">TKey</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>TResult</returns>
		protected abstract Task<TResult> CreateAsync(TKey key, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		///     Creates a key under which the object is stored or retrieved, default is a toString on the object.
		/// </summary>
		/// <param name="keyObject">TKey</param>
		/// <returns>string</returns>
		protected virtual string CreateKey(TKey keyObject)
		{
			return keyObject.ToString();
		}

		/// <summary>
		///     Get an element from the cache, if this is not available call the create function.
		/// </summary>
		/// <param name="keyObject">object for the key</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>TResult</returns>
		public async Task DeleteAsync(TKey keyObject, CancellationToken cancellationToken = default(CancellationToken))
		{
			var key = CreateKey(keyObject);
			using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
			{
				_cache.Remove(key);
			}
		}

		/// <summary>
		///     Get a task element from the cache, if this is not available return null.
		///     You probably want to call GetOrCreateAsync
		/// </summary>
		/// <param name="keyObject">object for the key</param>
		/// <returns>Task with TResult, null if no value</returns>
		public Task<TResult> GetAsync(TKey keyObject)
		{
			var key = CreateKey(keyObject);
			return _cache.Get(key) as Task<TResult> ?? EmptyValueTask;
		}

		/// <summary>
		///     Get a task element from the cache, if this is not available call the create function.
		/// </summary>
		/// <param name="keyObject">object for the key</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task with TResult</returns>
		public Task<TResult> GetOrCreateAsync(TKey keyObject, CancellationToken cancellationToken = default(CancellationToken))
		{
			var key = CreateKey(keyObject);
			return _cache.Get(key) as Task<TResult> ?? GetOrCreateInternalAsync(keyObject, null, cancellationToken);
		}

		/// <summary>
		///     Get a task element from the cache, if this is not available call the create function.
		/// </summary>
		/// <param name="keyObject">object for the key</param>
		/// <param name="cacheItemPolicy">CacheItemPolicy for when you want more control over the item</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task with TResult</returns>
		public Task<TResult> GetOrCreateAsync(TKey keyObject, CacheItemPolicy cacheItemPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			var key = CreateKey(keyObject);
			return _cache.Get(key) as Task<TResult> ?? GetOrCreateInternalAsync(keyObject, cacheItemPolicy, cancellationToken);
		}

		/// <summary>
		///     This takes care of the real async part of the code.
		/// </summary>
		/// <param name="keyObject"></param>
		/// <param name="cacheItemPolicy">CacheItemPolicy for when you want more control over the item</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>TResult</returns>
		private async Task<TResult> GetOrCreateInternalAsync(TKey keyObject, CacheItemPolicy cacheItemPolicy = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var key = CreateKey(keyObject);
			var completionSource = new TaskCompletionSource<TResult>();

			if (cacheItemPolicy == null)
			{
				cacheItemPolicy = new CacheItemPolicy
				{
					AbsoluteExpiration = ExpireTimeSpan.HasValue ? DateTimeOffset.Now.Add(ExpireTimeSpan.Value) : ObjectCache.InfiniteAbsoluteExpiration,
					SlidingExpiration = SlidingTimeSpan ?? ObjectCache.NoSlidingExpiration
				};
				if (ActivateUpdateCallback)
				{
					cacheItemPolicy.UpdateCallback = UpdateCallback;
				}
				if (ActivateRemovedCallback)
				{
					cacheItemPolicy.RemovedCallback = RemovedCallback;
				}
			}

			var result = _cache.AddOrGetExisting(key, completionSource.Task, cacheItemPolicy) as Task<TResult>;
			// Test if we got an existing object or our own
			if (result != null && !completionSource.Task.Equals(result))
			{
				return await result.ConfigureAwait(false);
			}

			using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
			{
				result = _cache.AddOrGetExisting(key, completionSource.Task, cacheItemPolicy) as Task<TResult>;
				if (result != null && !completionSource.Task.Equals(result))
				{
					return await result.ConfigureAwait(false);
				}

				// Now, start the background task, which will set the completionSource with the correct response
				// ReSharper disable once MethodSupportsCancellation
				// ReSharper disable once UnusedVariable
				var ignoreBackgroundTask = Task.Run(async () =>
				{
					try
					{
						var backgroundResult = await CreateAsync(keyObject, cancellationToken).ConfigureAwait(false);
						completionSource.TrySetResult(backgroundResult);
					}
					catch (TaskCanceledException)
					{
						completionSource.TrySetCanceled();
					}
					catch (Exception ex)
					{
						completionSource.TrySetException(ex);
					}
				});
			}
			return await completionSource.Task.ConfigureAwait(false);
		}

		/// <summary>
		///     Override to know when an item is removed, make sure to configure ActivateUpdateCallback / ActivateRemovedCallback
		/// </summary>
		/// <param name="cacheEntryRemovedArguments">CacheEntryRemovedArguments</param>
		protected virtual void RemovedCallback(CacheEntryRemovedArguments cacheEntryRemovedArguments)
		{
			_log.Verbose().WriteLine("Item {0} removed due to {1}.", cacheEntryRemovedArguments.CacheItem.Key, cacheEntryRemovedArguments.RemovedReason);
		}

		/// <summary>
		///     Override to modify the cache behaviour when an item is about to be removed, make sure to configure
		///     ActivateUpdateCallback / ActivateRemovedCallback
		/// </summary>
		/// <param name="cacheEntryUpdateArguments">CacheEntryUpdateArguments</param>
		protected virtual void UpdateCallback(CacheEntryUpdateArguments cacheEntryUpdateArguments)
		{
			_log.Verbose().WriteLine("Update request for {0} due to {1}.", cacheEntryUpdateArguments.Key, cacheEntryUpdateArguments.RemovedReason);
		}
	}
}