using System.Diagnostics;
using System.Drawing;
using System.Text;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace TerminalGuiFluentTesting;

/// <summary>
///     Fluent API context for testing a Terminal.Gui application. Create
///     an instance using <see cref="With"/> static class.
/// </summary>
public partial class GuiTestContext : IDisposable
{

    // ===== Threading & Synchronization =====
    private readonly CancellationTokenSource _runCancellationTokenSource = new ();
    private readonly CancellationTokenSource? _timeoutCts;
    private readonly Task? _runTask;
    private readonly SemaphoreSlim _booting;
    private readonly object _cancellationLock = new ();
    private volatile bool _finished;

    // ===== Exception Handling =====
    private readonly object _backgroundExceptionLock = new ();
    private Exception? _backgroundException;

    // ===== Driver & Application State =====
    private readonly FakeInput _fakeInput = new ();
    private IOutput? _output;
    private SizeMonitorImpl? _sizeMonitor;
    private ApplicationImpl? _applicationImpl;

    /// <summary>
    ///     The IApplication instance that was created.
    /// </summary>
    public IApplication App => _applicationImpl!;

    private TestDriver _driverType;

    // ===== Application State Preservation (for restoration) =====
    private IApplication? _originalApplicationInstance;
    private ILogger? _originalLogger;

    // ===== Test Configuration =====
    private readonly bool _runApplication;
    private TimeSpan _timeout;

    // ===== Logging =====
    private readonly object _logsLock = new ();
    private readonly TextWriter? _logWriter;
    private StringBuilder? _logsSb;


    /// <summary>
    ///     Constructor for tests that only need Application.Init without running the main loop.
    ///     Uses the driver's default screen size instead of forcing a specific size.
    /// </summary>
    public GuiTestContext (TestDriver driver, TextWriter? logWriter = null, TimeSpan? timeout = null)
    {
        _logWriter = logWriter;
        _runApplication = false;
        _booting = new (0, 1);
        _timeoutCts = new CancellationTokenSource (timeout ?? TimeSpan.FromSeconds (10)); // NEW

        // Don't force a size - let the driver determine it
        CommonInit (0, 0, driver, timeout);

        try
        {
            InitializeApplication ();
            _booting.Release ();

            // After Init, Application.Screen should be set by the driver
            if (_applicationImpl?.Screen == Rectangle.Empty)
            {
                throw new InvalidOperationException (
                                                     "Driver bug: Application.Screen is empty after Init. The driver should set the screen size during Init.");
            }
        }
        catch (Exception ex)
        {
            lock (_backgroundExceptionLock) // NEW: Thread-safe exception handling
            {
                _backgroundException = ex;
            }

            if (_logWriter != null)
            {
                WriteOutLogs (_logWriter);
            }

            throw new ("Application initialization failed", ex);
        }

        lock (_backgroundExceptionLock) // NEW: Thread-safe check
        {
            if (_backgroundException != null)
            {
                throw new ("Application initialization failed", _backgroundException);
            }
        }
    }

    /// <summary>
    ///     Constructor for tests that need to run the application with Application.Run.
    /// </summary>
    internal GuiTestContext (Func<Toplevel> topLevelBuilder, int width, int height, TestDriver driver, TextWriter? logWriter = null, TimeSpan? timeout = null)
    {
        _logWriter = logWriter;
        _runApplication = true;
        _booting = new (0, 1);

        CommonInit (width, height, driver, timeout);

        // Start the application in a background thread
        _runTask = Task.Run (
                             () =>
                             {
                                 try
                                 {
                                     InitializeApplication ();

                                     _booting.Release ();

                                     Toplevel t = topLevelBuilder ();
                                     t.Closed += (s, e) => { Finished = true; };
                                     _applicationImpl?.Run (t); // This will block, but it's on a background thread now

                                     t.Dispose ();
                                     Logging.Trace ("Application.Run completed");
                                     _applicationImpl?.Shutdown ();
                                     _runCancellationTokenSource.Cancel ();
                                 }
                                 catch (OperationCanceledException)
                                 { }
                                 catch (Exception ex)
                                 {
                                     _backgroundException = ex;
                                     _fakeInput.ExternalCancellationTokenSource!.Cancel ();
                                 }
                                 finally
                                 {
                                     CleanupApplication ();

                                     if (_logWriter != null)
                                     {
                                         WriteOutLogs (_logWriter);
                                     }
                                 }
                             },
                             _runCancellationTokenSource.Token);

        // Wait for booting to complete with a timeout to avoid hangs
        if (!_booting.WaitAsync (_timeout).Result)
        {
            throw new TimeoutException ($"Application failed to start within {_timeout}ms.");
        }

        ResizeConsole (width, height);

        if (_backgroundException is { })
        {
            throw new ("Application crashed", _backgroundException);
        }
    }

