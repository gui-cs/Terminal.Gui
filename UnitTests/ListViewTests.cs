using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.Views {
	public class ListViewTests {
		[Fact]
		public void KeyBindings_Command () {
			List<string> source = new List<string> () { "One", "Two", "Three" };
			ListView lv = new ListView (source) { Height = 2, AllowsMarking = true };
			Assert.Equal (0, lv.SelectedItem);
			lv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			Assert.Equal (1, lv.SelectedItem);
			lv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			Assert.Equal (0, lv.SelectedItem);
			lv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ()));
			Assert.Equal (2, lv.SelectedItem);
			Assert.Equal (2, lv.TopItem);
			lv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ()));
			Assert.Equal (0, lv.SelectedItem);
			Assert.Equal (0, lv.TopItem);
			Assert.False (lv.Source.IsMarked (lv.SelectedItem));
			lv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));
			Assert.True (lv.Source.IsMarked (lv.SelectedItem));
			var opened = false;
			lv.OpenSelectedItem += (_) => opened = true;
			lv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));
			Assert.True (opened);
			lv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ()));
			Assert.Equal (2, lv.SelectedItem);
			lv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ()));
			Assert.Equal (0, lv.SelectedItem);
		}
	}
}
