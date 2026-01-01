
namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Lazy<ScrollBar> _horizontalScrollBar = null!;

    /// <summary>
    ///     Gets the horizontal <see cref="ScrollBar"/>. This property is lazy-loaded and will not be created until it is accessed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="ScrollBar"/> for more information on how to use the ScrollBar.
    ///     </para>
    /// </remarks>
    public ScrollBar HorizontalScrollBar => _horizontalScrollBar.Value;

    private Lazy<ScrollBar> _verticalScrollBar = null!;

    /// <summary>
    ///     Gets the vertical <see cref="ScrollBar"/>. This property is lazy-loaded and will not be created until it is accessed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="ScrollBar"/> for more information on how to use the ScrollBar.
    ///     </para>
    /// </remarks>
    public ScrollBar VerticalScrollBar => _verticalScrollBar.Value;

    /// <summary>
    ///     Initializes the ScrollBars of the View. Called by the View constructor.
    /// </summary>
    private void SetupScrollBars ()
    {
        if (this is Adornment)
        {
            return;
        }

        _verticalScrollBar = new (() => CreateScrollBar (Orientation.Vertical));
        _horizontalScrollBar = new (() => CreateScrollBar (Orientation.Horizontal));
    }

    private ScrollBar CreateScrollBar (Orientation orientation)
    {
        var scrollBar = new ScrollBar
        {
            Orientation = orientation,
            Visible = false // Initially hidden until needed
        };

        if (orientation == Orientation.Vertical)
        {
            ConfigureVerticalScrollBar (scrollBar);
        }
        else
        {
            ConfigureHorizontalScrollBar (scrollBar);
        }

        scrollBar.Initialized += OnScrollBarInitialized;

        // Add after setting Initialized event!
        Padding?.Add (scrollBar);

        return scrollBar;
    }

    private void ConfigureVerticalScrollBar (ScrollBar scrollBar)
    {
        // Use dynamic Func to set the location & size. Note the -1 for X is to subtract
        // the Padding we add for the scrollbar. The Func is only called if the ScrollBar is
        // visible.
        scrollBar.X = Pos.AnchorEnd () - Pos.Func (_ => Padding!.Thickness.Right - 1);
        scrollBar.Y = Pos.Func (_ => Padding!.Thickness.Top);

        scrollBar.Height = Dim.Fill (Dim.Func (_ => Padding!.Thickness.Bottom));
        scrollBar.ScrollableContentSize = GetContentSize ().Height;

        ViewportChanged += (_, _) =>
                           {
                               scrollBar.Position = Viewport.Y;
                           };

        ContentSizeChanged += (_, _) =>
                              {
                                  scrollBar.ScrollableContentSize = GetContentSize ().Height;
                              };
    }

    private void ConfigureHorizontalScrollBar (ScrollBar scrollBar)
    {
        // Use dynamic Func to set the location & size. Note the -1 for Y is to subtract
        // the Padding we add for the scrollbar. The Func is only called if the ScrollBar is
        // visible.
        scrollBar.X = Pos.Func (_ => Padding!.Thickness.Left);
        scrollBar.Y = Pos.AnchorEnd () - Pos.Func (_ => Padding!.Thickness.Bottom - 1);

        scrollBar.Width = Dim.Fill (Dim.Func (_ => Padding!.Thickness.Right));
        scrollBar.ScrollableContentSize = GetContentSize ().Width;

        ViewportChanged += (_, _) =>
                           {
                               scrollBar.Position = Viewport.X;
                           };

        ContentSizeChanged += (_, _) => { scrollBar.ScrollableContentSize = GetContentSize ().Width; };
    }

    private void OnScrollBarInitialized (object? sender, EventArgs e)
    {
        var scrollBar = (ScrollBar)sender!;

        if (scrollBar.Orientation == Orientation.Vertical)
        {
            ConfigureVerticalScrollBarEvents (scrollBar);
        }
        else
        {
            ConfigureHorizontalScrollBarEvents (scrollBar);
        }
    }

    private void ConfigureVerticalScrollBarEvents (ScrollBar scrollBar)
    {
        Padding!.Thickness = Padding.Thickness with { Right = scrollBar.Visible ? Padding.Thickness.Right + 1 : Padding.Thickness.Right };

        scrollBar.PositionChanged += (_, args) =>
                                     {
                                         Viewport = Viewport with
                                         {
                                             Y = Math.Min (args.Value, scrollBar.ScrollableContentSize - scrollBar.VisibleContentSize)
                                         };
                                     };

        scrollBar.VisibleChanged += (_, _) =>
                                    {
                                        // Reset scrolling
                                        if (!scrollBar.Visible)
                                        {
                                            Viewport = Viewport with { Y = 0 };
                                        }

                                        Padding.Thickness = Padding.Thickness with
                                        {
                                            Right = scrollBar.Visible ? Padding.Thickness.Right + 1 : Padding.Thickness.Right - 1
                                        };

                                        // BUGBUG: This Layout call is a hack to work around some bug in Layout.
                                        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/4522
                                        // Layout ();
                                    };
    }

    private void ConfigureHorizontalScrollBarEvents (ScrollBar scrollBar)
    {
        Padding!.Thickness = Padding.Thickness with { Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : Padding.Thickness.Bottom };

        scrollBar.PositionChanged += (_, args) =>
                                     {
                                         Viewport = Viewport with
                                         {
                                             X = Math.Min (args.Value, scrollBar.ScrollableContentSize - scrollBar.VisibleContentSize)
                                         };
                                     };

        scrollBar.VisibleChanged += (_, _) =>
                                    {
                                        // Reset scrolling
                                        if (!scrollBar.Visible)
                                        {
                                            Viewport = Viewport with { X = 0 };
                                        }

                                        Padding.Thickness = Padding.Thickness with
                                        {
                                            Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : Padding.Thickness.Bottom - 1
                                        };

                                        // BUGBUG: This Layout call is a hack to work around some bug in Layout.
                                        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/4522
                                        // Layout ();
                                    };
    }

    /// <summary>
    ///     Clean up the ScrollBars of the View. Called by View.Dispose.
    /// </summary>
    private void DisposeScrollBars ()
    {
        if (this is Adornment)
        {
            return;
        }

        if (_horizontalScrollBar.IsValueCreated)
        {
            Padding?.Remove (_horizontalScrollBar.Value);
            _horizontalScrollBar.Value.Dispose ();
        }

        if (_verticalScrollBar.IsValueCreated)
        {
            Padding?.Remove (_verticalScrollBar.Value);
            _verticalScrollBar.Value.Dispose ();
        }
    }
}
