using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ViewKeyBindingTests {
	readonly ITestOutputHelper _output;

	public ViewKeyBindingTests (ITestOutputHelper output)
	{
		this._output = output;
	}

	// tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)



	public class ScopedKeyBindingView : View {
		public bool ApplicationCommand { get; set; }
		public bool HotKeyCommand { get; set; }
		public bool FocusedCommand { get; set; }

		public ScopedKeyBindingView ()
		{
			AddCommand (Command.Save, () => ApplicationCommand = true);
			AddCommand (Command.Default, () => HotKeyCommand = true);
			AddCommand (Command.Left, () => FocusedCommand = true);

			KeyBindings.Add (KeyCode.A, KeyBindingScope.Application, Command.Save);
			HotKey = KeyCode.H;
			KeyBindings.Add (KeyCode.F, KeyBindingScope.Focused, Command.Left);
		}
	}

	[Fact]
	[AutoInitShutdown]
	public void Focus_KeyBinding ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Application.OnKeyDown (new (KeyCode.A));
		Assert.True (invoked);

		invoked = false;
		Application.OnKeyDown (new (KeyCode.H));
		Assert.True (invoked);

		invoked = false;
		Assert.False (view.HasFocus);
		Application.OnKeyDown (new (KeyCode.F));
		Assert.False (invoked);
		Assert.False (view.FocusedCommand);

		invoked = false;
		view.CanFocus = true;
		view.SetFocus ();
		Assert.True (view.HasFocus);
		Application.OnKeyDown (new (KeyCode.F));
		Assert.True (invoked);

		Assert.True (view.ApplicationCommand);
		Assert.True (view.HotKeyCommand);
		Assert.True (view.FocusedCommand);
	}

	[Fact]
	[AutoInitShutdown]
	public void Focus_KeyBinding_Negative ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Application.OnKeyDown (new (KeyCode.Z));
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);

		invoked = false;
		Assert.False (view.HasFocus);
		Application.OnKeyDown (new (KeyCode.F));
		Assert.False (invoked);
		Assert.False (view.ApplicationCommand);
		Assert.False (view.HotKeyCommand);
		Assert.False (view.FocusedCommand);
	}


	[Fact]
	[AutoInitShutdown]
	public void HotKey_KeyBinding ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		invoked = false;
		Application.OnKeyDown (new (KeyCode.H));
		Assert.True (invoked);
		Assert.True (view.HotKeyCommand);

		view.HotKey = KeyCode.Z;
		invoked = false;
		view.HotKeyCommand = false;
		Application.OnKeyDown (new (KeyCode.H)); // old hot key
		Assert.False (invoked);
		Assert.False (view.HotKeyCommand);

		Application.OnKeyDown (new (KeyCode.Z)); // new hot key
		Assert.True (invoked);
		Assert.True (view.HotKeyCommand);

	}

	[Fact]
	[AutoInitShutdown]
	public void HotKey_KeyBinding_Negative ()
	{
		var view = new ScopedKeyBindingView ();
		var invoked = false;
		view.InvokingKeyBindings += (s, e) => invoked = true;

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Application.OnKeyDown (new (KeyCode.Z));
		Assert.False (invoked);
		Assert.False (view.HotKeyCommand);

		invoked = false;
		Application.OnKeyDown (new (KeyCode.F));
		Assert.False (view.HotKeyCommand);
	}
}