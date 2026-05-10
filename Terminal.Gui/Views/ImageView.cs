using System.Collections.Generic;

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
///         sixel escape sequences and render it through the driver's sixel pipeline. Sixel data
///         is only re-encoded and re-sent to the terminal when <see cref="View.NeedsDraw"/> is true,
///         avoiding redundant rendering of unchanged images.
///     </para>
///     <para>
///         When sixel is not available, the view falls back to cell-based rendering where each
///         terminal cell is colored with the background color of the corresponding pixel.
///     </para>
/// </remarks>
public class ImageView : View, IDesignable
{
    private Color [,]? _image;
    private Color [,]? _scaledImage;
    private SixelToRender? _sixelToRender;
    private string? _cachedSixelData;

    // Cell-based rendering cache
    private readonly Dictionary<Color, Attribute> _attributeCache = new ();

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
            _scaledImage = null;
            _cachedSixelData = null;
            UpdateSixelData ();
            SetNeedsDraw ();
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
    ///     Gets or sets the <see cref="Drawing.SixelEncoder"/> used to encode images as sixel data.
    ///     When <see langword="null"/>, a default encoder is created lazily on first use.
    /// </summary>
    /// <remarks>
    ///     Set this to provide a custom encoder with specific quantizer settings, palette building
    ///     algorithms, or color distance algorithms. The encoder's
    ///     <see cref="Drawing.SixelEncoder.Quantizer"/> <c>MaxColors</c> will be clamped to the
    ///     terminal's <see cref="SixelSupportResult.MaxPaletteColors"/> during rendering.
    /// </remarks>
    public SixelEncoder? SixelEncoder { get; set; }

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
        SixelSupportResult? support = App?.Driver?.SixelSupport;

        if (support is null)
        {
            throw new InvalidOperationException (@"No sixel support available.");
        }

        int pixelsPerCellX = support.Resolution.Width;
        int pixelsPerCellY = support.Resolution.Height;
        Rectangle boundsRect = ViewportToScreen ();

        // Calculate target size in pixels based on viewport and cell resolution
        int targetWidthInPixels = boundsRect.Width * pixelsPerCellX;
        int targetHeightInPixels = SixelEncoder.GetHeightInPixels (boundsRect.Height, pixelsPerCellY);

        return new Rectangle (boundsRect.X * pixelsPerCellX, boundsRect.Y * pixelsPerCellY, targetWidthInPixels, targetHeightInPixels);
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
        }
        else
        {
            DrawCellBased ();
        }

        context?.AddDrawnRectangle (ViewportToScreen ());

        return true;
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (frame);
        UpdateSixelData ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Renders the image using cell-based rendering where each terminal cell
    ///     gets the background color of the corresponding pixel.
    /// </summary>
    private void DrawCellBased ()
    {
        Color [,]? scaled = GetScaledImage (Viewport.Width, Viewport.Height);

        if (scaled is null)
        {
            return;
        }

        int drawWidth = Math.Min (Viewport.Width, scaled.GetLength (0));
        int drawHeight = Math.Min (Viewport.Height, scaled.GetLength (1));

        for (int y = 0; y < drawHeight; y++)
        {
            for (int x = 0; x < drawWidth; x++)
            {
                Color pixel = scaled [x, y];

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
        SixelSupportResult? support = App?.Driver?.SixelSupport;

        if (support is null)
        {
            return;
        }

        // Get screen position for this view's viewport
        Point screenPos = ViewportToScreen ().Location;

        if (_sixelToRender is null)
        {
            _sixelToRender = new SixelToRender
            {
                SixelData = _cachedSixelData,
                ScreenPosition = screenPos,
                Id = $"ImageView_{GetHashCode ()}",
                IsDirty = true
            };

            App?.Driver?.GetOutput ().GetSixels ().Enqueue (_sixelToRender);
        }
        else
        {
            _sixelToRender.SixelData = _cachedSixelData;
            _sixelToRender.ScreenPosition = screenPos;
            _sixelToRender.IsDirty = true;
        }
    }

    private void UpdateSixelData ()
    {
        SixelSupportResult? support = App?.Driver?.SixelSupport;

        if (support is null)
        {
            return;
        }

        // Use caller-provided encoder or create a default one
        if (SixelEncoder is null)
        {
            SixelEncoder = new SixelEncoder ();
            SixelEncoder.Quantizer.MaxColors = Math.Min (SixelEncoder.Quantizer.MaxColors, support.MaxPaletteColors);
        }

        Rectangle targetRect = ViewportToScreenInPixels ();

        // Scale the image to the target pixel size while maintaining aspect ratio
        Color [,]? scaled = GetScaledImage (targetRect.Width, targetRect.Height);

        if (scaled is null)
        {
            return;
        }

        // Encode sixel data
        _cachedSixelData = SixelEncoder.EncodeSixel (scaled);
    }

    /// <summary>
    ///     Scales the source image to the specified target dimensions using nearest-neighbor
    ///     interpolation while maintaining aspect ratio.
    /// </summary>
    /// <param name="targetWidth">The target width in the appropriate unit (cells or pixels).</param>
    /// <param name="targetHeight">The target height in the appropriate unit (cells or pixels).</param>
    /// <returns>The scaled image, or <see langword="null"/> if the source image is null or the target size is invalid.</returns>
    private Color [,]? GetScaledImage (int targetWidth, int targetHeight)
    {
        if (_image is null || targetWidth <= 0 || targetHeight <= 0)
        {
            return null;
        }

        int srcWidth = _image.GetLength (0);
        int srcHeight = _image.GetLength (1);

        if (srcWidth == 0 || srcHeight == 0)
        {
            return null;
        }

        // Calculate aspect-ratio-preserving size
        double widthScale = (double)targetWidth / srcWidth;
        double heightScale = (double)targetHeight / srcHeight;
        double scale = Math.Min (widthScale, heightScale);

        int newWidth = Math.Max (1, (int)(srcWidth * scale));
        int newHeight = Math.Max (1, (int)(srcHeight * scale));

        // We can start with the input image, maybe it's the correct size already
        if (_scaledImage is null)
        {
            _scaledImage = _image;
        }

        // Nearest-neighbor scale
        if (_scaledImage is null || _scaledImage.GetLength (0) != newWidth || _scaledImage.GetLength (1) != newHeight)
        {
            _scaledImage = new Color [newWidth, newHeight];
            ScaleNearestNeighbor (_image, _scaledImage);
        }

        return _scaledImage;
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

        for (int y = 0; y < newHeight; y++)
        {
            int srcY = Math.Min (y * srcHeight / newHeight, srcHeight - 1);

            for (int x = 0; x < newWidth; x++)
            {
                int srcX = Math.Min (x * srcWidth / newWidth, srcWidth - 1);
                destination [x, y] = source [srcX, srcY];
            }
        }
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing && _sixelToRender is { })
        {
            // Clear the sixel data so it's not rendered anymore.
            // The ConcurrentQueue doesn't support removal, but clearing the data
            // ensures OutputBase.Write skips it.
            _sixelToRender.SixelData = null;
            _sixelToRender.IsDirty = false;
        }

        base.Dispose (disposing);
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        // Create a simple gradient test image for the designer
        int width = 20;
        int height = 10;
        Color [,] testImage = new Color [width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = (byte)(x * 255 / Math.Max (1, width - 1));
                byte g = (byte)(y * 255 / Math.Max (1, height - 1));
                byte b = (byte)(128);
                testImage [x, y] = new Color (r, g, b);
            }
        }

        Image = testImage;

        return true;
    }
}