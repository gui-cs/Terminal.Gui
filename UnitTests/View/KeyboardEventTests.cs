using System;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests;
public class KeyboardEventTests {
	readonly ITestOutputHelper _output;

	public KeyboardEventTests (ITestOutputHelper output)
	{
		this._output = output;
	}

	[Fact]
	public void KeyPress_Handled_Cancels ()
	{
		var view = new View ();
		bool invokingKeyBindingsInvoked = false;
		bool processKeyPressInvoked = false;
		bool setHandledTo = false;

		view.KeyPress += (s, e) => {
			e.Handled = setHandledTo;
			Assert.Equal (setHandledTo, e.Handled);
			Assert.Equal (Key.N, e.Key);
		};

		view.InvokingKeyBindings += (s, e) => {
			invokingKeyBindingsInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (Key.N, e.Key);
		};

		view.ProcessKeyPress += (s, e) => {
			processKeyPressInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (Key.N, e.Key);
		};

		view.ProcessKeyPressEvent (new (Key.N));
		Assert.True (invokingKeyBindingsInvoked);
		Assert.True (processKeyPressInvoked);

		invokingKeyBindingsInvoked = false;
		processKeyPressInvoked = false;
		setHandledTo = true;
		view.ProcessKeyPressEvent (new (Key.N));
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

		view.KeyPress += (s, e) => {
			keyPressInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (Key.N, e.Key);
		};

		view.InvokingKeyBindings += (s, e) => {
			invokingKeyBindingsInvoked = true;
			e.Handled = setHandledTo;
			Assert.Equal (setHandledTo, e.Handled);
			Assert.Equal (Key.N, e.Key);
		};

		view.ProcessKeyPress += (s, e) => {
			processKeyPressInvoked = true;
			processKeyPressInvoked = true;
			Assert.False (e.Handled);
			Assert.Equal (Key.N, e.Key);
		};

		view.ProcessKeyPressEvent (new (Key.N));
		Assert.True (keyPressInvoked);
		Assert.True (invokingKeyBindingsInvoked);
		Assert.True (processKeyPressInvoked);

		keyPressInvoked = false;
		invokingKeyBindingsInvoked = false;
		processKeyPressInvoked = false;
		setHandledTo = true;
		view.ProcessKeyPressEvent (new (Key.N));
		Assert.True (keyPressInvoked);
		Assert.True (invokingKeyBindingsInvoked);
		Assert.False (processKeyPressInvoked);
	}

	[Theory]
	[InlineData (null, null)]
	[InlineData (true, true)]
	[InlineData (false, false)]
	public void InvokeKeyBindings_Returns_Nullable_Properly (bool? toReturn, bool? expected)
	{
		var view = new InvokeKeyBindingsTestView ();
		view.CommandReturns = toReturn;

		var result = view.OnInvokeKeyBindings (new (Key.A));
		Assert.Equal (expected, result);
	}

	/// <summary>
	/// A view that overrides the OnKey* methods so we can test that they are called. 
	/// </summary>
	public class InvokeKeyBindingsTestView: View {
		public bool? CommandReturns { get; set; }
		public InvokeKeyBindingsTestView ()
		{
			CanFocus = true;
			AddCommand (Command.Default, () => CommandReturns);
			AddKeyBinding (Key.A, Command.Default);
		}
	}

	[Fact]
	public void KeyPress_Handled_To_True_Stops_Processing ()
	{
		var view = new View ();
		view.KeyPress += (s, e) => {
			e.Handled = true;
			Assert.True (e.Handled);
			Assert.Equal (Key.N, e.Key);
		};

		bool processKeyPressInvoked = false;
		view.ProcessKeyPress += (s, e) => {
			processKeyPressInvoked = true;
		};

		Application.OnKeyPress (new (Key.N));
		Assert.False (processKeyPressInvoked);
	}


