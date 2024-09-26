using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using JetBrains.Annotations;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ContextMenus v2", "Context Menu v2 Sample.")]
[ScenarioCategory ("Menus")]
public class ContextMenusv2 : Scenario
{
    [CanBeNull]
    private ContextMenuv2 _contextMenu;
    private bool _forceMinimumPosToZero = true;
    private MenuItem _miForceMinimumPosToZero;
    private TextField _tfTopLeft, _tfTopRight, _tfMiddle, _tfBottomLeft, _tfBottomRight;
    private bool _useSubMenusSingleFrame;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        _contextMenu = new ContextMenuv2 ()
        {
        };

        ConfigureMenu (_contextMenu);
        _contextMenu.Key = Key.Space.WithCtrl;

        var text = "Context Menu";
        var width = 20;

        var label = new Label
        {
            X = Pos.Center (), Y = 1, Text = $"Press '{_contextMenu.Key}' to open the Window context menu."
        };
        appWindow.Add (label);

        label = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (label),
            Text = $"Press '{ContextMenu.DefaultKey}' to open the TextField context menu."
        };
        appWindow.Add (label);

        _tfTopLeft = new() { Id = "_tfTopLeft", Width = width, Text = text };
        appWindow.Add (_tfTopLeft);

        _tfTopRight = new() { Id = "_tfTopRight", X = Pos.AnchorEnd (width), Width = width, Text = text };
        appWindow.Add (_tfTopRight);

        _tfMiddle = new() { Id = "_tfMiddle", X = Pos.Center (), Y = Pos.Center (), Width = width, Text = text };
        appWindow.Add (_tfMiddle);

        _tfBottomLeft = new() { Id = "_tfBottomLeft", Y = Pos.AnchorEnd (1), Width = width, Text = text };
        appWindow.Add (_tfBottomLeft);

        _tfBottomRight = new() { Id = "_tfBottomRight", X = Pos.AnchorEnd (width), Y = Pos.AnchorEnd (1), Width = width, Text = text };
        appWindow.Add (_tfBottomRight);

        Point mousePos = default;

        appWindow.KeyDown += (s, e) =>
                             {
                                 if (e.KeyCode == _contextMenu.Key)
                                 {
                                     Application.Popover = _contextMenu;
                                     _contextMenu.Visible = true;
                                     e.Handled = true;
                                 }
                             };

        appWindow.MouseClick += (s, e) =>
                                {
                                    if (e.MouseEvent.Flags == MouseFlags.Button3Clicked)
                                    {
                                        Application.Popover = _contextMenu;
                                        _contextMenu.X = e.MouseEvent.ScreenPosition.X;
                                        _contextMenu.Y = e.MouseEvent.ScreenPosition.Y;
                                        _contextMenu.Visible = true;
                                        e.Handled = true;
                                    }
                                };

        appWindow.Closed += (s, e) =>
                            {
                                Thread.CurrentThread.CurrentUICulture = new ("en-US");
                            };


        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();
        _contextMenu.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void ConfigureMenu (Bar bar)
    {

        var shortcut1 = new Shortcut
        {
            Title = "Z_igzag",
            Key = Key.I.WithCtrl,
            Text = "Gonna zig zag",
            HighlightStyle = HighlightStyle.Hover
        };

        var shortcut2 = new Shortcut
        {
            Title = "Za_G",
            Text = "Gonna zag",
            Key = Key.G.WithAlt,
            HighlightStyle = HighlightStyle.Hover
        };

        var shortcut3 = new Shortcut
        {
            Title = "_Three",
            Text = "The 3rd item",
            Key = Key.D3.WithAlt,
            HighlightStyle = HighlightStyle.Hover
        };

        var line = new Line ()
        {
            BorderStyle = LineStyle.Dotted,
            Orientation = Orientation.Horizontal,
            CanFocus = false,
        };
        // HACK: Bug in Line
        line.Orientation = Orientation.Vertical;
        line.Orientation = Orientation.Horizontal;

        var shortcut4 = new Shortcut
        {
            Title = "_Four",
            Text = "Below the line",
            Key = Key.D3.WithAlt,
            HighlightStyle = HighlightStyle.Hover
        };
        bar.Add (shortcut1, shortcut2, shortcut3, line, shortcut4);
    }


    //private MenuItem [] GetSupportedCultures ()
    //{
    //    List<MenuItem> supportedCultures = new ();
    //    int index = -1;

    //    foreach (CultureInfo c in _cultureInfos)
    //    {
    //        var culture = new MenuItem { CheckType = MenuItemCheckStyle.Checked };

    //        if (index == -1)
    //        {
    //            culture.Title = "_English";
    //            culture.Help = "en-US";
    //            culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == "en-US";
    //            CreateAction (supportedCultures, culture);
    //            supportedCultures.Add (culture);
    //            index++;
    //            culture = new() { CheckType = MenuItemCheckStyle.Checked };
    //        }

    //        culture.Title = $"_{c.Parent.EnglishName}";
    //        culture.Help = c.Name;
    //        culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == c.Name;
    //        CreateAction (supportedCultures, culture);
    //        supportedCultures.Add (culture);
    //    }

    //    return supportedCultures.ToArray ();

    //    void CreateAction (List<MenuItem> supportedCultures, MenuItem culture)
    //    {
    //        culture.Action += () =>
    //                          {
    //                              Thread.CurrentThread.CurrentUICulture = new (culture.Help);
    //                              culture.Checked = true;

    //                              foreach (MenuItem item in supportedCultures)
    //                              {
    //                                  item.Checked = item.Help == Thread.CurrentThread.CurrentUICulture.Name;
    //                              }
    //                          };
    //    }
    //}

    //private void ShowContextMenu (int x, int y)
    //{
    //    _contextMenu = new()
    //    {
    //        Position = new (x, y),
    //        ForceMinimumPosToZero = _forceMinimumPosToZero,
    //        UseSubMenusSingleFrame = _useSubMenusSingleFrame
    //    };

    //    MenuBarItem menuItems = new (
    //                                 new []
    //                                 {
    //                                     new MenuBarItem (
    //                                                      "_Languages",
    //                                                      GetSupportedCultures ()
    //                                                     ),
    //                                     new (
    //                                          "_Configuration",
    //                                          "Show configuration",
    //                                          () => MessageBox.Query (
    //                                                                  50,
    //                                                                  5,
    //                                                                  "Info",
    //                                                                  "This would open settings dialog",
    //                                                                  "Ok"
    //                                                                 )
    //                                         ),
    //                                     new MenuBarItem (
    //                                                      "M_ore options",
    //                                                      new MenuItem []
    //                                                      {
    //                                                          new (
    //                                                               "_Setup",
    //                                                               "Change settings",
    //                                                               () => MessageBox
    //                                                                   .Query (
    //                                                                           50,
    //                                                                           5,
    //                                                                           "Info",
    //                                                                           "This would open setup dialog",
    //                                                                           "Ok"
    //                                                                          ),
    //                                                               shortcutKey: KeyCode.T
    //                                                                            | KeyCode
    //                                                                                .CtrlMask
    //                                                              ),
    //                                                          new (
    //                                                               "_Maintenance",
    //                                                               "Maintenance mode",
    //                                                               () => MessageBox
    //                                                                   .Query (
    //                                                                           50,
    //                                                                           5,
    //                                                                           "Info",
    //                                                                           "This would open maintenance dialog",
    //                                                                           "Ok"
    //                                                                          )
    //                                                              )
    //                                                      }
    //                                                     ),
    //                                     _miForceMinimumPosToZero =
    //                                         new (
    //                                              "Fo_rceMinimumPosToZero",
    //                                              "",
    //                                              () =>
    //                                              {
    //                                                  _miForceMinimumPosToZero
    //                                                          .Checked =
    //                                                      _forceMinimumPosToZero =
    //                                                          !_forceMinimumPosToZero;

    //                                                  _tfTopLeft.ContextMenu
    //                                                            .ForceMinimumPosToZero =
    //                                                      _forceMinimumPosToZero;

    //                                                  _tfTopRight.ContextMenu
    //                                                             .ForceMinimumPosToZero =
    //                                                      _forceMinimumPosToZero;

    //                                                  _tfMiddle.ContextMenu
    //                                                           .ForceMinimumPosToZero =
    //                                                      _forceMinimumPosToZero;

    //                                                  _tfBottomLeft.ContextMenu
    //                                                               .ForceMinimumPosToZero =
    //                                                      _forceMinimumPosToZero;

    //                                                  _tfBottomRight
    //                                                          .ContextMenu
    //                                                          .ForceMinimumPosToZero =
    //                                                      _forceMinimumPosToZero;
    //                                              }
    //                                             )
    //                                         {
    //                                             CheckType =
    //                                                 MenuItemCheckStyle
    //                                                     .Checked,
    //                                             Checked =
    //                                                 _forceMinimumPosToZero
    //                                         },
    //                                     _miUseSubMenusSingleFrame =
    //                                         new (
    //                                              "Use_SubMenusSingleFrame",
    //                                              "",
    //                                              () => _contextMenu
    //                                                            .UseSubMenusSingleFrame =
    //                                                        (bool)
    //                                                        (_miUseSubMenusSingleFrame
    //                                                                 .Checked =
    //                                                             _useSubMenusSingleFrame =
    //                                                                 !_useSubMenusSingleFrame)
    //                                             )
    //                                         {
    //                                             CheckType = MenuItemCheckStyle
    //                                                 .Checked,
    //                                             Checked =
    //                                                 _useSubMenusSingleFrame
    //                                         },
    //                                     null,
    //                                     new (
    //                                          "_Quit",
    //                                          "",
    //                                          () => Application.RequestStop ()
    //                                         )
    //                                 }
    //                                );
    //    _tfTopLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
    //    _tfTopRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
    //    _tfMiddle.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
    //    _tfBottomLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
    //    _tfBottomRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;

    //    _contextMenu.Show (menuItems);
    //}
}
