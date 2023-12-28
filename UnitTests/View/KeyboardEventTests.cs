﻿using System;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests;

public class KeyboardEventTests {
	readonly ITestOutputHelper _output;

	public KeyboardEventTests (ITestOutputHelper output) => _output = output;

	[Fact]
	public void KeyPress_Handled_Cancels ()
	{
		var view = new View ();
		bool invokingKeyBindingsInvoked = false;
		bool processKeyPressInvoked = false;
		bool setHandledTo = false;

		view.KeyDown += (s, e) => {
			e.Handled = setHandledTo;
			Assert.Equal (setHandledTo, e.Handled);
			Assert.Equal (KeyCode.N, e.KeyCode);
		};

		view.InvokingKeyBindings += (s, e) => {
			invokingKeyBindingsInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (KeyCode.N, e.KeyCode);
		};

		view.ProcessKeyDown += (s, e) => {
			processKeyPressInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (KeyCode.N, e.KeyCode);
		};

		view.NewKeyDownEvent (new Key (KeyCode.N));
		Assert.True (invokingKeyBindingsInvoked);
		Assert.True (processKeyPressInvoked);

		invokingKeyBindingsInvoked = false;
		processKeyPressInvoked = false;
		setHandledTo = true;
		view.NewKeyDownEvent (new Key (KeyCode.N));
		Assert.False (invokingKeyBindingsInvoked);
		Assert.False (processKeyPressInvoked);
	}

	[Fact]
	public void InvokingKeyBindings_Handled_Cancels ()
	{
		var view = new View ();
		bool keyPressInvoked = false;
		bool invokingKeyBindingsInvoked = false;
		bool processKeyPressInvoked = false;
		bool setHandledTo = false;

		view.KeyDown += (s, e) => {
			keyPressInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (KeyCode.N, e.KeyCode);
		};

		view.InvokingKeyBindings += (s, e) => {
			invokingKeyBindingsInvoked = true;
			e.Handled = setHandledTo;
			Assert.Equal (setHandledTo, e.Handled);
			Assert.Equal (KeyCode.N, e.KeyCode);
		};

		view.ProcessKeyDown += (s, e) => {
			processKeyPressInvoked = true;
			processKeyPressInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (KeyCode.N, e.KeyCode);
		};

		view.NewKeyDownEvent (new Key (KeyCode.N));
		Assert.True (keyPressInvoked);
		Assert.True (invokingKeyBindingsInvoked);
		Assert.True (processKeyPressInvoked);

		keyPressInvoked = false;
		invokingKeyBindingsInvoked = false;
		processKeyPressInvoked = false;
		setHandledTo = true;
		view.NewKeyDownEvent (new Key (KeyCode.N));
		Assert.True (keyPressInvoked);
		Assert.True (invokingKeyBindingsInvoked);
		Assert.False (processKeyPressInvoked);
	}

	[Theory]
	[InlineData (null, null)]
	[InlineData (true, true)]
	[InlineData (false, false)]
	public void OnInvokingKeyBindings_Returns_Nullable_Properly (bool? toReturn, bool? expected)
	{
		var view = new KeyBindingsTestView ();
		view.CommandReturns = toReturn;

		bool? result = view.OnInvokingKeyBindings (new Key (KeyCode.A));
		Assert.Equal (expected, result);
	}

	/// <summary>
	/// A view that overrides the OnKey* methods so we can test that they are called. 
	/// </summary>
	public class KeyBindingsTestView : View {
		public bool? CommandReturns { get; set; }

		public KeyBindingsTestView ()
		{
			CanFocus = true;
			AddCommand (Command.Default, () => CommandReturns);
			KeyBindings.Add (KeyCode.A, Command.Default);
		}
	}

