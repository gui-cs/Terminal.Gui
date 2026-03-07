// We use global using directives to simplify the code and avoid repetitive namespace declarations.
// Put them here so they are available throughout the application.
// Do not put them in AssemblyInfo.cs as it will break GitVersion's /updateassemblyinfo

global using Attribute = Terminal.Gui.Drawing.Attribute;
global using Color = Terminal.Gui.Drawing.Color;
global using CM = Terminal.Gui.Configuration.ConfigurationManager;
global using Terminal.Gui.App;
global using Terminal.Gui.Testing;
global using Terminal.Gui.Time;
global using Terminal.Gui.Drivers;
global using Terminal.Gui.Input;
global using Terminal.Gui.Configuration;
global using Terminal.Gui.ViewBase;
global using Terminal.Gui.Views;
global using Terminal.Gui.Drawing;
global using Terminal.Gui.Text;
global using Terminal.Gui.Resources;
global using Terminal.Gui.FileServices;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Terminal.Gui.App;

/// <summary>
///     <para>
///         The <c>Application</c> class provides static methods and properties for managing the application's lifecycle,
///         configuration, and global events. It serves as the primary entry point for creating and running a Terminal.Gui
///         application. The class includes methods for creating application instances, configuring global settings, and
///         raising events related to application creation, initialization, and disposal.
///     </para>
///     <para>
///         It also provides properties for managing supported cultures and key bindings for common actions. The
///         <c>Application</c> class is designed
///         to support both a modern instance-based model (where developers create and manage their own
///         <see cref="IApplication"/> instances) and a legacy static model (which is being phased out). The events in this
///         class are thread-local, allowing for parallel test execution where each thread can independently monitor
///         application instances created on that thread.
///     </para>
/// </summary>
/// <example>
///     <para>
///         Here's a simple example of how to create and run a Terminal.Gui application using the modern instance-based
///         model:
///     </para>
///     <code>
///       IApplication app = Application.Create ().Init ();
///       using Window top = new ();
///       top.Add(myView);
///       app.Run(top);
///    </code>
/// </example>
public static partial class Application
{
    /// <summary>
    ///     Creates a new <see cref="IApplication"/> instance.
    /// </summary>
    /// <param name="timeProvider">
    ///     Optional time provider for controlling time in tests. If <see langword="null"/>, defaults to
    ///     <see cref="SystemTimeProvider"/>.
    ///     For production use, omit this parameter or pass <see langword="null"/>. For testing, pass a
    ///     <see cref="VirtualTimeProvider"/>.
    /// </param>
    /// <remarks>
    ///     The recommended pattern is for developers to call <c>Application.Create()</c> and then use the returned
    ///     <see cref="IApplication"/> instance for all subsequent application operations.
    /// </remarks>
    /// <returns>A new <see cref="IApplication"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the legacy static Application model has already been used in this process.
    /// </exception>
    public static IApplication Create (ITimeProvider? timeProvider = null)
    {
        //Debug.Fail ("Application.Create() called");
        ApplicationImpl.MarkInstanceBasedModelUsed ();

        IApplication app = new ApplicationImpl (timeProvider ?? new SystemTimeProvider ());
        RaiseInstanceCreated (app);

        return app;
    }

    /// <summary>
    ///     Determines whether the current execution context is within a unit test project.
    /// </summary>
    /// <remarks>
    ///     This method leverages <see cref="AppContext"/> switches to ensure compatibility with
    ///     <b>Native AOT</b> and avoids trimming-related issues associated with reflection.
    ///     <para>
    ///         For this to return <c>true</c>, the test project (e.g., xUnit v3) must define the
    ///         switch in its <c>.csproj</c> file:
    ///         <code>
    /// &lt;ItemGroup&gt;
    ///   &lt;RuntimeHostConfigurationOption Include="Runtime.IsTestProject" Value="true" Trim="false" /&gt;
    /// &lt;/ItemGroup&gt;
    /// </code>
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the test execution flag is set; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsRunningInTest () => AppContext.TryGetSwitch ("Runtime.IsTestProject", out bool isTest) && isTest;

    #region Modern Instance-Based Model Events (Thread-Local)

    // Thread-local backing fields for events - each thread has its own subscribers
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceCreated = new ();
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceInitialized = new ();
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceDisposed = new ();

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance is created via <see cref="Create"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires immediately after
    ///         <see cref="Create"/> creates a new instance, before <see cref="IApplication.Init"/> is called.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances created on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceCreated
    {
        add => _instanceCreated.Value += value;
        remove => _instanceCreated.Value -= value;
    }

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance completes initialization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires after
    ///         <see cref="IApplication.Init"/> completes successfully.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances initialized on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceInitialized
    {
        add => _instanceInitialized.Value += value;
        remove => _instanceInitialized.Value -= value;
    }

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance is disposed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires when
    ///         <see cref="IDisposable.Dispose"/> is called on an instance.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances disposed on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceDisposed
    {
        add => _instanceDisposed.Value += value;
        remove => _instanceDisposed.Value -= value;
    }

