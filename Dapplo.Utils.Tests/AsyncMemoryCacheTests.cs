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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Tests.Cache;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	public class AsyncMemoryCacheTests
	{
		public AsyncMemoryCacheTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public async Task TestMemoryCache()
		{
			var cache = new AsyncBitmapCache();
			var bitmapFile = "dapplo.png";
			var tasks = new List<Task<BitmapSource>>();
			for (var i = 0; i < 10; i++)
			{
				tasks.Add(cache.GetOrCreateAsync(bitmapFile));
			}
			await Task.WhenAll(tasks);
			for (var i = 0; i < 10; i++)
			{
				var bitmapSource = await tasks[i];
				Assert.NotNull(bitmapSource);
				Assert.True(bitmapSource.Width > 0);
				Assert.True(bitmapSource.Height > 0);
			}
		}

		[Fact]
		public async Task TestMemoryCacheDelete()
		{
			var cache = new AsyncBitmapCache();
			var bitmapFile = "dapplo.png";
			var bitmapSource = await cache.GetOrCreateAsync(bitmapFile);

			Assert.NotNull(bitmapSource);
			Assert.NotNull(await cache.GetAsync(bitmapFile));
			Assert.True(bitmapSource.Width > 0);
			Assert.True(bitmapSource.Height > 0);

			await cache.DeleteAsync(bitmapFile);
			Assert.Null(await cache.GetAsync(bitmapFile));

			bitmapSource = await cache.GetOrCreateAsync(bitmapFile);
			Assert.NotNull(bitmapSource);
			Assert.True(bitmapSource.Width > 0);
			Assert.True(bitmapSource.Height > 0);
		}

		[Fact]
		public async Task TestMemoryCacheException()
		{
			var cache = new AsyncAwaitExceptionCache();
			var bitmapUri = new Uri("http://httpbin.org/image/png");
			var tasks = new List<Task<BitmapSource>>();
			for (var i = 0; i < 10; i++)
			{
				tasks.Add(cache.GetOrCreateAsync(bitmapUri));
			}
			for (var i = 0; i < 10; i++)
			{
				var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => tasks[i]);
				Assert.Contains(bitmapUri.AbsoluteUri, ex.Message);
			}
		}
	}
}