using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using Xunit.Sdk;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.Core {
	public class MainLoopTests {

		[Fact]
		public void Constructor_Setups_Driver ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			Assert.NotNull (ml.Driver);
		}

		// Idle Handler tests
		[Fact]
		public void AddIdle_Adds_And_Removes ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			Func<bool> fnTrue = () => { return true; };
			Func<bool> fnFalse = () => { return false; };
			ml.AddIdle (fnTrue);
			ml.AddIdle (fnFalse);

			Assert.True (ml.RemoveIdle (fnTrue));

			// BUGBUG: This doesn't throw or indicate an error. Ideally RemoveIdle would either 
			// throw an exception in this case, or return an error.
			// No. Only need to return a boolean.
			Assert.False (ml.RemoveIdle (fnTrue));

			Assert.True (ml.RemoveIdle (fnFalse));

			// BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
			// throw an exception in this case, or return an error.
			// No. Only need to return a boolean.
			Assert.False (ml.RemoveIdle (fnFalse));

			// Add again, but with dupe
			ml.AddIdle (fnTrue);
			ml.AddIdle (fnTrue);

			Assert.True (ml.RemoveIdle (fnTrue));
			Assert.True (ml.RemoveIdle (fnTrue));

			// BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
			// throw an exception in this case, or return an error.
			// No. Only need to return a boolean.
			Assert.False (ml.RemoveIdle (fnTrue));
		}

		[Fact]
		public void AddIdle_Function_GetsCalled_OnIteration ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			ml.AddIdle (fn);
			ml.MainIteration ();
			Assert.Equal (1, functionCalled);
		}

		[Fact]
		public void RemoveIdle_Function_NotCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			Assert.False (ml.RemoveIdle (fn));
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
		}

		[Fact]
		public void AddThenRemoveIdle_Function_NotCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			ml.AddIdle (fn);
			Assert.True (ml.RemoveIdle (fn));
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
		}

		[Fact]
		public void AddTwice_Function_CalledTwice ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			ml.AddIdle (fn);
			ml.AddIdle (fn);
			ml.MainIteration ();
			Assert.Equal (2, functionCalled);

			functionCalled = 0;
			Assert.True (ml.RemoveIdle (fn));
			ml.MainIteration ();
			Assert.Equal (1, functionCalled);

			functionCalled = 0;
			Assert.True (ml.RemoveIdle (fn));
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
			Assert.False (ml.RemoveIdle (fn));
		}

		[Fact]
		public void False_Idle_Stops_It_Being_Called_Again ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn1 = () => {
				functionCalled++;
				if (functionCalled == 10) {
					return false;
				}
				return true;
			};

			// Force stop if 20 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				stopCount++;
				if (stopCount == 20) {
					ml.Stop ();
				}
				return true;
			};

			ml.AddIdle (fnStop);
			ml.AddIdle (fn1);
			ml.Run ();
			Assert.True (ml.RemoveIdle (fnStop));
			Assert.False (ml.RemoveIdle (fn1));

			Assert.Equal (10, functionCalled);
			Assert.Equal (20, stopCount);
		}

		[Fact]
		public void AddIdle_Twice_Returns_False_Called_Twice ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn1 = () => {
				functionCalled++;
				return false;
			};

			// Force stop if 10 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				stopCount++;
				if (stopCount == 10) {
					ml.Stop ();
				}
				return true;
			};

			ml.AddIdle (fnStop);
			ml.AddIdle (fn1);
			ml.AddIdle (fn1);
			ml.Run ();
			Assert.True (ml.RemoveIdle (fnStop));
			Assert.False (ml.RemoveIdle (fn1));
			Assert.False (ml.RemoveIdle (fn1));

			Assert.Equal (2, functionCalled);
		}

		[Fact]
		public void Run_Runs_Idle_Stop_Stops_Idle ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				if (functionCalled == 10) {
					ml.Stop ();
				}
				return true;
			};

			ml.AddIdle (fn);
			ml.Run ();
			Assert.True (ml.RemoveIdle (fn));

			Assert.Equal (10, functionCalled);
		}

		// Timeout Handler Tests
		[Fact]
		public void AddTimer_Adds_Removes_NoFaults ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = 100;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				return true;
			};

			var token = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);

			Assert.True (ml.RemoveTimeout (token));

			// BUGBUG: This should probably fault?
			// Must return a boolean.
			Assert.False (ml.RemoveTimeout (token));
		}

		[Fact]
		public void AddTimer_Run_Called ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = 100;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				ml.Stop ();
				return true;
			};

			var token = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);
			ml.Run ();
			Assert.True (ml.RemoveTimeout (token));

			Assert.Equal (1, callbackCount);
		}

		[Fact]
		public async Task AddTimer_Duplicate_Keys_Not_Allowed ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			const int ms = 100;
			object token1 = null, token2 = null;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				if (callbackCount == 2) {
					ml.Stop ();
				}
				return true;
			};

			var task1 = new Task (() => token1 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback));
			var task2 = new Task (() => token2 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback));
			Assert.Null (token1);
			Assert.Null (token2);
			task1.Start ();
			task2.Start ();
			ml.Run ();
			Assert.NotNull (token1);
			Assert.NotNull (token2);
			await Task.WhenAll (task1, task2);
			Assert.True (ml.RemoveTimeout (token1));
			Assert.True (ml.RemoveTimeout (token2));

			Assert.Equal (2, callbackCount);
		}

		[Fact]
		public void AddTimer_In_Parallel_Wont_Throw ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			const int ms = 100;
			object token1 = null, token2 = null;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				if (callbackCount == 2) {
					ml.Stop ();
				}
				return true;
			};

			Parallel.Invoke (
				() => token1 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback),
				() => token2 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback)
			);
			ml.Run ();
			Assert.NotNull (token1);
			Assert.NotNull (token2);
			Assert.True (ml.RemoveTimeout (token1));
			Assert.True (ml.RemoveTimeout (token2));

			Assert.Equal (2, callbackCount);
		}


		class MillisecondTolerance : IEqualityComparer<TimeSpan> {
			int _tolerance = 0;
			public MillisecondTolerance (int tolerance) { _tolerance = tolerance; }
			public bool Equals (TimeSpan x, TimeSpan y) => Math.Abs (x.Milliseconds - y.Milliseconds) <= _tolerance;
			public int GetHashCode (TimeSpan obj) => obj.GetHashCode ();
		}

		[Fact]
		public void AddTimer_Run_CalledAtApproximatelyRightTime ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);
			var watch = new System.Diagnostics.Stopwatch ();

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				watch.Stop ();
				callbackCount++;
				ml.Stop ();
				return true;
			};

			var token = ml.AddTimeout (ms, callback);
			watch.Start ();
			ml.Run ();
			// +/- 100ms should be good enuf
			// https://github.com/xunit/assert.xunit/pull/25
			Assert.Equal<TimeSpan> (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (100));

			Assert.True (ml.RemoveTimeout (token));
			Assert.Equal (1, callbackCount);
		}

		[Fact]
		public void AddTimer_Run_CalledTwiceApproximatelyRightTime ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);
			var watch = new System.Diagnostics.Stopwatch ();

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				if (callbackCount == 2) {
					watch.Stop ();
					ml.Stop ();
				}
				return true;
			};

			var token = ml.AddTimeout (ms, callback);
			watch.Start ();
			ml.Run ();
			// +/- 100ms should be good enuf
			// https://github.com/xunit/assert.xunit/pull/25
			Assert.Equal<TimeSpan> (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (100));

			Assert.True (ml.RemoveTimeout (token));
			Assert.Equal (2, callbackCount);
		}

		[Fact]
		public void AddTimer_Remove_NotCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);

			// Force stop if 10 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				stopCount++;
				if (stopCount == 10) {
					ml.Stop ();
				}
				return true;
			};
			ml.AddIdle (fnStop);

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				return true;
			};

			var token = ml.AddTimeout (ms, callback);
			Assert.True (ml.RemoveTimeout (token));
			ml.Run ();
			Assert.Equal (0, callbackCount);
		}

		[Fact]
		public void AddTimer_ReturnFalse_StopsBeingCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);

			// Force stop if 10 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				Thread.Sleep (10); // Sleep to enable timer to fire
				stopCount++;
				if (stopCount == 10) {
					ml.Stop ();
				}
				return true;
			};
			ml.AddIdle (fnStop);

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				return false;
			};

			var token = ml.AddTimeout (ms, callback);
			ml.Run ();
			Assert.Equal (1, callbackCount);
			Assert.Equal (10, stopCount);
			Assert.False (ml.RemoveTimeout (token));
		}

		// Invoke Tests
		// TODO: Test with threading scenarios
		[Fact]
		public void Invoke_Adds_Idle ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var actionCalled = 0;
			ml.Invoke (() => { actionCalled++; });
			ml.MainIteration ();
			Assert.Equal (1, actionCalled);
		}

		// TODO: EventsPending tests
		// - wait = true
		// - wait = false

		// TODO: Add IMainLoop tests
	}
}
