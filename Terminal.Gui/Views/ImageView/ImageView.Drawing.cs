namespace Terminal.Gui.Views;

public partial class ImageView
{
    private readonly Dictionary<Color, Attribute> _attributeCache = new ();

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
        Point offset = _zoomLevel < FIT_ZOOM_LEVEL ? GetCenteredRenderOffset (new Size (drawWidth, drawHeight), Viewport.Size) : Point.Empty;

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
                AddRune (x + offset.X, y + offset.Y, (Rune)' ');
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
        Size destinationSize = new (Math.Min (viewport.Width, cellSize.Width), Math.Min (viewport.Height, cellSize.Height));
        Point offset = _zoomLevel < FIT_ZOOM_LEVEL ? GetCenteredRenderOffset (destinationSize, viewport.Size) : Point.Empty;
        Rectangle destinationCells = new (viewport.X + offset.X, viewport.Y + offset.Y, destinationSize.Width, destinationSize.Height);

        if (destinationCells.Width <= 0 || destinationCells.Height <= 0)
        {
            return;
        }

        RasterImageCommand command = new ()
        {
            Id = RasterImageId,
            Pixels = _scaledImage,
            EncodedSixel = _encodedSixel,
            DestinationCells = destinationCells,
            Encoder = SixelEncoder,
            IsDirty = true
        };

        driver.GetOutputBuffer ().AddRasterImage (command);
    }

    private bool? CenterFromCommand (ICommandContext? context) =>
        context?.Binding is MouseBinding { MouseEvent.Position: { } position } && CenterOnViewportPoint (position);
}
