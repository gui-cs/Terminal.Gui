#nullable enable
using System.Collections.Concurrent;
using Moq;
using TerminalGuiFluentTesting;

namespace UnitTests.ApplicationTests;

public class ApplicationImplTests
{
    public ApplicationImplTests () { ConsoleDriver.RunningUnitTests = true; }

    private ApplicationImpl NewApplicationImpl (TestDriver driver = TestDriver.DotNet)
    {
        if (driver == TestDriver.DotNet)
        {
            Mock<INetInput> netInput = new ();
            SetupRunInputMockMethodToBlock (netInput);

            Mock<IComponentFactory<ConsoleKeyInfo>> m = new ();
            m.Setup (f => f.CreateInput ()).Returns (netInput.Object);
            m.Setup (f => f.CreateInputProcessor (It.IsAny<ConcurrentQueue<ConsoleKeyInfo>> ())).Returns (Mock.Of<IInputProcessor> ());

            Mock<IConsoleOutput> consoleOutput = new ();
            var size = new Size (80, 25);
            consoleOutput.Setup (o => o.SetSize (It.IsAny<int> (), It.IsAny<int> ()))
                         .Callback<int, int> ((w, h) => size = new Size (w, h));
            consoleOutput.Setup (o => o.GetSize ()).Returns (() => size);
            m.Setup (f => f.CreateOutput ()).Returns (consoleOutput.Object);
            m.Setup (f => f.CreateConsoleSizeMonitor (It.IsAny<IConsoleOutput> (), It.IsAny<IOutputBuffer> ())).Returns (Mock.Of<IConsoleSizeMonitor> ());

            return new (m.Object);
        }
        else
        {
            Mock<IConsoleInput<WindowsConsole.InputRecord>> winInput = new ();
            SetupRunInputMockMethodToBlock (winInput);
            Mock<IComponentFactory<WindowsConsole.InputRecord>> m = new ();
            m.Setup (f => f.CreateInput ()).Returns (winInput.Object);
            m.Setup (f => f.CreateInputProcessor (It.IsAny<ConcurrentQueue<WindowsConsole.InputRecord>> ())).Returns (Mock.Of<IInputProcessor> ());
            m.Setup (f => f.CreateOutput ()).Returns (Mock.Of<IConsoleOutput> ());
            m.Setup (f => f.CreateConsoleSizeMonitor (It.IsAny<IConsoleOutput> (), It.IsAny<IOutputBuffer> ())).Returns (Mock.Of<IConsoleSizeMonitor> ());

            return new (m.Object);
        }
    }

    [Fact]
    public void Init_CreatesKeybindings ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        Application.KeyBindings.Clear ();

        Assert.Empty (Application.KeyBindings.GetBindings ());

        v2.Init (null, "fake");

