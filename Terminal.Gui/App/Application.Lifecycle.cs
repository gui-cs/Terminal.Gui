using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualBasic;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Views;

namespace Terminal.Gui.App;

public static partial class Application // Lifecycle (Init/Shutdown)
{
    /// <summary>
    ///     Creates a new <see cref="IApplication"/> instance.
    /// </summary>
    /// <remarks>
    ///     The recommended pattern is for developers to call <c>Application.Create()</c> and then use the returned
    ///     <see cref="IApplication"/> instance for all subsequent application operations.
    /// </remarks>
    /// <returns>A new <see cref="IApplication"/> instance.</returns>
    public static IApplication Create () { return new ApplicationImpl (); }

    /// <inheritdoc cref="IApplication.Init"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Init (string? driverName = null)
    {
        ApplicationImpl.Instance.Init (driverName ?? ForceDriver);
    }

    /// <summary>
    ///     Gets or sets the main thread ID for the application.
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    public static int? MainThreadId
    {
        get => ((ApplicationImpl)ApplicationImpl.Instance).MainThreadId;
        set => ((ApplicationImpl)ApplicationImpl.Instance).MainThreadId = value;
    }

    /// <inheritdoc cref="IApplication.Shutdown"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Shutdown () => ApplicationImpl.Instance.Shutdown ();

    /// <inheritdoc cref="IApplication.Initialized"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool Initialized
    {
        get => ApplicationImpl.Instance.Initialized;
        internal set => ApplicationImpl.Instance.Initialized = value;
    }

    /// <inheritdoc cref="IApplication.InitializedChanged"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<EventArgs<bool>>? InitializedChanged
    {
        add => ApplicationImpl.Instance.InitializedChanged += value;
        remove => ApplicationImpl.Instance.InitializedChanged -= value;
    }

    // IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
    // Encapsulate all setting of initial state for Application; Having
    // this in a function like this ensures we don't make mistakes in
    // guaranteeing that the state of this singleton is deterministic when Init
    // starts running and after Shutdown returns.
    [Obsolete ("The legacy static Application object is going away.")]
    internal static void ResetState (bool ignoreDisposed = false) => ApplicationImpl.Instance?.ResetState (ignoreDisposed);
}
