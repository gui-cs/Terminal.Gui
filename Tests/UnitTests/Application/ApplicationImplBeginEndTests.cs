using Xunit;
using Xunit.Abstractions;

namespace UnitTests.ApplicationTests;

/// <summary>
/// Comprehensive tests for ApplicationImpl.Begin/End logic that manages Current and SessionStack.
/// These tests ensure the fragile state management logic is robust and catches regressions.
/// </summary>
[Collection ("Sequential")]  // Ensures tests run sequentially, not in parallel
public class ApplicationImplBeginEndTests : IDisposable
{
    private readonly ITestOutputHelper _output;

    public ApplicationImplBeginEndTests (ITestOutputHelper output)
    {
        _output = output;
        // Ensure clean state before each test
        if (Application.Initialized)
        {
            Application.Shutdown ();
        }
    }

    public void Dispose ()
    {
        // Ensure Application is shutdown after each test
        if (Application.Initialized)
        {
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Begin_WithNullToplevel_ThrowsArgumentNullException ()
    {
        Application.Init (driverName: "fake");
        Assert.Throws<ArgumentNullException> (() => Application.Begin (null!));
    }

    [Fact]
    public void Begin_SetsCurrent_WhenCurrentIsNull ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel = new Toplevel ();
        Assert.Null (Application.Current);
        
        Application.Begin (toplevel);
        
        Assert.NotNull (Application.Current);
        Assert.Same (toplevel, Application.Current);
        Assert.Single (Application.SessionStack);
        
        toplevel.Dispose ();
    }

    [Fact]
    public void Begin_PushesToSessionStack ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel { Id = "1" };
        var toplevel2 = new Toplevel { Id = "2" };
        
        Application.Begin (toplevel1);
        Assert.Single (Application.SessionStack);
        Assert.Same (toplevel1, Application.Current);
        
        Application.Begin (toplevel2);
        Assert.Equal (2, Application.SessionStack.Count);
        Assert.Same (toplevel2, Application.Current);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
    }

    [Fact]
    public void Begin_SetsUniqueToplevelId_WhenIdIsEmpty ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel ();
        var toplevel2 = new Toplevel ();
        var toplevel3 = new Toplevel ();
        
        Assert.Empty (toplevel1.Id);
        Assert.Empty (toplevel2.Id);
        Assert.Empty (toplevel3.Id);
        
        Application.Begin (toplevel1);
        Application.Begin (toplevel2);
        Application.Begin (toplevel3);
        
        Assert.NotEmpty (toplevel1.Id);
        Assert.NotEmpty (toplevel2.Id);
        Assert.NotEmpty (toplevel3.Id);
        
