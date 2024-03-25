using System.Globalization;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class TabViewTests
{
    private readonly ITestOutputHelper _output;
    public TabViewTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void AddTab_SameTabMoreThanOnce ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        Assert.Equal (2, tv.Tabs.Count);

        // Tab is already part of the control so shouldn't result in duplication
        tv.AddTab (tab1, false);
        tv.AddTab (tab1, false);
        tv.AddTab (tab1, false);
        tv.AddTab (tab1, false);

        Assert.Equal (2, tv.Tabs.Count);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void AddTwoTabs_SecondIsSelected ()
    {
        InitFakeDriver ();

        var tv = new TabView ();
        Tab tab1;
        Tab tab2;
        tv.AddTab (tab1 = new Tab { DisplayText = "Tab1", View = new TextField { Text = "hi" } }, false);
        tv.AddTab (tab2 = new Tab { DisplayText = "Tab1", View = new Label { Text = "hi2" } }, true);

        Assert.Equal (2, tv.Tabs.Count);
        Assert.Equal (tab2, tv.SelectedTab);

        Application.Shutdown ();
    }

    [Fact]
    public void EnsureSelectedTabVisible_MustScroll ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        // Make tab width small to force only one tab visible at once
        tv.Width = 4;

        tv.SelectedTab = tab1;
        Assert.Equal (0, tv.TabScrollOffset);
        tv.EnsureSelectedTabIsVisible ();
        Assert.Equal (0, tv.TabScrollOffset);

        // Asking to show tab2 should automatically move scroll offset accordingly
        tv.SelectedTab = tab2;
        Assert.Equal (1, tv.TabScrollOffset);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void EnsureSelectedTabVisible_NullSelect ()
    {
        TabView tv = GetTabView ();

        tv.SelectedTab = null;

        Assert.Null (tv.SelectedTab);
        Assert.Equal (0, tv.TabScrollOffset);

        tv.EnsureSelectedTabIsVisible ();

        Assert.Null (tv.SelectedTab);
        Assert.Equal (0, tv.TabScrollOffset);

        Application.Shutdown ();
    }

    [Fact]
    public void EnsureValidScrollOffsets_TabScrollOffset ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        // Make tab width small to force only one tab visible at once
        tv.Width = 4;

        tv.SelectedTab = tab1;
        Assert.Equal (0, tv.TabScrollOffset);

        tv.TabScrollOffset = 10;
        tv.SelectedTab = tab2;
        Assert.Equal (1, tv.TabScrollOffset);

        tv.TabScrollOffset = -1;
        tv.SelectedTab = tab1;
        Assert.Equal (0, tv.TabScrollOffset);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseClick_ChangesTab ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);

        tv.Width = 20;
        tv.Height = 5;

        tv.LayoutSubviews ();

        tv.Draw ();

        View tabRow = tv.Subviews [0];
        Assert.Equal ("TabRowView", tabRow.GetType ().Name);

        TestHelpers.AssertDriverContentsAre (
                                             @"
╭────┬────╮
│Tab1│Tab2│
│    ╰────┴────────╮
│hi                │
└──────────────────┘
",
                                             _output
                                            );

        Tab clicked = null;

        tv.TabClicked += (s, e) => { clicked = e.Tab; };

        var top = new Toplevel ();
        top.Add (tv);
        Application.Begin (top);

        MouseEventEventArgs args;

        // Waving mouse around does not trigger click
        for (var i = 0; i < 100; i++)
        {
            args = new MouseEventEventArgs (
                                            new MouseEvent { X = i, Y = 1, Flags = MouseFlags.ReportMousePosition }
                                           );
            Application.OnMouseEvent (args);
            Application.Refresh ();
            Assert.Null (clicked);
            Assert.Equal (tab1, tv.SelectedTab);
        }

        args = new MouseEventEventArgs (
                                        new MouseEvent { X = 3, Y = 1, Flags = MouseFlags.Button1Clicked }
                                       );
        Application.OnMouseEvent (args);
        Application.Refresh ();
        Assert.Equal (tab1, clicked);
        Assert.Equal (tab1, tv.SelectedTab);

        // Click to tab2
        args = new MouseEventEventArgs (
                                        new MouseEvent { X = 6, Y = 1, Flags = MouseFlags.Button1Clicked }
                                       );
        Application.OnMouseEvent (args);
        Application.Refresh ();
        Assert.Equal (tab2, clicked);
        Assert.Equal (tab2, tv.SelectedTab);

        // cancel navigation
        tv.TabClicked += (s, e) =>
                         {
                             clicked = e.Tab;
                             e.MouseEvent.Handled = true;
                         };

        args = new MouseEventEventArgs (
                                        new MouseEvent { X = 3, Y = 1, Flags = MouseFlags.Button1Clicked }
                                       );
        Application.OnMouseEvent (args);
        Application.Refresh ();

        // Tab 1 was clicked but event handler blocked navigation
        Assert.Equal (tab1, clicked);
        Assert.Equal (tab2, tv.SelectedTab);

        args = new MouseEventEventArgs (
                                        new MouseEvent { X = 12, Y = 1, Flags = MouseFlags.Button1Clicked }
                                       );
        Application.OnMouseEvent (args);
        Application.Refresh ();

        // Clicking beyond last tab should raise event with null Tab
        Assert.Null (clicked);
        Assert.Equal (tab2, tv.SelectedTab);
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseClick_Right_Left_Arrows_ChangesTab ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);

        tv.Width = 7;
        tv.Height = 5;

        tv.LayoutSubviews ();

        tv.Draw ();

        View tabRow = tv.Subviews [0];
        Assert.Equal ("TabRowView", tabRow.GetType ().Name);

        TestHelpers.AssertDriverContentsAre (
                                             @"
╭────╮
│Tab1│
│    ╰►
│hi   │
└─────┘
",
                                             _output
                                            );

        Tab clicked = null;

        tv.TabClicked += (s, e) => { clicked = e.Tab; };

        Tab oldChanged = null;
        Tab newChanged = null;

        tv.SelectedTabChanged += (s, e) =>
                                 {
                                     oldChanged = e.OldTab;
                                     newChanged = e.NewTab;
                                 };

        var top = new Toplevel ();
        top.Add (tv);
        Application.Begin (top);

        // Click the right arrow
        var args = new MouseEventEventArgs (
                                            new MouseEvent { X = 6, Y = 2, Flags = MouseFlags.Button1Clicked }
                                           );
        Application.OnMouseEvent (args);
        Application.Refresh ();
        Assert.Null (clicked);
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);

        TestHelpers.AssertDriverContentsAre (
                                             @"
╭────╮
│Tab2│
◄    ╰╮
│hi2  │
└─────┘
",
                                             _output
                                            );

        // Click the left arrow
        args = new MouseEventEventArgs (
                                        new MouseEvent { X = 0, Y = 2, Flags = MouseFlags.Button1Clicked }
                                       );
        Application.OnMouseEvent (args);
        Application.Refresh ();
        Assert.Null (clicked);
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);

        TestHelpers.AssertDriverContentsAre (
                                             @"
╭────╮
│Tab1│
│    ╰►
│hi   │
└─────┘
",
                                             _output
                                            );
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseClick_Right_Left_Arrows_ChangesTab_With_Border ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);

        tv.Width = 9;
        tv.Height = 7;

        Assert.Equal (LineStyle.None, tv.BorderStyle);
        tv.BorderStyle = LineStyle.Single;

        tv.LayoutSubviews ();

        tv.Draw ();

        View tabRow = tv.Subviews [0];
        Assert.Equal ("TabRowView", tabRow.GetType ().Name);

        TestHelpers.AssertDriverContentsAre (
                                             @"
┌───────┐
│╭────╮ │
││Tab1│ │
││    ╰►│
││hi   ││
│└─────┘│
└───────┘
",
                                             _output
                                            );

        Tab clicked = null;

        tv.TabClicked += (s, e) => { clicked = e.Tab; };

        Tab oldChanged = null;
        Tab newChanged = null;

        tv.SelectedTabChanged += (s, e) =>
                                 {
                                     oldChanged = e.OldTab;
                                     newChanged = e.NewTab;
                                 };

        var top = new Toplevel ();
        top.Add (tv);
        Application.Begin (top);

        // Click the right arrow
        var args = new MouseEventEventArgs (
                                            new MouseEvent { X = 7, Y = 3, Flags = MouseFlags.Button1Clicked }
                                           );
        Application.OnMouseEvent (args);
        Application.Refresh ();
        Assert.Null (clicked);
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);

        TestHelpers.AssertDriverContentsAre (
                                             @"
┌───────┐
│╭────╮ │
││Tab2│ │
│◄    ╰╮│
││hi2  ││
│└─────┘│
└───────┘
",
                                             _output
                                            );

        // Click the left arrow
        args = new MouseEventEventArgs (
                                        new MouseEvent { X = 1, Y = 3, Flags = MouseFlags.Button1Clicked }
                                       );
        Application.OnMouseEvent (args);
        Application.Refresh ();
        Assert.Null (clicked);
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);

        TestHelpers.AssertDriverContentsAre (
                                             @"
┌───────┐
│╭────╮ │
││Tab1│ │
││    ╰►│
││hi   ││
│└─────┘│
└───────┘
",
                                             _output
                                            );
    }

    [Fact]
    [AutoInitShutdown]
    public void ProcessKey_Down_Up_Right_Left_Home_End_PageDown_PageUp ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);

        tv.Width = 7;
        tv.Height = 5;

        var btn = new Button
        {
            Y = Pos.Bottom (tv) + 1,
            AutoSize = false,
            Height = 1,
            Width = 7,
            Text = "Ok"
        };

        Toplevel top = new ();
        top.Add (tv, btn);
        Application.Begin (top);

        // Is the selected tab view hosting focused
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);
        Assert.Equal (tv.SelectedTab.View, top.Focused.MostFocused);

        // Press the cursor up key to focus the selected tab
        var args = new Key (Key.CursorUp);
        Application.OnKeyDown (args);
        Application.Refresh ();

        // Is the selected tab focused
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        Tab oldChanged = null;
        Tab newChanged = null;

        tv.SelectedTabChanged += (s, e) =>
                                 {
                                     oldChanged = e.OldTab;
                                     newChanged = e.NewTab;
                                 };

        // Press the cursor right key to select the next tab
        args = new Key (Key.CursorRight);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the cursor down key to focus the selected tab view hosting
        args = new Key (Key.CursorDown);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // The tab view hosting is a label which can't be focused
        // and the View container is the focused one
        Assert.Equal (tv.Subviews [1], top.Focused.MostFocused);

        // Press the cursor down key again will focus next view in the toplevel
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (btn, top.Focused);
        Assert.Null (tv.MostFocused);
        Assert.Null (top.Focused.MostFocused);

        // Press the cursor up key to focus the selected tab view hosting again
        args = new Key (Key.CursorUp);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the cursor up key to focus the selected tab
        args = new Key (Key.CursorUp);
        Application.OnKeyDown (args);
        Application.Refresh ();

        // Is the selected tab focused
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the cursor left key to select the previous tab
        args = new Key (Key.CursorLeft);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the end key to select the last tab
        args = new Key (Key.End);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the home key to select the first tab
        args = new Key (Key.Home);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the page down key to select the next set of tabs
        args = new Key (Key.PageDown);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the page up key to select the previous set of tabs
        args = new Key (Key.PageUp);
        Application.OnKeyDown (args);
        Application.Refresh ();
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);
    }

    [Fact]
    public void RemoveAllTabs_ClearsSelection ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.SelectedTab = tab1;
        tv.RemoveTab (tab1);
        tv.RemoveTab (tab2);

        Assert.Null (tv.SelectedTab);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void RemoveTab_ChangesSelection ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.SelectedTab = tab1;
        tv.RemoveTab (tab1);

        Assert.Equal (tab2, tv.SelectedTab);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void RemoveTab_MultipleCalls_NotAnError ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.SelectedTab = tab1;

        // Repeated calls to remove a tab that is not part of
        // the collection should be ignored
        tv.RemoveTab (tab1);
        tv.RemoveTab (tab1);
        tv.RemoveTab (tab1);
        tv.RemoveTab (tab1);

        Assert.Equal (tab2, tv.SelectedTab);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void SelectedTabChanged_Called ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.SelectedTab = tab1;

        Tab oldTab = null;
        Tab newTab = null;
        var called = 0;

        tv.SelectedTabChanged += (s, e) =>
                                 {
                                     oldTab = e.OldTab;
                                     newTab = e.NewTab;
                                     called++;
                                 };

        tv.SelectedTab = tab2;

        Assert.Equal (1, called);
        Assert.Equal (tab1, oldTab);
        Assert.Equal (tab2, newTab);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_False_TabsOnBottom_False_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 3;
        tv.Height = 5;
        tv.Style = new TabStyle { ShowTopLine = false };
        tv.ApplyStyleChanges ();
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
││ 
│╰►
│h│
│ │
└─┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_False_TabsOnBottom_False_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 4;
        tv.Height = 5;
        tv.Style = new TabStyle { ShowTopLine = false };
        tv.ApplyStyleChanges ();
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│T│ 
│ ╰►
│hi│
│  │
└──┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_False_TabsOnBottom_False_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);
        tv.Width = 10;
        tv.Height = 5;
        tv.Style = new TabStyle { ShowTopLine = false };
        tv.ApplyStyleChanges ();

        // Ensures that the tab bar subview gets the bounds of the parent TabView
        tv.LayoutSubviews ();

        // Test two tab names that fit 
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│12│13│   
│  ╰──┴──╮
│hi      │
│        │
└────────┘",
                                                      _output
                                                     );

        tv.SelectedTab = tab2;

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│12│13│   
├──╯  ╰──╮
│hi2     │
│        │
└────────┘",
                                                      _output
                                                     );

        tv.SelectedTab = tab1;

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│1234567│ 
│       ╰►
│hi      │
│        │
└────────┘",
                                                      _output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;
        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│13│      
