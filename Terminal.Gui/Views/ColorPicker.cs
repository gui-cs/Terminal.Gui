using System;
using System.Drawing;
using System.Reflection;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Terminal.Gui.SpinnerStyle;

namespace Terminal.Gui {

	/// <summary>
	/// Event arguments for the <see cref="Color"/> events.
	/// </summary>
	public class ColorEventArgs : EventArgs {

		/// <summary>
		/// Initializes a new instance of <see cref="ColorEventArgs"/>
		/// </summary>
		public ColorEventArgs ()
		{
		}

		/// <summary>
		/// The new Thickness.
		/// </summary>
		public Color Color { get; set; }

		/// <summary>
		/// The previous Thickness.
		/// </summary>
		public Color PreviousColor { get; set; }
	}

	/// <summary>
	/// ColorPicker view styles.
	/// </summary>
	public enum ColorPickerStyle {
		/// <summary>
		/// The color picker will display the 16 ANSI colors.
		/// </summary>
		Ansi,
		/// <summary>
		/// The color picker will display the 256 colors.
		/// </summary>
		Color256,
		/// <summary>
		/// The color picker will display the 16 ANSI colors and the 256 colors.
		/// </summary>
		AnsiAndColor256,

		/// <summary>
		/// The color picker will display the 24-bit colors.
		/// </summary>
		Rgb,

		/// <summary>
		/// The color picker will display the 16 ANSI colors and the 256 colors and the 24-bit colors.
		/// </summary>
		AnsiAndColor256AndRgb
	}

	/// <summary>
	/// The <see cref="ColorPicker"/> <see cref="View"/> Color picker.
	/// </summary>
	public class ColorPicker : View {
		private Color _selectedColor = new Color (Color.Black);

		/// <summary>
		/// Gets or sets the style of the color picker.
		/// </summary>
		public ColorPickerStyle Style { get; set; } = ColorPickerStyle.Ansi;

		/// <summary>
		/// Columns of color boxes
		/// </summary>
		private int _cols = 8;

		/// <summary>
		/// Rows of color boxes
		/// </summary>
		private int _rows = 2;

		/// <summary>
		/// Width of a color box.
		/// </summary>
		public int BoxWidth {
			get => _boxWidth;
			set {
				if (_boxWidth != value) {
					_boxWidth = value;
					Bounds = new Rect (Bounds.Location, new Size (_cols * BoxWidth, _rows * BoxHeight));
				}
			}
		}
		private int _boxWidth = 4;

		/// <summary>
		/// Height of a color box
		/// </summary>
		public int BoxHeight {
			get => _boxHeight;
			set {
				if (_boxHeight != value) {
					_boxHeight = value;
					Bounds = new Rect (Bounds.Location, new Size (_cols * BoxWidth, _rows * BoxHeight));
				}
			}
		}
		int _boxHeight = 2;

		/// <summary>
		/// Cursor for the selected color.
		/// </summary>
		public Point Cursor {
			get {
				switch (Style) {
				case ColorPickerStyle.Ansi:
					var index = (int)_selectedColor.ColorName;
					return new Point (index % _cols, index / _cols);
				case ColorPickerStyle.Rgb:
					int x = (int)(_selectedColor.R * Bounds.Width / 255);
					int y = (int)(_selectedColor.G * Bounds.Height / 255);
					return new Point (x, y);
				}
				return new Point ();
			}

			set {
				switch (Style) {
				case ColorPickerStyle.Ansi:
					SelectedColor = (Color)(ColorName)(value.Y * _cols + value.X);
					break;
				case ColorPickerStyle.Rgb:
					SelectedColor = new Color ((byte)(value.X * 255 / Bounds.Width), (byte)(value.Y * 255 / Bounds.Height), 0);
					break;
				}
			}
		}

		/// <summary>
		/// Fired when a color is picked.
		/// </summary>
		public event EventHandler<ColorEventArgs> ColorChanged;

