using NStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="RadioGroup"/> shows a group of radio labels, only one of those can be selected at a given time
	/// </summary>
	public class RadioGroup : View {
		int selected = -1;
		int cursor;

		void Init(Rect rect, ustring [] radioLabels, int selected)
		{
			if (radioLabels == null) {
				this.radioLabels = new List<ustring>();
			} else {
				this.radioLabels = radioLabels.ToList ();
			}
			
			this.selected = selected;
			SetWidthHeight (this.radioLabels);
			CanFocus = true;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public RadioGroup () : this (radioLabels: new ustring [] { }) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The index of the item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (ustring [] radioLabels, int selected = 0) : base ()
		{
			Init (Rect.Empty, radioLabels, selected);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="rect">Boundaries for the radio group.</param>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The index of item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (Rect rect, ustring [] radioLabels, int selected = 0) : base (rect)
		{
			Init (rect, radioLabels, selected);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// The <see cref="View"/> frame is computed from the provided radio labels.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The item to be selected, the value is clamped to the number of items.</param>		
		public RadioGroup (int x, int y, ustring [] radioLabels, int selected = 0) : 
			this (MakeRect (x, y, radioLabels != null ? radioLabels.ToList() : null), radioLabels, selected) { }

		/// <summary>
		/// The location of the cursor in the <see cref="RadioGroup"/>
		/// </summary>
		public int Cursor {
			get => cursor;
			set {
				if (cursor < 0 || cursor >= radioLabels.Count)
					return;
				cursor = value;
				SetNeedsDisplay ();
			}
		}

		void SetWidthHeight (List<ustring> radioLabels)
		{
			var r = MakeRect(0, 0, radioLabels);
			if (LayoutStyle == LayoutStyle.Computed) {
				Width = r.Width;
				Height = radioLabels.Count;
			} else {
				Frame = new Rect (Frame.Location, new Size (r.Width, radioLabels.Count));
			}
		}

		static Rect MakeRect (int x, int y, List<ustring> radioLabels)
		{
			int width = 0;

			if (radioLabels == null) {
				return new Rect (x, y, width, 0);
			}

			foreach (var s in radioLabels)
				width = Math.Max (s.Length + 3, width);
			return new Rect (x, y, width, radioLabels.Count);
		}


		List<ustring> radioLabels = new List<ustring> ();

		/// <summary>
		/// The radio labels to display
		/// </summary>
		/// <value>The radio labels.</value>
		public ustring [] RadioLabels { 
			get => radioLabels.ToArray();
			set {
				var prevCount = radioLabels.Count;
				radioLabels = value.ToList ();
				if (prevCount != radioLabels.Count) {
					SetWidthHeight (radioLabels);
				}
				SelectedItem = 0;
				cursor = 0;
				SetNeedsDisplay ();
			}
		}

		//// Redraws the RadioGroup 
		//void Update(List<ustring> newRadioLabels)
		//{
		//	for (int i = 0; i < radioLabels.Count; i++) {
		//		Move(0, i);
		//		Driver.SetAttribute(ColorScheme.Normal);
		//		Driver.AddStr(ustring.Make(new string (' ', radioLabels[i].Length + 4)));
		//	}
		//	if (newRadioLabels.Count != radioLabels.Count) {
		//		SetWidthHeight(newRadioLabels);
		//	}
		//}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (ColorScheme.Normal);
			Clear ();
			for (int i = 0; i < radioLabels.Count; i++) {
				Move (0, i);
				Driver.SetAttribute (ColorScheme.Normal);
				Driver.AddStr (ustring.Make(new Rune[] { (i == selected ? Driver.Selected : Driver.UnSelected), ' '}));
				DrawHotString (radioLabels [i], HasFocus && i == cursor, ColorScheme);
			}
			base.Redraw (bounds);
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			Move (0, cursor);
		}

		// TODO: Make this a global class
		/// <summary>
		/// Event arguments for the SelectedItemChagned event.
		/// </summary>
		public class SelectedItemChangedArgs : EventArgs {
			/// <summary>
			/// Gets the index of the item that was previously selected. -1 if there was no previous selection.
			/// </summary>
			public int PreviousSelectedItem { get;  }

			/// <summary>
			/// Gets the index of the item that is now selected. -1 if there is no selection.
			/// </summary>
			public int SelectedItem { get; }

			/// <summary>
			/// Initializes a new <see cref="SelectedItemChangedArgs"/> class.
			/// </summary>
			/// <param name="selectedItem"></param>
			/// <param name="previousSelectedItem"></param>
			public SelectedItemChangedArgs(int selectedItem, int previousSelectedItem)
			{
				PreviousSelectedItem = previousSelectedItem;
				SelectedItem = selectedItem;
			}
		}

		/// <summary>
		/// Invoked when the selected radio label has changed.
		/// </summary>
		public Action<SelectedItemChangedArgs> SelectedItemChanged;

		/// <summary>
		/// The currently selected item from the list of radio labels
		/// </summary>
		/// <value>The selected.</value>
		public int SelectedItem {
			get => selected;
			set {
				OnSelectedItemChanged (value, SelectedItem);
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Called whenever the current selected item changes. Invokes the <see cref="SelectedItemChanged"/> event.
		/// </summary>
		/// <param name="selectedItem"></param>
		/// <param name="previousSelectedItem"></param>
		public virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem)
		{
			selected = selectedItem;
			SelectedItemChanged?.Invoke (new SelectedItemChangedArgs (selectedItem, previousSelectedItem));
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
								SelectedItem = i;
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
				if (cursor + 1 < radioLabels.Count) {
					cursor++;
					SetNeedsDisplay ();
					return true;
				}
				break;
			case Key.Space:
				SelectedItem = cursor;
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

			if (me.Y < radioLabels.Count) {
				cursor = SelectedItem = me.Y;
				SetNeedsDisplay ();
			}
			return true;
		}
	}
}
