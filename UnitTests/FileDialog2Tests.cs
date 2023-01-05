using System;
using System.Linq;
using System.Reflection;
using Xunit;


namespace Terminal.Gui.Core {
	public class FileDialog2Tests {

		[Fact, AutoInitShutdown]
		public void OnLoad_TextBoxIsFocused ()
		{
			var dlg = GetInitializedFileDialog ();
			// First focused is ContentView :(
			Assert.NotNull (dlg.Focused.Focused);
			Assert.IsType<FileDialog2.TextFieldWithAppendAutocomplete> ( dlg.Focused.Focused);
		}

		[Fact,AutoInitShutdown]
		public void Autocomplete_NoSuggestion_WhenTextMatchesExactly ()
		{
			var tb = new FileDialog2.TextFieldWithAppendAutocomplete ();
			ForceFocus (tb);

			tb.Text = "/bob/fish";
			tb.CursorPosition = tb.Text.Length;
			tb.GenerateSuggestions (null, "fish", "fishes");

			// should not report success for autocompletion because we already have that exact
			// string
			Assert.False (tb.AcceptSelectionIfAny());
		}

		



		[Fact, AutoInitShutdown]
		public void Autocomplete_AcceptSuggstion ()
		{
			var tb = new FileDialog2.TextFieldWithAppendAutocomplete ();
			ForceFocus (tb);

			tb.Text = @"/bob/fi";
			tb.CursorPosition = tb.Text.Length;
			tb.GenerateSuggestions (null, "fish", "fishes");

			Assert.True (tb.AcceptSelectionIfAny ());
			Assert.Equal (@"/bob/fish", tb.Text);
		}

		private void ForceFocus (View v)
		{
			var hasFocus = typeof (View).GetField ("hasFocus", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new Exception ("Could not find expected private member hasFocus");

			hasFocus.SetValue (v, true);
		}

		private FileDialog2.TextFieldWithAppendAutocomplete GetTextField (FileDialog2 dlg = null)
		{
			if(dlg == null) {
				dlg = GetInitializedFileDialog ();
			}

			// First view of a Dialog is ContentView
			return dlg.Subviews[0].Subviews.OfType<FileDialog2.TextFieldWithAppendAutocomplete>().Single();
		}

		private FileDialog2 GetInitializedFileDialog ()
		{
			var dlg = new FileDialog2 ();
			dlg.BeginInit ();
			dlg.EndInit ();
			Application.Begin (dlg);
			
			return dlg;
		}
	}
}