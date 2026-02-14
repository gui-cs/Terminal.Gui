namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Lazy<ScrollBar> _horizontalScrollBar = null!;

    /// <summary>
    ///     Gets the horizontal <see cref="ScrollBar"/>. This property is lazy-loaded and will not be created until it is
    ///     accessed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ViewportSettings"/> controls how scrolling behaves for the View.
    ///     </para>
    ///     <para>
    ///         See <see cref="ScrollBar"/> for more information on how to use the ScrollBar.
    ///     </para>
    ///     <para>
    ///         See the Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    /// </remarks>
    public ScrollBar HorizontalScrollBar => _horizontalScrollBar.Value;

    private Lazy<ScrollBar> _verticalScrollBar = null!;

    /// <summary>
    ///     Gets the vertical <see cref="ScrollBar"/>. This property is lazy-loaded and will not be created until it is
    ///     accessed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ViewportSettings"/> controls how scrolling behaves for the View.
    ///     </para>
    ///     <para>
    ///         See <see cref="ScrollBar"/> for more information on how to use the ScrollBar.
    ///     </para>
    ///     <para>
    ///         See the Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>    /// </remarks>
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

        _verticalScrollBar = new Lazy<ScrollBar> (() => CreateScrollBar (Orientation.Vertical));
        _horizontalScrollBar = new Lazy<ScrollBar> (() => CreateScrollBar (Orientation.Horizontal));
    }

    private ScrollBar CreateScrollBar (Orientation orientation)
    {
        ScrollBar scrollBar = new ()
        {
            Orientation = orientation, Visible = false // Initially hidden until needed
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

        ViewportChanged += (_, _) => { scrollBar.Value = Viewport.Y; };

        ContentSizeChanged += (_, _) => { scrollBar.ScrollableContentSize = GetContentSize ().Height; };
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

        ViewportChanged += (_, _) => { scrollBar.Value = Viewport.X; };

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

        scrollBar.ValueChanged += (_, args) =>
                                  {
                                      Viewport = Viewport with { Y = Math.Min (args.NewValue, scrollBar.ScrollableContentSize - scrollBar.VisibleContentSize) };
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
                                    };
    }

    private void ConfigureHorizontalScrollBarEvents (ScrollBar scrollBar)
    {
        Padding!.Thickness = Padding.Thickness with { Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : Padding.Thickness.Bottom };

        scrollBar.ValueChanged += (_, args) =>
                                  {
                                      Viewport = Viewport with { X = Math.Min (args.NewValue, scrollBar.ScrollableContentSize - scrollBar.VisibleContentSize) };
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
                                    };
    }

    /// <summary>
    ///     Synchronizes the ScrollBar states with the ViewportSettings flags.
    ///     Called when ViewportSettings changes to enable/disable built-in scrollbars.
    /// </summary>
    private void SyncScrollBarsToSettings (ViewportSettingsFlags oldFlags, ViewportSettingsFlags newFlags)
    {
        if (this is Adornment)
        {
            return;
        }

        SyncOneScrollBar (
            oldFlags.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar),
            newFlags.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar),
            Orientation.Vertical
        );

        SyncOneScrollBar (
            oldFlags.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar),
            newFlags.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar),
            Orientation.Horizontal
        );
    }

    private void SyncOneScrollBar (bool hadFlag, bool hasFlag, Orientation orientation)
    {
        if (!hadFlag && hasFlag)
        {
            // Enabling: access triggers lazy creation, then set Auto mode
            ScrollBar scrollBar = orientation == Orientation.Vertical ? VerticalScrollBar : HorizontalScrollBar;
            scrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;
        }
        else if (hadFlag && !hasFlag)
        {
            // Disabling: only if the scrollbar was ever created
            Lazy<ScrollBar> lazy = orientation == Orientation.Vertical ? _verticalScrollBar : _horizontalScrollBar;

            if (lazy.IsValueCreated)
            {
                lazy.Value.VisibilityMode = ScrollBarVisibilityMode.Manual;
                lazy.Value.Visible = false;
            }
        }
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
