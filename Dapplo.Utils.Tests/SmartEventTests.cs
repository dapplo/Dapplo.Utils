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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Dapplo.HttpExtensions;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Events;
using Dapplo.Utils.Tests.TestEntities;
using Xunit;
using Xunit.Abstractions;
using Timer = System.Timers.Timer;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	///     Test ISmartEvent
	/// </summary>
	public class SmartEventTests : IDisposable
	{
		public SmartEventTests(ITestOutputHelper testOutputHelper)
		{
			// Make sure logging is enabled.
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		public void Dispose()
		{
			// Dispose all ISmartEvent that were created by passing the _enumerableEvents list
			SmartEvent.DisposeAll(_enumerableEvents);
		}

		private static readonly LogSource Log = new LogSource();
		private event EventHandler<SimpleTestEventArgs> TestStringEvent;
		private readonly IList<ISmartEvent> _enumerableEvents = new List<ISmartEvent>();

		/// <summary>
		///     Create a timer, register a SmartEvent to it.
		///     Start the timer and wait for the event, this should not block
		/// </summary>
		[Fact]
		public async Task EnumerableEvent_Reflectiont_Timer_Test()
		{
			var timer = new Timer(100);

			using (var smartEvent = SmartEvent.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				await smartEvent.ProcessAsync(e => e.First());
			}
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void SmartEvent_Enumerable_Test()
		{
			var smartEvent = SmartEvent.From<SimpleTestEventArgs>(this, nameof(TestStringEvent));

			// Create IEnumerable, this also registers for events
			var events = smartEvent.From;

			// Create the event
			smartEvent.Trigger(new EventData<SimpleTestEventArgs>(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Test if the event was processed correctly
			Assert.Equal("Dapplo", events.Flatten().Select(e => e.TestValue).First());
		}

		[Fact]
		public void SmartEvent_NotifyPropertyChanged_OnEach()
		{
			string testValue = null;
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				var handler = smartEvent.OnEach(e => testValue = e.Args.PropertyName);
				npc.Name = "Dapplo";
				Assert.Equal(nameof(npc.Name), testValue);
				testValue = null;
				// Test after Unsubscribe
				handler.Dispose();
				npc.Name = "Dapplo2";
				Assert.Null(testValue);
			}
		}

		[Fact]
		public async Task SmartEvent_NotifyPropertyChanged_Process_AwaitTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				// Register a do which throws an exception
				var task = smartEvent.ProcessAsync(eventArgs => eventArgs.Flatten().Where(e => e.PropertyName.Contains("2")).Select(e => e.PropertyName).First());
				npc.Name = "Dapplo";
				Thread.Sleep(100);
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				Thread.Sleep(100);
				Assert.True(task.IsCompleted);
				Assert.Equal("Name2", await task);
			}
		}

		[Fact]
		public void SmartEvent_NotifyPropertyChanged_Process_ExceptionTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				// Register ProcessAsync which throws an exception if there is a "2" in the PropertyName
				var task = smartEvent.ProcessAsync(eventArgs => eventArgs.Flatten().
					Where(e => e.PropertyName.Contains("2")).
					Select<PropertyChangedEventArgs, bool>(e => { throw new Exception("blub"); }).First());
				npc.Name = "Dapplo";
				Thread.Sleep(100);
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				Thread.Sleep(100);
				Assert.True(task.IsFaulted);
			}
		}

		[Fact]
		public void SmartEvent_NotifyPropertyChanged_ProcessTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var smartEvent = SmartEvent.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				var task = smartEvent.ProcessAsync(eventArgs => eventArgs.Flatten().Where(e => e.PropertyName.Contains("2")).Take(1).Count());
				npc.Name = "Dapplo";
				Thread.Sleep(100);
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				Thread.Sleep(100);
				Assert.True(task.IsCompleted);
			}
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void SmartEvent_OnEach_Test()
		{
			string testValue = null;
			using (var smartEvent = SmartEvent.From<SimpleTestEventArgs>(this, nameof(TestStringEvent)))
			{
				smartEvent.OnEach(e => { testValue = e.Args.TestValue; });
				smartEvent.Trigger(EventData.Create(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));
			}
			Assert.Equal("Dapplo", testValue);
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via a reference
		/// </summary>
		[Fact]
		public async Task SmartEvent_ProcessTest()
		{
			string testValue = null;
			string testValue2 = null;
			TestStringEvent += (sender, eventArgs) => testValue2 = eventArgs.TestValue;

			var smartEvent = SmartEvent.From(ref TestStringEvent, nameof(TestStringEvent));
			// For later disposing
			_enumerableEvents.Add(smartEvent);

			var eventHandleTask = smartEvent.ProcessAsync(enumerable => enumerable.ForEach(e => testValue = e.Args.TestValue));

			smartEvent.Trigger(EventData.Create(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Dispose makes sure no events are handled via the smart event, it also makes the ForEach stop!
			smartEvent.Dispose();

			// The following event should only be handled by the normal event handler
			TestStringEvent(this, new SimpleTestEventArgs {TestValue = "Robin"});
			await eventHandleTask;
			// Make sure both values are what they are supposed to!
			Assert.Equal("Dapplo", testValue);
			Assert.Equal("Robin", testValue2);
		}

		/// <summary>
		///     Test a global registration
		/// </summary>
		[Fact]
		public void SmartEvent_RegisterEvents()
		{
			var testValue = new SimpleTestEventArgs {TestValue = "Robin"};
			EventArgs resultValue = null;
			SmartEvent.RegisterEvents<EventArgs>(this, (e) => resultValue = e.Args);
			TestStringEvent(this, testValue);
			Assert.Equal(testValue, resultValue);
		}

		/// <summary>
		///     Create a timer, register a SmartEvent to it.
		///     Start the timer and wait for the event, while having a timeout smaller than the tick.
		///     This should create a timeout exception
		/// </summary>
		[Fact]
		public async Task SmartEvent_Timer_TimeoutFunctionTest()
		{
			var timer = new Timer(200);

			using (var smartEvent = SmartEvent.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();

				// Await with a timeout smaller than the timer tick
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await smartEvent.ProcessAsync(x => x.First(), TimeSpan.FromMilliseconds(100)));
				Log.Info().WriteLine(ex.Message);
			}
		}

		[Fact]
		public async Task SmartEvent_Timer_TimeoutActionTest()
		{
			var timer = new Timer(200);

			using (var smartEvent = SmartEvent.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();

				// Await with a timeout smaller than the timer tick
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await smartEvent.ProcessAsync(x => Log.Verbose().WriteLine("Elapsed at {0}", x.First().Args.SignalTime), TimeSpan.FromMilliseconds(100)));
				Log.Info().WriteLine(ex.Message);
			}
		}

		/// <summary>
		///     Create a timer, register a SmartEvent to it.
		///     Start the timer and wait for the event, while having a timeout bigger than the tick.
		///     This should not generate an exception
		/// </summary>
		[Fact]
		public async Task SmartEvent_TimerOk_Test()
		{
			var timer = new Timer(100);

			using (var smartEvent = SmartEvent.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				// Await with a timeout bigger than the timer tick
				await smartEvent.ProcessAsync(x => Log.Verbose().WriteLine("Elapsed at {0}", x.First().Args.SignalTime), TimeSpan.FromMilliseconds(200));
			}
		}
	}
}