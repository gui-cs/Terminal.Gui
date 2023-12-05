using System;
using Xunit;
using Xunit.Abstractions;
using System.Text;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;
using UICatalog.Scenarios;

namespace Terminal.Gui.ViewTests {
	public class KeyboardTests {
		readonly ITestOutputHelper _output;

		public KeyboardTests (ITestOutputHelper output)
		{
			this._output = output;
		}

		[Fact]
		public void KeyPress_Handled_To_True_Prevents_Changes ()
		{
			Application.Init (new FakeDriver ());

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('N', ConsoleKey.N, false, false, false));

			var top = Application.Top;

			var text = new TextField ("");
			text.KeyPressed += (s, e) => {
				e.Handled = true;
				Assert.True (e.Handled);
				Assert.Equal ((Key)'n', e.Key);
			};
			top.Add (text);

			Application.Iteration += (s, a) => {
				Console.MockKeyPresses.Push (new ConsoleKeyInfo ('N', ConsoleKey.N, false, false, false));
				Assert.Equal ("", text.Text);

				Application.RequestStop ();
			};

			Application.Run ();

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		
		[Fact, AutoInitShutdown]
		public void KeyDown_And_KeyUp_Events_Must_Called_Before_OnKeyDown_And_OnKeyUp ()
		{
			var keyDown = false;
			var keyPress = false;
			var keyUp = false;

			var view = new DerivedView ();
			view.KeyDown += (s, e) => {
				Assert.Equal ((Key)'a', e.Key);
				Assert.False (keyDown);
				Assert.False (view.IsKeyDown);
				e.Handled = true;
				keyDown = true;
			};
			view.KeyPressed += (s, e) => {
				Assert.Equal ((Key)'a', e.Key);
				Assert.False (keyPress);
				Assert.False (view.IsKeyPress);
				e.Handled = true;
				keyPress = true;
			};
			view.KeyUp += (s, e) => {
				Assert.Equal ((Key)'a', e.Key);
				Assert.False (keyUp);
				Assert.False (view.IsKeyUp);
				e.Handled = true;
				keyUp = true;
			};

			Application.Top.Add (view);

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false));

			Application.Iteration += (s, a) => Application.RequestStop ();

			Assert.True (view.CanFocus);

			Application.Run ();
			Application.Shutdown ();

			Assert.True (keyDown);
			Assert.True (keyPress);
			Assert.True (keyUp);
			Assert.False (view.IsKeyDown);
			Assert.False (view.IsKeyPress);
			Assert.False (view.IsKeyUp);
		}

		public class DerivedView : View {
			public DerivedView ()
			{
				CanFocus = true;
			}

			public bool IsKeyDown { get; set; }
			public bool IsKeyPress { get; set; }
			public bool IsKeyUp { get; set; }
			public override string Text { get; set; }

			public override bool OnKeyDown (KeyEventArgs keyEvent)
			{
				if (base.OnKeyDown (keyEvent)) {
					return true;
				}

				IsKeyDown = true;
				return true;
			}

			public override bool OnKeyPressed (KeyEventArgs keyEvent)
			{
				if (base.OnKeyPressed (keyEvent)) {
					return true;
				}

				IsKeyPress = true;
				return true;
			}

			public override bool OnKeyUp (KeyEventArgs keyEvent)
			{
				if (base.OnKeyUp (keyEvent)) {
					return true;
				}
				
				IsKeyUp = true;
				return true;
			}
		}

		[Theory, AutoInitShutdown]
		[InlineData (true, false, false)]
		[InlineData (true, true, false)]
		[InlineData (true, true, true)]
		public void KeyDown_And_KeyUp_Events_With_Only_Key_Modifiers (bool shift, bool alt, bool control)
		{
			var keyDown = false;
			var keyPress = false;
			var keyUp = false;

			var view = new DerivedView ();
			view.KeyDown += (s, e) => {
				Assert.Equal (Key.Null, e.Key & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask);
				Assert.Equal (shift, e.IsShift);
				Assert.Equal (alt, e.IsAlt);
				Assert.Equal (control, e.IsCtrl);
				Assert.False (keyDown);
				Assert.False (view.IsKeyDown);
				keyDown = true;
			};
			view.KeyPressed += (s, e) => {
				keyPress = true;
			};
			view.KeyUp += (s, e) => {
				Assert.Equal (Key.Null, e.Key & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask);
				Assert.Equal (shift, e.IsShift);
				Assert.Equal (alt, e.IsAlt);
				Assert.Equal (control, e.IsCtrl);
				Assert.False (keyUp);
				Assert.False (view.IsKeyUp);
				keyUp = true;
			};

			Application.Top.Add (view);

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('\0', 0, shift, alt, control));

			Application.Iteration += (s, a) => Application.RequestStop ();

			Assert.True (view.CanFocus);

			Application.Run ();
			Application.Shutdown ();
			
			Assert.True (keyDown);
			Assert.True (keyPress);
			Assert.True (keyUp);
			Assert.True (view.IsKeyDown);
			Assert.True (view.IsKeyPress);
			Assert.True (view.IsKeyUp);
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
}