◄  ╰─────╮
│hi2     │
│        │
└────────┘",
                                                      _output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│abcdefg│ 
◄       ╰╮
│hi2     │
│        │
└────────┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_False_TabsOnBottom_True_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 3;
        tv.Height = 5;
        tv.Style = new TabStyle { ShowTopLine = false, TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌─┐
│h│
│ │
│╭►
││ ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_False_TabsOnBottom_True_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 4;
        tv.Height = 5;
        tv.Style = new TabStyle { ShowTopLine = false, TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──┐
│hi│
│  │
│ ╭►
│T│ ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_False_TabsOnBottom_True_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);
        tv.Width = 10;
        tv.Height = 5;
        tv.Style = new TabStyle { ShowTopLine = false, TabsOnBottom = true };
        tv.ApplyStyleChanges ();

        // Ensures that the tab bar subview gets the bounds of the parent TabView
        tv.LayoutSubviews ();

        // Test two tab names that fit 
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│        │
│  ╭──┬──╯
│12│13│   ",
                                                      _output
                                                     );

        tv.SelectedTab = tab2;

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
│        │
├──╮  ╭──╯
│12│13│   ",
                                                      _output
                                                     );

        tv.SelectedTab = tab1;

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│        │
│       ╭►
│1234567│ ",
                                                      _output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;
        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
