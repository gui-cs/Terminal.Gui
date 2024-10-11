using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using JetBrains.Annotations;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ContextMenus", "Context Menu Sample.")]
[ScenarioCategory ("Menus")]
public class ContextMenus : Scenario
{
    [CanBeNull]
    private ContextMenuv2 _winContextMenu;
    private bool _forceMinimumPosToZero = true;
    private MenuItem _miForceMinimumPosToZero;
    private TextField _tfTopLeft, _tfTopRight, _tfMiddle, _tfBottomLeft, _tfBottomRight;
    private bool _useSubMenusSingleFrame;

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
            Arrangement = ViewArrangement.Fixed
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
            Text = $"Press '{ContextMenu.DefaultKey}' to open the TextField context menu."
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

        Point mousePos = default;

        appWindow.KeyDown += (s, e) =>
                             {
                                 if (e.KeyCode == _winContextMenuKey)
                                 {
                                     ShowWinContextMenu (Application.GetLastMousePosition ());
                                     e.Handled = true;
                                 }
                             };

        appWindow.MouseClick += (s, e) =>
                                {
                                    if (e.MouseEvent.Flags == MouseFlags.Button3Clicked)
                                    {
                                        ShowWinContextMenu (e.MouseEvent.ScreenPosition);
                                        e.Handled = true;
                                    }
                                };

        var originalCulture = Thread.CurrentThread.CurrentUICulture;
        appWindow.Closed += (s, e) =>
                            {
                                Thread.CurrentThread.CurrentUICulture = originalCulture;
                            };

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();
        _winContextMenu?.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
    private Shortcut [] GetSupportedCultures ()
    {
        List<Shortcut> supportedCultures = new ();
        int index = -1;

        foreach (CultureInfo c in _cultureInfos)
        {
            Shortcut culture = new ();

            culture.CommandView = new CheckBox () { CanFocus = false, HighlightStyle = HighlightStyle.None };

            if (index == -1)
            {
                culture.Id = "_English";
                culture.Title = "_English";
                culture.HelpText = "en-US";
                ((CheckBox)culture.CommandView).CheckedState = Thread.CurrentThread.CurrentUICulture.Name == "en-US" ? CheckState.Checked : CheckState.UnChecked;

                CreateAction (supportedCultures, culture);
                supportedCultures.Add (culture);
                index++;
                culture = new ();
                culture.CommandView = new CheckBox () { CanFocus = false, HighlightStyle = HighlightStyle.None};
            }

            culture.Id= $"_{c.Parent.EnglishName}";
            culture.Title = $"_{c.Parent.EnglishName}";
            culture.HelpText = c.Name;
            ((CheckBox)culture.CommandView).CheckedState = Thread.CurrentThread.CurrentUICulture.Name == culture.HelpText ? CheckState.Checked : CheckState.UnChecked;
            CreateAction (supportedCultures, culture);
            supportedCultures.Add (culture);
        }

        return supportedCultures.ToArray ();

        void CreateAction (List<Shortcut> cultures, Shortcut culture)
        {
            culture.Action += () =>
                              {
                                  Thread.CurrentThread.CurrentUICulture = new (culture.HelpText);
 
                                  foreach (Shortcut item in cultures)
                                  {
                                      ((CheckBox)item.CommandView).CheckedState = Thread.CurrentThread.CurrentUICulture.Name == item.HelpText ? CheckState.Checked : CheckState.UnChecked;
                                  }
                              };
        }
    }

    private void CreateWinContextMenu ()
    {
        if (_winContextMenu is { })
        {
            if (Application.Popover == _winContextMenu)
            {
                Application.Popover = null;
            }

            _winContextMenu.Dispose ();
            _winContextMenu = null;
        }

        _winContextMenu = new (GetSupportedCultures ())
        {
            Key = _winContextMenuKey,

            //Position = new (x, y),
            //ForceMinimumPosToZero = _forceMinimumPosToZero,
            //UseSubMenusSingleFrame = _useSubMenusSingleFrame
        };

        //_winContextMenu.KeyBindings.Add (_winContextMenuKey, Command.Context);
    }

    private void ShowWinContextMenu (Point? screenPosition)
    {
        _winContextMenu!.SetPosition(screenPosition);
        Application.Popover = _winContextMenu;
        _winContextMenu.Visible = true;
    }

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
