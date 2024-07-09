namespace Terminal.Gui;

/// <summary>Event arguments for the <see cref="Color"/> events.</summary>
public class ColorEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="ColorEventArgs"/></summary>
    public ColorEventArgs () { }

    /// <summary>
    ///     The new Color.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    ///     The previous Color.
    /// </summary>
    public Color PreviousColor { get; set; }
}

/// <summary>
///     ColorPicker view styles.
/// </summary>
public enum ColorPickerStyle
{
    /// <summary>
    ///     The color picker will display the 16 ANSI colors.
    /// </summary>
    Ansi,

    /// <summary>
    ///     The color picker will display the 256 colors.
    /// </summary>
    Color256,

    /// <summary>
    ///     The color picker will display the 16 ANSI colors and the 256 colors.
    /// </summary>
    AnsiAndColor256,

    /// <summary>
    ///     The color picker will display the 24-bit colors.
    /// </summary>
    Rgb,

    /// <summary>
    ///     The color picker will display the 16 ANSI colors and the 256 colors and the 24-bit colors.
    /// </summary>
    AnsiAndColor256AndRgb
}

/// <summary>
///     The <see cref="ColorPicker"/> <see cref="View"/> Color picker.
/// </summary>
public class ColorPicker : View
{
    /// <summary>
    ///     Columns of color boxes
    /// </summary>
    private readonly int _cols = 8;

    /// <summary>
    ///     Rows of color boxes
    /// </summary>
    private readonly int _rows = 2;

    private int _selectColorIndex = (int)Color.Black;

    /// <summary>
    ///     Initializes a new instance of <see cref="ColorPicker"/>.
    /// </summary>
    public ColorPicker () { SetInitialProperties (); }

    private void SetInitialProperties ()
    {
        HighlightStyle = HighlightStyle.PressedOutside | HighlightStyle.Pressed;

        CanFocus = true;
        AddCommands ();
        AddKeyBindings ();

        Width = Dim.Auto (minimumContentDim: _boxWidth * _cols);
        Height = Dim.Auto (minimumContentDim: _boxHeight * _rows);
        SetContentSize (new (_boxWidth * _cols, _boxHeight * _rows));

        MouseClick += ColorPicker_MouseClick;
    }

    private int _blueValue = 100;

    public int BlueValue
    {
        get => _blueValue;
        set
        {
            _blueValue = value;
            SetNeedsDisplay ();
        }
    }

    // TODO: Decouple Cursor from SelectedColor so that mouse press-and-hold can show the color under the cursor.

    private void ColorPicker_MouseClick (object sender, MouseEventEventArgs me)
    {
        // if (CanFocus)
        {
            Cursor = new (me.MouseEvent.Position.X / _boxWidth, me.MouseEvent.Position.Y / _boxHeight);
            SetFocus ();
            me.Handled = true;
        }
    }

    private int _boxHeight = 2;

    /// <summary>Height of a color box</summary>
    public int BoxHeight
    {
        get => _boxHeight;
        set
        {
            if (_boxHeight != value)
            {
                _boxHeight = value;
                Width = Dim.Auto (minimumContentDim: _boxWidth * _cols);
                Height = Dim.Auto (minimumContentDim: _boxHeight * _rows);
                SetContentSize (new (_boxWidth * _cols, _boxHeight * _rows));
                SetNeedsLayout ();
            }
        }
    }

    private int _boxWidth = 4;

    /// <summary>Width of a color box</summary>
    public int BoxWidth
    {
        get => _boxWidth;
        set
        {
            if (_boxWidth != value)
            {
                _boxWidth = value;
                Width = Dim.Auto (minimumContentDim: _boxWidth * _cols);
                Height = Dim.Auto (minimumContentDim: _boxHeight * _rows);
                SetContentSize (new (_boxWidth * _cols, _boxHeight * _rows));
                SetNeedsLayout ();
            }
        }
    }

    /// <summary>
    ///     Cursor for the selected color.
    /// </summary>
    public Point Cursor
    {
        get
        {
            switch (Style)
            {
                case ColorPickerStyle.Ansi:
                    var index = (int)_selectedColor.GetClosestNamedColor ();

                    return new (index % _cols, index / _cols);
                case ColorPickerStyle.Rgb:
                    int x = _selectedColor.R * Viewport.Width / 255;
                    int y = _selectedColor.G * Viewport.Height / 255;

                    return new (x, y);
            }

            return new ();
        }

        set
        {
            switch (Style)
            {
                case ColorPickerStyle.Ansi:
                    SelectedColor = (ColorName)(value.Y * _cols + value.X);

                    break;
                case ColorPickerStyle.Rgb:
                    SelectedColor = new (
                                         (byte)(value.X * 255 / Viewport.Width),
                                         (byte)(value.Y * 255 / Viewport.Height));

                    break;
            }
        }
    }

    private Color _selectedColor = new (Color.Black);

