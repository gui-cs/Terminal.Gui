namespace Terminal.Gui;

/// <summary>
///     Interface for color name resolvers.
/// </summary>
public interface IColorNameResolver
{
    /// <summary>
    ///     Returns the list of color names.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetColorNames ();

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a named color.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    bool TryParseColor (string name, out Color color);
}
