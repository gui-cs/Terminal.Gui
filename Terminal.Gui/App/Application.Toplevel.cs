#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // Toplevel handling
{
    // BUGBUG: Technically, this is not the full lst of TopLevels. There be dragons here, e.g. see how Toplevel.Id is used. What

    /// <summary>Holds the stack of TopLevel views.</summary>
    internal static ConcurrentStack<Toplevel> TopLevels => ApplicationImpl.Instance.TopLevels;

    /// <summary>The <see cref="Toplevel"/> that is currently active.</summary>
    /// <value>The top.</value>
    public static Toplevel? Top
    {
        get => ApplicationImpl.Instance.Top;
        internal set => ApplicationImpl.Instance.Top = value;
    }
}
