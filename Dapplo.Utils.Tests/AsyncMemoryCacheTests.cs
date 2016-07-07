using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Utils.Tests.Cache;

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
			var cache = new AsyncHttpCache();
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
			var cache = new AsyncAwaitExceptionCache();
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
			var cache = new AsyncHttpCache();
			var bitmapUri = new Uri("http://httpbin.org/image/png");
			var bitmapSource = await cache.GetOrCreateAsync(bitmapUri);

			Assert.NotNull(bitmapSource);
			Assert.NotNull(await cache.GetAsync(bitmapUri));
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
