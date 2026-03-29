using UnitTests;

namespace ViewsTests.TabView;

// Copilot

/// <summary>
///     Tests for the <see cref="Tabs"/> class.
/// </summary>
public class TabsTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Constructor_SetsExpectedDefaults ()
    {
        Tabs tabs = new ();

        Assert.True (tabs.CanFocus);
        Assert.Equal (Side.Top, tabs.TabSide);
        Assert.Equal (LineStyle.Rounded, tabs.TabLineStyle);
        Assert.Null (tabs.Value);
    }

    [Fact]
    public void Add_Tab_AssignsTabIndex ()
    {
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);

        Assert.Equal (0, tab1.TabIndex);
        Assert.Equal (1, tab2.TabIndex);
        Assert.Equal (2, tab3.TabIndex);
    }

    [Fact]
    public void Add_Tab_SetsTabSide ()
    {
        Tabs tabs = new () { TabSide = Side.Bottom };
        Tab tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (Side.Bottom, tab1.Border.TabSide);
    }

    [Fact]
    public void Add_Tab_SetsLineStyle ()
    {
        Tabs tabs = new () { TabLineStyle = LineStyle.Single };
        Tab tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (LineStyle.Single, tab1.BorderStyle);
    }

    [Fact]
    public void Add_Tab_SetsBorderThickness_Top ()
    {
        Tabs tabs = new () { TabSide = Side.Top };
        Tab tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (1, 3, 1, 1), tab1.Border.Thickness);
    }

    [Fact]
    public void Add_Tab_SetsBorderThickness_Bottom ()
    {
        Tabs tabs = new () { TabSide = Side.Bottom };
        Tab tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (1, 1, 1, 3), tab1.Border.Thickness);
    }

    [Fact]
    public void Add_Tab_SetsBorderThickness_Left ()
    {
        Tabs tabs = new () { TabSide = Side.Left };
        Tab tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (3, 1, 1, 1), tab1.Border.Thickness);
    }

    [Fact]
    public void Add_Tab_SetsBorderThickness_Right ()
    {
        Tabs tabs = new () { TabSide = Side.Right };
        Tab tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (1, 1, 3, 1), tab1.Border.Thickness);
    }

    [Fact]
    public void TabCollection_ReturnsTabsInLogicalOrder ()
    {
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);

        List<Tab> ordered = tabs.TabCollection.ToList ();
        Assert.Equal (3, ordered.Count);
        Assert.Same (tab1, ordered [0]);
        Assert.Same (tab2, ordered [1]);
        Assert.Same (tab3, ordered [2]);
    }

    [Fact]
    public void Remove_Tab_ReindexesRemainingTabs ()
    {
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Remove (tab2);

        Assert.Equal (0, tab1.TabIndex);
        Assert.Equal (1, tab3.TabIndex);
    }

    [Fact]
    public void UpdateTabOffsets_ComputesCumulativeOffsets ()
    {
        Tabs tabs = new ();

        // "Tab1" title → TabLength = 6 (4 chars + 2 border cells)
        Tab tab1 = new () { Title = "Tab1" };

        // "Tab2" title → TabLength = 6
        Tab tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);

        // Tab1 starts at 0
        Assert.Equal (0, tab1.Border.TabOffset);

        // Tab2 starts at TabLength-1 = 5 (sharing one edge)
        Assert.Equal (5, tab2.Border.TabOffset);
    }

    [Fact]
    public void UpdateTabOffsets_ThreeTabs ()
    {
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);

        Assert.Equal (0, tab1.Border.TabOffset);
        Assert.Equal (5, tab2.Border.TabOffset);
        Assert.Equal (10, tab3.Border.TabOffset);
    }

    [Fact]
    public void Value_SetsFocus ()
    {
        IDriver driver = CreateTestDriver ();

        Tabs tabs = new () { Driver = driver, Width = 40, Height = 10 };

        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Layout ();

        tabs.Value = tab2;

        Assert.Same (tab2, tabs.Value);
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        tabs.ValueChanging += (_, args) => args.Handled = true;

        tabs.Value = tab2;

        // Change was cancelled, value remains tab1
        Assert.Same (tab1, tabs.Value);
    }

    [Fact]
    public void ValueChanged_Fires ()
    {
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        Tab? newValue = null;
        tabs.ValueChanged += (_, args) => newValue = args.NewValue;

        tabs.Value = tab2;

        Assert.Same (tab2, newValue);
    }

    [Fact]
    public void TabSide_Change_UpdatesAllTabs ()
    {
        Tabs tabs = new () { TabSide = Side.Top };
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);

        tabs.TabSide = Side.Bottom;

        Assert.Equal (Side.Bottom, tab1.Border.TabSide);
        Assert.Equal (Side.Bottom, tab2.Border.TabSide);
        Assert.Equal (new Thickness (1, 1, 1, 3), tab1.Border.Thickness);
        Assert.Equal (new Thickness (1, 1, 1, 3), tab2.Border.Thickness);
    }

    [Fact]
    public void TabLineStyle_Change_UpdatesAllTabs ()
    {
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);

        tabs.TabLineStyle = LineStyle.Double;

        Assert.Equal (LineStyle.Double, tab1.BorderStyle);
        Assert.Equal (LineStyle.Double, tab2.BorderStyle);
    }

    [Fact]
    public void EnableForDesign_CreatesTabs ()
    {
        Tabs tabs = new ();
        bool result = ((IDesignable)tabs).EnableForDesign ();

        Assert.True (result);
        Assert.Equal (4, tabs.TabCollection.Count ());
        Assert.NotNull (tabs.Value);
    }

    [Fact]
    public void Top_TwoTabs_Tab1Focused_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (14, 5);

        Tabs tabs = new () { Driver = driver, Width = 14, Height = 5 };

        Tab tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        Tab tab2 = new () { Title = "Tab2", Text = "Tab2 content" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("╭────╮────╮   \r\n"
                                                       + "│Tab1│Tab2│   \r\n"
                                                       + "│    ╰────┴──╮\r\n"
                                                       + "│Tab1 content│\r\n"
                                                       + "╰────────────╯",
                                                       output,
                                                       driver);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_TwoTabs_Tab2Focused_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (14, 5);

        Tabs tabs = new () { Driver = driver, Width = 14, Height = 5 };

        Tab tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        Tab tab2 = new () { Title = "Tab2", Text = "Tab2 content" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab2;

        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("╭────╭────╮   \r\n"
                                                       + "│Tab1│Tab2│   \r\n"
                                                       + "├────╯    ╰──╮\r\n"
                                                       + "│Tab2 content│\r\n"
                                                       + "╰────────────╯",
                                                       output,
                                                       driver);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_ThreeTabs_Tab2Focused_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (20, 5);

        Tabs tabs = new () { Driver = driver, Width = 20, Height = 5 };

        Tab tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        Tab tab2 = new () { Title = "Tab2", Text = "Tab2 content" };
        Tab tab3 = new () { Title = "Tab3", Text = "Tab3 content" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Value = tab2;

        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("╭────╭────╮────╮    \r\n"
                                                       + "│Tab1│Tab2│Tab3│    \r\n"
                                                       + "├────╯    ╰────┴───╮\r\n"
                                                       + "│Tab2 content      │\r\n"
                                                       + "╰──────────────────╯",
                                                       output,
                                                       driver);

        tabs.Dispose ();
    }
}
