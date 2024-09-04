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
        X = SuperViewAsScroll.Orientation == Orientation.Vertical ? 0 : sliderLocationAndDimension.Location;
        Y = SuperViewAsScroll.Orientation == Orientation.Vertical ? sliderLocationAndDimension.Location : 0;

        SetContentSize (
                        new (
                             SuperViewAsScroll.Orientation == Orientation.Vertical
                                 ? SuperViewAsScroll.GetContentSize ().Width
                                 : sliderLocationAndDimension.Dimension,
                             SuperViewAsScroll.Orientation == Orientation.Vertical
                                 ? sliderLocationAndDimension.Dimension
                                 : SuperViewAsScroll.GetContentSize ().Height
                            ));
        SetSliderText ();
    }

    /// <inheritdoc/>
    public override Attribute GetNormalColor ()
    {
        if (_savedColorScheme is null)
        {
            ColorScheme = new () { Normal = new (SuperViewAsScroll.ColorScheme.HotNormal.Foreground, SuperViewAsScroll.ColorScheme.HotNormal.Foreground) };
        }
        else
        {
            ColorScheme = new () { Normal = new (SuperViewAsScroll.ColorScheme.Normal.Foreground, SuperViewAsScroll.ColorScheme.Normal.Foreground) };
        }

        return base.GetNormalColor ();
    }

    /// <inheritdoc/>
    protected internal override bool? OnMouseEnter (MouseEvent mouseEvent)
    {
        _savedColorScheme ??= SuperViewAsScroll.ColorScheme;

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
        int location = SuperViewAsScroll.Orientation == Orientation.Vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int offset = _lastLocation > -1 ? location - _lastLocation : 0;
        int barSize = SuperViewAsScroll.Orientation == Orientation.Vertical ? SuperViewAsScroll.Viewport.Height : SuperViewAsScroll.Viewport.Width;

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
            if (SuperViewAsScroll.Orientation == Orientation.Vertical)
            {
                Y = Frame.Y + offset < 0
                        ? 0
                        :
                        Frame.Y + offset + Frame.Height > barSize + (SuperViewAsScroll.KeepContentInAllViewport ? 0 : barSize)
                            ?
                            Math.Max (barSize - Frame.Height, 0)
                            : Frame.Y + offset;

                SuperViewAsScroll.Position = GetPositionFromSliderLocation (Frame.Y);
            }
            else
            {
                X = Frame.X + offset < 0 ? 0 :
                    Frame.X + offset + Frame.Width > barSize ? Math.Max (barSize - Frame.Width, 0) : Frame.X + offset;

                SuperViewAsScroll.Position = GetPositionFromSliderLocation (Frame.X);
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
        else if ((mouseEvent.Flags == MouseFlags.WheeledDown && SuperViewAsScroll.Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledRight && SuperViewAsScroll.Orientation == Orientation.Horizontal))
        {
            SuperViewAsScroll.Position = Math.Min (SuperViewAsScroll.Position + 1, SuperViewAsScroll.Size - barSize);
        }
        else if ((mouseEvent.Flags == MouseFlags.WheeledUp && SuperViewAsScroll.Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledLeft && SuperViewAsScroll.Orientation == Orientation.Horizontal))
        {
            SuperViewAsScroll.Position = Math.Max (SuperViewAsScroll.Position - 1, 0);
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
        if (SuperViewAsScroll.GetContentSize ().Height == 0 || SuperViewAsScroll.GetContentSize ().Width == 0)
        {
            return 0;
        }

        int scrollSize = SuperViewAsScroll.Orientation == Orientation.Vertical
                             ? SuperViewAsScroll.GetContentSize ().Height
                             : SuperViewAsScroll.GetContentSize ().Width;

        // Ensure the Position is valid if the slider is at end
        // We use Frame here instead of ContentSize because even if the slider has a margin or border, Frame indicates the actual size
        if ((SuperViewAsScroll.Orientation == Orientation.Vertical && location + Frame.Height >= scrollSize)
            || (SuperViewAsScroll.Orientation == Orientation.Horizontal && location + Frame.Width >= scrollSize))
        {
            return SuperViewAsScroll.Size - scrollSize + (SuperViewAsScroll.KeepContentInAllViewport ? 0 : scrollSize);
        }

        return (int)Math.Min (Math.Round ((double)(location * SuperViewAsScroll.Size + location) / scrollSize), SuperViewAsScroll.Size - scrollSize);
    }

    internal (int Location, int Dimension) GetSliderLocationDimensionFromPosition ()
    {
        if (SuperViewAsScroll.GetContentSize ().Height == 0 || SuperViewAsScroll.GetContentSize ().Width == 0)
        {
            return new (0, 0);
        }

        int scrollSize = SuperViewAsScroll.Orientation == Orientation.Vertical
                             ? SuperViewAsScroll.GetContentSize ().Height
                             : SuperViewAsScroll.GetContentSize ().Width;
        int location;
        int dimension;

        if (SuperViewAsScroll.Size > 0)
        {
            dimension = (int)Math.Min (Math.Max (Math.Ceiling ((double)scrollSize * scrollSize / SuperViewAsScroll.Size), 1), scrollSize);

            // Ensure the Position is valid
            if (SuperViewAsScroll.Position > 0
                && SuperViewAsScroll.Position + scrollSize > SuperViewAsScroll.Size + (SuperViewAsScroll.KeepContentInAllViewport ? 0 : scrollSize))
            {
                SuperViewAsScroll.Position = SuperViewAsScroll.Size - scrollSize;
            }

            location = (int)Math.Min (Math.Round ((double)SuperViewAsScroll.Position * scrollSize / (SuperViewAsScroll.Size + 1)), scrollSize - dimension);

            if (SuperViewAsScroll.Position == SuperViewAsScroll.Size - scrollSize && location + dimension < scrollSize)
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
        TextDirection = SuperViewAsScroll.Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        // QUESTION: Should these Glyphs be configurable via CM?
        Text = string.Concat (
                              Enumerable.Repeat (
                                                 Glyphs.ContinuousMeterSegment.ToString (),
                                                 SuperViewAsScroll.GetContentSize ().Width * SuperViewAsScroll.GetContentSize ().Height));
    }

    private Scroll SuperViewAsScroll => (SuperView as Scroll)!;
}
