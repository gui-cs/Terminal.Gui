using System;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	/// The <see cref="ColorPicker"/> <see cref="View"/> C
	/// olor picker.
	/// </summary>
	public class ColorPicker : View {
		/// <summary>
		/// Number of colors on a line.
		/// </summary>
		private static readonly int colorsPerLine = 8;

		/// <summary>
		/// Number of color lines.
		/// </summary>
		private static readonly int lineCount = 2;

		/// <summary>
		/// Horizontal zoom.
		/// </summary>
		private static readonly int horizontalZoom = 4;

		/// <summary>
		/// Vertical zoom.
		/// </summary>
		private static readonly int verticalZoom = 2;

		// Cursor runes.
		private static readonly Rune [] cursorRunes = new Rune []
		{
			0x250C, 0x2500, 0x2500, 0x2510,
			0x2514, 0x2500, 0x2500, 0x2518
		};

		/// <summary>
		/// Cursor for the selected color.
		/// </summary>
		public Point Cursor { get {
				return new Point (selectColorIndex % colorsPerLine, selectColorIndex / colorsPerLine);
			}

			set {
				var colorIndex = value.Y * colorsPerLine + value.X;
				SelectedColor = (Color)colorIndex;
			}
		}

		/// <summary>
		/// Fired when a color is picked.
		/// </summary>
		public event Action ColorChanged;

		private int selectColorIndex = (int)Color.Black;

		/// <summary>
		/// Selected color.
		/// </summary>
		public Color SelectedColor {
			get {
				return (Color)selectColorIndex;
			}

			set {
				selectColorIndex = (int)value;
				ColorChanged?.Invoke ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> based on the given text, using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public ColorPicker () : this (0, 0, "Color Picker")
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. 
		/// </remarks>
		public ColorPicker (int x, int y, ustring title) : base (new Rect (x, y, 0, 0), title, new Border () { BorderStyle = BorderStyle.Single })
		{
			CanFocus = true;
			Width = colorsPerLine * horizontalZoom;
			Height = lineCount * verticalZoom + 1;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. 
		/// </remarks>
		public ColorPicker (Point point, ustring title) : this (point.X, point.Y, title)
		{
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());
			var colorIndex = 0;

			for (var y = 0; y < (Height.Anchor (0) - 1) / verticalZoom; y++) {
				for (var x = 0; x < Width.Anchor (0) / horizontalZoom; x++) {
					var foregroundColorIndex = y == 0 ? colorIndex + colorsPerLine : colorIndex - colorsPerLine;
					Driver.SetAttribute (Driver.MakeAttribute ((Color)foregroundColorIndex, (Color)colorIndex));
					var selected = x == Cursor.X && y == Cursor.Y;
					DrawColorBox (x, y, selected);
					colorIndex++;
				}
			}
		}

		/// <summary>
		/// Draw a box for one color.
		/// </summary>
		/// <param name="x">X location.</param>
		/// <param name="y">Y location</param>
		/// <param name="selected"></param>
		private void DrawColorBox (int x, int y, bool selected)
		{
			var index = 0;

			for (var zommedY = 0; zommedY < verticalZoom; zommedY++) {
				for (var zommedX = 0; zommedX < horizontalZoom; zommedX++) {
					Move (x * horizontalZoom + zommedX, y * verticalZoom + zommedY + 1);

					if (selected) {
						var character = cursorRunes [index];
						Driver.AddRune (character);
					} else {
						Driver.AddRune (' ');
					}

					index++;
				}
			}
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (kb.Key == Key.CursorLeft) {
				if (Cursor.X > 0) SelectedColor--;
				return true;
			}

			if (kb.Key == Key.CursorRight) {
				if (Cursor.X < colorsPerLine - 1) SelectedColor++;
				return true;
			}

			if (kb.Key == Key.CursorUp) {
				if (Cursor.Y > 0) SelectedColor -= colorsPerLine;
				return true;
			}

			if (kb.Key == Key.CursorDown) {
				if (Cursor.Y < lineCount - 1) SelectedColor += colorsPerLine;
				return true;
			}

			return base.ProcessKey (kb);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) || !CanFocus)
				return false;

			SetFocus ();
			Cursor = new Point(me.X / horizontalZoom, (me.Y - 1) / verticalZoom);

			return true;
		}
	}
}
