#nullable enable
using System.Collections.Concurrent;
using Moq;

namespace UnitTests_Parallelizable.ApplicationTests;

public class ApplicationImplTests
{
    /// <summary>
    ///     Crates a new ApplicationImpl instance for testing. The input, output, and size monitor components are mocked.
    /// </summary>
    private IApplication? NewMockedApplicationImpl ()
    {
        Mock<INetInput> netInput = new ();
        SetupRunInputMockMethodToBlock (netInput);

        Mock<IComponentFactory<ConsoleKeyInfo>> m = new ();
        m.Setup (f => f.CreateInput ()).Returns (netInput.Object);
        m.Setup (f => f.CreateInputProcessor (It.IsAny<ConcurrentQueue<ConsoleKeyInfo>> ())).Returns (Mock.Of<IInputProcessor> ());

        Mock<IOutput> consoleOutput = new ();
        var size = new Size (80, 25);

        consoleOutput.Setup (o => o.SetSize (It.IsAny<int> (), It.IsAny<int> ()))
                     .Callback<int, int> ((w, h) => size = new (w, h));
        consoleOutput.Setup (o => o.GetSize ()).Returns (() => size);
        m.Setup (f => f.CreateOutput ()).Returns (consoleOutput.Object);
        m.Setup (f => f.CreateSizeMonitor (It.IsAny<IOutput> (), It.IsAny<IOutputBuffer> ())).Returns (Mock.Of<ISizeMonitor> ());

        return new ApplicationImpl (m.Object);
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
    public void Init_CreatesKeybindings ()
    {
        IApplication? app = NewMockedApplicationImpl ();

        app?.Keyboard.KeyBindings.Clear ();

        Assert.Empty (app?.Keyboard?.KeyBindings.GetBindings ()!);

        app?.Init ("fake");

        Assert.NotEmpty (app?.Keyboard?.KeyBindings.GetBindings ()!);

        app?.Shutdown ();
    }

    [Fact]
    public void NoInitThrowOnRun ()
    {
        IApplication? app = NewMockedApplicationImpl ();
        var ex = Assert.Throws<NotInitializedException> (() => app?.Run (new Window ()));
        Assert.Equal ("Run cannot be accessed before Initialization", ex.Message);
        app?.Shutdown ();
    }

    [Fact]
    public void InitRunShutdown_Top_Set_To_Null_After_Shutdown ()
    {
        IApplication? app = NewMockedApplicationImpl ();

        app?.Init ("fake");

        object? timeoutToken = app?.AddTimeout (
                                                TimeSpan.FromMilliseconds (150),
                                                () =>
                                                {
                                                    if (app.TopRunnable is { })
                                                    {
                                                        app.RequestStop ();

                                                        return false;
                                                    }

                                                    return false;
                                                }
                                               );
        Assert.Null (app?.TopRunnable);

        // Blocks until the timeout call is hit

        app?.Run (new Window ());

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app?.RemoveTimeout (timeoutToken!));

        Assert.NotNull (app?.TopRunnable);
        app.TopRunnable?.Dispose ();
        app.Shutdown ();
        Assert.Null (app.TopRunnable);
    }

    [Fact]
    public void InitRunShutdown_Running_Set_To_False ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init ("fake");

        Toplevel top = new Window
        {
            Title = "InitRunShutdown_Running_Set_To_False"
        };

        object timeoutToken = app.AddTimeout (
                                              TimeSpan.FromMilliseconds (150),
                                              () =>
                                              {
                                                  Assert.True (top!.Running);

                                                  if (app.TopRunnable != null)
                                                  {
                                                      app.RequestStop ();

                                                      return false;
                                                  }

                                                  return false;
                                              }
                                             );

        Assert.False (top!.Running);

