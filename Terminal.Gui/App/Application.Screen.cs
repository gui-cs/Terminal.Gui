#nullable enable

namespace Terminal.Gui.App;

public static partial class Application // Screen related stuff; intended to hide Driver details
{
    /// <summary>
    ///     Gets or sets the size of the screen. By default, this is the size of the screen as reported by the <see cref="IDriver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     If the <see cref="IDriver"/> has not been initialized, this will return a default size of 2048x2048; useful for unit tests.
    /// </para>
    /// </remarks>
    public static Rectangle Screen
    {
        get => ApplicationImpl.Instance.Screen;
        set => ApplicationImpl.Instance.Screen = value;
    }

    /// <summary>
    ///     Gets or sets whether the screen will be cleared, and all Views redrawn, during the next Application iteration.
    /// </summary>
    /// <remarks>
    ///     This is typical set to true when a View's <see cref="View.Frame"/> changes and that view has no
    ///     SuperView (e.g. when <see cref="Application.Top"/> is moved or resized.
    /// </remarks>
    internal static bool ClearScreenNextIteration
    {
        get => ApplicationImpl.Instance.ClearScreenNextIteration;
        set => ApplicationImpl.Instance.ClearScreenNextIteration = value;
    }
}
