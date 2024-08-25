#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Provides a proportional control for scrolling through content. Used within a <see cref="ScrollBar"/>.
/// </summary>
public class Scroll : View
{
    /// <inheritdoc/>
    public Scroll () : this (null) { }

    public Scroll (ScrollBar? host)
    {
        _host = host;
        _slider = new (this);
        Add (_slider);

        WantContinuousButtonPressed = true;
        CanFocus = false;
        Orientation = Orientation.Vertical;

        if (_host is { })
        {
            Y = 1;
            Width = Dim.Fill ();
            Height = Dim.Fill (1);
        }
        else
        {
            Width = Dim.Auto (DimAutoStyle.Content, 1);
            Height = Dim.Auto (DimAutoStyle.Content, 1);
        }
    }


    internal readonly ScrollBar? _host;
    internal bool _wasSliderLayoutComplete = true;

    internal readonly ScrollSlider _slider;
    private Orientation _orientation;
    private int _position;
    private int _size;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        AdjustScroll ();
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
            AdjustScroll ();
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

            if (_host is { IsInitialized: false })
            {
                // Ensures a more exactly calculation
                SetRelativeLayout (_host.Frame.Size);
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

            AdjustScroll ();

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
            AdjustScroll ();
        }
    }

    /// <summary>Raised when <see cref="Size"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? SizeChanged;

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (!_wasSliderLayoutComplete)
        {
            // Do not process if slider layout wasn't yet completed
            return base.OnMouseEvent (mouseEvent);
        }

        int location = Orientation == Orientation.Vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int barSize = Orientation == Orientation.Vertical ? GetContentSize ().Height : GetContentSize ().Width;

        (int topLeft, int bottomRight) sliderPos = _orientation == Orientation.Vertical
                                                       ? new (_slider.Frame.Y, _slider.Frame.Bottom - 1)
                                                       : new (_slider.Frame.X, _slider.Frame.Right - 1);

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) && location < sliderPos.topLeft)
        {
            Position = Math.Max (Position - barSize, 0);
        }
        else if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) && location > sliderPos.bottomRight)
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
        // Flag as false until slider layout is completed
        _wasSliderLayoutComplete = false;

        return base.OnMouseEvent (mouseEvent);
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseLeave (MouseEvent mouseEvent)
    {
        // If scroll isn't handling mouse then reset the flag
        _wasSliderLayoutComplete = true;

        return base.OnMouseLeave (mouseEvent);
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

    internal void AdjustScroll ()
    {
        if (_host is { })
        {
            X = Orientation == Orientation.Vertical ? 0 : 1;
            Y = Orientation == Orientation.Vertical ? 1 : 0;
            Width = Orientation == Orientation.Vertical ? Dim.Fill () : Dim.Fill (1);
            Height = Orientation == Orientation.Vertical ? Dim.Fill (1) : Dim.Fill ();
        }

        _slider.AdjustSlider ();
        SetScrollText ();
    }

    /// <inheritdoc/>
    internal override void OnLayoutComplete (LayoutEventArgs args)
    {
        base.OnLayoutComplete (args);

        AdjustScroll ();
    }

    private void SetScrollText ()
    {
        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        // QUESTION: Should these Glyphs be configurable via CM?
        Text = string.Concat (
                              Enumerable.Repeat (
                                                 Glyphs.Stipple.ToString (),
                                                 GetContentSize ().Width * GetContentSize ().Height));
    }
}
