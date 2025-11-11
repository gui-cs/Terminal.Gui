#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // Toplevel handling
{
    /// <inheritdoc cref="IApplication.TopLevels"/>
    public static ConcurrentStack<Toplevel> TopLevels => ApplicationImpl.Instance.TopLevels;

    /// <summary>The <see cref="Toplevel"/> that is currently active.</summary>
    /// <value>The top.</value>
    public static Toplevel? Top
    {
        get => ApplicationImpl.Instance.Top;
        internal set => ApplicationImpl.Instance.Top = value;
    }
}
