#nullable disable
﻿using System.Collections.Concurrent;

namespace Terminal.Gui.App;

/// <summary>Defines a session token for a running <see cref="Toplevel"/>.</summary>
public class SessionToken : IDisposable
{
    /// <summary>Initializes a new <see cref="SessionToken"/> class.</summary>
    /// <param name="view"></param>
    public SessionToken (Toplevel view) { Toplevel = view; }

    /// <summary>The <see cref="Toplevel"/> belonging to this <see cref="SessionToken"/>.</summary>
    public Toplevel Toplevel { get; internal set; }

    /// <summary>Releases all resource used by the <see cref="SessionToken"/> object.</summary>
    /// <remarks>Call <see cref="Dispose()"/> when you are finished using the <see cref="SessionToken"/>.</remarks>
    /// <remarks>
    ///     <see cref="Dispose()"/> method leaves the <see cref="SessionToken"/> in an unusable state. After calling
    ///     <see cref="Dispose()"/>, you must release all references to the <see cref="SessionToken"/> so the garbage collector can
    ///     reclaim the memory that the <see cref="SessionToken"/> was occupying.
    /// </remarks>
    public void Dispose ()
    {
        Dispose (true);
        GC.SuppressFinalize (this);
#if DEBUG_IDISPOSABLE
        WasDisposed = true;
#endif
    }

    /// <summary>Releases all resource used by the <see cref="SessionToken"/> object.</summary>
    /// <param name="disposing">If set to <see langword="true"/> we are disposing and should dispose held objects.</param>
    protected virtual void Dispose (bool disposing)
    {
        if (Toplevel is { } && disposing)
        {
            // Previously we were requiring Toplevel be disposed here.
            // But that is not correct becaue `Begin` didn't create the TopLevel, `Init` did; thus
            // disposing should be done by `Shutdown`, not `End`.
            throw new InvalidOperationException (
                                                 "Toplevel must be null before calling Application.SessionToken.Dispose"
                                                );
        }
    }

#if DEBUG_IDISPOSABLE
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    ///     Gets whether <see cref="SessionToken.Dispose"/> was called on this SessionToken or not.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public bool WasDisposed { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="SessionToken.Dispose"/> was called on this object.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public int DisposedCount { get; private set; } = 0;

    /// <summary>
    ///     Gets the list of SessionToken objects that have been created and not yet disposed.
    ///     Note, this is a static property and will affect all SessionToken objects.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public static ConcurrentBag<SessionToken> Instances { get; private set; } = [];

    /// <summary>Creates a new SessionToken object.</summary>
    public SessionToken ()
    {
        Instances.Add (this);
    }
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
#endif
}
