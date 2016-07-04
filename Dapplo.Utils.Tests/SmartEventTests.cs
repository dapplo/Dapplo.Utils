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

using System;
using Xunit;
using Dapplo.Utils.Events;

namespace Dapplo.Utils.Tests
{
	/// <summary>
	/// Arguments for the test event
	/// </summary>
	public class SimpleTestEvent : EventArgs
	{
		public string TestValue { get; set; }
	}

	/// <summary>
	/// Test SmartEvent
	/// </summary>
	public class SmartEventTests
	{
		private event EventHandler<SimpleTestEvent> TestStringEvent;

		/// <summary>
		/// Test the basic functionality for setting up an event handler via a reference
		/// </summary>
		[Fact]
		public void SmartEvent_EventHandlerTest()
		{
			string testValue = null;
			using (var smartEvent = SmartEvent.FromEvent(ref TestStringEvent))
			{
				smartEvent.On((sender, e) => testValue = e.TestValue).Start();
				smartEvent.Trigger(this, new SimpleTestEvent { TestValue = "Dapplo" });
			}
			Assert.Equal("Dapplo", testValue);
		}

		/// <summary>
		/// Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void SmartEvent_ReflectionTest()
		{
			string testValue = null;
			using (var smartEvent = SmartEvent.FromReflection<SimpleTestEvent>(this, nameof(TestStringEvent)))
			{
				smartEvent.On((sender, e) => testValue = e.TestValue).Start();
				smartEvent.Trigger(this, new SimpleTestEvent { TestValue = "Dapplo" });
			}
			Assert.Equal("Dapplo", testValue);
		}
	}
}
