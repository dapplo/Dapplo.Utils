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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.Log;

#endregion

namespace Dapplo.Utils.Tests.Cache
{
    /// <summary>
    ///     Test AsyncMemoryCache which retrieves a bitmap from the file system via the supplied filename
    /// </summary>
    public class AsyncBitmapCache : AsyncMemoryCache<string, BitmapSource>
    {
        private static readonly LogSource Log = new LogSource();
        /// <inheritdoc />
        protected override async Task<BitmapSource> CreateAsync(string key, CancellationToken cancellationToken = new CancellationToken())
        {
            string path = Path.Combine("TestFiles", key); 
            if (!File.Exists(path))
            {
                string location = Assembly.GetExecutingAssembly().Location;
                if (location != null)
                {
                    path = Path.Combine(Path.GetDirectoryName(location), "TestFiles", key);
                }
            }
            if (!File.Exists(path))
            {
                Log.Error().WriteLine("Couldn't find location {0}", path);
                // What is the default here?
                return null;
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // ReSharper disable once AccessToDisposedClosure
                return await Task.Run(() => stream.BitmapImageFromStream(), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}