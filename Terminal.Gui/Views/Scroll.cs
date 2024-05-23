//
// Scroll.cs: Vertical or horizontal scroll
//
// Author: BDisp
//
// Licensed under the MIT license
//

namespace Terminal.Gui;

/// <summary>
///     Represents the "inside part" of a scroll bar, minus the arrows.
/// </summary>
public class Scroll : View
{
    /// <inheritdoc/>
    public Scroll ()
    {
        WantContinuousButtonPressed = true;
        ClearOnVisibleFalse = false;
        CanFocus = false;
        Orientation = Orientation.Vertical;

        _sliderContainer = new () { Width = Dim.Fill (), Height = Dim.Fill (), WantContinuousButtonPressed = true };
        _slider = new () { Id = "slider" };
        _sliderContainer.Add (_slider);
        Add (_sliderContainer);

        Added += Scroll_Added;
        Removed += Scroll_Removed;
        Initialized += Scroll_Initialized;
        _sliderContainer.DrawContent += SubViews_DrawContent;
        _sliderContainer.MouseEvent += SliderContainer_MouseEvent;
        _slider.DrawContent += SubViews_DrawContent;
        _slider.MouseEvent += Slider_MouseEvent;
    }

    private readonly View _sliderContainer;
    private readonly View _slider;
    private int _lastLocation = -1;

    private Orientation _orientation;
    private int _size;
    private int _position;

    private bool _wasSliderMouse;

