using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.Views {
	public class CheckboxTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var ckb = new CheckBox ();
			Assert.False (ckb.Checked);
			Assert.Equal (string.Empty, ckb.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (0, 0, 4, 1), ckb.Frame);

			ckb = new CheckBox ("Test", true);
			Assert.True (ckb.Checked);
			Assert.Equal ("Test", ckb.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (0, 0, 8, 1), ckb.Frame);

			ckb = new CheckBox (1, 2, "Test");
			Assert.False (ckb.Checked);
			Assert.Equal ("Test", ckb.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (1, 2, 8, 1), ckb.Frame);

			ckb = new CheckBox (3, 4, "Test", true);
			Assert.True (ckb.Checked);
			Assert.Equal ("Test", ckb.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (3, 4, 8, 1), ckb.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var isChecked = false;
			CheckBox ckb = new CheckBox ("Test");
			ckb.Toggled += (e) => isChecked = true;
			Application.Top.Add (ckb);
			Application.Begin (Application.Top);

			Assert.Equal (Key.Null, ckb.HotKey);
			Assert.False (ckb.ProcessHotKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.False (isChecked);
			ckb.Text = "_Test";
			Assert.Equal (Key.T, ckb.HotKey);
			Assert.True (ckb.ProcessHotKey (new KeyEvent (Key.T | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (isChecked);
			isChecked = false;
			Assert.True (ckb.ProcessKey (new KeyEvent ((Key)' ', new KeyModifiers ())));
			Assert.True (isChecked);
			isChecked = false;
			Assert.True (ckb.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.True (isChecked);
		}
	}
}
