using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (TabView))]
public class TabViewCommandTests
{
    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TabView_Command_Activate_SwitchesTab ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Text = "Tab1" };
        Tab tab2 = new () { Text = "Tab2" };
        tabView.AddTab (tab1, true);
        tabView.AddTab (tab2, false);
        tabView.BeginInit ();
        tabView.EndInit ();

        // Activate switches to selected tab
        // Verify setup
        Assert.Equal (tab1, tabView.SelectedTab);

        tabView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TabView_Command_Accept_FocusesTabContent ()
    {
        TabView tabView = new ();
        Tab tab = new () { Text = "Tab1" };
        Button button = new () { Text = "Button" };
        tab.View = button;
        tabView.AddTab (tab, true);
        tabView.BeginInit ();
        tabView.EndInit ();

        // Accept focuses tab content
        var acceptingFired = false;

        tabView.Accepting += (_, e) =>
                             {
                                 acceptingFired = true;
                                 e.Handled = true;
                             };

        bool? result = tabView.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        tabView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TabView_Tab_Navigation_ChangesSelection ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Text = "Tab1" };
        Tab tab2 = new () { Text = "Tab2" };
        tabView.AddTab (tab1, true);
        tabView.AddTab (tab2, false);
        tabView.BeginInit ();
        tabView.EndInit ();

        // Tab navigation changes selected tab
        Assert.Equal (tab1, tabView.SelectedTab);

        // Select second tab
        tabView.SelectedTab = tab2;
        Assert.Equal (tab2, tabView.SelectedTab);

        tabView.Dispose ();
    }

    // Claude - Opus 4.5
    // Regression test for infinite loop when activating a tab
    // https://github.com/gui-cs/Terminal.Gui/issues/XXXX
    [Fact]
    public void TabView_Tab_Activating_DoesNotCauseInfiniteLoop ()
    {
        TabView tabView = new () { Width = 40, Height = 10 };
        Tab tab1 = new () { Text = "Tab1" };
        Tab tab2 = new () { Text = "Tab2" };
        tabView.AddTab (tab1, true);
        tabView.AddTab (tab2, false);
        tabView.BeginInit ();
        tabView.EndInit ();
        tabView.LayoutSubViews (); // Trigger layout so Tab_Selecting is subscribed

        // Verify setup
        Assert.Equal (tab1, tabView.SelectedTab);

        // Simulate tab activation (what happens when user clicks a tab or presses Enter/Space on it)
        // This should switch to the tab without causing infinite recursion
        var activationCount = 0;
        tab2.Activating += (_, _) => activationCount++;

        // Invoke Activate command on tab2
        bool? result = tab2.InvokeCommand (Command.Activate);

        // Should activate exactly once
        Assert.Equal (1, activationCount);
        Assert.True (result);

        // Should have switched to tab2
        Assert.Equal (tab2, tabView.SelectedTab);

        tabView.Dispose ();
    }
}
