using System.Collections.Concurrent;
using Moq;

namespace ApplicationTests;

public class ApplicationImplTests
{

    [Fact]
    public void Internal_Properties_Correct ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Assert.True (app.Initialized);
        Assert.Null (app.TopRunnableView);
        SessionToken? rs = app.Begin (new Runnable<bool> ());
        Assert.Equal (app.TopRunnable, rs!.Runnable);
        Assert.Null (app.Mouse.MouseGrabView); // public

        app.Dispose ();
    }


    #region DisposeTests

    [Fact]
    public async Task Dispose_Allows_Async ()
    {
        var isCompletedSuccessfully = false;

        async Task TaskWithAsyncContinuation ()
        {
            await Task.Yield ();
            await Task.Yield ();

            isCompletedSuccessfully = true;
        }

        IApplication app = Application.Create ();
        app.Dispose ();

        Assert.False (isCompletedSuccessfully);
        await TaskWithAsyncContinuation ();
        Thread.Sleep (100);
        Assert.True (isCompletedSuccessfully);
    }

    [Fact]
    public void Dispose_Resets_SyncContext ()
    {
        IApplication app = Application.Create ();
        app.Dispose ();
        Assert.Null (SynchronizationContext.Current);
    }

    [Fact]
    public void Dispose_Alone_Does_Nothing ()
    {
        IApplication app = Application.Create ();
        app.Dispose ();
    }


    #endregion


    /// <summary>
    ///     Crates a new ApplicationImpl instance for testing. The input, output, and size monitor components are mocked.
    /// </summary>
    private IApplication NewMockedApplicationImpl ()
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
    public void NoInitThrowOnRun ()
    {
        IApplication app = NewMockedApplicationImpl ();
        var ex = Assert.Throws<NotInitializedException> (() => app.Run (new Window ()));
        app.Dispose ();
    }

    [Fact]
    public void InitRunShutdown_Top_Set_To_Null_After_Shutdown ()
    {
        IApplication app = NewMockedApplicationImpl ();

        app.Init (DriverRegistry.Names.ANSI);

        object? timeoutToken = app.AddTimeout (
                                               TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   if (app.TopRunnableView is { })
                                                   {
                                                       app.RequestStop ();

                                                       return false;
                                                   }

                                                   return false;
                                               }
                                              );
        Assert.Null (app.TopRunnableView);

        // Blocks until the timeout call is hit

        app.Run (new Window ());

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        Assert.Null (app.TopRunnableView);
        app.Dispose ();
        Assert.Null (app.TopRunnableView);
    }

    [Fact]
    public void InitRunShutdown_Running_Set_To_False ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window
        {
            Title = "InitRunShutdown_Running_Set_To_False"
        };

        object? timeoutToken = app.AddTimeout (
                                               TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   Assert.True (top!.IsRunning);

                                                   if (app.TopRunnableView != null)
                                                   {
                                                       app.RequestStop ();

                                                       return false;
                                                   }

                                                   return false;
                                               }
                                              );

        Assert.False (top.IsRunning);

        // Blocks until the timeout call is hit
        app.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        Assert.False (top.IsRunning);

        // BUGBUG: Shutdown sets Top to null, not End.
        //Assert.Null (Application.TopRunnable);
        app.TopRunnableView?.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void InitRunShutdown_StopAfterFirstIteration_Stops ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        Assert.Null (app.TopRunnableView);
        Assert.Null (app.Driver);

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window ();
        var isIsModalChanged = 0;

        top.IsModalChanged
            += (_, a) => { isIsModalChanged++; };

        var isRunningChangedCount = 0;

        top.IsRunningChanged
            += (_, a) => { isRunningChangedCount++; };

        object? timeoutToken = app.AddTimeout (
                                               TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   //Assert.Fail (@"Didn't stop after first iteration.");

                                                   return false;
                                               }
                                              );

        Assert.Equal (0, isIsModalChanged);
        Assert.Equal (0, isRunningChangedCount);

        app.StopAfterFirstIteration = true;
        app.Run (top);

        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);

        app.TopRunnableView?.Dispose ();
        app.Dispose ();
        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);
    }

    [Fact]
    public void InitRunShutdown_End_Is_Called ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        Assert.Null (app.TopRunnableView);
        Assert.Null (app.Driver);

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window ();

        var isIsModalChanged = 0;

        top.IsModalChanged
            += (_, a) => { isIsModalChanged++; };

        var isRunningChangedCount = 0;

        top.IsRunningChanged
            += (_, a) => { isRunningChangedCount++; };

        object? timeoutToken = app.AddTimeout (
                                               TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   Assert.True (top!.IsRunning);

                                                   if (app.TopRunnableView != null)
                                                   {
                                                       app.RequestStop ();

                                                       return false;
                                                   }

                                                   return false;
                                               }
                                              );

        Assert.Equal (0, isIsModalChanged);
        Assert.Equal (0, isRunningChangedCount);

        // Blocks until the timeout call is hit
        app.Run (top);

        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        app.TopRunnableView?.Dispose ();
        app.Dispose ();
        Assert.Equal (2, isIsModalChanged);
        Assert.Equal (2, isRunningChangedCount);
    }

    [Fact]
    public void InitRunShutdown_QuitKey_Quits ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        IRunnable top = new Window
        {
            Title = "InitRunShutdown_QuitKey_Quits"
        };

        object? timeoutToken = app.AddTimeout (
                                               TimeSpan.FromMilliseconds (150),
                                               () =>
                                               {
                                                   Assert.True (top!.IsRunning);

                                                   if (app.TopRunnableView != null)
                                                   {
                                                       app.Keyboard.RaiseKeyDownEvent (app.Keyboard.QuitKey);
                                                   }

                                                   return false;
                                               }
                                              );

        Assert.False (top!.IsRunning);

        // Blocks until the timeout call is hit
        app.Run (top);

        // We returned false above, so we should not have to remove the timeout
        Assert.False (app.RemoveTimeout (timeoutToken!));

        Assert.False (top!.IsRunning);

        Assert.Null (app.TopRunnableView);
        ((top as Window)!).Dispose ();
        app.Dispose ();
        Assert.Null (app.TopRunnableView);
    }

    [Fact]
    public void InitRunShutdown_Generic_IdleForExit ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));
        Assert.Null (app.TopRunnableView);

        // Blocks until the timeout call is hit

        app.Run<Window> ();

        Assert.Null (app.TopRunnableView);
        app.Dispose ();
        Assert.Null (app.TopRunnableView);
    }

    [Fact]
    public void Run_IsRunningChanging_And_IsRunningChanged_Raised ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        var isRunningChanging = 0;
        var isRunningChanged = 0;
        Runnable<bool> t = new ();

        t.IsRunningChanging
            += (_, a) => { isRunningChanging++; };

        t.IsRunningChanged
            += (_, a) => { isRunningChanged++; };

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));

        // Blocks until the timeout call is hit
        app.Run (t);

        Assert.Equal (2, isRunningChanging);
        Assert.Equal (2, isRunningChanged);
    }

    [Fact]
    public void Run_IsRunningChanging_Cancel_IsRunningChanged_Not_Raised ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);

        var isRunningChanging = 0;
        var isRunningChanged = 0;
        Runnable<bool> t = new ();

        t.IsRunningChanging
            += (_, a) =>
               {
                   // Cancel the first time
                   if (isRunningChanging == 0)
                   {
                       a.Cancel = true;
                   }

                   isRunningChanging++;
               };

        t.IsRunningChanged
            += (_, a) => { isRunningChanged++; };

        app.AddTimeout (TimeSpan.Zero, () => IdleExit (app));

        // Blocks until the timeout call is hit

        app.Run (t);

        Assert.Equal (1, isRunningChanging);
        Assert.Equal (0, isRunningChanged);
    }

    private bool IdleExit (IApplication app)
    {
        if (app.TopRunnableView != null)
        {
            app.RequestStop ();
        }

        // Return false so the timer does not repeat
        return false;
    }

    [Fact]
    public void Open_Calls_ContinueWith_On_UIThread ()
    {
        IApplication app = NewMockedApplicationImpl ()!;

        app.Init (DriverRegistry.Names.ANSI);
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
                            if (app.TopRunnableView != null)
                            {
                                b.NewKeyDownEvent (Key.Enter);
                                b.NewKeyUpEvent (Key.Enter);
                            }

                            return false;
                        });

        Assert.Null (app.TopRunnableView);

        var w = new Window
        {
            Title = "Open_CallsContinueWithOnUIThread"
        };
        w.Add (b);

        // Blocks until the timeout call is hit
        app.Run (w);

        w?.Dispose ();
        app.Dispose ();

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
        Assert.Null (v2.TopRunnableView);
        Assert.Empty (v2.SessionStack!);

        // Init should populate instance fields
        v2.Init (DriverRegistry.Names.ANSI);

        // After Init, Driver, Navigation, and Popover should be populated
        Assert.NotNull (v2.Driver);
        Assert.True (v2.Initialized);
        Assert.NotNull (v2.Popover);
        Assert.NotNull (v2.Navigation);
        Assert.Null (v2.TopRunnableView); // Top is still null until Run

        // Shutdown should clean up instance fields
        v2.Dispose ();

        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);

        //Assert.Null (v2.Popover);
        //Assert.Null (v2.Navigation);
        Assert.Null (v2.TopRunnableView);
        Assert.Empty (v2.SessionStack!);
    }
}
