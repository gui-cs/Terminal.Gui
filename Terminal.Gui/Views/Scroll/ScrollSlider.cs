#nullable enable

namespace Terminal.Gui;

internal class ScrollSlider : View
{
    public ScrollSlider ()
    {
        Id = "scrollSlider";
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        WantMousePositionReports = true;
    }

    private int _lastLocation = -1;
    private ColorScheme? _savedColorScheme;

    public void AdjustSlider ()
    {
        if (!IsInitialized)
        {
            return;
        }

        (int Location, int Dimension) sliderLocationAndDimension = GetSliderLocationDimensionFromPosition ();
        X = SupView.Orientation == Orientation.Vertical ? 0 : sliderLocationAndDimension.Location;
        Y = SupView.Orientation == Orientation.Vertical ? sliderLocationAndDimension.Location : 0;

        SetContentSize (
                        new (
                             SupView.Orientation == Orientation.Vertical ? SupView.GetContentSize ().Width : sliderLocationAndDimension.Dimension,
                             SupView.Orientation == Orientation.Vertical ? sliderLocationAndDimension.Dimension : SupView.GetContentSize ().Height
                            ));
        SetSliderText ();
    }

    /// <inheritdoc/>
    public override Attribute GetNormalColor ()
    {
        if (_savedColorScheme is null)
        {
            ColorScheme = new () { Normal = new (SupView.ColorScheme.HotNormal.Foreground, SupView.ColorScheme.HotNormal.Foreground) };
        }
        else
        {
            ColorScheme = new () { Normal = new (SupView.ColorScheme.Normal.Foreground, SupView.ColorScheme.Normal.Foreground) };
        }

        return base.GetNormalColor ();
    }

    /// <inheritdoc/>
    protected internal override bool? OnMouseEnter (MouseEvent mouseEvent)
    {
        _savedColorScheme ??= SupView.ColorScheme;

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
        int location = SupView.Orientation == Orientation.Vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int offset = _lastLocation > -1 ? location - _lastLocation : 0;
        int barSize = SupView.Orientation == Orientation.Vertical ? SupView.GetContentSize ().Height : SupView.GetContentSize ().Width;

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) && _lastLocation == -1)
        {
            if (Application.MouseGrabView != this)
            {
                Application.GrabMouse (this);
                _lastLocation = location;
            }
        }
        else if (mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            if (SupView.Orientation == Orientation.Vertical)
            {
                Y = Frame.Y + offset < 0 ? 0 :
                    Frame.Y + offset + Frame.Height > barSize ? Math.Max (barSize - Frame.Height, 0) : Frame.Y + offset;

                SupView.Position = GetPositionFromSliderLocation (Frame.Y);
            }
            else
            {
                X = Frame.X + offset < 0 ? 0 :
                    Frame.X + offset + Frame.Width > barSize ? Math.Max (barSize - Frame.Width, 0) : Frame.X + offset;

                SupView.Position = GetPositionFromSliderLocation (Frame.X);
            }
        }
        else if (mouseEvent.Flags == MouseFlags.Button1Released)
        {
            _lastLocation = -1;

            if (Application.MouseGrabView == this)
            {
                Application.UngrabMouse ();
            }
        }
        else if ((mouseEvent.Flags == MouseFlags.WheeledDown && SupView.Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledRight && SupView.Orientation == Orientation.Horizontal))
        {
            SupView.Position = Math.Min (SupView.Position + 1, SupView.Size - barSize);
        }
        else if ((mouseEvent.Flags == MouseFlags.WheeledUp && SupView.Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledLeft && SupView.Orientation == Orientation.Horizontal))
        {
            SupView.Position = Math.Max (SupView.Position - 1, 0);
        }
        else if (mouseEvent.Flags != MouseFlags.ReportMousePosition)
        {
            return base.OnMouseEvent (mouseEvent);
        }

        return true;
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseLeave (MouseEvent mouseEvent)
    {
        if (_savedColorScheme is { } && !mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            ColorScheme = _savedColorScheme;
            _savedColorScheme = null;
        }

        return base.OnMouseLeave (mouseEvent);
    }

    internal int GetPositionFromSliderLocation (int location)
    {
        if (SupView.GetContentSize ().Height == 0 || SupView.GetContentSize ().Width == 0)
        {
            return 0;
        }

        int scrollSize = SupView.Orientation == Orientation.Vertical ? SupView.GetContentSize ().Height : SupView.GetContentSize ().Width;

        // Ensure the Position is valid if the slider is at end
        // We use Frame here instead of ContentSize because even if the slider has a margin or border, Frame indicates the actual size
        if ((SupView.Orientation == Orientation.Vertical && location + Frame.Height >= scrollSize)
            || (SupView.Orientation == Orientation.Horizontal && location + Frame.Width >= scrollSize))
        {
            return SupView.Size - scrollSize;
        }

        return (int)Math.Min (Math.Round ((double)(location * SupView.Size + location) / scrollSize), SupView.Size - scrollSize);
    }

    internal (int Location, int Dimension) GetSliderLocationDimensionFromPosition ()
    {
        if (SupView.GetContentSize ().Height == 0 || SupView.GetContentSize ().Width == 0)
        {
            return new (0, 0);
        }

        int scrollSize = SupView.Orientation == Orientation.Vertical ? SupView.GetContentSize ().Height : SupView.GetContentSize ().Width;
        int location;
        int dimension;

        if (SupView.Size > 0)
        {
            dimension = (int)Math.Min (Math.Max (Math.Ceiling ((double)scrollSize * scrollSize / SupView.Size), 1), scrollSize);

            // Ensure the Position is valid
            if (SupView.Position > 0 && SupView.Position + scrollSize > SupView.Size)
            {
                SupView.Position = SupView.Size - scrollSize;
            }

            location = (int)Math.Min (Math.Round ((double)SupView.Position * scrollSize / (SupView.Size + 1)), scrollSize - dimension);

            if (SupView.Position == SupView.Size - scrollSize && location + dimension < scrollSize)
            {
                location = scrollSize - dimension;
            }
        }
        else
        {
            location = 0;
            dimension = scrollSize;
        }

        return new (location, dimension);
    }

    // TODO: I think you should create a new `internal` view named "ScrollSlider" with an `Orientation` property. It should inherit from View and override GetNormalColor and the mouse events
    // that can be moved within it's Superview, constrained to move only horizontally or vertically depending on Orientation.
    // This will really simplify a lot of this.

    private void SetSliderText ()
    {
        TextDirection = SupView.Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        // QUESTION: Should these Glyphs be configurable via CM?
        Text = string.Concat (
                              Enumerable.Repeat (
                                                 Glyphs.ContinuousMeterSegment.ToString (),
                                                 SupView.GetContentSize ().Width * SupView.GetContentSize ().Height));
    }

    private Scroll SupView => (SuperView as Scroll)!;
}
