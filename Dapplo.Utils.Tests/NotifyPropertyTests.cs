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
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Extensions;
using Dapplo.Utils.Tests.TestEntities;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	public class NotifyPropertyTests
	{
		public NotifyPropertyTests(ITestOutputHelper testOutputHelper)
		{
			// Make sure logging is enabled.
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public async Task Observable_EnumerableTest()
		{
			var npc = new NotifyPropertyChangedImpl();
			
			var task = npc.ToObservable().FirstAsync(e => e.PropertyName.Contains("2")).ToTask();
			npc.Name = "Dapplo";
			await Task.Delay(100);
			Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
			npc.Name2 = "Dapplo";
			await Task.Delay(100);
			Assert.True(task.IsCompleted);
		}

		[Fact]
		public void Observable_ForEach()
		{
			string testValue = null;
			var npc = new NotifyPropertyChangedImpl();
			var handler = npc.ToObservable().Subscribe(e => testValue = e.PropertyName);
			npc.Name = "Dapplo";
			Assert.Equal(nameof(npc.Name), testValue);
			testValue = null;
			// Test after Unsubscribe
			handler.Dispose();
			npc.Name = "Dapplo2";
			Assert.Null(testValue);
		}

		[Fact]
		public async Task Observable_ToTask_ExceptionTest()
		{
			var npc = new NotifyPropertyChangedImpl();

			var task = npc.ToObservable().FirstAsync(e => e.PropertyName.Contains("2")).Select<PropertyChangedEventArgs, bool>(e => { throw new Exception("blub"); }).FirstAsync().ToTask();
			npc.Name = "Dapplo";
			await Task.Delay(100);
			Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
			npc.Name2 = "Dapplo";
			await Task.Delay(100);
			Assert.True(task.IsFaulted);
		}

		[Fact]
		public void NotifyPropertyExtensions_OnPropertyChanged()
		{
			var npc = new NotifyPropertyChangedImpl();
			string changedPropertyName = null;
			using (npc.OnPropertyChanged(propertyName => changedPropertyName = propertyName))
			{
				npc.Name = "Dapplo";
				Assert.Equal(nameof(npc.Name), changedPropertyName);
			}
			// Test if all registrations are gone
			Assert.Equal(0, npc.RemoveEventHandlers());
		}

		[Fact]
		public void NotifyPropertyExtensions_OnPropertyChanging()
		{
			var npc = new NotifyPropertyChangingImpl();
			string changingPropertyName = null;
			using (npc.OnPropertyChanging(propertyName => changingPropertyName = propertyName))
			{
				npc.Name = "Dapplo";
				Assert.Equal(nameof(npc.Name), changingPropertyName);
			}
			// Test if all registrations are gone
			Assert.Equal(0, npc.RemoveEventHandlers());
		}
	}
}