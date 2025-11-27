using System.Collections.Concurrent;

namespace Terminal.Gui.App;

/// <summary>
///     Represents a running session created by <see cref="IApplication.Begin(IRunnable)"/>.
///     Wraps an <see cref="IRunnable"/> instance and is stored in <see cref="IApplication.SessionStack"/>.
/// </summary>
public class SessionToken : IDisposable
{
    internal SessionToken (IRunnable runnable) { Runnable = runnable; }

    /// <summary>
    ///     Gets or sets the runnable associated with this session.
    ///     Set to <see langword="null"/> by <see cref="IApplication.End(SessionToken)"/> when the session completes.
    /// </summary>
    public IRunnable? Runnable { get; internal set; }

    /// <summary>
    ///     Releases all resource used by the <see cref="SessionToken"/> object.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Call <see cref="Dispose()"/> when you are finished using the <see cref="SessionToken"/>.
    ///     </para>
    ///     <para>
    ///         <see cref="Dispose()"/> method leaves the <see cref="SessionToken"/> in an unusable state. After
    ///         calling
    ///         <see cref="Dispose()"/>, you must release all references to the <see cref="SessionToken"/> so the
    ///         garbage collector can
    ///         reclaim the memory that the <see cref="SessionToken"/> was occupying.
    ///     </para>
    /// </remarks>
    public void Dispose ()
    {
        Dispose (true);
        GC.SuppressFinalize (this);

#if DEBUG_IDISPOSABLE
        WasDisposed = true;
#endif
    }

    /// <summary>
    ///     Releases all resource used by the <see cref="SessionToken"/> object.
    /// </summary>
    /// <param name="disposing">If set to <see langword="true"/> we are disposing and should dispose held objects.</param>
    protected virtual void Dispose (bool disposing)
    {
        if (Runnable is { } && disposing)
        {
            // Runnable must be null before disposing
            throw new InvalidOperationException (
                                                 "Runnable must be null before calling RunnableSessionToken.Dispose"
                                                );
        }
    }

#if DEBUG_IDISPOSABLE
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    ///     Gets whether <see cref="SessionToken.Dispose"/> was called on this RunnableSessionToken or not.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public bool WasDisposed { get; private set; }

    /// <summary>
    ///     Gets the number of times <see cref="SessionToken.Dispose"/> was called on this object.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public int DisposedCount { get; private set; }

    /// <summary>
    ///     Gets the list of RunnableSessionToken objects that have been created and not yet disposed.
    ///     Note, this is a static property and will affect all RunnableSessionToken objects.
    ///     For debug purposes to verify objects are being disposed properly.
    ///     Only valid when DEBUG_IDISPOSABLE is defined.
    /// </summary>
    public static ConcurrentBag<SessionToken> Instances { get; } = [];

    /// <summary>Creates a new RunnableSessionToken object.</summary>
    public SessionToken () { Instances.Add (this); }
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
#endif
}
