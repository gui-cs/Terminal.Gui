#nullable enable
namespace Terminal.Gui;

public static partial class Application // Toplevel handling
{
    // BUGBUG: Technically, this is not the full lst of TopLevels. There be dragons here, e.g. see how Toplevel.Id is used. What

    /// <summary>Holds the stack of TopLevel views.</summary>
    internal static Stack<Toplevel> TopLevels { get; } = new ();

    /// <summary>The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Top"/>)</summary>
    /// <value>The top.</value>
    public static Toplevel? Top { get; internal set; }

    // TODO: Determine why this can't just return _topLevels.Peek()?
    /// <summary>
    ///     The current <see cref="Toplevel"/> object. This is updated in <see cref="Application.Begin"/> enters and leaves to
    ///     point to the current
    ///     <see cref="Toplevel"/> .
    /// </summary>
    /// <remarks>
    ///     This will only be distinct from <see cref="Application.Top"/> in scenarios where <see cref="Toplevel.IsOverlappedContainer"/> is <see langword="true"/>.
    /// </remarks>
    /// <value>The current.</value>
    public static Toplevel? Current { get; internal set; }

    /// <summary>
    ///     If <paramref name="topLevel"/> is not already Current and visible, finds the last Modal Toplevel in the stack and makes it Current.
    /// </summary>
    private static void EnsureModalOrVisibleAlwaysOnTop (Toplevel topLevel)
    {
        if (!topLevel.Running
            || (topLevel == Current && topLevel.Visible)
            || ApplicationOverlapped.OverlappedTop == null
            || TopLevels.Peek ().Modal)
        {
            return;
        }

        foreach (Toplevel top in TopLevels.Reverse ())
        {
            if (top.Modal && top != Current)
            {
                ApplicationOverlapped.MoveCurrent (top);

                return;
            }
        }

        if (!topLevel.Visible && topLevel == Current)
        {
            ApplicationOverlapped.OverlappedMoveNext ();
        }
    }

}
