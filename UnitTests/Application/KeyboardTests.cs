using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

public class KeyboardTests {
	readonly ITestOutputHelper _output;

	public KeyboardTests (ITestOutputHelper output)
	{
		this._output = output;
#if DEBUG_IDISPOSABLE
		Responder.Instances.Clear ();
		RunState.Instances.Clear ();
#endif
	}
	
	[Fact]
	public void KeyUp_Event ()
	{
		Application.Init (new FakeDriver ());

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
		Application.Top.KeyUp += (object sender, Key args) => {
			if (args.KeyCode != (Key.Q.WithCtrl)) {
				output += args.AsRune;
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
		Application.Init (new FakeDriver ());

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
			top.NewKeyDownEvent (Key.Tab.WithCtrl);
			Assert.True (v2.HasFocus);
			top.NewKeyDownEvent (Key.Tab.WithCtrl);
			Assert.True (v3.HasFocus);
			top.NewKeyDownEvent (Key.Tab.WithCtrl);
			Assert.True (v4.HasFocus);
			top.NewKeyDownEvent (Key.Tab.WithCtrl);
			Assert.True (v1.HasFocus);

			top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
			Assert.True (v4.HasFocus);
			top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
			Assert.True (v3.HasFocus);
			top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
			Assert.True (v2.HasFocus);
			top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
			Assert.True (v1.HasFocus);

			top.NewKeyDownEvent (Key.PageDown.WithCtrl);
			Assert.True (v2.HasFocus);
			top.NewKeyDownEvent (Key.PageDown.WithCtrl);
			Assert.True (v3.HasFocus);
			top.NewKeyDownEvent (Key.PageDown.WithCtrl);
			Assert.True (v4.HasFocus);
			top.NewKeyDownEvent (Key.PageDown.WithCtrl);
			Assert.True (v1.HasFocus);

			top.NewKeyDownEvent (Key.PageUp.WithCtrl);
			Assert.True (v4.HasFocus);
			top.NewKeyDownEvent (Key.PageUp.WithCtrl);
			Assert.True (v3.HasFocus);
			top.NewKeyDownEvent (Key.PageUp.WithCtrl);
			Assert.True (v2.HasFocus);
			top.NewKeyDownEvent (Key.PageUp.WithCtrl);
			Assert.True (v1.HasFocus);

			// Using another's alternate keys.
			Application.AlternateForwardKey = Key.F7;
			Application.AlternateBackwardKey = Key.F6;

			top.NewKeyDownEvent (Key.F7);
			Assert.True (v2.HasFocus);
			top.NewKeyDownEvent (Key.F7);
			Assert.True (v3.HasFocus);
			top.NewKeyDownEvent (Key.F7);
			Assert.True (v4.HasFocus);
			top.NewKeyDownEvent (Key.F7);
			Assert.True (v1.HasFocus);

			top.NewKeyDownEvent (Key.F6);
			Assert.True (v4.HasFocus);
			top.NewKeyDownEvent (Key.F6);
			Assert.True (v3.HasFocus);
			top.NewKeyDownEvent (Key.F6);
			Assert.True (v2.HasFocus);
			top.NewKeyDownEvent (Key.F6);
			Assert.True (v1.HasFocus);

			Application.RequestStop ();
		};

		Application.Run (top);

		// Replacing the defaults keys to avoid errors on others unit tests that are using it.
		Application.AlternateForwardKey = Key.PageDown.WithCtrl;
		Application.AlternateBackwardKey = Key.PageUp.WithCtrl;
		Application.QuitKey = Key.Q.WithCtrl;

		Assert.Equal (Key.PageDown.WithCtrl, Application.AlternateForwardKey.KeyCode);
		Assert.Equal (Key.PageUp.WithCtrl, Application.AlternateBackwardKey.KeyCode);
		Assert.Equal (Key.Q.WithCtrl, Application.QuitKey.KeyCode);

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

		Assert.Equal (Key.Q.WithCtrl, Application.QuitKey.KeyCode);
		Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
		Assert.True (isQuiting);

		isQuiting = false;
		Application.OnKeyDown(new Key ( Key.Q.WithCtrl));
		Assert.True (isQuiting);

		isQuiting = false;
		Application.QuitKey = Key.C.WithCtrl;
		Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
		Assert.False (isQuiting);
		Application.OnKeyDown (new Key (Key.Q.WithCtrl));
		Assert.False (isQuiting);

		Application.OnKeyDown (Application.QuitKey);
		Assert.True (isQuiting);

		// Reset the QuitKey to avoid throws errors on another tests
		Application.QuitKey = Key.Q.WithCtrl;
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

		top.NewKeyDownEvent (Key.Tab.WithCtrl);
		Assert.True (win.CanFocus);
		Assert.False (win.HasFocus);
		Assert.True (win2.CanFocus);
		Assert.True (win2.HasFocus);
		Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

		top.NewKeyDownEvent (Key.Tab.WithCtrl);
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

		top.NewKeyDownEvent (Key.Tab.WithCtrl);
		Assert.True (win2.CanFocus);
		Assert.False (win.HasFocus);
		Assert.True (win2.CanFocus);
		Assert.True (win2.HasFocus);
		Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

		top.NewKeyDownEvent (Key.Tab.WithCtrl);
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

	// test Application key Bindings
	public class ScopedKeyBindingView : View {
		public bool ApplicationCommand { get; set; }
		public bool HotKeyCommand { get; set; }
		public bool FocusedCommand { get; set; }

		public ScopedKeyBindingView ()
		{
			AddCommand (Command.Refresh, () => ApplicationCommand = true);
			AddCommand (Command.Default, () => HotKeyCommand = true);
			AddCommand (Command.Left,    () => FocusedCommand = true);

			KeyBindings.Add (Key.A, KeyBindingScope.Application, Command.Refresh);
			HotKey = Key.H;
			KeyBindings.Add (Key.F, KeyBindingScope.Focused, Command.Left);
		
		}
	}

	[Fact]
	[AutoInitShutdown]
	public void Application_Scope_ScopedKeyBindings ()
	{
		var top = Application.Top;
		var view = new ScopedKeyBindingView ();
		top.Add (view);
		Application.Begin (top);

		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);

		Application.OnKeyDown (Key.A);
		Assert.True (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);
		view.ApplicationCommand = false;
		view.HotKeyCommand = false;
		view.FocusedCommand = false; 

		Application.OnKeyDown (Key.H);
		Assert.False (view.ApplicationCommand);
		Assert.True (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);
		view.ApplicationCommand = false;
		view.HotKeyCommand = false;
		view.FocusedCommand = false;

		view.CanFocus = true;
		view.SetFocus ();
		Application.OnKeyDown (Key.F);
		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.True (view.FocusedCommand);
	}

	[Fact]
	[AutoInitShutdown]
	public void Application_Scope_KeyBinding ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;
		
		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Application.OnKeyDown (Key.A);
		Assert.True (invoked);
		Assert.True (view.ApplicationCommand);

		invoked = false;
		view.ApplicationCommand = false;
		view.KeyBindings.Remove (Key.A);
		Application.OnKeyDown (Key.A); // old
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		view.KeyBindings.Add (Key.A.WithCtrl, KeyBindingScope.Application, Command.Refresh);
		Application.OnKeyDown (Key.A); // old
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Application.OnKeyDown (Key.A.WithCtrl); // new
		Assert.True (invoked);
		Assert.True (view.ApplicationCommand);

		invoked = false;
		Application.OnKeyDown (Key.H);
		Assert.True (invoked);

		invoked = false;
		Assert.False (view.HasFocus);
		Application.OnKeyDown (Key.F);
		Assert.False (invoked);

		Assert.True (view.ApplicationCommand);
		Assert.True (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);
	}

