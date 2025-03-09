using System.Diagnostics;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ApplicationTests;

/// <summary>Tests MainLoop using the FakeMainLoop.</summary>
public class MainLoopTests
{
    private static Button btn;
    private static string cancel;
    private static string clickMe;
    private static int four;
    private static int one;
    private static string pewPew;
    private static bool taskCompleted;

    // TODO: EventsPending tests
    // - wait = true
    // - wait = false

    // TODO: Add IMainLoop tests
    private static int three;
    private static int total;
    private static int two;
    private static int zero;

    // See Also ConsoleDRivers/MainLoopDriverTests.cs for tests of the MainLoopDriver

    // Idle Handler tests
    [Fact]
    public void AddIdle_Adds_And_Removes ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        Func<bool> fnTrue = () => true;
        Func<bool> fnFalse = () => false;

        ml.AddIdle (fnTrue);
        ml.AddIdle (fnFalse);

        Assert.Equal (2, ml.TimedEvents.IdleHandlers.Count);
        Assert.Equal (fnTrue, ml.TimedEvents.IdleHandlers [0]);
        Assert.NotEqual (fnFalse, ml.TimedEvents.IdleHandlers [0]);

        Assert.True (ml.TimedEvents.RemoveIdle (fnTrue));
        Assert.Single (ml.TimedEvents.IdleHandlers);

        // BUGBUG: This doesn't throw or indicate an error. Ideally RemoveIdle would either 
        // throw an exception in this case, or return an error.
        // No. Only need to return a boolean.
        Assert.False (ml.TimedEvents.RemoveIdle (fnTrue));

        Assert.True (ml.TimedEvents.RemoveIdle (fnFalse));

        // BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
        // throw an exception in this case, or return an error.
        // No. Only need to return a boolean.
        Assert.False (ml.TimedEvents.RemoveIdle (fnFalse));

        // Add again, but with dupe
        ml.AddIdle (fnTrue);
        ml.AddIdle (fnTrue);

        Assert.Equal (2, ml.TimedEvents.IdleHandlers.Count);
        Assert.Equal (fnTrue, ml.TimedEvents.IdleHandlers [0]);
        Assert.True (ml.TimedEvents.IdleHandlers [0] ());
        Assert.Equal (fnTrue, ml.TimedEvents.IdleHandlers [1]);
        Assert.True (ml.TimedEvents.IdleHandlers [1] ());

        Assert.True (ml.TimedEvents.RemoveIdle (fnTrue));
        Assert.Single (ml.TimedEvents.IdleHandlers);
        Assert.Equal (fnTrue, ml.TimedEvents.IdleHandlers [0]);
        Assert.NotEqual (fnFalse, ml.TimedEvents.IdleHandlers [0]);

        Assert.True (ml.TimedEvents.RemoveIdle (fnTrue));
        Assert.Empty (ml.TimedEvents.IdleHandlers);

        // BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
        // throw an exception in this case, or return an error.
        // No. Only need to return a boolean.
        Assert.False (ml.TimedEvents.RemoveIdle (fnTrue));
    }

    [Fact]
    public void AddIdle_Function_GetsCalled_OnIteration ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        var functionCalled = 0;

        Func<bool> fn = () =>
                        {
                            functionCalled++;

                            return true;
                        };

        ml.AddIdle (fn);
        ml.RunIteration ();
        Assert.Equal (1, functionCalled);
    }

    [Fact]
    public void AddIdle_Twice_Returns_False_Called_Twice ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        var functionCalled = 0;

        Func<bool> fn1 = () =>
                         {
                             functionCalled++;

                             return false;
                         };

        // Force stop if 10 iterations
        var stopCount = 0;

        Func<bool> fnStop = () =>
                            {
                                stopCount++;

                                if (stopCount == 10)
                                {
                                    ml.Stop ();
                                }

                                return true;
                            };

        ml.AddIdle (fnStop);
        ml.AddIdle (fn1);
        ml.AddIdle (fn1);
        ml.Run ();
        Assert.True (ml.TimedEvents.RemoveIdle (fnStop));
        Assert.False (ml.TimedEvents.RemoveIdle (fn1));
        Assert.False (ml.TimedEvents.RemoveIdle (fn1));

        Assert.Equal (2, functionCalled);
    }

    [Fact]
    public void AddIdleTwice_Function_CalledTwice ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        var functionCalled = 0;

        Func<bool> fn = () =>
                        {
                            functionCalled++;

                            return true;
                        };

        ml.AddIdle (fn);
        ml.AddIdle (fn);
        ml.RunIteration ();
        Assert.Equal (2, functionCalled);
        Assert.Equal (2, ml.TimedEvents.IdleHandlers.Count);

        functionCalled = 0;
        Assert.True (ml.TimedEvents.RemoveIdle (fn));
        Assert.Single (ml.TimedEvents.IdleHandlers);
        ml.RunIteration ();
        Assert.Equal (1, functionCalled);

        functionCalled = 0;
        Assert.True (ml.TimedEvents.RemoveIdle (fn));
        Assert.Empty (ml.TimedEvents.IdleHandlers);
        ml.RunIteration ();
        Assert.Equal (0, functionCalled);
        Assert.False (ml.TimedEvents.RemoveIdle (fn));
    }

    [Fact]
    public void AddThenRemoveIdle_Function_NotCalled ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        var functionCalled = 0;

        Func<bool> fn = () =>
                        {
                            functionCalled++;

                            return true;
                        };

        ml.AddIdle (fn);
        Assert.True (ml.TimedEvents.RemoveIdle (fn));
        ml.RunIteration ();
        Assert.Equal (0, functionCalled);
    }

    // Timeout Handler Tests
    [Fact]
    public void AddTimer_Adds_Removes_NoFaults ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        var ms = 100;

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;

                                  return true;
                              };

        object token = ml.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);

        Assert.True (ml.TimedEvents.RemoveTimeout (token));

        // BUGBUG: This should probably fault?
        // Must return a boolean.
        Assert.False (ml.TimedEvents.RemoveTimeout (token));
    }

    [Fact]
    public async Task AddTimer_Duplicate_Keys_Not_Allowed ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        const int ms = 100;
        object token1 = null, token2 = null;

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;

                                  if (callbackCount == 2)
                                  {
                                      ml.Stop ();
                                  }

                                  return true;
                              };

        var task1 = new Task (() => token1 = ml.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (ms), callback));
        var task2 = new Task (() => token2 = ml.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (ms), callback));
        Assert.Null (token1);
        Assert.Null (token2);
        task1.Start ();
        task2.Start ();
        ml.Run ();
        Assert.NotNull (token1);
        Assert.NotNull (token2);
        await Task.WhenAll (task1, task2);
        Assert.True (ml.TimedEvents.RemoveTimeout (token1));
        Assert.True (ml.TimedEvents.RemoveTimeout (token2));

        Assert.Equal (2, callbackCount);
    }

    // Timeout Handler Tests
    [Fact]
    public void AddTimer_EventFired ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        var ms = 100;

        long originTicks = DateTime.UtcNow.Ticks;

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;

                                  return true;
                              };

        object sender = null;
        TimeoutEventArgs args = null;

        ml.TimedEvents.TimeoutAdded += (s, e) =>
                                       {
                                           sender = s;
                                           args = e;
                                       };

        object token = ml.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);

        Assert.Same (ml.TimedEvents, sender);
        Assert.NotNull (args.Timeout);
        Assert.True (args.Ticks - originTicks >= 100 * TimeSpan.TicksPerMillisecond);
    }

    [Fact]
    public void AddTimer_In_Parallel_Wont_Throw ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        const int ms = 100;
        object token1 = null, token2 = null;

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;

                                  if (callbackCount == 2)
                                  {
                                      ml.Stop ();
                                  }

                                  return true;
                              };

        Parallel.Invoke (
                         () => token1 = ml.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (ms), callback),
                         () => token2 = ml.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (ms), callback)
                        );
        ml.Run ();
        Assert.NotNull (token1);
        Assert.NotNull (token2);
        Assert.True (ml.TimedEvents.RemoveTimeout (token1));
        Assert.True (ml.TimedEvents.RemoveTimeout (token2));

        Assert.Equal (2, callbackCount);
    }

    [Fact]
    public void AddTimer_Remove_NotCalled ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        TimeSpan ms = TimeSpan.FromMilliseconds (50);

        // Force stop if 10 iterations
        var stopCount = 0;

        Func<bool> fnStop = () =>
                            {
                                stopCount++;

                                if (stopCount == 10)
                                {
                                    ml.Stop ();
                                }

                                return true;
                            };
        ml.AddIdle (fnStop);

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;

                                  return true;
                              };

        object token = ml.TimedEvents.AddTimeout (ms, callback);
        Assert.True (ml.TimedEvents.RemoveTimeout (token));
        ml.Run ();
        Assert.Equal (0, callbackCount);
    }

    [Fact]
    public void AddTimer_ReturnFalse_StopsBeingCalled ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        TimeSpan ms = TimeSpan.FromMilliseconds (50);

        // Force stop if 10 iterations
        var stopCount = 0;

        Func<bool> fnStop = () =>
                            {
                                Thread.Sleep (10); // Sleep to enable timer to fire
                                stopCount++;

                                if (stopCount == 10)
                                {
                                    ml.Stop ();
                                }

                                return true;
                            };
        ml.AddIdle (fnStop);

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;

                                  return false;
                              };

        object token = ml.TimedEvents.AddTimeout (ms, callback);
        ml.Run ();
        Assert.Equal (1, callbackCount);
        Assert.Equal (10, stopCount);
        Assert.False (ml.TimedEvents.RemoveTimeout (token));
    }

    [Fact]
    public void AddTimer_Run_Called ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        var ms = 100;

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;
                                  ml.Stop ();

                                  return true;
                              };

        object token = ml.TimedEvents.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);
        ml.Run ();
        Assert.True (ml.TimedEvents.RemoveTimeout (token));

        Assert.Equal (1, callbackCount);
    }

    [Fact]
    public void AddTimer_Run_CalledAtApproximatelyRightTime ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        TimeSpan ms = TimeSpan.FromMilliseconds (50);
        var watch = new Stopwatch ();

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  watch.Stop ();
                                  callbackCount++;
                                  ml.Stop ();

                                  return true;
                              };

        object token = ml.TimedEvents.AddTimeout (ms, callback);
        watch.Start ();
        ml.Run ();

        // +/- 100ms should be good enuf
        // https://github.com/xunit/assert.xunit/pull/25
        Assert.Equal (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (100));

        Assert.True (ml.TimedEvents.RemoveTimeout (token));
        Assert.Equal (1, callbackCount);
    }

    [Fact]
    public void AddTimer_Run_CalledTwiceApproximatelyRightTime ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        TimeSpan ms = TimeSpan.FromMilliseconds (50);
        var watch = new Stopwatch ();

        var callbackCount = 0;

        Func<bool> callback = () =>
                              {
                                  callbackCount++;

                                  if (callbackCount == 2)
                                  {
                                      watch.Stop ();
                                      ml.Stop ();
                                  }

                                  return true;
                              };

        object token = ml.TimedEvents.AddTimeout (ms, callback);
        watch.Start ();
        ml.Run ();

        // +/- 100ms should be good enuf
        // https://github.com/xunit/assert.xunit/pull/25
        Assert.Equal (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (100));

        Assert.True (ml.TimedEvents.RemoveTimeout (token));
        Assert.Equal (2, callbackCount);
    }

    [Fact]
    public void CheckTimersAndIdleHandlers_NoTimers_Returns_False ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        bool retVal = ml.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeOut);
        Assert.False (retVal);
        Assert.Equal (-1, waitTimeOut);
    }

    [Fact]
    public void CheckTimersAndIdleHandlers_NoTimers_WithIdle_Returns_True ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        Func<bool> fnTrue = () => true;

        ml.AddIdle (fnTrue);
        bool retVal = ml.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeOut);
        Assert.True (retVal);
        Assert.Equal (-1, waitTimeOut);
    }

    [Fact]
    public void CheckTimersAndIdleHandlers_With1Timer_Returns_Timer ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        TimeSpan ms = TimeSpan.FromMilliseconds (50);

        static bool Callback () { return false; }

        _ = ml.TimedEvents.AddTimeout (ms, Callback);
        bool retVal = ml.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeOut);

        Assert.True (retVal);

        // It should take < 10ms to execute to here
        Assert.True (ms.TotalMilliseconds <= waitTimeOut + 10);
    }

    [Fact]
    public void CheckTimersAndIdleHandlers_With2Timers_Returns_Timer ()
    {
        var ml = new MainLoop (new FakeMainLoop ());
        TimeSpan ms = TimeSpan.FromMilliseconds (50);

        static bool Callback () { return false; }

        _ = ml.TimedEvents.AddTimeout (ms, Callback);
        _ = ml.TimedEvents.AddTimeout (ms, Callback);
        bool retVal = ml.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeOut);

        Assert.True (retVal);

        // It should take < 10ms to execute to here
        Assert.True (ms.TotalMilliseconds <= waitTimeOut + 10);
    }

    [Fact]
    public void False_Idle_Stops_It_Being_Called_Again ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        var functionCalled = 0;

        Func<bool> fn1 = () =>
                         {
                             functionCalled++;

                             if (functionCalled == 10)
                             {
                                 return false;
                             }

                             return true;
                         };

        // Force stop if 20 iterations
        var stopCount = 0;

        Func<bool> fnStop = () =>
                            {
                                stopCount++;

                                if (stopCount == 20)
                                {
                                    ml.Stop ();
                                }

                                return true;
                            };

        ml.AddIdle (fnStop);
        ml.AddIdle (fn1);
        ml.Run ();
        Assert.True (ml.TimedEvents.RemoveIdle (fnStop));
        Assert.False (ml.TimedEvents.RemoveIdle (fn1));

        Assert.Equal (10, functionCalled);
        Assert.Equal (20, stopCount);
    }

    [Fact]
    public void Internal_Tests ()
    {
        var testMainloop = new TestMainloop ();
        var mainloop = new MainLoop (testMainloop);
        Assert.Empty (mainloop.TimedEvents.Timeouts);
        Assert.Empty (mainloop.TimedEvents.IdleHandlers);

        Assert.NotNull (
                        new Timeout { Span = new (), Callback = () => true }
                       );
    }

    [Theory]
    [MemberData (nameof (TestAddIdle))]
    public void Mainloop_Invoke_Or_AddIdle_Can_Be_Used_For_Events_Or_Actions (
        Action action,
        string pclickMe,
        string pcancel,
        string ppewPew,
        int pzero,
        int pone,
        int ptwo,
        int pthree,
        int pfour
    )
    {
        // TODO: Expand this test to test all drivers
        Application.Init (new FakeDriver ());

        total = 0;
        btn = null;
        clickMe = pclickMe;
        cancel = pcancel;
        pewPew = ppewPew;
        zero = pzero;
        one = pone;
        two = ptwo;
        three = pthree;
        four = pfour;
        taskCompleted = false;

        var btnLaunch = new Button { Text = "Open Window" };

        btnLaunch.Accepting += (s, e) => action ();

        var top = new Toplevel ();
        top.Add (btnLaunch);

        int iterations = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         Assert.Null (btn);
                                         Assert.Equal (zero, total);
                                         Assert.False (btnLaunch.NewKeyDownEvent (Key.Space));

                                         if (btn == null)
                                         {
                                             Assert.Null (btn);
                                             Assert.Equal (zero, total);
                                         }
                                         else
                                         {
                                             Assert.Equal (clickMe, btn.Text);
                                             Assert.Equal (four, total);
                                         }
                                     }
                                     else if (iterations == 1)
                                     {
                                         Assert.Equal (clickMe, btn.Text);
                                         Assert.Equal (zero, total);
                                         Assert.False (btn.NewKeyDownEvent (Key.Space));
                                         Assert.Equal (cancel, btn.Text);
                                         Assert.Equal (one, total);
                                     }
                                     else if (taskCompleted)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
        top.Dispose ();

        Assert.True (taskCompleted);
        Assert.Equal (clickMe, btn.Text);
        Assert.Equal (four, total);

        Application.Shutdown ();
    }

    [Fact]
    public void RemoveIdle_Function_NotCalled ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        var functionCalled = 0;

        Func<bool> fn = () =>
                        {
                            functionCalled++;

                            return true;
                        };

        Assert.False (ml.TimedEvents.RemoveIdle (fn));
        ml.RunIteration ();
        Assert.Equal (0, functionCalled);
    }

    [Fact]
    public void Run_Runs_Idle_Stop_Stops_Idle ()
    {
        var ml = new MainLoop (new FakeMainLoop ());

        var functionCalled = 0;

        Func<bool> fn = () =>
                        {
                            functionCalled++;

                            if (functionCalled == 10)
                            {
                                ml.Stop ();
                            }

                            return true;
                        };

        ml.AddIdle (fn);
        ml.Run ();
        Assert.True (ml.TimedEvents.RemoveIdle (fn));

        Assert.Equal (10, functionCalled);
    }

    public static IEnumerable<object []> TestAddIdle
    {
        get
        {
            // Goes fine
            Action a1 = StartWindow;

            yield return new object [] { a1, "Click Me", "Cancel", "Pew Pew", 0, 1, 2, 3, 4 };

            // Also goes fine
            Action a2 = () => Application.Invoke (StartWindow);

            yield return new object [] { a2, "Click Me", "Cancel", "Pew Pew", 0, 1, 2, 3, 4 };
        }
    }

    private static async void RunAsyncTest (object sender, EventArgs e)
    {
        Assert.Equal (clickMe, btn.Text);
        Assert.Equal (zero, total);

        btn.Text = "Cancel";
        Interlocked.Increment (ref total);
        btn.SetNeedsDraw ();

        await Task.Run (
                        () =>
                        {
                            try
                            {
                                Assert.Equal (cancel, btn.Text);
                                Assert.Equal (one, total);

                                RunSql ();
                            }
                            finally
                            {
                                SetReadyToRun ();
                            }
                        }
                       )
                  .ContinueWith (
                                 async (s, e) =>
                                 {
                                     await Task.Delay (1000);
                                     Assert.Equal (clickMe, btn.Text);
                                     Assert.Equal (three, total);

                                     Interlocked.Increment (ref total);

                                     Assert.Equal (clickMe, btn.Text);
                                     Assert.Equal (four, total);

                                     taskCompleted = true;
                                 },
                                 TaskScheduler.FromCurrentSynchronizationContext ()
                                );
    }

    private static void RunSql ()
    {
        Thread.Sleep (100);
        Assert.Equal (cancel, btn.Text);
        Assert.Equal (one, total);

        Application.Invoke (
                            () =>
                            {
                                btn.Text = "Pew Pew";
                                Interlocked.Increment (ref total);
                                btn.SetNeedsDraw ();
                            }
                           );
    }

    private static void SetReadyToRun ()
    {
        Thread.Sleep (100);
        Assert.Equal (pewPew, btn.Text);
        Assert.Equal (two, total);

        Application.Invoke (
                            () =>
                            {
                                btn.Text = "Click Me";
                                Interlocked.Increment (ref total);
                                btn.SetNeedsDraw ();
                            }
                           );
    }

    private static void StartWindow ()
    {
        var startWindow = new Window { Modal = true };

        btn = new() { Text = "Click Me" };

        btn.Accepting += RunAsyncTest;

        var totalbtn = new Button { X = Pos.Right (btn), Text = "total" };

        totalbtn.Accepting += (s, e) => { MessageBox.Query ("Count", $"Count is {total}", "Ok"); };

        startWindow.Add (btn);
        startWindow.Add (totalbtn);

        Application.Run (startWindow);

        Assert.Equal (clickMe, btn.Text);
        Assert.Equal (four, total);

        Application.RequestStop ();
    }

    private class MillisecondTolerance : IEqualityComparer<TimeSpan>
    {
        public MillisecondTolerance (int tolerance) { _tolerance = tolerance; }
        private readonly int _tolerance;
        public bool Equals (TimeSpan x, TimeSpan y) { return Math.Abs (x.Milliseconds - y.Milliseconds) <= _tolerance; }
        public int GetHashCode (TimeSpan obj) { return obj.GetHashCode (); }
    }

    private class TestMainloop : IMainLoopDriver
    {
        private MainLoop mainLoop;
        public bool EventsPending () { throw new NotImplementedException (); }
        public void Iteration () { throw new NotImplementedException (); }
        public void TearDown () { throw new NotImplementedException (); }
        public void Setup (MainLoop mainLoop) { this.mainLoop = mainLoop; }
        public void Wakeup () { throw new NotImplementedException (); }
    }
}
