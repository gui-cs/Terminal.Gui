using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Terminal.Gui {

	/// <summary>
	/// Autocomplete for a <see cref="TextField"/> which shows suggestions within the box.
	/// Displayed suggestions can be completed using the tab key.
	/// </summary>
	public class AppendAutocomplete : IAutocomplete {

		private int? currentFragment = null;
		private string [] validFragments = new string [0];
		private TextField textField;

		public View HostControl { get => textField; set => textField = (TextField)value; }
		public bool PopupInsideContainer { get; set; }
		public int MaxWidth { get; set; }
		public int MaxHeight { get; set; }
		public bool Visible { get; set; }
		public ReadOnlyCollection<string> Suggestions { get; set; }
		public List<string> AllSuggestions { get; set; }
		public int SelectedIdx { get; set; }
		public ColorScheme ColorScheme { get; set; }
		public Key SelectionKey { get; set; } = Key.Tab;
		public Key CloseKey { get; set; }
		public Key Reopen { get; set; }

		public void ClearSuggestions ()
		{
			this.currentFragment = null;
			this.validFragments = new string [0];
			textField.SetNeedsDisplay ();
		}

		public void GenerateSuggestions (int columnOffset = 0)
		{
			validFragments = new string []{ "fish", "flipper", "fun" };
		}

		public bool MouseEvent (MouseEvent me, bool fromHost = false)
		{
			return false;
		}

		public bool ProcessKey (KeyEvent kb)
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

		public void RenderOverlay (Point renderAt)
		{
			if (!this.MakingSuggestion ()) {
				return;
			}

			// draw it like its selected even though its not
			Application.Driver.SetAttribute (new Attribute (Color.DarkGray, Color.Black));
			textField.Move (textField.Text.Length, 0);
			Application.Driver.AddStr (this.validFragments [this.currentFragment.Value]);
		}

		public AppendAutocomplete (TextField textField)
		{
			this.textField = textField;
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
				textField.Text += this.validFragments [this.currentFragment.Value];
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
			return this.currentFragment != null && textField.HasFocus && this.CursorIsAtEnd ();
		}

		private bool CycleSuggestion (int direction)
		{
			if (this.currentFragment == null || this.validFragments.Length <= 1) {
				return false;
			}

			this.currentFragment = (this.currentFragment + direction) % this.validFragments.Length;

			if (this.currentFragment < 0) {
				this.currentFragment = this.validFragments.Length - 1;
			}
			textField.SetNeedsDisplay ();
			return true;
		}
	}
}
