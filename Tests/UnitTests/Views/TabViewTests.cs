using System.Globalization;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class TabViewTests (ITestOutputHelper output)
{
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
    }

    [Fact]
    [SetupFakeDriver]
    public void AddTwoTabs_SecondIsSelected ()
    {
        var tv = new TabView ();
        Tab tab1;
        Tab tab2;
        tv.AddTab (tab1 = new () { DisplayText = "Tab1", View = new TextField { Text = "hi" } }, false);
        tv.AddTab (tab2 = new () { DisplayText = "Tab1", View = new Label { Text = "hi2" } }, true);

        Assert.Equal (2, tv.Tabs.Count);
        Assert.Equal (tab2, tv.SelectedTab);
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
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseClick_ChangesTab ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.Width = 20;
        tv.Height = 5;

        tv.Draw ();

        View tabRow = tv.SubViews.ElementAt (0);
        Assert.Equal ("TabRow", tabRow.GetType ().Name);

        DriverAssert.AssertDriverContentsAre (
                                             @"
╭────┬────╮
│Tab1│Tab2│
│    ╰────┴────────╮
│hi                │
└──────────────────┘
",
                                             output
                                            );

        Tab clicked = null;

        tv.TabClicked += (s, e) => { clicked = e.Tab; };

        var top = new Toplevel ();
        top.Add (tv);
        Application.Begin (top);

        MouseEventArgs args;

        // Waving mouse around does not trigger click
        for (var i = 0; i < 100; i++)
        {
            args = new () { ScreenPosition = new (i, 1), Flags = MouseFlags.ReportMousePosition };
            Application.RaiseMouseEvent (args);
            Application.LayoutAndDraw ();
            Assert.Null (clicked);
            Assert.Equal (tab1, tv.SelectedTab);
        }

        args = new () { ScreenPosition = new (3, 1), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();
        Assert.Equal (tab1, clicked);
        Assert.Equal (tab1, tv.SelectedTab);

        // Click to tab2
        args = new () { ScreenPosition = new (6, 1), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();
        Assert.Equal (tab2, clicked);
        Assert.Equal (tab2, tv.SelectedTab);

        // cancel navigation
        tv.TabClicked += (s, e) =>
                         {
                             clicked = e.Tab;
                             e.MouseEvent.Handled = true;
                         };

        args = new () { ScreenPosition = new (3, 1), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();

        // Tab 1 was clicked but event handler blocked navigation
        Assert.Equal (tab1, clicked);
        Assert.Equal (tab2, tv.SelectedTab);

        args = new () { ScreenPosition = new (12, 1), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();

        // Clicking beyond last tab should raise event with null Tab
        Assert.Null (clicked);
        Assert.Equal (tab2, tv.SelectedTab);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseClick_Right_Left_Arrows_ChangesTab ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.Width = 7;
        tv.Height = 5;

        tv.Draw ();

        View tabRow = tv.SubViews.ElementAt (0);
        Assert.Equal ("TabRow", tabRow.GetType ().Name);

        DriverAssert.AssertDriverContentsAre (
                                             @"
╭────╮
│Tab1│
│    ╰►
│hi   │
└─────┘
",
                                             output
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
        var args = new MouseEventArgs { ScreenPosition = new (6, 2), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();
        Assert.Null (clicked);
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);

        DriverAssert.AssertDriverContentsAre (
                                             @"
╭────╮
│Tab2│
◄    ╰╮
│hi2  │
└─────┘
",
                                             output
                                            );

        // Click the left arrow
        args = new () { ScreenPosition = new (0, 2), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();
        Assert.Null (clicked);
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);

        DriverAssert.AssertDriverContentsAre (
                                             @"
╭────╮
│Tab1│
│    ╰►
│hi   │
└─────┘
",
                                             output
                                            );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseClick_Right_Left_Arrows_ChangesTab_With_Border ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.Width = 9;
        tv.Height = 7;

        Assert.Equal (LineStyle.None, tv.BorderStyle);
        tv.BorderStyle = LineStyle.Single;
        tv.Layout ();

        tv.Draw ();

        View tabRow = tv.SubViews.ElementAt (0);
        Assert.Equal ("TabRow", tabRow.GetType ().Name);

        DriverAssert.AssertDriverContentsAre (
                                             @"
┌───────┐
│╭────╮ │
││Tab1│ │
││    ╰►│
││hi   ││
│└─────┘│
└───────┘
",
                                             output
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
        var args = new MouseEventArgs { ScreenPosition = new (7, 3), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();
        Assert.Null (clicked);
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);

        DriverAssert.AssertDriverContentsAre (
                                             @"
┌───────┐
│╭────╮ │
││Tab2│ │
│◄    ╰╮│
││hi2  ││
│└─────┘│
└───────┘
",
                                             output
                                            );

        // Click the left arrow
        args = new () { ScreenPosition = new (1, 3), Flags = MouseFlags.Button1Clicked };
        Application.RaiseMouseEvent (args);
        Application.LayoutAndDraw ();
        Assert.Null (clicked);
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);

        DriverAssert.AssertDriverContentsAre (
                                             @"
┌───────┐
│╭────╮ │
││Tab1│ │
││    ╰►│
││hi   ││
│└─────┘│
└───────┘
",
                                             output
                                            );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ProcessKey_Down_Up_Right_Left_Home_End_PageDown_PageUp_F6 ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.Width = 7;
        tv.Height = 5;

        var btn = new Button
        {
            Id = "btn",
            Y = Pos.Bottom (tv) + 1,
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
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Application.LayoutAndDraw ();

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
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorRight));
        Application.LayoutAndDraw ();
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the cursor down key. Since the selected tab has no focusable views, the focus should move to the next view in the toplevel
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (btn, top.MostFocused);

        // Add a focusable subview to Selected Tab View which is a Label with CanFocus as false
        var btnSubView = new View ()
        {
            Id = "btnSubView",
            Title = "_SubView",
            CanFocus = true
        };
        tv.SelectedTab.View.Add (btnSubView);

        Assert.False (tv.SelectedTab.View.CanFocus);

        // Press cursor up. Should focus the subview in the selected tab.
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.NotEqual (btnSubView, top.MostFocused);
        Assert.Equal (tab2, top.MostFocused);

        tv.SelectedTab.View.CanFocus = true;
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (btnSubView, top.MostFocused);

        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (tab2, top.MostFocused);
        Assert.NotEqual (btn, top.MostFocused);

        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (btnSubView, top.MostFocused);

        Assert.True (Application.RaiseKeyDownEvent (Key.F6));
        Assert.Equal (tab2, top.MostFocused);

        // Press the cursor down key twice.
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (btn, top.MostFocused);

        // Press the cursor down key again will focus next view in the toplevel, which is the TabView
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        // Due to the RestoreFocus method prioritize the _previouslyFocused, so btnSubView will be focused again
        Assert.Equal (btnSubView, tv.MostFocused);

        // Press the cursor up key to focus the selected tab which it's the only way to do that
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.False (tab2.View.HasFocus);
        Assert.Equal (tv, top.Focused);

        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (tv, top.Focused);
        Assert.Equal (btnSubView, top.MostFocused);

        Assert.True (Application.RaiseKeyDownEvent (Key.F6));
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tab2, top.Focused.MostFocused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the cursor left key to select the previous tab
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorLeft));
        Application.LayoutAndDraw ();
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);
        Assert.Equal (tab1, top.Focused.MostFocused);

        // Press the end key to select the last tab
        Assert.True (Application.RaiseKeyDownEvent (Key.End));
        Application.LayoutAndDraw ();
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the home key to select the first tab
        Assert.True (Application.RaiseKeyDownEvent (Key.Home));
        Application.LayoutAndDraw ();
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the page down key to select the next set of tabs
        Assert.True (Application.RaiseKeyDownEvent (Key.PageDown));
        Application.LayoutAndDraw ();
        Assert.Equal (tab1, oldChanged);
        Assert.Equal (tab2, newChanged);
        Assert.Equal (tab2, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);

        // Press the page up key to select the previous set of tabs
        Assert.True (Application.RaiseKeyDownEvent (Key.PageUp));
        Application.LayoutAndDraw ();
        Assert.Equal (tab2, oldChanged);
        Assert.Equal (tab1, newChanged);
        Assert.Equal (tab1, tv.SelectedTab);
        Assert.Equal (tv, top.Focused);
        Assert.Equal (tv.MostFocused, top.Focused.MostFocused);
        top.Dispose ();
    }

    [Fact]
    public void RemoveAllTabs_ClearsSelection ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.SelectedTab = tab1;
        tv.RemoveTab (tab1);
        tv.RemoveTab (tab2);

        Assert.Null (tv.SelectedTab);
    }

    [Fact]
    public void RemoveTab_ChangesSelection ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.SelectedTab = tab1;
        tv.RemoveTab (tab1);

        Assert.Equal (tab2, tv.SelectedTab);
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
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_False_TabsOnBottom_False_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 3;
        tv.Height = 5;
        tv.Style = new () { ShowTopLine = false };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
