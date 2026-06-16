namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes a raster image to compose through the output buffer.
/// </summary>
public class RasterImageCommand
{
    /// <summary>
    ///     Gets or sets the stable identifier for this raster image.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Gets or sets the image pixels to encode.
    /// </summary>
    public Color [,]? Pixels { get; set; }

    /// <summary>
    ///     Gets or sets pre-encoded sixel data for <see cref="Pixels"/>.
    /// </summary>
    /// <remarks>
    ///     This is used only when the full <see cref="DestinationCells"/> rectangle is visible. Clipped output still
    ///     encodes the cropped pixels so the emitted sixel dimensions match the clipped cell region.
    /// </remarks>
    public string? EncodedSixel { get; set; }

    /// <summary>
    ///     Gets or sets pre-encoded Kitty graphics protocol data for <see cref="Pixels"/>.
    /// </summary>
    /// <remarks>
    ///     This is used only when the full <see cref="DestinationCells"/> rectangle is visible. Clipped output still
    ///     re-encodes the cropped pixels so the emitted dimensions match the clipped cell region.
    /// </remarks>
    public string? EncodedKitty { get; set; }

    /// <summary>
    ///     Gets or sets the screen cells occupied by <see cref="Pixels"/>.
    /// </summary>
    public Rectangle DestinationCells { get; set; }

    /// <summary>
    ///     Gets or sets the clip region captured when the image is added to the output buffer.
    /// </summary>
    public Region? Clip { get; set; }

    /// <summary>
    ///     Gets or sets the sixel encoder to use. A default encoder is used when this is <see langword="null"/>.
    /// </summary>
    public SixelEncoder? Encoder { get; set; }

    /// <summary>
    ///     Gets or sets whether the image needs to be emitted during the next driver write.
    /// </summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the image should be emitted on every driver write.
    /// </summary>
    public bool AlwaysRender { get; set; }

    /// <summary>
    ///     Gets or sets whether the image should be emitted after dirty text cells rather than before them.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="false"/> so text drawn later can appear above a raster image. Set this to
    ///     <see langword="true"/> for animated Sixel overlays that need to repaint after the normal cell pass to
    ///     avoid visible flicker.
    /// </remarks>
    public bool RenderAfterText { get; set; }
}
