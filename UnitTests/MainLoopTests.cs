using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using Terminal.Gui;
using Xunit;
using Xunit.Sdk;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	public class MainLoopTests {

		[Fact]
		public void Constructor_Setups_Driver ()
		{
			var ml = new MainLoop (new NetMainLoop(() => FakeConsole.ReadKey (true)));
			Assert.NotNull (ml.Driver);
		}

		// Idle Handler tests
		[Fact]
		public void AddIdle_Adds_And_Removes ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

			Func<bool> fnTrue = () => { return true; };
			Func<bool> fnFalse = () => { return false; };
			ml.AddIdle (fnTrue);
			ml.AddIdle (fnFalse);

			ml.RemoveIdle (fnTrue);

			// BUGBUG: This doens't throw or indicate an error. Ideally RemoveIdle would either 
			// trhow an exception in this case, or return an error.
			ml.RemoveIdle (fnTrue);

			ml.RemoveIdle (fnFalse);

			// BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
			// trhow an exception in this case, or return an error.
			ml.RemoveIdle (fnFalse);

			// Add again, but with dupe
			ml.AddIdle (fnTrue);
			ml.AddIdle (fnTrue);

			ml.RemoveIdle (fnTrue);
			ml.RemoveIdle (fnTrue);

			// BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
			// trhow an exception in this case, or return an error.
			ml.RemoveIdle (fnTrue);
		}

		[Fact]
		public void AddIdle_Function_GetsCalled_OnIteration ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

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
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			functionCalled = 0;
			ml.RemoveIdle (fn);
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
		}

		[Fact]
		public void AddThenRemoveIdle_Function_NotCalled ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			functionCalled = 0;
			ml.AddIdle (fn);
			ml.RemoveIdle (fn);
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
		}

		[Fact]
		public void AddTwice_Function_CalledTwice ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			functionCalled = 0;
			ml.AddIdle (fn);
			ml.AddIdle (fn);
			ml.MainIteration ();
			Assert.Equal (2, functionCalled);

			functionCalled = 0;
			ml.RemoveIdle (fn);
			ml.MainIteration ();
			Assert.Equal (1, functionCalled);

			functionCalled = 0;
			ml.RemoveIdle (fn);
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
		}

		[Fact]
		public void False_Idle_Stops_It_Being_Called_Again ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

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
			ml.RemoveIdle (fnStop);
			ml.RemoveIdle (fn1);

			Assert.Equal (10, functionCalled);
		}

		[Fact]
		public void AddIdle_Twice_Returns_False_Called_Twice ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

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
			ml.RemoveIdle (fnStop);
			ml.RemoveIdle (fn1);
			ml.RemoveIdle (fn1);

			Assert.Equal (2, functionCalled);
		}

		[Fact]
		public void Run_Runs_Idle_Stop_Stops_Idle ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

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
			ml.RemoveIdle (fn);

			Assert.Equal (10, functionCalled);
		}

		// Timeout Handler Tests
		[Fact]
		public void AddTimer_Adds_Removes_NoFaults ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = 100;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				return true;
			};

			var token = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);

			ml.RemoveTimeout (token);

			// BUGBUG: This should probably fault?
			ml.RemoveTimeout (token);
		}

		[Fact]
		public void AddTimer_Run_Called ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = 100;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				ml.Stop ();
				return true;
			};

			var token = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);
			ml.Run ();
			ml.RemoveTimeout (token);

			Assert.Equal (1, callbackCount);
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
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));
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
			// +/- 10ms should be good enuf
			// https://github.com/xunit/assert.xunit/pull/25
			Assert.Equal<TimeSpan> (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (10));

			ml.RemoveTimeout (token);
			Assert.Equal (1, callbackCount);
		}

		[Fact]
		public void AddTimer_Run_CalledTwiceApproximatelyRightTime ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));
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
			// +/- 10ms should be good enuf
			// https://github.com/xunit/assert.xunit/pull/25
			Assert.Equal<TimeSpan> (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (10));

			ml.RemoveTimeout (token);
			Assert.Equal (2, callbackCount);
		}

		[Fact]
		public void AddTimer_Remove_NotCalled ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));
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
			ml.RemoveTimeout (token);
			ml.Run ();
			Assert.Equal (0, callbackCount);
		}

		[Fact]
		public void AddTimer_ReturnFalse_StopsBeingCalled ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);

			// Force stop if 10 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				Thread.Sleep (10); // Sleep to enable timeer to fire
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
			ml.RemoveTimeout (token);
		}

		// Invoke Tests
		// TODO: Test with threading scenarios
		[Fact]
		public void Invoke_Adds_Idle ()
		{
			var ml = new MainLoop (new NetMainLoop (() => FakeConsole.ReadKey (true)));

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
