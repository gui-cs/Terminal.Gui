using System;
namespace Terminal.Gui {
	/// <summary>
	/// Radio group shows a group of labels, only one of those can be selected at a given time
	/// </summary>
	public class RadioGroup : View {
		int selected, cursor;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.RadioGroup"/> class
		/// setting up the initial set of radio labels and the item that should be selected and uses
		/// an absolute layout for the result.
		/// </summary>
		/// <param name="rect">Boundaries for the radio group.</param>
		/// <param name="radioLabels">Radio labels, the strings can contain hotkeys using an undermine before the letter.</param>
		/// <param name="selected">The item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (Rect rect, string [] radioLabels, int selected = 0) : base (rect)
		{
			this.selected = selected;
			this.radioLabels = radioLabels;
			CanFocus = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.RadioGroup"/> class
		/// setting up the initial set of radio labels and the item that should be selected.
		/// </summary>
		/// <param name="radioLabels">Radio labels, the strings can contain hotkeys using an undermine before the letter.</param>
		/// <param name="selected">The item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (string [] radioLabels, int selected = 0) : base ()
		{
			var r = MakeRect (0, 0, radioLabels);
			Width = r.Width;
			Height = radioLabels.Length;

			this.selected = selected;
			this.radioLabels = radioLabels;
			CanFocus = true;
		}

		static Rect MakeRect (int x, int y, string [] radioLabels)
		{
			int width = 0;

			foreach (var s in radioLabels)
				width = Math.Max (s.Length + 4, width);
			return new Rect (x, y, width, radioLabels.Length);
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.RadioGroup"/> class
		/// setting up the initial set of radio labels and the item that should be selected, 
		/// the view frame is computed from the provided radioLabels.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="radioLabels">Radio labels, the strings can contain hotkeys using an undermine before the letter.</param>
		/// <param name="selected">The item to be selected, the value is clamped to the number of items.</param>		
		public RadioGroup (int x, int y, string [] radioLabels, int selected = 0) : this (MakeRect (x, y, radioLabels), radioLabels, selected)
		{
		}

		string [] radioLabels;

		/// <summary>
		/// The radio labels to display
		/// </summary>
		/// <value>The radio labels.</value>
		public string [] RadioLabels { 
			get => radioLabels;
			set {
				radioLabels = value;
				selected = 0;
				cursor = 0;
				SetNeedsDisplay ();
			}
		}

		public override void Redraw (Rect region)
		{
			base.Redraw (region);
			for (int i = 0; i < radioLabels.Length; i++) {
				Move (0, i);
				Driver.SetAttribute (ColorScheme.Normal);
				Driver.AddStr (i == selected ? "(o) " : "( ) ");
				DrawHotString (radioLabels [i], HasFocus && i == cursor, ColorScheme);
			}
		}

		public override void PositionCursor ()
		{
			Move (1, cursor);
		}

		public Action<int> SelectionChanged;

		/// <summary>
		/// The currently selected item from the list of radio labels
		/// </summary>
		/// <value>The selected.</value>
		public int Selected {
			get => selected;
			set {
				selected = value;
				SelectionChanged?.Invoke (selected);
				SetNeedsDisplay ();
			}
		}

		public override bool ProcessColdKey (KeyEvent kb)
		{
			var key = kb.KeyValue;
			if (key < Char.MaxValue && Char.IsLetterOrDigit ((char)key)) {
				int i = 0;
				key = Char.ToUpper ((char)key);
				foreach (var l in radioLabels) {
					bool nextIsHot = false;
					foreach (var c in l) {
						if (c == '_')
							nextIsHot = true;
						else {
							if (nextIsHot && c == key) {
								Selected = i;
								cursor = i;
								if (!HasFocus)
									SuperView.SetFocus (this);
								return true;
							}
							nextIsHot = false;
						}
					}
					i++;
				}
			}
			return false;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.CursorUp:
				if (cursor > 0) {
					cursor--;
					SetNeedsDisplay ();
					return true;
				}
				break;
			case Key.CursorDown:
				if (cursor + 1 < radioLabels.Length) {
					cursor++;
					SetNeedsDisplay ();
					return true;
				}
				break;
			case Key.Space:
				Selected = cursor;
				return true;
			}
			return base.ProcessKey (kb);
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked))
				return false;

			SuperView.SetFocus (this);

			if (me.Y < radioLabels.Length) {
				cursor = selected = me.Y;
				SetNeedsDisplay ();
			}
			return true;
		}
	}
}
