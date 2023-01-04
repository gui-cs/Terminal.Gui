using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Terminal.Gui;
using Terminal.Gui.Views;
using Xunit;
using Xunit.Abstractions;


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

			tb.Text = "c:/fish";
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

			tb.Text = @"c:/fi";
			tb.CursorPosition = tb.Text.Length;
			tb.GenerateSuggestions (null, "fish", "fishes");

			Assert.True (tb.AcceptSelectionIfAny ());
			Assert.Equal (@"c:\fish", tb.Text);
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