│        │
◄  ╭─────╯
│13│      ",
                                                      _output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
│        │
◄       ╭╯
│abcdefg│ ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_False_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 3;
        tv.Height = 5;
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭╮ 
││ 
│╰►
│h│
└─┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_False_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 4;
        tv.Height = 5;
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭─╮ 
│T│ 
│ ╰►
│hi│
└──┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_False_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);
        tv.Width = 10;
        tv.Height = 5;

        // Ensures that the tab bar subview gets the bounds of the parent TabView
        tv.LayoutSubviews ();

        // Test two tab names that fit 
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭──┬──╮   
│12│13│   
│  ╰──┴──╮
│hi      │
└────────┘",
                                                      _output
                                                     );

        tv.SelectedTab = tab2;

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭──┬──╮   
│12│13│   
├──╯  ╰──╮
│hi2     │
└────────┘",
                                                      _output
                                                     );

        tv.SelectedTab = tab1;

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭───────╮ 
│1234567│ 
│       ╰►
│hi      │
└────────┘",
                                                      _output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;
        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭──╮      
│13│      
◄  ╰─────╮
│hi2     │
└────────┘",
                                                      _output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭───────╮ 
│abcdefg│ 
◄       ╰╮
│hi2     │
└────────┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_False_With_Unicode ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);
        tv.Width = 20;
        tv.Height = 5;

        tv.LayoutSubviews ();

        tab1.DisplayText = "Tab0";

        tab2.DisplayText = "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭────╮              
