namespace Terminal.Gui.Views;

public partial class ImageView
{
    private static Color [,] ScaleVisibleImage (Color [,] source, RectangleF visibleSource, Size targetSize, bool allowUpscale, bool preserveAspectRatio)
    {
        int newWidth;
        int newHeight;

        if (preserveAspectRatio)
        {
            double widthScale = targetSize.Width / (double)visibleSource.Width;
            double heightScale = targetSize.Height / (double)visibleSource.Height;
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
            int srcY = Math.Clamp ((int)(visibleSource.Y + y * (double)visibleSource.Height / newHeight), 0, srcHeight - 1);

            for (var x = 0; x < newWidth; x++)
            {
                int srcX = Math.Clamp ((int)(visibleSource.X + x * (double)visibleSource.Width / newWidth), 0, srcWidth - 1);
                destination [x, y] = source [srcX, srcY];
            }
        }
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
}