	[Fact]
	public void OnKey_Events_Fire_Before_And_Cancel_VirtualMethods ()
	{
		var keyDown = false;
		var keyPress = false;
		var keyUp = false;

		var view = new OnKeyTestView ();
		Assert.True (view.CanFocus);
		view.CancelVirtualMethods = false;

		view.KeyDown += (s, e) => {
			Assert.Equal (Key.A, e.Key);
			Assert.False (keyDown);
			Assert.False (view.OnKeyDownWasCalled);
			e.Handled = true;
			keyDown = true;
		};
		view.KeyPress += (s, e) => {
			Assert.Equal (Key.A, e.Key);
			Assert.False (keyPress);
			Assert.False (view.OnKeyPressWasCalled);
			e.Handled = true;
			keyPress = true;
		};
		view.KeyUp += (s, e) => {
			Assert.Equal (Key.A, e.Key);
			Assert.False (keyUp);
			Assert.False (view.OnKeyUpWasCalled);
			e.Handled = true;
			keyUp = true;
		};

		view.ProcessKeyDownEvent (new (Key.A));
		Assert.True (keyDown);

		view.ProcessKeyPressEvent (new (Key.A));
		Assert.True (keyPress);

		view.ProcessKeyUpEvent (new (Key.A));
		Assert.True (keyUp);

		Assert.False (view.OnKeyDownWasCalled);
		Assert.False (view.OnKeyPressWasCalled);
		Assert.False (view.OnKeyUpWasCalled);
	}

	[Fact]
	public void OnProcessKey_Events_Fire_Before_And_Cancel_VirtualMethods ()
	{
		var keyDown = false;
		var keyPress = false;
		var keyUp = false;

		var view = new OnKeyTestView ();
		view.CancelVirtualMethods = false;
		Assert.True (view.CanFocus);

		view.ProcessKeyDown += (s, e) => {
			Assert.Equal (Key.A, e.Key);
			Assert.False (keyDown);
			Assert.False (view.OnProcessKeyDownWasCalled);
			e.Handled = true;
			keyDown = true;
		};
		view.ProcessKeyPress += (s, e) => {
			Assert.Equal (Key.A, e.Key);
			Assert.False (keyPress);
			Assert.False (view.OnProcessKeyPressWasCalled);
			e.Handled = true;
			keyPress = true;
		};
		view.ProcessKeyUp += (s, e) => {
			Assert.Equal (Key.A, e.Key);
			Assert.False (keyUp);
			Assert.False (view.OnProcessKeyUpWasCalled);
			e.Handled = true;
			keyUp = true;
		};

		view.ProcessKeyDownEvent (new (Key.A));
		Assert.True (keyDown);

		view.ProcessKeyPressEvent (new (Key.A));
		Assert.True (keyPress);

		view.ProcessKeyUpEvent (new (Key.A));
		Assert.True (keyUp);

		Assert.False (view.OnProcessKeyDownWasCalled);
		Assert.False (view.OnProcessKeyPressWasCalled);
		Assert.False (view.OnProcessKeyUpWasCalled);
	}

	/// <summary>
	/// A view that overrides the OnKey* methods so we can test that they are called. 
	/// </summary>
	public class OnKeyTestView : View {
		public bool CancelVirtualMethods { set; private get; }
		public OnKeyTestView ()
		{
			CanFocus = true;
		}

		public override string Text { get; set; }

		public bool OnKeyDownWasCalled { get; set; }
		public bool OnKeyPressWasCalled { get; set; }
		public bool OnKeyUpWasCalled { get; set; }

		public override bool OnKeyDown (KeyEventArgs keyEvent)
		{
			if (base.OnKeyDown (keyEvent)) {
				return true;
			}

			OnKeyDownWasCalled = true;
			return CancelVirtualMethods;
		}

		public override bool OnKeyPress (KeyEventArgs keyEvent)
		{
			if (base.OnKeyPress (keyEvent)) {
				return true;
			}

			OnKeyPressWasCalled = true;
			return CancelVirtualMethods;
		}

		public override bool OnKeyUp (KeyEventArgs keyEvent)
		{
			if (base.OnKeyUp (keyEvent)) {
				return true;
			}

			OnKeyUpWasCalled = true;
			return CancelVirtualMethods;
		}