	// Same as Application_Scope_KeyBinding, but tests KeyBindingScope.Application with nested views
	[Fact]
	[AutoInitShutdown]
	public void Application_Scope_KeyBinding_Nested ()
	{
		var viewWithAppScopeBinding = new ScopedKeyBindingView ();
		var invoked = false;
		viewWithAppScopeBinding.InvokingKeyBindings += (s, e) => invoked = true;

		var view2 = new View ();
		view2.Add (viewWithAppScopeBinding);
		Application.Top.Add (view2);
		Application.Begin (Application.Top);

		Application.OnKeyDown (Key.A);
		Assert.True (invoked);
		Assert.True (viewWithAppScopeBinding.ApplicationCommand);

		invoked = false;
		viewWithAppScopeBinding.ApplicationCommand = false;
		viewWithAppScopeBinding.KeyBindings.Remove (Key.A);
		Application.OnKeyDown (Key.A); // old
		Assert.False (invoked);
		Assert.False (viewWithAppScopeBinding.ApplicationCommand);
		viewWithAppScopeBinding.KeyBindings.Add (Key.A.WithCtrl, KeyBindingScope.Application, Command.Refresh);
		Application.OnKeyDown (Key.A); // old
		Assert.False (invoked);
		Assert.False (viewWithAppScopeBinding.ApplicationCommand);
		Application.OnKeyDown (Key.A.WithCtrl); // new
		Assert.True (invoked);
		Assert.True (viewWithAppScopeBinding.ApplicationCommand);

		invoked = false;
		Assert.False (view2.HasFocus);
		Assert.False (viewWithAppScopeBinding.HasFocus);
		Application.OnKeyDown (Key.H);
		Assert.False (invoked); // neither viewWithAppScopeBinding nor view2 have a Focus
		
		view2.CanFocus = true;
		view2.SetFocus ();
		Application.OnKeyDown (Key.H);
		Assert.True (invoked); // neither viewWithAppScopeBinding nor view2 have a Focus

		invoked = false;
		Assert.False (viewWithAppScopeBinding.HasFocus);
		Application.OnKeyDown (Key.F);
		Assert.False (invoked);

		Assert.True (viewWithAppScopeBinding.ApplicationCommand);
		Assert.True (viewWithAppScopeBinding.HotKeyCommand);
		Assert.False (viewWithAppScopeBinding.FocusedCommand);
	}


