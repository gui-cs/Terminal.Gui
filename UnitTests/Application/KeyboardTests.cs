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
		Application.Top.KeyUp += (object sender, KeyEventArgs args) => {
			if (args.ConsoleDriverKey != (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Q)) {
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
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v2.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v3.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v4.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v1.HasFocus);

			top.ProcessKeyDown (new (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v4.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v3.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v2.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
			Assert.True (v1.HasFocus);

			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageDown));
			Assert.True (v2.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageDown));
			Assert.True (v3.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageDown));
			Assert.True (v4.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageDown));
			Assert.True (v1.HasFocus);

			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageUp));
			Assert.True (v4.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageUp));
			Assert.True (v3.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageUp));
			Assert.True (v2.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.PageUp));
			Assert.True (v1.HasFocus);

			// Using another's alternate keys.
			Application.AlternateForwardKey = ConsoleDriverKey.F7;
			Application.AlternateBackwardKey = ConsoleDriverKey.F6;

			top.ProcessKeyDown (new (ConsoleDriverKey.F7));
			Assert.True (v2.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.F7));
			Assert.True (v3.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.F7));
			Assert.True (v4.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.F7));
			Assert.True (v1.HasFocus);

			top.ProcessKeyDown (new (ConsoleDriverKey.F6));
			Assert.True (v4.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.F6));
			Assert.True (v3.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.F6));
			Assert.True (v2.HasFocus);
			top.ProcessKeyDown (new (ConsoleDriverKey.F6));
			Assert.True (v1.HasFocus);

			Application.RequestStop ();
		};

		Application.Run (top);

		// Replacing the defaults keys to avoid errors on others unit tests that are using it.
		Application.AlternateForwardKey = ConsoleDriverKey.PageDown | ConsoleDriverKey.CtrlMask;
		Application.AlternateBackwardKey = ConsoleDriverKey.PageUp | ConsoleDriverKey.CtrlMask;
		Application.QuitKey = ConsoleDriverKey.Q | ConsoleDriverKey.CtrlMask;

		Assert.Equal (ConsoleDriverKey.PageDown | ConsoleDriverKey.CtrlMask, Application.AlternateForwardKey);
		Assert.Equal (ConsoleDriverKey.PageUp | ConsoleDriverKey.CtrlMask, Application.AlternateBackwardKey);
		Assert.Equal (ConsoleDriverKey.Q | ConsoleDriverKey.CtrlMask, Application.QuitKey);

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

		Assert.Equal (ConsoleDriverKey.Q | ConsoleDriverKey.CtrlMask, Application.QuitKey);
		Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
		Assert.True (isQuiting);

		isQuiting = false;
		Application.OnKeyDown(new KeyEventArgs ( ConsoleDriverKey.Q | ConsoleDriverKey.CtrlMask));
		Assert.True (isQuiting);

		isQuiting = false;
		Application.QuitKey = ConsoleDriverKey.C | ConsoleDriverKey.CtrlMask;
		Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
		Assert.False (isQuiting);
		Application.OnKeyDown (new KeyEventArgs (ConsoleDriverKey.Q | ConsoleDriverKey.CtrlMask));
		Assert.False (isQuiting);

		Application.OnKeyDown (new KeyEventArgs (Application.QuitKey));
		Assert.True (isQuiting);

		// Reset the QuitKey to avoid throws errors on another tests
		Application.QuitKey = ConsoleDriverKey.Q | ConsoleDriverKey.CtrlMask;
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

		top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
		Assert.True (win.CanFocus);
		Assert.False (win.HasFocus);
		Assert.True (win2.CanFocus);
		Assert.True (win2.HasFocus);
		Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

		top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
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

		top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
		Assert.True (win2.CanFocus);
		Assert.False (win.HasFocus);
		Assert.True (win2.CanFocus);
		Assert.True (win2.HasFocus);
		Assert.Equal ("win2", ((Window)top.Subviews [top.Subviews.Count - 1]).Title);

		top.ProcessKeyDown (new (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Tab));
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
			AddCommand (Command.Save, () => ApplicationCommand = true);
			AddCommand (Command.Default, () => HotKeyCommand = true);
			AddCommand (Command.Left, () => FocusedCommand = true);

			KeyBindings.Add (ConsoleDriverKey.A, KeyBindingScope.Application, Command.Save);
			HotKey = ConsoleDriverKey.H;
			KeyBindings.Add (ConsoleDriverKey.F, KeyBindingScope.Focused, Command.Left);
		}
	}

	[Fact]
	[AutoInitShutdown]
	public void OnKeyDown_Application_KeyBinding ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;
		
		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Application.OnKeyDown (new (ConsoleDriverKey.A));
		Assert.True (invoked);
		Assert.True (view.ApplicationCommand);

		invoked = false;
		view.ApplicationCommand = false;
		view.KeyBindings.Remove (ConsoleDriverKey.A);
		Application.OnKeyDown (new (ConsoleDriverKey.A)); // old
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		view.KeyBindings.Add (ConsoleDriverKey.A | ConsoleDriverKey.CtrlMask, KeyBindingScope.Application, Command.Save);
		Application.OnKeyDown (new (ConsoleDriverKey.A)); // old
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Application.OnKeyDown (new (ConsoleDriverKey.A | ConsoleDriverKey.CtrlMask)); // new
		Assert.True (invoked);
		Assert.True (view.ApplicationCommand);

		invoked = false;
		Application.OnKeyDown (new (ConsoleDriverKey.H));
		Assert.True (invoked);

		invoked = false;
		Assert.False (view.HasFocus);
		Application.OnKeyDown (new (ConsoleDriverKey.F));
		Assert.False (invoked);

		Assert.True (view.ApplicationCommand);
		Assert.True (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);
	}

	[Fact]
	[AutoInitShutdown]
	public void OnKeyDown_Application_KeyBinding_Negative ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Application.OnKeyDown (new (ConsoleDriverKey.A | ConsoleDriverKey.CtrlMask));
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);

		invoked = false;
		Assert.False (view.HasFocus);
		Application.OnKeyDown (new (ConsoleDriverKey.Z));
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);
	}
}