        // Blocks until the timeout call is hit
        app.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken));

        Assert.False (top!.Running);

        // BUGBUG: Shutdown sets Top to null, not End.
        //Assert.Null (Application.TopRunnable);
        app.TopRunnable?.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void InitRunShutdown_StopAfterFirstIteration_Stops ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        Assert.Null (app.TopRunnable);
        Assert.Null (app.Driver);

        app.Init ("fake");

        Toplevel top = new Window ();
        app.TopRunnable = top;

        var closedCount = 0;

        top.Closed
            += (_, a) => { closedCount++; };

        var unloadedCount = 0;

        top.Unloaded
            += (_, a) => { unloadedCount++; };

        object timeoutToken = app.AddTimeout (
                                              TimeSpan.FromMilliseconds (150),
                                              () =>
                                              {
                                                  Assert.Fail (@"Didn't stop after first iteration.");

                                                  return false;
                                              }
                                             );

        Assert.Equal (0, closedCount);
        Assert.Equal (0, unloadedCount);

        app.StopAfterFirstIteration = true;
        app.Run (top);

        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);

        app.TopRunnable?.Dispose ();
        app.Shutdown ();
        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);
    }

    [Fact]
    public void InitRunShutdown_End_Is_Called ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        Assert.Null (app.TopRunnable);
        Assert.Null (app.Driver);

        app.Init ("fake");

        Toplevel top = new Window ();

        // BUGBUG: Both Closed and Unloaded are called from End; what's the difference?
        var closedCount = 0;

        top.Closed
            += (_, a) => { closedCount++; };

        var unloadedCount = 0;

        top.Unloaded
            += (_, a) => { unloadedCount++; };

        object timeoutToken = app.AddTimeout (
                                              TimeSpan.FromMilliseconds (150),
                                              () =>
                                              {
                                                  Assert.True (top!.Running);

                                                  if (app.TopRunnable != null)
                                                  {
                                                      app.RequestStop ();

                                                      return false;
                                                  }

                                                  return false;
                                              }
                                             );

        Assert.Equal (0, closedCount);
        Assert.Equal (0, unloadedCount);

        // Blocks until the timeout call is hit
        app.Run (top);

        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken));

        app.TopRunnable?.Dispose ();
        app.Shutdown ();
        Assert.Equal (1, closedCount);
        Assert.Equal (1, unloadedCount);
    }

    [Fact]
    public void InitRunShutdown_QuitKey_Quits ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init ("fake");

        Toplevel top = new Window
        {
            Title = "InitRunShutdown_QuitKey_Quits"
        };

        object timeoutToken = app.AddTimeout (
                                              TimeSpan.FromMilliseconds (150),
                                              () =>
                                              {
                                                  Assert.True (top!.Running);

                                                  if (app.TopRunnable != null)
                                                  {
                                                      app.Keyboard.RaiseKeyDownEvent (app.Keyboard.QuitKey);
                                                  }

                                                  return false;
                                              }
                                             );

        Assert.False (top!.Running);

        // Blocks until the timeout call is hit
        app.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken));

        Assert.False (top!.Running);

        Assert.NotNull (app.TopRunnable);
        top.Dispose ();
        app.Shutdown ();
        Assert.Null (app.TopRunnable);
    }

    [Fact (Skip = "Phase 2: Ambiguous method call after Toplevel implements IRunnable. Use non-generic Run() or explicit cast.")]
    public void InitRunShutdown_Generic_IdleForExit ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init ("fake");

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));
        Assert.Null (app.TopRunnable);

        // Blocks until the timeout call is hit
        // Phase 2: Ambiguous method call - use non-generic Run()
        Window window = new ();
        app.Run (window);

        Assert.NotNull (app.TopRunnable);
        app.TopRunnable?.Dispose ();
        app.Shutdown ();
        Assert.Null (app.TopRunnable);
    }

    [Fact]
    public void Shutdown_Closing_Closed_Raised ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init ("fake");

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

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));

        // Blocks until the timeout call is hit

        app.Run (t);

        app.TopRunnable?.Dispose ();
        app.Shutdown ();

        Assert.Equal (2, closing);
        Assert.Equal (1, closed);
    }

    private bool IdleExit (IApplication app)
    {
        if (app.TopRunnable != null)
        {
            app.RequestStop ();

            return true;
        }

        return true;
    }

    [Fact]
    public void Open_Calls_ContinueWith_On_UIThread ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init ("fake");
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
                                       app.Invoke (() =>
                                                   {
                                                       result = true;
                                                       app.RequestStop ();
                                                   });
                                   },
                                   TaskScheduler.FromCurrentSynchronizationContext ());
            };

        app.AddTimeout (
                        TimeSpan.FromMilliseconds (150),
                        () =>
                        {
                            // Run asynchronous logic inside Task.Run
                            if (app.TopRunnable != null)
                            {
                                b.NewKeyDownEvent (Key.Enter);
                                b.NewKeyUpEvent (Key.Enter);
                            }

                            return false;
                        });

        Assert.Null (app.TopRunnable);

        var w = new Window
        {
            Title = "Open_CallsContinueWithOnUIThread"
        };
        w.Add (b);

        // Blocks until the timeout call is hit
        app.Run (w);

        Assert.NotNull (app.TopRunnable);
        app.TopRunnable?.Dispose ();
        app.Shutdown ();
        Assert.Null (app.TopRunnable);

        Assert.True (result);
    }

    [Fact]
    public void ApplicationImpl_UsesInstanceFields_NotStaticReferences ()
    {
        // This test verifies that ApplicationImpl uses instance fields instead of static Application references
        IApplication v2 = NewMockedApplicationImpl ()!;

        // Before Init, all fields should be null/default
        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);

        //Assert.Null (v2.Popover);
        //Assert.Null (v2.Navigation);
        Assert.Null (v2.TopRunnable);
        Assert.Empty (v2.SessionStack);

        // Init should populate instance fields
        v2.Init ("fake");

        // After Init, Driver, Navigation, and Popover should be populated
        Assert.NotNull (v2.Driver);
        Assert.True (v2.Initialized);
        Assert.NotNull (v2.Popover);
        Assert.NotNull (v2.Navigation);
        Assert.Null (v2.TopRunnable); // Top is still null until Run

        // Shutdown should clean up instance fields
        v2.Shutdown ();

        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);

        //Assert.Null (v2.Popover);
        //Assert.Null (v2.Navigation);
        Assert.Null (v2.TopRunnable);
        Assert.Empty (v2.SessionStack);
    }
}
