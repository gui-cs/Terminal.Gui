using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Rune = System.Rune;

namespace Terminal.Gui {

	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// </summary>
	public abstract class Autocomplete : IAutocomplete {

		/// <summary>
		/// The maximum width of the autocomplete dropdown
		/// </summary>
		public virtual int MaxWidth { get; set; } = 10;

		/// <summary>
		/// The maximum number of visible rows in the autocomplete dropdown to render
		/// </summary>
		public virtual int MaxHeight { get; set; } = 6;

		/// <summary>
		/// True if the autocomplete should be considered open and visible
		/// </summary>
		public virtual bool Visible { get; set; } = true;

		/// <summary>
		/// The strings that form the current list of suggestions to render
		/// based on what the user has typed so far.
		/// </summary>
		public virtual ReadOnlyCollection<string> Suggestions { get; set; } = new ReadOnlyCollection<string> (new string [0]);

		/// <summary>
		/// The full set of all strings that can be suggested.
		/// </summary>
		/// <returns></returns>
		public virtual List<string> AllSuggestions { get; set; } = new List<string> ();

		/// <summary>
		/// The currently selected index into <see cref="Suggestions"/> that the user has highlighted
		/// </summary>
		public virtual int SelectedIdx { get; set; }

		/// <summary>
		/// When more suggestions are available than can be rendered the user
		/// can scroll down the dropdown list.  This indicates how far down they
		/// have gone
		/// </summary>
		public virtual int ScrollOffset { get; set; }

		/// <summary>
		/// The colors to use to render the overlay.  Accessing this property before
		/// the Application has been initialized will cause an error
		/// </summary>
		public virtual ColorScheme ColorScheme {
			get {
				if (colorScheme == null) {
					colorScheme = Colors.Menu;
				}
				return colorScheme;
			}
			set {
				colorScheme = value;
			}
		}

		private ColorScheme colorScheme;

		/// <summary>
		/// The key that the user must press to accept the currently selected autocomplete suggestion
		/// </summary>
		public virtual Key SelectionKey { get; set; } = Key.Enter;

		/// <summary>
		/// The key that the user can press to close the currently popped autocomplete menu
		/// </summary>
		public virtual Key CloseKey { get; set; } = Key.Esc;

		/// <summary>
		/// Renders the autocomplete dialog inside the given <paramref name="view"/> at the
		/// given point.
		/// </summary>
		/// <param name="view">The view the overlay should be rendered into</param>
		/// <param name="renderAt"></param>
		public abstract void RenderOverlay (View view, Point renderAt);

		/// <summary>
		/// Updates <see cref="SelectedIdx"/> to be a valid index within <see cref="Suggestions"/>
		/// </summary>
		public virtual void EnsureSelectedIdxIsValid ()
		{
			SelectedIdx = Math.Max (0, Math.Min (Suggestions.Count - 1, SelectedIdx));

			// if user moved selection up off top of current scroll window
			if (SelectedIdx < ScrollOffset) {
				ScrollOffset = SelectedIdx;
			}

			// if user moved selection down past bottom of current scroll window
			while (SelectedIdx >= ScrollOffset + MaxHeight) {
				ScrollOffset++;
			}
		}

		/// <summary>
		/// Handle key events before <paramref name="hostControl"/> e.g. to make key events like
		/// up/down apply to the autocomplete control instead of changing the cursor position in
		/// the underlying text view.
		/// </summary>
		/// <param name="hostControl">The host control.</param>
		/// <param name="kb">The key event.</param>
		/// <returns><c>true</c>if the key can be handled <c>false</c>otherwise.</returns>
		public abstract bool ProcessKey (View hostControl, KeyEvent kb);

		/// <summary>
		/// Clears <see cref="Suggestions"/>
		/// </summary>
		public virtual void ClearSuggestions ()
		{
			Suggestions = Enumerable.Empty<string> ().ToList ().AsReadOnly ();
		}


		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <see cref="AllSuggestions"/> that
		/// match with the current cursor position/text in the <paramref name="hostControl"/>
		/// </summary>
		/// <param name="hostControl">The text view that you want suggestions for</param>
		public abstract void GenerateSuggestions (View hostControl);

		/// <summary>
		/// Return true if the given symbol should be considered part of a word
		/// and can be contained in matches.  Base behavior is to use <see cref="char.IsLetterOrDigit(char)"/>
		/// </summary>
		/// <param name="rune"></param>
		/// <returns></returns>
		public virtual bool IsWordChar (Rune rune)
		{
			return Char.IsLetterOrDigit ((char)rune);
		}
	}
}
