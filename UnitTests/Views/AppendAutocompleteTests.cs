using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests {
	public class AppendAutocompleteTests {
		readonly ITestOutputHelper output;

		public AppendAutocompleteTests (ITestOutputHelper output)
		{
			this.output = output;
		}

        [Fact, AutoInitShutdown]
        public void TestAutoAppend_ShowThenAccept_MatchCase()
        {
			var tf = GetTextFieldsInView();

			tf.Autocomplete = new AppendAutocomplete(tf);
			var generator = (SingleWordSuggestionGenerator)tf.Autocomplete.SuggestionGenerator;
			generator.AllSuggestions = new List<string>{"fish"};


			tf.Redraw(tf.Bounds);
			TestHelpers.AssertDriverContentsAre("",output);

			tf.ProcessKey(new KeyEvent(Key.f,new KeyModifiers()));

			tf.Redraw(tf.Bounds);
			TestHelpers.AssertDriverContentsAre("fish",output);
			Assert.Equal("f",tf.Text.ToString());

			Application.Driver.SendKeys('\t',ConsoleKey.Tab,false,false,false);

			tf.Redraw(tf.Bounds);
			TestHelpers.AssertDriverContentsAre("fish",output);
			Assert.Equal("fish",tf.Text.ToString());

			// Tab should autcomplete but not move focus
			Assert.Same(tf,Application.Top.Focused);

			// Second tab should move focus (nothing to autocomplete)
			Application.Driver.SendKeys('\t',ConsoleKey.Tab,false,false,false);
			Assert.NotSame(tf,Application.Top.Focused);
        }

		        [Fact, AutoInitShutdown]
        public void TestAutoAppend_AfterCloseKey_NoAutocomplete()
        {
			var tf = GetTextFieldsInViewSuggestingFish();

			// f is typed and suggestion is "fish"
			tf.Redraw(tf.Bounds);
			TestHelpers.AssertDriverContentsAre("fish",output);
			Assert.Equal("f",tf.Text.ToString());

			// When cancelling autocomplete
			Application.Driver.SendKeys('e',ConsoleKey.Escape,false,false,false);

			// Suggestion should disapear
			tf.Redraw(tf.Bounds);
			TestHelpers.AssertDriverContentsAre("f",output);
			Assert.Equal("f",tf.Text.ToString());

			// Still has focus though
			Assert.Same(tf,Application.Top.Focused);

			// But can tab away
			Application.Driver.SendKeys('\t',ConsoleKey.Tab,false,false,false);
			Assert.NotSame(tf,Application.Top.Focused);
        }

		private TextField GetTextFieldsInViewSuggestingFish ()
		{
			var tf = GetTextFieldsInView();
			
			tf.Autocomplete = new AppendAutocomplete(tf);
			var generator = (SingleWordSuggestionGenerator)tf.Autocomplete.SuggestionGenerator;
			generator.AllSuggestions = new List<string>{"fish"};

			tf.Redraw(tf.Bounds);
			TestHelpers.AssertDriverContentsAre("",output);

			tf.ProcessKey(new KeyEvent(Key.f,new KeyModifiers()));

			tf.Redraw(tf.Bounds);
			TestHelpers.AssertDriverContentsAre("fish",output);
			Assert.Equal("f",tf.Text.ToString());

			return tf;
		}

		private TextField GetTextFieldsInView ()
		{
            var tf = new TextField{
				Width = 10
			};
            var tf2 = new TextField{
				Y = 1,
				Width = 10
			};

			var top = Application.Top;
			top.Add (tf);
			top.Add (tf2);

			Application.Begin (top);
			
			Assert.Same(tf,top.Focused);

			return tf;
		}
	}
}