		/// <summary>
		/// Selected color.
		/// </summary>
		public Color SelectedColor {
			get => _selectedColor;

			set {
				if (_selectedColor != value) {
					var oldColor = _selectedColor;
					_selectedColor = value;
					ColorChanged?.Invoke (this, new ColorEventArgs () { Color = _selectedColor, PreviousColor = oldColor });
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ColorPicker"/>.
		/// </summary>
		public ColorPicker ()
		{
			SetInitialProperties ();
		}

		private void SetInitialProperties ()
		{
			CanFocus = true;
			AddCommands ();
			AddKeyBindings ();
			LayoutStarted += (o, a) => {
				switch (Style) {
				case ColorPickerStyle.Rgb:
					//Bounds = new Rect (Bounds.Location, new Size (32, 32));
					break;
				case ColorPickerStyle.Ansi:
				default:
					Bounds = new Rect (Bounds.Location, new Size (_cols * BoxWidth, _rows * BoxHeight));
					break;
				}
			};
		}

		/// <summary>
		/// Add the commands.
		/// </summary>
		private void AddCommands ()
		{
			AddCommand (Command.Left, () => MoveLeft ());
			AddCommand (Command.Right, () => MoveRight ());
			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.LineDown, () => MoveDown ());
		}

		/// <summary>
		/// Add the KeyBindinds.
		/// </summary>
		private void AddKeyBindings ()
		{
			KeyBindings.Add (KeyCode.CursorLeft, Command.Left);
			KeyBindings.Add (KeyCode.CursorRight, Command.Right);
			KeyBindings.Add (KeyCode.CursorUp, Command.LineUp);
			KeyBindings.Add (KeyCode.CursorDown, Command.LineDown);
		}

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			base.OnDrawContent (contentArea);

			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());

			switch (Style) {
			case ColorPickerStyle.Rgb:
				DrawRgbGraident (contentArea);
				break;
			case ColorPickerStyle.Ansi:
			default:
				DrawAnsi ();
				break;
			}

		}

		int _blueValue = 100;

		public int BlueValue {
			get { return _blueValue; }
			set {
				_blueValue = value;
				SetNeedsDisplay ();
			}
		}

		void DrawHSLGraident (Rect contentArea)
		{
			for (int x = 0; x < contentArea.Width; x++) {
				for (int y = 0; y < contentArea.Height; y++) {
					float ratioX = (float)x / (contentArea.Width - 1);
					float ratioY = (float)y / (contentArea.Height - 1);

					int red = (int)(255 * ratioX);
					int green = (int)(255 * ratioY);
					int blue = _blueValue; // We're displaying a slice of blue value

					Color color = new Color (red, green, blue);

					DrawBox (x, y, color);
				}
			}
		}

		void DrawRgbGraident (Rect contentArea)
		{
			for (int x = 0; x < Bounds.Width; x++) {
				for (int y = 0; y < Bounds.Height; y++) {
					// Map x and y to their corresponding RGB values
					int redValue = MapValue (x, 0, Bounds.Width, 0, 255);
					int greenValue = MapValue (y, 0, Bounds.Height, 0, 255);

					var color = new Color (redValue, greenValue, _blueValue);

					DrawBox (x, y, color);
				}
			}
		}

		private int MapValue (int value, int fromLow, int fromHigh, int toLow, int toHigh)
		{
			return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
		}

		private void DrawBox (int x, int y, Color color)
		{
			// Placeholder method; you'll replace this with the actual Terminal.Gui method to set cell colors
			// This assumes that `Color` can take RGB values directly, adjust as needed
			Driver.SetAttribute (new Attribute (color));
			AddRune (x, y, (Rune)' ');
		}

