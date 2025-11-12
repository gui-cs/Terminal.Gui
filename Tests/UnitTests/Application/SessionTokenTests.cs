
namespace UnitTests.ApplicationTests;

/// <summary>These tests focus on Application.SessionToken and the various ways it can be changed.</summary>
public class SessionTokenTests
{
    public SessionTokenTests ()
    {
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;

        View.Instances.Clear ();
        SessionToken.Instances.Clear ();
#endif
    }

    [Fact]
    [AutoInitShutdown]
    public void Begin_End_Cleans_Up_SessionToken ()
    {
        // Test null Toplevel
        Assert.Throws<ArgumentNullException> (() => Application.Begin (null));

        var top = new Toplevel ();
        SessionToken rs = Application.Begin (top);
        Assert.NotNull (rs);
        Application.End (rs);

        Assert.NotNull (Application.Top);

        // v2 does not use main loop, it uses MainLoop<T> and its internal
        //Assert.NotNull (Application.MainLoop);
        Assert.NotNull (Application.Driver);

        top.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (rs.WasDisposed);
#endif
    }

    [Fact]
    public void Dispose_Cleans_Up_SessionToken ()
    {
        var rs = new SessionToken (null);
        Assert.NotNull (rs);

        // Should not throw because Toplevel was null
        rs.Dispose ();
#if DEBUG_IDISPOSABLE
        Assert.True (rs.WasDisposed);
#endif
        var top = new Toplevel ();
        rs = new (top);
        Assert.NotNull (rs);

        // Should throw because Toplevel was not cleaned up
        Assert.Throws<InvalidOperationException> (() => rs.Dispose ());

        rs.Toplevel.Dispose ();
        rs.Toplevel = null;
        rs.Dispose ();
#if DEBUG_IDISPOSABLE
        Assert.True (rs.WasDisposed);
        Assert.True (top.WasDisposed);
#endif
    }

    [Fact]
    public void New_Creates_SessionToken ()
    {
        var rs = new SessionToken (null);
        Assert.Null (rs.Toplevel);

        var top = new Toplevel ();
        rs = new (top);
        Assert.Equal (top, rs.Toplevel);
    }
}
