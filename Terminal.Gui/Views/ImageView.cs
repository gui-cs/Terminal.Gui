namespace Terminal.Gui.Views;

/// <summary>
///     Displays an image represented as a 2D array of <see cref="Color"/> pixels.
///     Supports two rendering modes: cell-based (one colored space per pixel, works everywhere)
///     and sixel-based (when the terminal supports it).
/// </summary>
/// <remarks>
///     <para>
///         The image data is provided via the <see cref="Image"/> property as a <c>Color[,]</c> array
///         where the first dimension is width (x) and the second is height (y). Image loading and
///         decoding from file formats (PNG, JPEG, etc.) is the caller's responsibility — this view
///         has no dependency on any image library.
///     </para>
///     <para>
///         When sixel is available (detected via <see cref="IDriver.SixelSupport"/>) and
///         <see cref="UseSixel"/> is <see langword="true"/>, the view will encode the image as
///         sixel escape sequences and render it through the driver's output buffer. Sixel data
///         is only re-sent to the terminal when <see cref="View.NeedsDraw"/> is true,
///         avoiding redundant rendering of unchanged images.
///     </para>
///     <para>
///         When sixel is not available, the view falls back to cell-based rendering where each
///         terminal cell is colored with the background color of the corresponding pixel.
///     </para>
/// </remarks>
public class ImageView : View, IDesignable
{
    private const double MAX_ZOOM_LEVEL = 64d;
    private const double MIN_ZOOM_LEVEL = 1d;
    private const double ZOOM_FACTOR = 1.25d;
    private const int DEFAULT_MAX_SIXEL_PALETTE_COLORS = 64;

    private Color [,]? _image;
    private Color [,]? _scaledImage;
    private Size? _scaledImageCellSize;
    private RenderKey? _scaledImageRenderKey;
    private double _centerX = 0.5d;
    private double _centerY = 0.5d;
    private RenderKey? _backgroundRenderKey;
    private bool _backgroundRenderRunning;
    private bool _disposed;
    private Point? _lastDragPosition;
    private int _imageVersion;
    private SixelEncoder? _sixelEncoder;
    private RenderRequest? _queuedRenderRequest;
    private bool _usesDefaultSixelEncoder;
    private double _zoomLevel = MIN_ZOOM_LEVEL;
    private string RasterImageId => $"ImageView_{GetHashCode ()}";

    // Cell-based rendering cache
    private readonly Dictionary<Color, Attribute> _attributeCache = new ();
    private readonly Lock _renderLock = new ();

