namespace Terminal.Gui;

/// <summary>
///     Determines how items will be arranged in a container when alignment is <see cref="Alignment.Start"/> or <see cref="Alignment.End"/>,
/// </summary>
[Flags]
public enum FlowModes
{
    /// <summary>
    ///     The items will be arranged from start (left/top) to end (right/bottom).
    /// </summary>
    StartToEnd = 0,

    /// <summary>
    ///     The items will be arranged from end (right/bottom) to start (left/top).
    /// </summary>
    /// <remarks>
    ///     Not implemented.
    /// </remarks>
    EndToStart = 1,

    /// <summary>
    ///    When aligning via <see cref="Alignment.Start"/> or <see cref="Alignment.End"/>, the first item will be aligned at the opposite end.
    /// </summary>
    IgnoreFirst = 2,

    /// <summary>
    ///    When aligning via <see cref="Alignment.Start"/> or <see cref="Alignment.End"/>, the last item will be aligned at the opposite end.
    /// </summary>
    IgnoreLast = 4,
}