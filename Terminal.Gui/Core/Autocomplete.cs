using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public bool Visible { get; set; } = true;

		/// <summary>
		/// The strings that form the current list of suggestions to render
		/// </summary>
		public string [] Suggestions { get; set; } = new string [0];

		/// <summary>
		/// The currently selected index into <see cref="Suggestions"/> that the user has highlighted
		/// </summary>
		public int SelectedIdx { get; set; }

		/// <summary>
		/// The colors to use to render the overlay
		/// </summary>
		public ColorScheme ColorScheme { get; set; }

		/// <summary>
		/// The key that the user must press to accept the currently selected autocomplete suggestion
		/// </summary>
		public Key SelectionKey { get; set; } = Key.Enter;

		public Autocomplete ()
		{
			ColorScheme = new ColorScheme () {
				Normal = Application.Driver.MakeAttribute(Color.White,Color.Blue),
				HotNormal = Application.Driver.MakeAttribute (Color.Black, Color.BrightBlue),
			};
		}

		/// <summary>
		/// Renders the autocomplete dialog inside the given <paramref name="view"/> at the
		/// given point.
		/// </summary>
		/// <param name="view">The view the overlay should be rendered into</param>
		/// <param name="renderAt"></param>
		public void RenderOverlay (View view, Point renderAt)
		{
			if (!Visible || !view.HasFocus) {
				return;
			}

			view.Move (renderAt.X, renderAt.Y);
			for(int i=0;i<Math.Min(Suggestions.Length,MaxHeight); i++) {

				if(i== SelectedIdx) {
					Application.Driver.SetAttribute (ColorScheme.HotNormal);
				}
				else {
					Application.Driver.SetAttribute (ColorScheme.Normal);
				}

				view.Move (renderAt.X, renderAt.Y+i);
				Application.Driver.AddStr (Suggestions[i]);
			}
		}

		public bool ProcessKey (TextView hostControl, KeyEvent kb)
		{
			if(!Visible || Suggestions.Length == 0) {
				return false;
			}

			if (kb.Key == Key.CursorDown) {
				SelectedIdx = Math.Min (Suggestions.Length - 1, SelectedIdx + 1);
				hostControl.SetNeedsDisplay ();
				return true;
			}

			if (kb.Key == Key.CursorUp) {
				SelectedIdx = Math.Max (0, Math.Min(SelectedIdx,Suggestions.Length-1) - 1);
				hostControl.SetNeedsDisplay ();
				return true;
			}

			if(kb.Key == SelectionKey && SelectedIdx >=0 && SelectedIdx < Suggestions.Length) {

				var accepted = Suggestions [SelectedIdx];

				// TODO: read current line/word and produce the substring they have not typed yet only

				hostControl.InsertText(accepted);
				return true;
			}

			return false;
		}

		public void GenerateSuggestions (TextView hostControl)
		{
			// TODO: how to get the current line and the cursor position within that line?

			// generate suggestions based on current line and current word
		}
	}
}
