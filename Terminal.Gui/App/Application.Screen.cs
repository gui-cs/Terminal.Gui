
namespace Terminal.Gui.App;

public static partial class Application // Screen related stuff; intended to hide Driver details
{
    /// <inheritdoc cref="IApplication.Screen"/>

    [Obsolete ("The legacy static Application object is going away.")]
    public static Rectangle Screen
    {
        get => ApplicationImpl.Instance.Screen;
        set => ApplicationImpl.Instance.Screen = value;
    }

    /// <inheritdoc cref="IApplication.ScreenChanged"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<EventArgs<Rectangle>>? ScreenChanged
    {
        add => ApplicationImpl.Instance.ScreenChanged += value;
        remove => ApplicationImpl.Instance.ScreenChanged -= value;
    }

    /// <inheritdoc cref="IApplication.ClearScreenNextIteration"/>

    [Obsolete ("The legacy static Application object is going away.")]
    internal static bool ClearScreenNextIteration
    {
        get => ApplicationImpl.Instance.ClearScreenNextIteration;
        set => ApplicationImpl.Instance.ClearScreenNextIteration = value;
    }
}
