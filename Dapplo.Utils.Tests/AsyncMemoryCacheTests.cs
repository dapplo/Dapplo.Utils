using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.HttpExtensions;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Utils.Tests
{
	public class AsyncMemoryCacheTests
	{
		private static readonly LogSource Log = new LogSource();

		public AsyncMemoryCacheTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}
		
		[Fact]
		public async Task TestMemoryCache()
		{
			Func<Uri, CancellationToken, Task<BitmapSource>> testFunc = async (key, token) => await key.GetAsAsync<BitmapSource>(token);
			var cache = AsyncMemoryCache.Create("Bitmap", testFunc);
			var bitmapUri = new Uri("http://httpbin.org/image/png");
			var tasks = new List<Task<BitmapSource>>();
			for (int i = 0; i < 10; i++)
			{
				tasks.Add(cache.GetOrCreateAsync(bitmapUri));
			}
			await Task.WhenAll(tasks);
			for (int i = 0; i < 10; i++)
			{
				var bitmapSource = await tasks[i];
				Assert.NotNull(bitmapSource);
				Assert.True(bitmapSource.Width > 0);
				Assert.True(bitmapSource.Height > 0);
			}
		}

		[Fact]
		public async Task TestMemoryCacheException()
		{
			Func<Uri, CancellationToken, Task<BitmapSource>> testFunc = async (key, token) =>
			{
				await Task.Delay(100);
				throw new ArgumentNullException(nameof(key), key.AbsoluteUri);
			};
			var cache = AsyncMemoryCache.Create("Bitmap", testFunc);
			var bitmapUri = new Uri("http://httpbin.org/image/png");
			var tasks = new List<Task<BitmapSource>>();
			for (int i = 0; i < 10; i++)
			{
				tasks.Add(cache.GetOrCreateAsync(bitmapUri));
			}
			for (int i = 0; i < 10; i++)
			{
				var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => tasks[i]);
				Assert.Contains(bitmapUri.AbsoluteUri, ex.Message);
			}
		}
		[Fact]
		public async Task TestMemoryCacheDelete()
		{
			Func<Uri, CancellationToken, Task<BitmapSource>> testFunc = async (key, token) => await key.GetAsAsync<BitmapSource>(token);
			var cache = AsyncMemoryCache.Create("Bitmap", testFunc);
			var bitmapUri = new Uri("http://httpbin.org/image/png");
			var bitmapSource = await cache.GetOrCreateAsync(bitmapUri);
			Assert.NotNull(bitmapSource);
			Assert.True(bitmapSource.Width > 0);
			Assert.True(bitmapSource.Height > 0);

			await cache.DeleteAsync(bitmapUri);
			Assert.Null(await cache.GetAsync(bitmapUri));

			bitmapSource = await cache.GetOrCreateAsync(bitmapUri);
			Assert.NotNull(bitmapSource);
			Assert.True(bitmapSource.Width > 0);
			Assert.True(bitmapSource.Height > 0);

		}
	}
}
