namespace Terminal.Gui;

/// <summary>Event arguments for the <see cref="Color"/> events.</summary>
public class ColorEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="ColorEventArgs"/></summary>
    public ColorEventArgs () { }

    /// <summary>The new Thickness.</summary>
    public Color Color { get; set; }

    /// <summary>The previous Thickness.</summary>
    public Color PreviousColor { get; set; }
}

/// <summary>The <see cref="ColorPicker"/> <see cref="View"/> Color picker.</summary>
public class ColorPicker : View
{
    /// <summary>Columns of color boxes</summary>
    private readonly int _cols = 8;

    /// <summary>Rows of color boxes</summary>
    private readonly int _rows = 2;

    private int _boxHeight = 2;
    private int _boxWidth = 4;
    private int _selectColorIndex = (int)Color.Black;

    /// <summary>Initializes a new instance of <see cref="ColorPicker"/>.</summary>
    public ColorPicker () { SetInitialProperties (); }

    private void SetInitialProperties ()
    {
        HighlightStyle = Gui.HighlightStyle.PressedOutside | Gui.HighlightStyle.Pressed;

        CanFocus = true;
        AddCommands ();
        AddKeyBindings ();

        LayoutStarted += (o, a) =>
                         {
                             Thickness thickness = GetAdornmentsThickness ();
                             Width = _cols * BoxWidth + thickness.Vertical;
                             Height = _rows * BoxHeight + thickness.Horizontal;
                         };
//        MouseEvent += ColorPicker_MouseEvent;
        MouseClick += ColorPicker_MouseClick;
    }

    // TODO: Decouple Cursor from SelectedColor so that mouse press-and-hold can show the color under the cursor.
    //private void ColorPicker_MouseEvent (object sender, MouseEventEventArgs me)
    //{
    //    if (me.MouseEvent.X > Bounds.Width || me.MouseEvent.Y > Bounds.Height)
    //    {
    //        me.Handled = true;

    //        return;
    //    }

    //    me.Handled = true;

    //    return;
    //}

    private void ColorPicker_MouseClick (object sender, MouseEventEventArgs me)
    {
        if (CanFocus)
        {
            Cursor = new Point (me.MouseEvent.X / _boxWidth, me.MouseEvent.Y / _boxHeight);
            SetFocus ();
            me.Handled = true;
        }
    }

    /// <summary>Height of a color box</summary>
    public int BoxHeight
    {
        get => _boxHeight;
        set
        {
            if (_boxHeight != value)
            {
                _boxHeight = value;
                SetNeedsLayout ();
            }
        }
    }

    /// <summary>Width of a color box</summary>
    public int BoxWidth
    {
        get => _boxWidth;
        set
        {
            if (_boxWidth != value)
            {
                _boxWidth = value;
                SetNeedsLayout ();
            }
        }
    }

    /// <summary>Cursor for the selected color.</summary>
    public Point Cursor
    {
        get => new (_selectColorIndex % _cols, _selectColorIndex / _cols);
        set
        {
            int colorIndex = value.Y * _cols + value.X;
            SelectedColor = (ColorName)colorIndex;
        }
    }

    /// <summary>Selected color.</summary>
    public ColorName SelectedColor
    {
        get => (ColorName)_selectColorIndex;
        set
        {
            var prev = (ColorName)_selectColorIndex;
            _selectColorIndex = (int)value;

            ColorChanged?.Invoke (
                                  this,
                                  new ColorEventArgs { PreviousColor = new Color (prev), Color = new Color (value) }
                                 );
            SetNeedsDisplay ();
        }
    }

    /// <summary>Fired when a color is picked.</summary>
    public event EventHandler<ColorEventArgs> ColorChanged;


    /// <summary>Moves the selected item index to the previous column.</summary>
    /// <returns></returns>
    public virtual bool MoveLeft ()
    {
        if (Cursor.X > 0)
        {
            SelectedColor--;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the next column.</summary>
    /// <returns></returns>
    public virtual bool MoveRight ()
    {
        if (Cursor.X < _cols - 1)
        {
            SelectedColor++;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the previous row.</summary>
    /// <returns></returns>
    public virtual bool MoveUp ()
    {
        if (Cursor.Y > 0)
        {
            SelectedColor -= _cols;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the next row.</summary>
    /// <returns></returns>
    public virtual bool MoveDown ()
    {
        if (Cursor.Y < _rows - 1)
        {
            SelectedColor += _cols;
        }

        return true;
    }

    ///<inheritdoc/>
    public override void OnDrawContent (Rectangle contentArea)
    {
        base.OnDrawContent (contentArea);

        Driver.SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());
        var colorIndex = 0;

        for (var y = 0; y < Bounds.Height / BoxHeight; y++)
        {
            for (var x = 0; x < Bounds.Width / BoxWidth; x++)
            {
                int foregroundColorIndex = y == 0 ? colorIndex + _cols : colorIndex - _cols;
                Driver.SetAttribute (new Attribute ((ColorName)foregroundColorIndex, (ColorName)colorIndex));
                bool selected = x == Cursor.X && y == Cursor.Y;
                DrawColorBox (x, y, selected);
                colorIndex++;
            }
        }
    }

    ///<inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <summary>Add the commands.</summary>
    private void AddCommands ()
    {
        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.Right, () => MoveRight ());
        AddCommand (Command.LineUp, () => MoveUp ());
        AddCommand (Command.LineDown, () => MoveDown ());
    }

    /// <summary>Add the KeyBindinds.</summary>
    private void AddKeyBindings ()
    {
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorUp, Command.LineUp);
        KeyBindings.Add (Key.CursorDown, Command.LineDown);
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
                        LineStyle.Dotted
                       );

            lc.AddLine (rect.Location, rect.Height, Orientation.Vertical, LineStyle.Dotted);

            lc.AddLine (
                        rect.Location with { X = rect.Location.X + rect.Width - 1 },
                        rect.Height,
                        Orientation.Vertical,
                        LineStyle.Dotted
                       );
        }

        foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
        {
            AddRune (p.Key.X, p.Key.Y, p.Value);
        }
    }
}
