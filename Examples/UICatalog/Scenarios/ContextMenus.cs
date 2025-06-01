using System.Globalization;
using JetBrains.Annotations;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ContextMenus", "Context Menu Sample.")]
[ScenarioCategory ("Menus")]
public class ContextMenus : Scenario
{
    [CanBeNull]
    private PopoverMenu _winContextMenu;
    private TextField _tfTopLeft, _tfTopRight, _tfMiddle, _tfBottomLeft, _tfBottomRight;
    private readonly List<CultureInfo> _cultureInfos = Application.SupportedCultures;
    private readonly Key _winContextMenuKey = Key.Space.WithCtrl;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            Arrangement = ViewArrangement.Fixed,
            SchemeName = "Toplevel"
        };

        var text = "Context Menu";
        var width = 20;

        CreateWinContextMenu ();

        var label = new Label
        {
            X = Pos.Center (), Y = 1, Text = $"Press '{_winContextMenuKey}' to open the Window context menu."
        };
        appWindow.Add (label);

        label = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (label),
            Text = $"Press '{PopoverMenu.DefaultKey}' to open the TextField context menu."
        };
        appWindow.Add (label);

        _tfTopLeft = new () { Id = "_tfTopLeft", Width = width, Text = text };
        appWindow.Add (_tfTopLeft);

        _tfTopRight = new () { Id = "_tfTopRight", X = Pos.AnchorEnd (width), Width = width, Text = text };
        appWindow.Add (_tfTopRight);

        _tfMiddle = new () { Id = "_tfMiddle", X = Pos.Center (), Y = Pos.Center (), Width = width, Text = text };
        appWindow.Add (_tfMiddle);

        _tfBottomLeft = new () { Id = "_tfBottomLeft", Y = Pos.AnchorEnd (1), Width = width, Text = text };
        appWindow.Add (_tfBottomLeft);

        _tfBottomRight = new () { Id = "_tfBottomRight", X = Pos.AnchorEnd (width), Y = Pos.AnchorEnd (1), Width = width, Text = text };
        appWindow.Add (_tfBottomRight);

        appWindow.KeyDown += OnAppWindowOnKeyDown;
        appWindow.MouseClick += OnAppWindowOnMouseClick;

        CultureInfo originalCulture = Thread.CurrentThread.CurrentUICulture;
        appWindow.Closed += (s, e) => { Thread.CurrentThread.CurrentUICulture = originalCulture; };

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();
        appWindow.KeyDown -= OnAppWindowOnKeyDown;
        appWindow.MouseClick -= OnAppWindowOnMouseClick;
        _winContextMenu?.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();

        return;

        void OnAppWindowOnMouseClick (object s, MouseEventArgs e)
        {
            if (e.Flags == MouseFlags.Button3Clicked)
            {
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible (e.ScreenPosition);
                e.Handled = true;
            }
        }

        void OnAppWindowOnKeyDown (object s, Key e)
        {
            if (e == _winContextMenuKey)
            {
                // ReSharper disable once AccessToDisposedClosure
                _winContextMenu?.MakeVisible ();
                e.Handled = true;
            }
        }
    }

    private void CreateWinContextMenu ()
    {
        if (_winContextMenu is { })
        {
            _winContextMenu.Dispose ();
            _winContextMenu = null;
        }

        _winContextMenu = new (
                               [
                                   new MenuItemv2
                                   {
                                       Title = "C_ultures",
                                       SubMenu = GetSupportedCultureMenu (),
                                   },
                                   new Line (),
                                   new MenuItemv2
                                   {
                                       Title = "_Configuration...",
                                       HelpText = "Show configuration",
                                       Action = () => MessageBox.Query (
                                                                        50,
                                                                        10,
                                                                        "Configuration",
                                                                        "This would be a configuration dialog",
                                                                        "Ok"
                                                                       )
                                   },
                                   new MenuItemv2
                                   {
                                       Title = "M_ore options",
                                       SubMenu = new (
                                                      [
                                                          new MenuItemv2
                                                          {
                                                              Title = "_Setup...",
                                                              HelpText = "Perform setup",
                                                              Action = () => MessageBox
                                                                           .Query (
                                                                                   50,
                                                                                   10,
                                                                                   "Setup",
                                                                                   "This would be a setup dialog",
                                                                                   "Ok"
                                                                                  ),
                                                              Key = Key.T.WithCtrl
                                                          },
                                                          new MenuItemv2
                                                          {
                                                              Title = "_Maintenance...",
                                                              HelpText = "Maintenance mode",
                                                              Action = () => MessageBox
                                                                           .Query (
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
                                   new MenuItemv2
                                   {
                                       Title = "_Quit",
                                       Action = () => Application.RequestStop ()
                                   }
                               ])
        {
            Key = _winContextMenuKey
        };
    }

    private Menuv2 GetSupportedCultureMenu ()
    {
        List<MenuItemv2> supportedCultures = [];
        int index = -1;

        foreach (CultureInfo c in _cultureInfos)
        {
            MenuItemv2 culture = new ();

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

        Menuv2 menu = new (supportedCultures.ToArray ());

        return menu;

        void CreateAction (List<MenuItemv2> cultures, MenuItemv2 culture)
        {
            culture.Action += () =>
                              {
                                  Thread.CurrentThread.CurrentUICulture = new (culture.HelpText);

                                  foreach (MenuItemv2 item in cultures)
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
