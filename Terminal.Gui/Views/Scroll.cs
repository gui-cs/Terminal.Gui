#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Provides a proportional control for scrolling through content. Used within a <see cref="ScrollBar"/>.
/// </summary>
public class Scroll : View
{
    /// <inheritdoc/>
    public Scroll ()
    {
        WantContinuousButtonPressed = true;
        CanFocus = false;
        Orientation = Orientation.Vertical;
        Width = Dim.Auto (DimAutoStyle.Content, 1);
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        _slider = new ()
        {
            Id = "slider",
            Width = Dim.Auto (DimAutoStyle.Content),
            Height = Dim.Auto (DimAutoStyle.Content),
            WantMousePositionReports = true
        };
        Add (_slider);

        Added += Scroll_Added;
        Removed += Scroll_Removed;
        Initialized += Scroll_Initialized;
        MouseEvent += Scroll_MouseEvent;
        _slider.MouseEvent += Slider_MouseEvent;
        _slider.MouseEnter += Slider_MouseEnter;
        _slider.MouseLeave += Slider_MouseLeave;
    }

    private readonly View _slider;

    private int _lastLocation = -1;

    private bool _wasSliderMouse;

    private Orientation _orientation;
    /// <summary>
    ///     Gets or sets if the Scroll is oriented vertically or horizontally.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            AdjustSlider ();
        }
    }

    private int _position;
    /// <summary>
    ///     Gets or sets the position of the start of the Scroll slider, relative to <see cref="Size"/>.
    /// </summary>
    public int Position
    {
        get => _position;
        set
        {
            if (value == _position || value < 0)
            {
                return;
            }

            int barSize = Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;

            if (value + barSize > Size)
            {
                return;
            }

            CancelEventArgs<int> args = OnPositionChanging (_position, value);

            if (args.Cancel)
            {
                return;
            }

            _position = value;

            if (!_wasSliderMouse)
            {
                AdjustSlider ();
            }

            OnPositionChanged (_position);
        }
    }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>> PositionChanged;

    /// <summary>Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to prevent the position from being changed.</summary>
    public event EventHandler<CancelEventArgs<int>> PositionChanging;

    /// <summary>Virtual method called when <see cref="Position"/> has changed. Raises <see cref="PositionChanged"/>.</summary>
    protected virtual void OnPositionChanged (int position) { PositionChanged?.Invoke (this, new (ref position)); }

    /// <summary>Virtual method called when <see cref="Position"/> is changing. Raises <see cref="PositionChanging"/>, which is cancelable.</summary>
    protected virtual CancelEventArgs<int> OnPositionChanging (int currentPos, int newPos)
    {
        CancelEventArgs<int> args = new (ref currentPos, ref newPos);
        PositionChanging?.Invoke (this, args);

        return args;
    }

    private int _size;
    /// <summary>
    ///     Gets or sets the size of the Scroll. This is the total size of the content that can be scrolled through.
    /// </summary>
    public int Size
    {
        get => _size;
        set
        {
            _size = value;
            OnSizeChanged (_size);
            AdjustSlider ();
        }
    }

    /// <summary>Raised when <see cref="Size"/> has changed.</summary>
    public event EventHandler<EventArgs<int>> SizeChanged;

    /// <summary>Virtual method called when <see cref="Size"/> has changed. Raises <see cref="SizeChanged"/>.</summary>
    protected void OnSizeChanged (int size) { SizeChanged?.Invoke (this, new (ref size)); }

    private int GetPositionFromSliderLocation (int location)
    {
        if (GetContentSize ().Height == 0 || GetContentSize ().Width == 0)
        {
            return 0;
        }

        int scrollSize = Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;

        // Ensure the Position is valid if the slider is at end
        // We use Frame here instead of ContentSize because even if the slider has a margin or border, Frame indicates the actual size
        if ((Orientation == Orientation.Vertical && location + _slider.Frame.Height == scrollSize)
            || (Orientation == Orientation.Horizontal && location + _slider.Frame.Width == scrollSize))
        {
            return Size - scrollSize;
        }

        return Math.Min ((location * Size + location) / scrollSize, Size - scrollSize);
    }

    // QUESTION: This method is only called from one place. Should it be inlined? Or, should it be made internal and unit tests be provided?
    private (int Location, int Dimension) GetSliderLocationDimensionFromPosition ()
    {
        if (GetContentSize ().Height == 0 || GetContentSize ().Width == 0)
        {
            return new (0, 0);
        }

        int scrollSize = Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;
        int location;
        int dimension;

        if (Size > 0)
        {
            dimension = Math.Min (Math.Max (scrollSize * scrollSize / Size, 1), scrollSize);

            // Ensure the Position is valid
            if (Position > 0 && Position + scrollSize > Size)
            {
                Position = Size - scrollSize;
            }

            location = Math.Min ((Position * scrollSize + Position) / Size, scrollSize - dimension);

            if (Position == Size - scrollSize && location + dimension < scrollSize)
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

    // TODO: This is unnecessary. If Scroll.Width/Height is Dim.Auto, the Superview will get resized automatically.
    private void SuperView_LayoutComplete (object sender, LayoutEventArgs e)
    {
        if (!_wasSliderMouse)
        {
            AdjustSlider ();
        }
        else
        {
            _wasSliderMouse = false;
        }
    }



    private void Scroll_Added (object sender, SuperViewChangedEventArgs e)
    {
        View parent = e.Parent is Adornment adornment ? adornment.Parent : e.Parent;

        parent.LayoutComplete += SuperView_LayoutComplete;
    }


    /// <inheritdoc />
    public override Attribute GetNormalColor ()
    {
        if (_savedColorScheme is null)
        {
            _slider.ColorScheme = new () { Normal = new (ColorScheme.HotNormal.Foreground, ColorScheme.HotNormal.Foreground) };
        }
        else
        {
            _slider.ColorScheme = new () { Normal = new (ColorScheme.Normal.Foreground, ColorScheme.Normal.Foreground) };
        }

        return base.GetNormalColor ();
    }

    private void Scroll_Initialized (object sender, EventArgs e)
    {
        AdjustSlider ();
    }

    // TODO: I think you should create a new `internal` view named "ScrollSlider" with an `Orientation` property. It should inherit from View and override GetNormalColor and the mouse events
    // that can be moved within it's Superview, constrained to move only horizontally or vertically depending on Orientation.
    // This will really simplify a lot of this.
    private void Scroll_MouseEvent (object sender, MouseEventEventArgs e)
    {
        MouseEvent me = e.MouseEvent;
        int location = Orientation == Orientation.Vertical ? me.Position.Y : me.Position.X;
        int barSize = Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;

        (int topLeft, int bottomRight) sliderPos = _orientation == Orientation.Vertical
                                                       ? new (_slider.Frame.Y, _slider.Frame.Bottom - 1)
                                                       : new (_slider.Frame.X, _slider.Frame.Right - 1);

        if (me.Flags == MouseFlags.Button1Pressed && location < sliderPos.topLeft)
        {
            Position = Math.Max (Position - barSize, 0);
        }
        else if (me.Flags == MouseFlags.Button1Pressed && location > sliderPos.bottomRight)
        {
            Position = Math.Min (Position + barSize, Size - barSize);
        }
        else if ((me.Flags == MouseFlags.WheeledDown && Orientation == Orientation.Vertical)
                 || (me.Flags == MouseFlags.WheeledRight && Orientation == Orientation.Horizontal))
        {
            Position = Math.Min (Position + 1, Size - barSize);
        }
        else if ((me.Flags == MouseFlags.WheeledUp && Orientation == Orientation.Vertical)
                 || (me.Flags == MouseFlags.WheeledLeft && Orientation == Orientation.Horizontal))
        {
            Position = Math.Max (Position - 1, 0);
        }
        else if (me.Flags == MouseFlags.Button1Clicked)
        {
            if (_slider.Frame.Contains (me.Position))
            {
                Slider_MouseEnter (_slider, e);
            }
        }
    }

    private void Scroll_Removed (object sender, SuperViewChangedEventArgs e)
    {
        if (e.Parent is { })
        {
            View parent = e.Parent is Adornment adornment ? adornment.Parent : e.Parent;

            parent.LayoutComplete -= SuperView_LayoutComplete;
        }
    }

    private void SetSliderText ()
    {
        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        // QUESTION: Should these Glyphs be configurable via CM?
        Text = string.Concat (
                              Enumerable.Repeat (
                                                 Glyphs.Stipple.ToString (),
                                                 GetContentSize ().Width * GetContentSize ().Height));
        _slider.TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        _slider.Text = string.Concat (
                                      Enumerable.Repeat (
                                                         Glyphs.ContinuousMeterSegment.ToString (),
                                                         _slider.GetContentSize ().Width * _slider.GetContentSize ().Height));
    }

    private void AdjustSlider ()
    {
        if (!IsInitialized)
        {
            return;
        }

        (int Location, int Dimension) slider = GetSliderLocationDimensionFromPosition ();
        _slider.X = Orientation == Orientation.Vertical ? 0 : slider.Location;
        _slider.Y = Orientation == Orientation.Vertical ? slider.Location : 0;

        _slider.SetContentSize (
                                new (
                                     Orientation == Orientation.Vertical ? GetContentSize ().Width : slider.Dimension,
                                     Orientation == Orientation.Vertical ? slider.Dimension : GetContentSize ().Height
                                    ));
        SetSliderText ();
    }

    // TODO: Move this into "ScrollSlider" and override it there. Scroll can then subscribe to _slider.LayoutComplete and call AdjustSlider.
    // QUESTION: I've been meaning to add a "View.FrameChanged" event (fired from LayoutComplete only if Frame has changed). Should we do that as part of this PR?
    // QUESTION: Note I *did* add "View.ViewportChanged" in a previous PR.
    private void Slider_MouseEvent (object sender, MouseEventEventArgs e)
    {
        MouseEvent me = e.MouseEvent;
        int location = Orientation == Orientation.Vertical ? me.Position.Y : me.Position.X;
        int offset = _lastLocation > -1 ? location - _lastLocation : 0;
        int barSize = Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;

        if (me.Flags == MouseFlags.Button1Pressed)
        {
            if (Application.MouseGrabView != sender as View)
            {
                Application.GrabMouse (sender as View);
                _lastLocation = location;
            }
        }
        else if (me.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            if (Orientation == Orientation.Vertical)
            {
                if (_slider.Frame.Y + offset >= 0 && _slider.Frame.Y + offset + _slider.Frame.Height <= barSize)
                {
                    _wasSliderMouse = true;
                    _slider.Y = _slider.Frame.Y + offset;
                    Position = GetPositionFromSliderLocation (_slider.Frame.Y);
                }
            }
            else
            {
                if (_slider.Frame.X + offset >= 0 && _slider.Frame.X + offset + _slider.Frame.Width <= barSize)
                {
                    _wasSliderMouse = true;
                    _slider.X = _slider.Frame.X + offset;
                    Position = GetPositionFromSliderLocation (_slider.Frame.X);
                }
            }
        }
        else if (me.Flags == MouseFlags.Button1Released)
        {
            _lastLocation = -1;

            if (Application.MouseGrabView == sender as View)
            {
                Application.UngrabMouse ();
            }
        }
        else if ((me.Flags == MouseFlags.WheeledDown && Orientation == Orientation.Vertical)
                 || (me.Flags == MouseFlags.WheeledRight && Orientation == Orientation.Horizontal))
        {
            Position = Math.Min (Position + 1, Size - barSize);
        }
        else if ((me.Flags == MouseFlags.WheeledUp && Orientation == Orientation.Vertical)
                 || (me.Flags == MouseFlags.WheeledLeft && Orientation == Orientation.Horizontal))
        {
            Position = Math.Max (Position - 1, 0);
        }
        else if (me.Flags != MouseFlags.ReportMousePosition)
        {
            return;
        }

        e.Handled = true;
    }

    [CanBeNull]
    private ColorScheme _savedColorScheme;

    private void Slider_MouseEnter (object sender, MouseEventEventArgs e)
    {
        _savedColorScheme ??= _slider.ColorScheme;
        _slider.ColorScheme = new ()
        {
            Normal = new (_savedColorScheme.HotNormal.Foreground, _savedColorScheme.HotNormal.Foreground),
            Focus = new (_savedColorScheme.Focus.Foreground, _savedColorScheme.Focus.Foreground),
            HotNormal = new (_savedColorScheme.Normal.Foreground, _savedColorScheme.Normal.Foreground),
            HotFocus = new (_savedColorScheme.HotFocus.Foreground, _savedColorScheme.HotFocus.Foreground),
            Disabled = new (_savedColorScheme.Disabled.Foreground, _savedColorScheme.Disabled.Foreground)
        };
    }

    private void Slider_MouseLeave (object sender, MouseEventEventArgs e)
    {
        if (_savedColorScheme is { } && !e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            _slider.ColorScheme = _savedColorScheme;
            _savedColorScheme = null;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Added -= Scroll_Added;
        Initialized -= Scroll_Initialized;
        MouseEvent -= Scroll_MouseEvent;
        _slider.MouseEvent -= Slider_MouseEvent;
        _slider.MouseEnter -= Slider_MouseEnter;
        _slider.MouseLeave -= Slider_MouseLeave;

        base.Dispose (disposing);
    }
}