│Tab0│              
│    ╰─────────────►
│hi                │
└──────────────────┘",
                                                      _output
                                                     );

        tv.SelectedTab = tab2;

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
╭──────────────╮    
│Les Misérables│    
◄              ╰───╮
│hi2               │
└──────────────────┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_True_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 3;
        tv.Height = 5;
        tv.Style = new TabStyle { TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌─┐
│h│
│╭►
││ 
╰╯ ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_True_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _, false);
        tv.Width = 4;
        tv.Height = 5;
        tv.Style = new TabStyle { TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.LayoutSubviews ();

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──┐
│hi│
│ ╭►
│T│ 
╰─╯ ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_True_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);
        tv.Width = 10;
        tv.Height = 5;
        tv.Style = new TabStyle { TabsOnBottom = true };
        tv.ApplyStyleChanges ();

        // Ensures that the tab bar subview gets the bounds of the parent TabView
        tv.LayoutSubviews ();

        // Test two tab names that fit 
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│  ╭──┬──╯
│12│13│   
╰──┴──╯   ",
                                                      _output
                                                     );

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│       ╭►
│1234567│ 
╰───────╯ ",
                                                      _output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;
        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
◄  ╭─────╯
│13│      
╰──╯      ",
                                                      _output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
◄       ╭╯
│abcdefg│ 
╰───────╯ ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowTopLine_True_TabsOnBottom_True_With_Unicode ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2, false);
        tv.Width = 20;
        tv.Height = 5;
        tv.Style = new TabStyle { TabsOnBottom = true };
        tv.ApplyStyleChanges ();

        tv.LayoutSubviews ();

        tab1.DisplayText = "Tab0";

        tab2.DisplayText = "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables";

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│hi                │
│    ╭─────────────►
│Tab0│              
╰────╯              ",
                                                      _output
                                                     );

        tv.SelectedTab = tab2;

        tv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│hi2               │
