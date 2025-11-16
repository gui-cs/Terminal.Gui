#nullable disable
using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // Toplevel handling
{
    /// <inheritdoc cref="IApplication.SessionStack"/>
    public static ConcurrentStack<Toplevel> SessionStack => ApplicationImpl.Instance.SessionStack;

    /// <summary>The <see cref="Toplevel"/> that is currently active.</summary>
    /// <value>The current toplevel.</value>
    public static Toplevel? Current
    {
        get => ApplicationImpl.Instance.Current;
        internal set => ApplicationImpl.Instance.Current = value;
    }
}
