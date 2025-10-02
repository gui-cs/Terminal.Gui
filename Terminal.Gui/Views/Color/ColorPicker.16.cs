#nullable enable

namespace Terminal.Gui.Views;

/// <summary>A sinple color picker that supports the legacy 16 ANSI colors</summary>
public class ColorPicker16 : View
{
    /// <summary>Initializes a new instance of <see cref="ColorPicker16"/>.</summary>
    public ColorPicker16 () { SetInitialProperties (); }

    /// <summary>Columns of color boxes</summary>
    private const int COLS = 8;

    /// <summary>Rows of color boxes</summary>
    private const int ROWS = 2;

    private int _boxHeight = 2;
    private int _boxWidth = 4;
    private int _selectColorIndex = (int)Color.Black;

    /// <summary>Height of a color box</summary>
    public int BoxHeight
    {
        get => _boxHeight;
        set
        {
            if (_boxHeight != value)
            {
                _boxHeight = value;
                Width = Dim.Auto (minimumContentDim: _boxWidth * COLS);
                Height = Dim.Auto (minimumContentDim: _boxHeight * ROWS);
                SetContentSize (new (_boxWidth * COLS, _boxHeight * ROWS));
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
                Width = Dim.Auto (minimumContentDim: _boxWidth * COLS);
                Height = Dim.Auto (minimumContentDim: _boxHeight * ROWS);
                SetContentSize (new (_boxWidth * COLS, _boxHeight * ROWS));
                SetNeedsLayout ();
            }
        }
    }

    /// <summary>Fired when a color is picked.</summary>
    public event EventHandler<ResultEventArgs<Color>>? ColorChanged;

    /// <summary>Cursor for the selected color.</summary>
    public Point Cursor
    {
        get => new (_selectColorIndex % COLS, _selectColorIndex / COLS);
        set
        {
            int colorIndex = value.Y * COLS + value.X;
            SelectedColor = (ColorName16)colorIndex;
        }
    }

    /// <summary>Moves the selected item index to the next row.</summary>
    /// <returns></returns>
    private bool MoveDown (ICommandContext? commandContext)
    {
        if (RaiseSelecting (commandContext) == true)
        {
            return true;
        }
        if (Cursor.Y < ROWS - 1)
        {
            SelectedColor += COLS;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the previous column.</summary>
    /// <returns></returns>
    private bool MoveLeft (ICommandContext? commandContext)
    {
        if (RaiseSelecting (commandContext) == true)
        {
            return true;
        }

        if (Cursor.X > 0)
        {
            SelectedColor--;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the next column.</summary>
    /// <returns></returns>
    private bool MoveRight (ICommandContext? commandContext)
    {
        if (RaiseSelecting (commandContext) == true)
        {
            return true;
        }
        if (Cursor.X < COLS - 1)
        {
            SelectedColor++;
        }

        return true;
    }

    /// <summary>Moves the selected item index to the previous row.</summary>
    /// <returns></returns>
    private bool MoveUp (ICommandContext? commandContext)
    {
        if (RaiseSelecting (commandContext) == true)
        {
            return true;
        }
        if (Cursor.Y > 0)
        {
            SelectedColor -= COLS;
        }

        return true;
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        SetAttribute (HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal));
        var colorIndex = 0;

        for (var y = 0; y < Math.Max (2, Viewport.Height / BoxHeight); y++)
        {
            for (var x = 0; x < Math.Max (8, Viewport.Width / BoxWidth); x++)
            {
                int foregroundColorIndex = y == 0 ? colorIndex + COLS : colorIndex - COLS;

                if (foregroundColorIndex > 15 || colorIndex > 15)
                {
                    continue;
                }

                if (Enabled)
                {
                    SetAttribute (new ((ColorName16)foregroundColorIndex, (ColorName16)colorIndex));
                }
                else
                {
                    SetAttribute (new ((ColorName16)foregroundColorIndex, ((Color)(ColorName16)colorIndex).GetDimColor (), TextStyle.Faint));
                }

                bool selected = x == Cursor.X && y == Cursor.Y;
                DrawColorBox (x, y, selected);
                colorIndex++;
            }
        }

        return true;
    }

    /// <summary>Selected color.</summary>
    public ColorName16 SelectedColor
    {
        get => (ColorName16)_selectColorIndex;
        set
        {
            if (value == (ColorName16)_selectColorIndex)
            {
                return;
            }

            _selectColorIndex = (int)value;

            ColorChanged?.Invoke (
                                  this,
                                  new (value)
                                 );
            SetNeedsDraw ();
        }
    }

    /// <summary>Add the commands.</summary>
    private void AddCommands ()
    {
        AddCommand (Command.Left, (ctx) => MoveLeft (ctx));
        AddCommand (Command.Right, (ctx) => MoveRight (ctx));
        AddCommand (Command.Up, (ctx) => MoveUp (ctx));
        AddCommand (Command.Down, (ctx) => MoveDown (ctx));

        AddCommand (Command.Select, (ctx) =>
                                    {
                                        var set = false;

                                        if (ctx is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } } mouseCommandContext)
                                        {
                                            Cursor = new (mouseCommandContext.Binding.MouseEventArgs.Position.X / _boxWidth, mouseCommandContext.Binding.MouseEventArgs.Position.Y / _boxHeight);
                                            set = true;
                                        }
                                        return RaiseAccepting (ctx) == true || set;
                                    });
    }

    /// <summary>Add the KeyBindings.</summary>
    private void AddKeyBindings ()
    {
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
    }

    // TODO: Decouple Cursor from SelectedColor so that mouse press-and-hold can show the color under the cursor.

    /// <summary>Draw a box for one color.</summary>
    /// <param name="x">X location.</param>
    /// <param name="y">Y location</param>
    /// <param name="selected"></param>
    private void DrawColorBox (int x, int y, bool selected)
    {
        for (var zoomedY = 0; zoomedY < BoxHeight; zoomedY++)
        {
            for (var zoomedX = 0; zoomedX < BoxWidth; zoomedX++)
            {
                AddRune (x * BoxWidth + zoomedX, y * BoxHeight + zoomedY, (Rune)' ');
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

    private void SetInitialProperties ()
    {
        HighlightStates = ViewBase.MouseState.PressedOutside | ViewBase.MouseState.Pressed;

        CanFocus = true;
        AddCommands ();
        AddKeyBindings ();

        Width = Dim.Auto (minimumContentDim: _boxWidth * COLS);
        Height = Dim.Auto (minimumContentDim: _boxHeight * ROWS);
        SetContentSize (new (_boxWidth * COLS, _boxHeight * ROWS));
    }
}
