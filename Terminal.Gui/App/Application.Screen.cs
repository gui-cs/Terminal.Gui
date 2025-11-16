
namespace Terminal.Gui.App;

public static partial class Application // Screen related stuff; intended to hide Driver details
{
    /// <inheritdoc cref="IApplication.Screen"/>

    public static Rectangle Screen
    {
        get => ApplicationImpl.Instance.Screen;
        set => ApplicationImpl.Instance.Screen = value;
    }

    /// <inheritdoc cref="IApplication.ScreenChanged"/>
    public static event EventHandler<EventArgs<Rectangle>>? ScreenChanged
    {
        add => ApplicationImpl.Instance.ScreenChanged += value;
        remove => ApplicationImpl.Instance.ScreenChanged -= value;
    }

    /// <inheritdoc cref="IApplication.ClearScreenNextIteration"/>

    internal static bool ClearScreenNextIteration
    {
        get => ApplicationImpl.Instance.ClearScreenNextIteration;
        set => ApplicationImpl.Instance.ClearScreenNextIteration = value;
    }
}
