using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public static partial class Application // Run (Begin -> Run -> Layout/Draw -> End -> Stop)
{
    private static Key _quitKey = Key.Esc; // Resources/config.json overrides

    /// <summary>Gets or sets the key to quit the application.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key QuitKey
    {
        get => _quitKey;
        set
        {
            Key oldValue = _quitKey;
            _quitKey = value;
            QuitKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, _quitKey));
        }
    }

    /// <summary>Raised when <see cref="QuitKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? QuitKeyChanged;

    private static Key _arrangeKey = Key.F5.WithCtrl; // Resources/config.json overrides

    /// <summary>Gets or sets the key to activate arranging views using the keyboard.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key ArrangeKey
    {
        get => _arrangeKey;
        set
        {
            Key oldValue = _arrangeKey;
            _arrangeKey = value;
            ArrangeKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, _arrangeKey));
        }
    }

    /// <summary>Raised when <see cref="ArrangeKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? ArrangeKeyChanged;

    /// <inheritdoc cref="IApplication.Begin(IRunnable)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static SessionToken Begin (Toplevel toplevel) => ApplicationImpl.Instance.Begin (toplevel);

    /// <inheritdoc cref="IApplication.PositionCursor"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool PositionCursor () => ApplicationImpl.Instance.PositionCursor ();

    /// <inheritdoc cref="IApplication.Run{TRunnable}(Func{Exception, bool}, string)"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    [Obsolete ("The legacy static Application object is going away.")]
    public static IApplication Run<TRunnable> (Func<Exception, bool>? errorHandler = null, string? driverName = null)
        where TRunnable : Toplevel, new() => ApplicationImpl.Instance.Run<TRunnable> (errorHandler, driverName);

    /// <inheritdoc cref="IApplication.Run(Toplevel, Func{Exception, bool})"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Run (Toplevel view, Func<Exception, bool>? errorHandler = null) => ApplicationImpl.Instance.Run (view, errorHandler);

    /// <inheritdoc cref="IApplication.AddTimeout"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static object? AddTimeout (TimeSpan time, Func<bool> callback) => ApplicationImpl.Instance.AddTimeout (time, callback);

    /// <inheritdoc cref="IApplication.RemoveTimeout"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool RemoveTimeout (object token) => ApplicationImpl.Instance.RemoveTimeout (token);

    /// <inheritdoc cref="IApplication.TimedEvents"/>
    /// 
    [Obsolete ("The legacy static Application object is going away.")]
    public static ITimedEvents? TimedEvents => ApplicationImpl.Instance.TimedEvents;

    /// <inheritdoc cref="IApplication.Invoke(Action{IApplication})"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Invoke (Action<IApplication> action) => ApplicationImpl.Instance.Invoke (action);

    /// <inheritdoc cref="IApplication.Invoke(Action)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Invoke (Action action) => ApplicationImpl.Instance.Invoke (action);

    /// <inheritdoc cref="IApplication.LayoutAndDraw"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void LayoutAndDraw (bool forceRedraw = false) => ApplicationImpl.Instance.LayoutAndDraw (forceRedraw);

    /// <inheritdoc cref="IApplication.StopAfterFirstIteration"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool StopAfterFirstIteration
    {
        get => ApplicationImpl.Instance.StopAfterFirstIteration;
        set => ApplicationImpl.Instance.StopAfterFirstIteration = value;
    }

    /// <inheritdoc cref="IApplication.RequestStop(Toplevel)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void RequestStop (Toplevel? top = null) => ApplicationImpl.Instance.RequestStop (top);

    /// <inheritdoc cref="IApplication.End(RunnableSessionToken)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void End (SessionToken sessionToken) => ApplicationImpl.Instance.End (sessionToken);

    /// <inheritdoc cref="IApplication.Iteration"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<EventArgs<IApplication?>>? Iteration
    {
        add => ApplicationImpl.Instance.Iteration += value;
        remove => ApplicationImpl.Instance.Iteration -= value;
    }

    /// <inheritdoc cref="IApplication.SessionBegun"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<SessionTokenEventArgs>? SessionBegun
    {
        add => ApplicationImpl.Instance.SessionBegun += value;
        remove => ApplicationImpl.Instance.SessionBegun -= value;
    }

    /// <inheritdoc cref="IApplication.SessionEnded"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<ToplevelEventArgs>? SessionEnded
    {
        add => ApplicationImpl.Instance.SessionEnded += value;
        remove => ApplicationImpl.Instance.SessionEnded -= value;
    }
}