		public bool OnProcessKeyDownWasCalled { get; set; }
		public bool OnProcessKeyPressWasCalled { get; set; }
		public bool OnProcessKeyUpWasCalled { get; set; }

		public override bool OnProcessKeyDown (KeyEventArgs keyEvent)
		{
			if (base.OnProcessKeyDown (keyEvent)) {
				return true;
			}

			OnProcessKeyDownWasCalled = true;
			return CancelVirtualMethods;
		}

		public override bool OnProcessKeyPress (KeyEventArgs keyEvent)
		{
			if (base.OnProcessKeyPress (keyEvent)) {
				return true;
			}

			OnProcessKeyPressWasCalled = true;
			return CancelVirtualMethods;
		}

		public override bool OnProcessKeyUp (KeyEventArgs keyEvent)
		{
			if (base.OnProcessKeyUp (keyEvent)) {
				return true;
			}

			OnProcessKeyUpWasCalled = true;
			return CancelVirtualMethods;
		}
	}

	[Theory]
	[InlineData (true, false, false)]
	[InlineData (true, true, false)]
	[InlineData (true, true, true)]
	public void Events_Are_Called_With_Only_Key_Modifiers (bool shift, bool alt, bool control)
	{
		var keyDown = false;
		var keyPress = false;
		var keyUp = false;

		var view = new OnKeyTestView ();
		view.CancelVirtualMethods = false;

		view.KeyDown += (s, e) => {
			Assert.Equal (Key.Null, e.Key & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask);
			Assert.Equal (shift, e.IsShift);
			Assert.Equal (alt, e.IsAlt);
			Assert.Equal (control, e.IsCtrl);
			Assert.False (keyDown);
			Assert.False (view.OnKeyDownWasCalled);
			keyDown = true;
		};
		view.KeyPress += (s, e) => {
			keyPress = true;
		};
		view.KeyUp += (s, e) => {
			Assert.Equal (Key.Null, e.Key & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask);
			Assert.Equal (shift, e.IsShift);
			Assert.Equal (alt, e.IsAlt);
			Assert.Equal (control, e.IsCtrl);
			Assert.False (keyUp);
			Assert.False (view.OnKeyUpWasCalled);
			keyUp = true;
		};

		view.ProcessKeyDownEvent (new (Key.Null | (shift ? Key.ShiftMask : 0) | (alt ? Key.AltMask : 0) | (control ? Key.CtrlMask : 0)));
		Assert.True (keyDown);
		Assert.True (view.OnKeyDownWasCalled);
		Assert.True (view.OnProcessKeyDownWasCalled);

		view.ProcessKeyPressEvent (new (Key.Null | (shift ? Key.ShiftMask : 0) | (alt ? Key.AltMask : 0) | (control ? Key.CtrlMask : 0)));
		Assert.True (keyPress);
		Assert.True (view.OnKeyPressWasCalled);
		Assert.True (view.OnProcessKeyPressWasCalled);

		view.ProcessKeyUpEvent (new (Key.Null | (shift ? Key.ShiftMask : 0) | (alt ? Key.AltMask : 0) | (control ? Key.CtrlMask : 0)));
		Assert.True (keyUp);
		Assert.True (view.OnKeyUpWasCalled);
		Assert.True (view.OnProcessKeyUpWasCalled);
	}

	//[Fact]
	//public void AllViews_OnKeyPressed_CallsResponder ()
	//{
	//	foreach (var view in TestHelpers.GetAllViews ()) {
	//		if (view == null) {
	//			_output.WriteLine ($"ERROR: null view from {nameof (TestHelpers.GetAllViews)}");
	//			continue;
	//		}
	//		_output.WriteLine($"Testing {view.GetType().Name}");
	//		var keyPressed = false;
	//		view.KeyPressed += (s, a) => {
	//			a.Handled = true;
	//			keyPressed = true;
	//		};

	//		var handled = view.OnKeyPressed (new KeyEventArgs (Key.A));
	//		Assert.True (handled);
	//		Assert.True (keyPressed);
	//		view.Dispose ();
	//	}
	//}
}
