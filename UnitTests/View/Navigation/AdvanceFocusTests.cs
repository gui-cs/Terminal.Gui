using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdvanceFocusTests (ITestOutputHelper _output)
{
    [Fact]
    public void Subviews_TabIndexes_AreEqual ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.Subviews.IndexOf (v2) == 1);
        Assert.True (r.Subviews.IndexOf (v3) == 2);

        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v3) == 2);

        Assert.Equal (r.Subviews.IndexOf (v1), r.TabIndexes.IndexOf (v1));
        Assert.Equal (r.Subviews.IndexOf (v2), r.TabIndexes.IndexOf (v2));
        Assert.Equal (r.Subviews.IndexOf (v3), r.TabIndexes.IndexOf (v3));
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Invert_Order ()
    {
        var r = new View ();
        var v1 = new View { Id = "1", CanFocus = true };
        var v2 = new View { Id = "2", CanFocus = true };
        var v3 = new View { Id = "3", CanFocus = true };

        r.Add (v1, v2, v3);

        v1.TabIndex = 2;
        v2.TabIndex = 1;
        v3.TabIndex = 0;
        Assert.True (r.TabIndexes.IndexOf (v1) == 2);
        Assert.True (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v3) == 0);

        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.Subviews.IndexOf (v2) == 1);
        Assert.True (r.Subviews.IndexOf (v3) == 2);
    }

    [Fact]
    public void TabIndex_Invert_Order_Added_One_By_One_Does_Not_Do_What_Is_Expected ()
    {
        var r = new View ();
        var v1 = new View { Id = "1", CanFocus = true };
        r.Add (v1);
        v1.TabIndex = 2;
        var v2 = new View { Id = "2", CanFocus = true };
        r.Add (v2);
        v2.TabIndex = 1;
        var v3 = new View { Id = "3", CanFocus = true };
        r.Add (v3);
        v3.TabIndex = 0;

        Assert.False (r.TabIndexes.IndexOf (v1) == 2);
        Assert.True (r.TabIndexes.IndexOf (v1) == 1);
        Assert.False (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v2) == 2);

        // Only the last is in the expected index
        Assert.True (r.TabIndexes.IndexOf (v3) == 0);

        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.Subviews.IndexOf (v2) == 1);
        Assert.True (r.Subviews.IndexOf (v3) == 2);
    }

    [Fact]
    public void TabIndex_Invert_Order_Mixed ()
    {
        var r = new View ();
        var vl1 = new View { Id = "vl1" };
        var v1 = new View { Id = "v1", CanFocus = true };
        var vl2 = new View { Id = "vl2" };
        var v2 = new View { Id = "v2", CanFocus = true };
        var vl3 = new View { Id = "vl3" };
        var v3 = new View { Id = "v3", CanFocus = true };

        r.Add (vl1, v1, vl2, v2, vl3, v3);

        v1.TabIndex = 2;
        v2.TabIndex = 1;
        v3.TabIndex = 0;
        Assert.True (r.TabIndexes.IndexOf (v1) == 4);
        Assert.True (r.TabIndexes.IndexOf (v2) == 2);
        Assert.True (r.TabIndexes.IndexOf (v3) == 0);

        Assert.True (r.Subviews.IndexOf (v1) == 1);
        Assert.True (r.Subviews.IndexOf (v2) == 3);
        Assert.True (r.Subviews.IndexOf (v3) == 5);
    }

    [Fact]
    public void TabIndex_Set_CanFocus_False ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.CanFocus = false;
        v1.TabIndex = 0;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.NotEqual (-1, v1.TabIndex);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_False_To_True ()
    {
        var r = new View ();
        var v1 = new View ();
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.CanFocus = true;
        v1.TabIndex = 1;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 1);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_HigherValues ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.TabIndex = 3;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 2);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_LowerValues ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        //v1.TabIndex = -1;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_ValidValues ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.TabIndex = 1;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 1);

        v1.TabIndex = 2;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 2);
        r.Dispose ();
    }


    [Theory]
    [CombinatorialData]
    public void TabStop_And_CanFocus_Are_Decoupled (bool canFocus, TabBehavior tabStop)
    {
        var view = new View { CanFocus = canFocus, TabStop = tabStop };

        Assert.Equal (canFocus, view.CanFocus);
        Assert.Equal (tabStop, view.TabStop);
    }


    [Fact]
    public void AdvanceFocus_Compound_Subview ()
    {
        var top = new View () { Id = "top", CanFocus = true };

        var compoundSubview = new View ()
        {
            CanFocus = true,
            Id = "compoundSubview",
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
            Id = "otherSubview",
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
    public void AdvanceFocus_With_CanFocus_Are_All_True ()
    {
        var top = new View () { Id = "top", CanFocus = true };
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
        var r = new View () { CanFocus = true };
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
}
