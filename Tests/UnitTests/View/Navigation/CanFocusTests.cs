using UnitTests;

namespace Terminal.Gui.ViewTests;

public class CanFocusTests
{
    // TODO: Figure out what this test is supposed to be testing
    [Fact]
    public void CanFocus_Faced_With_Container_Before_Run ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window ();
        var f = new FrameView ();
        var v = new View { CanFocus = true };
        f.Add (v);
        w.Add (f);
        t.Add (w);

        Assert.True (t.CanFocus);
        Assert.True (w.CanFocus);
        Assert.True (f.CanFocus);
        Assert.True (v.CanFocus);

        f.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);

        v.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.False (v.CanFocus);

        v.CanFocus = true;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    //[Fact]
    //public void CanFocus_Set_Changes_TabIndex_And_TabStop ()
    //{
    //    var r = new View ();
    //    var v1 = new View { Text = "1" };
    //    var v2 = new View { Text = "2" };
    //    var v3 = new View { Text = "3" };

    //    r.Add (v1, v2, v3);

    //    v2.CanFocus = true;
    //    Assert.Equal (r.TabIndexes.IndexOf (v2), v2.TabIndex);
    //    Assert.Equal (0, v2.TabIndex);
    //    Assert.Equal (TabBehavior.TabStop, v2.TabStop);

    //    v1.CanFocus = true;
    //    Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
    //    Assert.Equal (1, v1.TabIndex);
    //    Assert.Equal (TabBehavior.TabStop, v1.TabStop);

    //    v1.TabIndex = 2;
    //    Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
    //    Assert.Equal (1, v1.TabIndex);
    //    v3.CanFocus = true;
    //    Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
    //    Assert.Equal (1, v1.TabIndex);
    //    Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
    //    Assert.Equal (2, v3.TabIndex);
    //    Assert.Equal (TabBehavior.TabStop, v3.TabStop);

    //    v2.CanFocus = false;
    //    Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
    //    Assert.Equal (1, v1.TabIndex);
    //    Assert.Equal (TabBehavior.TabStop, v1.TabStop);
    //    Assert.Equal (r.TabIndexes.IndexOf (v2), v2.TabIndex); // TabIndex is not changed
    //    Assert.NotEqual (-1, v2.TabIndex);
    //    Assert.Equal (TabBehavior.TabStop, v2.TabStop); // TabStop is not changed
    //    Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
    //    Assert.Equal (2, v3.TabIndex);
    //    Assert.Equal (TabBehavior.TabStop, v3.TabStop);
    //    r.Dispose ();
    //}

    [Fact]
    public void CanFocus_Set_True_Get_AdvanceFocus_Works ()
    {
        Label label = new () { Text = "label" };
        View view = new () { Text = "view", CanFocus = true };
        Application.Navigation = new ();
        Application.Top = new ();
        Application.Top.Add (label, view);

        Application.Top.SetFocus ();
        Assert.Equal (view, Application.Navigation.GetFocused ());
        Assert.False (label.CanFocus);
        Assert.False (label.HasFocus);
        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);

        Assert.False (Application.Navigation.AdvanceFocus (NavigationDirection.Forward, null));
        Assert.False (label.HasFocus);
        Assert.True (view.HasFocus);

        // Set label CanFocus to true
        label.CanFocus = true;
        Assert.False (label.HasFocus);
        Assert.True (view.HasFocus);

        // label can now be focused, so AdvanceFocus should move to it.
        Assert.True (Application.Navigation.AdvanceFocus (NavigationDirection.Forward, null));
        Assert.True (label.HasFocus);
        Assert.False (view.HasFocus);

        // Move back to view
        view.SetFocus ();
        Assert.False (label.HasFocus);
        Assert.True (view.HasFocus);

        Assert.True (Application.RaiseKeyDownEvent (Key.Tab));
        Assert.True (label.HasFocus);
        Assert.False (view.HasFocus);

        Application.Top.Dispose ();
        Application.ResetState ();
    }
}