││ 
│╰►
│h│
│ │
└─┘",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_False_TabsOnBottom_False_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 4;
        tv.Height = 5;
        tv.Style = new () { ShowTopLine = false };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
│T│ 
│ ╰►
│hi│
│  │
└──┘",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_False_TabsOnBottom_False_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        tv.Width = 10;
        tv.Height = 5;
        tv.Style = new () { ShowTopLine = false };
        tv.ApplyStyleChanges ();

        // Test two tab names that fit
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        // Ensures that the tab bar subview gets the bounds of the parent TabView
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
│12│13│   
│  ╰──┴──╮
│hi      │
│        │
└────────┘",
                                                      output
                                                     );

        tv.SelectedTab = tab2;
        Assert.Equal (tab2, tv.SubViews.First (v => v.Id.Contains ("tabRow")).MostFocused);

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
│12│13│   
├──╯  ╰──╮
│hi2     │
│        │
└────────┘",
                                                      output
                                                     );

        tv.SelectedTab = tab1;

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";
        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
│1234567│ 
│       ╰►
│hi      │
│        │
└────────┘",
                                                      output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
│13│      
◄  ╰─────╮
│hi2     │
│        │
└────────┘",
                                                      output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
│abcdefg│ 
◄       ╰╮
│hi2     │
│        │
└────────┘",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_False_TabsOnBottom_True_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 3;
        tv.Height = 5;
        tv.Style = new () { ShowTopLine = false, TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌─┐
│h│
│ │
│╭►
││ ",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_False_TabsOnBottom_True_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 4;
        tv.Height = 5;
        tv.Style = new () { ShowTopLine = false, TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──┐
│hi│
│  │
│ ╭►
│T│ ",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_False_TabsOnBottom_True_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        tv.Width = 10;
        tv.Height = 5;
        tv.Style = new () { ShowTopLine = false, TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        // Test two tab names that fit
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        tv.Layout ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│        │
│  ╭──┬──╯
│12│13│   ",
                                                      output
                                                     );

        tv.SelectedTab = tab2;
        Assert.Equal (tab2, tv.SubViews.First (v => v.Id.Contains ("tabRow")).MostFocused);

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
│        │
├──╮  ╭──╯
│12│13│   ",
                                                      output
                                                     );

        tv.SelectedTab = tab1;

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│        │
│       ╭►
│1234567│ ",
                                                      output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
│        │
◄  ╭─────╯
│13│      ",
                                                      output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
│        │
◄       ╭╯
│abcdefg│ ",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_False_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 3;
        tv.Height = 5;
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭╮ 
││ 
│╰►
│h│
└─┘",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_False_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 4;
        tv.Height = 5;
        tv.Layout ();

        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭─╮ 
│T│ 
│ ╰►
│hi│
└──┘",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_False_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        tv.Width = 10;
        tv.Height = 5;

        // Test two tab names that fit
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        tv.Layout ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭──┬──╮   
│12│13│   
│  ╰──┴──╮
│hi      │
└────────┘",
                                                      output
                                                     );

        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭──┬──╮   
