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
using System.Windows.Media.Imaging;
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Utils.Tests.Cache
{
	/// <summary>
	///     Simple helper extension
	/// </summary>
	public static class StreamExtensions
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     Creates a BitmapSource from the passed stream
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <returns>BitmapSource</returns>
		public static BitmapSource BitmapFromStream(this Stream stream)
		{
			Log.Debug().WriteLine("Creating a BitmapImage from the MemoryStream.");
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.StreamSource = stream;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
			// This is very important to make the bitmap usable in the UI thread:
			bitmap.Freeze();
			return bitmap;
		}
	}
}