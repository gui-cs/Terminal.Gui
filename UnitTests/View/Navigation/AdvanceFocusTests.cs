using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdvanceFocusTests (ITestOutputHelper _output)
{
    [Fact]
    public void AdvanceFocus_CanFocus_Mixed ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = false, TabStop = TabBehavior.TabStop };
        var v3 = new View { CanFocus = false, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Theory]
    [CombinatorialData]
    public void AdvanceFocus_Change_CanFocus_Works ([CombinatorialValues (TabBehavior.NoStop, TabBehavior.TabStop, TabBehavior.TabGroup)] TabBehavior behavior)
    {
        var r = new View { CanFocus = true };
        var v1 = new View ();
        var v2 = new View ();
        var v3 = new View ();
        Assert.True (r.CanFocus);
        Assert.False (v1.CanFocus);
        Assert.False (v2.CanFocus);
        Assert.False (v3.CanFocus);

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v1.CanFocus = true;
        v1.TabStop = behavior;
        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v2.CanFocus = true;
        v2.TabStop = behavior;
        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v3.CanFocus = true;
        v3.TabStop = behavior;
        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_Compound_Subview ()
    {
        var top = new View { Id = "top", CanFocus = true };

        var compoundSubview = new View
        {
            CanFocus = true,
            Id = "compoundSubview"
        };
        var v1 = new View { Id = "v1", CanFocus = true };
        var v2 = new View { Id = "v2", CanFocus = true };
        var v3 = new View { Id = "v3", CanFocus = false };

        compoundSubview.Add (v1, v2, v3);

        top.Add (compoundSubview);

        // Cycle through v1 & v2
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        // Add another subview
        View otherSubview = new ()
        {
            CanFocus = true,
            Id = "otherSubview"
        };

        top.Add (otherSubview);

        // Adding a focusable subview causes advancefocus
        Assert.True (otherSubview.HasFocus);
        Assert.False (v1.HasFocus);

        // Cycle through v1 & v2
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        Assert.True (otherSubview.HasFocus);

        // v2 was previously focused down the compoundSubView focus chain
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);

        top.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_NoStop_And_CanFocus_True_No_Focus ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v3 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_NoStop_Change_Enables_Stop ()
    {
        var r = new View { CanFocus = true };
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v3 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        v1.TabStop = TabBehavior.TabStop;
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v2.TabStop = TabBehavior.TabStop;
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v3.TabStop = TabBehavior.TabStop;
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_NoStop_Prevents_Stop ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v3 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
    }

    [Fact]
    public void AdvanceFocus_Null_And_CanFocus_False_No_Advance ()
    {
        var r = new View ();
        var v1 = new View ();
        var v2 = new View ();
        var v3 = new View ();
        Assert.False (v1.CanFocus);
        Assert.Null (v1.TabStop);

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_Subviews_Raises_HasFocusChanged ()
    {
        var top = new View
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
        top.Add (subView1, subView2);

        var subView1HasFocusChangedTrueCount = 0;
        var subView1HasFocusChangedFalseCount = 0;

        subView1.HasFocusChanged += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subView1HasFocusChangedTrueCount++;
                                        }
                                        else
                                        {
                                            subView1HasFocusChangedFalseCount++;
                                        }
                                    };

        var subView2HasFocusChangedTrueCount = 0;
        var subView2HasFocusChangedFalseCount = 0;

        subView2.HasFocusChanged += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subView2HasFocusChangedTrueCount++;
                                        }
                                        else
                                        {
                                            subView2HasFocusChangedFalseCount++;
                                        }
                                    };

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangedTrueCount);
        Assert.Equal (0, subView1HasFocusChangedFalseCount);

        Assert.Equal (0, subView2HasFocusChangedTrueCount);
        Assert.Equal (0, subView2HasFocusChangedFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.False (subView1.HasFocus);
        Assert.True (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangedTrueCount);
        Assert.Equal (1, subView1HasFocusChangedFalseCount);

        Assert.Equal (1, subView2HasFocusChangedTrueCount);
        Assert.Equal (0, subView2HasFocusChangedFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (2, subView1HasFocusChangedTrueCount);
        Assert.Equal (1, subView1HasFocusChangedFalseCount);

        Assert.Equal (1, subView2HasFocusChangedTrueCount);
        Assert.Equal (1, subView2HasFocusChangedFalseCount);
    }

    [Fact]
    public void AdvanceFocus_Subviews_Raises_HasFocusChanging ()
    {
        var top = new View
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
        top.Add (subView1, subView2);

        var subView1HasFocusChangingTrueCount = 0;
        var subView1HasFocusChangingFalseCount = 0;

        subView1.HasFocusChanging += (s, e) =>
                                     {
                                         if (e.NewValue)
                                         {
                                             subView1HasFocusChangingTrueCount++;
                                         }
                                         else
                                         {
                                             subView1HasFocusChangingFalseCount++;
                                         }
                                     };

        var subView2HasFocusChangingTrueCount = 0;
        var subView2HasFocusChangingFalseCount = 0;

        subView2.HasFocusChanging += (s, e) =>
                                     {
                                         if (e.NewValue)
                                         {
                                             subView2HasFocusChangingTrueCount++;
                                         }
                                         else
                                         {
                                             subView2HasFocusChangingFalseCount++;
                                         }
                                     };

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangingTrueCount);
        Assert.Equal (0, subView1HasFocusChangingFalseCount);

        Assert.Equal (0, subView2HasFocusChangingTrueCount);
        Assert.Equal (0, subView2HasFocusChangingFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.False (subView1.HasFocus);
        Assert.True (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangingTrueCount);
        Assert.Equal (1, subView1HasFocusChangingFalseCount);

        Assert.Equal (1, subView2HasFocusChangingTrueCount);
        Assert.Equal (0, subView2HasFocusChangingFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (2, subView1HasFocusChangingTrueCount);
        Assert.Equal (1, subView1HasFocusChangingFalseCount);

        Assert.Equal (1, subView2HasFocusChangingTrueCount);
        Assert.Equal (1, subView2HasFocusChangingFalseCount);
    }

    [Fact]
    public void AdvanceFocus_With_CanFocus_Are_All_True ()
    {
        var top = new View { Id = "top", CanFocus = true };
        var v1 = new View { Id = "v1", CanFocus = true };
        var v2 = new View { Id = "v2", CanFocus = true };
        var v3 = new View { Id = "v3", CanFocus = true };

        top.Add (v1, v2, v3);

        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        top.Dispose ();
    }

    [Theory]
    [CombinatorialData]
    public void TabStop_And_CanFocus_Are_Decoupled (bool canFocus, TabBehavior tabStop)
    {
        var view = new View { CanFocus = canFocus, TabStop = tabStop };

        Assert.Equal (canFocus, view.CanFocus);
        Assert.Equal (tabStop, view.TabStop);
    }
}
