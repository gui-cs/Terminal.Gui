// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ApplicationTests;

/// <summary>These tests focus on Application.RunState and the various ways it can be changed.</summary>
public class RunStateTests
{
    public RunStateTests ()
    {
#if DEBUG_IDISPOSABLE
        Responder.Instances.Clear ();
#endif
    }

    [Fact]
    public void Begin_End_Cleans_Up_RunState ()
    {
        // Setup Mock driver
        Init ();

        // Test null Toplevel
        Assert.Throws<ArgumentNullException> (() => Application.Begin (null));

        var top = new Toplevel ();
        RunState rs = Application.Begin (top);
        Assert.NotNull (rs);
        Assert.Equal (top, Application.Current);
        Application.End (rs);

        Assert.Null (Application.Current);
        Assert.NotNull (Application.Top);
        Assert.NotNull (Application.MainLoop);
        Assert.NotNull (Application.Driver);

        top.Dispose ();
        Shutdown ();

        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    public void New_Creates_RunState ()
    {
        RunState rs = new (null!);
        Assert.Null (rs.Toplevel);

        var top = new Toplevel ();
        rs = new (top);
        Assert.Equal (top, rs.Toplevel);
    }

    private void Init ()
    {
        Application.Init (new FakeDriver ());
        Assert.NotNull (Application.Driver);
        Assert.NotNull (Application.MainLoop);
        Assert.NotNull (SynchronizationContext.Current);
    }

    private void Shutdown ()
    {
        Application.Shutdown ();
    }
}
