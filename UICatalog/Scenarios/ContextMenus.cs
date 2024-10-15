using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ContextMenus", "Context Menu Sample.")]
[ScenarioCategory ("Menus")]
public class ContextMenus : Scenario
{
    private readonly List<CultureInfo> _cultureInfos = Application.SupportedCultures;
    private ContextMenu _contextMenu = new ();
    private bool _forceMinimumPosToZero = true;
    private MenuItem _miForceMinimumPosToZero;
    private MenuItem _miUseSubMenusSingleFrame;
    private TextField _tfTopLeft, _tfTopRight, _tfMiddle, _tfBottomLeft, _tfBottomRight;
    private bool _useSubMenusSingleFrame;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            Arrangement = ViewArrangement.Fixed
        };

        var text = "Context Menu";
        var width = 20;
        var winContextMenuKey = (KeyCode)Key.Space.WithCtrl;

        var label = new Label
        {
            X = Pos.Center (), Y = 1, Text = $"Press '{winContextMenuKey}' to open the Window context menu."
        };
        appWindow.Add (label);

        label = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (label),
            Text = $"Press '{ContextMenu.DefaultKey}' to open the TextField context menu."
        };
        appWindow.Add (label);

        _tfTopLeft = new() { Width = width, Text = text };
        appWindow.Add (_tfTopLeft);

        _tfTopRight = new() { X = Pos.AnchorEnd (width), Width = width, Text = text };
        appWindow.Add (_tfTopRight);

        _tfMiddle = new() { X = Pos.Center (), Y = Pos.Center (), Width = width, Text = text };
        appWindow.Add (_tfMiddle);

        _tfBottomLeft = new() { Y = Pos.AnchorEnd (1), Width = width, Text = text };
        appWindow.Add (_tfBottomLeft);

        _tfBottomRight = new() { X = Pos.AnchorEnd (width), Y = Pos.AnchorEnd (1), Width = width, Text = text };
        appWindow.Add (_tfBottomRight);

        Point mousePos = default;

        appWindow.KeyDown += (s, e) =>
                             {
                                 if (e.KeyCode == winContextMenuKey)
                                 {
                                     ShowContextMenu (mousePos.X, mousePos.Y);
                                     e.Handled = true;
                                 }
                             };

        appWindow.MouseClick += (s, e) =>
                                {
                                    if (e.Flags == _contextMenu.MouseFlags)
                                    {
                                        ShowContextMenu (e.Position.X, e.Position.Y);
                                        e.Handled = true;
                                    }
                                };

        Application.MouseEvent += ApplicationMouseEvent;

        void ApplicationMouseEvent (object sender, MouseEventArgs a) { mousePos = a.Position; }

        appWindow.WantMousePositionReports = true;

        appWindow.Closed += (s, e) =>
                            {
                                Thread.CurrentThread.CurrentUICulture = new ("en-US");
                                Application.MouseEvent -= ApplicationMouseEvent;
                            };

        var top = new Toplevel ();
        top.Add (appWindow);

        // Run - Start the application.
        Application.Run (top);
        top.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private MenuItem [] GetSupportedCultures ()
    {
        List<MenuItem> supportedCultures = new ();
        int index = -1;

        foreach (CultureInfo c in _cultureInfos)
        {
            var culture = new MenuItem { CheckType = MenuItemCheckStyle.Checked };

            if (index == -1)
            {
                culture.Title = "_English";
                culture.Help = "en-US";
                culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == "en-US";
                CreateAction (supportedCultures, culture);
                supportedCultures.Add (culture);
                index++;
                culture = new() { CheckType = MenuItemCheckStyle.Checked };
            }

            culture.Title = $"_{c.Parent.EnglishName}";
            culture.Help = c.Name;
            culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == c.Name;
            CreateAction (supportedCultures, culture);
            supportedCultures.Add (culture);
        }

        return supportedCultures.ToArray ();

        void CreateAction (List<MenuItem> supportedCultures, MenuItem culture)
        {
            culture.Action += () =>
                              {
                                  Thread.CurrentThread.CurrentUICulture = new (culture.Help);
                                  culture.Checked = true;

                                  foreach (MenuItem item in supportedCultures)
                                  {
                                      item.Checked = item.Help == Thread.CurrentThread.CurrentUICulture.Name;
                                  }
                              };
        }
    }

    private void ShowContextMenu (int x, int y)
    {
        _contextMenu = new()
        {
            Position = new (x, y),
            ForceMinimumPosToZero = _forceMinimumPosToZero,
            UseSubMenusSingleFrame = _useSubMenusSingleFrame
        };

        MenuBarItem menuItems = new (
                                     new []
                                     {
                                         new MenuBarItem (
                                                          "_Languages",
                                                          GetSupportedCultures ()
                                                         ),
                                         new (
                                              "_Configuration",
                                              "Show configuration",
                                              () => MessageBox.Query (
                                                                      50,
                                                                      5,
                                                                      "Info",
                                                                      "This would open settings dialog",
                                                                      "Ok"
                                                                     )
                                             ),
                                         new MenuBarItem (
                                                          "M_ore options",
                                                          new MenuItem []
                                                          {
                                                              new (
                                                                   "_Setup",
                                                                   "Change settings",
                                                                   () => MessageBox
                                                                       .Query (
                                                                               50,
                                                                               5,
                                                                               "Info",
                                                                               "This would open setup dialog",
                                                                               "Ok"
                                                                              ),
                                                                   shortcutKey: KeyCode.T
                                                                                | KeyCode
                                                                                    .CtrlMask
                                                                  ),
                                                              new (
                                                                   "_Maintenance",
                                                                   "Maintenance mode",
                                                                   () => MessageBox
                                                                       .Query (
                                                                               50,
                                                                               5,
                                                                               "Info",
                                                                               "This would open maintenance dialog",
                                                                               "Ok"
                                                                              )
                                                                  )
                                                          }
                                                         ),
                                         _miForceMinimumPosToZero =
                                             new (
                                                  "Fo_rceMinimumPosToZero",
                                                  "",
                                                  () =>
                                                  {
                                                      _miForceMinimumPosToZero
                                                              .Checked =
                                                          _forceMinimumPosToZero =
                                                              !_forceMinimumPosToZero;

                                                      _tfTopLeft.ContextMenu
                                                                .ForceMinimumPosToZero =
                                                          _forceMinimumPosToZero;

                                                      _tfTopRight.ContextMenu
                                                                 .ForceMinimumPosToZero =
                                                          _forceMinimumPosToZero;

                                                      _tfMiddle.ContextMenu
                                                               .ForceMinimumPosToZero =
                                                          _forceMinimumPosToZero;

                                                      _tfBottomLeft.ContextMenu
                                                                   .ForceMinimumPosToZero =
                                                          _forceMinimumPosToZero;

                                                      _tfBottomRight
                                                              .ContextMenu
                                                              .ForceMinimumPosToZero =
                                                          _forceMinimumPosToZero;
                                                  }
                                                 )
                                             {
                                                 CheckType =
                                                     MenuItemCheckStyle
                                                         .Checked,
                                                 Checked =
                                                     _forceMinimumPosToZero
                                             },
                                         _miUseSubMenusSingleFrame =
                                             new (
                                                  "Use_SubMenusSingleFrame",
                                                  "",
                                                  () => _contextMenu
                                                                .UseSubMenusSingleFrame =
                                                            (bool)
                                                            (_miUseSubMenusSingleFrame
                                                                     .Checked =
                                                                 _useSubMenusSingleFrame =
                                                                     !_useSubMenusSingleFrame)
                                                 )
                                             {
                                                 CheckType = MenuItemCheckStyle
                                                     .Checked,
                                                 Checked =
                                                     _useSubMenusSingleFrame
                                             },
                                         null,
                                         new (
                                              "_Quit",
                                              "",
                                              () => Application.RequestStop ()
                                             )
                                     }
                                    );
        _tfTopLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
        _tfTopRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
        _tfMiddle.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
        _tfBottomLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
        _tfBottomRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;

        _contextMenu.Show (menuItems);
    }
}
