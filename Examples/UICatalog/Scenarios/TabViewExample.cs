#nullable enable
using Terminal.Gui.Views;

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

        // === Left side: Demo TabView ===
        FrameView demoFrame = new ()
        {
            Title = "TabView _Demo",
            X = 0,
            Y = 0,
            Width = Dim.Percent (60),
            Height = Dim.Fill (10),
        };

        TabView tabView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };

        // Add some default tabs
        Tab tab1 = new () { Title = "Tab _1" };
        tab1.Add (new Label { Text = "This is the content of Tab 1.\nIt has a label." });

        Tab tab2 = new () { Title = "Tab _2" };
        TextField tf = new () { Text = "Edit me", Width = 20, Y = 0 };
        Button btn = new () { Text = "Click Me", Y = 2 };
        btn.Accepting += (_, _) => { btn.Text = "Clicked!"; };
        tab2.Add (tf, btn);

        Tab tab3 = new () { Title = "Tab T_hree" };
        tab3.Add (new Label { Text = "Third tab content.\nWith multiple lines.\nLine 3." });

        tabView.Add (tab1, tab2, tab3);
        tabView.SelectedTabIndex = 0;
        _tabCounter = 3;

        demoFrame.Add (tabView);
        mainWindow.Add (demoFrame);

        // === Right side: Configuration pane ===
        FrameView configFrame = new ()
        {
            Title = "Confi_guration",
            X = Pos.Right (demoFrame),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (10),
            SchemeName = "Dialog",
        };

        int configY = 0;

        // TabsOnBottom checkbox
        CheckBox tabsOnBottomCb = new ()
        {
            Text = "Tabs On _Bottom",
            X = 0,
            Y = configY++,
        };

        tabsOnBottomCb.ValueChanged += (_, args) =>
                                       {
                                           tabView.TabsOnBottom = args.NewValue == CheckState.Checked;
                                       };

        configFrame.Add (tabsOnBottomCb);

        configY++;

        // MaxTabTextWidth
        Label maxWidthLabel = new ()
        {
            Text = "MaxTabTextWidth:",
            X = 0,
            Y = configY,
        };

        NumericUpDown maxWidthUpDown = new ()
        {
            X = Pos.Right (maxWidthLabel) + 1,
            Y = configY,
            Value = (int)tabView.MaxTabTextWidth,
        };

        maxWidthUpDown.ValueChanged += (_, args) =>
                                       {
                                           tabView.MaxTabTextWidth = (uint)args.NewValue;
                                       };

        configFrame.Add (maxWidthLabel, maxWidthUpDown);
        configY += 2;

        // Add Tab button
        Button addTabBtn = new ()
        {
            Text = "_Add Tab",
            X = 0,
            Y = configY,
        };

        addTabBtn.Accepting += (_, _) =>
                               {
                                   _tabCounter++;
                                   Tab newTab = new () { Title = $"Tab {_tabCounter}" };
                                   newTab.Add (new Label { Text = $"Content of Tab {_tabCounter}" });
                                   tabView.Add (newTab);
                               };

        configFrame.Add (addTabBtn);

        // Remove Tab button
        Button removeTabBtn = new ()
        {
            Text = "_Remove Tab",
            X = Pos.Right (addTabBtn) + 1,
            Y = configY,
        };

        removeTabBtn.Accepting += (_, _) =>
                                  {
                                      Tab? selected = tabView.SelectedTab;

                                      if (selected is not null)
                                      {
                                          tabView.Remove (selected);
                                      }
                                  };

        configFrame.Add (removeTabBtn);
        configY += 2;

        // SelectedTabIndex
        Label selectedLabel = new ()
        {
            Text = "SelectedTabIndex:",
            X = 0,
            Y = configY,
        };

        Label selectedValueLabel = new ()
        {
            Text = tabView.SelectedTabIndex?.ToString () ?? "null",
            X = Pos.Right (selectedLabel) + 1,
            Y = configY,
            Width = 6,
        };

        tabView.SelectedTabChanged += (_, args) =>
                                      {
                                          selectedValueLabel.Text = tabView.SelectedTabIndex?.ToString () ?? "null";
                                      };

        configFrame.Add (selectedLabel, selectedValueLabel);
        configY += 2;

        // Select Previous / Next buttons
        Button prevBtn = new ()
        {
            Text = "_Prev",
            X = 0,
            Y = configY,
        };

        prevBtn.Accepting += (_, _) =>
                             {
                                 if (tabView.SelectedTabIndex.HasValue && tabView.SelectedTabIndex.Value > 0)
                                 {
                                     tabView.SelectedTabIndex = tabView.SelectedTabIndex.Value - 1;
                                 }
                             };

        Button nextBtn = new ()
        {
            Text = "_Next",
            X = Pos.Right (prevBtn) + 1,
            Y = configY,
        };

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
            AutoSelectViewToEdit = false,
        };

        adornmentsEditor.ViewToEdit = tabView;
        configFrame.Add (adornmentsEditor);

        mainWindow.Add (configFrame);

        // === Bottom: Event Log ===
        EventLog eventLog = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Height = 10,
            ViewToLog = tabView,
        };

        mainWindow.Add (eventLog);

        Application.Run (mainWindow);
    }
}
