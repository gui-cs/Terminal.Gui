using System;
using System.IO;
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
			Assert.IsType<FileDialog2.TextFieldWithAppendAutocomplete> (dlg.Focused.Focused);
		}

		[Fact, AutoInitShutdown]
		public void DirectTyping_Allowed ()
		{
			var dlg = GetInitializedFileDialog ();
			var tf = dlg.Subviews [0].Subviews.OfType<FileDialog2.TextFieldWithAppendAutocomplete> ().Single ();
			tf.ClearAllSelection ();
			tf.CursorPosition = tf.Text.Length;
			Assert.True (tf.HasFocus);

			SendSlash ();

			Assert.Equal (
				new DirectoryInfo (Environment.CurrentDirectory + Path.DirectorySeparatorChar).FullName,
				new DirectoryInfo (dlg.Path + Path.DirectorySeparatorChar).FullName
				);

			// continue typing the rest of the path
			Send ("bob");
			Send ('.', ConsoleKey.OemPeriod, false);
			Send ("csv");

			Assert.True (dlg.Canceled);

			Send ('\n', ConsoleKey.Enter, false);
			Assert.False (dlg.Canceled);
			Assert.Equal ("bob.csv", Path.GetFileName (dlg.Path));
		}

		private void SendSlash ()
		{
			if (Path.DirectorySeparatorChar == '/') {
				Send ('/', ConsoleKey.Separator, false);
			} else {
				Send ('\\', ConsoleKey.Separator, false);
			}
		}

		[Fact, AutoInitShutdown]
		public void DirectTyping_AutoComplete ()
		{
			var dlg = GetInitializedFileDialog ();
			var openIn = Path.Combine (Environment.CurrentDirectory, "zz");

			Directory.CreateDirectory (openIn);

			var expectedDest = Path.Combine (openIn, "xx");
			Directory.CreateDirectory (expectedDest);

			dlg.Path = openIn + Path.DirectorySeparatorChar;

			Send ("x");

			// nothing selected yet
			Assert.True (dlg.Canceled);
			Assert.Equal ("x", Path.GetFileName (dlg.Path));

			// complete auto typing
			Send ('\t', ConsoleKey.Tab, false);

			// but do not close dialog
			Assert.True (dlg.Canceled);
			Assert.EndsWith ("xx" + Path.DirectorySeparatorChar, dlg.Path);

			// press enter again to confirm the dialog
			Send ('\n', ConsoleKey.Enter, false);
			Assert.False (dlg.Canceled);
			Assert.EndsWith ("xx" + Path.DirectorySeparatorChar, dlg.Path);
		}

		private void Send (char ch, ConsoleKey ck, bool shift = false)
		{
			Application.Driver.SendKeys (ch, ck, shift, false, false);
		}
		private void Send (string chars)
		{
			foreach (var ch in chars) {
				Application.Driver.SendKeys (ch, ConsoleKey.NoName, false, false, false);
			}

		}

		[Fact, AutoInitShutdown]
		public void Autocomplete_NoSuggestion_WhenTextMatchesExactly ()
		{
			var tb = new FileDialog2.TextFieldWithAppendAutocomplete ();
			ForceFocus (tb);

			tb.Text = "/bob/fish";
			tb.CursorPosition = tb.Text.Length;
			tb.GenerateSuggestions (null, "fish", "fishes");

			// should not report success for autocompletion because we already have that exact
			// string
			Assert.False (tb.AcceptSelectionIfAny ());
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
			if (dlg == null) {
				dlg = GetInitializedFileDialog ();
			}

			// First view of a Dialog is ContentView
			return dlg.Subviews [0].Subviews.OfType<FileDialog2.TextFieldWithAppendAutocomplete> ().Single ();
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