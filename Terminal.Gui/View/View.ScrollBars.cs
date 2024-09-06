#nullable enable
namespace Terminal.Gui;

public partial class View
{
    private Lazy<ScrollBar> _horizontalScrollBar;
    private Lazy<ScrollBar> _verticalScrollBar;

    /// <summary>
    ///     Initializes the ScrollBars of the View. Called by the constructor.
    /// </summary>
    private void SetupScrollBars ()
    {
        _horizontalScrollBar = new (
                                    () =>
                                    {
                                        var scrollBar = new ScrollBar
                                        {
                                            Orientation = Orientation.Horizontal,
                                            X = 0,
                                            Y = Pos.AnchorEnd (),
                                            Width = Dim.Fill (
                                                              Dim.Func (
                                                                        () =>
                                                                        {
                                                                            if (_verticalScrollBar.IsValueCreated)
                                                                            {
                                                                                return _verticalScrollBar.Value.Visible ? 1 : 0;
                                                                            }

                                                                            return 0;
                                                                        })),
                                            Size = GetContentSize ().Width,
                                            Visible = false
                                        };

                                        Padding?.Add (scrollBar);

                                        scrollBar.Initialized += (_, _) =>
                                        {
                                            Padding!.Thickness = Padding.Thickness with
                                            {
                                                Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : 0
                                            };

                                            scrollBar.PositionChanged += (_, args) =>
                                            {
                                                Viewport = Viewport with { X = args.CurrentValue };
                                            };

                                            scrollBar.VisibleChanged += (_, _) =>
                                            {
                                                Padding.Thickness = Padding.Thickness with
                                                {
                                                    Bottom = scrollBar.Visible
                                                        ? Padding.Thickness.Bottom + 1
                                                        : Padding.Thickness.Bottom - 1
                                                };
                                            };
                                        };

                                        return scrollBar;
                                    });

        _verticalScrollBar = new (
                                  () =>
                                  {
                                      var scrollBar = new ScrollBar
                                      {
                                          Orientation = Orientation.Vertical,
                                          X = Pos.AnchorEnd (),
                                          Y = Pos.Func (() => Padding.Thickness.Top),
                                          Height = Dim.Fill (
                                                             Dim.Func (
                                                                       () =>
                                                                       {
                                                                           if (_horizontalScrollBar.IsValueCreated)
                                                                           {
                                                                               return _horizontalScrollBar.Value.Visible ? 1 : 0;
                                                                           }

                                                                           return 0;
                                                                       })),
                                          Size = GetContentSize ().Height,
                                          Visible = false
                                      };

                                      Padding?.Add (scrollBar);

                                      scrollBar.Initialized += (_, _) =>
                                      {
                                          if (Padding is { })
                                          {
                                              Padding.Thickness = Padding.Thickness with
                                              {
                                                  Right = scrollBar.Visible ? Padding.Thickness.Right + 1 : 0
                                              };

                                              scrollBar.PositionChanged += (_, args) =>
                                                                           {
                                                                               Viewport = Viewport with { Y = args.CurrentValue };
                                                                           };

                                              scrollBar.VisibleChanged += (_, _) =>
                                                                          {
                                                                              Padding.Thickness = Padding.Thickness with
                                                                              {
                                                                                  Right = scrollBar.Visible
                                                                                      ? Padding.Thickness.Right + 1
                                                                                      : Padding.Thickness.Right - 1
                                                                              };
                                                                          };
                                          }
                                      };

                                      return scrollBar;
                                  });

        ViewportChanged += (_, _) =>
        {
            if (_verticalScrollBar.IsValueCreated)
            {
                _verticalScrollBar.Value.Position = Viewport.Y;
            }

            if (_horizontalScrollBar.IsValueCreated)
            {
                _horizontalScrollBar.Value.Position = Viewport.X;
            }
        };

        ContentSizeChanged += (_, _) =>
        {
            if (_verticalScrollBar.IsValueCreated)
            {
                _verticalScrollBar.Value.Size = GetContentSize ().Height;
            }
            if (_horizontalScrollBar.IsValueCreated)
            {
                _horizontalScrollBar.Value.Size = GetContentSize ().Width;
            }
        };
    }

    /// <summary>
    /// </summary>
    public ScrollBar HorizontalScrollBar => _horizontalScrollBar.Value;

    /// <summary>
    /// </summary>
    public ScrollBar VerticalScrollBar => _verticalScrollBar.Value;

    /// <summary>
    ///     Clean up the ScrollBars of the View. Called by View.Dispose.
    /// </summary>
    private void DisposeScrollBars ()
    {
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

    private void SetScrollBarsKeepContentInAllViewport (ViewportSettings viewportSettings)
    {
        if (viewportSettings == ViewportSettings.None)
        {
            _horizontalScrollBar.Value.KeepContentInAllViewport = true;
            _verticalScrollBar.Value.KeepContentInAllViewport = true;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowNegativeX))
        {
            _horizontalScrollBar.Value.AutoHide = false;
            _horizontalScrollBar.Value.ShowScrollIndicator = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowNegativeY))
        {
            _verticalScrollBar.Value.AutoHide = false;
            _verticalScrollBar.Value.ShowScrollIndicator = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowNegativeLocation))
        {
            _horizontalScrollBar.Value.AutoHide = false;
            _horizontalScrollBar.Value.ShowScrollIndicator = false;
            _verticalScrollBar.Value.AutoHide = false;
            _verticalScrollBar.Value.ShowScrollIndicator = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowXGreaterThanContentWidth))
        {
            _horizontalScrollBar.Value.KeepContentInAllViewport = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowYGreaterThanContentHeight))
        {
            _verticalScrollBar.Value.KeepContentInAllViewport = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowLocationGreaterThanContentSize))
        {
            _horizontalScrollBar.Value.KeepContentInAllViewport = false;
            _verticalScrollBar.Value.KeepContentInAllViewport = false;
        }
    }
}
