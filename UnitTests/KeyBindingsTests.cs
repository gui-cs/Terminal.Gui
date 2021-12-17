using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace Terminal.Gui.Core {
	public class KeyBindingsTests {

		[Fact]
		public void KeyBinding_Constructors ()
		{
			KeyBinding binding = new KeyBinding (new Window (), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (Window).Name, binding.View);
			Assert.Equal ((Key)'j', binding.InKey);
			Assert.Equal (Key.CursorDown, binding.OutKey);
			Assert.Equal ("", binding.Description);
			Assert.True (binding.Enabled);
		}

		[Fact]
		public void KeyBindings_Constructors ()
		{
			KeyBindings bindings = new KeyBindings ();
			Assert.Empty (bindings.Views);
			Assert.Empty (bindings.Keys);
			Assert.False (bindings.Enabled);
			Assert.Equal (Key.Enter, bindings.EnableKey);
			Assert.Equal (Key.Esc, bindings.DisableKey);
			Assert.Equal (0, bindings.Count);

			bindings = new KeyBindings (new Window (), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (Window).Name, bindings.Views [0].View);
			Assert.True (bindings.Views [0].Enabled);
			Assert.Equal (typeof (Window).Name, bindings.Keys [0].View);
			Assert.False (bindings.Enabled);
			Assert.Equal (Key.Enter, bindings.EnableKey);
			Assert.Equal (Key.Esc, bindings.DisableKey);
			Assert.Equal (1, bindings.Count);

			bindings = new KeyBindings (new KeyBinding (new Window (), (Key)'j', Key.CursorDown));
			Assert.Equal (typeof (Window).Name, bindings.Views [0].View);
			Assert.True (bindings.Views [0].Enabled);
			Assert.Equal (typeof (Window).Name, bindings.Keys [0].View);
			Assert.False (bindings.Enabled);
			Assert.Equal (Key.Enter, bindings.EnableKey);
			Assert.Equal (Key.Esc, bindings.DisableKey);
			Assert.Equal (1, bindings.Count);
		}

		[Fact]
		public void KeyBinding_Allows_Intances ()
		{
			var view = new TextView ();
			KeyBindings bindings = new KeyBindings (view, (Key)'j', Key.CursorDown);
			Assert.Equal (view.GetType ().Name, bindings.Views [0].View);
			Assert.True (bindings.Views [0].Enabled);
			Assert.Equal (view.GetType ().Name, bindings.Keys [0].View);
			Assert.False (bindings.Enabled);
			Assert.Equal (Key.Enter, bindings.EnableKey);
			Assert.Equal (Key.Esc, bindings.DisableKey);
			Assert.Equal(1, bindings.Count);
		}

		[Fact]
		public void Enabled_Property ()
		{
			KeyBindings bindings = new KeyBindings (new Window (), (Key)'j', Key.CursorDown);
			Assert.False (bindings.Enabled);

			bindings.Enabled = true;
			Assert.True (bindings.Enabled);
		}

		[Fact]
		public void AddKey_Methods ()
		{
			KeyBindings bindings = new KeyBindings (new TextView (), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextView).Name, bindings.Views [0].View);
			Assert.Equal (typeof (TextView).Name, bindings.Keys [0].View);

			bindings.AddKey (new TextField (), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextField).Name, bindings.Views [1].View);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [1].View);

			bindings.AddKey (new KeyBinding (new TextField (), (Key)'k', Key.CursorUp));
			Assert.Equal (typeof (TextField).Name, bindings.Views [1].View);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [2].View);

			bindings.AddKey (new KeyBinding (new TextField (), Key.CursorUp, (Key)'k'));
			Assert.Equal (typeof (TextField).Name, bindings.Views [1].View);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [3].View);
		}

		[Fact]
		public void AddKey_Throws_If_View_Is_Null ()
		{
			var bindings = new KeyBindings ();
			Assert.Throws<ArgumentNullException> (() => bindings.AddKey (null, (Key)'j', Key.CursorDown));
		}

		[Fact]
		public void AddKey_Throws_On_Existing_Key_In_The_Given_View ()
		{
			KeyBindings bindings = new KeyBindings (new TextView (), (Key)'j', Key.CursorDown);
			Assert.Throws<ArgumentException> (() => bindings.AddKey (new TextView (), (Key)'j', Key.CursorDown));
			Assert.Throws<ArgumentException> (() => bindings.AddKey (new TextView (), (Key)'k', Key.CursorDown));
			Assert.Throws<ArgumentException> (() => bindings.AddKey (new TextView (), (Key)'j', Key.CursorUp));
		}

		[Fact]
		public void RemoveKey_Methods ()
		{
			KeyBindings bindings = new KeyBindings (new TextView (), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextView).Name, bindings.Views [0].View);
			Assert.Equal (typeof (TextView).Name, bindings.Keys [0].View);

			bindings.AddKey (new TextField (), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextField).Name, bindings.Views [1].View);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [1].View);

			bindings.AddKey (new KeyBinding (new TextField (), (Key)'k', Key.CursorUp));
			Assert.Equal (typeof (TextField).Name, bindings.Views [1].View);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [2].View);

			bindings.AddKey (new KeyBinding (new ListView (), (Key)'l', Key.CursorRight));
			Assert.Equal (typeof (ListView).Name, bindings.Views [2].View);
			Assert.Equal (typeof (ListView).Name, bindings.Keys [3].View);
			Assert.Equal (3, bindings.Views.Count);
			Assert.Equal (4, bindings.Keys.Count);

			Assert.True (bindings.RemoveKey (new TextField (), (Key)'j', Key.CursorDown));
			Assert.Equal (3, bindings.Views.Count);
			Assert.Equal (3, bindings.Keys.Count);

			Assert.True (bindings.RemoveKey (new KeyBinding (new TextView (), (Key)'j', Key.CursorDown)));
			Assert.Equal (2, bindings.Views.Count);
			Assert.Equal (2, bindings.Keys.Count);

			Assert.True (bindings.RemoveKey (new ListView (), (Key)'l', Key.CursorRight));
			Assert.Single (bindings.Views);
			Assert.Single (bindings.Keys);

			Assert.False (bindings.RemoveKey (new TextView (), (Key)'k', Key.CursorUp));
			Assert.Single (bindings.Views);
			Assert.Single (bindings.Keys);

			var kb = bindings.Keys [0];
			Assert.True (bindings.RemoveKey (kb.View, kb.InKey, kb.OutKey));
			Assert.Empty (bindings.Views);
			Assert.Empty (bindings.Keys);
		}

		[Fact]
		public void RemoveKey_Throws_If_View_Is_Null ()
		{
			var bindings = new KeyBindings ();
			Assert.Throws<ArgumentNullException> (() => bindings.RemoveKey ((View)null, (Key)'j', Key.CursorDown));
		}

		[Fact]
		public void RemoveAll_Methods ()
		{
			KeyBindings bindings = new KeyBindings (new TextView (), (Key)'j', Key.CursorDown);
			bindings.AddKey (new TextField (), (Key)'j', Key.CursorDown);
			bindings.AddKey (new KeyBinding (new TextField (), (Key)'k', Key.CursorUp));
			Assert.Equal (2, bindings.Views.Count);
			Assert.Equal (3, bindings.Keys.Count);

			Assert.True (bindings.RemoveAll (new TextView ()));
			Assert.Single (bindings.Views);
			Assert.Equal (2, bindings.Keys.Count);

			Assert.True (bindings.RemoveAll ());
			Assert.Empty (bindings.Views);
			Assert.Empty (bindings.Keys);

			Assert.False (bindings.RemoveAll ());
		}

		[Fact]
		public void RemoveAll_Throws_If_View_Is_Null ()
		{
			var bindings = new KeyBindings ();
			Assert.Throws<ArgumentNullException> (() => bindings.RemoveAll ((View)null));
		}

		[Fact]
		public void EnableDisableKeyBinding_Methods ()
		{
			KeyBindings bindings = new KeyBindings (new TextView (), (Key)'j', Key.CursorDown);
			Assert.True (bindings.Keys [0].Enabled);

			bindings.AddKey (new TextField (), (Key)'j', Key.CursorDown);
			Assert.True (bindings.Keys [1].Enabled);

			bindings.AddKey (new ListView (), (Key)'l', Key.CursorRight);
			Assert.True (bindings.Keys [2].Enabled);

			bindings.EnableDisableKeyBinding (new TextView (), (Key)'j', Key.CursorDown, false);
			Assert.False (bindings.Keys [0].Enabled);

			bindings.EnableDisableKeyBinding (new KeyBinding (new TextField (), (Key)'j', Key.CursorDown), false);
			Assert.False (bindings.Keys [1].Enabled);

			KeyBinding binding = bindings.Keys [2];
			bindings.EnableDisableKeyBinding (binding.View, binding.InKey, binding.OutKey, false);
			Assert.False (bindings.Keys [2].Enabled);
		}

		[Fact]
		public void EnableDisableKeyBinding_Throws_If_View_Is_Null ()
		{
			var bindings = new KeyBindings ();
			Assert.Throws<ArgumentNullException> (() => bindings.EnableDisableKeyBinding ((View)null, (Key)'j', Key.CursorDown, false));
			Assert.Throws<ArgumentNullException> (() => bindings.EnableDisableKeyBinding ((string)null, (Key)'j', Key.CursorDown, false));
			Assert.Throws<ArgumentNullException> (() => bindings.EnableDisableKeyBinding (null, false));
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyPress_Event ()
		{
			var kbs = Application.KeyBindings = new KeyBindings (new TextView (), (Key)'j', Key.CursorDown);
			kbs.Enabled = true;

			var top = Application.Top;

			var tv = new TextView () {
				Width = 10,
				Height = 5,
				Text = "Testing key bindings..."
			};
			tv.KeyPress += (e) => {
				Assert.Equal (Key.CursorDown, e.KeyEvent.Key);
				e.Handled = true;
			};
			top.Add (tv);

			var rs = Application.Begin (top);
			rs.Toplevel.Running = true;

			Application.Driver.SendKeys ('j', ConsoleKey.J, false, false, false);

		}


		[Fact]
		[AutoInitShutdown]
		public void ProcessKey_Event ()
		{
			var kbs = Application.KeyBindings = new KeyBindings (new TextViewBinding (), (Key)'j', Key.CursorDown);
			kbs.Enabled = true;

			var top = Application.Top;

			var tv = new TextViewBinding () {
				Width = 10,
				Height = 5,
				Text = "Testing key bindings..."
			};
			top.Add (tv);

			var rs = Application.Begin (top);
			rs.Toplevel.Running = true;

			Application.Driver.SendKeys ('j', ConsoleKey.J, false, false, false);

		}

		private class TextViewBinding : TextView {

			public override bool ProcessKey (KeyEvent kb)
			{
				Assert.Equal (Key.CursorDown, kb.Key);
				return true;
			}
		}

	}
}
