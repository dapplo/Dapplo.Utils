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

using Dapplo.Log.Facade;
using Xunit;
using Xunit.Abstractions;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Dapplo.Log.XUnit;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	/// Test the UiContext
	/// </summary>
	public class UiContextTests : IDisposable
	{
		private static readonly LogSource Log = new LogSource();

		public UiContextTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		public void Dispose()
		{
			Dispatcher.CurrentDispatcher.InvokeShutdown();
		}

		/// <summary>
		/// Test UiContext.RunOn()
		/// </summary>
		[WpfFact]
		public async Task TestRunOn()
		{
			var taskSchedulerId = TaskScheduler.Current.Id;

			Log.Info().WriteLine("Current id: {0}", taskSchedulerId);
			UiContext.Initialize();

			var window = new Window
			{
				Width = 200,
				Height = 200
			};
			window.Show();

			// Should not throw anything
			window.Focus();

			// Make sure the current task scheduler is not for the UI thread
			await Task.Delay(10).ConfigureAwait(false);

			// Should throw
			Assert.Throws<InvalidOperationException>(() => window.Focus());

			// This should also not throw anything
			await UiContext.RunOn(() =>
			{
				var taskSchedulerIdInside = TaskScheduler.Current.Id;
				Log.Info().WriteLine("Current id inside: {0}", taskSchedulerIdInside);
				Assert.NotEqual(taskSchedulerId, taskSchedulerIdInside);
				window.Close();
			});

		}
	}
}