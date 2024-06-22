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
            Height = Dim.Auto (DimAutoStyle.Content)
        };
        Add (_slider);

        Added += Scroll_Added;
        Removed += Scroll_Removed;
        Initialized += Scroll_Initialized;
        DrawContent += Scroll_DrawContent;
        MouseEvent += Scroll_MouseEvent;
        _slider.DrawContent += Scroll_DrawContent;
        _slider.MouseEvent += Slider_MouseEvent;
    }

    private readonly View _slider;

    private int _lastLocation = -1;

    private bool _wasSliderMouse;

    private int _barSize => Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;

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
            AdjustSlider();
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
            if (value == _position || value < 0 || value + _barSize > Size)
            {
                return;
            }

            StateEventArgs<int> args = OnPositionChanging (_position, value);

            if (args.Cancel)
            {
                return;
            }

            if (!_wasSliderMouse)
            {
                AdjustSlider ();
            }

            int oldPos = _position;
            _position = value;
            OnPositionChanged (oldPos);
        }
    }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<StateEventArgs<int>> PositionChanged;

    /// <summary>Raised when the <see cref="Position"/> is changing. Set <see cref="StateEventArgs{T}.Cancel"/> to <see langword="true"/> to prevent the position from being changed.</summary>
    public event EventHandler<StateEventArgs<int>> PositionChanging;

    /// <summary>Virtual method called when <see cref="Position"/> has changed. Fires <see cref="PositionChanged"/>.</summary>
    protected virtual void OnPositionChanged (int oldPos) { PositionChanged?.Invoke (this, new (oldPos, Position)); }

    /// <summary>Virtual method called when <see cref="Position"/> is changing. Fires <see cref="PositionChanging"/>, which is cancelable.</summary>
    protected virtual StateEventArgs<int> OnPositionChanging (int oldPos, int newPos)
    {
        StateEventArgs<int> args = new (oldPos, newPos);
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
            int oldSize = _size;
            _size = value;
            OnSizeChanged (oldSize);
            AdjustSlider ();
        }
    }

    /// <summary>Raised when <see cref="Size"/> has changed.</summary>
    public event EventHandler<StateEventArgs<int>> SizeChanged;

    /// <summary>Virtual method called when <see cref="Size"/> has changed. Fires <see cref="SizeChanged"/>.</summary>
    protected void OnSizeChanged (int oldSize) { SizeChanged?.Invoke (this, new (oldSize, Size)); }

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

        return Math.Min (location * Size / scrollSize, Size - scrollSize);
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

            location = Math.Min (Position * scrollSize / Size, scrollSize - dimension);

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

    private void SuperView_MouseEnter (object sender, MouseEventEventArgs e) { OnMouseEnter (e.MouseEvent); }

    private void SuperView_MouseLeave (object sender, MouseEventEventArgs e) { OnMouseLeave (e.MouseEvent); }

    private void Scroll_Added (object sender, SuperViewChangedEventArgs e)
    {
        View parent = e.SuperView is Adornment adornment ? adornment.Parent : e.SuperView;

        parent.LayoutComplete += SuperView_LayoutComplete;

        // QUESTION: I really don't like this. It feels like a hack that a subview needs to track its parent's mouse events.
        // QUESTION: Can we figure out a way to do this without tracking the parent's mouse events?
        parent.MouseEnter += SuperView_MouseEnter;
        parent.MouseLeave += SuperView_MouseLeave;
    }

    // TODO: Just override GetNormalColor instead of having this method (make Slider a View sub-class that overrides GetNormalColor)
    private void Scroll_DrawContent (object sender, DrawEventArgs e) { SetColorSchemeWithSuperview (sender as View); }

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

        (int topLeft, int bottomRight) sliderPos = _orientation == Orientation.Vertical
                                                       ? new (_slider.Frame.Y, _slider.Frame.Bottom - 1)
                                                       : new (_slider.Frame.X, _slider.Frame.Right - 1);

        if (me.Flags == MouseFlags.Button1Pressed && location < sliderPos.topLeft)
        {
            Position = Math.Max (Position - _barSize, 0);
        }
        else if (me.Flags == MouseFlags.Button1Pressed && location > sliderPos.bottomRight)
        {
            Position = Math.Min (Position + _barSize, Size - _barSize);
        }
        else if ((me.Flags == MouseFlags.WheeledDown && Orientation == Orientation.Vertical)
                 || (me.Flags == MouseFlags.WheeledRight && Orientation == Orientation.Horizontal))
        {
            Position = Math.Min (Position + 1, Size - _barSize);
        }
        else if ((me.Flags == MouseFlags.WheeledUp && Orientation == Orientation.Vertical)
                 || (me.Flags == MouseFlags.WheeledLeft && Orientation == Orientation.Horizontal))
        {
            Position = Math.Max (Position - 1, 0);
        }
    }

    private void Scroll_Removed (object sender, SuperViewChangedEventArgs e)
    {
        if (e.SuperView is { })
        {
            View parent = e.SuperView is Adornment adornment ? adornment.Parent : e.SuperView;

            parent.LayoutComplete -= SuperView_LayoutComplete;
            parent.MouseEnter -= SuperView_MouseEnter;
            parent.MouseLeave -= SuperView_MouseLeave;
        }
    }

    // TODO: Just override GetNormalColor instead of having this method
    private static void SetColorSchemeWithSuperview (View view)
    {
        if (view.SuperView is { })
        {
            View parent = view.SuperView is Adornment adornment ? adornment.Parent : view.SuperView;

            if (view.Id == "slider")
            {
                view.ColorScheme = new () { Normal = new (parent.ColorScheme.Normal.Foreground, parent.ColorScheme.Normal.Foreground) };
            }
            else
            {
                view.ColorScheme = parent.ColorScheme;
            }
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
                if (_slider.Frame.Y + offset >= 0 && _slider.Frame.Y + offset + _slider.Frame.Height <= _barSize)
                {
                    _wasSliderMouse = true;
                    _slider.Y = _slider.Frame.Y + offset;
                    Position = GetPositionFromSliderLocation (_slider.Frame.Y);
                }
            }
            else
            {
                if (_slider.Frame.X + offset >= 0 && _slider.Frame.X + offset + _slider.Frame.Width <= _barSize)
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
            Position = Math.Min (Position + 1, Size - _barSize);
        }
        else if ((me.Flags == MouseFlags.WheeledUp && Orientation == Orientation.Vertical)
                 || (me.Flags == MouseFlags.WheeledLeft && Orientation == Orientation.Horizontal))
        {
            Position = Math.Max (Position - 1, 0);
        }
        else
        {
            return;
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Added -= Scroll_Added;
        Initialized -= Scroll_Initialized;
        DrawContent -= Scroll_DrawContent;
        MouseEvent -= Scroll_MouseEvent;
        _slider.DrawContent -= Scroll_DrawContent;
        _slider.MouseEvent -= Slider_MouseEvent;

        base.Dispose (disposing);
    }

}
