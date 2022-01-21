using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Rune = System.Rune;

namespace Terminal.Gui {

	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// </summary>
	public interface IAutocomplete {

		/// <summary>
		/// The maximum width of the autocomplete dropdown
		/// </summary>
		int MaxWidth { get; set; }

		/// <summary>
		/// The maximum number of visible rows in the autocomplete dropdown to render
		/// </summary>
		int MaxHeight { get; set; }

		/// <summary>
		/// True if the autocomplete should be considered open and visible
		/// </summary>
		bool Visible { get; set; }

		/// <summary>
		/// The strings that form the current list of suggestions to render
		/// based on what the user has typed so far.
		/// </summary>
		ReadOnlyCollection<string> Suggestions { get; set; }

		/// <summary>
		/// The full set of all strings that can be suggested.
		/// </summary>
		List<string> AllSuggestions { get; set; }

		/// <summary>
		/// The currently selected index into <see cref="Suggestions"/> that the user has highlighted
		/// </summary>
		int SelectedIdx { get; set; }

		/// <summary>
		/// When more suggestions are available than can be rendered the user
		/// can scroll down the dropdown list.  This indicates how far down they
		/// have gone
		/// </summary>
		int ScrollOffset { get; set; }

		/// <summary>
		/// The colors to use to render the overlay.  Accessing this property before
		/// the Application has been initialized will cause an error
		/// </summary>
		ColorScheme ColorScheme { get; set; }

		/// <summary>
		/// The key that the user must press to accept the currently selected autocomplete suggestion
		/// </summary>
		Key SelectionKey { get; set; }

		/// <summary>
		/// The key that the user can press to close the currently popped autocomplete menu
		/// </summary>
		Key CloseKey { get; set; }

		/// <summary>
		/// Renders the autocomplete dialog inside the given <paramref name="view"/> at the
		/// given point.
		/// </summary>
		/// <param name="view">The view the overlay should be rendered into</param>
		/// <param name="renderAt"></param>
		void RenderOverlay (View view, Point renderAt);

		/// <summary>
		/// Updates <see cref="SelectedIdx"/> to be a valid index within <see cref="Suggestions"/>
		/// </summary>
		void EnsureSelectedIdxIsValid ();

		/// <summary>
		/// Handle key events before <paramref name="hostControl"/> e.g. to make key events like
		/// up/down apply to the autocomplete control instead of changing the cursor position in
		/// the underlying text view.
		/// </summary>
		/// <param name="hostControl">The host control.</param>
		/// <param name="kb">The key event.</param>
		/// <returns><c>true</c>if the key can be handled <c>false</c>otherwise.</returns>
		bool ProcessKey (View hostControl, KeyEvent kb);

		/// <summary>
		/// Clears <see cref="Suggestions"/>
		/// </summary>
		void ClearSuggestions ();

		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <see cref="AllSuggestions"/> that
		/// match with the current cursor position/text in the <paramref name="hostControl"/>
		/// </summary>
		/// <param name="hostControl">The text view that you want suggestions for</param>
		void GenerateSuggestions (View hostControl);

		/// <summary>
		/// Return true if the given symbol should be considered part of a word
		/// and can be contained in matches.  Base behavior is to use <see cref="char.IsLetterOrDigit(char)"/>
		/// </summary>
		/// <param name="rune"></param>
		/// <returns><c>true</c> if rune is part of a word contained in matches, <c>false</c>otherwise.</returns>
		bool IsWordChar (Rune rune);
	}
}
