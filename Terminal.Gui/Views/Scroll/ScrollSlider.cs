#nullable enable

namespace Terminal.Gui;

internal class ScrollSlider : View
{
    public ScrollSlider (Scroll host)
    {
        _host = host;
        Id = "slider";
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        WantMousePositionReports = true;
    }

    internal bool _wasSliderMouse;

    private readonly Scroll _host;
    private int _lastLocation = -1;
    private ColorScheme? _savedColorScheme;

    /// <inheritdoc/>
    public override Attribute GetNormalColor ()
    {
        if (_savedColorScheme is null)
        {
            ColorScheme = new () { Normal = new (_host.ColorScheme.HotNormal.Foreground, _host.ColorScheme.HotNormal.Foreground) };
        }
        else
        {
            ColorScheme = new () { Normal = new (_host.ColorScheme.Normal.Foreground, _host.ColorScheme.Normal.Foreground) };
        }

        return base.GetNormalColor ();
    }

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
        int location = _host.Orientation == Orientation.Vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int offset = _lastLocation > -1 ? location - _lastLocation : 0;
        int barSize = _host.Orientation == Orientation.Vertical ? _host.GetContentSize ().Height : _host.GetContentSize ().Width;

        if (mouseEvent.Flags == MouseFlags.Button1Pressed)
        {
            if (Application.MouseGrabView != this)
            {
                Application.GrabMouse (this);
                _lastLocation = location;
            }
        }
        else if (mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            if (_host.Orientation == Orientation.Vertical)
            {
                if (Frame.Y + offset >= 0 && Frame.Y + offset + Frame.Height <= barSize)
                {
                    _wasSliderMouse = true;
                    Y = Frame.Y + offset;
                    _host.Position = GetPositionFromSliderLocation (Frame.Y);
                }
            }
            else
            {
                if (Frame.X + offset >= 0 && Frame.X + offset + Frame.Width <= barSize)
                {
                    _wasSliderMouse = true;
                    X = Frame.X + offset;
                    _host.Position = GetPositionFromSliderLocation (Frame.X);
                }
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
        else if ((mouseEvent.Flags == MouseFlags.WheeledDown && _host.Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledRight && _host.Orientation == Orientation.Horizontal))
        {
            _host.Position = Math.Min (_host.Position + 1, _host.Size - barSize);
        }
        else if ((mouseEvent.Flags == MouseFlags.WheeledUp && _host.Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledLeft && _host.Orientation == Orientation.Horizontal))
        {
            _host.Position = Math.Max (_host.Position - 1, 0);
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

    private int GetPositionFromSliderLocation (int location)
    {
        if (_host.GetContentSize ().Height == 0 || _host.GetContentSize ().Width == 0)
        {
            return 0;
        }

        int scrollSize = _host.Orientation == Orientation.Vertical ? _host.GetContentSize ().Height : _host.GetContentSize ().Width;

        // Ensure the Position is valid if the slider is at end
        // We use Frame here instead of ContentSize because even if the slider has a margin or border, Frame indicates the actual size
        if ((_host.Orientation == Orientation.Vertical && location + Frame.Height == scrollSize)
            || (_host.Orientation == Orientation.Horizontal && location + Frame.Width == scrollSize))
        {
            return _host.Size - scrollSize;
        }

        return Math.Min ((location * _host.Size + location) / scrollSize, _host.Size - scrollSize);
    }
}
