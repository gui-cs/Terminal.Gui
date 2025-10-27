#nullable enable

namespace Terminal.Gui.App;

public static partial class Application // Screen related stuff; intended to hide Driver details
{
    /// <summary>
    ///     Gets or sets the size of the screen. By default, this is the size of the screen as reported by the <see cref="IConsoleDriver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     If the <see cref="IConsoleDriver"/> has not been initialized, this will return a default size of 2048x2048; useful for unit tests.
    /// </para>
    /// </remarks>
    public static Rectangle Screen
    {
        get => ApplicationImpl.Instance.Screen;
        set => ApplicationImpl.Instance.Screen = value;
    }

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    public static event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    /// <summary>
    ///     Called when the application's size has changed. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="ScreenChanged"/> event.
    /// </summary>
    /// <param name="screen">The new screen size and position.</param>
    public static void RaiseScreenChangedEvent (Rectangle screen)
    {
        Screen = new (Point.Empty, screen.Size);

        ScreenChanged?.Invoke (ApplicationImpl.Instance, new (screen));

        foreach (Toplevel t in TopLevels)
        {
            t.OnSizeChanging (new (screen.Size));
            t.SetNeedsLayout ();
        }

        LayoutAndDraw (true);
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
