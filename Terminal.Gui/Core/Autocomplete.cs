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
	public class Autocomplete {

		/// <summary>
		/// The maximum width of the autocomplete dropdown
		/// </summary>
		public int MaxWidth { get; set; } = 10;

		/// <summary>
		/// The maximum number of visible rows in the autocomplete dropdown to render
		/// </summary>
		public int MaxHeight { get; set; } = 6;

		/// <summary>
		/// True if the autocomplete should be considered open and visible
		/// </summary>
		protected bool Visible { get; set; } = true;

		/// <summary>
		/// The strings that form the current list of suggestions to render
		/// based on what the user has typed so far.
		/// </summary>
		public ReadOnlyCollection<string> Suggestions { get; protected set; } = new ReadOnlyCollection<string>(new string[0]);

		/// <summary>
		/// The full set of all strings that can be suggested.
		/// </summary>
		/// <returns></returns>
		public List<string> AllSuggestions { get; set; } = new List<string>();

		/// <summary>
		/// The currently selected index into <see cref="Suggestions"/> that the user has highlighted
		/// </summary>
		public int SelectedIdx { get; set; }

		/// <summary>
		/// When more suggestions are available than can be rendered the user
		/// can scroll down the dropdown list.  This indicates how far down they
		/// have gone
		/// </summary>
		public int ScrollOffset {get;set;}

		/// <summary>
		/// The colors to use to render the overlay.  Accessing this property before
		/// the Application has been initialised will cause an error
		/// </summary>
		public ColorScheme ColorScheme { 
			get
			{
				if(colorScheme == null)
				{
					colorScheme = Colors.Menu;
				}
				return colorScheme;
			}
		 	set
			{
				colorScheme = value;
			}
		}

		private ColorScheme colorScheme;

		/// <summary>
		/// The key that the user must press to accept the currently selected autocomplete suggestion
		/// </summary>
		public Key SelectionKey { get; set; } = Key.Enter;

		/// <summary>
		/// The key that the user can press to close the currently popped autocomplete menu
		/// </summary>
		public Key CloseKey {get;set;} = Key.Esc;

		/// <summary>
		/// Renders the autocomplete dialog inside the given <paramref name="view"/> at the
		/// given point.
		/// </summary>
		/// <param name="view">The view the overlay should be rendered into</param>
		/// <param name="renderAt"></param>
		public void RenderOverlay (View view, Point renderAt)
		{
			if (!Visible || !view.HasFocus || Suggestions.Count == 0) {
				return;
			}

			view.Move (renderAt.X, renderAt.Y);

			// don't overspill vertically
			var height = Math.Min(view.Bounds.Height - renderAt.Y,MaxHeight);

			var toRender = Suggestions.Skip(ScrollOffset).Take(height).ToArray();

			if(toRender.Length == 0)
			{
				return;
			}

			var width = Math.Min(MaxWidth,toRender.Max(s=>s.Length));

			// don't overspill horizontally
			width = Math.Min(view.Bounds.Width - renderAt.X ,width);

			for(int i=0;i<toRender.Length; i++) {

				if(i ==  SelectedIdx - ScrollOffset) {
					Application.Driver.SetAttribute (ColorScheme.Focus);
				}
				else {
					Application.Driver.SetAttribute (ColorScheme.Normal);
				}

				view.Move (renderAt.X, renderAt.Y+i);

				var text = TextFormatter.ClipOrPad(toRender[i],width);

				Application.Driver.AddStr (text );
			}
		}

		/// <summary>
		/// Updates <see cref="SelectedIdx"/> to be a valid index within <see cref="Suggestions"/>
		/// </summary>
		public void EnsureSelectedIdxIsValid()
		{				
			SelectedIdx = Math.Max (0,Math.Min (Suggestions.Count - 1, SelectedIdx));
			
			// if user moved selection up off top of current scroll window
			if(SelectedIdx < ScrollOffset)
			{
				ScrollOffset = SelectedIdx;
			}

			// if user moved selection down past bottom of current scroll window
			while(SelectedIdx >= ScrollOffset + MaxHeight ){
				ScrollOffset++;
			}
		}

		/// <summary>
		/// Handle key events before <paramref name="hostControl"/> e.g. to make key events like
		/// up/down apply to the autocomplete control instead of changing the cursor position in 
		/// the underlying text view.
		/// </summary>
		/// <param name="hostControl"></param>
		/// <param name="kb"></param>
		/// <returns></returns>
		public bool ProcessKey (TextView hostControl, KeyEvent kb)
		{
			if(IsWordChar((char)kb.Key))
			{
				Visible = true;
			}

			if(!Visible || Suggestions.Count == 0) {
				return false;
			}

			if (kb.Key == Key.CursorDown) {
				SelectedIdx++;
				EnsureSelectedIdxIsValid();
				hostControl.SetNeedsDisplay ();
				return true;
			}

			if (kb.Key == Key.CursorUp) {
				SelectedIdx--;
				EnsureSelectedIdxIsValid();
				hostControl.SetNeedsDisplay ();
				return true;
			}

			if(kb.Key == SelectionKey && SelectedIdx >=0 && SelectedIdx < Suggestions.Count) {

				var accepted = Suggestions [SelectedIdx];
								
				var typedSoFar = GetCurrentWord (hostControl) ?? "";
				
				if(typedSoFar.Length < accepted.Length) {

					// delete the text
					for(int i=0;i<typedSoFar.Length;i++)
					{
						hostControl.DeleteTextBackwards();
					}

					hostControl.InsertText (accepted);
					return true;
				}

				return false;
			}

			if(kb.Key == CloseKey)
			{
				ClearSuggestions ();
				Visible = false;
				hostControl.SetNeedsDisplay();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Clears <see cref="Suggestions"/>
		/// </summary>
		public void ClearSuggestions ()
		{
			Suggestions = Enumerable.Empty<string> ().ToList ().AsReadOnly ();
		}


		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <see cref="AllSuggestions"/> that
		/// match with the current cursor position/text in the <paramref name="hostControl"/>
		/// </summary>
		/// <param name="hostControl">The text view that you want suggestions for</param>
		public void GenerateSuggestions (TextView hostControl)
		{
			// if there is nothing to pick from
			if(AllSuggestions.Count == 0) {
				ClearSuggestions ();
				return;
			}

			var currentWord = GetCurrentWord (hostControl); 

			if(string.IsNullOrWhiteSpace(currentWord)) {
				ClearSuggestions ();
			}
			else {
				Suggestions = AllSuggestions.Where (o => 
				o.StartsWith (currentWord, StringComparison.CurrentCultureIgnoreCase) &&
				!o.Equals(currentWord,StringComparison.CurrentCultureIgnoreCase)
				).ToList ().AsReadOnly();

				EnsureSelectedIdxIsValid();
			}
		}

		private string GetCurrentWord (TextView hostControl)
		{
			var currentLine = hostControl.GetCurrentLine ();
			var cursorPosition = Math.Min (hostControl.CurrentColumn, currentLine.Count);
			return IdxToWord (currentLine, cursorPosition);
		}

		private string IdxToWord (List<Rune> line, int idx)
		{
			StringBuilder sb = new StringBuilder ();

			// do not generate suggestions if the cursor is positioned in the middle of a word
			bool areMidWord;

			if(idx == line.Count) {
				// the cursor positioned at the very end of the line
				areMidWord = false;
			}
			else {
				// we are in the middle of a word if the cursor is over a letter/number
				areMidWord = IsWordChar (line [idx]);
			}

			// if we are in the middle of a word then there is no way to autocomplete that word
			if(areMidWord) {
				return null;
			}

			// we are at the end of a word.  Work out what has been typed so far
			while(idx-- > 0) {

				if(IsWordChar(line [idx])) {
					sb.Insert(0,(char)line [idx]);
				}
				else {
					break;
				}
			}
			return sb.ToString ();
		}

		/// <summary>
		/// Return true if the given symbol should be considered part of a word
		/// and can be contained in matches.  Base behaviour is to use <see cref="char.IsLetterOrDigit(char)"/>
		/// </summary>
		/// <param name="rune"></param>
		/// <returns></returns>
		public virtual bool IsWordChar (Rune rune)
		{
			return Char.IsLetterOrDigit ((char)rune);
		}
	}
}
