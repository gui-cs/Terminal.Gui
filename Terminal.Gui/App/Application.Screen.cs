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
    /// <remarks>
    ///     Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/> to prevent
    ///     <see cref="Application"/> from changing it's size to match the new terminal size.
    /// </remarks>
    public static event EventHandler<SizeChangedEventArgs>? SizeChanging
    {
        add
        {
            if (ApplicationImpl.Instance is ApplicationImpl impl)
            {
                impl.SizeChanging += value;
            }
        }
        remove
        {
            if (ApplicationImpl.Instance is ApplicationImpl impl)
            {
                impl.SizeChanging -= value;
            }
        }
    }

    // Internal helper method for ApplicationImpl.ResetState to clear this event
    internal static void ClearSizeChangingEvent ()
    {
        if (ApplicationImpl.Instance is ApplicationImpl impl)
        {
            impl.SizeChanging = null;
        }
    }

    /// <summary>
    ///     Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="args">The new size.</param>
    /// <returns><see lanword="true"/>if the size was changed.</returns>
    public static bool OnSizeChanging (SizeChangedEventArgs args)
    {
        return ApplicationImpl.Instance.OnSizeChanging (args);
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
