#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("TabViews", "Demonstrates the TabView control.")]
[ScenarioCategory ("Controls")]
public class TabViewExample : Scenario
{
    private int _tabCounter;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.Title = GetQuitKeyAndName ();

        FrameView configFrame = new ()
        {
            Title = "Confi_guration",
            Y = Pos.AnchorEnd (),
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            SchemeName = "Dialog"
        };

        EventLog eventLog = new ()
        {
            X = Pos.AnchorEnd (),
            Height = Dim.Fill ()
        };

        FrameView demoFrame = new ()
        {
            Title = "TabView _Demo",
            X = 0,
            Y = 0,
            Width = Dim.Fill (eventLog),
            Height = Dim.Fill (configFrame)
        };

        TabView tabView = new ()
        {
            X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill (),
            AssignHotKeys = true,
            Arrangement = ViewArrangement.Resizable // Enables resizing the TabView by dragging its edges for testing
        };

        // Add some default tabs
        Tab tab1 = new () { Title = "Tab 1" };
        tab1.Add (new Label { Text = "This is the content of Tab 1.\nIt has a label." });

        Tab tab2 = new () { Title = "Tab 2" };
        TextField tf = new () { Text = "Edit me", Width = 20, Y = 0 };
        Button btn = new () { Text = "Clic_k Me", Y = 2 };
        btn.Accepting += (_, _) => { btn.Text = "Clic_ked!"; };
        tab2.Add (tf, btn);

        Tab tab3 = new () { Title = "Tab Three" };
        Label label = new () { Text = "Third tab content.\nWith multiple lines.\nLine 3." };
        tab3.Add (label);

        OptionSelector<Side> sideSelector = new ()
        {
            Title = "_Side",
            BorderStyle = LineStyle.Dashed,
            AssignHotKeys = true,
            Y = Pos.Bottom (label) + 1
        };
        tab3.Add (sideSelector);

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;
        _tabCounter = 3;

        configFrame.Width = Dim.Width (demoFrame);

        demoFrame.Add (tabView);

        var configY = 0;

        // TabsOnBottom checkbox
        CheckBox tabsOnBottomCb = new () { Text = "Tabs On _Bottom", X = 0, Y = configY++ };

        tabsOnBottomCb.ValueChanged += (_, args) => { tabView.TabsOnBottom = args.NewValue == CheckState.Checked; };

        configFrame.Add (tabsOnBottomCb);

        configY++;

        // MaxTabTextWidth
        Label maxWidthLabel = new () { Text = "MaxTabTextWidth:", X = 0, Y = configY };

        NumericUpDown maxWidthUpDown = new () { X = Pos.Right (maxWidthLabel) + 1, Y = configY, Value = (int)tabView.MaxTabTextWidth };

        maxWidthUpDown.ValueChanging += (_, args) =>
                                        {
                                            if (args.NewValue < 1 || args.NewValue > 100)
                                            {
                                                args.Handled = true;
                                            }
                                        };

        maxWidthUpDown.ValueChanged += (_, args) => { tabView.MaxTabTextWidth = (uint)args.NewValue; };

        configFrame.Add (maxWidthLabel, maxWidthUpDown);
        configY += 2;

        // Add Tab button
        Button addTabBtn = new () { Text = "_Add Tab", X = 0, Y = configY };

        addTabBtn.Accepting += (_, _) =>
                               {
                                   _tabCounter++;
                                   Tab newTab = new () { Title = $"Tab {_tabCounter}" };
                                   newTab.Add (new Label { Text = $"Content of Tab {_tabCounter}" });
                                   tabView.Add (newTab);
                               };

        configFrame.Add (addTabBtn);

        // Remove Tab button
        Button removeTabBtn = new () { Text = "_Remove Tab", X = Pos.Right (addTabBtn) + 1, Y = configY };

        removeTabBtn.Accepting += (_, _) =>
                                  {
                                      Tab? selected = tabView.SelectedTab;

                                      if (selected is { })
                                      {
                                          tabView.Remove (selected);
                                          selected.Dispose ();
                                      }
                                  };

        configFrame.Add (removeTabBtn);
        configY += 2;

        // SelectedTabIndex
        Label selectedLabel = new () { Text = "SelectedTabIndex:", X = 0, Y = configY };

        Label selectedValueLabel = new () { Text = tabView.SelectedTabIndex?.ToString () ?? "null", X = Pos.Right (selectedLabel) + 1, Y = configY, Width = 6 };

        tabView.SelectedTabChanged += (_, args) => { selectedValueLabel.Text = tabView.SelectedTabIndex?.ToString () ?? "null"; };

        configFrame.Add (selectedLabel, selectedValueLabel);
        configY += 2;

        // Select Previous / Next buttons
        Button prevBtn = new () { Text = "_Prev", X = 0, Y = configY };

        prevBtn.Accepting += (_, _) =>
                             {
                                 if (tabView.SelectedTabIndex.HasValue && tabView.SelectedTabIndex.Value > 0)
                                 {
                                     tabView.SelectedTabIndex = tabView.SelectedTabIndex.Value - 1;
                                 }
                             };

        Button nextBtn = new () { Text = "_Next", X = Pos.Right (prevBtn) + 1, Y = configY };

        nextBtn.Accepting += (_, _) =>
                             {
                                 if (tabView.SelectedTabIndex.HasValue && tabView.SelectedTabIndex.Value < tabView.Tabs.Count - 1)
                                 {
                                     tabView.SelectedTabIndex = tabView.SelectedTabIndex.Value + 1;
                                 }
                             };

        configFrame.Add (prevBtn, nextBtn);
        configY += 2;

        // AdornmentsEditor
        AdornmentsEditor adornmentsEditor = new ()
        {
            X = 0,
            Y = configY,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            AutoSelectViewToEdit = false
        };

        adornmentsEditor.ViewToEdit = tabView;
        configFrame.Add (adornmentsEditor);


        eventLog.ViewToLog = tabView;

        mainWindow.Add (demoFrame, configFrame, eventLog);

        app.Run (mainWindow);
    }
}
