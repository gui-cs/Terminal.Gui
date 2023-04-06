using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terminal.Gui.FileServices;
using Xunit;


namespace Terminal.Gui.Core {
	public class FileDialogTests {

		[Fact, AutoInitShutdown]
		public void OnLoad_TextBoxIsFocused ()
		{
			var dlg = GetInitializedFileDialog ();

			var tf = dlg.Subviews.FirstOrDefault (t => t.HasFocus);
			Assert.NotNull (tf);
			Assert.IsType<TextField> (tf);
		}

		[Fact, AutoInitShutdown]
		public void DirectTyping_Allowed ()
		{
			var dlg = GetInitializedFileDialog ();
			var tf = dlg.Subviews.OfType<TextField> ().First (t=>t.HasFocus);
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

		[Fact, AutoInitShutdown]
		public void DoNotConfirmSelectionWhenFindFocused ()
		{
			var dlg = GetInitializedFileDialog ();
			var openIn = Path.Combine (Environment.CurrentDirectory, "zz");
			Directory.CreateDirectory (openIn);
			dlg.Path = openIn + Path.DirectorySeparatorChar;

			Send ('f',ConsoleKey.F,false,false,true);

			Assert.IsType<TextField> (dlg.MostFocused);
			var tf = (TextField) dlg.MostFocused;
			Assert.Equal ("Enter Search", tf.Caption);

			// Dialog has not yet been confirmed with a choice
			Assert.True (dlg.Canceled);

			//pressing enter while search focused should not confirm path
			Send ('\n', ConsoleKey.Enter, false);

			Assert.True (dlg.Canceled);

			// tabbing out of search 
			Send ('\t', ConsoleKey.Tab, false);

			//should allow enter to confirm path
			Send ('\n', ConsoleKey.Enter, false);

			// Dialog has not yet been confirmed with a choice
			Assert.False(dlg.Canceled);
		}

		[Theory, AutoInitShutdown]
		[InlineData(true)]
		[InlineData (false)]
		public void CancelSelection (bool cancel)
		{
			var dlg = GetInitializedFileDialog ();
			var openIn = Path.Combine (Environment.CurrentDirectory, "zz");
			Directory.CreateDirectory (openIn);
			dlg.Path = openIn + Path.DirectorySeparatorChar;

			dlg.FilesSelected += (s, e) => e.Cancel = cancel;

			//pressing enter will complete the current selection
			// unless the event cancels the confirm
			Send ('\n', ConsoleKey.Enter, false);
			
			Assert.Equal(cancel,dlg.Canceled);
		}

		private void Send (char ch, ConsoleKey ck, bool shift = false, bool alt = false, bool control=false)
		{
			Application.Driver.SendKeys (ch, ck, shift, alt, control);
		}
		private void Send (string chars)
		{
			foreach (var ch in chars) {
				Application.Driver.SendKeys (ch, ConsoleKey.NoName, false, false, false);
			}

		}
/*
		[Fact, AutoInitShutdown]
		public void Autocomplete_NoSuggestion_WhenTextMatchesExactly ()
		{
			var tb = new TextFieldWithAppendAutocomplete ();
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
			var tb = new TextFieldWithAppendAutocomplete ();
			ForceFocus (tb);

			tb.Text = @"/bob/fi";
			tb.CursorPosition = tb.Text.Length;
			tb.GenerateSuggestions (null, "fish", "fishes");

			Assert.True (tb.AcceptSelectionIfAny ());
			Assert.Equal (@"/bob/fish", tb.Text);
		}*/


		private FileDialog GetInitializedFileDialog ()
		{
			var dlg = new FileDialog ();
			dlg.BeginInit ();
			dlg.EndInit ();
			Application.Begin (dlg);

			return dlg;
		}
	}
}