#nullable enable
using Xunit.Abstractions;

namespace UnitTests.ApplicationTests;

/// <summary>
///     Comprehensive tests for ApplicationImpl.Begin/End logic that manages Current and SessionStack.
///     These tests ensure the fragile state management logic is robust and catches regressions.
///     Tests work directly with ApplicationImpl instances to avoid global Application state issues.
/// </summary>
public class ApplicationImplBeginEndTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private IApplication NewApplicationImpl ()
    {
        IApplication app = ApplicationImpl.Instance; // Force legacy

        return app;
    }

    [Fact]
    public void Begin_WithNullToplevel_ThrowsArgumentNullException ()
    {
        IApplication app = NewApplicationImpl ();

        try
        {
            Assert.Throws<ArgumentNullException> (() => app.Begin ((Toplevel)null!));
        }
        finally
        {
            app.Shutdown ();
        }
    }

    [Fact]
    public void Begin_SetsCurrent_WhenCurrentIsNull ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel = null;

        try
        {
            toplevel = new ();
            Assert.Null (app.TopRunnable);

            app.Begin (toplevel);

            Assert.NotNull (app.TopRunnable);
            Assert.Same (toplevel, app.TopRunnable);
            Assert.Single (app.SessionStack);
        }
        finally
        {
            toplevel?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void Begin_PushesToSessionStack ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;

        try
        {
            toplevel1 = new () { Id = "1" };
            toplevel2 = new () { Id = "2" };

            app.Begin (toplevel1);
            Assert.Single (app.SessionStack);
            Assert.Same (toplevel1, app.TopRunnable);

            app.Begin (toplevel2);
            Assert.Equal (2, app.SessionStack.Count);
            Assert.Same (toplevel2, app.TopRunnable);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void Begin_SetsUniqueToplevelId_WhenIdIsEmpty ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        Toplevel? toplevel3 = null;

        try
        {
            toplevel1 = new ();
            toplevel2 = new ();
            toplevel3 = new ();

            Assert.Empty (toplevel1.Id);
            Assert.Empty (toplevel2.Id);
            Assert.Empty (toplevel3.Id);

            app.Begin (toplevel1);
            app.Begin (toplevel2);
            app.Begin (toplevel3);

            Assert.NotEmpty (toplevel1.Id);
            Assert.NotEmpty (toplevel2.Id);
            Assert.NotEmpty (toplevel3.Id);

            // IDs should be unique
            Assert.NotEqual (toplevel1.Id, toplevel2.Id);
            Assert.NotEqual (toplevel2.Id, toplevel3.Id);
            Assert.NotEqual (toplevel1.Id, toplevel3.Id);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            toplevel3?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_WithNullSessionToken_ThrowsArgumentNullException ()
    {
        IApplication app = NewApplicationImpl ();

        try
        {
            Assert.Throws<ArgumentNullException> (() => app.End ((SessionToken)null!));
        }
        finally
        {
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_PopsSessionStack ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;

        try
        {
            toplevel1 = new () { Id = "1" };
            toplevel2 = new () { Id = "2" };

            SessionToken token1 = app.Begin (toplevel1);
            SessionToken token2 = app.Begin (toplevel2);

            Assert.Equal (2, app.SessionStack.Count);

            app.End (token2);

            Assert.Single (app.SessionStack);
            Assert.Same (toplevel1, app.TopRunnable);

            app.End (token1);

            Assert.Empty (app.SessionStack);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_ThrowsArgumentException_WhenNotBalanced ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;

        try
        {
            toplevel1 = new () { Id = "1" };
            toplevel2 = new () { Id = "2" };

            SessionToken token1 = app.Begin (toplevel1);
            SessionToken token2 = app.Begin (toplevel2);

            // Trying to end token1 when token2 is on top should throw
            // NOTE: This throws but has the side effect of popping token2 from the stack
            Assert.Throws<ArgumentException> (() => app.End (token1));

            // Don't try to clean up with more End calls - the state is now inconsistent
            // Let Shutdown/ResetState handle cleanup
        }
        finally
        {
            // Dispose toplevels BEFORE Shutdown to satisfy DEBUG_IDISPOSABLE assertions
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();

            // Shutdown will call ResetState which clears any remaining state
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_RestoresCurrentToPreviousToplevel ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        Toplevel? toplevel3 = null;

        try
        {
            toplevel1 = new () { Id = "1" };
            toplevel2 = new () { Id = "2" };
            toplevel3 = new () { Id = "3" };

            SessionToken token1 = app.Begin (toplevel1);
            SessionToken token2 = app.Begin (toplevel2);
            SessionToken token3 = app.Begin (toplevel3);

            Assert.Same (toplevel3, app.TopRunnable);

            app.End (token3);
            Assert.Same (toplevel2, app.TopRunnable);

            app.End (token2);
            Assert.Same (toplevel1, app.TopRunnable);

            app.End (token1);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            toplevel3?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void MultipleBeginEnd_MaintainsStackIntegrity ()
    {
        IApplication app = NewApplicationImpl ();
        List<Toplevel> toplevels = new ();
        List<SessionToken> tokens = new ();

        try
        {
            // Begin multiple toplevels
            for (var i = 0; i < 5; i++)
            {
                var toplevel = new Toplevel { Id = $"toplevel-{i}" };
                toplevels.Add (toplevel);
                tokens.Add (app.Begin (toplevel));
            }

            Assert.Equal (5, app.SessionStack.Count);
            Assert.Same (toplevels [4], app.TopRunnable);

            // End them in reverse order (LIFO)
            for (var i = 4; i >= 0; i--)
            {
                app.End (tokens [i]);

                if (i > 0)
                {
                    Assert.Equal (i, app.SessionStack.Count);
                    Assert.Same (toplevels [i - 1], app.TopRunnable);
                }
                else
                {
                    Assert.Empty (app.SessionStack);
                }
            }
        }
        finally
        {
            foreach (Toplevel toplevel in toplevels)
            {
                toplevel.Dispose ();
            }

            app.Shutdown ();
        }
    }

    [Fact]
    public void End_UpdatesCachedSessionTokenToplevel ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel = null;

        try
        {
            toplevel = new ();

            SessionToken token = app.Begin (toplevel);
            Assert.Null (app.CachedSessionTokenToplevel);

            app.End (token);

            Assert.Same (toplevel, app.CachedSessionTokenToplevel);
        }
        finally
        {
            toplevel?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_NullsSessionTokenToplevel ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel = null;

        try
        {
            toplevel = new ();

            SessionToken token = app.Begin (toplevel);
            Assert.Same (toplevel, token.Toplevel);

            app.End (token);

            Assert.Null (token.Toplevel);
        }
        finally
        {
            toplevel?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void ResetState_ClearsSessionStack ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;

        try
        {
            toplevel1 = new () { Id = "1" };
            toplevel2 = new () { Id = "2" };

            app.Begin (toplevel1);
            app.Begin (toplevel2);

            Assert.Equal (2, app.SessionStack.Count);
            Assert.NotNull (app.TopRunnable);
        }
        finally
        {
            // Dispose toplevels BEFORE Shutdown to satisfy DEBUG_IDISPOSABLE assertions
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();

            // Shutdown calls ResetState, which will clear SessionStack and set Current to null
            app.Shutdown ();

            // Verify cleanup happened
            Assert.Empty (app.SessionStack);
            Assert.Null (app.TopRunnable);
            Assert.Null (app.CachedSessionTokenToplevel);
        }
    }

    [Fact]
    public void ResetState_StopsAllRunningToplevels ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;

        try
        {
            toplevel1 = new () { Id = "1", Running = true };
            toplevel2 = new () { Id = "2", Running = true };

            app.Begin (toplevel1);
            app.Begin (toplevel2);

            Assert.True (toplevel1.Running);
            Assert.True (toplevel2.Running);
        }
        finally
        {
            // Dispose toplevels BEFORE Shutdown to satisfy DEBUG_IDISPOSABLE assertions
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();

            // Shutdown calls ResetState, which will stop all running toplevels
            app.Shutdown ();

            // Verify toplevels were stopped
            Assert.False (toplevel1!.Running);
            Assert.False (toplevel2!.Running);
        }
    }

    [Fact]
    public void Begin_ActivatesNewToplevel_WhenCurrentExists ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;

        try
        {
            toplevel1 = new () { Id = "1" };
            toplevel2 = new () { Id = "2" };

            var toplevel1Deactivated = false;
            var toplevel2Activated = false;

            toplevel1.Deactivate += (s, e) => toplevel1Deactivated = true;
            toplevel2.Activate += (s, e) => toplevel2Activated = true;

            app.Begin (toplevel1);
            app.Begin (toplevel2);

            Assert.True (toplevel1Deactivated);
            Assert.True (toplevel2Activated);
            Assert.Same (toplevel2, app.TopRunnable);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void Begin_DoesNotDuplicateToplevel_WhenIdAlreadyExists ()
    {
        IApplication app = NewApplicationImpl ();
        Toplevel? toplevel = null;

        try
        {
            toplevel = new () { Id = "test-id" };

            app.Begin (toplevel);
            Assert.Single (app.SessionStack);

            // Calling Begin again with same toplevel should not duplicate
            app.Begin (toplevel);
            Assert.Single (app.SessionStack);
        }
        finally
        {
            toplevel?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void SessionStack_ContainsAllBegunToplevels ()
    {
        IApplication app = NewApplicationImpl ();
        List<Toplevel> toplevels = new ();

        try
        {
            for (var i = 0; i < 10; i++)
            {
                var toplevel = new Toplevel { Id = $"toplevel-{i}" };
                toplevels.Add (toplevel);
                app.Begin (toplevel);
            }

            // All toplevels should be in the stack
            Assert.Equal (10, app.SessionStack.Count);

            // Verify stack contains all toplevels
            List<Toplevel> stackList = app.SessionStack.ToList ();

            foreach (Toplevel toplevel in toplevels)
            {
                Assert.Contains (toplevel, stackList);
            }
        }
        finally
        {
            foreach (Toplevel toplevel in toplevels)
            {
                toplevel.Dispose ();
            }

            app.Shutdown ();
        }
    }
}
