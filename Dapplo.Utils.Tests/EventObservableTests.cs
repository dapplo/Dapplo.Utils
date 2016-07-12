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
using System.Threading.Tasks;
using System.Timers;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Enumerable;
using Dapplo.Utils.Events;
using Dapplo.Utils.Extensions;
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
		public event EventHandler<SimpleTestEventArgs> TestStringEvent;
		private readonly IList<IEventObservable> _enumerableEvents = new List<IEventObservable>();

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer and wait for the event, this should not block
		/// </summary>
		[Fact]
		public async Task EnumerableEvent_Timer_Test()
		{
			var timer = new Timer(100);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				await eventObservable.Subscribe().Take(1).ToTask();
			}
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void EventObservable_Enumerable_Test()
		{
			var eventObservable = EventObservable.From<SimpleTestEventArgs>(this, nameof(TestStringEvent));

			// Create IEnumerable, this also registers for events (otherwise they will be missed)
			var events = eventObservable.Subscribe();

			// Create the event
			eventObservable.Trigger(new EventData<SimpleTestEventArgs>(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Test if the event was processed correctly
			Assert.Equal("Dapplo", events.Flatten().Select(e => e.TestValue).First());
			// All event handlers should have unsubscribed
			Assert.Null(TestStringEvent);
		}

		[Fact]
		public void EventObservable_NotifyPropertyChanged_RemoveEventHandlers()
		{
			var npc = new NotifyPropertyChangedClass();
			npc.PropertyChanged += (sender, args) => { };
			npc.PropertyChanged += (sender, args) => { };
			Assert.Equal(2, npc.RemoveEventHandlers());
		}

		[Fact]
		public void EventObservable_NotifyPropertyChanged_OnEach()
		{
			string testValue = null;
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From(npc))
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
		public async Task EventObservable_NotifyPropertyChanged_ToTask_Test()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From(npc))
			{
				// Register a do which throws an exception
				var task = eventObservable.Subscribe().Flatten().Where(e => e.PropertyName.Contains("2")).Select(e => e.PropertyName).ToTask(x => { return x.First(); });
				npc.Name = "Dapplo";
				await Task.Delay(1000);
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				await Task.Delay(1000);
				Assert.True(task.IsCompleted);
				Assert.Equal("Name2", await task);
			}
		}

		[Fact]
		public async Task EventObservable_NotifyPropertyChanged_ToTask_ExceptionTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From<PropertyChangedEventArgs>(npc, nameof(npc.PropertyChanged)))
			{
				// Register ProcessAsync which throws an exception if there is a "2" in the PropertyName
				var task = eventObservable.Subscribe().Flatten().
					Where(e => e.PropertyName.Contains("2")).
					Select<PropertyChangedEventArgs, bool>(e => { throw new Exception("blub"); }).ToTask(x=> x.First());
				npc.Name = "Dapplo";
				await Task.Delay(1000);
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				await Task.Delay(1000);
				Assert.True(task.IsFaulted);
			}
		}

		[Fact]
		public async Task EventObservable_NotifyPropertyChanged_EnumerableTest()
		{
			var npc = new NotifyPropertyChangedClass();
			using (var eventObservable = EventObservable.From(npc))
			{
				var task = eventObservable.Subscribe().Flatten().Where(e => e.PropertyName.Contains("2")).Take(1).ToTask(x => x.Count());
				npc.Name = "Dapplo";
				await Task.Delay(1000);
				Assert.False(task.IsCanceled || task.IsCompleted || task.IsFaulted);
				npc.Name2 = "Dapplo";
				await Task.Delay(1000);
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
			// All event handlers should have unsubscribed
			Assert.Null(TestStringEvent);
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via a reference
		/// </summary>
		[Fact]
		public async Task EventObservable_ToTaskTest()
		{
			string testValue = null;
			string testValue2 = null;
			EventHandler<SimpleTestEventArgs> action = (sender, eventArgs) => testValue2 = eventArgs.TestValue;
			TestStringEvent += action;

			var eventObservable = EventObservable.From(ref TestStringEvent, nameof(TestStringEvent));
			// For later disposing
			_enumerableEvents.Add(eventObservable);

			var eventHandleTask = eventObservable.Subscribe().Flatten().ToTask(e => testValue = e.First().TestValue);

			eventObservable.Trigger(EventData.Create(this, new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Dispose makes sure no events are handled via the EventObservable, it also makes the ForEach stop!
			eventObservable.Dispose();

			// The following event should only be handled by the normal event handler
			TestStringEvent(this, new SimpleTestEventArgs {TestValue = "Robin"});
			await eventHandleTask;
			// Make sure both values are what they are supposed to!
			Assert.Equal("Dapplo", testValue);
			Assert.Equal("Robin", testValue2);
			TestStringEvent -= action;

			// All event handlers should have unsubscribed
			Assert.Null(TestStringEvent);
		}

		/// <summary>
		///     Test a global registration
		/// </summary>
		[Fact]
		public void EventObservable_RegisterEvents()
		{
			var testValue = new SimpleTestEventArgs {TestValue = "Robin"};
			EventArgs receivedValue = null;
			IDisposable subscription = null;
			foreach (var eventObservable in EventObservable.EventsIn<EventArgs>(this))
			{
				subscription = eventObservable.OnEach(x => receivedValue = x.Args);
				break;
			}
			// Test for subscriptions.
			Assert.NotNull(TestStringEvent);

			// Create an event
			TestStringEvent(this, testValue);
			// Make sure it arrived
			Assert.Equal(testValue, receivedValue);

			// Remove OnEach
			subscription?.Dispose();

			// All event handlers should have unsubscribed
			Assert.Null(TestStringEvent);
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
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await eventObservable.Subscribe().Flatten().ToTask(x => x.First()).WithTimeout(TimeSpan.FromSeconds(1)));
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
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await eventObservable.Subscribe().Flatten().ToTask(x => Log.Verbose().WriteLine("Elapsed at {0}", x.SignalTime)).WithTimeout(TimeSpan.FromSeconds(1)));
				Log.Info().WriteLine(ex.Message);
			}
		}

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer and wait for the event, while having a timeout bigger than the tick.
		///     This should not generate an exception
		/// </summary>
		[Fact]
		public async Task EventObservable_Timer_OkTest()
		{
			var timer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();

				// Await with a timeout bigger than the timer tick
				await eventObservable.Subscribe().Take(1).ToTask().WithTimeout(TimeSpan.FromSeconds(3));
			}
		}
	}
}