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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

#endregion

namespace Dapplo.Utils.Tests.Cache
{
	/// <summary>
	///     Test AsyncMemoryCache which retrieves a bitmap from the file system via the supplied filename
	/// </summary>
	public class AsyncBitmapCache : AsyncMemoryCache<string, BitmapSource>
	{
		/// <inheritdoc />
		protected override async Task<BitmapSource> CreateAsync(string filename, CancellationToken cancellationToken = new CancellationToken())
		{
			using (var stream = new FileStream(Path.Combine("TestFiles", filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				// ReSharper disable once AccessToDisposedClosure
				return await Task.Run(() => stream.BitmapImageFromStream(), cancellationToken).ConfigureAwait(false);
			}
		}
	}
}