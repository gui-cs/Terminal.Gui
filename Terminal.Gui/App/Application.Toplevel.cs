#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.App;

public static partial class Application // Toplevel handling
{
    // BUGBUG: Technically, this is not the full lst of TopLevels. There be dragons here, e.g. see how Toplevel.Id is used. What

    private static readonly ConcurrentStack<Toplevel> _topLevels = new ();
    private static readonly object _topLevelsLock = new ();

    /// <summary>Holds the stack of TopLevel views.</summary>
    internal static ConcurrentStack<Toplevel> TopLevels
    {
        get
        {
            lock (_topLevelsLock)
            {
                return _topLevels;
            }
        }
    }

    /// <summary>The <see cref="Toplevel"/> that is currently active.</summary>
    /// <value>The top.</value>
    public static Toplevel? Top { get; internal set; }
}