    /// <summary>
    ///     Raises the <see cref="InstanceCreated"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceCreated (IApplication app) => _instanceCreated.Value?.Invoke (null, new (app));

    /// <summary>
    ///     Raises the <see cref="InstanceInitialized"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceInitialized (IApplication app) => _instanceInitialized.Value?.Invoke (null, new (app));

    /// <summary>
    ///     Raises the <see cref="InstanceDisposed"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceDisposed (IApplication app) => _instanceDisposed.Value?.Invoke (null, new (app));

    #endregion Modern Instance-Based Model Events (Thread-Local)

    /// <summary>
    ///     Maximum number of iterations of the main loop (and hence draws)
    ///     to allow to occur per second. Defaults to 25ms.
    ///     <remarks>
    ///         Note that not every iteration draws (see <see cref="View.NeedsDraw"/>).
    ///     </remarks>
    /// </summary>

    public static ushort MaximumIterationsPerSecond { get; set; } = 25;

    /// <summary>
    ///     Gets the default maximum number of iterations per second for the main loop.
    /// </summary>
    /// <remarks>
    ///     This value determines the default upper limit on how many times the main loop can execute per
    ///     second. Adjusting this value can affect application responsiveness and CPU usage.
    /// </remarks>
    public static ushort DefaultMaximumIterationsPerSecond { get; } = 25;

    /// <summary>Gets all cultures supported by the application without the invariant language.</summary>
    public static List<CultureInfo>? SupportedCultures { get; private set; } = GetSupportedCultures ();

    internal static List<CultureInfo> GetAvailableCulturesFromEmbeddedResources ()
    {
        ResourceManager rm = new (typeof (Strings));

        CultureInfo [] cultures = CultureInfo.GetCultures (CultureTypes.AllCultures);

        return cultures.Where (cultureInfo => !cultureInfo.Equals (CultureInfo.InvariantCulture) && rm.GetResourceSet (cultureInfo, true, false) is { })
                       .ToList ();
    }

    // BUGBUG: This does not return en-US even though it's supported by default
    internal static List<CultureInfo> GetSupportedCultures ()
    {
        CultureInfo [] cultures = CultureInfo.GetCultures (CultureTypes.AllCultures);

        // Get the assembly
        var assembly = Assembly.GetExecutingAssembly ();

        //Find the location of the assembly
        string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

        // Find the resource file name of the assembly
        var resourceFilename = $"{assembly.GetName ().Name}.resources.dll";

        if (cultures.Length > 1 && Directory.Exists (Path.Combine (assemblyLocation, "pt-PT")))
        {
            // Return all culture for which satellite folder found with culture code.
            return cultures.Where (cultureInfo => Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name))
                                                  && File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename)))
                           .ToList ();
        }

        // It's called from a self-contained single-file and get available cultures from the embedded resources strings.
        return GetAvailableCulturesFromEmbeddedResources ();
    }

    /// <inheritdoc cref="IApplication.ForceDriver"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static string ForceDriver
    {
        get;
        set
        {
            string oldValue = field;
            field = value;
            ForceDriverChanged?.Invoke (null, new (oldValue, field));
        }
    } = string.Empty;

    /// <summary>Gets or sets the key to quit the application.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key QuitKey
    {
        get;
        set
        {
            Key oldValue = field;
            field = value;
            QuitKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, field));
        }
    } = Key.Esc;

    /// <summary>Raised when <see cref="QuitKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? QuitKeyChanged;

    /// <summary>Gets or sets the key to activate arranging views using the keyboard.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key ArrangeKey
    {
        get;
        set
        {
            Key oldValue = field;
            field = value;
            ArrangeKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, field));
        }
    } = Key.F5.WithCtrl;

    /// <summary>Raised when <see cref="ArrangeKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? ArrangeKeyChanged;

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabGroupKey
    {
        get;
        set
        {
            Key oldValue = field;
            field = value;
            NextTabGroupKeyChanged?.Invoke (null, new (oldValue, field));
        }
    } = Key.F6;

    /// <summary>Raised when <see cref="NextTabGroupKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? NextTabGroupKeyChanged;

    /// <summary>Alternative key to navigate forwards through views. Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabKey
    {
        get;
        set
        {
            Key oldValue = field;
            field = value;
            NextTabKeyChanged?.Invoke (null, new (oldValue, field));
        }
    } = Key.Tab;

    /// <summary>Raised when <see cref="NextTabKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? NextTabKeyChanged;

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabGroupKey
    {
        get;
        set
        {
            Key oldValue = field;
            field = value;
            PrevTabGroupKeyChanged?.Invoke (null, new (oldValue, field));
        }
    } = Key.F6.WithShift;

    /// <summary>Raised when <see cref="PrevTabGroupKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? PrevTabGroupKeyChanged;

    /// <summary>Alternative key to navigate backwards through views. Shift+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabKey
    {
        get;
        set
        {
            Key oldValue = field;
            field = value;
            PrevTabKeyChanged?.Invoke (null, new (oldValue, field));
        }
    } = Key.Tab.WithShift;

    /// <summary>Raised when <see cref="PrevTabKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? PrevTabKeyChanged;
}
