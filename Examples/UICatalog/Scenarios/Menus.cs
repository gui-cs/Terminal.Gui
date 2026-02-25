#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Menus", "Illustrates Menu and MenuItem")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public class Menus : Scenario
{
    private EventLog? _eventLog;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable runnable = new ();
        runnable.Title = GetQuitKeyAndName ();

        _eventLog = new EventLog
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Height = Dim.Fill (),
            SchemeName = "Runnable",
            BorderStyle = LineStyle.Double,
            Title = "E_vents",
            Arrangement = ViewArrangement.LeftResizable
        };

        MenuDemoHost menuHostView = new ()
        {
            Id = "menuHostView",
            Title = "Menu Demo Host",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (_eventLog),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        runnable.Add (menuHostView);

        _eventLog.SetViewToLog (runnable);
        _eventLog.SetViewToLog (menuHostView);

        runnable.Initialized += (_, _) =>
                                {
                                    foreach (Menu menu in menuHostView.SubViews.OfType<Menu> ())
                                    {
                                        _eventLog.SetViewToLog (menu);

                                        foreach (MenuItem mi in menu.SubViews.OfType<MenuItem> ())
                                        {
                                            _eventLog.SetViewToLog (mi);
                                            _eventLog.SetViewToLog (mi.CommandView);
                                        }
                                    }
                                };

        runnable.Add (_eventLog);

        app.Run (runnable);
    }

    /// <summary>
    ///     A demo view class that demonstrates Menu and MenuItem as direct SubViews.
    /// </summary>
    public class MenuDemoHost : View
    {
        public MenuDemoHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;
        }

        /// <inheritdoc/>
        protected override void OnAccepted (ICommandContext? ctx) => base.OnAccepted (ctx);

        /// <inheritdoc/>
        protected override void OnActivated (ICommandContext? ctx) => base.OnActivated (ctx);

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();

            Label lastCommandLabel = new () { Title = "_Last Command:", X = 1, Y = 0 };

            View lastCommandText = new () { X = Pos.Right (lastCommandLabel) + 1, Y = Pos.Top (lastCommandLabel), Height = Dim.Auto (), Width = Dim.Auto () };

            Add (lastCommandLabel, lastCommandText);

            AddCommand (Command.Quit,
                        _ =>
                        {
                            App?.RequestStop ();

                            return true;
                        });
            HotKeyBindings.Add (Application.QuitKey, Command.Quit);

            // --- Test Menu: demonstrates Menu with MenuItems ---
            Label testMenuLabel = new () { Title = "Menu with MenuItems:", X = 1, Y = Pos.Bottom (lastCommandLabel) + 1 };
            Add (testMenuLabel);

            Menu testMenu = new () { Y = Pos.Bottom (testMenuLabel), Id = "TestMenu" };
            ConfigureTestMenu (testMenu);
            Add (testMenu);

            // --- SubMenu Demo: demonstrates MenuItem.SubMenu for nested menus ---
            Label subMenuLabel = new () { Title = "MenuItem with SubMenu:", X = 1, Y = Pos.Bottom (testMenu) + 1 };
            Add (subMenuLabel);

            Menu subMenuDemo = new () { Y = Pos.Bottom (subMenuLabel) };
            subMenuDemo.EnableForDesign ();
            Add (subMenuDemo);

            // Wire up scenario-specific behavior for the About item
            MenuItem? aboutItem = subMenuDemo.GetMenuItemsOfAllSubMenus (mi => mi.Title == "_About").FirstOrDefault ();

            if (aboutItem is { })
            {
                aboutItem.Activated += (_, _) => MessageBox.Query (App!, "SubMenu Demo", "Demonstrates MenuItem.SubMenu for nested menus.", Strings.btnOk);
            }
        }

        private void ConfigureTestMenu (Menu menu)
        {
            MenuItem menuItem1 = new () { Title = "Z_igzag", Key = Key.I.WithCtrl, Text = "Gonna zig zag" };
            menuItem1.Activated += (_, _) => MessageBox.Query (App!, "This is a MessageBox", "This is a message box message", Strings.btnOk);

            Line line = new ();

            MenuItem menuItemBorders = new () { Title = "_Borders", Text = "Borders", Key = Key.D4.WithAlt };
            menuItemBorders.CommandView = new CheckBox { Title = menuItemBorders.Title, CanFocus = false };

            menuItemBorders.Action += () =>
                                      {
                                          if (menuItemBorders.CommandView is CheckBox cb)
                                          {
                                              menu.BorderStyle = cb.Value == CheckState.Checked ? LineStyle.Double : LineStyle.None;
                                          }
                                      };

            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            menuItemBorders.Accepting += (_, args) => args.Handled = true;

            OptionSelector<Schemes> schemeOptionSelector = new () { Title = "Scheme", CanFocus = true };

            MenuItem menuItemScheme = new () { Title = "Scheme", Text = "Scheme", Key = Key.S.WithCtrl, CommandView = schemeOptionSelector };

            schemeOptionSelector.ValueChanged += (_, args) =>
                                                 {
                                                     if (args.Value is { } scheme)
                                                     {
                                                         menu.SchemeName = scheme.ToString ();
                                                     }
                                                 };

            menu.Add (menuItem1, line, menuItemBorders, menuItemScheme);
        }
    }
}
