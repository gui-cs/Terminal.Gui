using System;
namespace Terminal.Gui {
	/// <summary>
	/// <see cref="RadioGroup"/> shows a group of radio labels, only one of those can be selected at a given time
	/// </summary>
	public class RadioGroup : View {
		int selected, cursor;

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class
		/// setting up the initial set of radio labels and the item that should be selected and uses
		/// an absolute layout for the result.
		/// </summary>
		/// <param name="rect">Boundaries for the radio group.</param>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The index of item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (Rect rect, string [] radioLabels, int selected = 0) : base (rect)
		{
			this.selected = selected;
			this.radioLabels = radioLabels;
			CanFocus = true;
		}

		/// <summary>
		/// The location of the cursor in the <see cref="RadioGroup"/>
		/// </summary>
		public int Cursor {
			get => cursor;
			set {
				if (cursor < 0 || cursor >= radioLabels.Length)
					return;
				cursor = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class
		/// setting up the initial set of radio labels and the item that should be selected.
		/// </summary>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The index of the item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (string [] radioLabels, int selected = 0) : base ()
		{
			SetWidthHeight(radioLabels);

			this.selected = selected;
			this.radioLabels = radioLabels;
			CanFocus = true;
		}

		void SetWidthHeight(string[] radioLabels)
		{
			var r = MakeRect(0, 0, radioLabels);
			Width = r.Width;
			Height = radioLabels.Length;
		}

		static Rect MakeRect (int x, int y, string [] radioLabels)
		{
			int width = 0;

			foreach (var s in radioLabels)
				width = Math.Max (s.Length + 4, width);
			return new Rect (x, y, width, radioLabels.Length);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class
		/// setting up the initial set of radio labels and the item that should be selected.
		/// The <see cref="View"/> frame is computed from the provided radio labels.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
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
				Update(value);
				radioLabels = value;
				selected = 0;
				cursor = 0;
				SetNeedsDisplay ();
			}
		}

		void Update(string [] newRadioLabels)
		{
			for (int i = 0; i < radioLabels.Length; i++) {
				Move(0, i);
				Driver.SetAttribute(ColorScheme.Normal);
				Driver.AddStr(new string(' ', radioLabels[i].Length + 4));
			}
			if (newRadioLabels.Length != radioLabels.Length) {
				SetWidthHeight(newRadioLabels);
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			for (int i = 0; i < radioLabels.Length; i++) {
				Move (0, i);
				Driver.SetAttribute (ColorScheme.Normal);
				Driver.AddStr (i == selected ? "(o) " : "( ) ");
				DrawHotString (radioLabels [i], HasFocus && i == cursor, ColorScheme);
			}
			base.Redraw (bounds);
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			Move (1, cursor);
		}

		/// <summary>
		/// Invoked when the selected radio label has changed
		/// </summary>
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

		///<inheritdoc/>
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

		///<inheritdoc/>
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

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked))
				return false;

			SuperView.SetFocus (this);

			if (me.Y < radioLabels.Length) {
				cursor = Selected = me.Y;
				SetNeedsDisplay ();
			}
			return true;
		}
	}
}
