// Claude - Opus 4.6

using UnitTests;

namespace ViewsTests;

public class TabViewTests (ITestOutputHelper output) : TestDriverBase
{
    #region Construction and Properties

    [Fact]
    public void Constructor_DefaultProperties ()
    {
        TabView tabView = new ();

        Assert.Equal (TabBehavior.TabGroup, tabView.TabStop);
        Assert.True (tabView.SuperViewRendersLineCanvas);
        Assert.Equal (LineStyle.Rounded, tabView.BorderStyle);
        Assert.Null (tabView.SelectedTabIndex);
        Assert.Null (tabView.SelectedTab);
        Assert.Empty (tabView.Tabs);
        Assert.False (tabView.TabsOnBottom);
        Assert.Equal (30u, tabView.MaxTabTextWidth);
    }

    [Fact]
    public void Tab_Constructor_DefaultProperties ()
    {
        Tab tab = new ();

        Assert.True (tab.CanFocus);
        Assert.False (tab.Visible);
    }

    [Fact]
    public void Tab_Title_SetsHeaderText ()
    {
        Tab tab = new () { Title = "My Tab" };

        Assert.Equal ("My Tab", tab.Title);
    }

    [Fact]
    public void MaxTabTextWidth_Default_Is30 ()
    {
        TabView tabView = new ();

        Assert.Equal (30u, tabView.MaxTabTextWidth);
    }

    [Fact]
    public void EnableForDesign_Creates3Tabs ()
    {
        TabView tabView = new ();
        bool result = ((IDesignable)tabView).EnableForDesign ();

        Assert.True (result);
        Assert.Equal (3, tabView.Tabs.Count);
        Assert.Equal (0, tabView.SelectedTabIndex);
    }

    #endregion

    #region Tab Add / Remove