◄              ╭───╯
│Les Misérables│    
╰──────────────╯    ",
                                                      _output
                                                     );
    }

    [Fact]
    public void SwitchTabBy_NormalUsage ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        Tab tab3;
        Tab tab4;
        Tab tab5;

        tv.AddTab (tab3 = new Tab (), false);
        tv.AddTab (tab4 = new Tab (), false);
        tv.AddTab (tab5 = new Tab (), false);

        tv.SelectedTab = tab1;

        var called = 0;
        tv.SelectedTabChanged += (s, e) => { called++; };

        tv.SwitchTabBy (1);

        Assert.Equal (1, called);
        Assert.Equal (tab2, tv.SelectedTab);

        //reset called counter
        called = 0;

        // go right 2
        tv.SwitchTabBy (2);

        // even though we go right 2 indexes the event should only be called once
        Assert.Equal (1, called);
        Assert.Equal (tab4, tv.SelectedTab);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void SwitchTabBy_OutOfTabsRange ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.SelectedTab = tab1;
        tv.SwitchTabBy (500);

        Assert.Equal (tab2, tv.SelectedTab);

        tv.SwitchTabBy (-500);

        Assert.Equal (tab1, tv.SelectedTab);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    private TabView GetTabView () { return GetTabView (out _, out _); }

    private TabView GetTabView (out Tab tab1, out Tab tab2, bool initFakeDriver = true)
    {
        if (initFakeDriver)
        {
            InitFakeDriver ();
        }

        var tv = new TabView ();
        tv.BeginInit ();
        tv.EndInit ();
        tv.ColorScheme = new ColorScheme ();

        tv.AddTab (
                   tab1 = new Tab { DisplayText = "Tab1", View = new TextField { Width = 2, Text = "hi" } },
                   false
                  );
        tv.AddTab (tab2 = new Tab { DisplayText = "Tab2", View = new Label { Text = "hi2" } }, false);

        return tv;
    }

    private void InitFakeDriver ()
    {
        var driver = new FakeDriver ();
        Application.Init (driver);
        driver.Init ();
    }
}
