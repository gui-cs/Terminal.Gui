using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public static partial class Application // Run (Begin -> Run -> Layout/Draw -> End -> Stop)
{
    /// <summary>Gets or sets the key to quit the application.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key QuitKey
    {
        get => ApplicationImpl.Instance.Keyboard.QuitKey;
        set => ApplicationImpl.Instance.Keyboard.QuitKey = value;
    }

    /// <summary>Gets or sets the key to activate arranging views using the keyboard.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key ArrangeKey
    {
        get => ApplicationImpl.Instance.Keyboard.ArrangeKey;
        set => ApplicationImpl.Instance.Keyboard.ArrangeKey = value;
    }

    /// <inheritdoc cref="IApplication.Begin(IRunnable)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static SessionToken Begin (Toplevel toplevel) => ApplicationImpl.Instance.Begin (toplevel);

    /// <inheritdoc cref="IApplication.PositionCursor"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool PositionCursor () => ApplicationImpl.Instance.PositionCursor ();

    /// <inheritdoc cref="IApplication.Run(Func{Exception, bool}, string)"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    [Obsolete ("The legacy static Application object is going away.")]
    public static Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driverName = null) => ApplicationImpl.Instance.Run (errorHandler, driverName);

    /// <inheritdoc cref="IApplication.Run{TView}(Func{Exception, bool}, string)"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    [Obsolete ("The legacy static Application object is going away.")]
    public static TView Run<TView> (Func<Exception, bool>? errorHandler = null, string? driverName = null)
        where TView : Toplevel, new() => ApplicationImpl.Instance.Run<TView> (errorHandler, driverName);

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
    public static ITimedEvents? TimedEvents => ApplicationImpl.Instance?.TimedEvents;

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
