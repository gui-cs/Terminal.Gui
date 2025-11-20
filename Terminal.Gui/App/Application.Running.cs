using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // Running handling
{
    /// <inheritdoc cref="IApplication.SessionStack"/>
    [Obsolete ("The legacy static Application object is going away.")] public static ConcurrentStack<Toplevel> SessionStack => ApplicationImpl.Instance.SessionStack;

    /// <summary>The <see cref="Toplevel"/> that is currently running.</summary>
    /// <value>The running toplevel.</value>
    [Obsolete ("The legacy static Application object is going away.")]
    public static Toplevel? Running
    {
        get => ApplicationImpl.Instance.Running;
        internal set => ApplicationImpl.Instance.Running = value;
    }
}
