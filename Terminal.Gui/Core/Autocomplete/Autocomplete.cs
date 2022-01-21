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
		/// <inheritdoc/>
		public virtual void RenderOverlay (View view, Point renderAt)
		{
			if (!Visible || !view.HasFocus || Suggestions.Count == 0) {
				return;
			}

			view.Move (renderAt.X, renderAt.Y);

			// don't overspill vertically
			var height = Math.Min (view.Bounds.Height - renderAt.Y, MaxHeight);

			var toRender = Suggestions.Skip (ScrollOffset).Take (height).ToArray ();

			if (toRender.Length == 0) {
				return;
			}

			var width = Math.Min (MaxWidth, toRender.Max (s => s.Length));

			// don't overspill horizontally
			width = Math.Min (view.Bounds.Width - renderAt.X, width);

			for (int i = 0; i < toRender.Length; i++) {

				if (i == SelectedIdx - ScrollOffset) {
					Application.Driver.SetAttribute (ColorScheme.Focus);
				} else {
					Application.Driver.SetAttribute (ColorScheme.Normal);
				}

				view.Move (renderAt.X, renderAt.Y + i);

				var text = TextFormatter.ClipOrPad (toRender [i], width);

				Application.Driver.AddStr (text);
			}
		}

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
		public virtual bool ProcessKey (View hostControl, KeyEvent kb)
		{
			if (IsWordChar ((char)kb.Key)) {
				Visible = true;
			}

			if (!Visible || Suggestions.Count == 0) {
				return false;
			}

			if (kb.Key == Key.CursorDown) {
				MoveDown (hostControl);
				return true;
			}

			if (kb.Key == Key.CursorUp) {
				MoveUp (hostControl);
				return true;
			}

			if (kb.Key == SelectionKey) {
				return Select (hostControl);
			}

			if (kb.Key == CloseKey) {
				Close (hostControl);
				return true;
			}

			return false;
		}

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
		/// <inheritdoc/>
		public virtual void GenerateSuggestions (View hostControl)
		{
			// if there is nothing to pick from
			if (AllSuggestions.Count == 0) {
				ClearSuggestions ();
				return;
			}

			var currentWord = GetCurrentWord(hostControl);

			if (string.IsNullOrWhiteSpace (currentWord)) {
				ClearSuggestions ();
			} else {
				Suggestions = AllSuggestions.Where (o =>
				o.StartsWith (currentWord, StringComparison.CurrentCultureIgnoreCase) &&
				!o.Equals (currentWord, StringComparison.CurrentCultureIgnoreCase)
				).ToList ().AsReadOnly ();

				EnsureSelectedIdxIsValid ();
			}
		}


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

		/// <summary>
		/// Completes the autocomplete selection process.  Called when user hits the <see cref="SelectionKey"/>.
		/// </summary>
		/// <param name="hostControl"></param>
		/// <returns></returns>
		protected bool Select (View hostControl)
		{
			if (SelectedIdx >= 0 && SelectedIdx < Suggestions.Count) {
				var accepted = Suggestions [SelectedIdx];

				return InsertSelection (hostControl,accepted);
			}

			return false;
		}

		/// <summary>
		/// Called when the user confirms a selection at the current cursor location in
		/// the <paramref name="hostControl"/>.  The <paramref name="accepted"/> string
		/// is the full autocomplete word to be inserted.  Typically a host will have to
		/// remove some characters such that the <paramref name="accepted"/> string 
		/// completes the word instead of simply being appended.
		/// </summary>
		/// <param name="hostControl"></param>
		/// <param name="accepted"></param>
		/// <returns>True if the insertion was possible otherwise false</returns>
		protected abstract bool InsertSelection (View hostControl, string accepted);


		/// <summary>
		/// Returns the currently selected word in the <paramref name="hostControl"/>.
		/// <para>
		/// When overriding this method views can make use of <see cref="IdxToWord(List{Rune}, int)"/>
		/// </para
		/// </summary>
		/// <param name="hostControl"></param>
		/// <returns></returns>
		protected abstract string GetCurrentWord (View hostControl);

		/// <summary>
		/// <para>
		/// Given a <paramref name="line"/> of characters, returns the word which ends at <paramref name="idx"/> 
		/// or null.  Also returns null if the <paramref name="idx"/> is positioned in the middle of a word.
		/// </para>
		/// 
		/// <para>Use this method to determine whether autocomplete should be shown when the cursor is at
		/// a given point in a line and to get the word from which suggestions should be generated.</para>
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		/// <returns></returns>
		protected virtual string IdxToWord (List<Rune> line, int idx)
		{
			StringBuilder sb = new StringBuilder ();

			// do not generate suggestions if the cursor is positioned in the middle of a word
			bool areMidWord;

			if (idx == line.Count) {
				// the cursor positioned at the very end of the line
				areMidWord = false;
			} else {
				// we are in the middle of a word if the cursor is over a letter/number
				areMidWord = IsWordChar (line [idx]);
			}

			// if we are in the middle of a word then there is no way to autocomplete that word
			if (areMidWord) {
				return null;
			}

			// we are at the end of a word.  Work out what has been typed so far
			while (idx-- > 0) {

				if (IsWordChar (line [idx])) {
					sb.Insert (0, (char)line [idx]);
				} else {
					break;
				}
			}
			return sb.ToString ();
		}
	
		/// <summary>
		/// Closes the Autocomplete context menu if it is showing and <see cref="ClearSuggestions"/>
		/// </summary>
		/// <param name="hostControl"></param>
		protected void Close (View hostControl)
		{
			ClearSuggestions ();
			Visible = false;
			hostControl.SetNeedsDisplay ();
		}

		/// <summary>
		/// Moves the selection in the Autocomplete context menu up one
		/// </summary>
		/// <param name="hostControl"></param>
		protected void MoveUp (View hostControl)
		{
			SelectedIdx--;
			if (SelectedIdx < 0) {
				SelectedIdx = Suggestions.Count - 1;
			}
			EnsureSelectedIdxIsValid ();
			hostControl.SetNeedsDisplay ();
		}

		/// <summary>
		/// Moves the selection in the Autocomplete context menu down one
		/// </summary>
		/// <param name="hostControl"></param>
		protected void MoveDown (View hostControl)
		{
			SelectedIdx++;
			if (SelectedIdx > Suggestions.Count - 1) {
				SelectedIdx = 0;
			}
			EnsureSelectedIdxIsValid ();
			hostControl.SetNeedsDisplay ();
		}
	}
}
