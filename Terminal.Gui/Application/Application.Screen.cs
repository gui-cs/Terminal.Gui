#nullable enable
namespace Terminal.Gui;

public static partial class Application // Screen related stuff
{
    private static Size _screenSize = new (2048, 2048);

    /// <summary>
    ///     INTERNAL API for Unit Tests. Only works if there's no driver.
    /// </summary>
    /// <param name="size"></param>
    internal static void SetScreenSize (Size size)
    {
        if (Driver is { })
        {
            throw new InvalidOperationException ("Cannot set the screen size when the ConsoleDriver is already initialized.");
        }
        _screenSize = size;
    }

    /// <summary>
    ///     Gets the size of the screen. This is the size of the screen as reported by the <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <remarks>
    ///     If the <see cref="ConsoleDriver"/> has not been initialized, this will return a default size of 2048x2048; useful for unit tests.
    /// </remarks>
    public static Rectangle Screen => Driver?.Screen ?? new (new (0, 0), _screenSize);

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/> to prevent
    ///     <see cref="Application"/> from changing it's size to match the new terminal size.
    /// </remarks>
    public static event EventHandler<SizeChangedEventArgs>? SizeChanging;

    /// <summary>
    ///     Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="args">The new size.</param>
    /// <returns><see lanword="true"/>if the size was changed.</returns>
    public static bool OnSizeChanging (SizeChangedEventArgs args)
    {
        SizeChanging?.Invoke (null, args);

        if (args.Cancel || args.Size is null)
        {
            return false;
        }

        foreach (Toplevel t in TopLevels)
        {
            t.OnSizeChanging (new (args.Size));
            t.SetLayoutNeeded ();
        }

        Refresh ();

        return true;
    }
}
