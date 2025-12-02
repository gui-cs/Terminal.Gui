using System.Collections.ObjectModel;
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
    ///     Gets the observable collection of all application instances.
    ///     External observers can subscribe to this collection to monitor application lifecycle.
    /// </summary>
    public static ObservableCollection<IApplication> Apps { get; } = [];
    /// <summary>
    ///     Gets the singleton <see cref="IApplication"/> instance used by the legacy static Application model.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For new code, prefer using <see cref="Create"/> to get an instance-based application.
    ///         This property is provided for backward compatibility and internal use.
    ///     </para>
    ///     <para>
    ///         This property returns the same singleton instance used by the legacy static <see cref="Application"/>
    ///         methods like <see cref="Init"/> and <see cref="Run(IRunnable, Func{Exception, bool}?)"/>.
    ///     </para>
    /// </remarks>
    [Obsolete ("The legacy static Application object is going away. Use Application.Create() for new code.")]
    public static IApplication Instance => ApplicationImpl.Instance;

    /// <summary>
    ///     Creates a new <see cref="IApplication"/> instance.
    /// </summary>
    /// <param name="example">
    ///     If <see langword="true"/>, the application will run in example mode where metadata is collected
    ///     and demo keys are automatically sent when the first TopRunnable is modal.
    /// </param>
    /// <remarks>
    ///     The recommended pattern is for developers to call <c>Application.Create()</c> and then use the returned
    ///     <see cref="IApplication"/> instance for all subsequent application operations.
    /// </remarks>
    /// <returns>A new <see cref="IApplication"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the legacy static Application model has already been used in this process.
    /// </exception>
    public static IApplication Create (bool example = false)
    {
        //Debug.Fail ("Application.Create() called");
        ApplicationImpl.MarkInstanceBasedModelUsed ();

        ApplicationImpl app = new () { IsExample = example };
        Apps.Add (app);

        return app;
    }

    /// <inheritdoc cref="IApplication.Init"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Init (string? driverName = null)
    {
        //Debug.Fail ("Application.Init() called - parallelizable tests should not use legacy static Application model");
        ApplicationImpl.Instance.Init (driverName ?? ForceDriver);
    }

    /// <summary>
    ///     Gets or sets the main thread ID for the application.
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    public static int? MainThreadId
    {
        get => ApplicationImpl.Instance.MainThreadId;
        internal set => ApplicationImpl.Instance.MainThreadId = value;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Shutdown () => ApplicationImpl.Instance.Dispose ();

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
    internal static void ResetState (bool ignoreDisposed = false)
    {
        // Use the static reset method to bypass the fence check
        ApplicationImpl.ResetStateStatic (ignoreDisposed);
    }
}
