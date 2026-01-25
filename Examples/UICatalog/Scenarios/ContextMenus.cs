#nullable enable
using System.Globalization;

// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ContextMenus", "Context Menu Sample.")]
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
        using Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            Arrangement = ViewArrangement.Fixed,
            SchemeName = "Runnable"
        };
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

            Label label = new ()
            {
                X = Pos.Center (), Y = 1, Text = $"Press '{_winContextMenuKey}' to open the Window context menu."
            };
            _appWindow.Add (label);

            label = new ()
            {
                X = Pos.Center (),
                Y = Pos.Bottom (label),
                Text = $"Press '{PopoverMenu.DefaultKey}' to open the TextField context menu."
            };
            _appWindow.Add (label);

            _tfTopLeft = new () { Id = "_tfTopLeft", Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfTopLeft);

            _tfTopRight = new () { Id = "_tfTopRight", X = Pos.AnchorEnd (WIDTH), Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfTopRight);

            _tfMiddle = new () { Id = "_tfMiddle", X = Pos.Center (), Y = Pos.Center (), Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfMiddle);

            _tfBottomLeft = new () { Id = "_tfBottomLeft", Y = Pos.AnchorEnd (1), Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfBottomLeft);

            _tfBottomRight = new () { Id = "_tfBottomRight", X = Pos.AnchorEnd (WIDTH), Y = Pos.AnchorEnd (1), Width = WIDTH, Text = TEXT };
            _appWindow.Add (_tfBottomRight);

            CultureInfo originalCulture = Thread.CurrentThread.CurrentUICulture;

            _appWindow.IsRunningChanged += (_, args) =>
                                           {
                                               if (!args.Value)
                                               {
                                                   Thread.CurrentThread.CurrentUICulture = originalCulture;
                                               }
                                           };
        }
    }

    private void HandleCommandNotBound (object? sender, CommandEventArgs e)
    {
        switch (e.Context)
        {
            case CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs }:
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible (mouseArgs.ScreenPosition);
                e.Handled = true;

                break;
            case CommandContext<KeyBinding> { Binding.Key: { } key } when key == _winContextMenuKey:
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible ();
                e.Handled = true;

                break;
        }
    }

    private void CreateWinContextMenu (IApplication? app)
    {
        _winContextMenu = new (
                               [
                                   new MenuItem
                                   {
                                       Title = "C_ultures",
                                       SubMenu = GetSupportedCultureMenu ()
                                   },
                                   new Line (),
                                   new MenuItem
                                   {
                                       Title = "_Configuration...",
                                       HelpText = "Show configuration",
                                       Action = () => MessageBox.Query (
                                                                        app!,
                                                                        50,
                                                                        10,
                                                                        "Configuration",
                                                                        "This would be a configuration dialog",
                                                                        "Ok"
                                                                       )
                                   },
                                   new MenuItem
                                   {
                                       Title = "M_ore options",
                                       SubMenu = new (
                                                      [
                                                          new ()
                                                          {
                                                              Title = "_Setup...",
                                                              HelpText = "Perform setup",
                                                              Action = () => MessageBox
                                                                           .Query (
                                                                                   app!,
                                                                                   50,
                                                                                   10,
                                                                                   "Setup",
                                                                                   "This would be a setup dialog",
                                                                                   "Ok"
                                                                                  ),
                                                              Key = Key.T.WithCtrl
                                                          },
                                                          new ()
                                                          {
                                                              Title = "_Maintenance...",
                                                              HelpText = "Maintenance mode",
                                                              Action = () => MessageBox
                                                                           .Query (
                                                                                   app!,
                                                                                   50,
                                                                                   10,
                                                                                   "Maintenance",
                                                                                   "This would be a maintenance dialog",
                                                                                   "Ok"
                                                                                  )
                                                          }
                                                      ])
                                   },
                                   new Line (),
                                   new MenuItem
                                   {
                                       Title = Strings.cmdQuit,
                                       Action = () => app!.RequestStop ()
                                   }
                               ])
        {
            Key = _winContextMenuKey
        };
        app!.Popover?.Register (_winContextMenu);
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

                ((CheckBox)culture.CommandView).CheckedState =
                    Thread.CurrentThread.CurrentUICulture.Name == "en-US" ? CheckState.Checked : CheckState.UnChecked;
                CreateAction (supportedCultures, culture);
                supportedCultures.Add (culture);

                index++;
                culture = new ();
                culture.CommandView = new CheckBox { CanFocus = false };
            }

            culture.Id = $"_{c.Parent.EnglishName}";
            culture.Title = $"_{c.Parent.EnglishName}";
            culture.HelpText = c.Name;

            ((CheckBox)culture.CommandView).CheckedState =
                Thread.CurrentThread.CurrentUICulture.Name == culture.HelpText ? CheckState.Checked : CheckState.UnChecked;
            CreateAction (supportedCultures, culture);
            supportedCultures.Add (culture);
        }

        return new (supportedCultures.ToArray ());

        void CreateAction (List<MenuItem> cultures, MenuItem culture)
        {
            culture.Action += () =>
                              {
                                  Thread.CurrentThread.CurrentUICulture = new (culture.HelpText);

                                  foreach (MenuItem item in cultures)
                                  {
                                      ((CheckBox)item.CommandView).CheckedState =
                                          Thread.CurrentThread.CurrentUICulture.Name == item.HelpText ? CheckState.Checked : CheckState.UnChecked;
                                  }
                              };
        }
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
