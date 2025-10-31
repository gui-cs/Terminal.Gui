using System.Collections.Concurrent;
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
    private readonly CancellationTokenSource _cts = new ();
    private CancellationTokenSource? _hardStop;
    private readonly Task? _runTask;
    internal Exception? _ex;
    internal readonly FakeOutput _output = new ();
    internal FakeWindowsInput? _winInput;
    internal FakeNetInput? _netInput;
    internal FakeInput<ConsoleKeyInfo>? _fakeInput;
    //internal FakeUnixInput? _unixInput;
    internal View? _lastView;
    private readonly object _logsLock = new ();
    private StringBuilder? _logsSb;
    internal TestDriver _driver;
    internal bool _finished;
    private ConsoleSizeMonitor? _sizeMonitor;
    internal TimeSpan _timeout;
    private IApplication? _origApp;
    private ILogger? _origLogger;
    private ApplicationImpl? _impl;
    private readonly SemaphoreSlim _booting;
    private readonly bool _runApplication;
    private readonly TextWriter? _logWriter;

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
                                     t.Closed += (s, e) => { _finished = true; };
                                     Application.Run (t); // This will block, but it's on a background thread now

                                     t.Dispose ();
                                     Application.Shutdown ();
                                     _cts.Cancel ();
                                 }
                                 catch (OperationCanceledException)
                                 { }
                                 catch (Exception ex)
                                 {
                                     _ex = ex;

                                     if (_logWriter != null)
                                     {
                                         WriteOutLogs (_logWriter);
                                     }

                                     _hardStop.Cancel ();
                                 }
                                 finally
                                 {
                                     CleanupApplication ();
                                 }
                             },
                             _cts.Token);

        // Wait for booting to complete with a timeout to avoid hangs
        if (!_booting.WaitAsync (_timeout).Result)
        {
            throw new TimeoutException ("Application failed to start within the allotted time.");
        }

        ResizeConsole (width, height);

        if (_ex != null)
        {
            throw new ("Application crashed", _ex);
        }
    }

    /// <summary>
    ///     Constructor for tests that only need Application.Init without running the main loop.
    ///     Uses the driver's default screen size instead of forcing a specific size.
    /// </summary>
    public GuiTestContext (TestDriver driver, TextWriter? logWriter = null, TimeSpan? timeout = null)
    {
        _logWriter = logWriter;
        _runApplication = false;
        _booting = new (0, 1);

        // Don't force a size - let the driver determine it
        CommonInit (0, 0, driver, timeout);

        try
        {
            InitializeApplication ();
            _booting.Release ();

            // After Init, Application.Screen should be set by the driver
            if (Application.Screen == Rectangle.Empty)
            {
                throw new InvalidOperationException (
                                                     "Driver bug: Application.Screen is empty after Init. The driver should set the screen size during Init.");
            }
        }
        catch (Exception ex)
        {
            _ex = ex;

            if (_logWriter != null)
            {
                WriteOutLogs (_logWriter);
            }

            throw new ("Application initialization failed", ex);
        }

        if (_ex != null)
        {
            throw new ("Application initialization failed", _ex);
        }
    }

    /// <summary>
    ///     Common initialization for both constructors.
    /// </summary>
    private void CommonInit (int width, int height, TestDriver driver, TimeSpan? timeout)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds (30);
        _hardStop = new (_timeout);

        // Remove frame limit
        Application.MaximumIterationsPerSecond = ushort.MaxValue;

        _origApp = ApplicationImpl.Instance;
        _origLogger = Logging.Logger;
        _logsSb = new ();
        _driver = driver;

        _netInput = new (_cts.Token);
        _winInput = new (_cts.Token);
        _fakeInput = new (_cts.Token);

        // Only set size if explicitly provided (width and height > 0)
        if (width > 0 && height > 0)
        {
            _output.SetSize (width, height);
        }

        _sizeMonitor = new (_output, _output.LastBuffer!);
        IComponentFactory? cf = null;

        switch (driver)
        {
            case TestDriver.DotNet:
                cf = new FakeNetComponentFactory (_netInput, _output, _sizeMonitor);

                break;
            case TestDriver.Windows:
                cf = new FakeWindowsComponentFactory (_winInput, _output, _sizeMonitor);

                break;
            case TestDriver.Unix:
                cf = new UnixComponentFactory ();

                break;
            case TestDriver.Fake:
                cf = new FakeComponentFactory (_fakeInput, _output);

                break;
        }

        _impl = new (cf!);
    }

    private void InitializeApplication ()
    {
        ApplicationImpl.ChangeInstance (_impl);

        ILogger logger = LoggerFactory.Create (builder =>
                                                   builder.SetMinimumLevel (LogLevel.Trace)
                                                          .AddProvider (
                                                                        new TextWriterLoggerProvider (
                                                                                                      new ThreadSafeStringWriter (_logsSb, _logsLock))))
                                      .CreateLogger ("Test Logging");
        Logging.Logger = logger;

        _impl.Init (null, GetDriverName ());
    }

    private void CleanupApplication ()
    {
        ApplicationImpl.ChangeInstance (_origApp);
        Logging.Logger = _origLogger;
        _finished = true;

        Application.MaximumIterationsPerSecond = Application.DefaultMaximumIterationsPerSecond;
    }

    private string GetDriverName ()
    {
        return _driver switch
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
    ///     Stops the application and waits for the background thread to exit.
    /// </summary>
    public GuiTestContext Stop ()
    {
        if (_runTask is null || _runTask.IsCompleted)
        {
            // If we didn't run the application, just cleanup
            if (!_runApplication && !_finished)
            {
                try
                {
                    Application.Shutdown ();
                }
                catch
                {
                    // Ignore errors during shutdown
                }

                CleanupApplication ();
            }

            return this;
        }

        WaitIteration (() => { Application.RequestStop (); });

        // Wait for the application to stop, but give it a 1-second timeout
        if (!_runTask.Wait (TimeSpan.FromMilliseconds (1000)))
        {
            _cts.Cancel ();

            // Timeout occurred, force the task to stop
            _hardStop.Cancel ();

            // App is having trouble shutting down, try sending some more shutdown stuff from this thread.
            // If this doesn't work there will be test cascade failures as the main loop continues to run during next test.
            try
            {
                Application.RequestStop ();
                Application.Shutdown ();
            }
            catch (Exception)
            {
                throw new TimeoutException ("Application failed to stop within the allotted time.", _ex);
            }

            throw new TimeoutException ("Application failed to stop within the allotted time.", _ex);
        }

        _cts.Cancel ();

        if (_ex != null)
        {
            throw _ex; // Propagate any exception that happened in the background task
        }

        return this;
    }

    /// <summary>
    ///     Hard stops the application and waits for the background thread to exit.
    /// </summary>
    public void HardStop (Exception? ex = null)
    {
        if (ex != null)
        {
            _ex = ex;
        }

        _hardStop.Cancel ();
        Stop ();
    }

    /// <summary>
    ///     Cleanup to avoid state bleed between tests
    /// </summary>
    public void Dispose ()
    {
        Stop ();

        if (_hardStop.IsCancellationRequested)
        {
            throw new (
                       "Application was hard stopped, typically this means it timed out or did not shutdown gracefully. Ensure you call Stop in your test",
                       _ex);
        }

        _hardStop.Cancel ();
        _booting.Dispose ();
    }

    /// <summary>
    ///     Writes all Terminal.Gui engine logs collected so far to the <paramref name="writer"/>
    /// </summary>
    /// <param name="writer"></param>
    /// <returns></returns>
    public GuiTestContext WriteOutLogs (TextWriter writer)
    {
        lock (_logsLock)
        {
            writer.WriteLine (_logsSb.ToString ());
        }

        return this; //WaitIteration();
    }

    /// <summary>
    ///     Waits until the end of the current iteration of the main loop. Optionally
    ///     running a given <paramref name="a"/> action on the UI thread at that time.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public GuiTestContext WaitIteration (Action? a = null)
    {
        // If application has already exited don't wait!
        if (_finished || _cts.Token.IsCancellationRequested || _hardStop.Token.IsCancellationRequested)
        {
            return this;
        }

        if (Thread.CurrentThread.ManagedThreadId == Application.MainThreadId)
        {
            throw new NotSupportedException ("Cannot WaitIteration during Invoke");
        }

        a ??= () => { };
        var ctsLocal = new CancellationTokenSource ();

        Application.Invoke (() =>
                            {
                                try
                                {
                                    a ();
                                    ctsLocal.Cancel ();
                                }
                                catch (Exception e)
                                {
                                    _ex = e;
                                    _hardStop.Cancel ();
                                }
                            });

        // Blocks until either the token or the hardStopToken is cancelled.
        WaitHandle.WaitAny (
                            new []
                            {
                                _cts.Token.WaitHandle,
                                _hardStop.Token.WaitHandle,
                                ctsLocal.Token.WaitHandle
                            });

        return this;
    }

    /// <summary>
    ///     Performs the supplied <paramref name="doAction"/> immediately.
    ///     Enables running commands without breaking the Fluent API calls.
    /// </summary>
    /// <param name="doAction"></param>
    /// <returns></returns>
    public GuiTestContext Then (Action doAction)
    {
        try
        {
            WaitIteration (doAction);
        }
        catch (Exception ex)
        {
            _ex = ex;
            HardStop ();

            throw;
        }

        return this;
    }

    internal GuiTestContext WaitUntil (Func<bool> condition)
    {
        GuiTestContext? c = null;
        var sw = Stopwatch.StartNew ();

        while (!condition ())
        {
            if (sw.Elapsed > _timeout)
            {
                throw new TimeoutException ("Failed to reach condition within the time limit");
            }

            c = WaitIteration ();
        }

        return c ?? this;
    }

    internal void Fail (string reason)
    {
        Stop ();

        throw new (reason);
    }

    /// <summary>
    ///     Returns the last set position of the cursor.
    /// </summary>
    /// <returns></returns>
    public Point GetCursorPosition () { return _output.CursorPosition; }

    /// <summary>
    ///     Simulates changing the console size e.g. by resizing window in your operating system
    /// </summary>
    /// <param name="width">new Width for the console.</param>
    /// <param name="height">new Height for the console.</param>
    /// <returns></returns>
    public GuiTestContext ResizeConsole (int width, int height) { return WaitIteration (() => { Application.Driver!.SetScreenSize (width, height); }); }

    public GuiTestContext ScreenShot (string title, TextWriter writer)
    {
        return WaitIteration (() =>
                              {
                                  writer.WriteLine (title + ":");
                                  var text = Application.ToString ();

                                  writer.WriteLine (text);
                              });
    }
}
