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
		// To detect redundant calls
		private bool _disposedValue;

		/// <summary>
		///     usage
		///     using(var lock = await asyncLock.LockAsync()) {
		///     }
		/// </summary>
		/// <returns>disposable</returns>
		public async Task<IDisposable> LockAsync()
		{
			await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
			return Disposable.Create(() => { _semaphoreSlim.Release(); });
		}

		/// <summary>
		///     usage
		///     using(var lock = await asyncLock.LockAsync()) {
		///     }
		/// </summary>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>disposable</returns>
		public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
		{
			await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
			return Disposable.Create(() => { _semaphoreSlim.Release(); });
		}

		/// <summary>
		///     usage
		///     using(var lock = await asyncLock.LockAsync(TimeSpan.FromMilliSeconds(100))) {
		///     }
		/// </summary>
		/// <param name="timeout">TimeSpan</param>
		/// <returns>disposable</returns>
		public async Task<IDisposable> LockAsync(TimeSpan timeout)
		{
			var cancellationTokenSource = new CancellationTokenSource(timeout);
			await _semaphoreSlim.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
			return Disposable.Create(() =>
			{
				cancellationTokenSource.Dispose();
				_semaphoreSlim.Release();
			});
		}

		#region IDisposable Support

		/// <summary>
		///     Dispose the current async lock, and it's underlying SemaphoreSlim
		/// </summary>
		private void DisposeInternal()
		{
			if (!_disposedValue)
			{
				_semaphoreSlim.Dispose();

				_disposedValue = true;
			}
		}

		/// <summary>
		///     Finalizer, as it would be bad to leave a SemaphoreSlim hanging around
		/// </summary>
		~AsyncLock()
		{
			DisposeInternal();
		}

		/// <summary>
		///     Implementation of the IDisposable
		/// </summary>
		public void Dispose()
		{
			DisposeInternal();
			// Make sure the finalizer for this instance is not called, as we already did what we need to do
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}