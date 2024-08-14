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

        _slider = new (this);
        Add (_slider);
    }

    private readonly ScrollSlider _slider;

    private Orientation _orientation;

    private int _position;

    private int _size;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        AdjustSlider ();
    }

    /// <inheritdoc/>
    public override void OnAdded (SuperViewChangedEventArgs e)
    {
        View parent = (e.Parent is Adornment adornment ? adornment.Parent : e.Parent)!;

        parent.LayoutComplete += SuperView_LayoutComplete!;

        base.OnAdded (e);
    }

    /// <inheritdoc/>
    public override void OnRemoved (SuperViewChangedEventArgs e)
    {
        if (e.Parent is { })
        {
            View parent = (e.Parent is Adornment adornment ? adornment.Parent : e.Parent)!;

            parent.LayoutComplete -= SuperView_LayoutComplete!;
        }

        base.OnRemoved (e);
    }

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

            if (!_slider._wasSliderMouse)
            {
                AdjustSlider ();
            }

            OnPositionChanged (_position);
        }
    }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? PositionChanged;

    /// <summary>
    ///     Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? PositionChanging;

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
    public event EventHandler<EventArgs<int>>? SizeChanged;

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        int location = Orientation == Orientation.Vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int barSize = Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;

        (int topLeft, int bottomRight) sliderPos = _orientation == Orientation.Vertical
                                                       ? new (_slider.Frame.Y, _slider.Frame.Bottom - 1)
                                                       : new (_slider.Frame.X, _slider.Frame.Right - 1);

        if (mouseEvent.Flags == MouseFlags.Button1Pressed && location < sliderPos.topLeft)
        {
            Position = Math.Max (Position - barSize, 0);
        }
        else if (mouseEvent.Flags == MouseFlags.Button1Pressed && location > sliderPos.bottomRight)
        {
            Position = Math.Min (Position + barSize, Size - barSize);
        }
        else if ((mouseEvent.Flags == MouseFlags.WheeledDown && Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledRight && Orientation == Orientation.Horizontal))
        {
            Position = Math.Min (Position + 1, Size - barSize);
        }
        else if ((mouseEvent.Flags == MouseFlags.WheeledUp && Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledLeft && Orientation == Orientation.Horizontal))
        {
            Position = Math.Max (Position - 1, 0);
        }
        else if (mouseEvent.Flags == MouseFlags.Button1Clicked)
        {
            if (_slider.Frame.Contains (mouseEvent.Position))
            {
                return _slider.OnMouseEvent (mouseEvent);
            }
        }

        return base.OnMouseEvent (mouseEvent);
    }

    // TODO: Move this into "ScrollSlider" and override it there. Scroll can then subscribe to _slider.LayoutComplete and call AdjustSlider.
    // QUESTION: I've been meaning to add a "View.FrameChanged" event (fired from LayoutComplete only if Frame has changed). Should we do that as part of this PR?
    // QUESTION: Note I *did* add "View.ViewportChanged" in a previous PR.

    /// <summary>Virtual method called when <see cref="Position"/> has changed. Raises <see cref="PositionChanged"/>.</summary>
    protected virtual void OnPositionChanged (int position) { PositionChanged?.Invoke (this, new (in position)); }

    /// <summary>
    ///     Virtual method called when <see cref="Position"/> is changing. Raises <see cref="PositionChanging"/>, which is
    ///     cancelable.
    /// </summary>
    protected virtual CancelEventArgs<int> OnPositionChanging (int currentPos, int newPos)
    {
        CancelEventArgs<int> args = new (ref currentPos, ref newPos);
        PositionChanging?.Invoke (this, args);

        return args;
    }

    /// <summary>Virtual method called when <see cref="Size"/> has changed. Raises <see cref="SizeChanged"/>.</summary>
    protected void OnSizeChanged (int size) { SizeChanged?.Invoke (this, new (in size)); }

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

    // TODO: I think you should create a new `internal` view named "ScrollSlider" with an `Orientation` property. It should inherit from View and override GetNormalColor and the mouse events
    // that can be moved within it's Superview, constrained to move only horizontally or vertically depending on Orientation.
    // This will really simplify a lot of this.

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

    // TODO: This is unnecessary. If Scroll.Width/Height is Dim.Auto, the Superview will get resized automatically.
    private void SuperView_LayoutComplete (object sender, LayoutEventArgs e)
    {
        if (!_slider._wasSliderMouse)
        {
            AdjustSlider ();
        }
        else
        {
            _slider._wasSliderMouse = false;
        }
    }
}