│12│13│   
├──╯  ╰──╮
│hi2     │
└────────┘",
                                                      output
                                                     );

        tv.SelectedTab = tab1;

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭───────╮ 
│1234567│ 
│       ╰►
│hi      │
└────────┘",
                                                      output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭──╮      
│13│      
◄  ╰─────╮
│hi2     │
└────────┘",
                                                      output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭───────╮ 
│abcdefg│ 
◄       ╰╮
│hi2     │
└────────┘",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_False_With_Unicode ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        tv.Width = 20;
        tv.Height = 5;

        tab1.DisplayText = "Tab0";

        tab2.DisplayText = "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables";

        tv.Layout ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭────╮              
│Tab0│              
│    ╰─────────────►
│hi                │
└──────────────────┘",
                                                      output
                                                     );

        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭──────────────╮    
│Les Misérables│    
◄              ╰───╮
│hi2               │
└──────────────────┘",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_True_TestTabView_Width3 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 3;
        tv.Height = 5;
        tv.Style = new () { TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌─┐
│h│
│╭►
││ 
╰╯ ",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_True_TestTabView_Width4 ()
    {
        TabView tv = GetTabView (out _, out _);
        tv.Width = 4;
        tv.Height = 5;
        tv.Style = new () { TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──┐
│hi│
│ ╭►
│T│ 
╰─╯ ",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_True_TestThinTabView_WithLongNames ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        tv.Width = 10;
        tv.Height = 5;
        tv.Style = new () { TabsOnBottom = true };
        tv.ApplyStyleChanges ();
        tv.Layout ();

        // Test two tab names that fit
        tab1.DisplayText = "12";
        tab2.DisplayText = "13";

        tv.Layout ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│  ╭──┬──╯
│12│13│   
╰──┴──╯   ",
                                                      output
                                                     );

        // Test first tab name too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "13";

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi      │
│       ╭►
│1234567│ 
╰───────╯ ",
                                                      output
                                                     );

        //switch to tab2
        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
◄  ╭─────╯
│13│      
╰──╯      ",
                                                      output
                                                     );

        // now make both tabs too long
        tab1.DisplayText = "12345678910";
        tab2.DisplayText = "abcdefghijklmnopq";

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│hi2     │
◄       ╭╯
│abcdefg│ 
╰───────╯ ",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void ShowTopLine_True_TabsOnBottom_True_With_Unicode ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        tv.Width = 20;
        tv.Height = 5;
        tv.Style = new () { TabsOnBottom = true };
        tv.ApplyStyleChanges ();

        tab1.DisplayText = "Tab0";

        tab2.DisplayText = "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables";

        tv.Layout ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│hi                │