    /// <summary>
    ///     Selected color.
    /// </summary>
    public Color SelectedColor
    {
        get => _selectedColor;

        set
        {
            if (_selectedColor != value)
            {
                Color oldColor = _selectedColor;
                _selectedColor = value;

                ColorChanged?.Invoke (
                                      this,
                                      new() { Color = _selectedColor, PreviousColor = oldColor });
                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the style of the color picker.
    /// </summary>
    public ColorPickerStyle Style { get; set; } = ColorPickerStyle.Ansi;

    /// <summary>Fired when a color is picked.</summary>
    public event EventHandler<ColorEventArgs> ColorChanged;

    /// <summary>
    ///     Moves the selected item index to the next row.
    /// </summary>
    /// <returns></returns>
    public virtual bool MoveDown ()
    {
        switch (Style)
        {
            case ColorPickerStyle.Ansi:
                int index = (int)SelectedColor.GetClosestNamedColor () + _cols;

                if (index >= 0 && index <= 15)
                {
                    SelectedColor = new ((ColorName)index);
                }

                return true;
            case ColorPickerStyle.Rgb:
                //return MoveLeftRgb ();
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Moves the selected item to the last color.
    /// </summary>
    /// <returns></returns>
    public virtual bool MoveEnd ()
    {
        switch (Style)
        {
            case ColorPickerStyle.Ansi:
                SelectedColor = new (Enum.GetValues (typeof (ColorName)).Cast<ColorName> ().Last ());

                return true;
            case ColorPickerStyle.Rgb:
                //return MoveLeftRgb ();
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Moves the selected item to the firstcolor.
    /// </summary>
    /// <returns></returns>
    public virtual bool MoveHome ()
    {
        switch (Style)
        {
            case ColorPickerStyle.Ansi:
                SelectedColor = new (Enum.GetValues (typeof (ColorName)).Cast<ColorName> ().First ());

                return true;
            case ColorPickerStyle.Rgb:
                //return MoveLeftRgb ();
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Moves the selected item index to the previous column.
    /// </summary>
    /// <returns></returns>
    public virtual bool MoveLeft ()
    {
        switch (Style)
        {
            case ColorPickerStyle.Ansi:
                var index = (int)SelectedColor.GetClosestNamedColor ();

                if (index > 0 && index <= 15)
                {
                    SelectedColor = new ((ColorName)(index - 1));
                }

                return true;
            case ColorPickerStyle.Rgb:
                //return MoveLeftRgb ();
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Moves the selected item index to the next column.
    /// </summary>
    /// <returns></returns>
    public virtual bool MoveRight ()
    {
        switch (Style)
        {
            case ColorPickerStyle.Ansi:
                var index = (int)SelectedColor.GetClosestNamedColor ();

                if (index >= 0 && index < 15)
                {
                    SelectedColor = new ((ColorName)(index + 1));
                }

                return true;
            case ColorPickerStyle.Rgb:
                //return MoveLeftRgb ();
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Moves the selected item index to the previous row.
    /// </summary>
    /// <returns></returns>
    public virtual bool MoveUp ()
    {
        switch (Style)
        {
            case ColorPickerStyle.Ansi:
                int index = (int)SelectedColor.GetClosestNamedColor () - _cols;

                if (index >= 0 && index <= 15)
                {
                    SelectedColor = new ((ColorName)index);
                }

                return true;
            case ColorPickerStyle.Rgb:
                //return MoveLeftRgb ();
                return true;
        }

        return false;
    }

    ///<inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        Driver.SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());

        switch (Style)
        {
            case ColorPickerStyle.Rgb:
                DrawRgbGradient (viewport);

                break;
            case ColorPickerStyle.Ansi:
            default:
                DrawAnsi ();

                break;
        }
    }

    /// <summary>
    ///     Add the commands.
    /// </summary>
    private void AddCommands ()
    {
        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.Right, () => MoveRight ());
        AddCommand (Command.LineUp, () => MoveUp ());
        AddCommand (Command.LineDown, () => MoveDown ());

        AddCommand (Command.TopHome, () => MoveHome ());
        AddCommand (Command.BottomEnd, () => MoveEnd ());
    }

    /// <summary>
    ///     Add the KeyBindinds.
    /// </summary>
    private void AddKeyBindings ()
    {
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorUp, Command.LineUp);
        KeyBindings.Add (Key.CursorDown, Command.LineDown);

        KeyBindings.Add (Key.Home, Command.TopHome);
        KeyBindings.Add (Key.End, Command.BottomEnd);
    }

    private void DrawAnsi ()
    {
        var colorIndex = 0;

        for (var y = 0; y < Viewport.Height / BoxHeight; y++)
        {
            for (var x = 0; x < Viewport.Width / BoxWidth; x++)
            {
                int foregroundColorIndex = y == 0 ? colorIndex + _cols : colorIndex - _cols;

                Driver.SetAttribute (
                                     new (
                                          (ColorName)foregroundColorIndex,
                                          (ColorName)colorIndex));
                bool selected = x == Cursor.X && y == Cursor.Y;
                DrawColorBox (x, y, selected);
                colorIndex++;
            }
        }
    }

    private void DrawBox (int x, int y, Color color)
    {
        // Placeholder method; you'll replace this with the actual Terminal.Gui method to set cell colors
        // This assumes that `Color` can take RGB values directly, adjust as needed
        Driver.SetAttribute (new (color));
        AddRune (x, y, (Rune)' ');
    }

    /// <summary>Draw a box for one color.</summary>
    /// <param name="x">X location.</param>
    /// <param name="y">Y location</param>
    /// <param name="selected"></param>
    private void DrawColorBox (int x, int y, bool selected)
    {
        var index = 0;

        for (var zoomedY = 0; zoomedY < BoxHeight; zoomedY++)
        {
            for (var zoomedX = 0; zoomedX < BoxWidth; zoomedX++)
            {
                Move (x * BoxWidth + zoomedX, y * BoxHeight + zoomedY);
                Driver.AddRune ((Rune)' ');
                index++;
            }
        }

        if (selected)
        {
            DrawFocusRect (new (x * BoxWidth, y * BoxHeight, BoxWidth, BoxHeight));
        }
    }

    private void DrawFocusRect (Rectangle rect)
    {
        var lc = new LineCanvas ();

        if (rect.Width == 1)
        {
            lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);
        }
        else if (rect.Height == 1)
        {
            lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);
        }
        else
        {
            lc.AddLine (rect.Location, rect.Width, Orientation.Horizontal, LineStyle.Dotted);

            lc.AddLine (
                        rect.Location with { Y = rect.Location.Y + rect.Height - 1 },
                        rect.Width,
                        Orientation.Horizontal,
                        LineStyle.Dotted);

            lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);

            lc.AddLine (
                        rect.Location with { X = rect.Location.X + rect.Width - 1 },
                        rect.Height,
                        Orientation.Vertical,
                        LineStyle.Dotted);
        }

        foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
        {
            AddRune (p.Key.X, p.Key.Y, p.Value);
        }
    }

    private void DrawHSLGradient (Rectangle contentArea)
    {
        for (var x = 0; x < contentArea.Width; x++)
        {
            for (var y = 0; y < contentArea.Height; y++)
            {
                float ratioX = (float)x / (contentArea.Width - 1);
                float ratioY = (float)y / (contentArea.Height - 1);

                var red = (int)(255 * ratioX);
                var green = (int)(255 * ratioY);
                int blue = _blueValue; // We're displaying a slice of blue value

                var color = new Color (red, green, blue);

                DrawBox (x, y, color);
            }
        }
    }

    private void DrawRgbGradient (Rectangle contentArea)
    {
        for (var x = 0; x < Viewport.Width; x++)
        {
            for (var y = 0; y < Viewport.Height; y++)
            {
                // Map x and y to their corresponding RGB values
                int redValue = MapValue (x, 0, Viewport.Width, 0, 255);
                int greenValue = MapValue (y, 0, Viewport.Height, 0, 255);

                var color = new Color (redValue, greenValue, _blueValue);

                DrawBox (x, y, color);
            }
        }
    }

    private int MapValue (int value, int fromLow, int fromHigh, int toLow, int toHigh)
    {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }
}

public class GradientView : View
{
    private int _blueValue = 100;

    public int BlueValue
    {
        get => _blueValue;
        set
        {
            _blueValue = value;
            SetNeedsDisplay ();
        }
    }

    public override void OnDrawContent (Rectangle contentArea)
    {
        base.OnDrawContent (contentArea);

        DrawRgbGradient (contentArea);
    }

    private void DrawBox (int x, int y, Color color)
    {
        // Placeholder method; you'll replace this with the actual Terminal.Gui method to set cell colors
        // This assumes that `Color` can take RGB values directly, adjust as needed
        Driver.SetAttribute (new (color));
        AddRune (x, y, (Rune)' ');
    }

    private void DrawHSLGradient (Rectangle contentArea)
    {
        for (var x = 0; x < contentArea.Width; x++)
        {
            for (var y = 0; y < contentArea.Height; y++)
            {
                float ratioX = (float)x / (contentArea.Width - 1);
                float ratioY = (float)y / (contentArea.Height - 1);

                var red = (int)(255 * ratioX);
                var green = (int)(255 * ratioY);
                int blue = _blueValue; // We're displaying a slice of blue value

                var color = new Color (red, green, blue);

                DrawBox (x, y, color);
            }
        }
    }

    private void DrawRgbGradient (Rectangle contentArea)
    {
        for (var x = 0; x < Viewport.Width; x++)
        {
            for (var y = 0; y < Viewport.Height; y++)
            {
                // Map x and y to their corresponding RGB values
                int redValue = MapValue (x, 0, Viewport.Width, 0, 255);
                int greenValue = MapValue (y, 0, Viewport.Height, 0, 255);

                var color = new Color (redValue, greenValue, _blueValue);

                DrawBox (x, y, color);
            }
        }
    }

    private int MapValue (int value, int fromLow, int fromHigh, int toLow, int toHigh)
    {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }
}
