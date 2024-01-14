using System;
using System.Text;

namespace Terminal.Gui;

/// <summary>
/// Event arguments for the <see cref="Color"/> events.
/// </summary>
public class ColorEventArgs : EventArgs {
	/// <summary>
	/// Initializes a new instance of <see cref="ColorEventArgs"/>
	/// </summary>
	public ColorEventArgs () { }

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
/// The <see cref="ColorPicker"/> <see cref="View"/> Color picker.
/// </summary>
public class ColorPicker : View {
	int _boxHeight = 2;
	int _boxWidth = 4;


	/// <summary>
	/// Columns of color boxes
	/// </summary>
	readonly int _cols = 8;

	/// <summary>
	/// Rows of color boxes
	/// </summary>
	readonly int _rows = 2;

	int _selectColorIndex = (int)Color.Black;

	/// <summary>
	/// Initializes a new instance of <see cref="ColorPicker"/>.
	/// </summary>
	public ColorPicker () => SetInitialProperties ();

	/// <summary>
	/// Width of a color box
	/// </summary>
	public int BoxWidth {
		get => _boxWidth;
		set {
			if (_boxWidth != value) {
				_boxWidth = value;
				SetNeedsLayout ();
			}
		}
	}

	/// <summary>
	/// Height of a color box
	/// </summary>
	public int BoxHeight {
		get => _boxHeight;
		set {
			if (_boxHeight != value) {
				_boxHeight = value;
				SetNeedsLayout ();
			}
		}
	}

	/// <summary>
	/// Cursor for the selected color.
	/// </summary>
	public Point Cursor {
		get => new (_selectColorIndex % _cols, _selectColorIndex / _cols);
		set {
			var colorIndex = value.Y * _cols + value.X;
			SelectedColor = (ColorName)colorIndex;
		}
	}

	/// <summary>
	/// Selected color.
	/// </summary>
	public ColorName SelectedColor {
		get => (ColorName)_selectColorIndex;
		set {
			var prev = (ColorName)_selectColorIndex;
			_selectColorIndex = (int)value;
			ColorChanged?.Invoke (this, new ColorEventArgs {
				PreviousColor = new Color (prev),
				Color = new Color (value)
			});
			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Fired when a color is picked.
	/// </summary>
	public event EventHandler<ColorEventArgs> ColorChanged;

	void SetInitialProperties ()
	{
		CanFocus = true;
		AddCommands ();
		AddKeyBindings ();
		LayoutStarted += (o, a) => {
			var thickness = GetAdornmentsThickness ();
			Width = _cols * BoxWidth + thickness.Vertical;
			Height = _rows * BoxHeight + thickness.Horizontal;
		};
	}

	/// <summary>
	/// Add the commands.
	/// </summary>
	void AddCommands ()
	{
		AddCommand (Command.Left, () => MoveLeft ());
		AddCommand (Command.Right, () => MoveRight ());
		AddCommand (Command.LineUp, () => MoveUp ());
		AddCommand (Command.LineDown, () => MoveDown ());
	}

	/// <summary>
	/// Add the KeyBindinds.
	/// </summary>
	void AddKeyBindings ()
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
		var colorIndex = 0;

		for (var y = 0; y < Bounds.Height / BoxHeight; y++) {
			for (var x = 0; x < Bounds.Width / BoxWidth; x++) {
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
	void DrawColorBox (int x, int y, bool selected)
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

	void DrawFocusRect (Rect rect)
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
		if (Cursor.X > 0) {
			SelectedColor--;
		}
		return true;
	}

	/// <summary>
	/// Moves the selected item index to the next column.
	/// </summary>
	/// <returns></returns>
	public virtual bool MoveRight ()
	{
		if (Cursor.X < _cols - 1) {
			SelectedColor++;
		}
		return true;
	}

	/// <summary>
	/// Moves the selected item index to the previous row.
	/// </summary>
	/// <returns></returns>
	public virtual bool MoveUp ()
	{
		if (Cursor.Y > 0) {
			SelectedColor -= _cols;
		}
		return true;
	}

	/// <summary>
	/// Moves the selected item index to the next row.
	/// </summary>
	/// <returns></returns>
	public virtual bool MoveDown ()
	{
		if (Cursor.Y < _rows - 1) {
			SelectedColor += _cols;
		}
		return true;
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
		Cursor = new Point (me.X / _boxWidth, me.Y / _boxHeight);

		return true;
	}

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		return base.OnEnter (view);
	}
}