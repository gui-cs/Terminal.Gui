using System.Collections.Concurrent;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public IDriver? Driver { get; set; }

    /// <inheritdoc/>
    public bool Force16Colors { get; set; }

    /// <inheritdoc/>
    public string ForceDriver { get; set; } = string.Empty;

    /// <inheritdoc/>
    public List<SixelToRender> Sixel { get; } = new ();

    /// <summary>
    ///     Creates the appropriate <see cref="IDriver"/> based on platform and driverName.
    /// </summary>
    /// <param name="driverName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private void CreateDriver (string? driverName)
    {
        PlatformID p = Environment.OSVersion.Platform;

        // Check component factory type first - this takes precedence over driverName
        bool factoryIsWindows = _componentFactory is IComponentFactory<WindowsConsole.InputRecord>;
        bool factoryIsDotNet = _componentFactory is IComponentFactory<ConsoleKeyInfo>;
        bool factoryIsUnix = _componentFactory is IComponentFactory<char>;
        bool factoryIsFake = _componentFactory is IComponentFactory<ConsoleKeyInfo>;

        // Then check driverName
        bool nameIsWindows = driverName?.Contains ("windows", StringComparison.OrdinalIgnoreCase) ?? false;
        bool nameIsDotNet = driverName?.Contains ("dotnet", StringComparison.OrdinalIgnoreCase) ?? false;
        bool nameIsUnix = driverName?.Contains ("unix", StringComparison.OrdinalIgnoreCase) ?? false;
        bool nameIsFake = driverName?.Contains ("fake", StringComparison.OrdinalIgnoreCase) ?? false;

        // Decide which driver to use - component factory type takes priority
        if (factoryIsFake || (!factoryIsWindows && !factoryIsDotNet && !factoryIsUnix && nameIsFake))
        {
            Coordinator = CreateSubcomponents (() => new FakeComponentFactory ());
            _driverName = "fake";
        }
        else if (factoryIsWindows || (!factoryIsDotNet && !factoryIsUnix && nameIsWindows))
        {
            Coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
            _driverName = "windows";
        }
        else if (factoryIsDotNet || (!factoryIsWindows && !factoryIsUnix && nameIsDotNet))
        {
            Coordinator = CreateSubcomponents (() => new NetComponentFactory ());
            _driverName = "dotnet";
        }
        else if (factoryIsUnix || (!factoryIsWindows && !factoryIsDotNet && nameIsUnix))
        {
            Coordinator = CreateSubcomponents (() => new UnixComponentFactory ());
            _driverName = "unix";
        }
        else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            Coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
            _driverName = "windows";
        }
        else if (p == PlatformID.Unix)
        {
            Coordinator = CreateSubcomponents (() => new UnixComponentFactory ());
            _driverName = "unix";
        }
        else
        {
            Logging.Information($"Falling back to dotnet driver.");
            Coordinator = CreateSubcomponents (() => new NetComponentFactory ());
            _driverName = "dotnet";
        }

        Logging.Trace ($"Created Subcomponents: {Coordinator}");

        Coordinator.StartInputTaskAsync (this).Wait ();

        if (Driver == null)
        {
            throw new ("Driver was null even after booting MainLoopCoordinator");
        }
    }

    private readonly IComponentFactory? _componentFactory;

    /// <summary>
    ///     INTERNAL: Gets or sets the main loop coordinator that orchestrates the application's event processing,
    ///     input handling, and rendering pipeline.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="IMainLoopCoordinator"/> is the central component responsible for:
    ///         <list type="bullet">
    ///             <item>Managing the platform-specific input thread that reads from the console</item>
    ///             <item>Coordinating the main application loop via <see cref="IMainLoopCoordinator.RunIteration"/></item>
    ///             <item>Processing queued input events and translating them to Terminal.Gui events</item>
    ///             <item>Managing the <see cref="ApplicationMainLoop{TInputRecord}"/> that handles rendering</item>
    ///             <item>Executing scheduled timeouts and callbacks via <see cref="ITimedEvents"/></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The coordinator is created in <see cref="CreateDriver"/> based on the selected driver
    ///         (Windows, Unix, .NET, or Fake) and is started by calling
    ///         <see cref="IMainLoopCoordinator.StartInputTaskAsync"/>.
    ///     </para>
    /// </remarks>
    internal IMainLoopCoordinator? Coordinator { get; private set; }

    /// <summary>
    ///     INTERNAL: Creates a <see cref="MainLoopCoordinator{TInputRecord}"/> with the appropriate component factory
    ///     for the specified input record type.
    /// </summary>
    /// <typeparam name="TInputRecord">
    ///     Platform-specific input type: <see cref="ConsoleKeyInfo"/> (.NET/Fake),
    ///     <see cref="WindowsConsole.InputRecord"/> (Windows), or <see cref="char"/> (Unix).
    /// </typeparam>
    /// <param name="fallbackFactory">
    ///     Factory function to create the component factory if <see cref="_componentFactory"/>
    ///     is not of type <see cref="IComponentFactory{TInputRecord}"/>.
    /// </param>
    /// <returns>
    ///     A <see cref="MainLoopCoordinator{TInputRecord}"/> configured with the input queue,
    ///     main loop, timed events, and selected component factory.
    /// </returns>
    private IMainLoopCoordinator CreateSubcomponents<TInputRecord> (Func<IComponentFactory<TInputRecord>> fallbackFactory) where TInputRecord : struct
    {
        ConcurrentQueue<TInputRecord> inputQueue = new ();
        ApplicationMainLoop<TInputRecord> loop = new ();

        IComponentFactory<TInputRecord> cf;

        if (_componentFactory is IComponentFactory<TInputRecord> typedFactory)
        {
            cf = typedFactory;
        }
        else
        {
            cf = fallbackFactory ();
        }

        return new MainLoopCoordinator<TInputRecord> (_timedEvents, inputQueue, loop, cf);
    }

    internal void SubscribeDriverEvents ()
    {
        ArgumentNullException.ThrowIfNull (Driver);

        Driver.SizeChanged += Driver_SizeChanged;
        Driver.KeyDown += Driver_KeyDown;
        Driver.KeyUp += Driver_KeyUp;
        Driver.MouseEvent += Driver_MouseEvent;
    }

    internal void UnsubscribeDriverEvents ()
    {
        ArgumentNullException.ThrowIfNull (Driver);

        Driver.SizeChanged -= Driver_SizeChanged;
        Driver.KeyDown -= Driver_KeyDown;
        Driver.KeyUp -= Driver_KeyUp;
        Driver.MouseEvent -= Driver_MouseEvent;
    }

    private void Driver_KeyDown (object? sender, Key e) { Keyboard?.RaiseKeyDownEvent (e); }

    private void Driver_KeyUp (object? sender, Key e) { Keyboard?.RaiseKeyUpEvent (e); }

    private void Driver_MouseEvent (object? sender, MouseEventArgs e) { Mouse?.RaiseMouseEvent (e); }
}
