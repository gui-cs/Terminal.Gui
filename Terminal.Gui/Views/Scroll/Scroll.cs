#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Indicates the position and size of scrollable content. The indicator can be dragged with the mouse. Can be
///     oriented either vertically or horizontally. Used within a <see cref="ScrollBar"/>.
/// </summary>
/// <remarks>
///     <para>
///         By default, this view cannot be focused and does not support keyboard.
///     </para>
/// </remarks>
public class Scroll : View
{
    /// <inheritdoc/>
    public Scroll ()
    {
        _slider = new ();
        Add (_slider);

        WantContinuousButtonPressed = true;
        CanFocus = false;
        Orientation = Orientation.Vertical;

        Width = Dim.Auto (DimAutoStyle.Content, 1);
        Height = Dim.Auto (DimAutoStyle.Content, 1);
    }

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

            if (SuperViewAsScrollBar is { IsInitialized: false })
            {
                // Ensures a more exactly calculation
                SetRelativeLayout (SuperViewAsScrollBar.Frame.Size);
            }

            int barSize = BarSize;

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
        int location = Orientation == Orientation.Vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int barSize = BarSize;

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

    /// <summary>Called when <see cref="Size"/> has changed. Raises <see cref="SizeChanged"/>.</summary>
    protected void OnSizeChanged (int size) { SizeChanged?.Invoke (this, new (in size)); }

    internal void AdjustScroll ()
    {
        if (SuperViewAsScrollBar is { })
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

    internal ScrollBar? SuperViewAsScrollBar => SuperView as ScrollBar;

    private int BarSize => Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;

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
