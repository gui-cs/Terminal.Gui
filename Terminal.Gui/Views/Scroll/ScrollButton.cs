#nullable enable

namespace Terminal.Gui;

internal enum VariationMode
{
    Decrease,
    Increase
}

internal class ScrollButton : View
{
    public ScrollButton (ScrollBar host, VariationMode variation = VariationMode.Decrease)
    {
        _host = host;
        VariationMode = variation;
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;
        Id = "scrollButton";

        //Width = Dim.Auto (DimAutoStyle.Content, 1);
        //Height = Dim.Auto (DimAutoStyle.Content, 1);
        WantContinuousButtonPressed = true;
    }

    private readonly ScrollBar _host;
    private ColorScheme? _savedColorScheme;

    public void AdjustButton ()
    {
        if (!IsInitialized)
        {
            return;
        }

        Width = _host.Orientation == Orientation.Vertical ? Dim.Fill () : 1;
        Height = _host.Orientation == Orientation.Vertical ? 1 : Dim.Fill ();

        switch (VariationMode)
        {
            case VariationMode.Decrease:
                X = 0;
                Y = 0;

                break;
            case VariationMode.Increase:
                X = _host.Orientation == Orientation.Vertical ? 0 : Pos.AnchorEnd (1);
                Y = _host.Orientation == Orientation.Vertical ? Pos.AnchorEnd (1) : 0;

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        SetButtonText ();
    }

    /// <inheritdoc/>
    public override Attribute GetNormalColor ()
    {
        if (_savedColorScheme is null)
        {
            ColorScheme = new () { Normal = new (_host.ColorScheme.HotNormal.Foreground, _host.ColorScheme.HotNormal.Background) };
        }
        else
        {
            ColorScheme = new () { Normal = new (_host.ColorScheme.Normal.Background, _host.ColorScheme.Normal.Foreground) };
        }

        return base.GetNormalColor ();
    }

    public VariationMode VariationMode { get; }

    /// <inheritdoc/>
    protected internal override bool? OnMouseEnter (MouseEvent mouseEvent)
    {
        _savedColorScheme ??= _host.ColorScheme;

        ColorScheme = new ()
        {
            Normal = new (_savedColorScheme.HotNormal.Foreground, _savedColorScheme.HotNormal.Foreground),
            Focus = new (_savedColorScheme.Focus.Foreground, _savedColorScheme.Focus.Foreground),
            HotNormal = new (_savedColorScheme.Normal.Foreground, _savedColorScheme.Normal.Foreground),
            HotFocus = new (_savedColorScheme.HotFocus.Foreground, _savedColorScheme.HotFocus.Foreground),
            Disabled = new (_savedColorScheme.Disabled.Foreground, _savedColorScheme.Disabled.Foreground)
        };

        return base.OnMouseEnter (mouseEvent);
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            switch (VariationMode)
            {
                case VariationMode.Decrease:
                    _host.Position--;

                    return true;
                case VariationMode.Increase:
                    _host.Position++;

                    return true;
                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }

        return base.OnMouseEvent (mouseEvent);
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseLeave (MouseEvent mouseEvent)
    {
        if (_savedColorScheme is { })
        {
            ColorScheme = _savedColorScheme;
            _savedColorScheme = null;
        }

        return base.OnMouseLeave (mouseEvent);
    }

    private void SetButtonText ()
    {
        switch (VariationMode)
        {
            case VariationMode.Decrease:
                Text = _host.Orientation == Orientation.Vertical ? Glyphs.UpArrow.ToString () : Glyphs.LeftArrow.ToString ();

                break;
            case VariationMode.Increase:
                Text = _host.Orientation == Orientation.Vertical ? Glyphs.DownArrow.ToString () : Glyphs.RightArrow.ToString ();

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }
    }
}
