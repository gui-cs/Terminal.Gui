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
			KeyBinding binding = new KeyBinding (typeof (Dialog), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (Dialog).Name, binding.View);
			Assert.Equal ((Key)'j', binding.InKey);
			Assert.Equal (Key.CursorDown, binding.OutKey);
			Assert.Equal ("", binding.Description);
			Assert.True (binding.Enabled);
		}

		[Fact]
		public void KeyBinding_Constructors_Exceptions ()
		{
			Assert.Throws<ArgumentNullException> (() => new KeyBinding ((Type)null, (Key)'j', Key.CursorDown));
			Assert.Throws<ArgumentException> (() => new KeyBinding (typeof (Responder), (Key)'j', Key.CursorDown));
		}

		[Fact]
		public void KeyBindings_Constructors ()
		{
			KeyBindings bindings = new KeyBindings ();
			Assert.Empty (bindings.Views);
			Assert.Empty (bindings.Keys);
			Assert.True (bindings.Enabled);
			Assert.Equal (Key.Esc, bindings.EnableKey);
			Assert.Equal (Key.Enter, bindings.DisableKey);
			Assert.Equal (0, bindings.Count);

			bindings = new KeyBindings (typeof (Window), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (Window).Name, bindings.Views.Keys.ToList () [0]);
			Assert.True (bindings.Views.Values.ToList () [0]);
			Assert.Equal (typeof (Window).Name, bindings.Keys [0].View);
			Assert.True (bindings.Enabled);
			Assert.Equal (Key.Esc, bindings.EnableKey);
			Assert.Equal (Key.Enter, bindings.DisableKey);
			Assert.Equal (1, bindings.Count);

			bindings = new KeyBindings (new KeyBinding (typeof (Window), (Key)'j', Key.CursorDown));
			Assert.Equal (typeof (Window).Name, bindings.Views.Keys.ToList () [0]);
			Assert.True (bindings.Views.Values.ToList () [0]);
			Assert.Equal (typeof (Window).Name, bindings.Keys [0].View);
			Assert.True (bindings.Enabled);
			Assert.Equal (Key.Esc, bindings.EnableKey);
			Assert.Equal (Key.Enter, bindings.DisableKey);
			Assert.Equal (1, bindings.Count);
		}

		[Fact]
		public void KeyBinding_Allows_Intances ()
		{
			var view = new TextView ();
			KeyBindings bindings = new KeyBindings (view.GetType (), (Key)'j', Key.CursorDown);
			Assert.Equal (view.GetType ().Name, bindings.Views.Keys.ToList () [0]);
			Assert.True (bindings.Views.Values.ToList () [0]);
			Assert.Equal (view.GetType ().Name, bindings.Keys [0].View);
			Assert.True (bindings.Enabled);
			Assert.Equal (Key.Esc, bindings.EnableKey);
			Assert.Equal (Key.Enter, bindings.DisableKey);
			Assert.Equal (1, bindings.Count);
		}

		[Fact]
		public void Enabled_Property ()
		{
			KeyBindings bindings = new KeyBindings (typeof (Window), (Key)'j', Key.CursorDown);
			Assert.True (bindings.Enabled);

			bindings.Enabled = false;
			Assert.False (bindings.Enabled);
		}

		[Fact]
		public void AddKey_Methods ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextView).Name, bindings.Views.Keys.ToList () [0]);
			Assert.Equal (typeof (TextView).Name, bindings.Keys [0].View);

			bindings.AddKey (typeof (TextField), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextField).Name, bindings.Views.Keys.ToList () [1]);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [1].View);

			bindings.AddKey (new KeyBinding (typeof (TextField), (Key)'k', Key.CursorUp));
			Assert.Equal (typeof (TextField).Name, bindings.Views.Keys.ToList () [1]);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [2].View);

			bindings.AddKey (new KeyBinding (typeof (TextField), Key.CursorUp, (Key)'k'));
			Assert.Equal (typeof (TextField).Name, bindings.Views.Keys.ToList () [1]);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [3].View);
		}

		[Fact]
		public void AddKey_Throws_If_View_Is_Null ()
		{
			var bindings = new KeyBindings ();
			Assert.Throws<ArgumentNullException> (() => bindings.AddKey ((Type)null, (Key)'j', Key.CursorDown));
		}

		[Fact]
		public void AddKey_Throws_If_View_Is_Not_Assignable_To_Given_Type ()
		{
			var bindings = new KeyBindings ();
			Assert.Throws<ArgumentException> (() => bindings.AddKey (typeof (Responder), (Key)'j', Key.CursorDown));
		}

		[Fact]
		public void AddKey_Throws_On_Existing_Key_In_The_Given_View ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			Assert.Throws<ArgumentException> (() => bindings.AddKey (typeof (TextView), (Key)'j', Key.CursorDown));
			Assert.Throws<ArgumentException> (() => bindings.AddKey (typeof (TextView), (Key)'k', Key.CursorDown));
			Assert.Throws<ArgumentException> (() => bindings.AddKey (typeof (TextView), (Key)'j', Key.CursorUp));
		}

		[Fact]
		public void RemoveKey_Methods ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextView).Name, bindings.Views.Keys.ToList () [0]);
			Assert.Equal (typeof (TextView).Name, bindings.Keys [0].View);

			bindings.AddKey (typeof (TextField), (Key)'j', Key.CursorDown);
			Assert.Equal (typeof (TextField).Name, bindings.Views.Keys.ToList () [1]);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [1].View);

			bindings.AddKey (new KeyBinding (typeof (TextField), (Key)'k', Key.CursorUp));
			Assert.Equal (typeof (TextField).Name, bindings.Views.Keys.ToList () [1]);
			Assert.Equal (typeof (TextField).Name, bindings.Keys [2].View);

			bindings.AddKey (new KeyBinding (typeof (ListView), (Key)'l', Key.CursorRight));
			Assert.Equal (typeof (ListView).Name, bindings.Views.Keys.ToList () [2]);
			Assert.Equal (typeof (ListView).Name, bindings.Keys [3].View);
			Assert.Equal (3, bindings.Views.Count);
			Assert.Equal (4, bindings.Keys.Count);

			Assert.True (bindings.RemoveKey (typeof (TextField), (Key)'j', Key.CursorDown));
			Assert.Equal (3, bindings.Views.Count);
			Assert.Equal (3, bindings.Keys.Count);

			Assert.True (bindings.RemoveKey (new KeyBinding (typeof (TextView), (Key)'j', Key.CursorDown)));
			Assert.Equal (2, bindings.Views.Count);
			Assert.Equal (2, bindings.Keys.Count);

			Assert.True (bindings.RemoveKey (typeof (ListView), (Key)'l', Key.CursorRight));
			Assert.Single (bindings.Views);
			Assert.Single (bindings.Keys);

			Assert.False (bindings.RemoveKey (typeof (TextView), (Key)'k', Key.CursorUp));
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
			Assert.Throws<ArgumentNullException> (() => bindings.RemoveKey ((Type)null, (Key)'j', Key.CursorDown));
			Assert.Throws<ArgumentNullException> (() => bindings.RemoveKey ((string)null, (Key)'j', Key.CursorDown));
		}

		[Fact]
		public void RemoveAll_Methods ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			bindings.AddKey (typeof (TextField), (Key)'j', Key.CursorDown);
			bindings.AddKey (new KeyBinding (typeof (TextField), (Key)'k', Key.CursorUp));
			Assert.Equal (2, bindings.Views.Count);
			Assert.Equal (3, bindings.Keys.Count);

			Assert.True (bindings.RemoveAll (typeof (TextView)));
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
			Assert.Throws<ArgumentNullException> (() => bindings.RemoveAll ((Type)null));
		}

		[Fact]
		public void EnableDisableKeyBinding_Methods ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			Assert.True (bindings.Keys [0].Enabled);

			bindings.AddKey (typeof (TextField), (Key)'j', Key.CursorDown);
			Assert.True (bindings.Keys [1].Enabled);

			bindings.AddKey (typeof (ListView), (Key)'l', Key.CursorRight);
			Assert.True (bindings.Keys [2].Enabled);

			bindings.EnableDisableKeyBinding (typeof (TextView), (Key)'j', Key.CursorDown, false);
			Assert.False (bindings.Keys [0].Enabled);

			bindings.EnableDisableKeyBinding (new KeyBinding (typeof (TextField), (Key)'j', Key.CursorDown), false);
			Assert.False (bindings.Keys [1].Enabled);

			KeyBinding binding = bindings.Keys [2];
			bindings.EnableDisableKeyBinding (binding.View, binding.InKey, binding.OutKey, false);
			Assert.False (bindings.Keys [2].Enabled);
		}

		[Fact]
		public void EnableDisableKeyBinding_Throws_If_View_Is_Null ()
		{
			var bindings = new KeyBindings ();
			Assert.Throws<ArgumentNullException> (() => bindings.EnableDisableKeyBinding ((Type)null, (Key)'j', Key.CursorDown, false));
			Assert.Throws<ArgumentNullException> (() => bindings.EnableDisableKeyBinding ((string)null, (Key)'j', Key.CursorDown, false));
			Assert.Throws<ArgumentNullException> (() => bindings.EnableDisableKeyBinding (null, false));
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyPress_Event ()
		{
			var kbs = Application.KeyBindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
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
			var kbs = Application.KeyBindings = new KeyBindings (typeof (TextViewBinding), (Key)'j', Key.CursorDown);
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

		[Fact]
		public void ReplaceViewKey_Method ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			bindings.AddKey (typeof (TextField), (Key)'j', Key.CursorDown);
			bindings.AddKey (typeof (TextView), (Key)'l', Key.CursorRight);

			KeyBinding kbFrom = bindings.Keys [2];
			KeyBinding kbTo = new KeyBinding (typeof (TextView), kbFrom.InKey, Key.PageDown, "New key 1", false);
			bindings.ReplaceViewKey (kbFrom, kbTo);
			KeyBinding kb = bindings.Keys [2];
			Assert.Equal (typeof (TextView).Name, kb.View);
			Assert.Equal ((Key)'l', kb.InKey);
			Assert.Equal (Key.PageDown, kb.OutKey);
			Assert.Equal ("New key 1", kb.Description);
			Assert.False (kb.Enabled);

			kbFrom = bindings.Keys [0];
			kbTo = new KeyBinding (typeof (TextView), (Key)'i', kbFrom.OutKey, "New key 2", false);
			bindings.ReplaceViewKey (kbFrom, kbTo);
			kb = bindings.Keys [0];
			Assert.Equal (typeof (TextView).Name, kb.View);
			Assert.Equal ((Key)'i', kb.InKey);
			Assert.Equal (Key.CursorDown, kb.OutKey);
			Assert.Equal ("New key 2", kb.Description);
			Assert.False (kb.Enabled);

			kbFrom = bindings.Keys [1];
			kbTo = new KeyBinding (typeof (TextField), (Key)'i', kbFrom.OutKey, "New key 3", false);
			bindings.ReplaceViewKey (kbFrom, kbTo);
			kb = bindings.Keys [1];
			Assert.Equal (typeof (TextField).Name, kb.View);
			Assert.Equal ((Key)'i', kb.InKey);
			Assert.Equal (Key.CursorDown, kb.OutKey);
			Assert.Equal ("New key 3", kb.Description);
			Assert.False (kb.Enabled);
		}

		[Fact]
		public void ReplaceViewKey_Exceptions ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);

			KeyBinding kb = new KeyBinding (typeof (TextView), Key.J, Key.CursorLeft, "New Description", false);
			Assert.Throws<ArgumentNullException> (() => bindings.ReplaceViewKey (null, kb));
			Assert.Throws<ArgumentNullException> (() => bindings.ReplaceViewKey (kb, null));

			KeyBinding kbFrom = new KeyBinding (typeof (TextView), Key.J, Key.CursorLeft, "From Description", false);
			KeyBinding kbTo = new KeyBinding (typeof (TextField), Key.J, Key.CursorLeft, "From Description", false);
			Assert.Throws<InvalidOperationException> (() => bindings.ReplaceViewKey (kbFrom, kbTo));

			kbTo = new KeyBinding (typeof (TextView), Key.J, Key.CursorLeft, "From Description", false);
			Assert.Throws<ArgumentException> (() => bindings.ReplaceViewKey (kbFrom, kbTo));

			kbTo = new KeyBinding (typeof (TextView), Key.J, Key.CursorLeft, "To Description", false);
			Assert.Throws<ArgumentException> (() => bindings.ReplaceViewKey (kbFrom, kbTo));

			bindings.AddKey (typeof (TextView), (Key)'k', Key.CursorUp);
			kbFrom = new KeyBinding (typeof (TextView), (Key)'j', Key.CursorDown, "From Description", false);
			kbTo = new KeyBinding (typeof (TextView), Key.J, Key.CursorUp, "To Description", false);
			Assert.Throws<ArgumentException> (() => bindings.ReplaceViewKey (kbFrom, kbTo));
		}

		[Fact]
		public void ReplaceAllKeysFromView_Methods ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			bindings.AddKey (typeof (TextField), (Key)'j', Key.CursorDown);
			bindings.AddKey (typeof (TextField), (Key)'l', Key.CursorRight);
			bindings.AddKey (typeof (ListView), (Key)'k', Key.CursorRight);

			Assert.True (bindings.ReplaceAllKeysFromView (typeof (TextField), typeof (ListView), true));
			Assert.True (bindings.Views.ContainsKey (nameof (TextView)));
			Assert.False (bindings.Views.ContainsKey (nameof (TextField)));
			Assert.True (bindings.Views.ContainsKey (nameof (ListView)));
			Assert.Equal (1, bindings.Keys.Count (x => x.View == nameof (TextView)));
			Assert.Equal (0, bindings.Keys.Count (x => x.View == nameof (TextField)));
			Assert.Equal (2, bindings.Keys.Count (x => x.View == nameof (ListView)));

			Assert.True (bindings.ReplaceAllKeysFromView (nameof (ListView), nameof (TableView)));
			Assert.True (bindings.Views.ContainsKey (nameof (TextView)));
			Assert.False (bindings.Views.ContainsKey (nameof (TextField)));
			Assert.False (bindings.Views.ContainsKey (nameof (ListView)));
			Assert.True (bindings.Views.ContainsKey (nameof (TableView)));
			Assert.Equal (1, bindings.Keys.Count (x => x.View == nameof (TextView)));
			Assert.Equal (0, bindings.Keys.Count (x => x.View == nameof (TextField)));
			Assert.Equal (0, bindings.Keys.Count (x => x.View == nameof (ListView)));
			Assert.Equal (2, bindings.Keys.Count (x => x.View == nameof (TableView)));
		}

		[Fact]
		public void ReplaceAllKeysFromView_Exceptions ()
		{
			KeyBindings bindings = new KeyBindings (typeof (TextView), (Key)'j', Key.CursorDown);
			bindings.AddKey (typeof (ListView), (Key)'j', Key.CursorDown);

			Assert.Throws<ArgumentNullException> (() => bindings.ReplaceAllKeysFromView (null, nameof (TextView)));
			Assert.Throws<ArgumentNullException> (() => bindings.ReplaceAllKeysFromView (nameof (TextView), null));
			Assert.Throws<ArgumentException> (() => bindings.ReplaceAllKeysFromView (nameof (TextField), nameof (TextField)));
			Assert.Throws<ArgumentException> (() => bindings.ReplaceAllKeysFromView (nameof (TextField), nameof (TextView)));
			Assert.Throws<InvalidOperationException> (() => bindings.ReplaceAllKeysFromView (nameof (ListView), nameof (TextView)));
		}
	}
}
