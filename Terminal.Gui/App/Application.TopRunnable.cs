using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // TopRunnable handling
{
    /// <summary>The <see cref="View"/> that is on the top of the <see cref="IApplication.SessionStack"/>.</summary>
    /// <value>The top runnable.</value>
    [Obsolete ("The legacy static Application object is going away.")]
    public static View? TopRunnableView
    {
        get => ApplicationImpl.Instance.TopRunnableView;
        internal set => ApplicationImpl.Instance.TopRunnableView = value;
    }

    /// <summary>The <see cref="View"/> that is on the top of the <see cref="IApplication.SessionStack"/>.</summary>
    /// <value>The top runnable.</value>
    [Obsolete ("The legacy static Application object is going away.")]
    public static IRunnable? TopRunnable
    {
        get => ApplicationImpl.Instance.TopRunnable;
        internal set => ApplicationImpl.Instance.TopRunnable= value;
    }
}
