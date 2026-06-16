namespace Terminal.Gui.Drawing;

/// <summary>
///     Encodes a pixel array into the Kitty terminal graphics protocol APC escape sequence stream.
/// </summary>
/// <remarks>
///     <para>
///         The Kitty graphics protocol transmits images via APC sequences of the form:
///         <c>ESC_G &lt;key=value,...&gt;;&lt;base64-data&gt;ESC\</c>.
///     </para>
///     <para>
///         Images are encoded as raw 32-bit RGBA pixel data (no palette, no quantization), split
///         into chunks of at most <see cref="MaxChunkSize"/> base64 characters. Multi-chunk
///         transmissions set <c>m=1</c> on all chunks except the last (<c>m=0</c>).
///     </para>
///     <para>
///         See <see href="https://sw.kovidgoyal.net/kitty/graphics-protocol/"/> for the full spec.
///     </para>
/// </remarks>
public class KittyGraphicsEncoder
{
    /// <summary>
    ///     Maximum number of base64-encoded characters per APC chunk.
    ///     The Kitty protocol recommends ≤4096 bytes of payload per chunk.
    /// </summary>
    public const int MaxChunkSize = 4096;

    // APC = ESC _ ... ESC \
    private const string APC_START = "\x1b_G";
    private const string APC_END = "\x1b\\";

    /// <summary>
    ///     Encodes the provided pixel array into a Kitty graphics protocol APC sequence string
    ///     that, when written to the terminal, renders the image at the current cursor position
    ///     occupying <paramref name="destCols"/> columns and <paramref name="destRows"/> rows.
    /// </summary>
    /// <param name="pixels">
    ///     The pixel array to encode, indexed as <c>[x, y]</c> where the first dimension is width
    ///     and the second is height.
    /// </param>
    /// <param name="destCols">The number of terminal columns the image should occupy.</param>
    /// <param name="destRows">The number of terminal rows the image should occupy.</param>
    /// <param name="imageId">
    ///     An optional stable image identifier (the Kitty <c>i</c> key). When supplied, placements can be
    ///     deleted and replaced by id via <see cref="EncodeDeletePlacements"/>, which is required to erase
    ///     a previous placement when the image is resized or moved. Use <see cref="GetImageId"/> to derive
    ///     a stable id from a string. When <see langword="null"/> the terminal assigns its own id and the
    ///     placement cannot be targeted for deletion.
    /// </param>
    /// <returns>The complete Kitty APC escape sequence string.</returns>
    public string EncodeKitty (Color [,] pixels, int destCols, int destRows, int? imageId = null)
    {
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);

        byte [] rgba = PixelsToRgba (pixels, width, height);
        string base64 = Convert.ToBase64String (rgba);

        return BuildApcSequence (base64, width, height, destCols, destRows, imageId);
    }

    /// <summary>
    ///     Builds a Kitty graphics protocol APC sequence that deletes all placements of the image with the
    ///     specified <paramref name="imageId"/>, leaving the transmitted image data intact so it can be
    ///     re-displayed.
    /// </summary>
    /// <remarks>
    ///     Kitty image placements persist on screen until explicitly deleted — unlike Sixel, drawing text
    ///     over their cells does not erase them. Emit this before re-placing a resized or moved image, and
    ///     when an image is removed, so a stale placement is not left behind.
    /// </remarks>
    /// <param name="imageId">The image id (the Kitty <c>i</c> key) whose placements should be deleted.</param>
    /// <returns>The complete Kitty APC delete escape sequence string.</returns>
    public static string EncodeDeletePlacements (int imageId) => $"{APC_START}a=d,d=i,i={imageId}{APC_END}";

    /// <summary>
    ///     Derives a stable, positive, non-zero Kitty image id from the given string identifier.
    /// </summary>
    /// <remarks>
    ///     The same <paramref name="id"/> always maps to the same image id, so a view's placement can be
    ///     consistently replaced or deleted across renders.
    /// </remarks>
    /// <param name="id">The string identifier (e.g. a <see cref="Terminal.Gui.Drivers.RasterImageCommand.Id"/>).</param>
    /// <returns>A positive, non-zero image id suitable for the Kitty <c>i</c> key.</returns>
    public static int GetImageId (string id)
    {
        ArgumentException.ThrowIfNullOrEmpty (id);

        // FNV-1a 32-bit hash, clamped to a positive, non-zero value.
        var hash = 2166136261u;

        foreach (char c in id)
        {
            hash = (hash ^ c) * 16777619u;
        }

        var result = (int)(hash & 0x7FFFFFFF);

        return result == 0 ? 1 : result;
    }

    private static byte [] PixelsToRgba (Color [,] pixels, int width, int height)
    {
        byte [] rgba = new byte [width * height * 4];
        int i = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Color c = pixels [x, y];
                rgba [i++] = c.R;
                rgba [i++] = c.G;
                rgba [i++] = c.B;
                rgba [i++] = c.A;
            }
        }

        return rgba;
    }

    private static string BuildApcSequence (string base64, int pixelWidth, int pixelHeight, int destCols, int destRows, int? imageId)
    {
        var sb = new StringBuilder ();
        int totalLength = base64.Length;
        int offset = 0;

        // First chunk carries the image metadata.
        string firstChunk = offset + MaxChunkSize < totalLength
                                ? base64.Substring (offset, MaxChunkSize)
                                : base64.Substring (offset);
        bool isLastChunk = offset + firstChunk.Length >= totalLength;

        // i=<id> tags the placement with a stable image id so a prior placement can be deleted by
        // id (see EncodeDeletePlacements) when the image is resized or moved.
        string idField = imageId.HasValue ? $"i={imageId.Value}," : string.Empty;

        // C=1 suppresses cursor movement after the image is displayed. Without it the terminal
        // advances the cursor past the bottom-right of the image, and an image near the bottom of
        // the screen scrolls the whole display up one row on every repaint (animated images march
        // up the screen instead of repainting in place).
        sb.Append (APC_START);
        sb.Append ($"a=T,f=32,{idField}s={pixelWidth},v={pixelHeight},c={destCols},r={destRows},z=-1,C=1,q=2,m={( isLastChunk ? 0 : 1 )}");
        sb.Append (';');
        sb.Append (firstChunk);
        sb.Append (APC_END);
        offset += firstChunk.Length;

        // Continuation chunks carry only data.
        while (offset < totalLength)
        {
            string chunk = offset + MaxChunkSize < totalLength
                               ? base64.Substring (offset, MaxChunkSize)
                               : base64.Substring (offset);
            bool last = offset + chunk.Length >= totalLength;

            sb.Append (APC_START);
            sb.Append ($"m={( last ? 0 : 1 )}");
            sb.Append (';');
            sb.Append (chunk);
            sb.Append (APC_END);
            offset += chunk.Length;
        }

        return sb.ToString ();
    }
}
