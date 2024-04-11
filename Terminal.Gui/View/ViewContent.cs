﻿using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Settings for how scrolling the <see cref="View.Viewport"/> on the View's Content Area is handled.
/// </summary>
[Flags]
public enum ScrollSettings
{
    /// <summary>
    ///     No settings.
    /// </summary>
    None = 0,

    /// <summary>
    ///     If set, <c>Viewport.Location.Y</c> can be negative or greater than to <see cref="View.ContentSize"/>.<c>Height</c>,
    ///     enabling scrolling beyond the dimensions of the content area vertically.
    /// </summary>
    AllowViewportYBeyondContent = 1,

    /// <summary>
    ///     If set, <c>Viewport.Location.X</c> can be negative or greater than to <see cref="View.ContentSize"/>.<c>Width</c>,
    ///     enabling scrolling beyond the dimensions of the content area horizontally.
    /// </summary>
    AllowViewportXBeyondContent = 2,

    /// <summary>
    ///     If set, <c>Viewport.Location</c> can be negative or greater than to <see cref="View.ContentSize"/>,
    ///     enabling scrolling beyond the dimensions of the content area either horizontally or vertically.
    /// </summary>
    AllowViewportLocationBeyondContent = AllowViewportYBeyondContent | AllowViewportXBeyondContent
}

public partial class View
{
    #region Content Area

    private Size _contentSize;

    /// <summary>
    ///     Gets or sets the size of the View's content. If the value is <c>Size.Empty</c> the size of the content is
    ///     the same as the size of <see cref="Viewport"/>, and <c>Viewport.Location</c> will always be <c>0, 0</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a positive size is provided, <see cref="Viewport"/> describes the portion of the content currently visible
    ///         to the view. This enables virtual scrolling.
    ///     </para>
    ///     <para>
    ///         Negative sizes are not supported.
    ///     </para>
    /// </remarks>
    public Size ContentSize
    {
        get => _contentSize == Size.Empty ? Viewport.Size : _contentSize;
        set
        {
            if (value.Width < 0 || value.Height < 0)
            {
                throw new ArgumentException (@"ContentSize cannot be negative.", nameof (value));
            }

            if (value == _contentSize)
            {
                return;
            }

            _contentSize = value;
            OnContentSizeChanged (new (_contentSize));
        }
    }

    /// <summary>
    ///     Called when <see cref="ContentSize"/> changes. Invokes the <see cref="ContentSizeChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected bool? OnContentSizeChanged (SizeChangedEventArgs e)
    {
        ContentSizeChanged?.Invoke (this, e);

        if (e.Cancel != true)
        {
            SetNeedsDisplay ();
        }

        return e.Cancel;
    }

    /// <summary>
    ///     Event that is raised when the <see cref="ContentSize"/> changes.
    /// </summary>
    public event EventHandler<SizeChangedEventArgs> ContentSizeChanged;

    /// <summary>
    ///     Converts a Content-relative location to a Screen-relative location.
    /// </summary>
    /// <param name="location">The Content-relative location.</param>
    /// <returns>The Screen-relative location.</returns>
    public Point ContentToScreen (in Point location)
    {
        // Subtract the ViewportOffsetFromFrame to get the Viewport-relative location.
        Point viewportOffset = GetViewportOffsetFromFrame ();
        Point contentRelativeToViewport = location;
        contentRelativeToViewport.Offset (-Viewport.X, -Viewport.Y);

        // Translate to Screen-Relative (our SuperView's Viewport-relative coordinates)
        Rectangle screen = ViewportToScreen (new (contentRelativeToViewport, Size.Empty));

        return screen.Location;
    }

    /// <summary>Converts a Screen-relative coordinate to a Content-relative coordinate.</summary>
    /// <remarks>
    ///     Content-relative means relative to the top-left corner of the view's Content, which is
    ///     always at <c>0, 0</c>.
    /// </remarks>
    /// <param name="x">Column relative to the left side of the Content.</param>
    /// <param name="y">Row relative to the top of the Content</param>
    /// <returns>The coordinate relative to this view's Content.</returns>
    public Point ScreenToContent (in Point location)
    {
        Point viewportOffset = GetViewportOffsetFromFrame ();
        Point screen = ScreenToFrame (location.X, location.Y);
        screen.Offset (Viewport.X - viewportOffset.X, Viewport.Y - viewportOffset.Y);

        return screen;
    }

    #endregion Content Area

    #region Viewport

    private ScrollSettings _scrollSettings;

    /// <summary>
    ///     Gets or sets how scrolling the <see cref="View.Viewport"/> on the View's Content Area is handled.
    /// </summary>
    public ScrollSettings ScrollSettings
    {
        get => _scrollSettings;
        set
        {
            if (_scrollSettings == value)
            {
                return;
            }

            _scrollSettings = value;

            // Force set Viewport to cause settings to be applied as needed
            SetViewport (Viewport);
        }
    }

    /// <summary>
    ///     The location of the viewport into the view's content (0,0) is the top-left corner of the content. The Content
    ///     area's size
    ///     is <see cref="ContentSize"/>.
    /// </summary>
    private Point _viewportLocation;

