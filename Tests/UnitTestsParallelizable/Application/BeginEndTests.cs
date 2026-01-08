using Xunit.Abstractions;

namespace ApplicationTests.BeginEnd;

/// <summary>
///     Comprehensive tests for ApplicationImpl.Begin/End logic that manages Current and SessionStack.
///     These tests ensure the fragile state management logic is robust and catches regressions.
///     Tests work directly with ApplicationImpl instances to avoid global Application state issues.
/// </summary>
[Collection("Application Tests")]
public class ApplicationImplBeginEndTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


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
    public void Begin_Null_Runnable_Throws ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Test null Runnable
        Assert.Throws<ArgumentNullException> (() => app.Begin (null!));

        app.Dispose ();
    }

    [Fact]
    public void Begin_Sets_Application_Top_To_Console_Size ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Assert.Null (app.TopRunnableView);
        app.Driver!.SetScreenSize (80, 25);
        Runnable top = new ();
        SessionToken? token = app.Begin (top);
        Assert.Equal (new (0, 0, 80, 25), app.TopRunnableView!.Frame);
        app.Driver!.SetScreenSize (5, 5);
        app.LayoutAndDraw ();
        Assert.Equal (new (0, 0, 5, 5), app.TopRunnableView!.Frame);

        if (token is { })
        {
            app.End (token);
        }
        top.Dispose ();

        app.Dispose ();
    }

    [Fact]
    public void Begin_WithNullRunnable_ThrowsArgumentNullException ()
    {
        IApplication app = Application.Create ();

        try
        {
            Assert.Throws<ArgumentNullException> (() => app.Begin (null!));
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void Begin_SetsCurrent_WhenCurrentIsNull ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable = null;

        try
        {
            runnable = new ();
            Assert.Null (app.TopRunnableView);

            app.Begin (runnable);

            Assert.NotNull (app.TopRunnableView);
            Assert.Same (runnable, app.TopRunnableView);
            Assert.Single (app.SessionStack!);
        }
        finally
        {
            runnable?.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void Begin_PushesToSessionStack ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable1 = null;
        Runnable? runnable2 = null;

        try
        {
            runnable1 = new () { Id = "1" };
            runnable2 = new () { Id = "2" };

            app.Begin (runnable1);
            Assert.Single (app.SessionStack!);
            Assert.Same (runnable1, app.TopRunnableView);

            app.Begin (runnable2);
            Assert.Equal (2, app.SessionStack!.Count);
            Assert.Same (runnable2, app.TopRunnableView);
        }
        finally
        {
            runnable1?.Dispose ();
            runnable2?.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void End_WithNullSessionToken_ThrowsArgumentNullException ()
    {
        IApplication app = Application.Create ();

        try
        {
            Assert.Throws<ArgumentNullException> (() => app.End (null!));
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void End_PopsSessionStack ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable1 = null;
        Runnable? runnable2 = null;

        try
        {
            runnable1 = new () { Id = "1" };
            runnable2 = new () { Id = "2" };

            SessionToken token1 = app.Begin (runnable1)!;
            SessionToken token2 = app.Begin (runnable2)!;

            Assert.Equal (2, app.SessionStack!.Count);

            app.End (token2);

            Assert.Single (app.SessionStack!);
            Assert.Same (runnable1, app.TopRunnableView);

            app.End (token1);

            Assert.Empty (app.SessionStack!);
        }
        finally
        {
            runnable1?.Dispose ();
            runnable2?.Dispose ();
            app.Dispose ();
        }
    }

    [Fact (Skip = "This test may be bogus. What's wrong with ending a non-top session?")]
    public void End_ThrowsArgumentException_WhenNotBalanced ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable1 = null;
        Runnable? runnable2 = null;

        try
        {
            runnable1 = new () { Id = "1" };
            runnable2 = new () { Id = "2" };

            SessionToken? token1 = app.Begin (runnable1);
            SessionToken? token2 = app.Begin (runnable2);

            // Trying to end token1 when token2 is on top should throw
            // NOTE: This throws but has the side effect of popping token2 from the stack
            Assert.Throws<ArgumentException> (() => app.End (token1!));

            // Don't try to clean up with more End calls - the state is now inconsistent
            // Let Shutdown/ResetState handle cleanup
        }
        finally
        {
            // Dispose runnables BEFORE Shutdown to satisfy DEBUG_IDISPOSABLE assertions
            runnable1?.Dispose ();
            runnable2?.Dispose ();

            // Shutdown will call ResetState which clears any remaining state
            app.Dispose ();
        }
    }

    [Fact]
    public void End_RestoresCurrentToPreviousRunnable ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable1 = null;
        Runnable? runnable2 = null;
        Runnable? runnable3 = null;

        try
        {
            runnable1 = new () { Id = "1" };
            runnable2 = new () { Id = "2" };
            runnable3 = new () { Id = "3" };

            SessionToken? token1 = app.Begin (runnable1);
            SessionToken? token2 = app.Begin (runnable2);
            SessionToken? token3 = app.Begin (runnable3);

            Assert.Same (runnable3, app.TopRunnableView);

            app.End (token3!);
            Assert.Same (runnable2, app.TopRunnableView);

            app.End (token2!);
            Assert.Same (runnable1, app.TopRunnableView);

            app.End (token1!);
        }
        finally
        {
            runnable1?.Dispose ();
            runnable2?.Dispose ();
            runnable3?.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void MultipleBeginEnd_MaintainsStackIntegrity ()
    {
        IApplication app = Application.Create ();
        List<Runnable> runnables = new ();
        List<SessionToken> tokens = new ();

        try
        {
            // Begin multiple runnables
            for (var i = 0; i < 5; i++)
            {
                var runnable = new Runnable { Id = $"runnable-{i}" };
                runnables.Add (runnable);
                SessionToken? token = app.Begin (runnable);
                tokens.Add (token!);
            }

            Assert.Equal (5, app.SessionStack!.Count);
            Assert.Same (runnables [4], app.TopRunnableView);

            // End them in reverse order (LIFO)
            for (var i = 4; i >= 0; i--)
            {
                app.End (tokens [i]);

                if (i > 0)
                {
                    Assert.Equal (i, app.SessionStack.Count);
                    Assert.Same (runnables [i - 1], app.TopRunnableView);
                }
                else
                {
                    Assert.Empty (app.SessionStack);
                }
            }
        }
        finally
        {
            foreach (Runnable runnable in runnables)
            {
                runnable.Dispose ();
            }

            app.Dispose ();
        }
    }

    [Fact]
    public void End_NullsSessionTokenRunnable ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable = null;

        try
        {
            runnable = new ();

            SessionToken? token = app.Begin (runnable);
            Assert.Same (runnable, token!.Runnable);

            app.End (token);

            Assert.Null (token.Runnable);
        }
        finally
        {
            runnable?.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void ResetState_ClearsSessionStack ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable1 = null;
        Runnable? runnable2 = null;

        try
        {
            runnable1 = new () { Id = "1" };
            runnable2 = new () { Id = "2" };

            app.Begin (runnable1);
            app.Begin (runnable2);

            Assert.Equal (2, app.SessionStack!.Count);
            Assert.NotNull (app.TopRunnableView);
        }
        finally
        {
            // Dispose runnables BEFORE Shutdown to satisfy DEBUG_IDISPOSABLE assertions
            runnable1?.Dispose ();
            runnable2?.Dispose ();

            // Shutdown calls ResetState, which will clear SessionStack and set Current to null
            app.Dispose ();

            // Verify cleanup happened
            Assert.Empty (app.SessionStack!);
            Assert.Null (app.TopRunnableView);
        }
    }

    [Fact]
    public void ResetState_StopsAllRunningRunnables ()
    {
        IApplication app = Application.Create ();
        Runnable? runnable1 = null;
        Runnable? runnable2 = null;

        try
        {
            runnable1 = new () { Id = "1" };
            runnable2 = new () { Id = "2" };

            app.Begin (runnable1);
            app.Begin (runnable2);

            Assert.True (runnable1.IsRunning);
            Assert.True (runnable2.IsRunning);
        }
        finally
        {
            // Dispose runnables BEFORE Shutdown to satisfy DEBUG_IDISPOSABLE assertions
            runnable1?.Dispose ();
            runnable2?.Dispose ();

            // Shutdown calls ResetState, which will stop all running runnables
            app.Dispose ();

            // Verify runnables were stopped
            Assert.False (runnable1!.IsRunning);
            Assert.False (runnable2!.IsRunning);
        }
    }

    //[Fact]
    //public void Begin_ActivatesNewRunnable_WhenCurrentExists ()
    //{
    //    IApplication app = Application.Create ();
    //    Runnable? runnable1 = null;
    //    Runnable? runnable2 = null;

    //    try
    //    {
    //        runnable1 = new () { Id = "1" };
    //        runnable2 = new () { Id = "2" };

    //        var runnable1Deactivated = false;
    //        var runnable2Activated = false;

    //        runnable1.Deactivate += (s, e) => runnable1Deactivated = true;
    //        runnable2.Activate += (s, e) => runnable2Activated = true;

    //        app.Begin (runnable1);
    //        app.Begin (runnable2);

    //        Assert.True (runnable1Deactivated);
    //        Assert.True (runnable2Activated);
    //        Assert.Same (runnable2, app.TopRunnable);
    //    }
    //    finally
    //    {
    //        runnable1?.Dispose ();
    //        runnable2?.Dispose ();
    //        app.Dispose ();
    //    }
    //}

    [Fact]
    public void SessionStack_ContainsAllBegunRunnables ()
    {
        IApplication app = Application.Create ();
        List<Runnable> runnables = new ();

        try
        {
            for (var i = 0; i < 10; i++)
            {
                var runnable = new Runnable { Id = $"runnable-{i}" };
                runnables.Add (runnable);
                app.Begin (runnable);
            }

            // All runnables should be in the stack
            Assert.Equal (10, app.SessionStack!.Count);

            // Verify stack contains all runnables
            List<SessionToken> stackList = app.SessionStack.ToList ();

            foreach (Runnable runnable in runnables)
            {
                Assert.Contains (runnable, stackList.Select (r => r.Runnable));
            }
        }
        finally
        {
            foreach (Runnable runnable in runnables)
            {
                runnable.Dispose ();
            }

            app.Dispose ();
        }
    }
}
