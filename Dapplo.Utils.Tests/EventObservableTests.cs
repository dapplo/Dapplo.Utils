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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Events;
using Dapplo.Utils.Extensions;
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
	public class EventObservableTests : IDisposable, IHaveEvents
	{
		public EventObservableTests(ITestOutputHelper testOutputHelper)
		{
			// Make sure logging is enabled.
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		public void Dispose()
		{
			// Dispose all IEventObservable that were created by passing the _enumerableEvents list
			_enumerableEvents.DisposeAll();
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
				await eventObservable.Subscribe().Take(1).ToListAsync();
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
			Assert.True(eventObservable.Trigger(new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Test if the event was processed correctly
			Assert.Equal("Dapplo", events.Flatten().Select(e => e.TestValue).First());
			// All event handlers should have unsubscribed
			Assert.Null(TestStringEvent);
		}

		/// <summary>
		///     Test the basic functionality for setting up an event handler via reflection
		/// </summary>
		[Fact]
		public void EventObservable_ForEach_Test()
		{
			string testValue = null;
			using (var eventObservable = EventObservable.From<SimpleTestEventArgs>(this, nameof(TestStringEvent)))
			{
				eventObservable.ForEach(e => { testValue = e.Args.TestValue; });
				Assert.True(eventObservable.Trigger(new SimpleTestEventArgs {TestValue = "Dapplo"}));
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

			var eventObservable = EventObservable.From<SimpleTestEventArgs>(this, nameof(TestStringEvent));
			// For later disposing
			_enumerableEvents.Add(eventObservable);

			var eventHandleTask = eventObservable.Flatten().FirstAsync().ContinueWith(task =>
			{
				testValue = task.Result.TestValue;
			});

			Assert.True(eventObservable.Trigger(new SimpleTestEventArgs {TestValue = "Dapplo"}));

			// Dispose makes sure no events are handled via the EventObservable, it also makes the ForEach stop!
			eventObservable.Dispose();

			// The following event should only be handled by the initial event handler
			// ReSharper disable once PossibleNullReferenceException
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
				subscription = eventObservable.ForEach(x => receivedValue = x.Args);
				break;
			}
			// Test for subscriptions.
			Assert.NotNull(TestStringEvent);

			// Create an event
			TestStringEvent(this, testValue);
			// Make sure it arrived
			Assert.Equal(testValue, receivedValue);

			// Remove ForEach
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
			var timer = new Timer(TimeSpan.FromMilliseconds(400).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();

				// Await with a timeout smaller than the timer tick
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await eventObservable.FirstAsync().WithTimeout(TimeSpan.FromMilliseconds(300)));
				Log.Info().WriteLine(ex.Message);
			}
		}

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer and count the ticks.
		/// </summary>
		[Fact]
		public async Task EventObservable_CountAsync_Test()
		{
			// 1000 / 100 would be 10, but make it tick a bit less than 100 ms, as it would just not fit due to the "starup overhead"
			var timer = new Timer(TimeSpan.FromMilliseconds(80).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(300));
				timer.Start();
				Assert.Equal(3, await eventObservable.CountAsync(cancellationToken: cancellationTokenSource.Token));
			}
		}

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer and count the ticks.
		/// </summary>
		[Fact]
		public void EventObservable_ForEach_Cancel_Test()
		{
			// 1000 / 100 would be 10, but make it tick a bit less than 100 ms, as it would just not fit due to the "starup overhead"
			var timer = new Timer(TimeSpan.FromMilliseconds(85).TotalMilliseconds);

			int count = 0;
			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(400));
				timer.Start();
				eventObservable.ForEach(data => count++, cancellationToken: cancellationTokenSource.Token);
				// Sleep longer, to make sure it was the cancellationTokenSource which cancelled
				Thread.Sleep(500);
			}
			Assert.Equal(4, count);
		}


		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Start the timer, cancel after a certain time and count the ticks.
		/// </summary>
		[Fact]
		public async Task EventObservable_ForEachAsync_Cancel_Test()
		{
			// 1000 / 100 would be 10, but make it tick a bit less than 100 ms, as it would just not fit due to the "starup overhead"
			var timer = new Timer(TimeSpan.FromMilliseconds(85).TotalMilliseconds);

			int count = 0;
			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(400));
				timer.Start();
				await eventObservable.ForEachSync(data => count++, cancellationToken: cancellationTokenSource.Token);
			}
			Assert.Equal(4, count);
		}

		[Fact]
		public void EventObservable_OfType()
		{
			var eventObservables = EventObservable.EventsIn<EventArgs>(this);
			Assert.Equal(1, eventObservables.OfType<SimpleTestEventArgs>().Count());
		}

		[Fact]
		public void EventObservable_This_EventsIn_OfType()
		{
			var eventObservables = this.EventsIn<EventArgs>();
			Assert.Equal(1, eventObservables.OfType<SimpleTestEventArgs>().Count());
		}

		/// <summary>
		///     Create a timer, register a EventObservable to it.
		///     Expect that there is an event with a signaltime which is greater than now + 400
		/// </summary>
		[Fact]
		public async Task EventObservable_AnyAsync_Test()
		{
			var timer = new Timer(TimeSpan.FromMilliseconds(200).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				var waitUntil = DateTime.Now.AddMilliseconds(400);
				var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

				Assert.True(await eventObservable.AnyAsync(args => args.Args.SignalTime > waitUntil, cts.Token));
			}
		}

		[Fact]
		public async Task EventObservable_Timer_TimeoutActionTest()
		{
			var timer = new Timer(TimeSpan.FromMilliseconds(300).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();
				// Await with a timeout smaller than the timer tick
				var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await eventObservable.Flatten().ForEachAsync(x => Log.Verbose().WriteLine("Elapsed at {0}", x.SignalTime)).WithTimeout(TimeSpan.FromMilliseconds(200)));
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
			var timer = new Timer(TimeSpan.FromMilliseconds(300).TotalMilliseconds);

			using (var eventObservable = EventObservable.From<ElapsedEventArgs>(timer, nameof(timer.Elapsed)))
			{
				timer.Start();

				// Await with a timeout bigger than the timer tick
				await eventObservable.Subscribe().Take(1).ToListAsync().WithTimeout(TimeSpan.FromSeconds(400));
			}
		}
	}
}