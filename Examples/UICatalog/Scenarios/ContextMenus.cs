#nullable enable
using System.Globalization;
using JetBrains.Annotations;
// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ContextMenus", "Context Menu Sample.")]
[ScenarioCategory ("Menus")]
public class ContextMenus : Scenario
{
    private PopoverMenu? _winContextMenu;
    private TextField? _tfTopLeft, _tfTopRight, _tfMiddle, _tfBottomLeft, _tfBottomRight;
    private readonly List<CultureInfo>? _cultureInfos = Application.SupportedCultures;
    private readonly Key _winContextMenuKey = Key.Space.WithCtrl;

    private Window? _appWindow;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        _appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            Arrangement = ViewArrangement.Fixed,
            SchemeName = "Runnable"
        };

        _appWindow.Initialized += AppWindowOnInitialized;

        // Run - Start the application.
        Application.Run (_appWindow);
        _appWindow.Dispose ();
        _appWindow.KeyDown -= OnAppWindowOnKeyDown;
        _appWindow.MouseClick -= OnAppWindowOnMouseClick;
        _winContextMenu?.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();

        return;

        void AppWindowOnInitialized (object? sender, EventArgs e)
        {

            var text = "Context Menu";
            var width = 20;

            CreateWinContextMenu (Application.Instance);

            var label = new Label
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

            _tfTopLeft = new () { Id = "_tfTopLeft", Width = width, Text = text };
            _appWindow.Add (_tfTopLeft);

            _tfTopRight = new () { Id = "_tfTopRight", X = Pos.AnchorEnd (width), Width = width, Text = text };
            _appWindow.Add (_tfTopRight);

            _tfMiddle = new () { Id = "_tfMiddle", X = Pos.Center (), Y = Pos.Center (), Width = width, Text = text };
            _appWindow.Add (_tfMiddle);

            _tfBottomLeft = new () { Id = "_tfBottomLeft", Y = Pos.AnchorEnd (1), Width = width, Text = text };
            _appWindow.Add (_tfBottomLeft);

            _tfBottomRight = new () { Id = "_tfBottomRight", X = Pos.AnchorEnd (width), Y = Pos.AnchorEnd (1), Width = width, Text = text };
            _appWindow.Add (_tfBottomRight);

            _appWindow.KeyDown += OnAppWindowOnKeyDown;
            _appWindow.MouseClick += OnAppWindowOnMouseClick;

            CultureInfo originalCulture = Thread.CurrentThread.CurrentUICulture;
            _appWindow.IsRunningChanged += (s, e) => {
                                               if (!e.Value)
                                               {
                                                   Thread.CurrentThread.CurrentUICulture = originalCulture;
                                               } };
        }

        void OnAppWindowOnMouseClick (object? s, MouseEventArgs e)
        {
            if (e.Flags == MouseFlags.Button3Clicked)
            {
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible (e.ScreenPosition);
                e.Handled = true;
            }
        }

        void OnAppWindowOnKeyDown (object? s, Key e)
        {
            if (e == _winContextMenuKey)
            {
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible ();
                e.Handled = true;
            }
        }
    }

    private void CreateWinContextMenu (IApplication? app)
    {
        _winContextMenu = new (
                               [
                                   new MenuItem
                                   {
                                       Title = "C_ultures",
                                       SubMenu = GetSupportedCultureMenu (),
                                   },
                                   new Line (),
                                   new MenuItem
                                   {
                                       Title = "_Configuration...",
                                       HelpText = "Show configuration",
                                       Action = () => MessageBox.Query (app,
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
                                                          new MenuItem
                                                          {
                                                              Title = "_Setup...",
                                                              HelpText = "Perform setup",
                                                              Action = () => MessageBox
                                                                           .Query (app,
                                                                                   50,
                                                                                   10,
                                                                                   "Setup",
                                                                                   "This would be a setup dialog",
                                                                                   "Ok"
                                                                                  ),
                                                              Key = Key.T.WithCtrl
                                                          },
                                                          new MenuItem
                                                          {
                                                              Title = "_Maintenance...",
                                                              HelpText = "Maintenance mode",
                                                              Action = () => MessageBox
                                                                           .Query (app,
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
                                       Title = "_Quit",
                                       Action = () => Application.RequestStop ()
                                   }
                               ])
        {
            Key = _winContextMenuKey
        };
        Application.Popover?.Register (_winContextMenu);
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
                // Create English because GetSupportedCutures doesn't include it
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

        Menu menu = new (supportedCultures.ToArray ());

        return menu;

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

    public override List<Key> GetDemoKeyStrokes ()
    {
        List<Key> keys = new ();

        keys.Add (Key.F10.WithShift);
        keys.Add (Key.Esc);

        keys.Add (Key.Space.WithCtrl);
        keys.Add (Key.CursorDown);
        keys.Add (Key.Enter);

        keys.Add (Key.F10.WithShift);
        keys.Add (Key.Esc);

        keys.Add (Key.Tab);

        keys.Add (Key.Space.WithCtrl);
        keys.Add (Key.CursorDown);
        keys.Add (Key.CursorDown);
        keys.Add (Key.Enter);

        keys.Add (Key.F10.WithShift);
        keys.Add (Key.Esc);

        keys.Add (Key.Tab);

        keys.Add (Key.Space.WithCtrl);
        keys.Add (Key.CursorDown);
        keys.Add (Key.CursorDown);
        keys.Add (Key.CursorDown);
        keys.Add (Key.Enter);

        keys.Add (Key.F10.WithShift);
        keys.Add (Key.Esc);

        return keys;
    }
}
