﻿#region Dapplo 2016 - GNU Lesser General Public License

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
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Enumerable;
using Dapplo.Utils.Events;
using Dapplo.Utils.Tasks;
using Dapplo.Utils.Tests.TestEntities;
using Xunit;
using Xunit.Abstractions;
using Timer = System.Timers.Timer;

#endregion

namespace Dapplo.Utils.Tests
{
	/// <summary>
	///     Test IEventObservable
	/// </summary>
	public class EventObservableTests : IDisposable
	{
		public EventObservableTests(ITestOutputHelper testOutputHelper)
		{
			// Make sure logging is enabled.
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		public void Dispose()
		{
			// Dispose all IEventObservable that were created by passing the _enumerableEvents list
			EventObservable.DisposeAll(_enumerableEvents);
		}

		private static readonly LogSource Log = new LogSource();
		private event EventHandler<SimpleTestEventArgs> TestStringEvent;
		private readonly IList<IEventObservable> _enumerableEvents = new List<IEventObservable>();

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer and wait for the event, this should not block
		/// </summary>
		[Fact]
		public async Task EnumerableEvent_Reflectiont_Timer_Test()
		{
			var timer = new Timer(100);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				await eventObservable.ProcessAsync(e => e.First());
			}
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void EventObservable_Enumerable_Test()
		{
			var eventObservable = EventObservable.From<SimpleTestEventArgs>(this, nameof(TestStringEvent));

			// Create IEnumerable, this also registers for events
			var events = eventObservable.From;

			// Create the event
			eventObservable.Trigger(new EventData<SimpleTestEventArgs>(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Test if the event was processed correctly
			Assert.Equal("Dapplo", events.Flatten().Select(e => e.TestValue).First());
		}

		[Fact]
		public void EventObservable_NotifyPropertyChanged_OnEach()
		{
			string testValue = null;
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				var handler = eventObservable.OnEach(e => testValue = e.Args.PropertyName);
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
		public async Task EventObservable_NotifyPropertyChanged_Process_AwaitTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				// Register a do which throws an exception
				var task = eventObservable.ProcessAsync(eventArgs => eventArgs.Flatten().Where(e => e.PropertyName.Contains("2")).Select(e => e.PropertyName).First());
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
		public void EventObservable_NotifyPropertyChanged_Process_ExceptionTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				// Register ProcessAsync which throws an exception if there is a "2" in the PropertyName
				var task = eventObservable.ProcessAsync(eventArgs => eventArgs.Flatten().
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
		public void EventObservable_NotifyPropertyChanged_ProcessTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				var task = eventObservable.ProcessAsync(eventArgs => eventArgs.Flatten().Where(e => e.PropertyName.Contains("2")).Take(1).Count());
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
		public void EventObservable_OnEach_Test()
		{
			string testValue = null;
			using (var eventObservable = EventObservable.From<SimpleTestEventArgs>(this, nameof(TestStringEvent)))
			{
				eventObservable.OnEach(e => { testValue = e.Args.TestValue; });
				eventObservable.Trigger(EventData.Create(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));
			}
			Assert.Equal("Dapplo", testValue);
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via a reference
		/// </summary>
		[Fact]
		public async Task EventObservable_ProcessTest()
		{
			string testValue = null;
			string testValue2 = null;
			TestStringEvent += (sender, eventArgs) => testValue2 = eventArgs.TestValue;

			var eventObservable = EventObservable.From(ref TestStringEvent, nameof(TestStringEvent));
			// For later disposing
			_enumerableEvents.Add(eventObservable);

			var eventHandleTask = eventObservable.ProcessAsync(enumerable => enumerable.ForEach(e => testValue = e.Args.TestValue));

			eventObservable.Trigger(EventData.Create(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Dispose makes sure no events are handled via the EventObservable, it also makes the ForEach stop!
			eventObservable.Dispose();

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
		public void EventObservable_RegisterEvents()
		{
			var testValue = new SimpleTestEventArgs {TestValue = "Robin"};
			EventArgs resultValue = null;
			IList<IEventObservable> eventObservables = null;
			try
			{
				eventObservables = EventObservable.RegisterEvents<EventArgs>(this, (e) => resultValue = e.Args);
				Assert.NotNull(TestStringEvent);
				TestStringEvent(this, testValue);
				Assert.Equal(testValue, resultValue);
			}
			finally
			{
				EventObservable.DisposeAll(eventObservables);
			}
		}

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer and wait for the event, while having a timeout smaller than the tick.
		///     This should create a timeout exception
		/// </summary>
		[Fact]
		public async Task EventObservable_Timer_TimeoutFunctionTest()
		{
			var timer = new Timer(TimeSpan.FromSeconds(2).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();

				// Await with a timeout smaller than the timer tick
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await eventObservable.ProcessAsync(x => x.First()).WithTimeout(TimeSpan.FromSeconds(1)));
				Log.Info().WriteLine(ex.Message);
			}
		}

		[Fact]
		public async Task EventObservable_Timer_TimeoutActionTest()
		{
			var timer = new Timer(TimeSpan.FromSeconds(2).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();

				// Await with a timeout smaller than the timer tick
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await eventObservable.ProcessAsync(x => Log.Verbose().WriteLine("Elapsed at {0}", x.First().Args.SignalTime)).WithTimeout(TimeSpan.FromSeconds(1)));
				Log.Info().WriteLine(ex.Message);
			}
		}

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer and wait for the event, while having a timeout bigger than the tick.
		///     This should not generate an exception
		/// </summary>
		[Fact]
		public async Task EventObservable_TimerOk_Test()
		{
			var timer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				// Await with a timeout bigger than the timer tick
				await eventObservable.ProcessAsync(x => Log.Verbose().WriteLine("Elapsed at {0}", x.First().Args.SignalTime)).WithTimeout(TimeSpan.FromSeconds(2));
			}
		}
	}
}