using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Dapplo.Utils.Tests.Cache
{
	public class AsyncAwaitExceptionCache : AsyncMemoryCache<Uri, BitmapSource>
	{
		protected override async Task<BitmapSource> CreateAsync(Uri key, CancellationToken cancellationToken = new CancellationToken())
		{
			await Task.Delay(100, cancellationToken);
			throw new ArgumentNullException(nameof(key), key.AbsoluteUri);
		}
	}
}
