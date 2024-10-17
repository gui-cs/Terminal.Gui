using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests.NavigationTests;

public class ApplicationNavigationTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (TabBehavior.NoStop)]
    [InlineData (TabBehavior.TabStop)]
    [InlineData (TabBehavior.TabGroup)]
    public void Begin_SetsFocus_On_Deepest_Focusable_View (TabBehavior behavior)
    {
        Application.Init (new FakeDriver ());

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

        RunState rs = Application.Begin (top);
        Assert.True (top.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);

        top.Dispose ();

        Application.Shutdown ();
    }

    [Fact]
    public void Begin_SetsFocus_On_Top ()
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel ();
        Assert.False (top.HasFocus);

        RunState rs = Application.Begin (top);
        Assert.True (top.HasFocus);

        top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void Focused_Change_Raises_FocusedChanged ()
    {
        var raised = false;

        Application.Navigation = new ();

        Application.Navigation.FocusedChanged += ApplicationNavigationOnFocusedChanged;

        Application.Navigation.SetFocused (new () { CanFocus = true, HasFocus = true });

        Assert.True (raised);

        Application.Navigation.GetFocused ().Dispose ();
        Application.Navigation.SetFocused (null);

        Application.Navigation.FocusedChanged -= ApplicationNavigationOnFocusedChanged;

        Application.Navigation = null;

        return;

        void ApplicationNavigationOnFocusedChanged (object sender, EventArgs e) { raised = true; }
    }

    [Fact]
    public void GetFocused_Returns_Focused_View ()
    {
        Application.Navigation = new ();

        Application.Top = new ()
        {
            Id = "top",
            CanFocus = true
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
        Application.Top.Add (subView1, subView2);
        Assert.False (Application.Top.HasFocus);

        Application.Top.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal (subView1, Application.Navigation.GetFocused ());

        Application.Navigation.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.Equal (subView2, Application.Navigation.GetFocused ());

        Application.Top.Dispose ();
        Application.ResetState ();
    }

    [Fact]
    public void GetFocused_Returns_Null_If_No_Focused_View ()
    {
        Application.Navigation = new ();

        Application.Top = new ()
        {
            Id = "top",
            CanFocus = true
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        Application.Top.Add (subView1);
        Assert.False (Application.Top.HasFocus);

        Application.Top.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal (subView1, Application.Navigation.GetFocused ());

        subView1.HasFocus = false;
        Assert.False (subView1.HasFocus);
        Assert.True (Application.Top.HasFocus);
        Assert.Equal (Application.Top, Application.Navigation.GetFocused ());

        Application.Top.HasFocus = false;
        Assert.False (Application.Top.HasFocus);
        Assert.Null (Application.Navigation.GetFocused ());

        Application.Top.Dispose ();
        Application.ResetState ();
    }
}
