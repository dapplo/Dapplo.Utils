//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2015-2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Utils
// 
//  Dapplo.Utils is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Utils is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Utils. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Utils
{
	/// <summary>
	///     A simple class to make it possible to lock a resource while waiting
	/// </summary>
	public class AsyncLock : IDisposable
	{
		private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

		/// <summary>
		///     usage
		///     using(var lock = await asyncLock.LockAsync()) {
		///     }
		/// </summary>
		/// <returns>disposable</returns>
		public async Task<IDisposable> LockAsync()
		{
			await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
			return new Releaser(_semaphoreSlim);
		}

		/// <summary>
		/// Internal structure used to make it possible to dispose
		/// </summary>
		private struct Releaser : IDisposable
		{
			private readonly SemaphoreSlim _semaphoreSlim;
			private bool _isReleased;

			public Releaser(SemaphoreSlim semaphoreSlim)
			{
				_isReleased = false;
				_semaphoreSlim = semaphoreSlim;
			}

			public void Dispose()
			{
				if (!_isReleased)
				{
					_semaphoreSlim.Release();
					_isReleased = true;
				}
			}
		}

		#region IDisposable Support

		// To detect redundant calls
		private bool _disposedValue;

		/// <summary>
		/// Dispose the current async lock, and it's underlying SemaphoreSlim
		/// </summary>
		/// <param name="disposing">bool which is currently ignored as we have no managed objects</param>
		private void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				_semaphoreSlim.Dispose();

				_disposedValue = true;
			}
		}

		/// <summary>
		/// Finalizer, as it would be bad to leave a SemaphoreSlim
		/// </summary>
		~AsyncLock()
		{
			Dispose(false);
		}

		/// <summary>
		/// Implementation of the IDisposable
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			// Make sure the finalizer for this instance is not called
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}