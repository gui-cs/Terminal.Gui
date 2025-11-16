#nullable disable
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

    /// <inheritdoc cref="IApplication.Begin"/>
    public static SessionToken Begin (Toplevel toplevel) => ApplicationImpl.Instance.Begin (toplevel);

    /// <inheritdoc cref="IApplication.PositionCursor"/>
    public static bool PositionCursor () => ApplicationImpl.Instance.PositionCursor ();

    /// <inheritdoc cref="IApplication.Run(Func{Exception, bool}, string)"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driver = null) => ApplicationImpl.Instance.Run (errorHandler, driver);

    /// <inheritdoc cref="IApplication.Run{TView}(Func{Exception, bool}, string)"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static TView Run<TView> (Func<Exception, bool>? errorHandler = null, string? driver = null)
        where TView : Toplevel, new() => ApplicationImpl.Instance.Run<TView> (errorHandler, driver);

    /// <inheritdoc cref="IApplication.Run(Toplevel, Func{Exception, bool})"/>
    public static void Run (Toplevel view, Func<Exception, bool>? errorHandler = null) => ApplicationImpl.Instance.Run (view, errorHandler);

    /// <inheritdoc cref="IApplication.AddTimeout"/>
    public static object? AddTimeout (TimeSpan time, Func<bool> callback) => ApplicationImpl.Instance.AddTimeout (time, callback);

    /// <inheritdoc cref="IApplication.RemoveTimeout"/>
    public static bool RemoveTimeout (object token) => ApplicationImpl.Instance.RemoveTimeout (token);

    /// <inheritdoc cref="IApplication.TimedEvents"/>
    /// 
    public static ITimedEvents? TimedEvents => ApplicationImpl.Instance?.TimedEvents;
    /// <inheritdoc cref="IApplication.Invoke"/>
    public static void Invoke (Action action) => ApplicationImpl.Instance.Invoke (action);

    /// <inheritdoc cref="IApplication.LayoutAndDraw"/>
    public static void LayoutAndDraw (bool forceRedraw = false) => ApplicationImpl.Instance.LayoutAndDraw (forceRedraw);

    /// <inheritdoc cref="IApplication.StopAfterFirstIteration"/>
    public static bool StopAfterFirstIteration
    {
        get => ApplicationImpl.Instance.StopAfterFirstIteration;
        set => ApplicationImpl.Instance.StopAfterFirstIteration = value;
    }

    /// <inheritdoc cref="IApplication.RequestStop(Toplevel)"/>
    public static void RequestStop (Toplevel? top = null) => ApplicationImpl.Instance.RequestStop (top);

    /// <inheritdoc cref="IApplication.End"/>
    public static void End (SessionToken sessionToken) => ApplicationImpl.Instance.End (sessionToken);

    /// <inheritdoc cref="IApplication.RaiseIteration"/>
    internal static void RaiseIteration () => ApplicationImpl.Instance.RaiseIteration ();

    /// <inheritdoc cref="IApplication.Iteration"/>
    public static event EventHandler<IterationEventArgs>? Iteration
    {
        add => ApplicationImpl.Instance.Iteration += value;
        remove => ApplicationImpl.Instance.Iteration -= value;
    }

    /// <inheritdoc cref="IApplication.SessionBegun"/>
    public static event EventHandler<SessionTokenEventArgs>? SessionBegun
    {
        add => ApplicationImpl.Instance.SessionBegun += value;
        remove => ApplicationImpl.Instance.SessionBegun -= value;
    }

    /// <inheritdoc cref="IApplication.SessionEnded"/>
    public static event EventHandler<ToplevelEventArgs>? SessionEnded
    {
        add => ApplicationImpl.Instance.SessionEnded += value;
        remove => ApplicationImpl.Instance.SessionEnded -= value;
    }
}
