#nullable enable

namespace Terminal.Gui.Text;

/// <summary>
///     Interface for rendering formatted text to the console.
/// </summary>
public interface ITextRenderer
{
    /// <summary>
    ///     Draws the formatted text to the console driver.
    /// </summary>
    /// <param name="formattedText">The formatted text to draw.</param>
    /// <param name="screen">The screen bounds for drawing.</param>
    /// <param name="normalColor">The color for normal text.</param>
    /// <param name="hotColor">The color for HotKey text.</param>
    /// <param name="fillRemaining">Whether to fill remaining area with spaces.</param>
    /// <param name="maximum">The maximum container bounds.</param>
    /// <param name="driver">The console driver to use for drawing.</param>
    void Draw(
        FormattedText formattedText,
        Rectangle screen,
        Attribute normalColor,
        Attribute hotColor,
        bool fillRemaining = false,
        Rectangle maximum = default,
        IConsoleDriver? driver = null);

    /// <summary>
    ///     Gets the region that would be drawn by the formatted text.
    /// </summary>
    /// <param name="formattedText">The formatted text.</param>
    /// <param name="screen">The screen bounds.</param>
    /// <param name="maximum">The maximum container bounds.</param>
    /// <returns>A region representing the areas that would be drawn.</returns>
    Region GetDrawRegion(
        FormattedText formattedText,
        Rectangle screen,
        Rectangle maximum = default);
}