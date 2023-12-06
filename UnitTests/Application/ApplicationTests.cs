﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ApplicationTests;

public class ApplicationTests {
	readonly ITestOutputHelper _output;

	public ApplicationTests (ITestOutputHelper output)
	{
		this._output = output;
#if DEBUG_IDISPOSABLE
		Responder.Instances.Clear ();
		RunState.Instances.Clear ();
#endif
	}

	void Pre_Init_State ()
	{
		Assert.Null (Application.Driver);
		Assert.Null (Application.Top);
		Assert.Null (Application.Current);
		Assert.Null (Application.MainLoop);
	}

	void Post_Init_State ()
	{
		Assert.NotNull (Application.Driver);
		Assert.NotNull (Application.Top);
		Assert.NotNull (Application.Current);
		Assert.NotNull (Application.MainLoop);
		// FakeDriver is always 80x25
		Assert.Equal (80, Application.Driver.Cols);
		Assert.Equal (25, Application.Driver.Rows);

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
		//Pre_Init_State ();

		Application.Init (new FakeDriver ());

		// Verify post-Init state is correct
		//Post_Init_State ();

		Application.Shutdown ();

		// Verify state is back to initial
		//Pre_Init_State ();
#if DEBUG_IDISPOSABLE
			// Validate there are no outstanding Responder-based instances 
			// after a scenario was selected to run. This proves the main UI Catalog
			// 'app' closed cleanly.
			Assert.Empty (Responder.Instances);
#endif
	}

	[Fact]
	public void Init_Unbalanced_Throws ()
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
	public void Init_Null_Driver_Should_Pick_A_Driver ()
	{
		Application.Init (null);

		Assert.NotNull (Application.Driver);

		Shutdown ();
	}

