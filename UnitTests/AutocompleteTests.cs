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

			var tv = new TextView ();
			tv.InsertText ("co");

			ac.GenerateSuggestions (
				tv,
				new [] { "fish","const","Cobble"});

			Assert.Equal (2, ac.Suggestions.Length);
			Assert.Equal ("const", ac.Suggestions[0]);
			Assert.Equal ("Cobble", ac.Suggestions[1]);

		}
	}
}
