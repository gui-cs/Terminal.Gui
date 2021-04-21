using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

// Since Application is a singleton we can't run tests in parallel
[assembly: CollectionBehavior (DisableTestParallelization = true)]

namespace Terminal.Gui.Core {
	public class ApplicationTests {
		public ApplicationTests ()
		{
#if DEBUG_IDISPOSABLE
			Responder.Instances.Clear ();
#endif
		}

		[Fact]
		public void Init_Shutdown_Cleans_Up ()
		{
			Assert.Null (Application.Current);
			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);

			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			Assert.NotNull (Application.Current);
			Assert.NotNull (Application.Top);
			Assert.NotNull (Application.MainLoop);
			Assert.NotNull (Application.Driver);

			// MockDriver is always 80x25
			Assert.Equal (80, Application.Driver.Cols);
			Assert.Equal (25, Application.Driver.Rows);

			Application.Shutdown ();
			Assert.Null (Application.Current);
			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void RunState_Dispose_Cleans_Up ()
		{
			var rs = new Application.RunState (null);
			Assert.NotNull (rs);

			// Should not throw because Toplevel was null
			rs.Dispose ();

			var top = new Toplevel ();
			rs = new Application.RunState (top);
			Assert.NotNull (rs);

			// Should throw because there's no stack
			Assert.Throws<InvalidOperationException> (() => rs.Dispose ());
		}

		void Init ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			Assert.NotNull (Application.Driver);
			Assert.NotNull (Application.MainLoop);
		}

		void Shutdown ()
		{
			Application.Shutdown ();
		}

		[Fact]
		public void Begin_End_Cleana_Up ()
		{
			// Setup Mock driver
			Init ();

			// Test null Toplevel
			Assert.Throws<ArgumentNullException> (() => Application.Begin (null));

			var top = new Toplevel ();
			var rs = Application.Begin (top);
			Assert.NotNull (rs);
			Assert.Equal (top, Application.Current);
			Application.End (rs);

			Assert.Null (Application.Current);
			Assert.NotNull (Application.Top);
			Assert.NotNull (Application.MainLoop);
			Assert.NotNull (Application.Driver);

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void RequestStop_Stops ()
		{
			// Setup Mock driver
			Init ();

			var top = new Toplevel ();
			var rs = Application.Begin (top);
			Assert.NotNull (rs);
			Assert.Equal (top, Application.Current);

			Application.Iteration = () => {
				Application.RequestStop ();
			};

			Application.Run (top);

			Application.Shutdown ();
			Assert.Null (Application.Current);
			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void RunningFalse_Stops ()
		{
			// Setup Mock driver
			Init ();

			var top = new Toplevel ();
			var rs = Application.Begin (top);
			Assert.NotNull (rs);
			Assert.Equal (top, Application.Current);

			Application.Iteration = () => {
				top.Running = false;
			};

			Application.Run (top);

			Application.Shutdown ();
			Assert.Null (Application.Current);
			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}


		[Fact]
		public void KeyUp_Event ()
		{
			// Setup Mock driver
			Init ();

			// Setup some fake kepresses (This)
			var input = "Tests";

			// Put a control-q in at the end
			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('q', ConsoleKey.Q, shift: false, alt: false, control: true));
			foreach (var c in input.Reverse ()) {
				if (char.IsLetter (c)) {
					Console.MockKeyPresses.Push (new ConsoleKeyInfo (c, (ConsoleKey)char.ToUpper (c), shift: char.IsUpper (c), alt: false, control: false));
				} else {
					Console.MockKeyPresses.Push (new ConsoleKeyInfo (c, (ConsoleKey)c, shift: false, alt: false, control: false));
				}
			}

			int stackSize = Console.MockKeyPresses.Count;

			int iterations = 0;
			Application.Iteration = () => {
				iterations++;
				// Stop if we run out of control...
				if (iterations > 10) {
					Application.RequestStop ();
				}
			};

			int keyUps = 0;
			var output = string.Empty;
			Application.Top.KeyUp += (View.KeyEventEventArgs args) => {
				if (args.KeyEvent.Key != (Key.CtrlMask | Key.Q)) {
					output += (char)args.KeyEvent.KeyValue;
				}
				keyUps++;
			};

			Application.Run (Application.Top);

			// Input string should match output
			Assert.Equal (input, output);

			// # of key up events should match stack size
			Assert.Equal (stackSize, keyUps);

			// # of key up events should match # of iterations
			Assert.Equal (stackSize, iterations);

			Application.Shutdown ();
			Assert.Null (Application.Current);
			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Loaded_Ready_Unlodaded_Events ()
		{
			Init ();
			var top = Application.Top;
			var count = 0;
			top.Loaded += () => count++;
			top.Ready += () => count++;
			top.Unloaded += () => count++;
			Application.Iteration = () => Application.RequestStop ();
			Application.Run ();
			Application.Shutdown ();
			Assert.Equal (3, count);
		}

		[Fact]
		public void Shutdown_Allows_Async ()
		{
			static async Task TaskWithAsyncContinuation ()
			{
				await Task.Yield ();
				await Task.Yield ();
			}

			Init ();
			Application.Shutdown ();

			var task = TaskWithAsyncContinuation ();
			Thread.Sleep (20);
			Assert.True (task.IsCompletedSuccessfully);
		}

		[Fact]
		public void Shutdown_Resets_SyncContext ()
		{
			Init ();
			Application.Shutdown ();
			Assert.Null (SynchronizationContext.Current);
		}
	}
}