	[Fact]
	public void Init_Begin_End_Cleans_Up ()
	{
		Init ();

		// Begin will cause Run() to be called, which will call Begin(). Thus will block the tests
		// if we don't stop
		Application.Iteration += (s, a) => {
			Application.RequestStop ();
		};

		RunState runstate = null;
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
		Application.Iteration += (s, a) => {
			Application.RequestStop ();
		};

		// NOTE: Run<T>, when called after Init has been called behaves differently than
		// when called if Init has not been called.
		Toplevel topLevel = null;
		Application.InternalInit (() => topLevel = new TestToplevel (), new FakeDriver ());

		RunState runstate = null;
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

		Application.Iteration += (s, a) => {
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

		Application.Init (null);
		Assert.Equal (typeof (FakeDriver), Application.Driver.GetType ());

		Application.Iteration += (s, a) => {
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

		Application.Iteration += (s, a) => {
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

		Application.Iteration += (s, a) => {
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
		Application.Iteration += (s, a) => {
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

		Application.Iteration += (s, a) => {
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

		Application.Iteration += (s, a) => {
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
		Application.Iteration += (s, a) => Application.RequestStop ();
		Application.Run ();
		Application.Shutdown ();
		Assert.Equal (3, count);
	}

	[Fact]
	public void Run_Toplevel_With_Modal_View_Does_Not_Refresh_If_Not_Dirty ()
	{
		Init ();
		var count = 0;
		Dialog d = null;
		var top = Application.Top;
		top.DrawContent += (s, a) => count++;
		var iteration = -1;
		Application.Iteration += (s, a) => {
			iteration++;
			if (iteration == 0) {
				d = new Dialog ();
				d.DrawContent += (s, a) => count++;
				Application.Run (d);
			} else if (iteration < 3) {
				Application.OnMouseEvent (new (new () { X = 0, Y = 0, Flags = MouseFlags.ReportMousePosition }));
				Assert.False (top.NeedsDisplay);
				Assert.False (top.SubViewNeedsDisplay);
				Assert.False (top.LayoutNeeded);
				Assert.False (d.NeedsDisplay);
				Assert.False (d.SubViewNeedsDisplay);
				Assert.False (d.LayoutNeeded);
			} else {
				Application.RequestStop ();
			}
		};
		Application.Run ();
		Application.Shutdown ();
		// 1 - First top load, 1 - Dialog load, 1 - Dialog unload, Total - 3.
		Assert.Equal (3, count);
	}

	[Fact]
	public void Run_A_Modal_Toplevel_Refresh_Background_On_Moving ()
	{
		Init ();
		var d = new Dialog () { Width = 5, Height = 5 };
		((FakeDriver)Application.Driver).SetBufferSize (10, 10);
		var rs = Application.Begin (d);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
  ┌───┐
  │   │
  │   │
  │   │
  └───┘", _output);

		var attributes = new Attribute [] {
			// 0
			new Attribute (ColorName.White, ColorName.Black),
			// 1
			Colors.Dialog.Normal
		};
		TestHelpers.AssertDriverColorsAre (@"
0000000000
0000000000
0011111000
0011111000
0011111000
0011111000
0011111000
0000000000
0000000000
0000000000
", null, attributes);

		// TODO: In PR #2920 this breaks because the mouse is not grabbed anymore.
		// TODO: Move the mouse grap/drag mode from Toplevel to Border.
		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () { X = 2, Y = 2, Flags = MouseFlags.Button1Pressed }));
		Assert.Equal (d, Application.MouseGrabView);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () { X = 1, Y = 1, Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition }));
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
 ┌───┐
 │   │
 │   │
 │   │
 └───┘", _output);

		attributes = new Attribute [] {
			// 0
			new Attribute (ColorName.White, ColorName.Black),
			// 1
			Colors.Dialog.Normal
		};
		TestHelpers.AssertDriverColorsAre (@"
0000000000
0111110000
0111110000
0111110000
0111110000
0111110000
0000000000
0000000000
0000000000
0000000000
", null, attributes);

		Application.End (rs);
		Application.Shutdown ();
	}

	// TODO: Add tests for Run that test errorHandler

	#endregion

	#region ShutdownTests
	[Fact]
	public async void Shutdown_Allows_Async ()
	{
		bool isCompletedSuccessfully = false;

		async Task TaskWithAsyncContinuation ()
		{
			await Task.Yield ();
			await Task.Yield ();

			isCompletedSuccessfully = true;
		}

		Init ();
		Application.Shutdown ();

		Assert.False (isCompletedSuccessfully);
		await TaskWithAsyncContinuation ();
		Thread.Sleep (100);
		Assert.True (isCompletedSuccessfully);
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
		Assert.Equal (new Rect (0, 0, 80, 25), Application.Top.Frame);
		((FakeDriver)Application.Driver).SetBufferSize (5, 5);
		Assert.Equal (new Rect (0, 0, 5, 5), Application.Top.Frame);
	}

	[Fact]
	[AutoInitShutdown]
	public void SetCurrentAsTop_Run_A_Not_Modal_Toplevel_Make_It_The_Current_Application_Top ()
	{
		var t1 = new Toplevel ();
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
			Assert.Equal (t2, Application.Top);
			Application.Run (t3);
		};
		t3.Ready += (s, e) => {
			Assert.Equal (t3, Application.Top);
			Application.Run (d);
		};
		d.Ready += (s, e) => {
			Assert.Equal (t3, Application.Top);
			Application.Run (t4);
		};
		t4.Ready += (s, e) => {
			Assert.Equal (t4, Application.Top);
			t4.RequestStop ();
			d.RequestStop ();
			t3.RequestStop ();
			t2.RequestStop ();
		};
		// Now this will close the OverlappedContainer when all OverlappedChildren was closed
		t2.Closed += (s, _) => {
			t1.RequestStop ();
		};
		Application.Iteration += (s, a) => {
			if (iterations == 5) {
				// The Current still is t4 because Current.Running is false.
				Assert.Equal (t4, Application.Current);
				Assert.False (Application.Current.Running);
				Assert.Equal (t4, Application.Top);
			} else if (iterations == 4) {
				// The Current is d and Current.Running is false.
				Assert.Equal (d, Application.Current);
				Assert.False (Application.Current.Running);
				Assert.Equal (t4, Application.Top);
			} else if (iterations == 3) {
				// The Current is t3 and Current.Running is false.
				Assert.Equal (t3, Application.Current);
				Assert.False (Application.Current.Running);
				Assert.Equal (t3, Application.Top);
			} else if (iterations == 2) {
				// The Current is t2 and Current.Running is false.
				Assert.Equal (t2, Application.Current);
				Assert.False (Application.Current.Running);
				Assert.Equal (t2, Application.Top);
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
		FakeConsole.MockKeyPresses.Push (new ConsoleKeyInfo ('Q', ConsoleKey.Q, shift: false, alt: false, control: true));
		foreach (var c in input.Reverse ()) {
			if (char.IsLetter (c)) {
				FakeConsole.MockKeyPresses.Push (new ConsoleKeyInfo (c, (ConsoleKey)char.ToUpper (c), shift: char.IsUpper (c), alt: false, control: false));
			} else {
				FakeConsole.MockKeyPresses.Push (new ConsoleKeyInfo (c, (ConsoleKey)c, shift: false, alt: false, control: false));
			}
		}

		int stackSize = FakeConsole.MockKeyPresses.Count;

		int iterations = 0;
		Application.Iteration += (s, a) => {
			iterations++;
			// Stop if we run out of control...
			if (iterations > 10) {
				Application.RequestStop ();
			}
		};

		int keyUps = 0;
		var output = string.Empty;
		Application.Top.KeyUp += (object sender, KeyEventArgs args) => {
			if (args.Key != (Key.CtrlMask | Key.Q)) {
				output += (char)args.KeyValue;
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

		Application.Iteration += (s, a) => {
			Assert.True (v1.HasFocus);
			// Using default keys.
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
			Assert.True (v2.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
			Assert.True (v3.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
			Assert.True (v4.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
			Assert.True (v1.HasFocus);

			top.ProcessKeyPressed (new (Key.ShiftMask | Key.CtrlMask | Key.Tab));
			Assert.True (v4.HasFocus);
			top.ProcessKeyPressed (new (Key.ShiftMask | Key.CtrlMask | Key.Tab));
			Assert.True (v3.HasFocus);
			top.ProcessKeyPressed (new (Key.ShiftMask | Key.CtrlMask | Key.Tab));
			Assert.True (v2.HasFocus);
			top.ProcessKeyPressed (new (Key.ShiftMask | Key.CtrlMask | Key.Tab));
			Assert.True (v1.HasFocus);

			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageDown));
			Assert.True (v2.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageDown));
			Assert.True (v3.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageDown));
			Assert.True (v4.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageDown));
			Assert.True (v1.HasFocus);

			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageUp));
			Assert.True (v4.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageUp));
			Assert.True (v3.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageUp));
			Assert.True (v2.HasFocus);
			top.ProcessKeyPressed (new (Key.CtrlMask | Key.PageUp));
			Assert.True (v1.HasFocus);

			// Using another's alternate keys.
			Application.AlternateForwardKey = Key.F7;
			Application.AlternateBackwardKey = Key.F6;

			top.ProcessKeyPressed (new (Key.F7));
			Assert.True (v2.HasFocus);
			top.ProcessKeyPressed (new (Key.F7));
			Assert.True (v3.HasFocus);
			top.ProcessKeyPressed (new (Key.F7));
			Assert.True (v4.HasFocus);
			top.ProcessKeyPressed (new (Key.F7));
			Assert.True (v1.HasFocus);

			top.ProcessKeyPressed (new (Key.F6));
			Assert.True (v4.HasFocus);
			top.ProcessKeyPressed (new (Key.F6));
			Assert.True (v3.HasFocus);
			top.ProcessKeyPressed (new (Key.F6));
			Assert.True (v2.HasFocus);
			top.ProcessKeyPressed (new (Key.F6));
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
		Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
		Assert.True (isQuiting);

		isQuiting = false;
		Application.OnKeyPressed(new KeyEventArgs( Key.Q | Key.CtrlMask));
		Assert.True (isQuiting);

		isQuiting = false;
		Application.QuitKey = Key.C | Key.CtrlMask;
		Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
		Assert.False (isQuiting);
		Application.OnKeyPressed (new KeyEventArgs (Key.Q | Key.CtrlMask));
		Assert.False (isQuiting);

		Application.OnKeyPressed (new KeyEventArgs (Application.QuitKey));
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

		top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
		Assert.True (win.CanFocus);
		Assert.False (win.HasFocus);
		Assert.True (win2.CanFocus);
		Assert.True (win2.HasFocus);
		Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

		top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
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

		top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
		Assert.True (win2.CanFocus);
		Assert.False (win.HasFocus);
		Assert.True (win2.CanFocus);
		Assert.True (win2.HasFocus);
		Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

		top.ProcessKeyPressed (new (Key.CtrlMask | Key.Tab));
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


	// Invoke Tests
	// TODO: Test with threading scenarios
	[Fact]
	public void Invoke_Adds_Idle ()
	{
		Application.Init (new FakeDriver ());
		var top = new Toplevel ();
		var rs = Application.Begin (top);
		bool firstIteration = false;

		var actionCalled = 0;
		Application.Invoke (() => { actionCalled++; });
		Application.RunIteration (ref rs, ref firstIteration);
		Assert.Equal (1, actionCalled);
		Application.Shutdown ();
	}
}