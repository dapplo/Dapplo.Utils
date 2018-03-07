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
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Collections;
using Dapplo.Utils.Notify;
using Dapplo.Utils.Tests.TestEntities;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	/// Tests for the  ObservableConcurrentDictionary, currently a lot are still missing
	/// </summary>
	public class ObservableConcurrentDictionaryTests
	{
		private static readonly LogSource Log = new LogSource();
		public ObservableConcurrentDictionaryTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public void TestAddOrUpdate_TryRemove()
		{
			var npc = new NotifyPropertyChangedImpl();

			const string testKey = "One";

			var ocd = new ObservableConcurrentDictionary<string, NotifyPropertyChangedImpl>();
			ocd.AddOrUpdate(testKey, npc, (s, impl) => throw new InvalidOperationException());

			// Register without doing, just to make sure we also test the event generation for now
			ocd.CollectionChanged += (sender, args) =>
			{
				Log.Info().WriteLine($"Old location: {args.OldStartingIndex}");
			};

			var isNpcTriggered = false;
			// Subscribe to the PropertyChanged
			using (ocd.OnPropertyChanged().Subscribe(args => isNpcTriggered = true))
			{
				// change a value, a NPC is triggered
				npc.Name = "Robin1";
				// Test trigger
				Assert.True(isNpcTriggered);

				// Reset the trigger test value
				isNpcTriggered = false;

				// Remove the item, this should remove the event registration
				Assert.True(ocd.TryRemove(testKey, out _));

				// change a value, a NPC is triggered
				npc.Name = "Robin2";
				// Make sure we DID NOT get the trigger
				Assert.False(isNpcTriggered);
			}
		}
	}
}