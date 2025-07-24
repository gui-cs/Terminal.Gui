#nullable enable
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class ApplicationV2Tests
{
    public ApplicationV2Tests ()
    {
        ConsoleDriver.RunningUnitTests = true;
    }

    private ApplicationV2 NewApplicationV2 ()
    {
        var netInput = new Mock<INetInput> ();
        SetupRunInputMockMethodToBlock (netInput);
        var winInput = new Mock<IWindowsInput> ();
        SetupRunInputMockMethodToBlock (winInput);

        return new (
                    () => netInput.Object,
                    Mock.Of<IConsoleOutput>,
                    () => winInput.Object,
                    Mock.Of<IConsoleOutput>);
    }

    [Fact]
    public void Init_CreatesKeybindings ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        Application.KeyBindings.Clear ();

        Assert.Empty (Application.KeyBindings.GetBindings ());

        v2.Init ();

        Assert.NotEmpty (Application.KeyBindings.GetBindings ());

        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void Init_DriverIsFacade ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Driver);
        v2.Init ();
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True (type.IsGenericType);
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);

        ApplicationImpl.ChangeInstance (orig);
    }

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

    private void SetupRunInputMockMethodToBlock (Mock<IWindowsInput> winInput)
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
        var orig = ApplicationImpl.Instance;

        Assert.Null (Application.Driver);
        var app = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (app);

        var ex = Assert.Throws<NotInitializedException> (() => app.Run (new Window ()));
        Assert.Equal ("Run cannot be accessed before Initialization", ex.Message);
        app.Shutdown();

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void InitRunShutdown_Top_Set_To_Null_After_Shutdown ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        var timeoutToken = v2.AddTimeout (TimeSpan.FromMilliseconds (150),
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
        Assert.False(v2.RemoveTimeout (timeoutToken));

        Assert.NotNull (Application.Top);
        Application.Top?.Dispose ();
        v2.Shutdown ();
        Assert.Null (Application.Top);

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void InitRunShutdown_Running_Set_To_False ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        Toplevel top = new Window ()
        {
            Title = "InitRunShutdown_Running_Set_To_False"
        };
        var timeoutToken = v2.AddTimeout (TimeSpan.FromMilliseconds (150),
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
    public void InitRunShutdown_End_Is_Called ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Top);
        Assert.Null (Application.Driver);

        v2.Init ();

        Toplevel top = new Window ();

        // BUGBUG: Both Closed and Unloaded are called from End; what's the difference?
        int closedCount = 0;
        top.Closed
            += (_, a) =>
               {
                   closedCount++;
               };

        int unloadedCount = 0;
        top.Unloaded
            += (_, a) =>
               {
                   unloadedCount++;
               };

        var timeoutToken = v2.AddTimeout (TimeSpan.FromMilliseconds (150),
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
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        Toplevel top = new Window ()
        {
            Title = "InitRunShutdown_QuitKey_Quits"
        };
        var timeoutToken = v2.AddTimeout (TimeSpan.FromMilliseconds (150),
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
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

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
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        int closing = 0;
        int closed = 0;
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

        v2.AddTimeout(TimeSpan.Zero, IdleExit);

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
        v2.Shutdown ();
        outputMock!.Verify (o => o.Dispose (), Times.Once);

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void Init_Called_Repeatedly_WarnsAndIgnores ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        Assert.Null (Application.Driver);
        v2.Init ();
        Assert.NotNull (Application.Driver);

        var mockLogger = new Mock<ILogger> ();

        var beforeLogger = Logging.Logger;
        Logging.Logger = mockLogger.Object;

        v2.Init ();
        v2.Init ();

        mockLogger.Verify (
                          l => l.Log (LogLevel.Error,
                                    It.IsAny<EventId> (),
                                    It.Is<It.IsAnyType> ((v, t) => v.ToString () == "Init called multiple times without shutdown, ignoring."),
                                    It.IsAny<Exception> (),
                                    It.IsAny<Func<It.IsAnyType, Exception, string>> ()!)
                          , Times.Exactly (2));

        v2.Shutdown ();

        // Restore the original null logger to be polite to other tests
        Logging.Logger = beforeLogger;

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void Open_Calls_ContinueWith_On_UIThread ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();
        var b = new Button ();

        bool result = false;

        b.Accepting +=
            (_, _) =>
            {

                Task.Run (() =>
                          {
                              Task.Delay (300).Wait ();
                          }).ContinueWith (
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

        v2.AddTimeout (TimeSpan.FromMilliseconds (150),
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

        var w = new Window ()
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
}
