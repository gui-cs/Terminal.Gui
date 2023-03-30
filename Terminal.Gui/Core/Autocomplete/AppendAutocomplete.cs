using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Autocomplete for a <see cref="TextField"/> which shows suggestions within the box.
	/// Displayed suggestions can be completed using the tab key.
	/// </summary>
	public class AppendAutocomplete : AutocompleteBase {

		private TextField textField;

		public override View HostControl { get => textField; set => textField = (TextField)value; }
		public override ColorScheme ColorScheme { get; set; }

		public override void ClearSuggestions ()
		{
			base.ClearSuggestions ();
			textField.SetNeedsDisplay ();
		}

		public override bool MouseEvent (MouseEvent me, bool fromHost = false)
		{
			return false;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			var key = kb.Key;
			if (key == SelectionKey) {
				return this.AcceptSelectionIfAny ();
			} else
			if (key == Key.CursorUp) {
				return this.CycleSuggestion (1);
			} else
			if (key == Key.CursorDown) {
				return this.CycleSuggestion (-1);
			}

			return false;
		}

		public override void RenderOverlay (Point renderAt)
		{
			if (!this.MakingSuggestion ()) {
				return;
			}

			// draw it like its selected even though its not
			Application.Driver.SetAttribute (new Attribute (Color.DarkGray, textField.ColorScheme.Focus.Background));
			textField.Move (textField.Text.Length, 0);

			var suggestion = this.Suggestions.ElementAt (this.SelectedIdx);
			var fragment = suggestion.Replacement.Substring (suggestion.Remove);
			Application.Driver.AddStr (fragment);
		}

		public AppendAutocomplete (TextField textField)
		{
			this.textField = textField;
			SelectionKey = Key.Tab;
		}

		/// <summary>
		/// Accepts the current autocomplete suggestion displaying in the text box.
		/// Returns true if a valid suggestion was being rendered and acceptable or
		/// false if no suggestion was showing.
		/// </summary>
		/// <returns></returns>
		internal bool AcceptSelectionIfAny ()
		{
			if (this.MakingSuggestion ()) {

				var insert = this.Suggestions.ElementAt (this.SelectedIdx);
				var newText = textField.Text.ToString ();
				newText = newText.Substring (0, newText.Length - insert.Remove);
				newText += insert.Replacement;
				textField.Text = newText;

				this.MoveCursorToEnd ();

				this.ClearSuggestions ();
				return true;
			}

			return false;
		}

		internal void MoveCursorToEnd ()
		{
			textField.ClearAllSelection ();
			textField.CursorPosition = textField.Text.Length;
		}

		internal void SetTextTo (FileSystemInfo fileSystemInfo)
		{
			var newText = fileSystemInfo.FullName;
			if (fileSystemInfo is DirectoryInfo) {
				newText += System.IO.Path.DirectorySeparatorChar;
			}
			textField.Text = newText;
			this.MoveCursorToEnd ();
		}

		internal bool CursorIsAtEnd ()
		{
			return textField.CursorPosition == textField.Text.Length;
		}

		/// <summary>
		/// Returns true if there is a suggestion that can be made and the control
		/// is in a state where user would expect to see auto-complete (i.e. focused and
		/// cursor in right place).
		/// </summary>
		/// <returns></returns>
		private bool MakingSuggestion ()
		{
			return Suggestions.Any () && this.SelectedIdx != -1 && textField.HasFocus && this.CursorIsAtEnd ();
		}

		private bool CycleSuggestion (int direction)
		{
			if (this.Suggestions.Count <= 1) {
				return false;
			}

			this.SelectedIdx = (this.SelectedIdx + direction) % this.Suggestions.Count;

			if (this.SelectedIdx < 0) {
				this.SelectedIdx = this.Suggestions.Count () - 1;
			}
			textField.SetNeedsDisplay ();
			return true;
		}
	}
}