	[Fact]
	[AutoInitShutdown]
	public void Application_Scope_KeyBinding_Negative ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Application.OnKeyDown (Key.A.WithCtrl);
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);

		invoked = false;
		Assert.False (view.HasFocus);
		Application.OnKeyDown (Key.Z);
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);
	}

	[Fact]
	[AutoInitShutdown]
	public void Application_Scope_View_Command_Invoked_If_Application_Has_No_Binding ()
	{
		var view = new ScopedKeyBindingView ();

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		var drawContentInvoked = false;
		Application.Top.DrawContent += (s, e) => drawContentInvoked = true;

		Application.OnKeyDown (Key.F5);
		Assert.True (drawContentInvoked);
		drawContentInvoked = false;

		// Remove the key binding for Refresh from the top level
		Application.Top.KeyBindings.Remove (Key.F5);
		Application.OnKeyDown (Key.F5);
		Assert.False (drawContentInvoked);
		drawContentInvoked = false;
		
		// The Command is still valid, even if the binding is gone
		// So this should work.
		Application.OnKeyDown (Key.A);
		Assert.True (view.ApplicationCommand);
		Assert.False (drawContentInvoked); // not invoked because the view ate it
		drawContentInvoked = false;
	}

	[Fact]
	[AutoInitShutdown]
	public void Application_Scope_View_Command_Invoked_If_Application_Has_Binding ()
	{
		var view = new ScopedKeyBindingView ();

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		var drawContentInvoked = false;
		Application.Top.DrawContent += (s, e) => drawContentInvoked = true;
		
		Application.OnKeyDown (Key.F5);
		Assert.True (drawContentInvoked);
		drawContentInvoked = false;

		Application.OnKeyDown (Key.A);
		Assert.True (view.ApplicationCommand);
		Assert.False (drawContentInvoked); // not invoked because the view ate it
		drawContentInvoked = false;
	}
}