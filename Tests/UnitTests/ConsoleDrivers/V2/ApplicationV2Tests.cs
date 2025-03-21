#nullable enable
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class ApplicationV2Tests
{
    
    private ApplicationV2 NewApplicationV2 ()
    {
        var netInput = new Mock<INetInput> ();
        SetupRunInputMockMethodToBlock (netInput);
        var winInput = new Mock<IWindowsInput> ();
        SetupRunInputMockMethodToBlock (winInput);

        return new (
                    ()=>netInput.Object,
                    Mock.Of<IConsoleOutput>,
                    () => winInput.Object,
                    Mock.Of<IConsoleOutput>);
    }

    [Fact]
    public void TestInit_CreatesKeybindings ()
    {
        var v2 = NewApplicationV2();

        Application.KeyBindings.Clear();

        Assert.Empty(Application.KeyBindings.GetBindings ());

        v2.Init ();

        Assert.NotEmpty (Application.KeyBindings.GetBindings ());

        v2.Shutdown ();
    }

    [Fact]
    public void TestInit_DriverIsFacade ()
    {
        var v2 = NewApplicationV2();

        Assert.Null (Application.Driver);
        v2.Init ();
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True(type.IsGenericType); 
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);
    }

    [Fact]
    public void TestInit_ExplicitlyRequestWin ()
    {
        var netInput = new Mock<INetInput> (MockBehavior.Strict);
        var netOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);
        var winInput = new Mock<IWindowsInput> (MockBehavior.Strict);
        var winOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);

        winInput.Setup (i => i.Initialize (It.IsAny<ConcurrentQueue<WindowsConsole.InputRecord>> ()))
                .Verifiable(Times.Once);
        SetupRunInputMockMethodToBlock (winInput);
        winInput.Setup (i=>i.Dispose ())
                .Verifiable(Times.Once);
        winOutput.Setup (i => i.Dispose ())
                 .Verifiable (Times.Once);

        var v2 = new ApplicationV2 (
                                    ()=> netInput.Object,
                                    () => netOutput.Object,
                                    () => winInput.Object,
                                    () => winOutput.Object);

        Assert.Null (Application.Driver);
        v2.Init (null,"v2win");
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True (type.IsGenericType);
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);

        winInput.VerifyAll();
    }

    [Fact]
    public void TestInit_ExplicitlyRequestNet ()
    {
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

        Assert.Null (Application.Driver);
        v2.Init (null, "v2net");
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True (type.IsGenericType);
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);

        netInput.VerifyAll ();
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
    public void Test_NoInitThrowOnRun ()
    {
        var app = NewApplicationV2();

        var ex = Assert.Throws<NotInitializedException> (() => app.Run (new Window ()));
        Assert.Equal ("Run cannot be accessed before Initialization", ex.Message);
    }

    [Fact]
    public void Test_InitRunShutdown ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        var timeoutToken = v2.AddTimeout (TimeSpan.FromMilliseconds (150),
                       () =>
                       {
                           if (Application.Top != null)
                           {
                               Application.RequestStop ();
                               return true;
                           }

                           return true;
                       }
                       );
        Assert.Null (Application.Top);

        // Blocks until the timeout call is hit

        v2.Run (new Window ());

        Assert.True(v2.RemoveTimeout (timeoutToken));

        Assert.Null (Application.Top);
        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);
    }


    [Fact]
    public void Test_InitRunShutdown_Generic_IdleForExit ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        v2.AddIdle (IdleExit);
        Assert.Null (Application.Top);

        // Blocks until the timeout call is hit

        v2.Run<Window> ();

        Assert.Null (Application.Top);
        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);
    }

    [Fact]
    public void Test_V2_ClosingRaised ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        int closing=0;
        int closed = 0;
        var t=new Toplevel ();
        t.Closing
            += (_, a) =>
               {
                   // Cancel the first time
                   if (closing==0)
                   {
                       a.Cancel = true;
                   }
                   closing++;
                   Assert.Same(t,a.RequestingTop);
               };

        t.Closed
            += (_, a) =>
               {
                   closed++;
                   Assert.Same (t, a.Toplevel);
               };

        v2.AddIdle (IdleExit);

        // Blocks until the timeout call is hit

        v2.Run (t);

        Assert.Null (Application.Top);
        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);

        Assert.Equal (2,closing);
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
    public void TestRepeatedShutdownCalls_DoNotDuplicateDisposeOutput ()
    {
        var netInput = new Mock<INetInput> ();
        SetupRunInputMockMethodToBlock (netInput);
        Mock<IConsoleOutput>? outputMock = null;


        var v2 = new ApplicationV2(
                                   () => netInput.Object,
                                   ()=> (outputMock = new Mock<IConsoleOutput>()).Object,
                                   Mock.Of<IWindowsInput>,
                                   Mock.Of<IConsoleOutput>);

        v2.Init (null,"v2net");


        v2.Shutdown ();
        v2.Shutdown ();
        outputMock!.Verify(o=>o.Dispose (),Times.Once);
    }
    [Fact]
    public void TestRepeatedInitCalls_WarnsAndIgnores ()
    {
        var v2 = NewApplicationV2 ();

        Assert.Null (Application.Driver);
        v2.Init ();
        Assert.NotNull (Application.Driver);

        var mockLogger = new Mock<ILogger> ();

        var beforeLogger = Logging.Logger;
        Logging.Logger = mockLogger.Object;

        v2.Init ();
        v2.Init ();

        mockLogger.Verify(
                          l=>l.Log (LogLevel.Error,
                                    It.IsAny<EventId> (),
                                    It.Is<It.IsAnyType> ((v, t) => v.ToString () == "Init called multiple times without shutdown, ignoring."),
                                    It.IsAny<Exception> (),
                                    It.IsAny<Func<It.IsAnyType, Exception, string>> ()!)
                          ,Times.Exactly (2));

        v2.Shutdown ();

        // Restore the original null logger to be polite to other tests
        Logging.Logger = beforeLogger;
    }

    [Fact]
    public void Test_Open_CallsContinueWithOnUIThread ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2 ();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();
        var b = new Button ();

        bool result = false;

        b.Accepting +=
            (_,_) =>
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
                                          ()=>
                                          {
                                              // Run asynchronous logic inside Task.Run
                                              if (Application.Top != null)
                                              {
                                                  b.NewKeyDownEvent (Key.Enter);
                                                  b.NewKeyUpEvent (Key.Enter);

                                                  return false;
                                              }

                                              return true;
                                          });

        Assert.Null (Application.Top);

        var w = new Window ();
        w.Add (b);

        // Blocks until the timeout call is hit
        v2.Run (w);

        Assert.Null (Application.Top);
        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);

        Assert.True (result);
    }
}