│    ╭─────────────►
│Tab0│              
╰────╯              ",
                                                      output
                                                     );

        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│hi2               │
◄              ╭───╯
│Les Misérables│    
╰──────────────╯    ",
                                                      output
                                                     );
    }

    [Fact]
    public void SwitchTabBy_NormalUsage ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        Tab tab3;
        Tab tab4;
        Tab tab5;

        tv.AddTab (tab3 = new (), false);
        tv.AddTab (tab4 = new (), false);
        tv.AddTab (tab5 = new (), false);

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
    }

    [Fact]
    public void RemoveTab_ThatHasFocus ()
    {
        TabView tv = GetTabView (out Tab _, out Tab tab2);

        tv.SelectedTab = tab2;
        tab2.HasFocus = true;

        Assert.Equal (2, tv.Tabs.Count);

        foreach (Tab t in tv.Tabs.ToArray ())
        {
            tv.RemoveTab (t);
        }

        Assert.Empty (tv.Tabs);
    }

    [Fact]
    [SetupFakeDriver]
    public void Add_Three_TabsOnTop_ChangesTab ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        Tab tab3;

        tv.AddTab (
                   tab3 = new () { Id = "tab3", DisplayText = "Tab3", View = new TextView { Id = "tab3.TextView", Width = 3, Height = 1, Text = "hi3" } },
                   false);

        tv.Width = 20;
        tv.Height = 5;

        tv.Layout ();
        tv.Draw ();

        Assert.Equal (tab1, tv.SelectedTab);

        DriverAssert.AssertDriverContentsAre (
                                             @"
╭────┬────┬────╮
│Tab1│Tab2│Tab3│
│    ╰────┴────┴───╮
│hi                │
└──────────────────┘
",
                                             output
                                            );

        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭────┬────┬────╮    
