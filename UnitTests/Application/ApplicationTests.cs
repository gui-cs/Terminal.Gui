using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ApplicationTests {
	public class ApplicationTests {
		public ApplicationTests ()
		{
#if DEBUG_IDISPOSABLE
			Responder.Instances.Clear ();
			Application.RunState.Instances.Clear ();
#endif
		}

		void Pre_Init_State ()
		{
			Assert.Null (Application.Driver);
			Assert.Null (Application.Top);
			Assert.Null (Application.Current);
			Assert.False (Application.EnableConsoleScrolling);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Iteration);
			Assert.Null (Application.RootMouseEvent);
			Assert.Null (Application.TerminalResized);
		}

		void Post_Init_State ()
		{
			Assert.NotNull (Application.Driver);
			Assert.NotNull (Application.Top);
			Assert.NotNull (Application.Current);
			Assert.False (Application.EnableConsoleScrolling);
			Assert.NotNull (Application.MainLoop);
			Assert.Null (Application.Iteration);
			Assert.Null (Application.RootMouseEvent);
			Assert.Null (Application.TerminalResized);
		}

		void Init ()
		{
			Application.Init (new FakeDriver ());
			Assert.NotNull (Application.Driver);
			Assert.NotNull (Application.MainLoop);
			Assert.NotNull (SynchronizationContext.Current);
		}

		void Shutdown ()
		{
			Application.Shutdown ();
		}

		[Fact]
		public void Init_Shutdown_Cleans_Up ()
		{
			// Verify initial state is per spec
			Pre_Init_State ();

			Application.Init (new FakeDriver ());

			// Verify post-Init state is correct
			Post_Init_State ();

			// MockDriver is always 80x25
			Assert.Equal (80, Application.Driver.Cols);
			Assert.Equal (25, Application.Driver.Rows);

			Application.Shutdown ();

			// Verify state is back to initial
			Pre_Init_State ();
#if DEBUG_IDISPOSABLE
			// Validate there are no outstanding Responder-based instances 
			// after a scenario was selected to run. This proves the main UI Catalog
			// 'app' closed cleanly.
			foreach (var inst in Responder.Instances) {
				Assert.True (inst.WasDisposed);
			}
#endif
		}

		[Fact]
		public void Init_Shutdown_Toplevel_Not_Disposed ()
		{
			Application.Init (new FakeDriver ());

			Application.Shutdown ();

#if DEBUG_IDISPOSABLE
			// 4 for Application.Top and it's 3 Frames
			Assert.Equal (4, Responder.Instances.Count);
			Assert.True (Responder.Instances [0].WasDisposed);
#endif
		}

		[Fact]
		public void Init_Unbalanced_Throwss ()
		{
			Application.Init (new FakeDriver ());

			Toplevel topLevel = null;
			Assert.Throws<InvalidOperationException> (() => Application.InternalInit (() => topLevel = new TestToplevel (), new FakeDriver ()));
			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);

			// Now try the other way
			topLevel = null;
			Application.InternalInit (() => topLevel = new TestToplevel (), new FakeDriver ());

			Assert.Throws<InvalidOperationException> (() => Application.Init (new FakeDriver ()));
			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		class TestToplevel : Toplevel {
			public TestToplevel ()
			{
				IsOverlappedContainer = false;
			}
		}

		[Fact]
		public void Init_Begin_End_Cleans_Up_New_Toplevel ()
		{
			Init ();

			// Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
			// if we don't stop
			Application.Iteration = () => {
				Application.RequestStop ();
			};

			Application.RunState runstate = null;
			EventHandler<RunStateEventArgs> NewRunStateFn = (s, e) => {
				Assert.NotNull (e.State);
				runstate = e.State;
			};
			Application.NotifyNewRunState += NewRunStateFn;

			Toplevel topLevel = new Toplevel ();
			var rs = Application.Begin (topLevel);
			Assert.NotNull (rs);
			Assert.NotNull (runstate);
			Assert.Equal (rs, runstate);

			Assert.NotEqual (topLevel, Application.Top);
			Assert.Equal (topLevel, Application.Current);

			Application.NotifyNewRunState -= NewRunStateFn;
			Application.End (runstate);

			Assert.NotNull (Application.Current);
			Assert.NotNull (Application.Top);
			Assert.NotNull (Application.MainLoop);
			Assert.NotNull (Application.Driver);

			Shutdown ();

			Assert.Null (Application.Current);
			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Init_Begin_End_Cleans_Up_Application_Top ()
		{
			Init ();

			// Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
			// if we don't stop
			Application.Iteration = () => {
				Application.RequestStop ();
			};

			Application.RunState runstate = null;
			EventHandler<RunStateEventArgs> NewRunStateFn = (s, e) => {
				Assert.NotNull (e.State);
				runstate = e.State;
			};
			Application.NotifyNewRunState += NewRunStateFn;

			Toplevel topLevel = Application.Top;
			var rs = Application.Begin (topLevel);
			Assert.NotNull (rs);
			Assert.NotNull (runstate);
			Assert.Equal (rs, runstate);

			Assert.Equal (topLevel, Application.Top);
			Assert.Equal (topLevel, Application.Current);

			Application.NotifyNewRunState -= NewRunStateFn;
			Application.End (runstate);

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
		public void InitWithTopLevelFactory_Begin_End_Cleans_Up ()
		{
			// Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
			// if we don't stop
			Application.Iteration = () => {
				Application.RequestStop ();
			};

			// NOTE: Run<T>, when called after Init has been called behaves differently than
			// when called if Init has not been called.
			Toplevel topLevel = null;
			Application.InternalInit (() => topLevel = new TestToplevel (), new FakeDriver ());

			Application.RunState runstate = null;
			EventHandler<RunStateEventArgs> NewRunStateFn = (s, e) => {
				Assert.NotNull (e.State);
				runstate = e.State;
			};
			Application.NotifyNewRunState += NewRunStateFn;

			var rs = Application.Begin (topLevel);
			Assert.NotNull (rs);
			Assert.NotNull (runstate);
			Assert.Equal (rs, runstate);

			Assert.Equal (topLevel, Application.Top);
			Assert.Equal (topLevel, Application.Current);

			Application.NotifyNewRunState -= NewRunStateFn;
			Application.End (runstate);

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
		public void Begin_Null_Toplevel_Throws ()
		{
			// Setup Mock driver
			Init ();

			// Test null Toplevel
			Assert.Throws<ArgumentNullException> (() => Application.Begin (null));

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		#region RunTests

		[Fact]
		public void Run_T_After_InitWithDriver_with_TopLevel_Throws ()
		{
			// Setup Mock driver
			Init ();

			// Run<Toplevel> when already initialized with a Driver will throw (because Toplevel is not dervied from TopLevel)
			Assert.Throws<ArgumentException> (() => Application.Run<Toplevel> (errorHandler: null));

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Run_T_After_InitWithDriver_with_TopLevel_and_Driver_Throws ()
		{
			// Setup Mock driver
			Init ();

			// Run<Toplevel> when already initialized with a Driver will throw (because Toplevel is not dervied from TopLevel)
			Assert.Throws<ArgumentException> (() => Application.Run<Toplevel> (errorHandler: null, new FakeDriver ()));

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Run_T_After_InitWithDriver_with_TestTopLevel_DoesNotThrow ()
		{
			// Setup Mock driver
			Init ();

			Application.Iteration = () => {
				Application.RequestStop ();
			};

			// Init has been called and we're passing no driver to Run<TestTopLevel>. This is ok.
			Application.Run<TestToplevel> ();

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Run_T_After_InitNullDriver_with_TestTopLevel_Throws ()
		{
			Application._forceFakeConsole = true;

			Application.Init (null, null);
			Assert.Equal (typeof (FakeDriver), Application.Driver.GetType ());

			Application.Iteration = () => {
				Application.RequestStop ();
			};

			// Init has been called without selecting a driver and we're passing no driver to Run<TestTopLevel>. Bad
			Application.Run<TestToplevel> ();

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Run_T_Init_Driver_Cleared_with_TestTopLevel_Throws ()
		{
			Init ();

			Application.Driver = null;

			Application.Iteration = () => {
				Application.RequestStop ();
			};

			// Init has been called, but Driver has been set to null. Bad.
			Assert.Throws<InvalidOperationException> (() => Application.Run<TestToplevel> ());

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Run_T_NoInit_DoesNotThrow ()
		{
			Application._forceFakeConsole = true;

			Application.Iteration = () => {
				Application.RequestStop ();
			};

			Application.Run<TestToplevel> ();
			Assert.Equal (typeof (FakeDriver), Application.Driver.GetType ());

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Run_T_NoInit_WithDriver_DoesNotThrow ()
		{
			Application.Iteration = () => {
				Application.RequestStop ();
			};

			// Init has NOT been called and we're passing a valid driver to Run<TestTopLevel>. This is ok.
			Application.Run<TestToplevel> (errorHandler: null, new FakeDriver ());

			Shutdown ();

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}

		[Fact]
		public void Run_RequestStop_Stops ()
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
		public void Run_RunningFalse_Stops ()
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
		public void Run_Loaded_Ready_Unlodaded_Events ()
		{
			Init ();
			var top = Application.Top;
			var count = 0;
			top.Loaded += (s, e) => count++;
			top.Ready += (s, e) => count++;
			top.Unloaded += (s, e) => count++;
			Application.Iteration = () => Application.RequestStop ();
			Application.Run ();
			Application.Shutdown ();
			Assert.Equal (3, count);
		}

		// TODO: Add tests for Run that test errorHandler

		#endregion

		#region ShutdownTests
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
		#endregion

		[Fact, AutoInitShutdown]
		public void Begin_Sets_Application_Top_To_Console_Size ()
		{
			Assert.Equal (new Rect (0, 0, 80, 25), Application.Top.Frame);

			((FakeDriver)Application.Driver).SetBufferSize (5, 5);
			Application.Begin (Application.Top);
			Assert.Equal (new Rect (0, 0, 5, 5), Application.Top.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void Run_A_Not_Modal_Toplevel_Does_Not_Make_It_The_Current_Application_Top ()
		{
			var t1 = Application.Top;
			var t2 = new Toplevel ();
			var t3 = new Toplevel ();
			var d = new Dialog ();
			var t4 = new Toplevel ();

			// t1, t2, t3, d, t4
			var iterations = 5;

			t1.Ready += (s, e) => {
				Assert.Equal (t1, Application.Top);
				Application.Run (t2);
			};
			t2.Ready += (s, e) => {
				Assert.Equal (t1, Application.Top);
				Assert.Equal (t2, Application.Current);
				Application.Run (t3);
			};
			t3.Ready += (s, e) => {
				Assert.Equal (t1, Application.Top);
				Assert.Equal (t3, Application.Current);
				Application.Run (d);
			};
			d.Ready += (s, e) => {
				Assert.Equal (t1, Application.Top);
				Assert.Equal (d, Application.Current);
				Application.Run (t4);
			};
			t4.Ready += (s, e) => {
				Assert.Equal (t1, Application.Top);
				Assert.Equal (t4, Application.Current);
				t4.RequestStop ();
				d.RequestStop ();
				t3.RequestStop ();
				t2.RequestStop ();
			};
			// Now this will close the OverlappedContainer when all OverlappedChildren was closed
			t2.Closed += (s, _) => {
				t1.RequestStop ();
			};
			Application.Iteration += () => {
				if (iterations == 5) {
					// The Current still is t4 because Current.Running is false.
					Assert.Equal (t4, Application.Current);
					Assert.False (Application.Current.Running);
					Assert.Equal (t1, Application.Top);
				} else if (iterations == 4) {
					// The Current is d and Current.Running is false.
					Assert.Equal (d, Application.Current);
					Assert.False (Application.Current.Running);
					Assert.Equal (t1, Application.Top);
				} else if (iterations == 3) {
					// The Current is t3 and Current.Running is false.
					Assert.Equal (t3, Application.Current);
					Assert.False (Application.Current.Running);
					Assert.Equal (t1, Application.Top);
				} else if (iterations == 2) {
					// The Current is t2 and Current.Running is false.
					Assert.Equal (t2, Application.Current);
					Assert.False (Application.Current.Running);
					Assert.Equal (t1, Application.Top);
				} else {
					// The Current is t1.
					Assert.Equal (t1, Application.Current);
					Assert.False (Application.Current.Running);
					Assert.Equal (t1, Application.Top);
				}
				iterations--;
			};

			Application.Run (t1);

			Assert.Equal (t1, Application.Top);
		}

		[Fact]
		[AutoInitShutdown]
		public void Internal_Properties_Correct ()
		{
			Assert.True (Application._initialized);
			Assert.NotNull (Application.Top);
			var rs = Application.Begin (Application.Top);
			Assert.Equal (Application.Top, rs.Toplevel);
			Assert.Null (Application.MouseGrabView);  // public
			Assert.Null (Application.WantContinuousButtonPressedView); // public
			Assert.False (Application.MoveToOverlappedChild (Application.Top));
		}

		#region KeyboardTests
		[Fact]
		public void KeyUp_Event ()
		{
			// Setup Mock driver
			Init ();

			// Setup some fake keypresses (This)
			var input = "Tests";

			// Put a control-q in at the end
			FakeConsole.MockKeyPresses.Push (new ConsoleKeyInfo ('q', ConsoleKey.Q, shift: false, alt: false, control: true));
			foreach (var c in input.Reverse ()) {
				if (char.IsLetter (c)) {
					FakeConsole.MockKeyPresses.Push (new ConsoleKeyInfo (c, (ConsoleKey)char.ToUpper (c), shift: char.IsUpper (c), alt: false, control: false));
				} else {
					FakeConsole.MockKeyPresses.Push (new ConsoleKeyInfo (c, (ConsoleKey)c, shift: false, alt: false, control: false));
				}
			}

			int stackSize = FakeConsole.MockKeyPresses.Count;

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
			Application.Top.KeyUp += (object sender, KeyEventEventArgs args) => {
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

			// Replacing the defaults keys to avoid errors on others unit tests that are using it.
			Application.AlternateForwardKey = Key.PageDown | Key.CtrlMask;
			Application.AlternateBackwardKey = Key.PageUp | Key.CtrlMask;
			Application.QuitKey = Key.Q | Key.CtrlMask;

			Assert.Equal (Key.PageDown | Key.CtrlMask, Application.AlternateForwardKey);
			Assert.Equal (Key.PageUp | Key.CtrlMask, Application.AlternateBackwardKey);
			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void QuitKey_Getter_Setter ()
		{
			var top = Application.Top;
			var isQuiting = false;

			top.Closing += (s, e) => {
				isQuiting = true;
				e.Cancel = true;
			};

			Application.Begin (top);
			top.Running = true;

			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);
			Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
			Assert.True (isQuiting);

			isQuiting = false;
			Application.QuitKey = Key.C | Key.CtrlMask;

			Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
			Assert.False (isQuiting);
			Application.Driver.SendKeys ('c', ConsoleKey.C, false, false, true);
			Assert.True (isQuiting);

			// Reset the QuitKey to avoid throws errors on another tests
			Application.QuitKey = Key.Q | Key.CtrlMask;
		}

		[Fact]
		[AutoInitShutdown]
		public void EnsuresTopOnFront_CanFocus_True_By_Keyboard_And_Mouse ()
		{
			var top = Application.Top;
			var win = new Window () { Title = "win", X = 0, Y = 0, Width = 20, Height = 10 };
			var tf = new TextField () { Width = 10 };
			win.Add (tf);
			var win2 = new Window () { Title = "win2", X = 22, Y = 0, Width = 20, Height = 10 };
			var tf2 = new TextField () { Width = 10 };
			win2.Add (tf2);
			top.Add (win, win2);

			Application.Begin (top);

			Assert.True (win.CanFocus);
			Assert.True (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.False (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

			top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab, new KeyModifiers ()));
			Assert.True (win.CanFocus);
			Assert.False (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.True (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

			top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab, new KeyModifiers ()));
			Assert.True (win.CanFocus);
			Assert.True (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.False (win2.HasFocus);
			Assert.Equal ("win", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

			win2.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Pressed });
			Assert.True (win.CanFocus);
			Assert.False (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.True (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);
			win2.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Released });
			Assert.Null (Toplevel._dragPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void EnsuresTopOnFront_CanFocus_False_By_Keyboard_And_Mouse ()
		{
			var top = Application.Top;
			var win = new Window () { Title = "win", X = 0, Y = 0, Width = 20, Height = 10 };
			var tf = new TextField () { Width = 10 };
			win.Add (tf);
			var win2 = new Window () { Title = "win2", X = 22, Y = 0, Width = 20, Height = 10 };
			var tf2 = new TextField () { Width = 10 };
			win2.Add (tf2);
			top.Add (win, win2);

			Application.Begin (top);

			Assert.True (win.CanFocus);
			Assert.True (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.False (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

			win.CanFocus = false;
			Assert.False (win.CanFocus);
			Assert.False (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.True (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

			top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab, new KeyModifiers ()));
			Assert.True (win2.CanFocus);
			Assert.False (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.True (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

			top.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Tab, new KeyModifiers ()));
			Assert.False (win.CanFocus);
			Assert.False (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.True (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

			win.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Pressed });
			Assert.False (win.CanFocus);
			Assert.False (win.HasFocus);
			Assert.True (win2.CanFocus);
			Assert.True (win2.HasFocus);
			Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);
			win2.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Released });
			Assert.Null (Toplevel._dragPosition);
		}

		#endregion


		#region mousegrabtests
		[Fact, AutoInitShutdown]
		public void MouseGrabView_WithNullMouseEventView ()
		{
			var tf = new TextField () { Width = 10 };
			var sv = new ScrollView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ContentSize = new Size (100, 100)
			};

			sv.Add (tf);
			Application.Top.Add (sv);

			var iterations = -1;

			Application.Iteration = () => {
				iterations++;
				if (iterations == 0) {
					Assert.True (tf.HasFocus);
					Assert.Null (Application.MouseGrabView);

					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 5,
							Y = 5,
							Flags = MouseFlags.ReportMousePosition
						});

					Assert.Equal (sv, Application.MouseGrabView);

					MessageBox.Query ("Title", "Test", "Ok");

					Assert.Null (Application.MouseGrabView);
				} else if (iterations == 1) {
					Assert.Equal (sv, Application.MouseGrabView);

					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 5,
							Y = 5,
							Flags = MouseFlags.ReportMousePosition
						});

					Assert.Equal (sv, Application.MouseGrabView);

					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 40,
							Y = 12,
							Flags = MouseFlags.ReportMousePosition
						});

					Assert.Null (Application.MouseGrabView);

					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 0,
							Y = 0,
							Flags = MouseFlags.Button1Pressed
						});

					Assert.Null (Application.MouseGrabView);

					Application.RequestStop ();
				} else if (iterations == 2) {
					Assert.Null (Application.MouseGrabView);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void MouseGrabView_GrabbedMouse_UnGrabbedMouse ()
		{
			View grabView = null;
			var count = 0;

			var view1 = new View ();
			var view2 = new View ();

			Application.GrabbedMouse += Application_GrabbedMouse;
			Application.UnGrabbedMouse += Application_UnGrabbedMouse;

			Application.GrabMouse (view1);
			Assert.Equal (0, count);
			Assert.Equal (grabView, view1);
			Assert.Equal (view1, Application.MouseGrabView);

			Application.UngrabMouse ();
			Assert.Equal (1, count);
			Assert.Equal (grabView, view1);
			Assert.Null (Application.MouseGrabView);

			Application.GrabbedMouse += Application_GrabbedMouse;
			Application.UnGrabbedMouse += Application_UnGrabbedMouse;

			Application.GrabMouse (view2);
			Assert.Equal (1, count);
			Assert.Equal (grabView, view2);
			Assert.Equal (view2, Application.MouseGrabView);

			Application.UngrabMouse ();
			Assert.Equal (2, count);
			Assert.Equal (grabView, view2);
			Assert.Null (Application.MouseGrabView);

			void Application_GrabbedMouse (object sender, ViewEventArgs e)
			{
				if (count == 0) {
					Assert.Equal (view1, e.View);
					grabView = view1;
				} else {
					Assert.Equal (view2, e.View);
					grabView = view2;
				}

				Application.GrabbedMouse -= Application_GrabbedMouse;
			}

			void Application_UnGrabbedMouse (object sender, ViewEventArgs e)
			{
				if (count == 0) {
					Assert.Equal (view1, e.View);
					Assert.Equal (grabView, e.View);
				} else {
					Assert.Equal (view2, e.View);
					Assert.Equal (grabView, e.View);
				}
				count++;

				Application.UnGrabbedMouse -= Application_UnGrabbedMouse;
			}
		}
		#endregion

		[Fact, AutoInitShutdown]
		public void Applicaton_Top_LayoutStyle_Pos_Dim_Frame ()
		{
			Assert.Equal (LayoutStyle.Absolute, Application.Top.LayoutStyle);
			Assert.False (Application.Top.IsInitialized);
			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);

			Application.Begin (Application.Top);

			Assert.Equal (LayoutStyle.Absolute, Application.Top.LayoutStyle);
			Assert.True (Application.Top.IsInitialized);
			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);

			Application.Top.LayoutStyle = LayoutStyle.Computed;

			Assert.Equal (LayoutStyle.Absolute, Application.Top.LayoutStyle);

			Application.Top.X = 1;
			Application.Top.Y = 1;
			Application.Top.Width = 1;
			Application.Top.Height = 1;

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);

			Application.Top.Frame = new Rect (1, 1, 1, 1);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);

			((FakeDriver)Application.Driver).SetBufferSize (1, 1);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (new Rect (0, 0, 1, 1), Application.Top.Frame);

			// empty frame
			((FakeDriver)Application.Driver).SetBufferSize (0, 0);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (Rect.Empty, Application.Top.Frame);
		}

		[Fact, AutoInitShutdown]
		public void Applicaton_Top_LayoutStyle_Pos_Dim_Frame_With_Other_Toplevel ()
		{
			var top = new Toplevel (new Rect (1, 1, 10, 10));

			Application.Begin (top);

			Assert.Equal (LayoutStyle.Absolute, Application.Top.LayoutStyle);
			Assert.True (Application.Top.IsInitialized);
			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (LayoutStyle.Absolute, top.LayoutStyle);
			Assert.True (top.IsInitialized);
			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (1, 1, 10, 10), top.Frame);

			top.LayoutStyle = LayoutStyle.Computed;

			Assert.Equal (LayoutStyle.Absolute, Application.Top.LayoutStyle);
			Assert.Equal (LayoutStyle.Computed, top.LayoutStyle);

			top.X = 1;
			top.Y = 1;
			top.Width = 1;
			top.Height = 1;

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (1, top.X);
			Assert.Equal (1, top.Y);
			Assert.Equal (1, top.Width);
			Assert.Equal (1, top.Height);
			Assert.Equal (new Rect (1, 1, 1, 1), top.Frame);

			top.Frame = new Rect (5, 5, 10, 10);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (1, top.X);
			Assert.Equal (1, top.Y);
			Assert.Equal (1, top.Width);
			Assert.Equal (1, top.Height);
			Assert.Equal (new Rect (5, 5, 10, 10), top.Frame);

			top.LayoutStyle = LayoutStyle.Absolute;
			((FakeDriver)Application.Driver).SetBufferSize (1, 1);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (new Rect (0, 0, 1, 1), Application.Top.Frame);
			Assert.Equal (1, top.X);
			Assert.Equal (1, top.Y);
			Assert.Equal (1, top.Width);
			Assert.Equal (1, top.Height);
			Assert.Equal (new Rect (5, 5, 10, 10), top.Frame);

			top.LayoutStyle = LayoutStyle.Computed;
			((FakeDriver)Application.Driver).SetBufferSize (1, 1);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (new Rect (0, 0, 1, 1), Application.Top.Frame);
			Assert.Equal (0, top.X);
			Assert.Equal (0, top.Y);
			Assert.Equal (1, top.Width);
			Assert.Equal (1, top.Height);
			Assert.Equal (new Rect (0, 0, 1, 1), top.Frame);

			top.LayoutStyle = LayoutStyle.Absolute;
			// empty frame
			((FakeDriver)Application.Driver).SetBufferSize (0, 0);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (Rect.Empty, Application.Top.Frame);
			Assert.Equal (0, top.X);
			Assert.Equal (0, top.Y);
			Assert.Equal (1, top.Width);
			Assert.Equal (1, top.Height);
			Assert.Equal (new Rect (0, 0, 1, 1), top.Frame);

			top.LayoutStyle = LayoutStyle.Computed;
			// empty frame
			((FakeDriver)Application.Driver).SetBufferSize (0, 0);

			Assert.Null (Application.Top.X);
			Assert.Null (Application.Top.Y);
			Assert.Null (Application.Top.Width);
			Assert.Null (Application.Top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), Application.Top.Frame);
			Assert.Equal (Rect.Empty, Application.Top.Frame);
			Assert.Equal (0, top.X);
			Assert.Equal (0, top.Y);
			Assert.Equal (1, top.Width);
			Assert.Equal (1, top.Height);
			Assert.Equal (new Rect (0, 0, 1, 1), top.Frame);
		}

		[Fact, AutoInitShutdown]
		public void Applicaton_Top_LayoutStyle_Pos_Dim_Frame_With_A_OverlappedTop ()
		{
			var top = new Toplevel (new Rect (1, 1, 10, 10)) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				IsOverlappedContainer = true
			};

			Application.Begin (top);

			Assert.Equal (Application.Top, top);
			Assert.Equal (LayoutStyle.Absolute, top.LayoutStyle);
			Assert.True (top.IsInitialized);
			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);

			top.LayoutStyle = LayoutStyle.Computed;

			Assert.Equal (LayoutStyle.Absolute, top.LayoutStyle);

			top.X = 1;
			top.Y = 1;
			top.Width = 1;
			top.Height = 1;

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);

			top.Frame = new Rect (1, 1, 1, 1);

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);

			((FakeDriver)Application.Driver).SetBufferSize (1, 1);

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);
			Assert.Equal (new Rect (0, 0, 1, 1), top.Frame);

			// empty frame
			((FakeDriver)Application.Driver).SetBufferSize (0, 0);

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);
			Assert.Equal (Rect.Empty, top.Frame);
		}

		[Fact, AutoInitShutdown]
		public void Applicaton_Top_LayoutStyle_Pos_Dim_Frame_Handled_By_Toplevel_Variable ()
		{
			var top = Application.Top;

			Application.Begin (top);

			Assert.Equal (Application.Top, top);
			Assert.Equal (LayoutStyle.Absolute, top.LayoutStyle);
			Assert.True (top.IsInitialized);
			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);

			top.LayoutStyle = LayoutStyle.Computed;

			Assert.Equal (LayoutStyle.Absolute, top.LayoutStyle);

			top.X = 1;
			top.Y = 1;
			top.Width = 1;
			top.Height = 1;

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);

			top.Frame = new Rect (1, 1, 1, 1);

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);

			((FakeDriver)Application.Driver).SetBufferSize (1, 1);

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);
			Assert.Equal (new Rect (0, 0, 1, 1), top.Frame);

			// empty frame
			((FakeDriver)Application.Driver).SetBufferSize (0, 0);

			Assert.Null (top.X);
			Assert.Null (top.Y);
			Assert.Null (top.Width);
			Assert.Null (top.Height);
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Frame);
			Assert.Equal (Rect.Empty, top.Frame);
		}
	}
}