    private readonly Label _renderingOverlay = new ()
    {
        Text = "Rendering...",
        X = Pos.Center (),
        Y = Pos.Center (),
        CanFocus = false,
        Visible = false
    };

    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="ImageView"/>.
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        [Command.ScrollLeft] = Bind.All (Key.CursorLeft),
        [Command.ScrollRight] = Bind.All (Key.CursorRight),
        [Command.ScrollUp] = Bind.All (Key.CursorUp),
        [Command.ScrollDown] = Bind.All (Key.CursorDown),
        [Command.Home] = Bind.All (Key.Home),
        [Command.ZoomOut] = Bind.All (Key.PageUp),
        [Command.ZoomIn] = Bind.All (Key.PageDown)
    };

    /// <summary>
    ///     Gets or sets the default mouse bindings for <see cref="ImageView"/>.
    /// </summary>
    public new static Dictionary<Command, PlatformMouseBinding>? DefaultMouseBindings { get; set; } = new ()
    {
        [Command.ZoomIn] = BindMouse.All (MouseFlags.WheeledUp),
        [Command.ZoomOut] = BindMouse.All (MouseFlags.WheeledDown),
        [Command.Center] = BindMouse.All (MouseFlags.LeftButtonDoubleClicked)
    };

    /// <summary>Initializes a new instance of the <see cref="ImageView"/> class.</summary>
    public ImageView ()
    {
        CanFocus = true;
        MousePositionTracking = true;

        AddCommand (Command.ScrollLeft, () => ScrollFromCommand (-1, 0));
        AddCommand (Command.ScrollRight, () => ScrollFromCommand (1, 0));
        AddCommand (Command.ScrollUp, () => ScrollFromCommand (0, -1));
        AddCommand (Command.ScrollDown, () => ScrollFromCommand (0, 1));
        AddCommand (Command.Home, () => ResetView ());
        AddCommand (Command.ZoomIn, context => ZoomFromCommand (context, _zoomLevel * ZOOM_FACTOR));
        AddCommand (Command.ZoomOut, context => ZoomFromCommand (context, _zoomLevel / ZOOM_FACTOR));
        AddCommand (Command.PageDown, context => ZoomFromCommand (context, _zoomLevel * ZOOM_FACTOR));
        AddCommand (Command.PageUp, context => ZoomFromCommand (context, _zoomLevel / ZOOM_FACTOR));
        AddCommand (Command.Center, CenterFromCommand);

        ApplyKeyBindings (DefaultKeyBindings, View.DefaultKeyBindings);
        ApplyMouseBindings (DefaultMouseBindings, View.DefaultMouseBindings);
        ReplacePanAndZoomBindings ();
        Add (_renderingOverlay);
    }

    private void ReplacePanAndZoomBindings ()
    {
        KeyBindings.ReplaceCommands (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.ReplaceCommands (Key.CursorRight, Command.ScrollRight);
        KeyBindings.ReplaceCommands (Key.CursorUp, Command.ScrollUp);
        KeyBindings.ReplaceCommands (Key.CursorDown, Command.ScrollDown);
        KeyBindings.ReplaceCommands (Key.Home, Command.Home);
        KeyBindings.ReplaceCommands (Key.PageUp, Command.ZoomOut);
        KeyBindings.ReplaceCommands (Key.PageDown, Command.ZoomIn);

        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ZoomIn);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ZoomOut);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Center);
    }

    /// <summary>
    ///     Gets or sets the pixel data to display. The array is indexed as [x, y] where
    ///     the first dimension is width and the second is height.
    /// </summary>
    /// <remarks>
    ///     Setting this property marks the view as needing redraw. The image will be
    ///     scaled to fit the current <see cref="View.Viewport"/> while maintaining
    ///     aspect ratio using nearest-neighbor interpolation.
    /// </remarks>
    public Color [,]? Image
    {
        get => _image;
        set
        {
            _image = value;
            _imageVersion++;
            ClampCenter ();

            if (_image is null)
            {
                InvalidateScaledImage (true);
                App?.Driver?.GetOutputBuffer ().RemoveRasterImage (RasterImageId);

                return;
            }

            InvalidateScaledImage ();
        }
    }

    /// <summary>
    ///     Gets or sets whether to prefer sixel rendering when the terminal supports it.
    ///     Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true"/> and the terminal supports sixel
    ///     (per <see cref="IDriver.SixelSupport"/>), the image is rendered using sixel
    ///     escape sequences for full-resolution display. When <see langword="false"/>,
    ///     cell-based rendering is always used.
    /// </remarks>
    public bool UseSixel { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether ImageView scales image renders on a background thread.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="false"/>. When enabled, ImageView keeps the last completed render visible while
    ///     a newer render is being prepared and shows a centered "Rendering..." overlay until the background render is
    ///     ready.
    /// </remarks>
    public bool UseBackgroundRendering
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            if (!field)
            {
                SetRenderingOverlayVisible (false);
            }

            InvalidateScaledImage ();
        }
    }

    /// <summary>
    ///     Gets or sets the maximum number of colors to use when encoding ImageView sixel output.
    /// </summary>
    /// <remarks>
    ///     The default is 64 colors to keep interactive ImageView redraws responsive. The effective
    ///     encoder palette is also limited by <see cref="SixelSupportResult.MaxPaletteColors"/> and by
    ///     the configured <see cref="SixelEncoder.Quantizer"/> <c>MaxColors</c>.
    /// </remarks>
    public int MaxSixelPaletteColors
    {
        get;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"Maximum sixel palette colors must be greater than zero.");
            }

            if (field == value)
            {
                return;
            }

            field = value;
            InvalidateScaledImage ();
        }
    } = DEFAULT_MAX_SIXEL_PALETTE_COLORS;

    /// <summary>
    ///     Gets or sets whether sixel rendering may upscale the visible image region above its source pixel size.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="true"/> so the image fills the Viewport at <see cref="ZoomLevel"/> 1.
    ///     Set to <see langword="false"/> to avoid encoding more pixels than the source image provides during fit-to-view
    ///     rendering.
    /// </remarks>
    public bool AllowSixelUpscaling
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            InvalidateScaledImage ();
        }
    } = true;

    /// <summary>
    ///     Gets or sets the zoom level. A value of <c>1</c> fits the image in the viewport.
    /// </summary>
    public double ZoomLevel { get => _zoomLevel; set => SetZoomLevel (value, null); }

    /// <summary>
    ///     Gets or sets the <see cref="Drawing.SixelEncoder"/> used to encode images as sixel data.
    ///     When <see langword="null"/>, a default encoder is created lazily on first use.
    /// </summary>
    /// <remarks>
    ///     Set this to provide a custom encoder with specific quantizer settings, palette building
    ///     algorithms, or color distance algorithms. The encoder's
    ///     <see cref="Drawing.SixelEncoder.Quantizer"/> <c>MaxColors</c> will be clamped during
    ///     rendering. The default encoder is capped by <see cref="MaxSixelPaletteColors"/> and the
    ///     terminal's <see cref="SixelSupportResult.MaxPaletteColors"/>. Custom encoders are capped
    ///     only by the terminal's <see cref="SixelSupportResult.MaxPaletteColors"/>.
    /// </remarks>
    public SixelEncoder? SixelEncoder
    {
        get => _sixelEncoder;
        set
        {
            if (ReferenceEquals (_sixelEncoder, value))
            {
                return;
            }

            _sixelEncoder = value;
            _usesDefaultSixelEncoder = false;
            InvalidateScaledImage ();
        }
    }

    /// <summary>
    ///     Gets whether the current rendering mode is using sixel.
    /// </summary>
    public bool IsUsingSixel => UseSixel && App?.Driver?.SixelSupport is { IsSupported: true };

    /// <summary>
    ///     Converts the Viewport to screen coordinates in pixels.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method accounts for the terminal's cell resolution and the viewport's
    ///         size, returning the exact pixel dimensions and position required for
    ///         fully cover the viewport.
    ///     </para>
    /// </remarks>
    /// <returns>The screen coordinates of the Viewport in pixels.</returns>
    public Rectangle ViewportToScreenInPixels ()
    {
        SixelSupportResult support = App?.Driver?.SixelSupport ?? throw new InvalidOperationException (@"No sixel support available.");

        int pixelsPerCellX = support.Resolution.Width;
        int pixelsPerCellY = support.Resolution.Height;
        Rectangle boundsRect = ViewportToScreen ();

        // Calculate target size in pixels based on viewport and cell resolution
        int targetWidthInPixels = boundsRect.Width * pixelsPerCellX;
        int targetHeightInPixels = SixelEncoder?.GetHeightInPixels (boundsRect.Height, pixelsPerCellY) ?? boundsRect.Height * pixelsPerCellY;

        return new Rectangle (boundsRect.X * pixelsPerCellX, boundsRect.Y * pixelsPerCellY, targetWidthInPixels, targetHeightInPixels);
    }

    /// <summary>
    ///     Returns the size in cell terms of the given image resized to fit in the viewport.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method accounts for the terminal's cell resolution and the viewport's
    ///         size, returning the exact pixel dimensions and position required for
    ///         fully cover the viewport without changing the images aspect ratio.
    ///     </para>
    /// </remarks>
    /// <param name="imageSizeInPixels">The size of the image in pixels.</param>
    /// <returns>The largest possible size of the image in cell terms that fits within the viewport.</returns>
    public Size FitImageInViewportCells (Size imageSizeInPixels)
    {
        if (imageSizeInPixels.Width == 0 || imageSizeInPixels.Height == 0)
        {
            return Size.Empty;
        }

        // Account for the terminal cell aspect ratio
        double cellAspectRatio = App?.Driver?.SixelSupport is { } support ? (double)support.Resolution.Height / support.Resolution.Width : 2.0;
        Size imageSize = imageSizeInPixels with { Height = (int)(imageSizeInPixels.Height / cellAspectRatio) };

        // Calculate aspect-ratio-preserving size
        double widthScale = (double)Viewport.Width / imageSize.Width;
        double heightScale = (double)Viewport.Height / imageSize.Height;
        double scale = Math.Min (widthScale, heightScale);

        int newWidth = Math.Max (1, (int)(imageSize.Width * scale));
        int newHeight = Math.Max (1, (int)(imageSize.Height * scale));

        return new Size (newWidth, newHeight);
    }

    /// <summary>
    ///     Scales an image to fit within the current Viewport while maintaining aspect ratio.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method calculates the largest possible size for the given image that will fit
    ///         within the current <see cref="View.Viewport"/> while maintaining its aspect ratio.
    ///     </para>
    ///     <para>
    ///         The calculation is based on the terminal's cell resolution and the <see cref="View.Viewport"/>
    ///         size, returning the exact pixel dimensions and position required for the scaled image.
    ///     </para>
    /// </remarks>
    /// <param name="imageSize">The original size of the image to scale.</param>
    /// <returns>The scaled size of the image that fits within the <see cref="View.Viewport"/>.</returns>
    public Size FitImageInViewportInPixels (Size imageSize)
    {
        Rectangle viewportInPixels = ViewportToScreenInPixels ();

        if (imageSize.Width == 0 || imageSize.Height == 0)
        {
            return Size.Empty;
        }

        // Calculate aspect-ratio-preserving size
        double widthScale = (double)viewportInPixels.Width / imageSize.Width;
        double heightScale = (double)viewportInPixels.Height / imageSize.Height;
        double scale = Math.Min (widthScale, heightScale);

        int newWidth = Math.Max (1, (int)(imageSize.Width * scale));
        int newHeight = Math.Max (1, (int)(imageSize.Height * scale));

        return new Size (newWidth, newHeight);
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        base.OnDrawingContent (context);

        if (_image is null)
        {
            return true;
        }

        if (IsUsingSixel)
        {
            DrawSixel ();

            if (_scaledImageCellSize is not { } cellSize)
            {
                return true;
            }
            Rectangle viewport = ViewportToScreen ();
            Rectangle dirtyRect = viewport with { Width = Math.Min (viewport.Width, cellSize.Width), Height = Math.Min (viewport.Height, cellSize.Height) };
            context?.AddDrawnRectangle (dirtyRect);
        }
        else
        {
            DrawCellBased ();

            if (_scaledImageCellSize is not { } cellSize)
            {
                return true;
            }
            Rectangle viewport = ViewportToScreen ();
            Rectangle dirtyRect = viewport with { Width = Math.Min (viewport.Width, cellSize.Width), Height = Math.Min (viewport.Height, cellSize.Height) };
            context?.AddDrawnRectangle (dirtyRect);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (frame);
        InvalidateScaledImage ();
    }

    /// <summary>
    ///     Renders the image using cell-based rendering where each terminal cell
    ///     gets the background color of the corresponding pixel.
    /// </summary>
    private void DrawCellBased ()
    {
        RenderRequest? request = CreateRenderRequest (false);

        if (request is null)
        {
            return;
        }

        EnsureScaledImage (request);

        if (_scaledImage is null || _scaledImageCellSize is null)
        {
            return;
        }

        int drawWidth = Math.Min (Viewport.Width, _scaledImage.GetLength (0));
        int drawHeight = Math.Min (Viewport.Height, _scaledImage.GetLength (1));

        for (var y = 0; y < drawHeight; y++)
        {
            for (var x = 0; x < drawWidth; x++)
            {
                Color pixel = _scaledImage [x, y];

                if (!_attributeCache.TryGetValue (pixel, out Attribute attr))
                {
                    attr = new Attribute (new Color (), pixel);
                    _attributeCache.Add (pixel, attr);
                }

                SetAttribute (attr);
                AddRune (x, y, (Rune)' ');
            }
        }
    }

    /// <summary>
    ///     Renders the image using sixel escape sequences.
    /// </summary>
    private void DrawSixel ()
    {
        RenderRequest? request = CreateRenderRequest (true);

        if (request is null)
        {
            return;
        }

        EnsureScaledImage (request);

        if (_scaledImage is null || _scaledImageCellSize is null || SixelEncoder is null || App?.Driver is not { } driver)
        {
            return;
        }

        Rectangle viewport = ViewportToScreen ();
        Size cellSize = _scaledImageCellSize.Value;
        Rectangle destinationCells = viewport with { Width = Math.Min (viewport.Width, cellSize.Width), Height = Math.Min (viewport.Height, cellSize.Height) };

        if (destinationCells.Width <= 0 || destinationCells.Height <= 0)
        {
            return;
        }

        RasterImageCommand command = new ()
        {
            Id = RasterImageId,
            Pixels = _scaledImage,
            DestinationCells = destinationCells,
            Encoder = SixelEncoder,
            IsDirty = true
        };

        driver.GetOutputBuffer ().AddRasterImage (command);
    }

    private bool? CenterFromCommand (ICommandContext? context) => context?.Binding is MouseBinding { MouseEvent.Position: { } position } && CenterOnViewportPoint (position);

    /// <summary>
    ///     Centers the image on the specified viewport-relative point.
    /// </summary>
    /// <param name="position">The viewport-relative point.</param>
    /// <returns><see langword="true"/> if the view was centered; otherwise, <see langword="false"/>.</returns>
    protected virtual bool CenterOnViewportPoint (Point position)
    {
        if (!TryMapViewportPointToSourceCenter (position, out double centerX, out double centerY))
        {
            return false;
        }

        return SetCenter (centerX, centerY);
    }

    private void ClampCenter ()
    {
        if (_image is null || _zoomLevel <= MIN_ZOOM_LEVEL)
        {
            _centerX = 0.5d;
            _centerY = 0.5d;

            return;
        }

        double halfVisible = 0.5d / _zoomLevel;
        _centerX = Math.Clamp (_centerX, halfVisible, 1d - halfVisible);
        _centerY = Math.Clamp (_centerY, halfVisible, 1d - halfVisible);
    }

    private RectangleF GetVisibleSourceRectangle ()
    {
        if (_image is null)
        {
            return RectangleF.Empty;
        }

        int srcWidth = _image.GetLength (0);
        int srcHeight = _image.GetLength (1);
        double visibleWidth = srcWidth / _zoomLevel;
        double visibleHeight = srcHeight / _zoomLevel;
        double x = _centerX * srcWidth - visibleWidth / 2d;
        double y = _centerY * srcHeight - visibleHeight / 2d;
        x = Math.Clamp (x, 0d, Math.Max (0d, srcWidth - visibleWidth));
        y = Math.Clamp (y, 0d, Math.Max (0d, srcHeight - visibleHeight));

        return new RectangleF ((float)x, (float)y, (float)visibleWidth, (float)visibleHeight);
    }

    private static Size GetSixelCellSize (Size pixelSize, Size resolution)
    {
        int cellWidth = Math.Max (1, (int)Math.Ceiling ((double)pixelSize.Width / Math.Max (1, resolution.Width)));
        int cellHeight = Math.Max (1, (int)Math.Ceiling ((double)pixelSize.Height / Math.Max (1, resolution.Height)));

        return new Size (cellWidth, cellHeight);
    }

    private void InvalidateScaledImage (bool clearCurrentRender = false)
    {
        if (clearCurrentRender || !UseBackgroundRendering || _scaledImage is null)
        {
            _scaledImage = null;
            _scaledImageCellSize = null;
            _attributeCache.Clear ();
        }

        _scaledImageRenderKey = null;
        SetNeedsDraw ();
    }

    private bool IsRenderCacheCurrent (RenderKey key) => _scaledImage is { } && _scaledImageCellSize is { } && _scaledImageRenderKey == key;

    private void EnsureScaledImage (RenderRequest request)
    {
        if (IsRenderCacheCurrent (request.Key))
        {
            SetRenderingOverlayVisible (false);

            return;
        }

        if (UseBackgroundRendering && App?.Initialized == true)
        {
            ScheduleBackgroundRender (request);

            return;
        }

        ApplyRenderResult (RenderScaledImage (request));
        SetRenderingOverlayVisible (false);
    }

    private RenderRequest? CreateRenderRequest (bool useSixel)
    {
        if (_image is null)
        {
            return null;
        }

        RectangleF visibleSource = GetVisibleSourceRectangle ();

        if (visibleSource.Width <= 0 || visibleSource.Height <= 0)
        {
            return null;
        }

        Size? resolution = null;
        int? maxColors = null;
        Size targetSize;
        var allowUpscale = true;
        var preserveAspectRatio = false;

        if (useSixel)
        {
            if (!IsUsingSixel || App?.Driver?.SixelSupport is not { } support)
            {
                return null;
            }

            SixelEncoder encoder = PrepareSixelEncoder (support);
            Rectangle targetRect = ViewportToScreenInPixels ();
            targetSize = targetRect.Size;
            resolution = support.Resolution;
            maxColors = encoder.Quantizer.MaxColors;
            allowUpscale = AllowSixelUpscaling || _zoomLevel > MIN_ZOOM_LEVEL;
            preserveAspectRatio = true;
        }
        else
        {
            targetSize = new Size (Viewport.Width, Viewport.Height);
        }

        if (targetSize.Width <= 0 || targetSize.Height <= 0)
        {
            return null;
        }

        RenderKey key = new (_imageVersion,
                             useSixel,
                             targetSize,
                             resolution,
                             maxColors,
                             _centerX,
                             _centerY,
                             _zoomLevel,
                             allowUpscale,
                             preserveAspectRatio);

        return new RenderRequest (key, _image, visibleSource, targetSize, resolution);
    }

    private SixelEncoder PrepareSixelEncoder (SixelSupportResult support)
    {
        SixelEncoder? encoder = SixelEncoder;

        if (encoder is null)
        {
            encoder = new SixelEncoder ();
            _sixelEncoder = encoder;
            _usesDefaultSixelEncoder = true;
        }

        int maxColors = _usesDefaultSixelEncoder
                            ? Math.Min (MaxSixelPaletteColors, support.MaxPaletteColors)
                            : Math.Min (encoder.Quantizer.MaxColors, support.MaxPaletteColors);
        encoder.Quantizer.MaxColors = maxColors;

        return encoder;
    }

    private void ApplyRenderResult (RenderResult result)
    {
        _scaledImage = result.ScaledImage;
        _scaledImageCellSize = result.CellSize;
        _scaledImageRenderKey = result.Key;
        _attributeCache.Clear ();
    }

    private static RenderResult RenderScaledImage (RenderRequest request)
    {
        Color [,] scaledImage = ScaleVisibleImage (request.Source,
                                                   request.VisibleSource,
                                                   request.TargetSize,
                                                   request.Key.AllowUpscale,
                                                   request.Key.PreserveAspectRatio);

        Size cellSize = request.Key.UseSixel && request.Resolution is { } resolution
                            ? GetSixelCellSize (new Size (scaledImage.GetLength (0), scaledImage.GetLength (1)), resolution)
                            : new Size (scaledImage.GetLength (0), scaledImage.GetLength (1));

        return new RenderResult (request.Key, scaledImage, cellSize);
    }

    private void ScheduleBackgroundRender (RenderRequest request)
    {
        if (_disposed)
        {
            return;
        }

        SetRenderingOverlayVisible (true);

        RenderRequest? requestToStart;

        lock (_renderLock)
        {
            if (_backgroundRenderRunning)
            {
                if (_backgroundRenderKey == request.Key || _queuedRenderRequest?.Key == request.Key)
                {
                    return;
                }

                _queuedRenderRequest = request;

                return;
            }

            _backgroundRenderRunning = true;
            requestToStart = request;
        }

        StartBackgroundRender (requestToStart);
    }

    private void StartBackgroundRender (RenderRequest request)
    {
        lock (_renderLock)
        {
            _backgroundRenderKey = request.Key;
        }

        Task<RenderResult> task = Task.Run (() => RenderScaledImage (request));
        task.ContinueWith (CompleteBackgroundRender, TaskScheduler.Default);
    }

    private void CompleteBackgroundRender (Task<RenderResult> completed)
    {
        AggregateException? exception = completed.Exception;

        if (_disposed)
        {
            _ = exception;

            return;
        }

        IApplication? app = App;

        if (app is null || !app.Initialized)
        {
            _ = exception;

            return;
        }

        try
        {
            if (exception is { })
            {
                app.Invoke (() => FailBackgroundRender (exception.GetBaseException ()));

                return;
            }

            app.Invoke (() => CompleteBackgroundRenderOnMainThread (completed.Result));
        }
        catch (NotInitializedException)
        {
            _ = exception;
        }
        catch (ObjectDisposedException)
        {
            _ = exception;
        }
    }

    private void CompleteBackgroundRenderOnMainThread (RenderResult result)
    {
        if (_disposed)
        {
            return;
        }

        RenderRequest? currentRequest = CreateRenderRequest (result.Key.UseSixel);

        if (UseBackgroundRendering && currentRequest?.Key == result.Key)
        {
            ApplyRenderResult (result);
        }

        StartNextQueuedRenderOrFinish (result.Key);
        SetNeedsDraw ();
    }

    private void FailBackgroundRender (Exception exception)
    {
        StartNextQueuedRenderOrFinish (_backgroundRenderKey);

        throw new InvalidOperationException ("Background ImageView rendering failed.", exception);
    }

    private void StartNextQueuedRenderOrFinish (RenderKey? completedKey)
    {
        RenderRequest? nextRequest = null;

        lock (_renderLock)
        {
            if (_queuedRenderRequest is { } queuedRequest && queuedRequest.Key != completedKey)
            {
                nextRequest = queuedRequest;
                _queuedRenderRequest = null;
            }
            else
            {
                _queuedRenderRequest = null;
                _backgroundRenderKey = null;
                _backgroundRenderRunning = false;
            }
        }

        if (nextRequest is { })
        {
            StartBackgroundRender (nextRequest);

            return;
        }

        SetRenderingOverlayVisible (false);
    }

    private void SetRenderingOverlayVisible (bool visible)
    {
        if (_renderingOverlay.Visible == visible)
        {
            return;
        }

        _renderingOverlay.Visible = visible;
    }

    private bool PanByCells (int deltaX, int deltaY)
    {
        if (_image is null || _zoomLevel <= MIN_ZOOM_LEVEL || Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return false;
        }

        RectangleF visibleSource = GetVisibleSourceRectangle ();
        double centerX = _centerX + deltaX * visibleSource.Width / Viewport.Width / _image.GetLength (0);
        double centerY = _centerY + deltaY * visibleSource.Height / Viewport.Height / _image.GetLength (1);

        return SetCenter (centerX, centerY);
    }

    private bool ScrollFromCommand (int deltaX, int deltaY)
    {
        PanByCells (deltaX, deltaY);

        return true;
    }

    private bool ResetView ()
    {
        if (_zoomLevel == MIN_ZOOM_LEVEL && Math.Abs (_centerX - 0.5d) < double.Epsilon && Math.Abs (_centerY - 0.5d) < double.Epsilon)
        {
            return false;
        }

        _zoomLevel = MIN_ZOOM_LEVEL;
        _centerX = 0.5d;
        _centerY = 0.5d;
        InvalidateScaledImage ();

        return true;
    }

    private bool SetCenter (double centerX, double centerY)
    {
        double previousCenterX = _centerX;
        double previousCenterY = _centerY;
        _centerX = centerX;
        _centerY = centerY;
        ClampCenter ();

        if (Math.Abs (previousCenterX - _centerX) < double.Epsilon && Math.Abs (previousCenterY - _centerY) < double.Epsilon)
        {
            return false;
        }

        InvalidateScaledImage ();

        return true;
    }

    private bool SetZoomLevel (double zoomLevel, Point? anchor)
    {
        if (double.IsNaN (zoomLevel) || double.IsInfinity (zoomLevel))
        {
            throw new ArgumentOutOfRangeException (nameof (zoomLevel), @"Zoom level must be a finite number.");
        }

        double previousZoomLevel = _zoomLevel;
        double clampedZoomLevel = Math.Clamp (zoomLevel, MIN_ZOOM_LEVEL, MAX_ZOOM_LEVEL);

        if (Math.Abs (previousZoomLevel - clampedZoomLevel) < double.Epsilon)
        {
            return false;
        }

        if (anchor is { } position && TryMapViewportPointToSourceCenter (position, out double sourceX, out double sourceY))
        {
            _zoomLevel = clampedZoomLevel;
            SetCenterForAnchor (position, sourceX, sourceY);
        }
        else
        {
            _zoomLevel = clampedZoomLevel;
            ClampCenter ();
        }

        InvalidateScaledImage ();

        return true;
    }

    private void SetCenterForAnchor (Point position, double sourceX, double sourceY)
    {
        if (_image is null || Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return;
        }

        double visibleWidth = 1d / _zoomLevel;
        double visibleHeight = 1d / _zoomLevel;
        _centerX = sourceX - ((position.X + 0.5d) / Viewport.Width - 0.5d) * visibleWidth;
        _centerY = sourceY - ((position.Y + 0.5d) / Viewport.Height - 0.5d) * visibleHeight;
        ClampCenter ();
    }

    private static Color [,] ScaleVisibleImage (Color [,] source, RectangleF visibleSource, Size targetSize, bool allowUpscale, bool preserveAspectRatio)
    {
        int newWidth;
        int newHeight;

        if (preserveAspectRatio)
        {
            double widthScale = targetSize.Width / visibleSource.Width;
            double heightScale = targetSize.Height / visibleSource.Height;
            double scale = Math.Min (widthScale, heightScale);

            if (!allowUpscale)
            {
                scale = Math.Min (scale, 1d);
            }

            newWidth = Math.Max (1, (int)(visibleSource.Width * scale));
            newHeight = Math.Max (1, (int)(visibleSource.Height * scale));
        }
        else
        {
            newWidth = targetSize.Width;
            newHeight = targetSize.Height;
        }

        Color [,] scaledImage = new Color [newWidth, newHeight];
        ScaleVisibleNearestNeighbor (source, scaledImage, visibleSource);

        return scaledImage;
    }

    private static void ScaleVisibleNearestNeighbor (Color [,] source, Color [,] destination, RectangleF visibleSource)
    {
        int srcWidth = source.GetLength (0);
        int srcHeight = source.GetLength (1);
        int newWidth = destination.GetLength (0);
        int newHeight = destination.GetLength (1);

        for (var y = 0; y < newHeight; y++)
        {
            int srcY = Math.Clamp ((int)(visibleSource.Y + y * visibleSource.Height / newHeight), 0, srcHeight - 1);

            for (var x = 0; x < newWidth; x++)
            {
                int srcX = Math.Clamp ((int)(visibleSource.X + x * visibleSource.Width / newWidth), 0, srcWidth - 1);
                destination [x, y] = source [srcX, srcY];
            }
        }
    }

    private bool TryMapViewportPointToSourceCenter (Point position, out double centerX, out double centerY)
    {
        centerX = 0.5d;
        centerY = 0.5d;

        if (_image is null || Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return false;
        }

        if (position.X < 0 || position.Y < 0 || position.X >= Viewport.Width || position.Y >= Viewport.Height)
        {
            return false;
        }

        RectangleF visibleSource = GetVisibleSourceRectangle ();
        centerX = (visibleSource.X + (position.X + 0.5d) * visibleSource.Width / Viewport.Width) / _image.GetLength (0);
        centerY = (visibleSource.Y + (position.Y + 0.5d) * visibleSource.Height / Viewport.Height) / _image.GetLength (1);

        return true;
    }

    private bool ZoomFromCommand (ICommandContext? context, double zoomLevel)
    {
        Point? anchor = context?.Binding is MouseBinding { MouseEvent.Position: { } position } ? position : null;

        return SetZoomLevel (zoomLevel, anchor);
    }

    /// <summary>
    ///     Scales a <c>Color[,]</c> pixel array into a destination array using nearest-neighbor interpolation.
    /// </summary>
    /// <param name="source">The source pixel array indexed as [x, y].</param>
    /// <param name="destination">The destination pixel array indexed as [x, y].</param>
    public static void ScaleNearestNeighbor (Color [,] source, Color [,] destination)
    {
        int srcWidth = source.GetLength (0);
        int srcHeight = source.GetLength (1);
        int newWidth = destination.GetLength (0);
        int newHeight = destination.GetLength (1);

        for (var y = 0; y < newHeight; y++)
        {
            int srcY = Math.Min (y * srcHeight / newHeight, srcHeight - 1);

            for (var x = 0; x < newWidth; x++)
            {
                int srcX = Math.Min (x * srcWidth / newWidth, srcWidth - 1);
                destination [x, y] = source [srcX, srcY];
            }
        }
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (HandleDrag (mouse))
        {
            return true;
        }

        return base.OnMouseEvent (mouse);
    }

    private bool HandleDrag (Mouse mouse)
    {
        if (mouse.Position is not { } position)
        {
            return false;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed) && !mouse.Flags.FastHasFlags (MouseFlags.PositionReport))
        {
            _lastDragPosition = position;
            App?.Mouse.GrabMouse (this);

            return true;
        }

        if (mouse.Flags == (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport) && _lastDragPosition is { } lastDragPosition)
        {
            bool panned = PanByCells (lastDragPosition.X - position.X, lastDragPosition.Y - position.Y);
            _lastDragPosition = position;

            return panned;
        }

        if (!mouse.Flags.FastHasFlags (MouseFlags.LeftButtonReleased))
        {
            return false;
        }
        _lastDragPosition = null;

        if (App is { } && App.Mouse.IsGrabbed (this))
        {
            App.Mouse.UngrabMouse ();
        }

        return true;

    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _disposed = true;

            lock (_renderLock)
            {
                _queuedRenderRequest = null;
                _backgroundRenderKey = null;
                _backgroundRenderRunning = false;
            }

            App?.Driver?.GetOutputBuffer ().RemoveRasterImage (RasterImageId);
        }

        base.Dispose (disposing);
    }

    private readonly record struct RenderKey (int ImageVersion,
                                              bool UseSixel,
                                              Size TargetSize,
                                              Size? Resolution,
                                              int? MaxColors,
                                              double CenterX,
                                              double CenterY,
                                              double ZoomLevel,
                                              bool AllowUpscale,
                                              bool PreserveAspectRatio);

    private sealed class RenderRequest (RenderKey key, Color [,] source, RectangleF visibleSource, Size targetSize, Size? resolution)
    {
        public RenderKey Key { get; } = key;
        public Color [,] Source { get; } = source;
        public RectangleF VisibleSource { get; } = visibleSource;
        public Size TargetSize { get; } = targetSize;
        public Size? Resolution { get; } = resolution;
    }

    private readonly record struct RenderResult (RenderKey Key, Color [,] ScaledImage, Size CellSize);

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        // Create a simple gradient test image for the designer
        var width = 20;
        var height = 10;
        Color [,] testImage = new Color [width, height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var r = (byte)(x * 255 / Math.Max (1, width - 1));
                var g = (byte)(y * 255 / Math.Max (1, height - 1));
                var b = (byte)128;
                testImage [x, y] = new Color (r, g, b);
            }
        }

        Image = testImage;

        return true;
    }
}
