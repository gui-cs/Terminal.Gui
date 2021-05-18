using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

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
			// Verify initial state is per spec
			Pre_Init_State ();

			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			// Verify post-Init state is correct
			Post_Init_State ();

			// MockDriver is always 80x25
			Assert.Equal (80, Application.Driver.Cols);
			Assert.Equal (25, Application.Driver.Rows);

			Application.Shutdown ();

			// Verify state is back to initial
			Pre_Init_State ();

		}

		void Pre_Init_State ()
		{
			Assert.Null (Application.Driver);
			Assert.Null (Application.Top);
			Assert.Null (Application.Current);
			Assert.Throws<ArgumentNullException> (() => Application.HeightAsBuffer == true);
			Assert.False (Application.AlwaysSetPosition);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Iteration);
			Assert.Null (Application.RootMouseEvent);
			Assert.Null (Application.Resized);
		}

		void Post_Init_State ()
		{
			Assert.NotNull (Application.Driver);
			Assert.NotNull (Application.Top);
			Assert.NotNull (Application.Current);
			Assert.False (Application.HeightAsBuffer);
			Assert.False (Application.AlwaysSetPosition);
			Assert.NotNull (Application.MainLoop);
			Assert.Null (Application.Iteration);
			Assert.Null (Application.RootMouseEvent);
			Assert.Null (Application.Resized);
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

			// Setup some fake keypresses (This)
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
			//Assert.Equal (stackSize, keyUps);
			// We can't use numbers variables on the left side of an Assert.Equal/NotEqual,
			// it must be literal (Linux only).
			Assert.Equal (6, keyUps);

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

		[Fact]
		public void AlternateForwardKey_AlternateBackwardKey_Tests ()
		{
			Init ();

			var top = Application.Top;
			var w1 = new Window ();
			var v1 = new TextField ();
			var v2 = new TextView ();
			w1.Add (v1, v2);

			var w2 = new Window ();
			var v3 = new CheckBox ();
			var v4 = new Button ();
			w2.Add (v3, v4);

			top.Add (w1, w2);

			Application.Iteration += () => {
				Assert.True (v1.HasFocus);
				// Using default keys.
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v2.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v3.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v4.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v1.HasFocus);

				top.ProcessKey (new KeyEvent (Key.ShiftMask | Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Shift = true, Ctrl = true }));
				Assert.True (v4.HasFocus);
				top.ProcessKey (new KeyEvent (Key.ShiftMask | Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Shift = true, Ctrl = true }));
				Assert.True (v3.HasFocus);
				top.ProcessKey (new KeyEvent (Key.ShiftMask | Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Shift = true, Ctrl = true }));
				Assert.True (v2.HasFocus);
				top.ProcessKey (new KeyEvent (Key.ShiftMask | Key.CtrlMask | Key.Tab,
					new KeyModifiers () { Shift = true, Ctrl = true }));
				Assert.True (v1.HasFocus);

				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageDown,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v2.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageDown,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v3.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageDown,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v4.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageDown,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v1.HasFocus);

				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageUp,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v4.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageUp,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v3.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageUp,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v2.HasFocus);
				top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.PageUp,
					new KeyModifiers () { Ctrl = true }));
				Assert.True (v1.HasFocus);

				// Using another's alternate keys.
				Application.AlternateForwardKey = Key.F7;
				Application.AlternateBackwardKey = Key.F6;

				top.ProcessKey (new KeyEvent (Key.F7, new KeyModifiers ()));
				Assert.True (v2.HasFocus);
				top.ProcessKey (new KeyEvent (Key.F7, new KeyModifiers ()));
				Assert.True (v3.HasFocus);
				top.ProcessKey (new KeyEvent (Key.F7, new KeyModifiers ()));
				Assert.True (v4.HasFocus);
				top.ProcessKey (new KeyEvent (Key.F7, new KeyModifiers ()));
				Assert.True (v1.HasFocus);

				top.ProcessKey (new KeyEvent (Key.F6, new KeyModifiers ()));
				Assert.True (v4.HasFocus);
				top.ProcessKey (new KeyEvent (Key.F6, new KeyModifiers ()));
				Assert.True (v3.HasFocus);
				top.ProcessKey (new KeyEvent (Key.F6, new KeyModifiers ()));
				Assert.True (v2.HasFocus);
				top.ProcessKey (new KeyEvent (Key.F6, new KeyModifiers ()));
				Assert.True (v1.HasFocus);

				Application.RequestStop ();
			};

			Application.Run (top);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}
	}
}
