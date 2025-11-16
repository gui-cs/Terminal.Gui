#nullable disable
﻿namespace Terminal.Gui.Views;

/// <summary><see cref="Slider{T}"/>  Types</summary>
public enum SliderType
{
    /// <summary>
    ///     <code>
    /// ├─┼─┼─┼─┼─█─┼─┼─┼─┼─┼─┼─┤
    /// </code>
    /// </summary>
    Single,

    /// <summary>
    ///     <code>
    /// ├─┼─█─┼─┼─█─┼─┼─┼─┼─█─┼─┤
    /// </code>
    /// </summary>
    Multiple,

    /// <summary>
    ///     <code>
    /// ├▒▒▒▒▒▒▒▒▒█─┼─┼─┼─┼─┼─┼─┤
    /// </code>
    /// </summary>
    LeftRange,

    /// <summary>
    ///     <code>
    /// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒▒▒▒▒▒▒┤
    /// </code>
    /// </summary>
    RightRange,

    /// <summary>
    ///     <code>
    /// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒█─┼─┼─┤
    /// </code>
    /// </summary>
    Range
}
