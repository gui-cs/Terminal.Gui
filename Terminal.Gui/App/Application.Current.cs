using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // Current handling
{
    /// <inheritdoc cref="IApplication.SessionStack"/>
    [Obsolete ("The legacy static Application object is going away.")] public static ConcurrentStack<Toplevel> SessionStack => ApplicationImpl.Instance.SessionStack;

    /// <summary>The <see cref="Toplevel"/> that is currently active.</summary>
    /// <value>The current toplevel.</value>
    [Obsolete ("The legacy static Application object is going away.")]
    public static Toplevel? Current
    {
        get => ApplicationImpl.Instance.Current;
        internal set => ApplicationImpl.Instance.Current = value;
    }
}
