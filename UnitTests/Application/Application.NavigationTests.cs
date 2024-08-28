using System.Diagnostics;
using Moq;
using Xunit.Abstractions;
using Terminal.Gui;
using Terminal.Gui.ViewTests;

namespace Terminal.Gui.ApplicationTests.NavigationTests;

public class ApplicationNavigationTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Focused_Change_Raises_FocusedChanged ()
    {
        bool raised = false;

        Application.Navigation = new ApplicationNavigation ();

        Application.Navigation.FocusedChanged += ApplicationNavigationOnFocusedChanged;

        Application.Navigation.SetFocused (new View ());

        Assert.True (raised);

        Application.Navigation.GetFocused ().Dispose ();
        Application.Navigation.SetFocused (null);

        Application.Navigation.FocusedChanged -= ApplicationNavigationOnFocusedChanged;

        Application.Navigation = null;

        return;

        void ApplicationNavigationOnFocusedChanged (object sender, EventArgs e) { raised = true; }
    }

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnNull_WhenViewIsNull ()
    {
        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (null);

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnSameView_WhenNoSubviewsHaveFocus ()
    {
        // Arrange
        var view = new View () { Id = "view", CanFocus = true };
        ;

        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (view);

        // Assert
        Assert.Equal (view, result);
    }

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnFocusedSubview ()
    {
        // Arrange
        var parentView = new View () { Id = "parentView", CanFocus = true };
        ;
        var childView1 = new View () { Id = "childView1", CanFocus = true };
        ;
        var childView2 = new View () { Id = "childView2", CanFocus = true };
        ;
        var grandChildView = new View () { Id = "grandChildView", CanFocus = true };
        ;

        parentView.Add (childView1, childView2);
        childView2.Add (grandChildView);

        grandChildView.SetFocus ();

        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (parentView);

        // Assert
        Assert.Equal (grandChildView, result);
    }

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnDeepestFocusedSubview ()
    {
        // Arrange
        var parentView = new View () { Id = "parentView", CanFocus = true };
        ;
        var childView1 = new View () { Id = "childView1", CanFocus = true };
        ;
        var childView2 = new View () { Id = "childView2", CanFocus = true };
        ;
        var grandChildView = new View () { Id = "grandChildView", CanFocus = true };
        ;
        var greatGrandChildView = new View () { Id = "greatGrandChildView", CanFocus = true };
        ;

        parentView.Add (childView1, childView2);
        childView2.Add (grandChildView);
        grandChildView.Add (greatGrandChildView);

        grandChildView.SetFocus ();

        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (parentView);

        // Assert
        Assert.Equal (greatGrandChildView, result);

        // Arrange
        greatGrandChildView.CanFocus = false;
        grandChildView.SetFocus ();

        // Act
        result = ApplicationNavigation.GetDeepestFocusedSubview (parentView);

        // Assert
        Assert.Equal (grandChildView, result);
    }

    [Fact]
    public void GetFocused_Returns_Null_If_No_Focused_View ()
    {
        Application.Navigation = new ();

        Application.Current = new Toplevel()
        {
            Id = "top",
            CanFocus = true
        };

        View subView1 = new View ()
        {
            Id = "subView1",
            CanFocus = true
        };

        Application.Current.Add (subView1);
        Assert.False (Application.Current.HasFocus);

        Application.Current.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal (subView1, Application.Navigation.GetFocused ());

        subView1.HasFocus = false;
        Assert.False (subView1.HasFocus);
        Assert.True (Application.Current.HasFocus);
        Assert.Equal (Application.Current, Application.Navigation.GetFocused ());

        Application.Current.HasFocus = false;
        Assert.False (Application.Current.HasFocus);
        Assert.Null (Application.Navigation.GetFocused ());

        Application.ResetState ();
    }


    [Fact]
    public void GetFocused_Returns_Focused_View ()
    {
        Application.Navigation = new ();

        Application.Current = new Toplevel ()
        {
            Id = "top",
            CanFocus = true
        };

        View subView1 = new View ()
        {
            Id = "subView1",
            CanFocus = true
        };

        View subView2 = new View ()
        {
            Id = "subView2",
            CanFocus = true
        };
        Application.Current.Add (subView1, subView2);
        Assert.False (Application.Current.HasFocus);

        Application.Current.SetFocus ();
        Assert.True (subView1.HasFocus);
        Assert.Equal(subView1, Application.Navigation.GetFocused());

        Application.Current.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.Equal (subView2, Application.Navigation.GetFocused ());

        Application.ResetState ();
    }

    [Fact]
    public void Begin_SetsFocus_On_Top ()
    {
        Application.Init(new FakeDriver());

        var top = new Toplevel ();
        Assert.False (top.HasFocus);

        RunState rs = Application.Begin (top);
        Assert.True (top.HasFocus);

        top.Dispose ();
        Application.Shutdown();
    }

    [Theory]
    [InlineData(TabBehavior.NoStop)]
    [InlineData (TabBehavior.TabStop)]
    [InlineData (TabBehavior.TabGroup)]
    public void Begin_SetsFocus_On_Deepest_Focusable_View (TabBehavior behavior)
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel ()
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
}
