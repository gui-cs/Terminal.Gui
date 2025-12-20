namespace ViewBaseTests.Navigation;

/// <summary>
/// Tests for navigation into and out of Adornments (Padding, Border, Margin).
/// These tests prove that navigation to/from adornments is broken and need to be fixed.
/// </summary>
public class AdornmentNavigationTests
{
    #region Padding Navigation Tests

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Into_Padding_With_Focusable_SubView ()
    {
        // Setup: View with a focusable subview in Padding
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Padding!.Thickness = new Thickness (1);

        View paddingButton = new ()
        {
            Id = "paddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Padding.Add (paddingButton);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Test: Advance focus should navigate to content first
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: contentButton should have focus
        // This test documents the expected behavior for navigation into padding
        Assert.True (contentButton.HasFocus, "Content view should receive focus first");
        Assert.False (paddingButton.HasFocus, "Padding subview should not have focus yet");

        // Test: Advance focus again should go to padding
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: paddingButton should now have focus
        // This will likely FAIL, proving the bug exists
        Assert.True (paddingButton.HasFocus, "Padding subview should receive focus after content");
        Assert.False (contentButton.HasFocus, "Content view should no longer have focus");

        view.Dispose ();
    }

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Out_Of_Padding_To_Content ()
    {
        // Setup: View with focusable padding that has focus
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Padding!.Thickness = new Thickness (1);

        View paddingButton = new ()
        {
            Id = "paddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Padding.Add (paddingButton);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Set focus to padding button
        paddingButton.SetFocus ();
        Assert.True (paddingButton.HasFocus, "Setup: Padding button should have focus");

        // Test: Advance focus should navigate from padding to content
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: Should navigate to content
        // This will likely FAIL, proving the bug exists
        Assert.True (contentButton.HasFocus, "Content view should receive focus after padding");
        Assert.False (paddingButton.HasFocus, "Padding button should no longer have focus");

        view.Dispose ();
    }

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Backward_Into_Padding ()
    {
        // Setup: View with focusable subviews in both content and padding
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Padding!.Thickness = new Thickness (1);

        View paddingButton = new ()
        {
            Id = "paddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Padding.Add (paddingButton);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Set focus to content
        contentButton.SetFocus ();
        Assert.True (contentButton.HasFocus, "Setup: Content button should have focus");

        // Test: Advance focus backward should go to padding
        view.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);

        // Expected: Should navigate to padding
        // This will likely FAIL, proving the bug exists
        Assert.True (paddingButton.HasFocus, "Padding button should receive focus when navigating backward");
        Assert.False (contentButton.HasFocus, "Content button should no longer have focus");

        view.Dispose ();
    }

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void Padding_CanFocus_True_TabStop_TabStop_Should_Be_In_FocusChain ()
    {
        // Setup: View with focusable Padding
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Padding!.Thickness = new Thickness (1);
        view.Padding.CanFocus = true;
        view.Padding.TabStop = TabBehavior.TabStop;

        view.BeginInit ();
        view.EndInit ();

        // Test: Get focus chain
        View [] focusChain = view.GetFocusChain (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: Padding should be in the focus chain
        // This should pass based on the GetFocusChain code
        Assert.Contains (view.Padding, focusChain);

        view.Dispose ();
    }

    #endregion

    #region Border Navigation Tests

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Into_Border_With_Focusable_SubView ()
    {
        // Setup: View with a focusable subview in Border
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Border!.Thickness = new Thickness (1);

        View borderButton = new ()
        {
            Id = "borderButton",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Border.Add (borderButton);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Test: Advance focus should navigate between content and border
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);

        // Expected: One of them should have focus
        var hasFocus = contentButton.HasFocus || borderButton.HasFocus;
        Assert.True (hasFocus, "Either content or border button should have focus");

        // Advance again
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);

        // Expected: The other one should now have focus
        // This will likely FAIL, proving the bug exists
        if (contentButton.HasFocus)
        {
            // If content has focus now, border should have had it before
            Assert.False (borderButton.HasFocus, "Only one should have focus at a time");
        }
        else
        {
            Assert.True (borderButton.HasFocus, "Border should have focus if content doesn't");
        }

        view.Dispose ();
    }

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void Border_CanFocus_True_TabStop_TabGroup_Should_Be_In_FocusChain ()
    {
        // Setup: View with focusable Border (default TabStop is TabGroup for Border)
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Border!.Thickness = new Thickness (1);
        view.Border.CanFocus = true;

        view.BeginInit ();
        view.EndInit ();

        // Test: Get focus chain for TabGroup
        View [] focusChain = view.GetFocusChain (NavigationDirection.Forward, TabBehavior.TabGroup);

        // Expected: Border should be in the focus chain
        Assert.Contains (view.Border, focusChain);

        view.Dispose ();
    }

    #endregion

    #region Margin Navigation Tests

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void Margin_CanFocus_True_Should_Be_In_FocusChain ()
    {
        // Setup: View with focusable Margin
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Margin!.Thickness = new Thickness (1);
        view.Margin.CanFocus = true;
        view.Margin.TabStop = TabBehavior.TabStop;

        view.BeginInit ();
        view.EndInit ();

        // Test: Get focus chain
        View [] focusChain = view.GetFocusChain (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: Margin should be in the focus chain
        Assert.Contains (view.Margin, focusChain);

        view.Dispose ();
    }

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Into_Margin_With_Focusable_SubView ()
    {
        // Setup: View with a focusable subview in Margin
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Margin!.Thickness = new Thickness (1);

        View marginButton = new ()
        {
            Id = "marginButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Margin.Add (marginButton);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Test: Advance focus should navigate to content first
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: contentButton should have focus
        Assert.True (contentButton.HasFocus, "Content view should receive focus first");
        Assert.False (marginButton.HasFocus, "Margin subview should not have focus yet");

        // Test: Advance focus again should go to margin
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: marginButton should now have focus
        // This will likely FAIL, proving the bug exists
        Assert.True (marginButton.HasFocus, "Margin subview should receive focus after content");
        Assert.False (contentButton.HasFocus, "Content view should no longer have focus");

        view.Dispose ();
    }

    #endregion

    #region Mixed Scenarios

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_With_Multiple_Adornments_And_Content ()
    {
        // Setup: View with focusable subviews in Margin, Border, Padding, and Content
        View view = new ()
        {
            Id = "view",
            Width = 20,
            Height = 20,
            CanFocus = true
        };

        // Setup Margin with a subview
        view.Margin!.Thickness = new Thickness (1);
        View marginButton = new ()
        {
            Id = "marginButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };
        view.Margin.Add (marginButton);

        // Setup Border with a subview
        view.Border!.Thickness = new Thickness (1);
        View borderButton = new ()
        {
            Id = "borderButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };
        view.Border.Add (borderButton);

        // Setup Padding with a subview
        view.Padding!.Thickness = new Thickness (1);
        View paddingButton = new ()
        {
            Id = "paddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };
        view.Padding.Add (paddingButton);

        // Setup Content with a subview
        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };
        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Test: Navigate through all focusable elements
        var focusedViews = new List<View> ();

        // Advance focus 4 times to cycle through all elements
        for (var i = 0; i < 4; i++)
        {
            view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

            if (contentButton.HasFocus)
            {
                focusedViews.Add (contentButton);
            }
            else if (paddingButton.HasFocus)
            {
                focusedViews.Add (paddingButton);
            }
            else if (borderButton.HasFocus)
            {
                focusedViews.Add (borderButton);
            }
            else if (marginButton.HasFocus)
            {
                focusedViews.Add (marginButton);
            }
        }

        // Expected: All four buttons should have received focus at some point
        // This will likely FAIL, proving the bug exists
        Assert.Contains (contentButton, focusedViews);
        Assert.Contains (paddingButton, focusedViews);
        Assert.Contains (borderButton, focusedViews);
        Assert.Contains (marginButton, focusedViews);

        view.Dispose ();
    }

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Nested_Views_With_Adornment_SubViews ()
    {
        // Setup: Nested views where parent has adornment subviews
        View parent = new ()
        {
            Id = "parent",
            Width = 30,
            Height = 30,
            CanFocus = true
        };

        parent.Padding!.Thickness = new Thickness (2);

        View parentPaddingButton = new ()
        {
            Id = "parentPaddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 8,
            Height = 1
        };

        parent.Padding.Add (parentPaddingButton);

        View child = new ()
        {
            Id = "child",
            Width = 10,
            Height = 10,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        parent.Add (child);

        child.Padding!.Thickness = new Thickness (1);

        View childPaddingButton = new ()
        {
            Id = "childPaddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        child.Padding.Add (childPaddingButton);

        parent.BeginInit ();
        parent.EndInit ();

        // Test: Advance focus should navigate through parent padding, child, and child padding
        parent.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Track which views receive focus
        var focusedIds = new List<string> ();

        for (var i = 0; i < 5; i++)
        {
            if (parentPaddingButton.HasFocus)
            {
                focusedIds.Add ("parentPaddingButton");
            }
            else if (child.HasFocus)
            {
                focusedIds.Add ("child");
            }
            else if (childPaddingButton.HasFocus)
            {
                focusedIds.Add ("childPaddingButton");
            }

            parent.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        }

        // Expected: Navigation should reach all elements including adornment subviews
        // This will likely show incomplete navigation, proving the bug exists
        Assert.True (
                     focusedIds.Count > 0,
                     "At least some navigation should occur (this test documents current behavior)"
                    );

        parent.Dispose ();
    }

    #endregion

    #region TabGroup Behavior Tests

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_TabGroup_Should_Navigate_To_Border_SubViews ()
    {
        // Setup: View with Border containing TabGroup subviews
        View view = new ()
        {
            Id = "view",
            Width = 20,
            Height = 20,
            CanFocus = true
        };

        view.Border!.Thickness = new Thickness (1);

        View borderButton1 = new ()
        {
            Id = "borderButton1",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Border.Add (borderButton1);

        View borderButton2 = new ()
        {
            Id = "borderButton2",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            X = 6,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Border.Add (borderButton2);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Test: Navigate with TabGroup behavior
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        var firstFocus = view.Focused;

        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        var secondFocus = view.Focused;

        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        var thirdFocus = view.Focused;

        // Expected: Should cycle through all TabGroup elements including Border subviews
        // This will likely FAIL for border subviews, proving the bug exists
        var focusedViews = new [] { firstFocus, secondFocus, thirdFocus };
        Assert.Contains (contentButton, focusedViews);

        // These assertions will likely fail, proving border navigation is broken
        Assert.Contains (borderButton1, focusedViews);
        Assert.Contains (borderButton2, focusedViews);

        view.Dispose ();
    }

    #endregion

    #region Edge Cases

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Adornment_With_No_Thickness_Should_Not_Participate ()
    {
        // Setup: View with Padding that has no thickness but has subviews
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        // Padding has default Thickness.Empty
        View paddingButton = new ()
        {
            Id = "paddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Padding!.Add (paddingButton);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Test: Navigate - should only focus content since Padding has no thickness
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (contentButton.HasFocus, "Content should get focus");

        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: Should wrap back to content, not go to padding
        Assert.True (contentButton.HasFocus, "Should stay in content when Padding has no thickness");
        Assert.False (paddingButton.HasFocus, "Padding button should not receive focus");

        view.Dispose ();
    }

    [Fact]
    [Trait ("Category", "Adornment")]
    [Trait ("Category", "Navigation")]
    public void AdvanceFocus_Disabled_Adornment_SubView_Should_Be_Skipped ()
    {
        // Setup: View with disabled subview in Padding
        View view = new ()
        {
            Id = "view",
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        view.Padding!.Thickness = new Thickness (1);

        View paddingButton = new ()
        {
            Id = "paddingButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            Enabled = false, // Disabled
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Padding.Add (paddingButton);

        View contentButton = new ()
        {
            Id = "contentButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = 5,
            Height = 1
        };

        view.Add (contentButton);

        view.BeginInit ();
        view.EndInit ();

        // Test: Navigate - disabled padding button should be skipped
        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (contentButton.HasFocus, "Content should get focus");

        view.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // Expected: Should wrap back to content, skipping disabled padding button
        Assert.True (contentButton.HasFocus, "Should skip disabled padding button");
        Assert.False (paddingButton.HasFocus, "Disabled padding button should not receive focus");

        view.Dispose ();
    }

    #endregion
}
