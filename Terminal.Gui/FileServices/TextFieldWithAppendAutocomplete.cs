using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Terminal.Gui.FileServices {

	internal class TextFieldWithAppendAutocomplete : CaptionedTextField {

		private int? currentFragment = null;
		private string [] validFragments = new string [0];

		public TextFieldWithAppendAutocomplete ()
		{
			this.KeyPress += (s, k) => {
				var key = k.KeyEvent.Key;
				if (key == Key.Tab) {
					k.Handled = this.AcceptSelectionIfAny ();
				} else
				if (key == Key.CursorUp) {
					k.Handled = this.CycleSuggestion (1);
				} else
				if (key == Key.CursorDown) {
					k.Handled = this.CycleSuggestion (-1);
				}
			};

			this.ColorScheme = new ColorScheme {
				Normal = new Attribute (Color.White, Color.Black),
				HotNormal = new Attribute (Color.White, Color.Black),
				Focus = new Attribute (Color.White, Color.Black),
				HotFocus = new Attribute (Color.White, Color.Black),
			};
		}

		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			if (!this.MakingSuggestion ()) {
				return;
			}

			// draw it like its selected even though its not
			Driver.SetAttribute (new Attribute (Color.DarkGray, Color.Black));
			this.Move (this.Text.Length, 0);
			Driver.AddStr (this.validFragments [this.currentFragment.Value]);
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
				this.Text += this.validFragments [this.currentFragment.Value];
				this.MoveCursorToEnd ();

				this.ClearSuggestions ();
				return true;
			}

			return false;
		}

		internal void MoveCursorToEnd ()
		{
			this.ClearAllSelection ();
			this.CursorPosition = this.Text.Length;
		}

		internal void GenerateSuggestions (FileDialogState state, params string [] suggestions)
		{
			if (!this.CursorIsAtEnd ()) {
				return;
			}

			var path = this.Text.ToString ();
			var last = path.LastIndexOfAny (FileDialog.Separators);

			if (last == -1 || suggestions.Length == 0 || last >= path.Length - 1) {
				this.currentFragment = null;
				return;
			}

			var term = path.Substring (last + 1);

			if (term.Equals (state?.Directory?.Name)) {
				this.ClearSuggestions ();
				return;
			}

			bool isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

			var validSuggestions = suggestions
				.Where (s => s.StartsWith (term, isWindows ?
					StringComparison.InvariantCultureIgnoreCase :
					StringComparison.InvariantCulture))
				.OrderBy (m => m.Length)
				.ToArray ();


			// nothing to suggest
			if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length) {
				this.ClearSuggestions ();
				return;
			}

			this.validFragments = validSuggestions.Select (f => f.Substring (term.Length)).ToArray ();
			this.currentFragment = 0;
		}

		internal void ClearSuggestions ()
		{
			this.currentFragment = null;
			this.validFragments = new string [0];
			this.SetNeedsDisplay ();
		}

		internal void GenerateSuggestions (FileDialogState state)
		{
			if (state == null) {
				return;
			}

			var suggestions = state.Children.Select (
				e => e.FileSystemInfo is DirectoryInfo d
					? d.Name + System.IO.Path.DirectorySeparatorChar
					: e.FileSystemInfo.Name)
				.ToArray ();

			this.GenerateSuggestions (state, suggestions);
		}

		internal void SetTextTo (FileSystemInfo fileSystemInfo)
		{
			var newText = fileSystemInfo.FullName;
			if (fileSystemInfo is DirectoryInfo) {
				newText += System.IO.Path.DirectorySeparatorChar;
			}
			this.Text = newText;
			this.MoveCursorToEnd ();
		}

		internal bool CursorIsAtEnd ()
		{
			return this.CursorPosition == this.Text.Length;
		}

		/// <summary>
		/// Returns true if there is a suggestion that can be made and the control
		/// is in a state where user would expect to see auto-complete (i.e. focused and
		/// cursor in right place).
		/// </summary>
		/// <returns></returns>
		private bool MakingSuggestion ()
		{
			return this.currentFragment != null && this.HasFocus && this.CursorIsAtEnd ();
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
			this.SetNeedsDisplay ();
			return true;
		}
	}
}