        // IDs should be unique
        Assert.NotEqual (toplevel1.Id, toplevel2.Id);
        Assert.NotEqual (toplevel2.Id, toplevel3.Id);
        Assert.NotEqual (toplevel1.Id, toplevel3.Id);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
        toplevel3.Dispose ();
    }

    [Fact]
    public void End_WithNullSessionToken_ThrowsArgumentNullException ()
    {
        Application.Init (driverName: "fake");
        Assert.Throws<ArgumentNullException> (() => Application.End (null!));
    }

    [Fact]
    public void End_PopsSessionStack ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel { Id = "1" };
        var toplevel2 = new Toplevel { Id = "2" };
        
        SessionToken token1 = Application.Begin (toplevel1);
        SessionToken token2 = Application.Begin (toplevel2);
        
        Assert.Equal (2, Application.SessionStack.Count);
        
        Application.End (token2);
        
        Assert.Single (Application.SessionStack);
        Assert.Same (toplevel1, Application.Current);
        
        Application.End (token1);
        
        Assert.Empty (Application.SessionStack);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
    }

    [Fact]
    public void End_ThrowsArgumentException_WhenNotBalanced ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel { Id = "1" };
        var toplevel2 = new Toplevel { Id = "2" };
        
        SessionToken token1 = Application.Begin (toplevel1);
        SessionToken token2 = Application.Begin (toplevel2);
        
        // Trying to end token1 when token2 is on top should throw
        Assert.Throws<ArgumentException> (() => Application.End (token1));
        
        // Cleanup
        Application.End (token2);
        Application.End (token1);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
    }

    [Fact]
    public void End_RestoresCurrentToPreviousToplevel ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel { Id = "1" };
        var toplevel2 = new Toplevel { Id = "2" };
        var toplevel3 = new Toplevel { Id = "3" };
        
        SessionToken token1 = Application.Begin (toplevel1);
        SessionToken token2 = Application.Begin (toplevel2);
        SessionToken token3 = Application.Begin (toplevel3);
        
        Assert.Same (toplevel3, Application.Current);
        
        Application.End (token3);
        Assert.Same (toplevel2, Application.Current);
        
        Application.End (token2);
        Assert.Same (toplevel1, Application.Current);
        
        Application.End (token1);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
        toplevel3.Dispose ();
    }

    [Fact]
    public void MultipleBeginEnd_MaintainsStackIntegrity ()
    {
        Application.Init (driverName: "fake");
        
        var toplevels = new List<Toplevel> ();
        var tokens = new List<SessionToken> ();
        
        // Begin multiple toplevels
        for (int i = 0; i < 5; i++)
        {
            var toplevel = new Toplevel { Id = $"toplevel-{i}" };
            toplevels.Add (toplevel);
            tokens.Add (Application.Begin (toplevel));
        }
        
        Assert.Equal (5, Application.SessionStack.Count);
        Assert.Same (toplevels [4], Application.Current);
        
        // End them in reverse order (LIFO)
        for (int i = 4; i >= 0; i--)
        {
            Application.End (tokens [i]);
            
            if (i > 0)
            {
                Assert.Equal (i, Application.SessionStack.Count);
                Assert.Same (toplevels [i - 1], Application.Current);
            }
            else
            {
                Assert.Empty (Application.SessionStack);
            }
        }
        
        foreach (var toplevel in toplevels)
        {
            toplevel.Dispose ();
        }
    }

    [Fact]
    public void End_UpdatesCachedSessionTokenToplevel ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel = new Toplevel ();
        
        SessionToken token = Application.Begin (toplevel);
        Assert.Null (ApplicationImpl.Instance.CachedSessionTokenToplevel);
        
        Application.End (token);
        
        Assert.Same (toplevel, ApplicationImpl.Instance.CachedSessionTokenToplevel);
        
        toplevel.Dispose ();
    }

    [Fact]
    public void End_NullsSessionTokenToplevel ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel = new Toplevel ();
        
        SessionToken token = Application.Begin (toplevel);
        Assert.Same (toplevel, token.Toplevel);
        
        Application.End (token);
        
        Assert.Null (token.Toplevel);
        
        toplevel.Dispose ();
    }

    [Fact]
    public void ResetState_ClearsSessionStack ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel { Id = "1" };
        var toplevel2 = new Toplevel { Id = "2" };
        
        Application.Begin (toplevel1);
        Application.Begin (toplevel2);
        
        Assert.Equal (2, Application.SessionStack.Count);
        Assert.NotNull (Application.Current);
        
        ApplicationImpl.Instance.ResetState ();
        
        Assert.Empty (Application.SessionStack);
        Assert.Null (Application.Current);
        Assert.Null (ApplicationImpl.Instance.CachedSessionTokenToplevel);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
    }

    [Fact]
    public void ResetState_StopsAllRunningToplevels ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel { Id = "1", Running = true };
        var toplevel2 = new Toplevel { Id = "2", Running = true };
        
        Application.Begin (toplevel1);
        Application.Begin (toplevel2);
        
        Assert.True (toplevel1.Running);
        Assert.True (toplevel2.Running);
        
        ApplicationImpl.Instance.ResetState ();
        
        Assert.False (toplevel1.Running);
        Assert.False (toplevel2.Running);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
    }

    [Fact]
    public void Begin_ActivatesNewToplevel_WhenCurrentExists ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel1 = new Toplevel { Id = "1" };
        var toplevel2 = new Toplevel { Id = "2" };
        
        bool toplevel1Deactivated = false;
        bool toplevel2Activated = false;
        
        toplevel1.Deactivate += (s, e) => toplevel1Deactivated = true;
        toplevel2.Activate += (s, e) => toplevel2Activated = true;
        
        Application.Begin (toplevel1);
        Application.Begin (toplevel2);
        
        Assert.True (toplevel1Deactivated);
        Assert.True (toplevel2Activated);
        Assert.Same (toplevel2, Application.Current);
        
        toplevel1.Dispose ();
        toplevel2.Dispose ();
    }

    [Fact]
    public void Begin_DoesNotDuplicateToplevel_WhenIdAlreadyExists ()
    {
        Application.Init (driverName: "fake");
        
        var toplevel = new Toplevel { Id = "test-id" };
        
        Application.Begin (toplevel);
        Assert.Single (Application.SessionStack);
        
        // Calling Begin again with same toplevel should not duplicate
        Application.Begin (toplevel);
        Assert.Single (Application.SessionStack);
        
        toplevel.Dispose ();
    }

    [Fact]
    public void SessionStack_ContainsAllBegunToplevels ()
    {
        Application.Init (driverName: "fake");
        
        var toplevels = new List<Toplevel> ();
        
        for (int i = 0; i < 10; i++)
        {
            var toplevel = new Toplevel { Id = $"toplevel-{i}" };
            toplevels.Add (toplevel);
            Application.Begin (toplevel);
        }
        
        // All toplevels should be in the stack
        Assert.Equal (10, Application.SessionStack.Count);
        
        // Verify stack contains all toplevels
        var stackList = Application.SessionStack.ToList ();
        foreach (var toplevel in toplevels)
        {
            Assert.Contains (toplevel, stackList);
        }
        
        foreach (var toplevel in toplevels)
        {
            toplevel.Dispose ();
        }
    }
}
