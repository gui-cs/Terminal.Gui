using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // TopRunnable handling
{
    /// <inheritdoc cref="IApplication.SessionStack"/>
    [Obsolete ("The legacy static Application object is going away.")] public static ConcurrentStack<Toplevel> SessionStack => ApplicationImpl.Instance.SessionStack;

    /// <summary>The <see cref="Toplevel"/> that is on the top of the <see cref="SessionStack"/>.</summary>
    /// <value>The top runnable.</value>
    [Obsolete ("The legacy static Application object is going away.")]
    public static Toplevel? TopRunnable
    {
        get => ApplicationImpl.Instance.TopRunnable;
        internal set => ApplicationImpl.Instance.TopRunnable = value;
    }
}
