using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ApplicationTests;

public class ApplicationNavigationTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [AutoInitShutdown]
    [Theory]
    [InlineData (TabBehavior.NoStop)]
    [InlineData (TabBehavior.TabStop)]
    [InlineData (TabBehavior.TabGroup)]
    public void Begin_SetsFocus_On_Deepest_Focusable_View (TabBehavior behavior)
    {
        var top = new Toplevel
        {
            TabStop = behavior
        };
        Assert.False (top.HasFocus);

        View subView = new ()
        {
            CanFocus = true,
            TabStop = behavior
        };
        top.Add (subView);

        View subSubView = new ()
        {
            CanFocus = true,
            TabStop = TabBehavior.NoStop
        };
        subView.Add (subSubView);

        SessionToken rs = Application.Begin (top);
        Assert.True (top.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Begin_SetsFocus_On_Top ()
    {
        var top = new Toplevel ();
        Assert.False (top.HasFocus);

        SessionToken rs = Application.Begin (top);
        Assert.True (top.HasFocus);

        top.Dispose ();
    }

    [Fact]
    public void Focused_Change_Raises_FocusedChanged ()
    {
        var raised = false;

        Application.Navigation!.FocusedChanged += ApplicationNavigationOnFocusedChanged;

        Application.Navigation.SetFocused (new () { CanFocus = true, HasFocus = true });

        Assert.True (raised);

        Application.Navigation.GetFocused ().Dispose ();
        Application.Navigation.SetFocused (null);

        Application.Navigation.FocusedChanged -= ApplicationNavigationOnFocusedChanged;

        return;

        void ApplicationNavigationOnFocusedChanged (object sender, EventArgs e) { raised = true; }
    }

    [Fact]
    public void GetFocused_Returns_Focused_View ()
    {
        IApplication app = ApplicationImpl.Instance;

        app.TopRunnableView = new ()
        {
            Id = "top",
            CanFocus = true,
            App = app
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = true
        };

        app.TopRunnableView?.Add (subView1, subView2);
        Assert.False (app.TopRunnableView?.HasFocus);

        app.TopRunnableView?.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal (subView1, app.Navigation?.GetFocused ());

        app.Navigation?.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.Equal (subView2, app.Navigation?.GetFocused ());
    }

    [Fact]
    public void GetFocused_Returns_Null_If_No_Focused_View ()
    {
        IApplication app = ApplicationImpl.Instance; // Force legacy

        app.TopRunnableView = new ()
        {
            Id = "top",
            CanFocus = true,
            App = app
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        app!.TopRunnableView.Add (subView1);
        Assert.False (app.TopRunnableView.HasFocus);

        app.TopRunnableView.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal (subView1, app.Navigation!.GetFocused ());

        subView1.HasFocus = false;
        Assert.False (subView1.HasFocus);
        Assert.True (app.TopRunnableView.HasFocus);
        Assert.Equal (app.TopRunnableView, app.Navigation.GetFocused ());

        app.TopRunnableView.HasFocus = false;
        Assert.False (app.TopRunnableView.HasFocus);
        Assert.Null (app.Navigation.GetFocused ());

    }
}
