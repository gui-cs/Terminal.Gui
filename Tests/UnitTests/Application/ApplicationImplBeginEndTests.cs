#nullable enable
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.ApplicationTests;

/// <summary>
/// Comprehensive tests for ApplicationImpl.Begin/End logic that manages Current and SessionStack.
/// These tests ensure the fragile state management logic is robust and catches regressions.
/// Tests work directly with ApplicationImpl instances to avoid global Application state issues.
/// </summary>
public class ApplicationImplBeginEndTests
{
    private readonly ITestOutputHelper _output;

    public ApplicationImplBeginEndTests (ITestOutputHelper output)
    {
        _output = output;
    }

    private ApplicationImpl NewApplicationImpl ()
    {
        var app = new ApplicationImpl ();
        return app;
    }

    [Fact]
    public void Begin_WithNullToplevel_ThrowsArgumentNullException ()
    {
        ApplicationImpl app = NewApplicationImpl ();
        try
        {
            Assert.Throws<ArgumentNullException> (() => app.Begin (null!));
        }
        finally
        {
            app.Shutdown ();
        }
    }

    [Fact]
    public void Begin_SetsCurrent_WhenCurrentIsNull ()
    {
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel = null;
        
        try
        {
            toplevel = new Toplevel ();
            Assert.Null (app.Current);
            
            app.Begin (toplevel);
            
            Assert.NotNull (app.Current);
            Assert.Same (toplevel, app.Current);
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
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        
        try
        {
            toplevel1 = new Toplevel { Id = "1" };
            toplevel2 = new Toplevel { Id = "2" };
            
            app.Begin (toplevel1);
            Assert.Single (app.SessionStack);
            Assert.Same (toplevel1, app.Current);
            
            app.Begin (toplevel2);
            Assert.Equal (2, app.SessionStack.Count);
            Assert.Same (toplevel2, app.Current);
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
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        Toplevel? toplevel3 = null;
        
        try
        {
            toplevel1 = new Toplevel ();
            toplevel2 = new Toplevel ();
            toplevel3 = new Toplevel ();
            
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
        ApplicationImpl app = NewApplicationImpl ();
        try
        {
            Assert.Throws<ArgumentNullException> (() => app.End (null!));
        }
        finally
        {
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_PopsSessionStack ()
    {
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        
        try
        {
            toplevel1 = new Toplevel { Id = "1" };
            toplevel2 = new Toplevel { Id = "2" };
            
            SessionToken token1 = app.Begin (toplevel1);
            SessionToken token2 = app.Begin (toplevel2);
            
            Assert.Equal (2, app.SessionStack.Count);
            
            app.End (token2);
            
            Assert.Single (app.SessionStack);
            Assert.Same (toplevel1, app.Current);
            
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
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        
        try
        {
            toplevel1 = new Toplevel { Id = "1" };
            toplevel2 = new Toplevel { Id = "2" };
            
            SessionToken token1 = app.Begin (toplevel1);
            SessionToken token2 = app.Begin (toplevel2);
            
            // Trying to end token1 when token2 is on top should throw
            Assert.Throws<ArgumentException> (() => app.End (token1));
            
            // Cleanup
            app.End (token2);
            app.End (token1);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_RestoresCurrentToPreviousToplevel ()
    {
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        Toplevel? toplevel3 = null;
        
        try
        {
            toplevel1 = new Toplevel { Id = "1" };
            toplevel2 = new Toplevel { Id = "2" };
            toplevel3 = new Toplevel { Id = "3" };
            
            SessionToken token1 = app.Begin (toplevel1);
            SessionToken token2 = app.Begin (toplevel2);
            SessionToken token3 = app.Begin (toplevel3);
            
            Assert.Same (toplevel3, app.Current);
            
            app.End (token3);
            Assert.Same (toplevel2, app.Current);
            
            app.End (token2);
            Assert.Same (toplevel1, app.Current);
            
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
        ApplicationImpl app = NewApplicationImpl ();
        var toplevels = new List<Toplevel> ();
        var tokens = new List<SessionToken> ();
        
        try
        {
            
            // Begin multiple toplevels
            for (int i = 0; i < 5; i++)
            {
                var toplevel = new Toplevel { Id = $"toplevel-{i}" };
                toplevels.Add (toplevel);
                tokens.Add (app.Begin (toplevel));
            }
            
            Assert.Equal (5, app.SessionStack.Count);
            Assert.Same (toplevels [4], app.Current);
            
            // End them in reverse order (LIFO)
            for (int i = 4; i >= 0; i--)
            {
                app.End (tokens [i]);
                
                if (i > 0)
                {
                    Assert.Equal (i, app.SessionStack.Count);
                    Assert.Same (toplevels [i - 1], app.Current);
                }
                else
                {
                    Assert.Empty (app.SessionStack);
                }
            }
        }
        finally
        {
            foreach (var toplevel in toplevels)
            {
                toplevel.Dispose ();
            }
            app.Shutdown ();
        }
    }

    [Fact]
    public void End_UpdatesCachedSessionTokenToplevel ()
    {
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel = null;
        
        try
        {
            toplevel = new Toplevel ();
            
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
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel = null;
        
        try
        {
            toplevel = new Toplevel ();
            
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
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        
        try
        {
            toplevel1 = new Toplevel { Id = "1" };
            toplevel2 = new Toplevel { Id = "2" };
            
            app.Begin (toplevel1);
            app.Begin (toplevel2);
            
            Assert.Equal (2, app.SessionStack.Count);
            Assert.NotNull (app.Current);
            
            app.ResetState ();
            
            Assert.Empty (app.SessionStack);
            Assert.Null (app.Current);
            Assert.Null (app.CachedSessionTokenToplevel);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void ResetState_StopsAllRunningToplevels ()
    {
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        
        try
        {
            toplevel1 = new Toplevel { Id = "1", Running = true };
            toplevel2 = new Toplevel { Id = "2", Running = true };
            
            app.Begin (toplevel1);
            app.Begin (toplevel2);
            
            Assert.True (toplevel1.Running);
            Assert.True (toplevel2.Running);
            
            app.ResetState ();
            
            Assert.False (toplevel1.Running);
            Assert.False (toplevel2.Running);
        }
        finally
        {
            toplevel1?.Dispose ();
            toplevel2?.Dispose ();
            app.Shutdown ();
        }
    }

    [Fact]
    public void Begin_ActivatesNewToplevel_WhenCurrentExists ()
    {
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel1 = null;
        Toplevel? toplevel2 = null;
        
        try
        {
            toplevel1 = new Toplevel { Id = "1" };
            toplevel2 = new Toplevel { Id = "2" };
            
            bool toplevel1Deactivated = false;
            bool toplevel2Activated = false;
            
            toplevel1.Deactivate += (s, e) => toplevel1Deactivated = true;
            toplevel2.Activate += (s, e) => toplevel2Activated = true;
            
            app.Begin (toplevel1);
            app.Begin (toplevel2);
            
            Assert.True (toplevel1Deactivated);
            Assert.True (toplevel2Activated);
            Assert.Same (toplevel2, app.Current);
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
        ApplicationImpl app = NewApplicationImpl ();
        Toplevel? toplevel = null;
        
        try
        {
            toplevel = new Toplevel { Id = "test-id" };
            
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
        ApplicationImpl app = NewApplicationImpl ();
        var toplevels = new List<Toplevel> ();
        
        try
        {
            
            for (int i = 0; i < 10; i++)
            {
                var toplevel = new Toplevel { Id = $"toplevel-{i}" };
                toplevels.Add (toplevel);
                app.Begin (toplevel);
            }
            
            // All toplevels should be in the stack
            Assert.Equal (10, app.SessionStack.Count);
            
            // Verify stack contains all toplevels
            var stackList = app.SessionStack.ToList ();
            foreach (var toplevel in toplevels)
            {
                Assert.Contains (toplevel, stackList);
            }
        }
        finally
        {
            foreach (var toplevel in toplevels)
            {
                toplevel.Dispose ();
            }
            app.Shutdown ();
        }
    }
}
