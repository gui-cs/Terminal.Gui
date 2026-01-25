#nullable enable

using System.Linq;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tab View", "Demos TabView control with limited screen space in Absolute layout.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TabView")]
public class TabViewExample : Scenario
{
    private CheckBox? _miShowBorderCheckBox;
    private CheckBox? _miShowTabViewBorderCheckBox;
    private CheckBox? _miShowTopLineCheckBox;
    private CheckBox? _miTabsOnBottomCheckBox;
    private TabView? _tabView;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ()
        {
            BorderStyle = LineStyle.None
        };

        // MenuBar
        MenuBar menu = new ();

        _tabView = new ()
        {
            Title = "_Tab View",
            X = 0,
            Y = Pos.Bottom (menu),
            Width = 60,
            Height = 20,
            BorderStyle = LineStyle.Single
        };

        _tabView.AddTab (new () { DisplayText = "Tab_1", View = new Label { Text = "hodor!" } }, false);
        _tabView.AddTab (new () { DisplayText = "Tab_2", View = new TextField { Text = "durdur", Width = 10 } }, false);
        _tabView.AddTab (new () { DisplayText = "_Interactive Tab", View = GetInteractiveTab () }, false);
        _tabView.AddTab (new () { DisplayText = "Big Text", View = GetBigTextFileTab () }, false);

        _tabView.AddTab (
                         new ()
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
                         new ()
                         {
                             DisplayText = "Les Mise" + '\u0301' + "rables",
                             View = new Label { Text = "This tab name is unicode" }
                         },
                         false
                        );

        _tabView.AddTab (
                         new ()
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
                             new () { DisplayText = $"Tab{i}", View = new Label { Text = $"Welcome to tab {i}" } },
                             false
                            );
        }

        _tabView.SelectedTab = _tabView.Tabs.First ();

        View frameRight = new ()
        {
            X = Pos.Right (_tabView),
            Y = Pos.Top (_tabView),
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
                            Text =
                                "This demos the tabs control\nSwitch between tabs using cursor keys.\nThis TextView has AllowsTab = false, so tab should nav too.",
                            Width = Dim.Fill (),
                            Height = Dim.Fill (),
                            TabKeyAddsTab = false
                        }
                       );

        View frameBelow = new ()
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
                            Height = Dim.Fill ()
                        }
                       );

        // StatusBar
        StatusBar statusBar = new (
                                   [
                                       new (Application.QuitKey, "Quit", Quit)
                                   ]
                                  );

        // Setup menu checkboxes
        _miShowTopLineCheckBox = new ()
        {
            Title = "_Show Top Line",
            CheckedState = CheckState.Checked
        };
        _miShowTopLineCheckBox.CheckedStateChanged += (_, _) => ShowTopLine ();

        _miShowBorderCheckBox = new ()
        {
            Title = "_Show Border",
            CheckedState = CheckState.Checked
        };
        _miShowBorderCheckBox.CheckedStateChanged += (_, _) => ShowBorder ();

        _miTabsOnBottomCheckBox = new ()
        {
            Title = "_Tabs On Bottom"
        };
        _miTabsOnBottomCheckBox.CheckedStateChanged += (_, _) => SetTabsOnBottom ();

        _miShowTabViewBorderCheckBox = new ()
        {
            Title = "_Show TabView Border",
            CheckedState = CheckState.Checked
        };
        _miShowTabViewBorderCheckBox.CheckedStateChanged += (_, _) => ShowTabViewBorder ();

        menu.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuItem
                                       {
                                           Title = "_Add Blank Tab",
                                           Action = AddBlankTab
                                       },
                                       new MenuItem
                                       {
                                           Title = "_Clear SelectedTab",
                                           Action = () =>
                                                    {
                                                        if (_tabView is not null)
                                                        {
                                                            _tabView.SelectedTab = null;
                                                        }
                                                    }
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        menu.Add (
                  new MenuBarItem (
                                   "_View",
                                   [
                                       new MenuItem
                                       {
                                           CommandView = _miShowTopLineCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miShowBorderCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miTabsOnBottomCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miShowTabViewBorderCheckBox
                                       }
                                   ]
                                  )
                 );

        appWindow.Add (menu, _tabView, frameRight, frameBelow, statusBar);

        app.Run (appWindow);
    }

    private void AddBlankTab () { _tabView?.AddTab (new (), false); }

    private View GetBigTextFileTab ()
    {
        TextView text = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        StringBuilder sb = new ();

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
        View interactiveTab = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };
        Label lblName = new () { Text = "Name:" };
        interactiveTab.Add (lblName);

        TextField tbName = new () { X = Pos.Right (lblName), Width = 10 };
        interactiveTab.Add (tbName);

        Label lblAddr = new () { Y = 1, Text = "Address:" };
        interactiveTab.Add (lblAddr);

        TextField tbAddr = new () { X = Pos.Right (lblAddr), Y = 1, Width = 10 };
        interactiveTab.Add (tbAddr);

        return interactiveTab;
    }

    private void Quit () { _tabView?.App?.RequestStop (); }

    private void SetTabsOnBottom ()
    {
        if (_tabView is null || _miTabsOnBottomCheckBox is null)
        {
            return;
        }

        _tabView.Style.TabsOnBottom = _miTabsOnBottomCheckBox.CheckedState == CheckState.Checked;
        _tabView.ApplyStyleChanges ();
    }

    private void ShowBorder ()
    {
        if (_tabView is null || _miShowBorderCheckBox is null)
        {
            return;
        }

        _tabView.Style.ShowBorder = _miShowBorderCheckBox.CheckedState == CheckState.Checked;
        _tabView.ApplyStyleChanges ();
    }

    private void ShowTabViewBorder ()
    {
        if (_tabView is null || _miShowTabViewBorderCheckBox is null)
        {
            return;
        }

        _tabView.BorderStyle = _miShowTabViewBorderCheckBox.CheckedState == CheckState.Checked
                                   ? LineStyle.Single
                                   : LineStyle.None;
        _tabView.ApplyStyleChanges ();
    }

    private void ShowTopLine ()
    {
        if (_tabView is null || _miShowTopLineCheckBox is null)
        {
            return;
        }

        _tabView.Style.ShowTopLine = _miShowTopLineCheckBox.CheckedState == CheckState.Checked;
        _tabView.ApplyStyleChanges ();
    }
}