		void DrawAnsi ()
		{
			var colorIndex = 0;

			for (var y = 0; y < (Bounds.Height / BoxHeight); y++) {
				for (var x = 0; x < (Bounds.Width / BoxWidth); x++) {
					var foregroundColorIndex = y == 0 ? colorIndex + _cols : colorIndex - _cols;
					Driver.SetAttribute (new Attribute ((ColorName)foregroundColorIndex, (ColorName)colorIndex));
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

			for (var zoomedY = 0; zoomedY < BoxHeight; zoomedY++) {
				for (var zoomedX = 0; zoomedX < BoxWidth; zoomedX++) {
					Move (x * BoxWidth + zoomedX, y * BoxHeight + zoomedY);
					Driver.AddRune ((Rune)' ');
					index++;
				}
			}

			if (selected) {
				DrawFocusRect (new Rect (x * BoxWidth, y * BoxHeight, BoxWidth, BoxHeight));
			}
		}

		private void DrawFocusRect (Rect rect)
		{
			var lc = new LineCanvas ();
			if (rect.Width == 1) {
				lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);
			} else if (rect.Height == 1) {
				lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);
			} else {
				lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);
				lc.AddLine (new Point (rect.Location.X, rect.Location.Y + rect.Height - 1), rect.Width, Orientation.Horizontal, LineStyle.Dotted);

				lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);
				lc.AddLine (new Point (rect.Location.X + rect.Width - 1, rect.Location.Y), rect.Height, Orientation.Vertical, LineStyle.Dotted);
			}
			foreach (var p in lc.GetMap ()) {
				AddRune (p.Key.X, p.Key.Y, p.Value);
			}
		}

		/// <summary>
		/// Moves the selected item index to the previous column.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveLeft ()
		{
			switch (Style) {
			case ColorPickerStyle.Ansi:
				var index = (int)SelectedColor.ColorName;
				if (index > 0 && index <= 15) {
					SelectedColor = new Color ((ColorName)(index - 1));
				}
				return true;
			case ColorPickerStyle.Rgb:
				//return MoveLeftRgb ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Moves the selected item index to the next column.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveRight ()
		{
			switch (Style) {
			case ColorPickerStyle.Ansi:
				var index = (int)SelectedColor.ColorName;
				if (index >= 0 && index < 15) {
					SelectedColor = new Color ((ColorName)(index + 1));
				}
				return true;
			case ColorPickerStyle.Rgb:
				//return MoveLeftRgb ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Moves the selected item index to the previous row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveUp ()
		{
			switch (Style) {
			case ColorPickerStyle.Ansi:
				var index = (int)SelectedColor.ColorName - _cols;
				if (index >= 0 && index <= 15) {
					SelectedColor = new Color ((ColorName)index);
				}
				return true;
			case ColorPickerStyle.Rgb:
				//return MoveLeftRgb ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Moves the selected item index to the next row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveDown ()
		{
			switch (Style) {
			case ColorPickerStyle.Ansi:
				var index = (int)SelectedColor.ColorName + _cols;
				if (index >= 0 && index <= 15) {
					SelectedColor = new Color ((ColorName)index);
				}
				return true;
			case ColorPickerStyle.Rgb:
				//return MoveLeftRgb ();
				return true;
			}
			return false;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) || !CanFocus) {
				return false;
			}

			SetFocus ();
			if (me.X > Bounds.Width || me.Y > Bounds.Height) {
				return true;
			}
			//var x = Math.Max (GetFramesThickness().Left, me.X);
			//var y = Math.Max (GetFramesThickness ().Top, me.Y);
			switch (Style) {
			case ColorPickerStyle.Ansi:
				Cursor = new Point ((me.X - GetFramesThickness ().Left) / _boxWidth, (me.Y - GetFramesThickness ().Top) / _boxHeight);
				break;
			case ColorPickerStyle.Rgb:
				Cursor = new Point ((me.X - GetFramesThickness ().Left), (me.Y - GetFramesThickness ().Top));
				break;
			}


			return true;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}

	public class GradientView : View {

		int _blueValue = 100;

		public int BlueValue {
			get { return _blueValue; }
			set {
				_blueValue = value;
				SetNeedsDisplay ();
			}
		}

		public override void OnDrawContent (Rect contentArea)
		{
			base.OnDrawContent (contentArea);

			DrawRgbGraident (contentArea);
		}

		void DrawHSLGraident (Rect contentArea)
		{
			for (int x = 0; x < contentArea.Width; x++) {
				for (int y = 0; y < contentArea.Height; y++) {
					float ratioX = (float)x / (contentArea.Width - 1);
					float ratioY = (float)y / (contentArea.Height - 1);

					int red = (int)(255 * ratioX);
					int green = (int)(255 * ratioY);
					int blue = _blueValue; // We're displaying a slice of blue value

					Color color = new Color (red, green, blue);

					DrawBox (x, y, color);
				}
			}
		}

		void DrawRgbGraident (Rect contentArea)
		{
			for (int x = 0; x < Bounds.Width; x++) {
				for (int y = 0; y < Bounds.Height; y++) {
					// Map x and y to their corresponding RGB values
					int redValue = MapValue (x, 0, Bounds.Width, 0, 255);
					int greenValue = MapValue (y, 0, Bounds.Height, 0, 255);

					var color = new Color (redValue, greenValue, _blueValue);

					DrawBox (x, y, color);
				}
			}
		}

		private int MapValue (int value, int fromLow, int fromHigh, int toLow, int toHigh)
		{
			return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
		}

		private void DrawBox (int x, int y, Color color)
		{
			// Placeholder method; you'll replace this with the actual Terminal.Gui method to set cell colors
			// This assumes that `Color` can take RGB values directly, adjust as needed
			Driver.SetAttribute (new Attribute (color));
			AddRune (x, y, (Rune)' ');
		}
	}

}