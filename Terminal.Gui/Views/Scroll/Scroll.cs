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
    private bool _keepContentInAllViewport = true;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        AdjustScroll ();
    }

    /// <summary>Get or sets if the view-port is kept in all visible area of this <see cref="Scroll"/></summary>
    public bool KeepContentInAllViewport
    {
        get => _keepContentInAllViewport;
        set
        {
            if (_keepContentInAllViewport != value)
            {
                _keepContentInAllViewport = value;
                var pos = 0;

                if (value
                    && Orientation == Orientation.Horizontal
                    && _position + (SuperViewAsScrollBar is { } ? SuperViewAsScrollBar.Viewport.Width : Viewport.Width) > Size)
                {
                    pos = Size - (SuperViewAsScrollBar is { } ? SuperViewAsScrollBar.Viewport.Width : Viewport.Width);
                }

                if (value
                    && Orientation == Orientation.Vertical
                    && _position + (SuperViewAsScrollBar is { } ? SuperViewAsScrollBar.Viewport.Height : Viewport.Height) > Size)
                {
                    pos = _size - (SuperViewAsScrollBar is { } ? SuperViewAsScrollBar.Viewport.Height : Viewport.Height);
                }

                if (pos != 0)
                {
                    Position = pos;
                }

                SetNeedsDisplay ();
                AdjustScroll ();
            }
        }
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

            int pos = SetPosition (value);

            if (pos == _position)
            {
                return;
            }

            CancelEventArgs<int> args = OnPositionChanging (_position, pos);

            if (args.Cancel)
            {
                return;
            }

            _position = pos;

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
            if (value == _size || value < 0)
            {
                return;
            }

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

        (int start, int end) sliderPos = _orientation == Orientation.Vertical
                                             ? new (_slider.Frame.Y, _slider.Frame.Bottom - 1)
                                             : new (_slider.Frame.X, _slider.Frame.Right - 1);

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) && location < sliderPos.start)
        {
            Position = Math.Max (Position - barSize, 0);
        }
        else if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) && location > sliderPos.end)
        {
            Position = Math.Min (Position + barSize, Size - barSize + (KeepContentInAllViewport ? 0 : barSize));
        }
        else if ((mouseEvent.Flags == MouseFlags.WheeledDown && Orientation == Orientation.Vertical)
                 || (mouseEvent.Flags == MouseFlags.WheeledRight && Orientation == Orientation.Horizontal))
        {
            Position = Math.Min (Position + 1, Size - barSize + (KeepContentInAllViewport ? 0 : barSize));
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

    private int SetPosition (int position)
    {
        int barSize = BarSize;

        if (position + barSize > Size)
        {
            return KeepContentInAllViewport ? Math.Max (Size - barSize, 0) : Math.Max (Size - 1, 0);
        }

        return position;
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
