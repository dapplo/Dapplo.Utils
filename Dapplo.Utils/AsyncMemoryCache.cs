using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Utils
{
	/// <summary>
	/// Factory for the AsyncMemoryCached
	/// </summary>
	public static class AsyncMemoryCache
	{
		/// <summary>
		/// Create a typed async memory cache
		/// </summary>
		/// <param name="name">Name of the memory cache</param>
		/// <param name="createFunc">The function which creates items</param>
		/// <typeparam name="TKey">Type for the key</typeparam>
		/// <typeparam name="TResult">Type for the result</typeparam>
		/// <returns>A typed async memory cache</returns>
		public static AsyncMemoryCache<TKey, TResult> Create<TKey, TResult>(string name, Func<TKey, CancellationToken, Task<TResult>> createFunc) where TResult : class
		{
			return new AsyncMemoryCache<TKey, TResult>(name, createFunc);
		}
	}

	/// <summary>
	/// This is a memory cache which is filled via a function.
	/// </summary>
	/// <typeparam name="TKey">Type for the key</typeparam>
	/// <typeparam name="TResult">Type for the result</typeparam>
	public class AsyncMemoryCache<TKey, TResult> where TResult : class
	{
		private readonly Func<TKey, CancellationToken, Task<TResult>> _createFunc;
		private readonly MemoryCache _cache;
		private readonly AsyncLock _asyncLock = new AsyncLock();

		/// <summary>
		/// Set the timespan for items to expire.
		/// </summary>
		public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.FromMinutes(15);

		/// <summary>
		/// creates a key under which the object is stored or retrieved
		/// </summary>
		public Func<TKey, string> CreateKey { get; set; } = keyObject => keyObject.ToString();

		/// <summary>
		/// Create the Async Cache
		/// </summary>
		/// <param name="name">Name of the cache</param>
		/// <param name="createFunc">async function which takes a key and returns a result</param>
		public AsyncMemoryCache(string name, Func<TKey, CancellationToken, Task<TResult>> createFunc)
		{
			_createFunc = createFunc;
			_cache = new MemoryCache(name);
		}

		/// <summary>
		/// Get an element from the cache, if this is not available call the create function.
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
		/// Get a task element from the cache, if this is not available return null.
		/// </summary>
		/// <param name="keyObject">object for the key</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task with TResult</returns>
		public Task<TResult> GetAsync(TKey keyObject, CancellationToken cancellationToken = default(CancellationToken))
		{
			var key = CreateKey(keyObject);
			return _cache.Get(key) as Task<TResult> ?? Task.FromResult<TResult>(null);
		}

		/// <summary>
		/// Get a task element from the cache, if this is not available call the create function.
		/// </summary>
		/// <param name="keyObject">object for the key</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task with TResult</returns>
		public Task<TResult> GetOrCreateAsync(TKey keyObject, CancellationToken cancellationToken = default(CancellationToken))
		{
			var key = CreateKey(keyObject);
			return _cache.Get(key) as Task<TResult> ?? GetOrCreateInternalAsync(keyObject, cancellationToken);
		}

		/// <summary>
		/// This takes care of the real async part of the code.
		/// </summary>
		/// <param name="keyObject"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		private async Task<TResult> GetOrCreateInternalAsync(TKey keyObject, CancellationToken cancellationToken = default(CancellationToken))
		{
			Task<TResult> result = null;
			var key = CreateKey(keyObject);
			using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
			{
				result = _cache.Get(key) as Task<TResult>;
				if (result == null)
				{
					result = _createFunc(keyObject, cancellationToken);
					var cacheItem = new CacheItem(key, result);
					var cacheItemPolicy = new CacheItemPolicy
					{
						AbsoluteExpiration = DateTimeOffset.Now.Add(ExpireTimeSpan)
					};
					_cache.Add(cacheItem, cacheItemPolicy);
				}
			}
			return await result.ConfigureAwait(false);
		}
	}
}
