using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.Views {
	public class ButtonTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var btn = new Button ();
			Assert.Equal (string.Empty, btn.Text);
			Assert.Equal ("[  ]", btn.GetType ().BaseType.GetProperty ("Text").GetValue (btn).ToString ());
			Assert.False (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 4, 1), btn.Frame);
			Assert.Equal (Key.Null, btn.HotKey);

			btn = new Button ("Test", true);
			Assert.Equal ("Test", btn.Text);
			Assert.Equal ("[< Test >]", btn.GetType ().BaseType.GetProperty ("Text").GetValue (btn).ToString ());
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 10, 1), btn.Frame);
			Assert.Equal (Key.Null, btn.HotKey);

			btn = new Button (3, 4, "Test", true);
			Assert.Equal ("Test", btn.Text);
			Assert.Equal ("[< Test >]", btn.GetType ().BaseType.GetProperty ("Text").GetValue (btn).ToString ());
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (3, 4, 10, 1), btn.Frame);
			Assert.Equal (Key.Null, btn.HotKey);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var clicked = false;
			Button btn = new Button ("Test");
			btn.Clicked += () => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.False (btn.ProcessHotKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.False (clicked);
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.T | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
			clicked = false;
			Assert.False (btn.IsDefault);
			Assert.False (btn.ProcessColdKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (clicked);
			btn.IsDefault = true;
			Assert.True (btn.ProcessColdKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessColdKey (new KeyEvent (Key.AltMask | Key.T, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent ((Key)'t', new KeyModifiers ())));
			Assert.True (clicked);
		}


		[Fact]
		[AutoInitShutdown]
		public void ChangeHotKey ()
		{
			var clicked = false;
			Button btn = new Button ("Test");
			btn.Clicked += () => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.False (btn.ProcessHotKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.False (clicked);
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.T | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
			clicked = false;

			btn.HotKey = Key.E;
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.E | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
		}
	}
}
