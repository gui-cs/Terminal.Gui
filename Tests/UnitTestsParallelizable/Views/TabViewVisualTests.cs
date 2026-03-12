// Claude - Opus 4.6

using UnitTests;

namespace ViewsTests;

public class TabViewVisualTests (ITestOutputHelper output) : TestDriverBase
{
    #region Visual Rendering — Tabs on Top

    [Fact]
    public void Renders_TwoTabs_FirstSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 6);

        TabView tabView = new () { Width = 20, Height = 6 };

        Tab tab1 = new () { Title = "Tab1" };
        tab1.Add (new Label { Text = "Content1" });

        Tab tab2 = new () { Title = "Tab2" };
        tab2.Add (new Label { Text = "Content2" });

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────┬────╮
                                              │Tab1│Tab2│
                                              │    ╰────┴────────╮
                                              │Content1          │
                                              │                  │
                                              ╰──────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        Assert.True (tab1.Visible);
        Assert.False (tab2.Visible);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Renders_TwoTabs_SecondSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 6);

        TabView tabView = new () { Width = 20, Height = 6 };

        Tab tab1 = new () { Title = "Tab1" };
        tab1.Add (new Label { Text = "Content1" });

        Tab tab2 = new () { Title = "Tab2" };
        tab2.Add (new Label { Text = "Content2" });

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 1;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────┬────╮
                                              │Tab1│Tab2│
                                              ├────╯    ╰────────╮
                                              │Content2          │
                                              │                  │
                                              ╰──────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        Assert.False (tab1.Visible);
        Assert.True (tab2.Visible);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Renders_ThreeTabs_MiddleSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (25, 6);

        TabView tabView = new () { Width = 25, Height = 6 };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };
        Tab tab3 = new () { Title = "CC" };
        tab2.Add (new Label { Text = "Middle" });

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 1;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──┬──┬──╮
                                              │AA│BB│CC│
                                              ├──╯  ╰──┴──────────────╮
                                              │Middle                 │
                                              │                       │
                                              ╰───────────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        Assert.False (tab1.Visible);
        Assert.True (tab2.Visible);
        Assert.False (tab3.Visible);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_ThreeTabs_FirstSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (25, 6);

        TabView tabView = new () { Width = 25, Height = 6 };

        Tab tab1 = new () { Title = "AA" };
        tab1.Add (new Label { Text = "First" });

        Tab tab2 = new () { Title = "BB" };
        Tab tab3 = new () { Title = "CC" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──┬──┬──╮
                                              │AA│BB│CC│
                                              │  ╰──┴──┴──────────────╮
                                              │First                  │
                                              │                       │
                                              ╰───────────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_ThreeTabs_LastSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (25, 6);

        TabView tabView = new () { Width = 25, Height = 6 };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };

        Tab tab3 = new () { Title = "CC" };
        tab3.Add (new Label { Text = "Third" });

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 2;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──┬──┬──╮
                                              │AA│BB│CC│
                                              ├──┴──╯  ╰──────────────╮
                                              │Third                  │
                                              │                       │
                                              ╰───────────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_SingleTab ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (15, 7);

        TabView tabView = new () { Width = 15, Height = 7 };

        Tab tab1 = new () { Title = "One" };
        tab1.Add (new Label { Text = "Solo" });

        tabView.Add (tab1);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │One│
                                              │   ╰─────────╮
                                              │Solo         │
                                              │             │
                                              │             │
                                              ╰─────────────╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_NoTabs ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (15, 6);

        TabView tabView = new () { Width = 15, Height = 6 };

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              │             │
                                              │             │
                                              │             │
                                              │             │
                                              │             │
                                              ╰─────────────╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    #endregion

    #region Visual Rendering — Tabs on Bottom

    [Fact]
    public void Renders_TabsOnBottom ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 7);

        TabView tabView = new () { Width = 20, Height = 7, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "T1" };
        tab1.Add (new Label { Text = "Bottom" });

        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──────────────────╮
                                              │Bottom            │
                                              │                  │
                                              │                  │
                                              │  ╭──┬────────────╯
                                              │T1│T2│
                                              ╰──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        Assert.Equal (0, tabView.Padding!.Thickness.Top);
        Assert.Equal (3, tabView.Padding.Thickness.Bottom);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_TabsOnBottom_SecondSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 7);

        TabView tabView = new () { Width = 20, Height = 7, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "T1" };

        Tab tab2 = new () { Title = "T2" };
        tab2.Add (new Label { Text = "Second" });

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 1;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──────────────────╮
                                              │Second            │
                                              │                  │
                                              │                  │
                                              ├──╮  ╭────────────╯
                                              │T1│T2│
                                              ╰──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_TabsOnBottom_ThreeTabs_FirstSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (25, 8);

        TabView tabView = new () { Width = 25, Height = 8, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "AA" };
        tab1.Add (new Label { Text = "First" });

        Tab tab2 = new () { Title = "BB" };
        Tab tab3 = new () { Title = "CC" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────────────────╮
                                              │First                  │
                                              │                       │
                                              │                       │
                                              │                       │
                                              │  ╭──┬──┬──────────────╯
                                              │AA│BB│CC│
                                              ╰──┴──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_TabsOnBottom_ThreeTabs_MiddleSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (25, 8);

        TabView tabView = new () { Width = 25, Height = 8, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "AA" };

        Tab tab2 = new () { Title = "BB" };
        tab2.Add (new Label { Text = "Middle" });

        Tab tab3 = new () { Title = "CC" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 1;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────────────────╮
                                              │Middle                 │
                                              │                       │
                                              │                       │
                                              │                       │
                                              ├──╮  ╭──┬──────────────╯
                                              │AA│BB│CC│
                                              ╰──┴──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_TabsOnBottom_ThreeTabs_LastSelected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (25, 8);

        TabView tabView = new () { Width = 25, Height = 8, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };

        Tab tab3 = new () { Title = "CC" };
        tab3.Add (new Label { Text = "Last" });

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 2;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────────────────╮
                                              │Last                   │
                                              │                       │
                                              │                       │
                                              │                       │
                                              ├──┬──╮  ╭──────────────╯
                                              │AA│BB│CC│
                                              ╰──┴──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_TabsOnBottom_SingleTab ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (15, 8);

        TabView tabView = new () { Width = 15, Height = 8, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "One" };
        tab1.Add (new Label { Text = "Solo" });

        tabView.Add (tab1);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭─────────────╮
                                              │Solo         │
                                              │             │
                                              │             │
                                              │             │
                                              │   ╭─────────╯
                                              │One│
                                              ╰───╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    #endregion

    #region Visual Rendering — State Transitions

    // Claude - Opus 4.6
    [Fact]
    public void Renders_SwitchTab_UpdatesVisual ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 7);

        TabView tabView = new () { Width = 20, Height = 7 };

        Tab tab1 = new () { Title = "Tab1" };
        tab1.Add (new Label { Text = "Content1" });

        Tab tab2 = new () { Title = "Tab2" };
        tab2.Add (new Label { Text = "Content2" });

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        // First render: Tab1 selected
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────┬────╮
                                              │Tab1│Tab2│
                                              │    ╰────┴────────╮
                                              │Content1          │
                                              │                  │
                                              │                  │
                                              ╰──────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        // Switch to Tab2 and re-render
        tabView.SelectedTabIndex = 1;
        top.Layout ();
        top.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────┬────╮
                                              │Tab1│Tab2│
                                              ├────╯    ╰────────╮
                                              │Content2          │
                                              │                  │
                                              │                  │
                                              ╰──────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_SwitchTab_TabsOnBottom_UpdatesVisual ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 7);

        TabView tabView = new () { Width = 20, Height = 7, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "T1" };
        tab1.Add (new Label { Text = "First" });

        Tab tab2 = new () { Title = "T2" };
        tab2.Add (new Label { Text = "Second" });

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        // First render: T1 selected
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──────────────────╮
                                              │First             │
                                              │                  │
                                              │                  │
                                              │  ╭──┬────────────╯
                                              │T1│T2│
                                              ╰──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        // Switch to T2 and re-render
        tabView.SelectedTabIndex = 1;
        top.Layout ();
        top.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──────────────────╮
                                              │Second            │
                                              │                  │
                                              │                  │
                                              ├──╮  ╭────────────╯
                                              │T1│T2│
                                              ╰──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Renders_ToggleTabsOnBottom_ChangesRendering ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 7);

        TabView tabView = new () { Width = 20, Height = 7 };

        Tab tab1 = new () { Title = "T1" };
        tab1.Add (new Label { Text = "Content" });

        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        Runnable top = new ();
        top.Add (tabView);
        app.Begin (top);

        // First render: tabs on top
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──┬──╮
                                              │T1│T2│
                                              │  ╰──┴────────────╮
                                              │Content           │
                                              │                  │
                                              │                  │
                                              ╰──────────────────╯
                                              """,
                                              output,
                                              app.Driver);

        // Toggle to tabs on bottom
        tabView.TabSide = Side.Bottom;
        top.Layout ();
        top.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──────────────────╮
                                              │Content           │
                                              │                  │
                                              │                  │
                                              │  ╭──┬────────────╯
                                              │T1│T2│
                                              ╰──┴──╯
                                              """,
                                              output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    #endregion

    #region Rendering — Header Properties

    [Fact]
    public void Render_SelectedTab_HasOpenBorder ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        // Force layout to compute frames
        tabView.Layout ();

        // Get tab headers
        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Selected tab (index 0): bottom border should be open (thickness bottom = 0)
        Assert.Equal (0, headers [0].Border!.Thickness.Bottom);
        Assert.Equal (1, headers [0].Border!.Thickness.Top);
        Assert.Equal (1, headers [0].Border!.Thickness.Left);
        Assert.Equal (1, headers [0].Border!.Thickness.Right);

        // Unselected tab (index 1): full border
        Assert.Equal (1, headers [1].Border!.Thickness.Bottom);
        Assert.Equal (1, headers [1].Border!.Thickness.Top);
        Assert.Equal (1, headers [1].Border!.Thickness.Left);
        Assert.Equal (1, headers [1].Border!.Thickness.Right);
    }

    [Fact]
    public void Render_SelectedTab_TabsOnBottom_HasOpenTopBorder ()
    {
        TabView tabView = new () { Width = 30, Height = 10, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        tabView.Layout ();

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Selected tab: top border open (tabs on bottom)
        Assert.Equal (0, headers [0].Border!.Thickness.Top);
        Assert.Equal (1, headers [0].Border!.Thickness.Bottom);
        Assert.Equal (1, headers [0].Border!.Thickness.Left);
        Assert.Equal (1, headers [0].Border!.Thickness.Right);

        // Unselected tab: full border
        Assert.Equal (new Thickness (1), headers [1].Border!.Thickness);
    }

    [Fact]
    public void Render_SwitchingSelection_UpdatesHeaderBorders ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        tabView.Layout ();

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Initially: tab1 selected (open bottom), tab2 unselected (full)
        Assert.Equal (0, headers [0].Border!.Thickness.Bottom);
        Assert.Equal (1, headers [1].Border!.Thickness.Bottom);

        // Switch to tab2
        tabView.SelectedTabIndex = 1;

        // Now: tab1 full, tab2 open bottom
        Assert.Equal (1, headers [0].Border!.Thickness.Bottom);
        Assert.Equal (0, headers [1].Border!.Thickness.Bottom);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Render_SelectedTab_TabsOnBottom_HasPaddingOffset ()
    {
        TabView tabView = new () { Width = 30, Height = 10, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;
        tabView.Layout ();

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Selected tab (index 0): should have Padding.Top=1 to push text below continuation line
        Assert.Equal (1, headers [0].Padding!.Thickness.Top);
        Assert.Equal (0, headers [0].Padding!.Thickness.Bottom);

        // Unselected tab (index 1): should have Padding.Top=0
        Assert.Equal (0, headers [1].Padding!.Thickness.Top);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Render_SwitchingSelection_TabsOnBottom_UpdatesPadding ()
    {
        TabView tabView = new () { Width = 30, Height = 10, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;
        tabView.Layout ();

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Initially: tab1 selected, has padding
        Assert.Equal (1, headers [0].Padding!.Thickness.Top);
        Assert.Equal (0, headers [1].Padding!.Thickness.Top);

        // Switch to tab2
        tabView.SelectedTabIndex = 1;

        // Now: tab2 should have padding, tab1 should not
        Assert.Equal (0, headers [0].Padding!.Thickness.Top);
        Assert.Equal (1, headers [1].Padding!.Thickness.Top);
    }

    #endregion

    #region Rendering — Border Gaps

    [Fact]
    public void Render_BorderGap_CreatedForSelectedTab ()
    {
        IDriver driver = CreateTestDriver ();

        TabView tabView = new () { Driver = driver, Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        // Layout computes frames; SubViewsLaidOut recomputes border gaps
        tabView.Layout ();

        // The TabView's border should have a RightGap to suppress the right border
        // above the continuation line (where tab headers are).
        Assert.NotEmpty (tabView.Border!.RightGaps);
        Assert.Single (tabView.Border.RightGaps);

        // Gap should suppress the top 2 rows of the right border
        BorderGap gap = tabView.Border.RightGaps [0];
        Assert.Equal (0, gap.Position);
        Assert.Equal (2, gap.Length);
    }

    [Fact]
    public void Render_BorderGap_MovesWhenSelectionChanges ()
    {
        IDriver driver = CreateTestDriver ();

        TabView tabView = new () { Driver = driver, Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;
        tabView.Layout ();

        BorderGap gap1 = tabView.Border!.RightGaps [0];

        // Switch to tab2 and re-layout
        tabView.SelectedTabIndex = 1;
        tabView.Layout ();

        Assert.Single (tabView.Border.RightGaps);
        BorderGap gap2 = tabView.Border.RightGaps [0];

        // RightGap stays in the same position regardless of selection —
        // it always suppresses the top 2 rows of the right border.
        Assert.Equal (gap1.Position, gap2.Position);
        Assert.Equal (gap1.Length, gap2.Length);
    }

    [Fact]
    public void Render_BorderGap_TabsOnBottom_UsesBottomGaps ()
    {
        IDriver driver = CreateTestDriver ();

        TabView tabView = new () { Driver = driver, Width = 30, Height = 10, TabSide = Side.Bottom };

        Tab tab1 = new () { Title = "T1" };

        tabView.Add (tab1);
        tabView.SelectedTabIndex = 0;
        tabView.Layout ();

        // When tabs are on bottom, the RightGap suppresses the bottom 2 rows of the right border
        Assert.NotEmpty (tabView.Border!.RightGaps);
        BorderGap gap = tabView.Border.RightGaps [0];
        Assert.Equal (tabView.Border.Frame.Height - 2, gap.Position);
        Assert.Equal (2, gap.Length);
    }

    [Fact]
    public void Render_NoSelection_NoBorderGap ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "T1" };

        tabView.Add (tab1);
        tabView.SelectedTabIndex = null;
        tabView.Layout ();

        // RightGaps are set even with no selection — they suppress the right border
        // where tab headers exist, regardless of which tab is selected.
        Assert.NotEmpty (tabView.Border!.RightGaps);
    }

    #endregion

    #region Rendering — Header Layout

    [Fact]
    public void Render_HeadersRebuildOnAdd ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "AA" };
        tabView.Add (tab1);

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        Assert.Single (tabRow.SubViews);

        // Add another tab
        Tab tab2 = new () { Title = "BB" };
        tabView.Add (tab2);

        View [] newHeaders = [.. tabRow.SubViews];
        Assert.Equal (2, newHeaders.Length);
    }

    [Fact]
    public void Render_HeadersRebuildOnRemove ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };
        tabView.Add (tab1, tab2);

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        Assert.Equal (2, tabRow.SubViews.Count ());

        // Remove one
        tabView.Remove (tab2);

        Assert.Single (tabRow.SubViews);
    }

    [Fact]
    public void Render_HeaderPositions_Overlap ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };
        Tab tab3 = new () { Title = "CC" };
        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        // Force layout
        tabView.Layout ();

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // First header at X=0
        Assert.Equal (0, headers [0].Frame.X);

        // Second header should start at Right(first) - 1 (overlap by 1)
        Assert.Equal (headers [0].Frame.Right - 1, headers [1].Frame.X);

        // Third header should start at Right(second) - 1
        Assert.Equal (headers [1].Frame.Right - 1, headers [2].Frame.X);
    }

    #endregion
}
