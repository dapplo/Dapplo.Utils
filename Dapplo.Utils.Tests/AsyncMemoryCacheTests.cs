using System;
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
			Func<Uri, CancellationToken, Task<BitmapSource>> cacheFunc = async (key, token) => await key.GetAsAsync<BitmapSource>(token);
			var cache = AsyncMemoryCached.Create("Bitmap", cacheFunc);
			var bitmapUri = new Uri("http://httpbin.org/image/png");
			var bitmapSource = await cache.GetOrCreateAsync(bitmapUri);
			Assert.NotNull(bitmapSource);
			Assert.True(bitmapSource.Width > 0);
			Assert.True(bitmapSource.Height > 0);
		}
	}
}
