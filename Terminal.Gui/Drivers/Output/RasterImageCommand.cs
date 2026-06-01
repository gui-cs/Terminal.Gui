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
}