    /// <summary>
    ///     Determines the Orientation of the scroll.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            SetWidthHeight ();
        }
    }

    /// <summary>
    ///     The position, relative to <see cref="Size"/>, to set the scrollbar at.
    /// </summary>
    public int Position
    {
        get => _position;
        set
        {
            int barSize = Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;

            if (value < 0 || (value > 0 && value + barSize > Size))
            {
                return;
            }

            StateEventArgs<int> args = OnPositionChanging (_position, value);

            if (args.Cancel)
            {
                return;
            }

            int oldPos = _position;
            _position = value;
            OnPositionChanged (oldPos);

            if (!_wasSliderMouse)
            {
                SetWidthHeight ();
            }
        }
    }

    /// <summary>This event is raised when the position on the scrollbar has changed.</summary>
    public event EventHandler<StateEventArgs<int>> PositionChanged;

    /// <summary>This event is raised when the position on the scrollbar is changing.</summary>
    public event EventHandler<StateEventArgs<int>> PositionChanging;

    /// <summary>
    ///     The size of content the scroll represents.
    /// </summary>
    public int Size
    {
        get => _size;
        set
        {
            int oldSize = _size;
            _size = value;
            OnSizeChanged (oldSize);
            SetWidthHeight ();
        }
    }

    /// <summary>This event is raised when the size of the scroll has changed.</summary>
    public event EventHandler<StateEventArgs<int>> SizeChanged;

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Added -= Scroll_Added;
        Initialized -= Scroll_Initialized;
        _sliderContainer.DrawContent -= SubViews_DrawContent;
        _sliderContainer.MouseEvent -= SliderContainer_MouseEvent;
        _slider.DrawContent -= SubViews_DrawContent;
        _slider.MouseEvent -= Slider_MouseEvent;

        base.Dispose (disposing);
    }

    /// <summary>Virtual method to invoke the <see cref="PositionChanged"/> event handler.</summary>
    protected virtual void OnPositionChanged (int oldPos) { PositionChanged?.Invoke (this, new (oldPos, Position)); }

    /// <summary>Virtual method to invoke the cancelable <see cref="PositionChanging"/> event handler.</summary>
    protected virtual StateEventArgs<int> OnPositionChanging (int oldPos, int newPos)
    {
        StateEventArgs<int> args = new (oldPos, newPos);
        PositionChanging?.Invoke (this, args);

        return args;
    }

    /// <summary>Virtual method to invoke the <see cref="SizeChanged"/> event handler.</summary>
    protected void OnSizeChanged (int oldSize) { SizeChanged?.Invoke (this, new (oldSize, Size)); }

    private int GetPositionFromSliderLocation (int location)
    {
        if (Frame.Height == 0 || Frame.Width == 0)
        {
            return 0;
        }

        int barSize = Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;

        return Math.Min (location * Size / barSize, Size - barSize);
    }

    private (int Location, int Dimension) GetSliderLocationDimensionFromPosition ()
    {
        if (Frame.Height == 0 || Frame.Width == 0)
        {
            return new (0, 0);
        }

        int barSize = Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;
        int location;
        int dimension;

        if (Size > 0)
        {
            dimension = Math.Min (Math.Max (barSize * barSize / Size, 1), barSize);

            // Ensure the Position is valid
            if (Position > 0 && Position + barSize > Size)
            {
                Position = Size - barSize;
            }

            location = Math.Min (Position * barSize / Size, barSize - dimension);

            if (Position == Size - barSize && location + dimension < barSize)
            {
                location = barSize - dimension;
            }
        }
        else
        {
            location = 0;
            dimension = barSize;
        }

        return new (location, dimension);
    }

    private void Parent_LayoutComplete (object sender, LayoutEventArgs e)
    {
        if (!_wasSliderMouse)
        {
            SetWidthHeight ();
        }
        else
        {
            _wasSliderMouse = false;
        }
    }

    private void Parent_MouseEnter (object sender, MouseEventEventArgs e) { OnMouseEnter (e.MouseEvent); }

    private void Parent_MouseLeave (object sender, MouseEventEventArgs e) { OnMouseLeave (e.MouseEvent); }

    private void Scroll_Added (object sender, SuperViewChangedEventArgs e)
    {
        View parent = e.Parent is Adornment adornment ? adornment.Parent : e.Parent;

        parent.LayoutComplete += Parent_LayoutComplete;
        parent.MouseEnter += Parent_MouseEnter;
        parent.MouseLeave += Parent_MouseLeave;
    }

    private void Scroll_Initialized (object sender, EventArgs e) { SetWidthHeight (); }

    private void Scroll_Removed (object sender, SuperViewChangedEventArgs e)
    {
        if (e.Parent is { })
        {
            View parent = e.Parent is Adornment adornment ? adornment.Parent : e.Parent;

            parent.LayoutComplete -= Parent_LayoutComplete;
            parent.MouseEnter -= Parent_MouseEnter;
            parent.MouseLeave -= Parent_MouseLeave;
        }
    }

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
        _sliderContainer.TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        _sliderContainer.Text = string.Concat (
                                               Enumerable.Repeat (
                                                                  Glyphs.Stipple.ToString (),
                                                                  Orientation == Orientation.Vertical
                                                                      ? _sliderContainer.Frame.Height
                                                                      : _sliderContainer.Frame.Width));
        _slider.TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        _slider.Text = string.Concat (
                                      Enumerable.Repeat (
                                                         Glyphs.ContinuousMeterSegment.ToString (),
                                                         Orientation == Orientation.Vertical
                                                             ? _sliderContainer.Frame.Height
                                                             : _slider.Frame.Width));
    }

    private void SetWidthHeight ()
    {
        if (Orientation == Orientation.Vertical)
        {
            Width = 1;
        }
        else
        {
            Height = 1;
        }

        if (!IsInitialized)
        {
            return;
        }

        _sliderContainer.Width = Orientation == Orientation.Vertical ? 1 : Dim.Fill ();
        _sliderContainer.Height = Orientation == Orientation.Vertical ? Dim.Fill () : 1;

        (int Location, int Dimension) slider = GetSliderLocationDimensionFromPosition ();
        _slider.X = Orientation == Orientation.Vertical ? 0 : slider.Location;
        _slider.Y = Orientation == Orientation.Vertical ? slider.Location : 0;
        _slider.Width = Orientation == Orientation.Vertical ? 1 : slider.Dimension;
        _slider.Height = Orientation == Orientation.Vertical ? slider.Dimension : 1;

        SetSliderText ();
    }

    private void Slider_MouseEvent (object sender, MouseEventEventArgs e)
    {
        MouseEvent me = e.MouseEvent;
        int location = Orientation == Orientation.Vertical ? me.Position.Y : me.Position.X;
        int barSize = Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;
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
        else
        {
            return;
        }

        e.Handled = true;
    }

    private void SliderContainer_MouseEvent (object sender, MouseEventEventArgs e)
    {
        MouseEvent me = e.MouseEvent;
        int location = Orientation == Orientation.Vertical ? me.Position.Y : me.Position.X;
        int barSize = Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;

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
    }

    private void SubViews_DrawContent (object sender, DrawEventArgs e) { SetColorSchemeWithSuperview (sender as View); }
}
