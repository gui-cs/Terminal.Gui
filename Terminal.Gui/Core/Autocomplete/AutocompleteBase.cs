using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Rune = System.Rune;

namespace Terminal.Gui {

	public interface ISuggestionGenerator
	{

		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <see cref="AllSuggestions"/> that
		/// match with the current cursor position/text in the <see cref="HostControl"/>.
		/// </summary>
		/// <param name="columnOffset">The column offset. Current (zero - default), left (negative), right (positive).</param>
		IEnumerable<Suggestion> GenerateSuggestions (List<Rune> currentLine, int idx);

		bool IsWordChar (Rune rune);

	}

	public class SingleWordSuggestionGenerator : ISuggestionGenerator
	{
		/// <summary>
		/// The full set of all strings that can be suggested.
		/// </summary>
		/// <returns></returns>
		public virtual List<string> AllSuggestions { get; set; } = new List<string> ();

		public IEnumerable<Suggestion> GenerateSuggestions (List<Rune> currentLine, int idx)
		{
			// if there is nothing to pick from
			if (AllSuggestions.Count == 0) {
				return Enumerable.Empty<Suggestion>();
			}

			var currentWord = IdxToWord (currentLine, idx);

			if (string.IsNullOrWhiteSpace (currentWord)) {
				return Enumerable.Empty<Suggestion>();
			} else {
				return AllSuggestions.Where (o =>
				o.StartsWith (currentWord, StringComparison.CurrentCultureIgnoreCase) &&
				!o.Equals (currentWord, StringComparison.CurrentCultureIgnoreCase)
				).Select(o=>new Suggestion(currentWord.Length,o))
					.ToList ().AsReadOnly ();

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

	public abstract class AutocompleteBase : IAutocomplete {
		
		/// <inheritdoc/>
		public abstract View HostControl { get; set; }
		/// <inheritdoc/>
		public bool PopupInsideContainer { get; set; }
		
		public ISuggestionGenerator SuggestionGenerator {get;set;} = new SingleWordSuggestionGenerator();

		/// <inheritdoc/>
		public virtual int MaxWidth { get; set; } = 10;

		/// <inheritdoc/>
		public virtual int MaxHeight { get; set; } = 6;

		/// <inheritdoc/>


		/// <inheritdoc/>
		public virtual bool Visible { get; set; }

		/// <inheritdoc/>
		public virtual ReadOnlyCollection<Suggestion> Suggestions { get; set; } = new ReadOnlyCollection<Suggestion> (new Suggestion [0]);



		/// <inheritdoc/>
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

		/// <inheritdoc/>>
		public virtual void ClearSuggestions ()
		{
			Suggestions = Enumerable.Empty<Suggestion> ().ToList ().AsReadOnly ();
		}


		/// <inheritdoc/>
		public virtual void GenerateSuggestions (List<Rune> currentLine, int idx)
		{
			Suggestions = SuggestionGenerator.GenerateSuggestions(currentLine, idx).ToList().AsReadOnly();

			EnsureSelectedIdxIsValid ();
		}

		/// <summary>
		/// Updates <see cref="SelectedIdx"/> to be a valid index within <see cref="Suggestions"/>
		/// </summary>
		public virtual void EnsureSelectedIdxIsValid ()
		{
			SelectedIdx = Math.Max (0, Math.Min (Suggestions.Count - 1, SelectedIdx));
		}
	}
}

