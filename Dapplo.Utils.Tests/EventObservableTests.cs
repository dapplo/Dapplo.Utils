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
using System.Linq;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Extensions;
using Dapplo.Utils.Tests.TestEntities;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	///     Test IEventObservable
	/// </summary>
	public class EventObservableTests
	{
		public EventObservableTests(ITestOutputHelper testOutputHelper)
		{
			// Make sure logging is enabled.
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}


		[Fact]
		public void Observable_RemoveEventHandlers()
		{
			var npc = new NotifyPropertyChangedImpl();
			npc.PropertyChanged += (sender, args) => { };
			npc.PropertyChanged += (sender, args) => { };
			Assert.Equal(2, npc.RemoveEventHandlers());
		}

		[Fact]
		public void Observable_EventsIn()
		{
			var npc = new NotifyPropertyChangedImpl();
			Assert.True(npc.EventsIn<EventArgs>().Count(e => e.Is<PropertyChangedEventArgs>()) == 1);
		}
	}
}