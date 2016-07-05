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
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Xunit;
using Dapplo.Utils.Events;
using Dapplo.Utils.Tests.TestEntities;
using Xunit.Abstractions;
using System.Timers;

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
	public class SmartEventTests : IDisposable
	{
		private event EventHandler<SimpleTestEvent> TestStringEvent;
		private readonly IList<ISmartEvent> _smartEvents = new List<ISmartEvent>();
		public void Dispose()
		{
			// Dispose all SmartEvents that were created by passing the _smartEvents list
			SmartEvent.DisposeAll(_smartEvents);
		}

		public SmartEventTests(ITestOutputHelper testOutputHelper)
		{
			// Make sure logging is enabled.
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}


		/// <summary>
		/// Test the basic functionality for setting up an event handler via a reference
		/// </summary>
		[Fact]
		public void SmartEvent_EventHandler_SimpleTest()
		{
			string testValue = null;
			string testValue2 = null;
			TestStringEvent += (sender, eventArgs) => testValue2 = eventArgs.TestValue;

			var smartEvent = SmartEvent.FromEventHandler(ref TestStringEvent, nameof(TestStringEvent), _smartEvents).Every.Do((sender, e) => testValue = e.TestValue).Start();
			smartEvent.Trigger(this, new SimpleTestEvent { TestValue = "Dapplo" });
			// Dispose makes sure no events are handled via the smart event
			smartEvent.Dispose();
			// The following event should only be handled by the normal event handler
			TestStringEvent(this, new SimpleTestEvent {TestValue = "Robin"});
			// Make sure both values are what they are supposed to!
			Assert.Equal("Dapplo", testValue);
			Assert.Equal("Robin", testValue2);
		}

		/// <summary>
		/// Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void SmartEvent_Reflection_SimpleTest()
		{
			string testValue = null;
			var smartEvent = SmartEvent.FromReflection<SimpleTestEvent>(this, nameof(TestStringEvent), _smartEvents).Every.Do((sender, e) => testValue = e.TestValue).Start();
			smartEvent.Trigger(this, new SimpleTestEvent { TestValue = "Dapplo" });
			Assert.Equal("Dapplo", testValue);
		}

		/// <summary>
		/// Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void SmartEvent_Reflection_First_Test()
		{
			string testValue = null;
			using (var smartEvent = SmartEvent.FromReflection<SimpleTestEvent>(this, nameof(TestStringEvent)))
			{
				smartEvent.First.Do((sender, e) => testValue = e.TestValue).Start();
				smartEvent.Trigger(this, new SimpleTestEvent { TestValue = "Dapplo" });
			}
			Assert.Equal("Dapplo", testValue);
		}

		[Fact]
		public void SmartEvent_Reflection_NotifyPropertyChanged_Test()
		{
			string testValue = null;
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.FromReflection<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				smartEvent.First.Do((o, args) => testValue = args.PropertyName).Start();
				npc.Name = "Dapplo";
				Assert.Equal(nameof(npc.Name), testValue);
				testValue = null;
				npc.Name = "Dapplo2";
				Assert.Null(testValue);
			}
		}

		[Fact]
		public void SmartEvent_Reflection_NotifyPropertyChanged_AwaitTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.FromReflection<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				var task = smartEvent.First.When((o, args) => args.PropertyName.Contains("2")).WaitForAsync();
				npc.Name = "Dapplo";
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				Assert.True(task.IsCompleted);
			}
		}

		[Fact]
		public void SmartEvent_Reflection_NotifyPropertyChanged_AwaitExceptionTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.FromReflection<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				// Register a do which throws an exception
				var task = smartEvent.First.When((o, args) => args.PropertyName.Contains("2")).Do(
					(o, args) =>
					{
						throw new Exception("blub");
					}).WaitForAsync();
				npc.Name = "Dapplo";
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				Assert.True(task.IsFaulted);
			}
		}

		[Fact]
		public async Task SmartEvent_Reflection_NotifyPropertyChanged_AwaitValueTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.FromReflection<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				// Register a do which throws an exception
				var task = smartEvent.First.When((o, args) => args.PropertyName.Contains("2")).WaitForAsync((o, args) => "Robin");
				npc.Name = "Dapplo";
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				Assert.True(task.IsCompleted);
				Assert.Equal("Robin", await task);
			}
		}

		/// <summary>
		/// Create a timer, register a smartevent to it.
		/// Start the timer and wait for the event, this should not block
		/// </summary>
		[Fact]
		public async Task SmartEven_Reflectiont_Timer_Test()
		{
			var timer = new Timer(1000);

			using (var smartEvent = SmartEvent.FromReflection<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				await smartEvent.First.WaitForAsync();
			}
		}

		/// <summary>
		/// Create a timer, register a smartevent to it.
		/// Start the timer and wait for the event, while having a timeout bigger than the tick.
		/// This should not generate an exception
		/// </summary>
		[Fact]
		public async Task SmartEvent_Reflection_TimerOk_Test()
		{
			var timer = new Timer(1000);

			using (var smartEvent = SmartEvent.FromReflection<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				// Await with a timeout bigger than the timer tick
				await smartEvent.First.WaitForAsync(TimeSpan.FromSeconds(2));
			}
		}

		/// <summary>
		/// Create a timer, register a smartevent to it.
		/// Start the timer and wait for the event, while having a timeout smaller than the tick.
		/// This should create a timeout exception
		/// </summary>
		[Fact]
		public async Task SmartEvent_Reflection_TimerTimeout_Test()
		{
			var timer = new Timer(2000);

			using (var smartEvent = SmartEvent.FromReflection<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				// Await with a timeout smaller than the timer tick
				await Assert.ThrowsAsync<TimeoutException>(async () => await smartEvent.First.WaitForAsync(TimeSpan.FromSeconds(1)));
			}
		}
	}
}
