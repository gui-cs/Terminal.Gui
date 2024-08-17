﻿namespace Terminal.Gui;

/// <summary>
/// When implemented by a class, allows mapping <see cref="Color"/> to
/// human understandable name (e.g. w3c color names) and vice versa.
/// </summary>
public interface IColorNameResolver
{
    /// <summary>
    /// Returns the names of all known colors.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetColorNames ();

    /// <summary>
    ///  Returns <see langword="true"/> if <paramref name="name"/> is a recognised
    /// color. In which case <paramref name="color"/> will be the color the name corresponds
    /// to otherwise returns false.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    bool TryParseColor (string name, out Color color);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="color"/> is a recognised
    /// color. In which case <paramref name="name"/> will be the name of the color and
    /// return value will be true otherwise false.
    /// </summary>
    /// <param name="color"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    bool TryNameColor (Color color, out string name);
}
