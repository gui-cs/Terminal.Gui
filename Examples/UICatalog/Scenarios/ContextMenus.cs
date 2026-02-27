#nullable enable
using System.Globalization;

// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ContextMenus", "Illustrates PopoverMenu as a context menu")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public class ContextMenus : Scenario
{
    private PopoverMenu? _winContextMenu;
    private TextField? _tfTopLeft, _tfTopRight, _tfMiddle, _tfBottomLeft, _tfBottomRight;
    private List<CultureInfo>? _cultureInfos;
    private readonly Key _winContextMenuKey = Key.Space.WithCtrl;

    private Window? _appWindow;

    public override void Main ()
    {
        // Init
        ConfigurationManager.Enable (ConfigLocations.All);

        // Prepping for modern app model
        using IApplication app = Application.Create ();
        app.Init ();
        _cultureInfos = Application.SupportedCultures;

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new () { Title = GetQuitKeyAndName (), Arrangement = ViewArrangement.Fixed, SchemeName = "Runnable" };
        _appWindow = appWindow;

        // Changing the key-bindings of a View is not allowed, however,
        // by default, Runnable doesn't bind to Command.Context, so
        // we can take advantage of the CommandNotBound event to handle it
        //
        // An alternative implementation would be to create a Runnable subclass that
        // calls AddCommand/KeyBindings.Add in the constructor. See the Snake game scenario
        // for an example.
        _appWindow.CommandNotBound += HandleCommandNotBound;

        _appWindow.KeyBindings.Add (_winContextMenuKey, Command.Context);
        _appWindow.MouseBindings.Add (MouseFlags.RightButtonClicked, Command.Context);

        _appWindow.Initialized += AppWindowOnInitialized;

        // Run - Start the application.
        app.Run (_appWindow);
        _appWindow.Dispose ();
        _winContextMenu?.Dispose ();

        return;

        void AppWindowOnInitialized (object? sender, EventArgs e)
        {
            const string TEXT = "Context Menu";
            const int WIDTH = 20;

            CreateWinContextMenu ((sender as Window)!.App);

            Label label = new () { X = Pos.Center (), Y = 1, Text = $"Press '{_winContextMenuKey}' to open the Window context menu." };
            _appWindow.Add (label);

            label = new Label { X = Pos.Center (), Y = Pos.Bottom (label), Text = $"Press '{PopoverMenu.DefaultKey}' to open the TextField context menu." };
            _appWindow.Add (label);

            _tfTopLeft = new TextField { Id = "_tfTopLeft", Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfTopLeft);

            _tfTopRight = new TextField { Id = "_tfTopRight", X = Pos.AnchorEnd (WIDTH), Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfTopRight);

            _tfMiddle = new TextField
            {
                Id = "_tfMiddle",
                X = Pos.Center (),
                Y = Pos.Center (),
                Width = WIDTH,
                Text = TEXT
            };
            _appWindow.Add (_tfMiddle);

            _tfBottomLeft = new TextField { Id = "_tfBottomLeft", Y = Pos.AnchorEnd (1), Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfBottomLeft);

            _tfBottomRight = new TextField
            {
                Id = "_tfBottomRight",
                X = Pos.AnchorEnd (WIDTH),
                Y = Pos.AnchorEnd (1),
                Width = WIDTH,
                Text = TEXT
            };
            _appWindow.Add (_tfBottomRight);

            CultureInfo originalCulture = Thread.CurrentThread.CurrentUICulture;

            _appWindow.IsRunningChanged += (_, args) =>
                                           {
                                               if (!args.Value)
                                               {
                                                   Thread.CurrentThread.CurrentUICulture = originalCulture;
                                               }
                                           };

            // PopoverMenu-with-Menu-Root demo: a FrameView with its own context menu
            PopoverMenuHost popoverMenuHost = new ()
            {
                X = Pos.Center (),
                Y = Pos.Bottom (_tfMiddle!) + 1,
                Width = 50,
                Height = 10,
                Title = $"PopoverMenu Host - Right-click or {PopoverMenu.DefaultKey}",
                BorderStyle = LineStyle.Dashed
            };

            _appWindow.CommandsToBubbleUp = [Command.Activate];

            popoverMenuHost.Activated += (s, args) =>
                                         {
                                             // If the Activate command is from the Borders menu item, toggle the border style on the MenuHostView
                                             if (args.Value?.TryGetSource (out View? source) is true && source is CheckBox { Id: "bordersCheckbox" } bordersCheckbox)
                                             {
                                                 _appWindow.BorderStyle = (args.Value?.Value as CheckState?) == CheckState.Checked ? LineStyle.Double : LineStyle.None;

                                                 return;
                                             }

                                             if (args.Value?.TryGetSource (out source) is true
                                                 && source?.SuperView is OptionSelector<Schemes> { Id: "schemeOptionSelector" } schemeOptionSelector)
                                             {
                                                 if (schemeOptionSelector.Value is { } scheme)
                                                 {
                                                     _appWindow.SchemeName = scheme.ToString ();
                                                 }
                                             }
                                         };
            _appWindow.Add (popoverMenuHost);
        }
    }

    /// <summary>
    ///     A demo view that owns a PopoverMenu with a Menu as Root, demonstrating
    ///     the PopoverMenu-as-context-menu pattern.
    /// </summary>
    private class PopoverMenuHost : FrameView
    {
        private PopoverMenu? _popoverMenu;

        public PopoverMenuHost ()
        {
            CanFocus = true;

            base.Text = "Right-click or press the context menu key to open.";
            TextAlignment = Alignment.Center;
            VerticalTextAlignment = Alignment.Center;
            CommandsToBubbleUp = [Command.Activate];
        }

        /// <inheritdoc/>
        protected override void OnActivated (ICommandContext? ctx)
        {
            base.OnActivated (ctx);

            MenuItem? menuItem = _popoverMenu?.Root?.GetMenuItemsOfAllSubMenus (item => item.Id == "menuItemBorders").FirstOrDefault ();

            if (menuItem?.CommandView is CheckBox bordersCheckBox)
            {
                BorderStyle = bordersCheckBox.Value == CheckState.Checked ? LineStyle.Double : LineStyle.None;
            }
        }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();

            _popoverMenu = new PopoverMenu { Title = "ContextMenu", Id = "PopoverMenuHostContextMenu" };
            _popoverMenu.Target = new WeakReference<View> (this); // Bridge commands to this host

            Menu testContextMenu = new () { Id = "TestContextMenu" };
            _popoverMenu.Root = testContextMenu;
            ConfigureTestMenu (testContextMenu);
            _popoverMenu.Visible = false;

            _popoverMenu.Activated += (_, _) =>
                                      {
                                          MenuItem? menuItem = _popoverMenu.Root?.GetMenuItemsOfAllSubMenus (item => item.Id == "menuItemScheme")
                                                                           .FirstOrDefault ();

                                          if (menuItem?.CommandView is OptionSelector<Schemes> schemeSelector)
                                          {
                                              SchemeName = schemeSelector.Value.ToString ();
                                          }
                                      };

            AddCommand (Command.Context,
                        _ =>
                        {
                            _popoverMenu?.MakeVisible ();

                            return true;
                        });

            MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked, Command.Context);
            KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

            App?.Popovers?.Register (_popoverMenu);
        }

        private void ConfigureTestMenu (Menu menu)
        {
            MenuItem menuItem1 = new () { Title = "Z_igzag", Key = Key.I.WithCtrl, Text = "Gonna zig zag" };
            menuItem1.Activated += (_, _) => MessageBox.Query (App!, "This is a MessageBox", "This is a message box message", Strings.btnOk);

            Line line = new ();

            MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", Text = "Borders", Key = Key.D4.WithAlt };
            menuItemBorders.CommandView = new CheckBox { Id = "bordersCheckbox", Title = menuItemBorders.Title, CanFocus = false };

            menuItemBorders.Action += () =>
                                      {
                                          if (menuItemBorders.CommandView is CheckBox cb)
                                          {
                                              menu.BorderStyle = cb.Value == CheckState.Checked ? LineStyle.Double : LineStyle.None;
                                          }
                                      };

            OptionSelector<Schemes> schemeOptionSelector = new () { Id = "schemeOptionSelector", Title = "Scheme", CanFocus = true };

            MenuItem menuItemScheme = new ()
            {
                Id = "menuItemScheme",
                Title = "_Scheme",
                Text = "Scheme",
                Key = Key.S.WithCtrl,
                CommandView = schemeOptionSelector
            };

            schemeOptionSelector.ValueChanged += (_, args) =>
                                                 {
                                                     if (args.Value is { } scheme)
                                                     {
                                                         menu.SchemeName = scheme.ToString ();
                                                     }
                                                 };

            menu.Add (menuItem1, line, menuItemBorders, menuItemScheme);
        }

        /// <inheritdoc/>
        protected override void Dispose (bool disposing)
        {
            if (_popoverMenu is { })
            {
                _popoverMenu.Dispose ();
                _popoverMenu = null;
            }

            base.Dispose (disposing);
        }
    }

    private void HandleCommandNotBound (object? sender, CommandEventArgs e)
    {
        switch (e.Context?.Binding)
        {
            case MouseBinding { MouseEvent: { } mouseArgs }:
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible (mouseArgs.ScreenPosition);
                e.Handled = true;

                break;

            case KeyBinding { Key: { } key } when key == _winContextMenuKey:
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible ();
                e.Handled = true;

                break;
        }
    }

    private void CreateWinContextMenu (IApplication? app)
    {
        _winContextMenu = new PopoverMenu ([
                                               new MenuItem { Title = "C_ultures", SubMenu = GetSupportedCultureMenu () },
                                               new Line (),
                                               new MenuItem
                                               {
                                                   Title = "_Configuration...",
                                                   HelpText = "Show configuration",
                                                   Action =
                                                       () => MessageBox.Query (app!, 50, 10, "Configuration", "This would be a configuration dialog", "Ok")
                                               },
                                               new MenuItem
                                               {
                                                   Title = "M_ore options",
                                                   SubMenu = new Menu ([
                                                                           new MenuItem
                                                                           {
                                                                               Title = "_Setup...",
                                                                               HelpText = "Perform setup",
                                                                               Action =
                                                                                   () => MessageBox.Query (app!,
                                                                                       50,
                                                                                       10,
                                                                                       "Setup",
                                                                                       "This would be a setup dialog",
                                                                                       "Ok"),
                                                                               Key = Key.T.WithCtrl
                                                                           },
                                                                           new MenuItem
                                                                           {
                                                                               Title = "_Maintenance...",
                                                                               HelpText = "Maintenance mode",
                                                                               Action =
                                                                                   () => MessageBox.Query (app!,
                                                                                       50,
                                                                                       10,
                                                                                       "Maintenance",
                                                                                       "This would be a maintenance dialog",
                                                                                       "Ok")
                                                                           }
                                                                       ])
                                               },
                                               new Line (),
                                               new MenuItem { Title = Strings.cmdQuit, Action = () => app!.RequestStop () }
                                           ])
        { Key = _winContextMenuKey };
        app!.Popovers?.Register (_winContextMenu);
    }

    private Menu GetSupportedCultureMenu ()
    {
        List<MenuItem> supportedCultures = [];
        int index = -1;

        foreach (CultureInfo c in _cultureInfos!)
        {
            MenuItem culture = new ();

            culture.CommandView = new CheckBox { CanFocus = false };

            if (index == -1)
            {
                // Create English because GetSupportedCultures doesn't include it
                culture.Id = "_English";
                culture.Title = "_English";
                culture.HelpText = "en-US";

                ((CheckBox)culture.CommandView).Value = Thread.CurrentThread.CurrentUICulture.Name == "en-US" ? CheckState.Checked : CheckState.UnChecked;
                CreateAction (supportedCultures, culture);
                supportedCultures.Add (culture);

                index++;
                culture = new MenuItem ();
                culture.CommandView = new CheckBox { CanFocus = false };
            }

            culture.Id = $"_{c.Parent.EnglishName}";
            culture.Title = $"_{c.Parent.EnglishName}";
            culture.HelpText = c.Name;

            ((CheckBox)culture.CommandView).Value = Thread.CurrentThread.CurrentUICulture.Name == culture.HelpText ? CheckState.Checked : CheckState.UnChecked;
            CreateAction (supportedCultures, culture);
            supportedCultures.Add (culture);
        }

        return new Menu (supportedCultures.ToArray ());

        void CreateAction (List<MenuItem> cultures, MenuItem culture) =>
            culture.Action += () =>
                              {
                                  Thread.CurrentThread.CurrentUICulture = new CultureInfo (culture.HelpText);

                                  foreach (MenuItem item in cultures)
                                  {
                                      ((CheckBox)item.CommandView).Value =
                                          Thread.CurrentThread.CurrentUICulture.Name == item.HelpText ? CheckState.Checked : CheckState.UnChecked;
                                  }
                              };
    }

    public override List<Key> GetDemoKeyStrokes (IApplication? app) =>
    [
        Key.F10.WithShift,
        Key.Esc,
        Key.Space.WithCtrl,
        Key.CursorDown,
        Key.Enter,
        Key.F10.WithShift,
        Key.Esc,
        Key.Tab,
        Key.Space.WithCtrl,
        Key.CursorDown,
        Key.CursorDown,
        Key.Enter,
        Key.F10.WithShift,
        Key.Esc,
        Key.Tab,
        Key.Space.WithCtrl,
        Key.CursorDown,
        Key.CursorDown,
        Key.CursorDown,
        Key.Enter,
        Key.F10.WithShift,
        Key.Esc
    ];
}
