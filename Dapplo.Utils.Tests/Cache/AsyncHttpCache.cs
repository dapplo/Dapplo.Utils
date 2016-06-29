using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.HttpExtensions;

namespace Dapplo.Utils.Tests.Cache
{
	/// <summary>
	/// Test AsyncMemoryCache which retrieves a bitmap from the supplied Uri
	/// </summary>
	public class AsyncHttpCache : AsyncMemoryCache<Uri, BitmapSource>
	{
		protected override async Task<BitmapSource> CreateAsync(Uri key, CancellationToken cancellationToken = new CancellationToken())
		{
			return await key.GetAsAsync<BitmapSource>(cancellationToken);
		}
	}
}
