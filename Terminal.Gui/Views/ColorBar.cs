#nullable enable

namespace Terminal.Gui;

/// <summary>
///     A bar representing a single component of a <see cref="Color"/> e.g.
///     the Red portion of a <see cref="ColorModel.RGB"/>.
/// </summary>
internal abstract class ColorBar : View, IColorBar
{
    /// <summary>
    ///     Creates a new instance of the <see cref="ColorBar"/> class.
    /// </summary>
    protected ColorBar ()
    {
        Height = 1;
        Width = Dim.Fill ();
        CanFocus = true;

        AddCommand (Command.Left, _ => Adjust (-1));
        AddCommand (Command.Right, _ => Adjust (1));

        AddCommand (Command.LeftExtend, _ => Adjust (-MaxValue / 20));
        AddCommand (Command.RightExtend, _ => Adjust (MaxValue / 20));

        AddCommand (Command.LeftStart, _ => SetZero ());
        AddCommand (Command.RightEnd, _ => SetMax ());

        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);
        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.End, Command.RightEnd);
    }

    /// <summary>
    ///     X coordinate that the bar starts at excluding any label.
    /// </summary>
    private int _barStartsAt;

    /// <summary>
    ///     0-1 for how much of the color element is present currently (HSL)
    /// </summary>
    private int _value;

    /// <summary>
    ///     The amount of <see cref="Value"/> represented by each cell width on the bar
    ///     Can be less than 1 e.g. if Saturation (0-100) and width > 100
    /// </summary>
    private double _cellValue = 1d;

    /// <summary>
    ///     Last known width of the bar as passed to <see cref="DrawBar"/>.
    /// </summary>
    private int _barWidth;

    /// <summary>
    ///     The currently selected amount of the color component stored by this class e.g.
    ///     the amount of Hue in a <see cref="ColorModel.HSL"/>.
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            int clampedValue = Math.Clamp (value, 0, MaxValue);

            if (_value != clampedValue)
            {
                _value = clampedValue;
                OnValueChanged ();
            }
        }
    }

    /// <inheritdoc/>
    void IColorBar.SetValueWithoutRaisingEvent (int v)
    {
        _value = v;
        SetNeedsDisplay ();
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        var xOffset = 0;

        if (!string.IsNullOrWhiteSpace (Text))
        {
            Move (0, 0);
            Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
            Driver.AddStr (Text);

            // TODO: is there a better method than this? this is what it is in TableView
            xOffset = Text.EnumerateRunes ().Sum (c => c.GetColumns ());
        }

        _barWidth = viewport.Width - xOffset;
        _barStartsAt = xOffset;

        DrawBar (xOffset, 0, _barWidth);
    }

    /// <summary>
    ///     Event fired when <see cref="Value"/> is changed to a new value
    /// </summary>
    public event EventHandler<EventArgs<int>>? ValueChanged;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            if (mouseEvent.Position.X >= _barStartsAt)
            {
                double v = MaxValue * ((double)mouseEvent.Position.X - _barStartsAt) / (_barWidth - 1);
                Value = Math.Clamp ((int)v, 0, MaxValue);
            }

            mouseEvent.Handled = true;
            SetFocus ();

        }

        return mouseEvent.Handled;
    }

    /// <summary>
    ///     When overriden in a derived class, returns the <see cref="Color"/> to
    ///     render at <paramref name="fraction"/> proportion of the full bars width.
    ///     e.g. 0.5 fraction of Saturation is 50% because Saturation goes from 0-100.
    /// </summary>
    /// <param name="fraction"></param>
    /// <returns></returns>
    protected abstract Color GetColor (double fraction);

    /// <summary>
    ///     The maximum value allowed for this component e.g. Saturation allows up to 100 as it
    ///     is a percentage while Hue allows up to 360 as it is measured in degrees.
    /// </summary>
    protected abstract int MaxValue { get; }

    /// <summary>
    ///     The last drawn location in View's viewport where the Triangle appeared.
    ///     Used exclusively for tests.
    /// </summary>
    internal int TrianglePosition { get; private set; }

    private bool? Adjust (int delta)
    {
        var change = (int)(delta * _cellValue);

        // Ensure that the change is at least 1 or -1 if delta is non-zero
        if (change == 0 && delta != 0)
        {
            change = delta > 0 ? 1 : -1;
        }

        Value += change;

        return true;
    }

    private void DrawBar (int xOffset, int yOffset, int width)
    {
        // Each 1 unit of X in the bar corresponds to this much of Value
        _cellValue = (double)MaxValue / (width - 1);

        for (var x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            Color color = GetColor (fraction);

            // Adjusted isSelectedCell calculation
            double cellBottomThreshold = (x - 1) * _cellValue;
            double cellTopThreshold = x * _cellValue;

            if (x == width - 1)
            {
                cellTopThreshold = MaxValue;
            }

            bool isSelectedCell = Value > cellBottomThreshold && Value <= cellTopThreshold;

            // Check the brightness of the background color
            double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

            Color triangleColor = Color.Black;

            if (brightness < 0.15) // Threshold to determine if the color is too close to black
            {
                triangleColor = Color.DarkGray;
            }

            if (isSelectedCell)
            {
                // Draw the triangle at the closest position
                Application.Driver?.SetAttribute (new (triangleColor, color));
                AddRune (x + xOffset, yOffset, new ('▲'));

                // Record for tests
                TrianglePosition = x + xOffset;
            }
            else
            {
                Application.Driver?.SetAttribute (new (color, color));
                AddRune (x + xOffset, yOffset, new ('█'));
            }
        }
    }

    private void OnValueChanged ()
    {
        ValueChanged?.Invoke (this, new (in _value));
        SetNeedsDisplay ();
    }

    private bool? SetMax ()
    {
        Value = MaxValue;

        return true;
    }

    private bool? SetZero ()
    {
        Value = 0;

        return true;
    }
}