    /// <summary>
    ///     Gets or sets the rectangle describing the portion of the View's content that is visible to the user.
    ///     The viewport Location is relative to the top-left corner of the inner rectangle of <see cref="Padding"/>.
    ///     If the viewport Size is the same as <see cref="ContentSize"/> the Location will be <c>0, 0</c>.
    /// </summary>
    /// <value>
    ///     The rectangle describing the location and size of the viewport into the View's virtual content, described by
    ///     <see cref="ContentSize"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         Positive values for the location indicate the visible area is offset into (down-and-right) the View's virtual
    ///         <see cref="ContentSize"/>. This enables virtual scrolling.
    ///     </para>
    ///     <para>
    ///         Negative values for the location indicate the visible area is offset above (up-and-left) the View's virtual
    ///         <see cref="ContentSize"/>. This enables virtual zoom.
    ///     </para>
    ///     <para>
    ///         The <see cref="ScrollSettings"/> property controls how scrolling is handled. If <see cref="ScrollSettings"/> is
    ///     </para>
    ///     <para>
    ///         If <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Computed"/> the value of Viewport is indeterminate until
    ///         the view has been initialized ( <see cref="IsInitialized"/> is true) and <see cref="LayoutSubviews"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Updates to the Viewport Size updates <see cref="Frame"/>, and has the same impact as updating the
    ///         <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         Altering the Viewport Size will eventually (when the view is next laid out) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    /// </remarks>
    public virtual Rectangle Viewport
    {
        get
        {
#if DEBUG
            if (LayoutStyle == LayoutStyle.Computed && !IsInitialized)
            {
                Debug.WriteLine (
                                 $"WARNING: Viewport is being accessed before the View has been initialized. This is likely a bug in {this}"
                                );
            }
#endif // DEBUG

            if (Margin is null || Border is null || Padding is null)
            {
                // CreateAdornments has not been called yet.
                return new (_viewportLocation, Frame.Size);
            }

            Thickness thickness = GetAdornmentsThickness ();

            return new (
                        _viewportLocation,
                        new (
                             Math.Max (0, Frame.Size.Width - thickness.Horizontal),
                             Math.Max (0, Frame.Size.Height - thickness.Vertical)
                            ));
        }
        set => SetViewport (value);
    }

    private void SetViewport (Rectangle viewport)
    {
        ApplySettings (ref viewport);

        Thickness thickness = GetAdornmentsThickness ();

        Size newSize = new (
                            viewport.Size.Width + thickness.Horizontal,
                            viewport.Size.Height + thickness.Vertical);

        if (newSize == Frame.Size)
        {
            // The change is not changing the Frame, so we don't need to update it.
            // Just call SetNeedsLayout to update the layout.
            if (_viewportLocation != viewport.Location)
            {
                _viewportLocation = viewport.Location;
                SetNeedsLayout ();
            }

            return;
        }

        _viewportLocation = viewport.Location;

        // Update the Frame because we made it bigger or smaller which impacts subviews.
        Frame = Frame with
        {
            Size = newSize
        };


        void ApplySettings (ref Rectangle location)
        {
            if (!ScrollSettings.HasFlag (ScrollSettings.AllowViewportYBeyondContent))
            {
                if (location.Y + Viewport.Height > ContentSize.Height)
                {
                    location.Y = ContentSize.Height - Viewport.Height;
                }

                if (location.Y < 0)
                {
                    location.Y = 0;
                }
            }

            if (!ScrollSettings.HasFlag (ScrollSettings.AllowViewportXBeyondContent))
            {
                if (location.X + Viewport.Width > ContentSize.Width)
                {
                    location.X = ContentSize.Width - Viewport.Width;
                }

                if (location.X < 0)
                {
                    location.X = 0;
                }
            }
        }
    }


    /// <summary>
    ///     Converts a <see cref="Viewport"/>-relative location to a screen-relative location.
    /// </summary>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    public Rectangle ViewportToScreen (in Rectangle location)
    {
        // Translate bounds to Frame (our SuperView's Viewport-relative coordinates)
        Rectangle screen = FrameToScreen ();
        Point viewportOffset = GetViewportOffsetFromFrame ();
        screen.Offset (viewportOffset.X + location.X, viewportOffset.Y + location.Y);

        return new (screen.Location, location.Size);
    }

    /// <summary>Converts a screen-relative coordinate to a Viewport-relative coordinate.</summary>
    /// <returns>The coordinate relative to this view's <see cref="Viewport"/>.</returns>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="x">Column relative to the left side of the Viewport.</param>
    /// <param name="y">Row relative to the top of the Viewport</param>
    public Point ScreenToViewport (int x, int y)
    {
        Point viewportOffset = GetViewportOffsetFromFrame ();
        Point screen = ScreenToFrame (x, y);
        screen.Offset (-viewportOffset.X, -viewportOffset.Y);

        return screen;
    }

    /// <summary>
    ///     Helper to get the X and Y offset of the Viewport from the Frame. This is the sum of the Left and Top properties
    ///     of <see cref="Margin"/>, <see cref="Border"/> and <see cref="Padding"/>.
    /// </summary>
    public Point GetViewportOffsetFromFrame () { return Padding is null ? Point.Empty : Padding.Thickness.GetInside (Padding.Frame).Location; }

    /// <summary>
    ///     Scrolls the view vertically by the specified number of rows.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     </para>
    /// </remarks>
    /// <param name="rows"></param>
    /// <returns><see langword="true"/> if the <see cref="Viewport"/> was changed.</returns>
    public bool? ScrollVertical (int rows)
    {
        if (ContentSize == Size.Empty || ContentSize == Viewport.Size)
        {
            return false;
        }

        Viewport = Viewport with { Y = Viewport.Y + rows };

        return true;
    }

    /// <summary>
    ///     Scrolls the view horizontally by the specified number of columns.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     </para>
    /// </remarks>
    /// <param name="cols"></param>
    /// <returns><see langword="true"/> if the <see cref="Viewport"/> was changed.</returns>
    public bool? ScrollHorizontal (int cols)
    {
        if (ContentSize == Size.Empty || ContentSize == Viewport.Size)
        {
            return false;
        }

        Viewport = Viewport with { X = Viewport.X + cols };

        return true;
    }

    #endregion Viewport
}