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
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	///     Test Disposable and Disposables
	/// </summary>
	public class DisposeTests
	{
		public DisposeTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public void DisposablesTest()
		{
			var testValue1 = false;
			var testValue2 = false;

			using (new Disposables(Disposable.Create(() => testValue1 = true)).Add(Disposable.Create(() => testValue2 = true)))
			{
				// Do nothing, just make the using dispose the Disposables
			}
			Assert.True(testValue1);
			Assert.True(testValue2);
		}

		[Fact]
		public void Disposables_Order_Test()
		{
			DateTimeOffset testValue1 = DateTimeOffset.MinValue;
			DateTimeOffset testValue2 = DateTimeOffset.MaxValue;

			Assert.False(testValue1 > testValue2);
			// Create 2 Disposable which set a DateTimeOffset.Now (and sleep, otherwise it's to quick) for testValue1 and testValue2
			var disposable1 = Disposable.Create(() =>
			{
				testValue1 = DateTimeOffset.Now;
				Thread.Sleep(10);
			});
			var disposable2 = Disposable.Create(() =>
			{
				testValue2 = DateTimeOffset.Now;
				Thread.Sleep(10);
			});

			// Create a Disposables for disposable1 and disposable2, dispose them in reverse order: first disposable2 and than disposable1
			using (new Disposables(disposable1).Add(disposable2))
			{
				// Do nothing, just make the using dispose the Disposables
			}
			// Now testValue1 should be greater (disposed last) as testValue2
			Assert.True(testValue1 > testValue2);

			// Again Create 2 Disposable which set a DateTimeOffset.Now (and sleep, otherwise it's to quick) for testValue1 and testValue2
			disposable1 = Disposable.Create(() =>
			{
				testValue1 = DateTimeOffset.Now;
				Thread.Sleep(10);
			});
			disposable2 = Disposable.Create(() =>
			{
				testValue2 = DateTimeOffset.Now;
				Thread.Sleep(10);
			});

			// Create a Disposables for disposable1 and disposable2, dispose them in added order: first disposable1 and than disposable2
			using (new Disposables(disposable1, false).Add(disposable2))
			{
				// Do nothing, just make the using dispose the Disposables
			}
			// Now testValue2 should be greater (disposed last) as testValue1
			Assert.True(testValue2 > testValue1);
		}

		[Fact]
		public void DisposableTest()
		{
			var testValue = false;
			using (Disposable.Create(() => testValue = true))
			{
				// Do nothing, just make the using dispose the Disposable
			}
			Assert.True(testValue);
		}
	}
}