using System.Reflection;

namespace Terminal.Gui;

/// <summary>Responder base class implemented by objects that want to participate on keyboard and mouse input.</summary>
public class Responder : IDisposable
{
    private bool _disposedValue;

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose ()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Disposing?.Invoke (this, EventArgs.Empty);
        Dispose (true);
        GC.SuppressFinalize (this);
#if DEBUG_IDISPOSABLE
        WasDisposed = true;

        foreach (Responder instance in Instances.Where (x => x.WasDisposed).ToList ())
        {
            Instances.Remove (instance);
        }
#endif
    }

    /// <summary>Event raised when <see cref="Dispose" /> has been called to signal that this object is being disposed.</summary>
    [CanBeNull]
    public event EventHandler Disposing;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    /// <remarks>
    ///     If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and
    ///     unmanaged resources can be disposed. If disposing equals false, the method has been called by the runtime from
    ///     inside the finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
    /// </remarks>
    /// <param name="disposing"></param>
    protected virtual void Dispose (bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _disposedValue = true;
        }
    }

    // TODO: v2 - nuke this
    /// <summary>Utility function to determine <paramref name="method"/> is overridden in the <paramref name="subclass"/>.</summary>
    /// <param name="subclass">The view.</param>
    /// <param name="method">The method name.</param>
    /// <returns><see langword="true"/> if it's overridden, <see langword="false"/> otherwise.</returns>
    internal static bool IsOverridden (Responder subclass, string method)
    {
        MethodInfo m = subclass.GetType ()
                               .GetMethod (
                                           method,
                                           BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.NonPublic
                                           | BindingFlags.DeclaredOnly
                                          );

        if (m is null)
        {
            return false;
        }

        return m.GetBaseDefinition ().DeclaringType != m.DeclaringType;
    }

#if DEBUG_IDISPOSABLE
    /// <summary>For debug purposes to verify objects are being disposed properly</summary>
    public bool WasDisposed;

    /// <summary>For debug purposes to verify objects are being disposed properly</summary>
    public int DisposedCount = 0;

    /// <summary>For debug purposes</summary>
    public static List<Responder> Instances = new ();

    /// <summary>For debug purposes</summary>
    public Responder () { Instances.Add (this); }
#endif
}
