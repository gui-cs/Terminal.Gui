#nullable enable
namespace Terminal.Gui;

public static partial class Application // Screen related stuff
{
    /// <summary>
    ///     Gets the size of the screen. This is the size of the screen as reported by the <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <remarks>
    ///     If the <see cref="ConsoleDriver"/> has not been initialized, this will return a default size of 2048x2048; useful for unit tests.
    /// </remarks>
    public static Rectangle Screen => Driver?.Screen ?? new (0, 0, 2048, 2048);

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
            t.SetRelativeLayout (args.Size.Value);
            t.LayoutSubviews ();
            t.PositionToplevels ();
            t.OnSizeChanging (new (args.Size));

            if (PositionCursor (t))
            {
                Driver?.UpdateCursor ();
            }
        }

        Refresh ();

        return true;
    }
}