│Tab1│Tab2│Tab3│    
├────╯    ╰────┴───╮
│hi2               │
└──────────────────┘
",
                                                      output
                                                     );

        tv.SelectedTab = tab3;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
╭────┬────┬────╮    
│Tab1│Tab2│Tab3│    
├────┴────╯    ╰───╮
│hi3               │
└──────────────────┘
",
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Add_Three_TabsOnBottom_ChangesTab ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);
        Tab tab3;

        tv.AddTab (
                   tab3 = new () { Id = "tab3", DisplayText = "Tab3", View = new TextView { Id = "tab3.TextView", Width = 3, Height = 1, Text = "hi3" } },
                   false);

        tv.Width = 20;
        tv.Height = 5;
        tv.Style = new () { TabsOnBottom = true };
        tv.ApplyStyleChanges ();

        tv.Layout ();
        tv.Draw ();

        Assert.Equal (tab1, tv.SelectedTab);

        DriverAssert.AssertDriverContentsAre (
                                             @"
┌──────────────────┐
│hi                │
│    ╭────┬────┬───╯
│Tab1│Tab2│Tab3│
╰────┴────┴────╯
",
                                             output
                                            );

        tv.SelectedTab = tab2;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│hi2               │
├────╮    ╭────┬───╯
│Tab1│Tab2│Tab3│    
╰────┴────┴────╯    
",
                                                      output
                                                     );

        tv.SelectedTab = tab3;

        tv.Layout ();
        View.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│hi3               │
├────┬────╮    ╭───╯
│Tab1│Tab2│Tab3│    
╰────┴────┴────╯    
",
                                                      output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Tab_Get_Focus_By_Press_F6 ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.Width = 20;
        tv.Height = 5;

        Toplevel top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.False (tab1.HasFocus);

        Assert.True (Application.RaiseKeyDownEvent (Key.F6));
        Assert.True (tab1.HasFocus);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Mouse_Wheel_Changes_Tab ()
    {
        TabView tv = GetTabView (out Tab tab1, out Tab tab2);

        tv.Width = 20;
        tv.Height = 5;

        Toplevel top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.False (tab1.HasFocus);

        Application.RaiseMouseEvent (new () { Position = new (1, 1), Flags = MouseFlags.WheeledDown });
        Assert.True (tab2.HasFocus);

        Application.RaiseMouseEvent (new () { Position = new (1, 1), Flags = MouseFlags.WheeledUp });
        Assert.True (tab1.HasFocus);

        Application.RaiseMouseEvent (new () { Position = new (1, 1), Flags = MouseFlags.WheeledRight });
        Assert.True (tab2.HasFocus);

        Application.RaiseMouseEvent (new () { Position = new (1, 1), Flags = MouseFlags.WheeledLeft });
        Assert.True (tab1.HasFocus);
        top.Dispose ();
    }

    private TabView GetTabView () { return GetTabView (out _, out _); }

    private TabView GetTabView (out Tab tab1, out Tab tab2)
    {
        var tv = new TabView () { Id = "tv " };
        tv.BeginInit ();
        tv.EndInit ();
        //tv.Scheme = new ();

        tv.AddTab (
                   tab1 = new () { Id = "tab1", DisplayText = "Tab1", View = new TextField { Id = "tab1.TextField", Width = 2, Text = "hi" } },
                   false
                  );
        tv.AddTab (tab2 = new () { Id = "tab2", DisplayText = "Tab2", View = new Label { Id = "tab1.Label", Text = "hi2" } }, false);

        return tv;
    }
}