    [Fact]
    public void Tabs_ReturnsOnlyTabSubViews ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1, tab2);

        Assert.Equal (2, tabView.Tabs.Count);
        Assert.Same (tab1, tabView.Tabs [0]);
        Assert.Same (tab2, tabView.Tabs [1]);
    }

    [Fact]
    public void Add_NonTab_DoesNotAffectTabs ()
    {
        TabView tabView = new ();
        View regularView = new () { Title = "Not a Tab" };

        tabView.Add (regularView);

        Assert.Empty (tabView.Tabs);
        Assert.Null (tabView.SelectedTabIndex);
    }

    [Fact]
    public void Add_FirstTab_SelectsIt ()
    {
        TabView tabView = new ();
        Tab tab = new () { Title = "First" };

        tabView.Add (tab);

        Assert.Equal (0, tabView.SelectedTabIndex);
        Assert.Same (tab, tabView.SelectedTab);
        Assert.True (tab.Visible);
    }

    [Fact]
    public void Add_SecondTab_DoesNotChangeSelection ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1);
        tabView.Add (tab2);

        Assert.Equal (0, tabView.SelectedTabIndex);
        Assert.Same (tab1, tabView.SelectedTab);
        Assert.True (tab1.Visible);
        Assert.False (tab2.Visible);
    }

    [Fact]
    public void Remove_SelectedTab_AdjustsSelection ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 2;

        tabView.Remove (tab3);

        Assert.Equal (1, tabView.SelectedTabIndex);
        Assert.Same (tab2, tabView.SelectedTab);
    }

    [Fact]
    public void Remove_LastTab_SetsSelectionNull ()
    {
        TabView tabView = new ();
        Tab tab = new () { Title = "Tab1" };
        tabView.Add (tab);

        tabView.Remove (tab);

        Assert.Null (tabView.SelectedTabIndex);
        Assert.Null (tabView.SelectedTab);
        Assert.Empty (tabView.Tabs);
    }

    [Fact]
    public void Remove_NonSelectedTab_KeepsSelection ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        tabView.Remove (tab3);

        Assert.Equal (0, tabView.SelectedTabIndex);
        Assert.Same (tab1, tabView.SelectedTab);
    }

    #endregion

    #region Selection

    [Fact]
    public void SelectedTabIndex_SwitchesVisibility ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        Assert.True (tab1.Visible);
        Assert.False (tab2.Visible);

        tabView.SelectedTabIndex = 1;

        Assert.False (tab1.Visible);
        Assert.True (tab2.Visible);
    }

    [Fact]
    public void SelectedTabIndex_OutOfRange_Ignored ()
    {
        TabView tabView = new ();
        Tab tab = new () { Title = "Tab1" };
        tabView.Add (tab);
        tabView.SelectedTabIndex = 0;

        tabView.SelectedTabIndex = 5;
        Assert.Equal (0, tabView.SelectedTabIndex);

        tabView.SelectedTabIndex = -1;
        Assert.Equal (0, tabView.SelectedTabIndex);
    }

    [Fact]
    public void SelectedTabIndex_Null_HidesAllTabs ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;
        Assert.True (tab1.Visible);

        tabView.SelectedTabIndex = null;

        Assert.Null (tabView.SelectedTab);
        Assert.False (tab1.Visible);
        Assert.False (tab2.Visible);
    }

    [Fact]
    public void Multiple_Tabs_OnlySelectedVisible ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 1;

        Assert.False (tab1.Visible);
        Assert.True (tab2.Visible);
        Assert.False (tab3.Visible);
    }

    [Fact]
    public void SelectedTab_ReturnsCorrectTab ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1, tab2);

        tabView.SelectedTabIndex = 0;
        Assert.Same (tab1, tabView.SelectedTab);

        tabView.SelectedTabIndex = 1;
        Assert.Same (tab2, tabView.SelectedTab);

        tabView.SelectedTabIndex = null;
        Assert.Null (tabView.SelectedTab);
    }

    #endregion

    #region Events

    [Fact]
    public void SelectedTabChanged_RaisedOnSelection ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        Tab? oldTab = null;
        Tab? newTab = null;
        var raised = false;

        tabView.SelectedTabChanged += (_, args) =>
                                      {
                                          raised = true;
                                          oldTab = args.OldValue;
                                          newTab = args.NewValue;
                                      };

        tabView.SelectedTabIndex = 1;

        Assert.True (raised);
        Assert.Same (tab1, oldTab);
        Assert.Same (tab2, newTab);
    }

    [Fact]
    public void SelectedTabChanged_RaisedWhenSetToNull ()
    {
        TabView tabView = new ();
        Tab tab = new () { Title = "Tab1" };
        tabView.Add (tab);

        Tab? oldTab = null;
        Tab? newTab = tab;
        var raised = false;

        tabView.SelectedTabChanged += (_, args) =>
                                      {
                                          raised = true;
                                          oldTab = args.OldValue;
                                          newTab = args.NewValue;
                                      };

        tabView.SelectedTabIndex = null;

        Assert.True (raised);
        Assert.Same (tab, oldTab);
        Assert.Null (newTab);
    }

    #endregion

    #region TabsOnBottom

    [Fact]
    public void TabsOnBottom_SwitchesPaddingThickness ()
    {
        TabView tabView = new ();

        Assert.False (tabView.TabsOnBottom);
        Assert.Equal (3, tabView.Padding!.Thickness.Top);
        Assert.Equal (0, tabView.Padding.Thickness.Bottom);

        tabView.TabsOnBottom = true;

        Assert.Equal (0, tabView.Padding.Thickness.Top);
        Assert.Equal (3, tabView.Padding.Thickness.Bottom);

        tabView.TabsOnBottom = false;

        Assert.Equal (3, tabView.Padding.Thickness.Top);
        Assert.Equal (0, tabView.Padding.Thickness.Bottom);
    }

    [Fact]
    public void TabsOnBottom_SameValue_NoChange ()
    {
        TabView tabView = new ();
        Thickness originalThickness = tabView.Padding!.Thickness;

        tabView.TabsOnBottom = false;

        Assert.Equal (originalThickness, tabView.Padding.Thickness);
    }

    #endregion

    #region Keyboard Navigation Commands

    [Fact]
    public void SelectNextTab_Wraps ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 2;

        tabView.InvokeCommand (Command.Right);

        Assert.Equal (0, tabView.SelectedTabIndex);
    }

    [Fact]
    public void SelectPreviousTab_Wraps ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        tabView.InvokeCommand (Command.Left);

        Assert.Equal (2, tabView.SelectedTabIndex);
    }

    [Fact]
    public void SelectFirstTab_Command ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 2;

        tabView.InvokeCommand (Command.LeftStart);

        Assert.Equal (0, tabView.SelectedTabIndex);
    }

    [Fact]
    public void SelectLastTab_Command ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        tabView.InvokeCommand (Command.RightEnd);

        Assert.Equal (2, tabView.SelectedTabIndex);
    }

    [Fact]
    public void Navigation_NoTabs_ReturnsFalse ()
    {
        TabView tabView = new ();

        bool? result = tabView.InvokeCommand (Command.Right);

        Assert.Equal (false, result);
    }

    [Fact]
    public void KeyBinding_CtrlRight_SelectsNextTab ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        tabView.NewKeyDownEvent (Key.CursorRight.WithCtrl);

        Assert.Equal (1, tabView.SelectedTabIndex);
    }

    [Fact]
    public void KeyBinding_CtrlLeft_SelectsPreviousTab ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 1;

        tabView.NewKeyDownEvent (Key.CursorLeft.WithCtrl);

        Assert.Equal (0, tabView.SelectedTabIndex);
    }

    [Fact]
    public void KeyBinding_CtrlHome_SelectsFirstTab ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 2;

        tabView.NewKeyDownEvent (Key.Home.WithCtrl);

        Assert.Equal (0, tabView.SelectedTabIndex);
    }

    [Fact]
    public void KeyBinding_CtrlEnd_SelectsLastTab ()
    {
        TabView tabView = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        Tab tab3 = new () { Title = "Tab3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        tabView.NewKeyDownEvent (Key.End.WithCtrl);

        Assert.Equal (2, tabView.SelectedTabIndex);
    }

    #endregion

    #region Mouse Interaction

    [Fact]
    public void MouseClick_OnTabHeader_SelectsTab ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        // Get the tab row's header views (Padding -> TabRow -> headers)
        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        Assert.NotEmpty (paddingSubViews);

        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];
        Assert.Equal (2, headers.Length);

        // Invoke Activate command on the second header (simulates mouse click)
        headers [1].InvokeCommand (Command.Activate);

        Assert.Equal (1, tabView.SelectedTabIndex);
        Assert.True (tab2.Visible);
        Assert.False (tab1.Visible);
    }

    [Fact]
    public void MouseClick_OnFirstHeader_SelectsFirstTab ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 1;

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Invoke Activate on the first header
        headers [0].InvokeCommand (Command.Activate);

        Assert.Equal (0, tabView.SelectedTabIndex);
        Assert.True (tab1.Visible);
        Assert.False (tab2.Visible);
    }

    [Fact]
    public void MouseClick_SwitchBetweenTabs ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };
        Tab tab3 = new () { Title = "T3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Click tab 3
        headers [2].InvokeCommand (Command.Activate);
        Assert.Equal (2, tabView.SelectedTabIndex);

        // Click tab 1
        headers [0].InvokeCommand (Command.Activate);
        Assert.Equal (0, tabView.SelectedTabIndex);

        // Click tab 2
        headers [1].InvokeCommand (Command.Activate);
        Assert.Equal (1, tabView.SelectedTabIndex);
    }

    [Fact]
    public void MouseBinding_LeftClick_MapsToActivate ()
    {
        TabView tabView = new () { Width = 30, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        // Verify the mouse binding exists on the header
        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        // Verify headers have LeftButtonClicked bound
        Assert.True (headers [0].MouseBindings.TryGet (MouseFlags.LeftButtonClicked, out _));
        Assert.True (headers [1].MouseBindings.TryGet (MouseFlags.LeftButtonClicked, out _));
    }

    #endregion

    #region Disposal

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_DisposesAllTabs ()
    {
        TabView tabView = new () { Width = 20, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };
        Tab tab3 = new () { Title = "T3" };

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;

        tabView.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (tab1.WasDisposed);
        Assert.True (tab2.WasDisposed);
        Assert.True (tab3.WasDisposed);
#endif
    }

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_DisposesTabRow ()
    {
        TabView tabView = new () { Width = 20, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        tabView.Add (tab1);
        tabView.SelectedTabIndex = 0;

        // Get reference to TabRow before disposing
        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];

        tabView.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (tabRow.WasDisposed);
#endif
    }

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_DisposesTabHeaderViews ()
    {
        TabView tabView = new () { Width = 20, Height = 10 };

        Tab tab1 = new () { Title = "AA" };
        Tab tab2 = new () { Title = "BB" };
        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;
        tabView.Layout ();

        // Get references to header views before disposing
        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] headers = [.. tabRow.SubViews];

        Assert.Equal (2, headers.Length);

        tabView.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (headers [0].WasDisposed);
        Assert.True (headers [1].WasDisposed);
#endif
    }

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_DisposesTabContentSubViews ()
    {
        TabView tabView = new () { Width = 20, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Label label = new () { Text = "Hello" };
        TextField textField = new () { Text = "World", Width = 10, Y = 1 };
        tab1.Add (label, textField);

        tabView.Add (tab1);
        tabView.SelectedTabIndex = 0;

        tabView.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (label.WasDisposed);
        Assert.True (textField.WasDisposed);
#endif
    }

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_DisposesAdornments ()
    {
        TabView tabView = new () { Width = 20, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        tabView.Add (tab1);

        // Capture adornment references before disposing
        Adornment? padding = tabView.Padding;
        Adornment? border = tabView.Border;
        Adornment? margin = tabView.Margin;

        Assert.NotNull (padding);
        Assert.NotNull (border);
        Assert.NotNull (margin);

        tabView.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (padding!.WasDisposed);
        Assert.True (border!.WasDisposed);
        Assert.True (margin!.WasDisposed);
#endif

        // After dispose, adornments should be null
        Assert.Null (tabView.Padding);
        Assert.Null (tabView.Border);
        Assert.Null (tabView.Margin);
    }

    // Claude - Opus 4.6
    [Fact]
    public void RemovedTab_IsNotDisposedByTabViewDispose ()
    {
        TabView tabView = new () { Width = 20, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        Tab tab2 = new () { Title = "T2" };
        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        // Remove tab2 before disposing — it's the caller's responsibility to dispose it
        tabView.Remove (tab2);

        tabView.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (tab1.WasDisposed);
        Assert.False (tab2.WasDisposed);
#endif

        // Caller must dispose the removed tab
        tab2.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.True (tab2.WasDisposed);
#endif
    }

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_WithSelectedTabChangedHandler_DoesNotThrow ()
    {
        TabView tabView = new () { Width = 20, Height = 10 };

        Tab tab1 = new () { Title = "T1" };
        tab1.Add (new Label { Text = "Content1" });

        Tab tab2 = new () { Title = "T2" };
        tab2.Add (new Label { Text = "Content2" });

        tabView.Add (tab1, tab2);
        tabView.SelectedTabIndex = 0;

        // Simulate a scenario handler that updates a label on tab change
        Label statusLabel = new () { Text = "Tab 0" };

        tabView.SelectedTabChanged += (_, _) =>
                                      {
                                          statusLabel.Text = tabView.SelectedTabIndex?.ToString () ?? "null";
                                      };

        // Disposing should not throw ObjectDisposedException even though
        // SubView removal would normally fire SelectedTabChanged.
        Exception? exception = Record.Exception (() => tabView.Dispose ());
        Assert.Null (exception);

        statusLabel.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void RebuildHeaders_DisposesOldHeaders ()
    {
        TabView tabView = new () { Width = 30, Height = 6 };

        Tab tab1 = new () { Title = "T1" };
        tabView.Add (tab1);

        // Get the TabRow and capture its header views
        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];
        View [] oldHeaders = [.. tabRow.SubViews];
        Assert.Single (oldHeaders);

        // Adding a new tab triggers RebuildHeaders, which should dispose old headers
        Tab tab2 = new () { Title = "T2" };
        tabView.Add (tab2);

        // Old headers should have been disposed by RebuildHeaders.
        // After disposal, adornments (Border, Padding, Margin) are set to null.
        Assert.Null (oldHeaders [0].Border);
        Assert.Null (oldHeaders [0].Padding);
        Assert.Null (oldHeaders [0].Margin);

        // New headers should exist and NOT be disposed
        View [] newHeaders = [.. tabRow.SubViews];
        Assert.Equal (2, newHeaders.Length);
        Assert.NotNull (newHeaders [0].Border);
        Assert.NotNull (newHeaders [1].Border);

        tabView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void AddRemoveTabs_DisposesAllOldHeaders ()
    {
        TabView tabView = new () { Width = 30, Height = 6 };

        Tab tab1 = new () { Title = "T1" };
        tabView.Add (tab1);

        View [] paddingSubViews = [.. tabView.Padding!.SubViews];
        View tabRow = paddingSubViews [0];

        // Capture headers from first RebuildHeaders call
        View [] gen1Headers = [.. tabRow.SubViews];
        Assert.Single (gen1Headers);

        // Add tab2 — triggers RebuildHeaders, creating gen2 headers
        Tab tab2 = new () { Title = "T2" };
        tabView.Add (tab2);
        View [] gen2Headers = [.. tabRow.SubViews];
        Assert.Equal (2, gen2Headers.Length);

        // gen1 should be disposed
        Assert.Null (gen1Headers [0].Border);

        // Remove tab1 — triggers RebuildHeaders again, creating gen3 headers
        tabView.Remove (tab1);
        View [] gen3Headers = [.. tabRow.SubViews];
        Assert.Single (gen3Headers);

        // gen2 headers should be disposed
        Assert.Null (gen2Headers [0].Border);
        Assert.Null (gen2Headers [1].Border);

        // gen3 should still be alive
        Assert.NotNull (gen3Headers [0].Border);

        tabView.Dispose ();

        // After full dispose, gen3 headers should also be disposed
        Assert.Null (gen3Headers [0].Border);

        // Caller's responsibility to dispose removed tabs
        tab1.Dispose ();
    }

    #endregion
}
