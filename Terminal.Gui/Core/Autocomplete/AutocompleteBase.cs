using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Rune = System.Rune;

namespace Terminal.Gui {
	public abstract class AutocompleteBase : IAutocomplete
	{
		public abstract View HostControl { get; set; }
		public bool PopupInsideContainer { get; set; }

		/// <summary>
		/// The maximum width of the autocomplete dropdown
		/// </summary>
		public virtual int MaxWidth { get; set; } = 10;

		/// <summary>
		/// The maximum number of visible rows in the autocomplete dropdown to render
		/// </summary>
		public virtual int MaxHeight { get; set; } = 6;

		/// <inheritdoc/>


		/// <summary>
		/// True if the autocomplete should be considered open and visible
		/// </summary>
		public virtual bool Visible { get; set; }

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


		/// <inheritdoc/>
		public abstract ColorScheme ColorScheme { get; set; }

		/// <inheritdoc/>
		public virtual Key SelectionKey { get; set; } = Key.Enter;

		/// <inheritdoc/>
		public virtual Key CloseKey { get; set; } = Key.Esc;

		/// <inheritdoc/>
		public virtual Key Reopen { get; set; } = Key.Space | Key.CtrlMask | Key.AltMask;

		/// <inheritdoc/>
		public abstract bool MouseEvent (MouseEvent me, bool fromHost = false);

		/// <inheritdoc/>
		public abstract bool ProcessKey (KeyEvent kb);
		/// <inheritdoc/>
		public abstract void RenderOverlay (Point renderAt);

		/// <summary>
		/// Clears <see cref="Suggestions"/>
		/// </summary>
		public virtual void ClearSuggestions ()
		{
			Suggestions = Enumerable.Empty<string> ().ToList ().AsReadOnly ();
		}


		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <see cref="AllSuggestions"/> that
		/// match with the current cursor position/text in the <see cref="HostControl"/>
		/// </summary>
		/// <param name="columnOffset">The column offset.</param>
		public virtual void GenerateSuggestions (int columnOffset = 0)
		{
			// if there is nothing to pick from
			if (AllSuggestions.Count == 0) {
				ClearSuggestions ();
				return;
			}

			var currentWord = GetCurrentWord (columnOffset);

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
		/// Updates <see cref="SelectedIdx"/> to be a valid index within <see cref="Suggestions"/>
		/// </summary>
		public virtual void EnsureSelectedIdxIsValid ()
		{
			SelectedIdx = Math.Max (0, Math.Min (Suggestions.Count - 1, SelectedIdx));
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
		/// Returns the currently selected word from the <see cref="HostControl"/>.
		/// <para>
		/// When overriding this method views can make use of <see cref="IdxToWord(List{Rune}, int, int)"/>
		/// </para>
		/// </summary>
		/// <param name="columnOffset">The column offset.</param>
		/// <returns></returns>
		protected abstract string GetCurrentWord (int columnOffset = 0);

		/// <summary>
		/// <para>
		/// Given a <paramref name="line"/> of characters, returns the word which ends at <paramref name="idx"/> 
		/// or null.  Also returns null if the <paramref name="idx"/> is positioned in the middle of a word.
		/// </para>
		/// 
		/// <para>
		/// Use this method to determine whether autocomplete should be shown when the cursor is at
		/// a given point in a line and to get the word from which suggestions should be generated.
		/// Use the <paramref name="columnOffset"/> to indicate if search the word at left (negative),
		/// at right (positive) or at the current column (zero) which is the default.
		/// </para>
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		/// <param name="columnOffset"></param>
		/// <returns></returns>
		protected virtual string IdxToWord (List<Rune> line, int idx, int columnOffset = 0)
		{
			StringBuilder sb = new StringBuilder ();
			var endIdx = idx;

			// get the ending word index
			while (endIdx < line.Count) {
				if (IsWordChar (line [endIdx])) {
					endIdx++;
				} else {
					break;
				}
			}

			// It isn't a word char then there is no way to autocomplete that word
			if (endIdx == idx && columnOffset != 0) {
				return null;
			}

			// we are at the end of a word.  Work out what has been typed so far
			while (endIdx-- > 0) {
				if (IsWordChar (line [endIdx])) {
					sb.Insert (0, (char)line [endIdx]);
				} else {
					break;
				}
			}
			return sb.ToString ();
		}
	}
}

