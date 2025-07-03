using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tab View", "Demos TabView control with limited screen space in Absolute layout.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TabView")]
public class TabViewExample : Scenario
{
    private MenuItem _miShowBorder;
    private MenuItem _miShowTabViewBorder;
    private MenuItem _miShowTopLine;
    private MenuItem [] _miTabsSide;
    private MenuItem _cachedTabsSide;
    private MenuItem [] _miTabsTextAlignment;
    private MenuItem _cachedTabsTextAlignment;
    private TabView _tabView;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Toplevel appWindow = new ();

        _miTabsSide = SetTabsSide ();
        _miTabsTextAlignment = SetTabsTextAlignment ();

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new ("_Add Blank Tab", "", AddBlankTab),
                         new (
                              "_Clear SelectedTab",
                              "",
                              () => _tabView.SelectedTab = null
                             ),
                         new ("_Quit", "", Quit)
                     }
                    ),
                new (
                     "_View",
                     new []
                     {
                         _miShowTopLine =
                             new ("_Show Top Line", "", ShowTopLine)
                             {
                                 Checked = true, CheckType = MenuItemCheckStyle.Checked
                             },
                         _miShowBorder =
                             new ("_Show Border", "", ShowBorder)
                             {
                                 Checked = true, CheckType = MenuItemCheckStyle.Checked
                             },
                         null,
                         _miTabsSide [0],
                         _miTabsSide [1],
                         _miTabsSide [2],
                         _miTabsSide [3],
                         null,
                         _miShowTabViewBorder =
                             new (
                                  "_Show TabView Border",
                                  "",
                                  ShowTabViewBorder
                                 ) { Checked = true, CheckType = MenuItemCheckStyle.Checked },
                         null,
                         _miTabsTextAlignment [0],
                         _miTabsTextAlignment [1],
                         _miTabsTextAlignment [2],
                         _miTabsTextAlignment [3]
                     }
                    )
            ]
        };
        appWindow.Add (menu);

        _tabView = new()
        {
            Title = "_Tab View",
            X = 0,
            Y = 1,
            Width = 60,
            Height = 20,
            BorderStyle = LineStyle.Single
        };

        _tabView.AddTab (new() { DisplayText = "Tab_1", View = new Label { Text = "hodor!" } }, false);
        _tabView.AddTab (new() { DisplayText = "Tab_2", View = new TextField { Text = "durdur", Width = 10 } }, false);
        _tabView.AddTab (new() { DisplayText = "_Interactive Tab", View = GetInteractiveTab () }, false);
        _tabView.AddTab (new() { DisplayText = "Big Text", View = GetBigTextFileTab () }, false);

        _tabView.AddTab (
                         new()
                         {
                             DisplayText =
                                 "Long name Tab, I mean seriously long.  Like you would not believe how long this tab's name is its just too much really woooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooowwww thats long",
                             View = new Label
                             {
                                 Text =
                                     "This tab has a very long name which should be truncated.  See TabView.MaxTabTextWidth"
                             }
                         },
                         false
                        );

        _tabView.AddTab (
                         new()
                         {
                             DisplayText = "Les Mise" + '\u0301' + "rables", View = new Label { Text = "This tab name is unicode" }
                         },
                         false
                        );

        _tabView.AddTab (
                         new()
                         {
                             DisplayText = "Les Mise" + '\u0328' + '\u0301' + "rables",
                             View = new Label
                             {
                                 Text =
                                     "This tab name has two combining marks. Only one will show due to Issue #2616."
                             }
                         },
                         false
                        );

        for (var i = 0; i < 100; i++)
        {
            _tabView.AddTab (
                             new() { DisplayText = $"Tab{i}", View = new Label { Text = $"Welcome to tab {i}" } },
                             false
                            );
        }

        _tabView.SelectedTab = _tabView.Tabs.First ();

        appWindow.Add (_tabView);

        var frameRight = new View
        {
            X = Pos.Right (_tabView),
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            Title = "_About",
            BorderStyle = LineStyle.Single,
            TabStop = TabBehavior.TabStop,
            CanFocus = true
        };

        frameRight.Add (
                        new TextView
                        {
                            Text = "This demos the tabs control\nSwitch between tabs using cursor keys.\nThis TextView has AllowsTab = false, so tab should nav too.",
                            Width = Dim.Fill (),
                            Height = Dim.Fill (),
                            AllowsTab = false,
                        }
                       );

        appWindow.Add (frameRight);

        var frameBelow = new View
        {
            X = 0,
            Y = Pos.Bottom (_tabView),
            Width = _tabView.Width,
            Height = Dim.Fill (1),
            Title = "B_ottom Frame",
            BorderStyle = LineStyle.Single,
            TabStop = TabBehavior.TabStop,
            CanFocus = true

        };

        frameBelow.Add (
                        new TextView
                        {
                            Text =
                                "This frame exists to check that you can still tab here\nand that the tab control doesn't overspill it's bounds\nAllowsTab is true.",
                            Width = Dim.Fill (),
                            Height = Dim.Fill (),
                        }
                       );

        appWindow.Add (frameBelow);

        var statusBar = new StatusBar ([new (Application.QuitKey, "Quit", Quit)]);
        appWindow.Add (statusBar);

        // Run - Start the application.
        Application.Run (appWindow);

        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void AddBlankTab () { _tabView.AddTab (new (), false); }

    private View GetBigTextFileTab ()
    {
        var text = new TextView { Width = Dim.Fill (), Height = Dim.Fill () };

        var sb = new StringBuilder ();

        for (var y = 0; y < 300; y++)
        {
            for (var x = 0; x < 500; x++)
            {
                sb.Append ((x + y) % 2 == 0 ? '1' : '0');
            }

            sb.AppendLine ();
        }

        text.Text = sb.ToString ();

        return text;
    }

    private View GetInteractiveTab ()
    {
        var interactiveTab = new View
        {
            Width = Dim.Fill (), Height = Dim.Fill (),
            CanFocus = true
        };
        var lblName = new Label { Text = "Name:" };
        interactiveTab.Add (lblName);

        var tbName = new TextField { X = Pos.Right (lblName), Width = 10 };
        interactiveTab.Add (tbName);

        var lblAddr = new Label { Y = 1, Text = "Address:" };
        interactiveTab.Add (lblAddr);

        var tbAddr = new TextField { X = Pos.Right (lblAddr), Y = 1, Width = 10 };
        interactiveTab.Add (tbAddr);

        return interactiveTab;
    }

    private void Quit () { Application.RequestStop (); }

    private MenuItem [] SetTabsSide ()
    {
        List<MenuItem> menuItems = [];

        foreach (TabSide side in Enum.GetValues (typeof (TabSide)))
        {
            string sideName = Enum.GetName (typeof (TabSide), side);
            var item = new MenuItem { Title = $"_{sideName}", Data = side };
            item.CheckType |= MenuItemCheckStyle.Radio;

            item.Action += () =>
                           {
                               if (_cachedTabsSide == item)
                               {
                                   return;
                               }

                               _cachedTabsSide.Checked = false;
                               item.Checked = true;
                               _cachedTabsSide = item;
                               _tabView.Style.TabsSide = (TabSide)item.Data;
                               _tabView.ApplyStyleChanges ();
                           };
            item.ShortcutKey = ((Key)sideName! [0].ToString ().ToLower ()).WithCtrl;

            if (sideName == "Top")
            {
                item.Checked = true;
                _cachedTabsSide = item;
            }

            menuItems.Add (item);
        }

        return menuItems.ToArray ();
    }

    private MenuItem [] SetTabsTextAlignment ()
    {
        List<MenuItem> menuItems = [];

        foreach (TabSide align in Enum.GetValues (typeof (Alignment)))
        {
            string alignName = Enum.GetName (typeof (Alignment), align);
            var item = new MenuItem { Title = $"_{alignName}", Data = align };
            item.CheckType |= MenuItemCheckStyle.Radio;

            item.Action += () =>
                           {
                               if (_cachedTabsTextAlignment == item)
                               {
                                   return;
                               }

                               _cachedTabsTextAlignment.Checked = false;
                               item.Checked = true;
                               _cachedTabsTextAlignment = item;
                               _tabView.Style.TabsTextAlignment = (Alignment)item.Data;
                               _tabView.ApplyStyleChanges ();
                           };
            item.ShortcutKey = ((Key)alignName! [0].ToString ().ToLower ()).WithCtrl;

            if (alignName == "Start")
            {
                item.Checked = true;
                _cachedTabsTextAlignment = item;
            }

            menuItems.Add (item);
        }

        return menuItems.ToArray ();
    }

    private void ShowBorder ()
    {
        _miShowBorder.Checked = !_miShowBorder.Checked;

        _tabView.Style.ShowBorder = (bool)_miShowBorder.Checked;
        _tabView.ApplyStyleChanges ();
    }

    private void ShowTabViewBorder ()
    {
        _miShowTabViewBorder.Checked = !_miShowTabViewBorder.Checked;

        _tabView.BorderStyle = _miShowTabViewBorder.Checked == true
                                   ? _tabView.BorderStyle = LineStyle.Single
                                   : LineStyle.None;
        _tabView.ApplyStyleChanges ();
    }

    private void ShowTopLine ()
    {
        _miShowTopLine.Checked = !_miShowTopLine.Checked;

        _tabView.Style.ShowInitialLine = (bool)_miShowTopLine.Checked;
        _tabView.ApplyStyleChanges ();
    }
}
