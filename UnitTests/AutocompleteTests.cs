using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;

namespace UnitTests {
	public class AutocompleteTests {

		[Fact][AutoInitShutdown]
		public void Test_GenerateSuggestions_Simple()
		{
			var ac = new Autocomplete ();
			ac.AllSuggestions = new List<string> { "fish","const","Cobble"};

			var tv = new TextView ();
			tv.InsertText ("co");

			ac.GenerateSuggestions (tv);

			Assert.Equal (2, ac.Suggestions.Count);
			Assert.Equal ("const", ac.Suggestions[0]);
			Assert.Equal ("Cobble", ac.Suggestions[1]);

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

			// should be seperate instance
			Assert.NotSame (Colors.Menu, tv.Autocomplete.ColorScheme);

			// with the values we set on it
			Assert.Equal (Color.Black, tv.Autocomplete.ColorScheme.Normal.Foreground);
			Assert.Equal (Color.Blue, tv.Autocomplete.ColorScheme.Normal.Background);

			Assert.Equal (Color.Black, tv.Autocomplete.ColorScheme.Focus.Foreground);
			Assert.Equal (Color.Cyan, tv.Autocomplete.ColorScheme.Focus.Background);


		}
	}
}
