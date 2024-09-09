#nullable enable

namespace Terminal.Gui;

internal class ScrollButton : View
{
    public ScrollButton ()
    {
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;
        Id = "scrollButton";
        NavigationDirection = NavigationDirection.Backward;
        WantContinuousButtonPressed = true;
    }

    private ColorScheme? _savedColorScheme;

    public void AdjustButton ()
    {
        if (!IsInitialized)
        {
            return;
        }

        Width = SuperViewAsScrollBar.Orientation == Orientation.Vertical ? Dim.Fill () : 1;
        Height = SuperViewAsScrollBar.Orientation == Orientation.Vertical ? 1 : Dim.Fill ();

        switch (NavigationDirection)
        {
            case NavigationDirection.Backward:
                X = 0;
                Y = 0;

                break;
            case NavigationDirection.Forward:
                X = SuperViewAsScrollBar.Orientation == Orientation.Vertical ? 0 : Pos.AnchorEnd (1);
                Y = SuperViewAsScrollBar.Orientation == Orientation.Vertical ? Pos.AnchorEnd (1) : 0;

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
            ColorScheme = new () { Normal = new (SuperViewAsScrollBar.ColorScheme.HotNormal.Foreground, SuperViewAsScrollBar.ColorScheme.HotNormal.Background) };
        }
        else
        {
            ColorScheme = new () { Normal = new (SuperViewAsScrollBar.ColorScheme.Normal.Background, SuperViewAsScrollBar.ColorScheme.Normal.Foreground) };
        }

        return base.GetNormalColor ();
    }

    public NavigationDirection NavigationDirection { get; init; }

    /// <inheritdoc/>
    protected internal override bool? OnMouseEnter (MouseEvent mouseEvent)
    {
        _savedColorScheme ??= SuperViewAsScrollBar.ColorScheme;

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
            switch (NavigationDirection)
            {
                case NavigationDirection.Backward:
                    SuperViewAsScrollBar.Position--;

                    return true;
                case NavigationDirection.Forward:
                    SuperViewAsScrollBar.Position++;

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
        switch (NavigationDirection)
        {
            case NavigationDirection.Backward:
                Text = SuperViewAsScrollBar.Orientation == Orientation.Vertical ? Glyphs.UpArrow.ToString () : Glyphs.LeftArrow.ToString ();

                break;
            case NavigationDirection.Forward:
                Text = SuperViewAsScrollBar.Orientation == Orientation.Vertical ? Glyphs.DownArrow.ToString () : Glyphs.RightArrow.ToString ();

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }
    }

    private ScrollBar SuperViewAsScrollBar => (SuperView as ScrollBar)!;
}
