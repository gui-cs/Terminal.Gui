using System.Collections.Concurrent;
using Moq;

namespace UnitTests_Parallelizable.ApplicationTests;

public class ApplicationImplTests
{
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
    public void Init_CreatesKeybindings ()
    {
        IApplication app = NewMockedApplicationImpl ();

        app.Keyboard.KeyBindings.Clear ();

        Assert.Empty (app.Keyboard.KeyBindings.GetBindings ());

        app.Init ("fake");

        Assert.NotEmpty (app.Keyboard.KeyBindings.GetBindings ());

        app.Dispose ();
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

        app.Init ("fake");

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

        app.Init ("fake");

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

        app.Init ("fake");

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

        app.Init ("fake");

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

        app.Init ("fake");

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

        app.Init ("fake");

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

        app.Init ("fake");

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

        app.Init ("fake");

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
        v2.Init ("fake");

        // After Init, Driver, Navigation, and Popover should be populated
        Assert.NotNull (v2.Driver);
        Assert.True (v2.Initialized);
        Assert.NotNull (v2.Popover);
        Assert.NotNull (v2.Navigation);
        Assert.Null (v2.TopRunnableView); // Top is still null until Run

        // Shutdown should clean up instance fields
        v2.Shutdown ();

        Assert.Null (v2.Driver);
        Assert.False (v2.Initialized);

        //Assert.Null (v2.Popover);
        //Assert.Null (v2.Navigation);
        Assert.Null (v2.TopRunnableView);
        Assert.Empty (v2.SessionStack!);
    }

    [Fact]
    public void Init_Begin_End_Cleans_Up ()
    {
        IApplication? app = Application.Create ();

        SessionToken? newSessionToken = null;

        EventHandler<SessionTokenEventArgs> newSessionTokenFn = (s, e) =>
                                                                {
                                                                    Assert.NotNull (e.State);
                                                                    newSessionToken = e.State;
                                                                };
        app.SessionBegun += newSessionTokenFn;

        Runnable<bool> runnable = new ();
        SessionToken sessionToken = app.Begin (runnable)!;
        Assert.NotNull (sessionToken);
        Assert.NotNull (newSessionToken);
        Assert.Equal (sessionToken, newSessionToken);

        // Assert.Equal (runnable, Application.TopRunnable);

        app.SessionBegun -= newSessionTokenFn;
        app.End (newSessionToken);

        Assert.Null (app.TopRunnable);
        Assert.Null (app.Driver);

        runnable.Dispose ();
    }

    [Fact]
    public void Run_RequestStop_Stops ()
    {
        IApplication? app = Application.Create ();
        app.Init ("fake");

        var top = new Toplevel ();
        SessionToken? sessionToken = app.Begin (top);
        Assert.NotNull (sessionToken);

        app.Iteration += OnApplicationOnIteration;
        app.Run (top);
        app.Iteration -= OnApplicationOnIteration;

        top.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a) { app.RequestStop (); }
    }

    [Fact]
    public void Run_T_Init_Driver_Cleared_with_Runnable_Throws ()
    {
        IApplication? app = Application.Create ();

        app.Init ("fake");
        app.Driver = null;

        app.StopAfterFirstIteration = true;

        // Init has been called, but Driver has been set to null. Bad.
        Assert.Throws<InvalidOperationException> (() => app.Run<Toplevel> ());
    }

    [Fact]
    public void Init_Unbalanced_Throws ()
    {
        IApplication? app = Application.Create ();
        app.Init ("fake");

        Assert.Throws<InvalidOperationException> (() =>
                                                      app.Init ("fake")
                                                 );
    }
}
