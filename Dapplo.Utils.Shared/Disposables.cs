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
using System.Collections.Concurrent;
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Utils
{
	/// <summary>
	///     This can store IDisposable items, and dispose all of them when dispose is called.
	///     The internal storage is thread-safe, and you can continue to use it (call Add) after calling Dispose!
	/// </summary>
	public class Disposables : IDisposable
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IProducerConsumerCollection<IDisposable> _disposables;

		/// <summary>
		///     Create a Disposables
		/// </summary>
		/// <param name="disposable">optional IDisposable</param>
		/// <param name="reverseDisposal">
		///     Specify true (default) if the disposal needs to be done in reverse order (last in, first out)
		/// </param>
		public Disposables(IDisposable disposable = null, bool reverseDisposal = true)
		{
			if (reverseDisposal)
			{
				_disposables = new ConcurrentStack<IDisposable>();
			}
			else
			{
				_disposables = new ConcurrentQueue<IDisposable>();
			}
			Add(disposable);
		}

		/// <summary>
		///     Specifies if exceptions from calling Dispose need to be logged
		/// </summary>
		public bool LogExceptions { get; set; } = true;

		/// <summary>
		///     Specifies if exceptions from calling Dispose on an added IDisposable should be thrown.
		/// </summary>
		public bool IgnoreExceptions { get; set; } = true;

		/// <summary>
		///     Dispose will dispose all the the stored IDisposables
		/// </summary>
		public void Dispose()
		{
			IDisposable disposable;
			while (_disposables.TryTake(out disposable))
			{
				try
				{
					disposable?.Dispose();
				}
				catch (Exception ex)
				{
					if (LogExceptions)
					{
						Log.Error().WriteLine(ex, "Error while disposing");
					}
					if (!IgnoreExceptions)
					{
						throw;
					}
				}
			}
		}

		/// <summary>
		///     Factory method
		/// </summary>
		/// <param name="disposable">optional IDisposable</param>
		/// <param name="reverseDisposal">
		///     Specify true (default) if the disposal needs to be done in reverse order (last in, first
		///     out)
		/// </param>
		/// <returns>Disposables</returns>
		public static Disposables Create(IDisposable disposable = null, bool reverseDisposal = true)
		{
			return new Disposables(disposable, reverseDisposal);
		}

		/// <summary>
		///     Add a Disposable to dispose of
		/// </summary>
		/// <param name="disposable">IDisposable</param>
		/// <returns>this</returns>
		public Disposables Add(IDisposable disposable)
		{
			_disposables.TryAdd(disposable);
			return this;
		}

		/// <summary>
		///     Simplify the adding of a disposable to the Disposables, just add it.
		/// </summary>
		/// <param name="disposables">Disposables</param>
		/// <param name="disposable">IDisposable</param>
		/// <returns></returns>
		public static Disposables operator +(Disposables disposables, IDisposable disposable)
		{
			disposables.Add(disposable);
			return disposables;
		}
	}
}