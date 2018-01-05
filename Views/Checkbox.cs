using System;

namespace Terminal {
	public class CheckBox : View {
		string text;
		int hot_pos = -1;
		char hot_key;

		/// <summary>
		///   Toggled event, raised when the CheckButton is toggled.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the checkbutton is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public event EventHandler Toggled;

		/// <summary>
		///   Public constructor, creates a CheckButton based on
		///   the given text at the given position.
		/// </summary>
		/// <remarks>
		///   The size of CheckButton is computed based on the
		///   text length. This CheckButton is not toggled.
		/// </remarks>
		public CheckBox (int x, int y, string s) : this (x, y, s, false)
		{
		}

		/// <summary>
		///   Public constructor, creates a CheckButton based on
		///   the given text at the given position and a state.
		/// </summary>
		/// <remarks>
		///   The size of CheckButton is computed based on the
		///   text length. 
		/// </remarks>
		public CheckBox (int x, int y, string s, bool is_checked) : base (new Rect (x, y, s.Length + 4, 1))
		{
			Checked = is_checked;
			Text = s;

			CanFocus = true;
		}

		/// <summary>
		///    The state of the checkbox.
		/// </summary>
		public bool Checked { get; set; }

		/// <summary>
		///   The text displayed by this widget.
		/// </summary>
		public string Text {
			get {
				return text;
			}

			set {
				text = value;

				int i = 0;
				hot_pos = -1;
				hot_key = (char)0;
				foreach (char c in text) {
					if (Char.IsUpper (c)) {
						hot_key = c;
						hot_pos = i;
						break;
					}
					i++;
				}
			}
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (HasFocus ? Colors.Base.Focus : Colors.Base.Normal);
			Move (0, 0);
			Driver.AddStr (Checked ? "[x] " : "[ ] ");
			Move (4, 0);
			Driver.AddStr (Text);
			if (hot_pos != -1) {
				Move (4 + hot_pos, 0);
				Driver.SetAttribute (HasFocus ? Colors.Base.HotFocus : Colors.Base.HotNormal);
				Driver.AddCh (hot_key);
			}
		}

		public override void PositionCursor ()
		{
			Move (1, 0);
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			if (kb.KeyValue == ' ') {
				Checked = !Checked;

				if (Toggled != null)
					Toggled (this, EventArgs.Empty);

				SetNeedsDisplay ();
				return true;
			}
			return false;
		}

#if false
		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			if ((ev.ButtonState & Curses.Event.Button1Clicked) != 0){
				Container.SetFocus (this);
				Container.Redraw ();

				Checked = !Checked;
				
				if (Toggled != null)
					Toggled (this, EventArgs.Empty);
				Redraw ();
			}
		}
#endif
	}
}