    private void InitializeApplication ()
    {
        ApplicationImpl.ChangeInstance (_applicationImpl);

        _applicationImpl?.Init (GetDriverName ());
    }


    /// <summary>
    ///     Common initialization for both constructors.
    /// </summary>
    private void CommonInit (int width, int height, TestDriver driverType, TimeSpan? timeout)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds (10);
        _originalApplicationInstance = ApplicationImpl.Instance;
        _originalLogger = Logging.Logger;
        _logsSb = new ();
        _driverType = driverType;

        ILogger logger = LoggerFactory.Create (builder =>
                                                   builder.SetMinimumLevel (LogLevel.Trace)
                                                          .AddProvider (
                                                                        new TextWriterLoggerProvider (
                                                                                                      new ThreadSafeStringWriter (_logsSb, _logsLock))))
                                      .CreateLogger ("Test Logging");
        Logging.Logger = logger;

        // ✅ Link _runCancellationTokenSource with a timeout
        // This creates a token that responds to EITHER the run cancellation OR timeout
        _fakeInput.ExternalCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource (
                                                             _runCancellationTokenSource.Token,
                                                             new CancellationTokenSource (_timeout).Token);

        // Now when InputImpl.Run receives this ExternalCancellationTokenSource,
        // it will create ANOTHER linked token internally that combines:
        // - Its own runCancellationToken parameter
        // - The ExternalCancellationTokenSource (which is already linked)
        // This creates a chain: any of these triggers will stop input:
        // 1. _runCancellationTokenSource.Cancel() (normal stop)
        // 2. Timeout expires (test timeout)
        // 3. Direct cancel of ExternalCancellationTokenSource (hard stop/error)

        // Remove frame limit
        Application.MaximumIterationsPerSecond = ushort.MaxValue;

        IComponentFactory? cf = null;

        _output = new FakeOutput ();

        // Only set size if explicitly provided (width and height > 0)
        if (width > 0 && height > 0)
        {
           _output.SetSize (width, height);
        }

        // TODO: As each drivers' IInput/IOutput implementations are made testable (e.g. 
        // TODO: safely injectable/mocked), we can expand this switch to use them.
        switch (driverType)
        {
            case TestDriver.DotNet:
                _sizeMonitor = new (_output);
                cf = new FakeComponentFactory (_fakeInput, _output, _sizeMonitor);

                break;
            case TestDriver.Windows:
                _sizeMonitor = new (_output);
                cf = new FakeComponentFactory (_fakeInput, _output, _sizeMonitor);

                break;
            case TestDriver.Unix:
                _sizeMonitor = new (_output);
                cf = new FakeComponentFactory (_fakeInput, _output, _sizeMonitor);

                break;
            case TestDriver.Fake:
                _sizeMonitor = new (_output);
                cf = new FakeComponentFactory (_fakeInput, _output, _sizeMonitor);

                break;
        }