        Assert.NotEmpty (Application.KeyBindings.GetBindings ());

        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void Init_DriverIsFacade ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Driver);
        v2.Init (null, "fake");
        Assert.NotNull (Application.Driver);

        Type type = Application.Driver.GetType ();
        Assert.True (type.IsGenericType);
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);

        ApplicationImpl.ChangeInstance (orig);
    }

    /*
    [Fact]
    public void Init_ExplicitlyRequestWin ()
    {
        var orig = ApplicationImpl.Instance;

        Assert.Null (Application.Driver);
        var netInput = new Mock<INetInput> (MockBehavior.Strict);
        var netOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);
        var winInput = new Mock<IWindowsInput> (MockBehavior.Strict);
        var winOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);

        winInput.Setup (i => i.Initialize (It.IsAny<ConcurrentQueue<WindowsConsole.InputRecord>> ()))
                .Verifiable (Times.Once);
        SetupRunInputMockMethodToBlock (winInput);
        winInput.Setup (i => i.Dispose ())
                .Verifiable (Times.Once);
        winOutput.Setup (i => i.Dispose ())
                 .Verifiable (Times.Once);

        var v2 = new ApplicationV2 (
                                    () => netInput.Object,
                                    () => netOutput.Object,
                                    () => winInput.Object,
                                    () => winOutput.Object);
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Driver);
        v2.Init (null, "v2win");
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True (type.IsGenericType);
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);

        winInput.VerifyAll ();

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void Init_ExplicitlyRequestNet ()
    {
        var orig = ApplicationImpl.Instance;

        var netInput = new Mock<INetInput> (MockBehavior.Strict);
        var netOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);
        var winInput = new Mock<IWindowsInput> (MockBehavior.Strict);
        var winOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);

        netInput.Setup (i => i.Initialize (It.IsAny<ConcurrentQueue<ConsoleKeyInfo>> ()))
                .Verifiable (Times.Once);
        SetupRunInputMockMethodToBlock (netInput);
        netInput.Setup (i => i.Dispose ())
                .Verifiable (Times.Once);
        netOutput.Setup (i => i.Dispose ())
                 .Verifiable (Times.Once);
        var v2 = new ApplicationV2 (
                                    () => netInput.Object,
                                    () => netOutput.Object,
                                    () => winInput.Object,
                                    () => winOutput.Object);
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Driver);
        v2.Init (null, "v2net");
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True (type.IsGenericType);
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);

        netInput.VerifyAll ();

        ApplicationImpl.ChangeInstance (orig);
    }
*/
    private void SetupRunInputMockMethodToBlock (Mock<IConsoleInput<WindowsConsole.InputRecord>> winInput)
    {
        winInput.Setup (r => r.Run (It.IsAny<CancellationToken> ()))
                .Callback<CancellationToken> (token =>
                                              {
                                                  // Simulate an infinite loop that checks for cancellation
                                                  while (!token.IsCancellationRequested)
                                                  {
                                                      // Perform the action that should repeat in the loop
                                                      // This could be some mock behavior or just an empty loop depending on the context
                                                  }
                                              })
                .Verifiable (Times.Once);
    }

    private void SetupRunInputMockMethodToBlock (Mock<INetInput> netInput)
    {
        netInput.Setup (r => r.Run (It.IsAny<CancellationToken> ()))
                .Callback<CancellationToken> (token =>
                                              {
                                                  // Simulate an infinite loop that checks for cancellation
                                                  while (!token.IsCancellationRequested)
                                                  {
                                                      // Perform the action that should repeat in the loop
                                                      // This could be some mock behavior or just an empty loop depending on the context
                                                  }
                                              })
                .Verifiable (Times.Once);
    }

    [Fact]
    public void NoInitThrowOnRun ()
    {
        IApplication orig = ApplicationImpl.Instance;

        Assert.Null (Application.Driver);
        ApplicationImpl app = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (app);

        var ex = Assert.Throws<NotInitializedException> (() => app.Run (new Window ()));
        Assert.Equal ("Run cannot be accessed before Initialization", ex.Message);
        app.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void InitRunShutdown_Top_Set_To_Null_After_Shutdown ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init (null, "fake");

        object timeoutToken = v2.AddTimeout (
                                             TimeSpan.FromMilliseconds (150),
                                             () =>
                                             {
                                                 if (Application.Top != null)
                                                 {
                                                     Application.RequestStop ();

                                                     return false;
                                                 }

                                                 return false;
                                             }
                                            );
        Assert.Null (Application.Top);

        // Blocks until the timeout call is hit

        v2.Run (new Window ());

        // We returned false above, so we should not have to remove the timeout
        Assert.False (v2.RemoveTimeout (timeoutToken));

        Assert.NotNull (Application.Top);
        Application.Top?.Dispose ();
        v2.Shutdown ();
        Assert.Null (Application.Top);

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void InitRunShutdown_Running_Set_To_False ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init (null, "fake");

        Toplevel top = new Window
        {
            Title = "InitRunShutdown_Running_Set_To_False"
        };

        object timeoutToken = v2.AddTimeout (
                                             TimeSpan.FromMilliseconds (150),
                                             () =>
                                             {
                                                 Assert.True (top!.Running);

                                                 if (Application.Top != null)
                                                 {
                                                     Application.RequestStop ();

                                                     return false;
                                                 }

                                                 return false;
                                             }
                                            );

        Assert.False (top!.Running);

        // Blocks until the timeout call is hit
        v2.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (v2.RemoveTimeout (timeoutToken));

        Assert.False (top!.Running);

        // BUGBUG: Shutdown sets Top to null, not End.
        //Assert.Null (Application.Top);
        Application.Top?.Dispose ();
        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);
    }


    [Fact]
    public void InitRunShutdown_StopAfterFirstIteration_Stops ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Top);
        Assert.Null (Application.Driver);

        v2.Init (null, "fake");

        Toplevel top = new Window ();

        var closedCount = 0;

        top.Closed
            += (_, a) => { closedCount++; };

        var unloadedCount = 0;

        top.Unloaded
            += (_, a) => { unloadedCount++; };

        object timeoutToken = v2.AddTimeout (
                                             TimeSpan.FromMilliseconds (150),
                                             () =>
                                             {
                                                 Assert.Fail(@"Didn't stop after first iteration.");
                                                 return false;
                                             }
                                            );

        Assert.Equal (0, closedCount);
        Assert.Equal (0, unloadedCount);

        v2.StopAfterFirstIteration = true;
        v2.Run (top);

        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);

        Application.Top?.Dispose ();
        v2.Shutdown ();
        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);

        ApplicationImpl.ChangeInstance (orig);
    }


    [Fact]
    public void InitRunShutdown_End_Is_Called ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Top);
        Assert.Null (Application.Driver);

        v2.Init (null, "fake");

        Toplevel top = new Window ();

        // BUGBUG: Both Closed and Unloaded are called from End; what's the difference?
        var closedCount = 0;

        top.Closed
            += (_, a) => { closedCount++; };

        var unloadedCount = 0;

        top.Unloaded
            += (_, a) => { unloadedCount++; };

        object timeoutToken = v2.AddTimeout (
                                             TimeSpan.FromMilliseconds (150),
                                             () =>
                                             {
                                                 Assert.True (top!.Running);

                                                 if (Application.Top != null)
                                                 {
                                                     Application.RequestStop ();

                                                     return false;
                                                 }

                                                 return false;
                                             }
                                            );

        Assert.Equal (0, closedCount);
        Assert.Equal (0, unloadedCount);

        // Blocks until the timeout call is hit
        v2.Run (top);

        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (v2.RemoveTimeout (timeoutToken));

        Application.Top?.Dispose ();
        v2.Shutdown ();
        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void InitRunShutdown_QuitKey_Quits ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init (null, "fake");

        Toplevel top = new Window
        {
            Title = "InitRunShutdown_QuitKey_Quits"
        };

        object timeoutToken = v2.AddTimeout (
                                             TimeSpan.FromMilliseconds (150),
                                             () =>
                                             {
                                                 Assert.True (top!.Running);

                                                 if (Application.Top != null)
                                                 {
                                                     Application.RaiseKeyDownEvent (Application.QuitKey);
                                                 }

                                                 return false;
                                             }
                                            );

        Assert.False (top!.Running);

        // Blocks until the timeout call is hit
        v2.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (v2.RemoveTimeout (timeoutToken));

        Assert.False (top!.Running);

        Assert.NotNull (Application.Top);
        top.Dispose ();
        v2.Shutdown ();
        Assert.Null (Application.Top);

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void InitRunShutdown_Generic_IdleForExit ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init (null, "fake");

        v2.AddTimeout (TimeSpan.Zero, IdleExit);
        Assert.Null (Application.Top);

        // Blocks until the timeout call is hit

        v2.Run<Window> ();

        Assert.NotNull (Application.Top);
        Application.Top?.Dispose ();
        v2.Shutdown ();
        Assert.Null (Application.Top);

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void Shutdown_Closing_Closed_Raised ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init (null, "fake");

        var closing = 0;
        var closed = 0;
        var t = new Toplevel ();

        t.Closing
            += (_, a) =>
               {
                   // Cancel the first time
                   if (closing == 0)
                   {
                       a.Cancel = true;
                   }

                   closing++;
                   Assert.Same (t, a.RequestingTop);
               };

        t.Closed
            += (_, a) =>
               {
                   closed++;
                   Assert.Same (t, a.Toplevel);
               };

        v2.AddTimeout (TimeSpan.Zero, IdleExit);

        // Blocks until the timeout call is hit

        v2.Run (t);

        Application.Top?.Dispose ();
        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);

        Assert.Equal (2, closing);
        Assert.Equal (1, closed);
    }

    private bool IdleExit ()
    {
        if (Application.Top != null)
        {
            Application.RequestStop ();

            return true;
        }

        return true;
    }
    /*
    [Fact]
    public void Shutdown_Called_Repeatedly_DoNotDuplicateDisposeOutput ()
    {
        var orig = ApplicationImpl.Instance;

        var netInput = new Mock<INetInput> ();
        SetupRunInputMockMethodToBlock (netInput);
        Mock<IConsoleOutput>? outputMock = null;


        var v2 = new ApplicationV2 (
                                   () => netInput.Object,
                                   () => (outputMock = new Mock<IConsoleOutput> ()).Object,
                                   Mock.Of<IWindowsInput>,
                                   Mock.Of<IConsoleOutput>);
        ApplicationImpl.ChangeInstance (v2);

        v2.Init (null, "v2net");


        v2.Shutdown ();
        outputMock!.Verify (o => o.Dispose (), Times.Once);

        ApplicationImpl.ChangeInstance (orig);
    }
    */

    [Fact]
    public void Open_Calls_ContinueWith_On_UIThread ()
    {
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init (null, "fake");
        var b = new Button ();

        var result = false;

        b.Accepting +=
            (_, _) =>
            {
                Task.Run (() => { Task.Delay (300).Wait (); })
                    .ContinueWith (
                                   (t, _) =>
                                   {
                                       // no longer loading
                                       Application.Invoke (() =>
                                                           {
                                                               result = true;
                                                               Application.RequestStop ();
                                                           });
                                   },
                                   TaskScheduler.FromCurrentSynchronizationContext ());
            };

        v2.AddTimeout (
                       TimeSpan.FromMilliseconds (150),
                       () =>
                       {
                           // Run asynchronous logic inside Task.Run
                           if (Application.Top != null)
                           {
                               b.NewKeyDownEvent (Key.Enter);
                               b.NewKeyUpEvent (Key.Enter);
                           }

                           return false;
                       });

        Assert.Null (Application.Top);

        var w = new Window
        {
            Title = "Open_CallsContinueWithOnUIThread"
        };
        w.Add (b);

        // Blocks until the timeout call is hit
        v2.Run (w);

        Assert.NotNull (Application.Top);
        Application.Top?.Dispose ();
        v2.Shutdown ();
        Assert.Null (Application.Top);

        ApplicationImpl.ChangeInstance (orig);

        Assert.True (result);
    }

    [Fact]
    public void ApplicationImpl_UsesInstanceFields_NotStaticReferences ()
    {
        // This test verifies that ApplicationImpl uses instance fields instead of static Application references
        IApplication orig = ApplicationImpl.Instance;

        ApplicationImpl v2 = NewApplicationImpl ();
        ApplicationImpl.ChangeInstance (v2);

        // Before Init, all fields should be null/default
        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);
        Assert.Null (v2.Popover);
        Assert.Null (v2.Navigation);
        Assert.Null (v2.Top);
        Assert.Empty (v2.TopLevels);

        // Init should populate instance fields
        v2.Init (null, "fake");

        // After Init, Driver, Navigation, and Popover should be populated
        Assert.NotNull (v2.Driver);
        Assert.True (v2.Initialized);
        Assert.NotNull (v2.Popover);
        Assert.NotNull (v2.Navigation);
        Assert.Null (v2.Top); // Top is still null until Run

        // Verify that static Application properties delegate to instance
        Assert.Equal (v2.Driver, Application.Driver);
        Assert.Equal (v2.Initialized, Application.Initialized);
        Assert.Equal (v2.Popover, Application.Popover);
        Assert.Equal (v2.Navigation, Application.Navigation);
        Assert.Equal (v2.Top, Application.Top);
        Assert.Same (v2.TopLevels, Application.TopLevels);

        // Shutdown should clean up instance fields
        v2.Shutdown ();

        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);
        Assert.Null (v2.Popover);
        Assert.Null (v2.Navigation);
        Assert.Null (v2.Top);
        Assert.Empty (v2.TopLevels);

        ApplicationImpl.ChangeInstance (orig);
    }
}
