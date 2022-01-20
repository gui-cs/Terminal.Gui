using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.Core {
	public class AutocompleteTests {

		[Fact]
		[AutoInitShutdown]
		public void Test_GenerateSuggestions_Simple ()
		{
			var ac = new TextViewAutocomplete ();
			ac.AllSuggestions = new List<string> { "fish", "const", "Cobble" };

			var tv = new TextView ();
			tv.InsertText ("co");

			ac.GenerateSuggestions (tv);

			Assert.Equal (2, ac.Suggestions.Count);
			Assert.Equal ("const", ac.Suggestions [0]);
			Assert.Equal ("Cobble", ac.Suggestions [1]);

		}

		[Fact]
		[AutoInitShutdown]
		public void TestSettingColorSchemeOnAutocomplete ()
		{
			var tv = new TextView ();

			// to begin with we should be using the default menu color scheme
			Assert.Same (Colors.Menu, tv.Autocomplete.ColorScheme);

			// allocate a new custom scheme
			tv.Autocomplete.ColorScheme = new ColorScheme () {
				Normal = Application.Driver.MakeAttribute (Color.Black, Color.Blue),
				Focus = Application.Driver.MakeAttribute (Color.Black, Color.Cyan),
			};

			// should be separate instance
			Assert.NotSame (Colors.Menu, tv.Autocomplete.ColorScheme);

			// with the values we set on it
			Assert.Equal (Color.Black, tv.Autocomplete.ColorScheme.Normal.Foreground);
			Assert.Equal (Color.Blue, tv.Autocomplete.ColorScheme.Normal.Background);

			Assert.Equal (Color.Black, tv.Autocomplete.ColorScheme.Focus.Foreground);
			Assert.Equal (Color.Cyan, tv.Autocomplete.ColorScheme.Focus.Background);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = " Fortunately super feature."
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.NotNull (tv.Autocomplete);
			Assert.Empty (tv.Autocomplete.AllSuggestions);
			tv.Autocomplete.AllSuggestions = Regex.Matches (tv.Text.ToString (), "\\w+")
				.Select (s => s.Value)
				.Distinct ().ToList ();
			Assert.Equal (3, tv.Autocomplete.AllSuggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.AllSuggestions [0]);
			Assert.Equal ("super", tv.Autocomplete.AllSuggestions [1]);
			Assert.Equal ("feature", tv.Autocomplete.AllSuggestions [^1]);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.F, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0]);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1]);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx]);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0]);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1]);
			Assert.Equal (1, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx]);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0]);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1]);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx]);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0]);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1]);
			Assert.Equal (1, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx]);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			tv.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0]);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1]);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx]);
			Assert.True (tv.ProcessKey (new KeyEvent (tv.Autocomplete.SelectionKey, new KeyModifiers ())));
			Assert.Equal ($"Fortunately Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0]);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1]);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx]);
			Assert.True (tv.ProcessKey (new KeyEvent (tv.Autocomplete.CloseKey, new KeyModifiers ())));
			Assert.Equal ($"Fortunately Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.Equal (3, tv.Autocomplete.AllSuggestions.Count);
		}
	}
}