        _applicationImpl = new (cf!);
        Logging.Trace ($"Driver: {GetDriverName ()}. Timeout: {_timeout}");
    }

    private string GetDriverName ()
    {
        return _driverType switch
        {
            TestDriver.Windows => "windows",
            TestDriver.DotNet => "dotnet",
            TestDriver.Unix => "unix",
            TestDriver.Fake => "fake",
            _ =>
                throw new ArgumentOutOfRangeException ()
        };
    }

    /// <summary>
    ///     Gets whether the application has finished running; aka Stop has been called and the main loop has exited.
    /// </summary>
    public bool Finished
    {
        get => _finished;
        private set => _finished = value;
    }


    /// <summary>
    ///     Performs the supplied <paramref name="doAction"/> immediately.
    ///     Enables running commands without breaking the Fluent API calls.
    /// </summary>
    /// <param name="doAction"></param>
    /// <returns></returns>
    public GuiTestContext Then (Action<IApplication> doAction)
    {
        try
        {
            Logging.Trace ($"Invoking action via WaitIteration");
            WaitIteration (doAction);
        }
        catch (Exception ex)
        {
            _backgroundException = ex;
            HardStop ();

            throw;
        }

        return this;
    }

    /// <summary>
    ///     Waits until the end of the current iteration of the main loop. Optionally
    ///     running a given <paramref name="action"/> action on the UI thread at that time.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public GuiTestContext WaitIteration (Action<IApplication>? action = null)
    {
        // If application has already exited don't wait!
        if (Finished || _runCancellationTokenSource.Token.IsCancellationRequested || _fakeInput.ExternalCancellationTokenSource!.Token.IsCancellationRequested)
        {
            Logging.Warning ("WaitIteration called after context was stopped");

            return this;
        }

        if (Thread.CurrentThread.ManagedThreadId == _applicationImpl?.MainThreadId)
        {
            throw new NotSupportedException ("Cannot WaitIteration during Invoke");
        }

        Logging.Trace ($"WaitIteration started");
        if (action is null)
        {
            action = (app) => { };
        }
        CancellationTokenSource ctsActionCompleted = new ();

        _applicationImpl?.Invoke (app =>
                                 {
                                     try
                                     {
                                         action (app);

                                         //Logging.Trace ("Action completed");
                                         ctsActionCompleted.Cancel ();
                                     }
                                     catch (Exception e)
                                     {
                                         Logging.Warning ($"Action failed with exception: {e}");
                                         _backgroundException = e;
                                         _fakeInput.ExternalCancellationTokenSource?.Cancel ();
                                     }
                                 });

        // Blocks until either the token or the hardStopToken is cancelled.
        // With linked tokens, we only need to wait on _runCancellationTokenSource and ctsLocal
        // ExternalCancellationTokenSource is redundant because it's linked to _runCancellationTokenSource
        WaitHandle.WaitAny (
                            [
                                _runCancellationTokenSource.Token.WaitHandle,
                                ctsActionCompleted.Token.WaitHandle
                            ]);

        // Logging.Trace ($"Return from WaitIteration");
        return this;
    }

    public GuiTestContext WaitUntil (Func<bool> condition)
    {
        GuiTestContext? c = null;
        var sw = Stopwatch.StartNew ();

        //Logging.Trace ($"WaitUntil started with timeout {_timeout}");

        while (!condition ())
        {
            if (sw.Elapsed > _timeout)
            {
                throw new TimeoutException ($"Failed to reach condition within {_timeout}ms");
            }

            c = WaitIteration ();
        }

        return c ?? this;
    }


    /// <summary>
    ///     Returns the last set position of the cursor.
    /// </summary>
    /// <returns></returns>
    public Point GetCursorPosition () { return _output!.GetCursorPosition (); }

    /// <summary>
    ///     Simulates changing the console size e.g. by resizing window in your operating system
    /// </summary>
    /// <param name="width">new Width for the console.</param>
    /// <param name="height">new Height for the console.</param>
    /// <returns></returns>
    public GuiTestContext ResizeConsole (int width, int height) { return WaitIteration ((app) => { app.Driver!.SetScreenSize (width, height); }); }

    public GuiTestContext ScreenShot (string title, TextWriter? writer)
    {
        //Logging.Trace ($"{title}");
        return WaitIteration ((app) =>
                              {
                                  writer?.WriteLine (title + ":");
                                  var text = app.Driver?.ToString ();

                                  writer?.WriteLine (text);
                              });
    }

    public GuiTestContext AnsiScreenShot (string title, TextWriter? writer)
    {
        //Logging.Trace ($"{title}");
        return WaitIteration ((app) =>
                              {
                                  writer?.WriteLine (title + ":");
                                  var text = app.Driver?.ToAnsi ();

                                  writer?.WriteLine (text);
                              });
    }

    /// <summary>
    ///     Stops the application and waits for the background thread to exit.
    /// </summary>
    public GuiTestContext Stop ()
    {
        Logging.Trace ($"Stopping application for driver: {GetDriverName ()}");

        if (_runTask is null || _runTask.IsCompleted)
        {
            // If we didn't run the application, just cleanup
            if (!_runApplication && !Finished)
            {
                try
                {
                    _applicationImpl?.Shutdown ();
                }
                catch
                {
                    // Ignore errors during shutdown
                }

                CleanupApplication ();
            }

            return this;
        }

        WaitIteration ((app) => { app.RequestStop (); });

        // Wait for the application to stop, but give it a 1-second timeout
        const int WAIT_TIMEOUT_MS = 1000;

        if (!_runTask.Wait (TimeSpan.FromMilliseconds (WAIT_TIMEOUT_MS)))
        {
            _runCancellationTokenSource.Cancel ();

            // No need to manually cancel ExternalCancellationTokenSource

            // App is having trouble shutting down, try sending some more shutdown stuff from this thread.
            // If this doesn't work there will be test failures as the main loop continues to run during next test.
            try
            {
                _applicationImpl?.RequestStop ();
                _applicationImpl?.Shutdown ();
            }
            catch (Exception ex)
            {
                Logging.Critical ($"Application failed to stop in {WAIT_TIMEOUT_MS}. Then shutdown threw {ex}");
            }
            finally
            {
                Logging.Critical ($"Application failed to stop in {WAIT_TIMEOUT_MS}. Exception was thrown: {_backgroundException}");
            }
        }

        _runCancellationTokenSource.Cancel ();

        if (_backgroundException != null)
        {
            Logging.Critical ($"Exception occurred: {_backgroundException}");
            //throw _ex; // Propagate any exception that happened in the background task
        }

        return this;
    }

    /// <summary>
    ///     Hard stops the application and waits for the background thread to exit.HardStop is used by the source generator for
    ///     wrapping Xunit assertions.
    /// </summary>
    public void HardStop (Exception? ex = null)
    {
        if (ex != null)
        {
            _backgroundException = ex;
        }

        Logging.Critical ($"HardStop called with exception: {_backgroundException}");

        // With linked tokens, just cancelling ExternalCancellationTokenSource
        // will cascade to stop everything
        _fakeInput.ExternalCancellationTokenSource?.Cancel ();
        WriteOutLogs (_logWriter);
        Stop ();
    }

    /// <summary>
    ///     Writes all Terminal.Gui engine logs collected so far to the <paramref name="writer"/>
    /// </summary>
    /// <param name="writer"></param>
    /// <returns></returns>
    public GuiTestContext WriteOutLogs (TextWriter? writer)
    {
        if (writer is null)
        {
            return this;
        }

        lock (_logsLock)
        {
            writer.WriteLine (_logsSb!.ToString ());
        }

        return this; //WaitIteration();
    }

    internal void Fail (string reason)
    {
        Logging.Error ($"{reason}");

        throw new (reason);
    }

    private void CleanupApplication ()
    {
        Logging.Trace ("CleanupApplication");
        _fakeInput.ExternalCancellationTokenSource = null;

        _applicationImpl?.ResetState (true);
        ApplicationImpl.ChangeInstance (_originalApplicationInstance);
        Logging.Logger = _originalLogger!;
        Finished = true;

        Application.MaximumIterationsPerSecond = Application.DefaultMaximumIterationsPerSecond;
    }


    /// <summary>
    ///     Cleanup to avoid state bleed between tests
    /// </summary>
    public void Dispose ()
    {
        Logging.Trace ($"Disposing GuiTestContext");
        Stop ();

        bool shouldThrow = false;
        Exception? exToThrow = null;

        lock (_cancellationLock) // NEW: Thread-safe check
        {
            if (_fakeInput.ExternalCancellationTokenSource is { IsCancellationRequested: true })
            {
                shouldThrow = true;
                lock (_backgroundExceptionLock)
                {
                    exToThrow = _backgroundException;
                }
            }

            // ✅ Dispose the linked token source
            _fakeInput.ExternalCancellationTokenSource?.Dispose ();
        }

        _timeoutCts?.Dispose (); // NEW: Dispose timeout CTS
        _runCancellationTokenSource?.Dispose ();
        _fakeInput.Dispose ();
        _output?.Dispose ();
        _booting.Dispose ();

        if (shouldThrow)
        {
            throw new ("Application was hard stopped...", exToThrow);
        }
    }

}