	[Fact]
	public void KeyDown_Handled_True_Stops_Processing ()
	{
		bool keyDown = false;
		bool invokingKeyBindings = false;
		bool keyPressed = false;

		var view = new OnKeyTestView ();
		Assert.True (view.CanFocus);
		view.CancelVirtualMethods = false;

		view.KeyDown += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyDown);
			Assert.False (view.OnKeyDownContinued);
			e.Handled = true;
			keyDown = true;
		};
		view.InvokingKeyBindings += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyPressed);
			Assert.False (view.OnInvokingKeyBindingsContinued);
			e.Handled = true;
			invokingKeyBindings = true;
		};
		view.ProcessKeyDown += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyPressed);
			Assert.False (view.OnKeyPressedContinued);
			e.Handled = true;
			keyPressed = true;
		};

		view.NewKeyDownEvent (new Key (KeyCode.A));
		Assert.True (keyDown);
		Assert.False (invokingKeyBindings);
		Assert.False (keyPressed);

		Assert.False (view.OnKeyDownContinued);
		Assert.False (view.OnInvokingKeyBindingsContinued);
		Assert.False (view.OnKeyPressedContinued);
	}

	[Fact]
	public void InvokingKeyBindings_Handled_True_Stops_Processing ()
	{
		bool keyDown = false;
		bool invokingKeyBindings = false;
		bool keyPressed = false;

		var view = new OnKeyTestView ();
		Assert.True (view.CanFocus);
		view.CancelVirtualMethods = false;

		view.KeyDown += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyDown);
			Assert.False (view.OnKeyDownContinued);
			e.Handled = false;
			keyDown = true;
		};
		view.InvokingKeyBindings += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyPressed);
			Assert.False (view.OnInvokingKeyBindingsContinued);
			e.Handled = true;
			invokingKeyBindings = true;
		};
		view.ProcessKeyDown += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyPressed);
			Assert.False (view.OnKeyPressedContinued);
			e.Handled = true;
			keyPressed = true;
		};

		view.NewKeyDownEvent (new Key (KeyCode.A));
		Assert.True (keyDown);
		Assert.True (invokingKeyBindings);
		Assert.False (keyPressed);

		Assert.True (view.OnKeyDownContinued);
		Assert.False (view.OnInvokingKeyBindingsContinued);
		Assert.False (view.OnKeyPressedContinued);
	}


	[Fact]
	public void KeyPressed_Handled_True_Stops_Processing ()
	{
		bool keyDown = false;
		bool invokingKeyBindings = false;
		bool keyPressed = false;

		var view = new OnKeyTestView ();
		Assert.True (view.CanFocus);
		view.CancelVirtualMethods = false;

		view.KeyDown += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyDown);
			Assert.False (view.OnKeyDownContinued);
			e.Handled = false;
			keyDown = true;
		};
		view.InvokingKeyBindings += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyPressed);
			Assert.False (view.OnInvokingKeyBindingsContinued);
			e.Handled = false;
			invokingKeyBindings = true;
		};
		view.ProcessKeyDown += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyPressed);
			Assert.False (view.OnKeyPressedContinued);
			e.Handled = true;
			keyPressed = true;
		};

		view.NewKeyDownEvent (new Key (KeyCode.A));
		Assert.True (keyDown);
		Assert.True (invokingKeyBindings);
		Assert.True (keyPressed);

		Assert.True (view.OnKeyDownContinued);
		Assert.True (view.OnInvokingKeyBindingsContinued);
		Assert.False (view.OnKeyPressedContinued);
	}


	[Fact]
	public void KeyUp_Handled_True_Stops_Processing ()
	{
		bool keyUp = false;

		var view = new OnKeyTestView ();
		Assert.True (view.CanFocus);
		view.CancelVirtualMethods = false;

		view.KeyUp += (s, e) => {
			Assert.Equal (KeyCode.A, e.KeyCode);
			Assert.False (keyUp);
			Assert.False (view.OnKeyPressedContinued);
			e.Handled = true;
			keyUp = true;
		};

		view.NewKeyUpEvent (new Key (KeyCode.A));
		Assert.True (keyUp);

		Assert.False (view.OnKeyUpContinued);
		Assert.False (view.OnKeyDownContinued);
		Assert.False (view.OnInvokingKeyBindingsContinued);
		Assert.False (view.OnKeyPressedContinued);
	}

	/// <summary>
	/// A view that overrides the OnKey* methods so we can test that they are called. 
	/// </summary>
	public class OnKeyTestView : View {
		public bool CancelVirtualMethods { set; private get; }

		public OnKeyTestView () => CanFocus = true;

		public override string Text { get; set; }

		public bool OnKeyDownContinued { get; set; }

		public bool OnInvokingKeyBindingsContinued { get; set; }

		public bool OnKeyPressedContinued { get; set; }

		public bool OnKeyUpContinued { get; set; }

		public override bool OnKeyDown (Key keyEvent)
		{
			if (base.OnKeyDown (keyEvent)) {
				return true;
			}

			OnKeyDownContinued = true;
			return CancelVirtualMethods;
		}

		public override bool? OnInvokingKeyBindings (Key keyEvent)
		{
			bool? handled = base.OnInvokingKeyBindings (keyEvent);
			if (handled != null && (bool)handled) {
				return true;
			}

			OnInvokingKeyBindingsContinued = true;
			return CancelVirtualMethods;
		}

		public override bool OnProcessKeyDown (Key keyEvent)
		{
			if (base.OnProcessKeyDown (keyEvent)) {
				return true;
			}

			OnKeyPressedContinued = true;
			return CancelVirtualMethods;
		}

		public override bool OnKeyUp (Key keyEvent)
		{
			if (base.OnKeyUp (keyEvent)) {
				return true;
			}

			OnKeyUpContinued = true;
			return CancelVirtualMethods;
		}
	}

	[Theory]
	[InlineData (true, false, false)]
	[InlineData (true, true, false)]
	[InlineData (true, true, true)]
	public void Events_Are_Called_With_Only_Key_Modifiers (bool shift, bool alt, bool control)
	{
		bool keyDown = false;
		bool keyPressed = false;
		bool keyUp = false;

		var view = new OnKeyTestView ();
		view.CancelVirtualMethods = false;

		view.KeyDown += (s, e) => {
			Assert.Equal (KeyCode.Null, e.KeyCode & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask);
			Assert.Equal (shift, e.IsShift);
			Assert.Equal (alt, e.IsAlt);
			Assert.Equal (control, e.IsCtrl);
			Assert.False (keyDown);
			Assert.False (view.OnKeyDownContinued);
			keyDown = true;
		};
		view.ProcessKeyDown += (s, e) => {
			keyPressed = true;
		};
		view.KeyUp += (s, e) => {
			Assert.Equal (KeyCode.Null, e.KeyCode & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask);
			Assert.Equal (shift, e.IsShift);
			Assert.Equal (alt, e.IsAlt);
			Assert.Equal (control, e.IsCtrl);
			Assert.False (keyUp);
			Assert.False (view.OnKeyUpContinued);
			keyUp = true;
		};

		//view.ProcessKeyDownEvent (new (Key.Null | (shift ? Key.ShiftMask : 0) | (alt ? Key.AltMask : 0) | (control ? Key.CtrlMask : 0)));
		//Assert.True (keyDown);
		//Assert.True (view.OnKeyDownWasCalled);
		//Assert.True (view.OnProcessKeyDownWasCalled);

		view.NewKeyDownEvent (new Key (KeyCode.Null | (shift ? KeyCode.ShiftMask : 0) | (alt ? KeyCode.AltMask : 0) | (control ? KeyCode.CtrlMask : 0)));
		Assert.True (keyPressed);
		Assert.True (view.OnKeyDownContinued);
		Assert.True (view.OnKeyPressedContinued);

		view.NewKeyUpEvent (new Key (KeyCode.Null | (shift ? KeyCode.ShiftMask : 0) | (alt ? KeyCode.AltMask : 0) | (control ? KeyCode.CtrlMask : 0)));
		Assert.True (keyUp);
		Assert.True (view.OnKeyUpContinued);
	}

	/// <summary>
	/// This tests that when a new key down event is sent to the view
	/// the view will fire the 3 key-down related events: KeyDown, InvokingKeyBindings, and ProcessKeyDown.
	/// Note that KeyUp is independent.
	/// </summary>
	[Fact]
	public void AllViews_KeyDown_All_EventsFire ()
	{
		foreach (var view in TestHelpers.GetAllViews ()) {
			if (view == null) {
				_output.WriteLine ($"ERROR: null view from {nameof (TestHelpers.GetAllViews)}");
				continue;
			}
			_output.WriteLine ($"Testing {view.GetType ().Name}");

			bool keyDown = false;
			view.KeyDown += (s, a) => {
				a.Handled = false; // don't handle it so the other events are called
				keyDown = true;
			};

			bool invokingKeyBindings = false;
			view.InvokingKeyBindings += (s, a) => {
				a.Handled = false; // don't handle it so the other events are called
				invokingKeyBindings = true;
			};

			bool keyDownProcessed = false;
			view.ProcessKeyDown += (s, a) => {
				a.Handled = true;
				keyDownProcessed = true;
			};

			Assert.True (view.NewKeyDownEvent (Key.A)); // this will be true because the ProcessKeyDown event handled it
			Assert.True (keyDown);
			Assert.True (invokingKeyBindings);
			Assert.True (keyDownProcessed);
			view.Dispose ();
		}
	}

	/// <summary>
	/// This tests that when a new key up event is sent to the view
	/// the view will fire the 1 key-up related event: KeyUp
	/// </summary>
	[Fact]
	public void AllViews_KeyUp_All_EventsFire ()
	{
		foreach (var view in TestHelpers.GetAllViews ()) {
			if (view == null) {
				_output.WriteLine ($"ERROR: null view from {nameof (TestHelpers.GetAllViews)}");
				continue;
			}
			_output.WriteLine ($"Testing {view.GetType ().Name}");

			bool keyUp = false;
			view.KeyUp += (s, a) => {
				a.Handled = true;  
				keyUp = true;
			};

			Assert.True (view.NewKeyUpEvent (Key.A)); // this will be true because the KeyUp event handled it
			Assert.True (keyUp);
			view.Dispose ();
		}

	}
}