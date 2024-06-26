using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MenuBar", "Demonstrates the MenuBar using the same menu used in unit tests.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public class MenuBarScenario : Scenario
{
    private Label _currentMenuBarItem;
    private Label _currentMenuItem;
    private Label _focusedView;
    private Label _lastAction;
    private Label _lastKey;

    /// <summary>
    ///     This method creates at test menu bar. It is called by the MenuBar unit tests, so it's possible to do both unit
    ///     testing and user-experience testing with the same setup.
    /// </summary>
    /// <param name="actionFn"></param>
    /// <returns></returns>
    public static MenuBar CreateTestMenu (Func<string, bool> actionFn)
    {
        // TODO: add a disabled menu item to this
        var mb = new MenuBar
        {
            Menus =
            [
                new MenuBarItem (
                                 "_File",
                                 new MenuItem []
                                 {
                                     new (
                                          "_New",
                                          "",
                                          () => actionFn ("New"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask | KeyCode.N
                                         ),
                                     new (
                                          "_Open",
                                          "",
                                          () => actionFn ("Open"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask | KeyCode.O
                                         ),
                                     new (
                                          "_Save",
                                          "",
                                          () => actionFn ("Save"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask | KeyCode.S
                                         ),
                                     null,

                                     // Don't use Application.Quit so we can disambiguate between quitting and closing the toplevel
                                     new (
                                          "_Quit",
                                          "",
                                          () => actionFn ("Quit"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask | KeyCode.Q
                                         )
                                 }
                                ),
                new MenuBarItem (
                                 "_Edit",
                                 new MenuItem []
                                 {
                                     new (
                                          "_Copy",
                                          "",
                                          () => actionFn ("Copy"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask | KeyCode.C
                                         ),
                                     new (
                                          "C_ut",
                                          "",
                                          () => actionFn ("Cut"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask | KeyCode.X
                                         ),
                                     new (
                                          "_Paste",
                                          "",
                                          () => actionFn ("Paste"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask | KeyCode.V
                                         ),
                                     new MenuBarItem (
                                                      "_Find and Replace",
                                                      new MenuItem []
                                                      {
                                                          new (
                                                               "F_ind",
                                                               "",
                                                               () => actionFn ("Find"),
                                                               null,
                                                               null,
                                                               KeyCode.CtrlMask | KeyCode.F
                                                              ),
                                                          new (
                                                               "_Replace",
                                                               "",
                                                               () => actionFn ("Replace"),
                                                               null,
                                                               null,
                                                               KeyCode.CtrlMask | KeyCode.H
                                                              ),
                                                          new MenuBarItem (
                                                                           "_3rd Level",
                                                                           new MenuItem []
                                                                           {
                                                                               new (
                                                                                    "_1st",
                                                                                    "",
                                                                                    () => actionFn (
                                                                                                    "1"
                                                                                                   ),
                                                                                    null,
                                                                                    null,
                                                                                    KeyCode.F1
                                                                                   ),
                                                                               new (
                                                                                    "_2nd",
                                                                                    "",
                                                                                    () => actionFn (
                                                                                                    "2"
                                                                                                   ),
                                                                                    null,
                                                                                    null,
                                                                                    KeyCode.F2
                                                                                   )
                                                                           }
                                                                          ),
                                                          new MenuBarItem (
                                                                           "_4th Level",
                                                                           new MenuItem []
                                                                           {
                                                                               new (
                                                                                    "_5th",
                                                                                    "",
                                                                                    () => actionFn (
                                                                                                    "5"
                                                                                                   ),
                                                                                    null,
                                                                                    null,
                                                                                    KeyCode.CtrlMask
                                                                                    | KeyCode.D5
                                                                                   ),
                                                                               new (
                                                                                    "_6th",
                                                                                    "",
                                                                                    () => actionFn (
                                                                                                    "6"
                                                                                                   ),
                                                                                    null,
                                                                                    null,
                                                                                    KeyCode.CtrlMask
                                                                                    | KeyCode.D6
                                                                                   )
                                                                           }
                                                                          )
                                                      }
                                                     ),
                                     new (
                                          "_Select All",
                                          "",
                                          () => actionFn ("Select All"),
                                          null,
                                          null,
                                          KeyCode.CtrlMask
                                          | KeyCode.ShiftMask
                                          | KeyCode.S
                                         )
                                 }
                                ),
                new MenuBarItem ("_About", "Top-Level", () => actionFn ("About"))
            ]
        };
        mb.UseKeysUpDownAsKeysLeftRight = true;
        mb.Key = KeyCode.F9;
        mb.Title = "TestMenuBar";

        return mb;
    }

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
            BorderStyle = LineStyle.None
        };

        MenuItem mbiCurrent = null;
        MenuItem miCurrent = null;

        var label = new Label { X = 0, Y = 10, Text = "Last Key: " };
        appWindow.Add (label);

        _lastKey = new Label { X = Pos.Right (label), Y = Pos.Top (label), Text = "" };

        appWindow.Add (_lastKey);
        label = new Label { X = 0, Y = Pos.Bottom (label), Text = "Current MenuBarItem: " };
        appWindow.Add (label);

        _currentMenuBarItem = new Label { X = Pos.Right (label), Y = Pos.Top (label), Text = "" };
        appWindow.Add (_currentMenuBarItem);

        label = new Label { X = 0, Y = Pos.Bottom (label), Text = "Current MenuItem: " };
        appWindow.Add (label);

        _currentMenuItem = new Label { X = Pos.Right (label), Y = Pos.Top (label), Text = "" };
        appWindow.Add (_currentMenuItem);

        label = new Label { X = 0, Y = Pos.Bottom (label), Text = "Last Action: " };
        appWindow.Add (label);

        _lastAction = new Label { X = Pos.Right (label), Y = Pos.Top (label), Text = "" };
        appWindow.Add (_lastAction);

        label = new Label { X = 0, Y = Pos.Bottom (label), Text = "Focused View: " };
        appWindow.Add (label);

        _focusedView = new Label { X = Pos.Right (label), Y = Pos.Top (label), Text = "" };
        appWindow.Add (_focusedView);

        MenuBar menuBar = CreateTestMenu (
                                          s =>
                                          {
                                              _lastAction.Text = s;

                                              return true;
                                          }
                                         );

        menuBar.MenuOpening += (s, e) =>
                               {
                                   mbiCurrent = e.CurrentMenu;
                                   SetCurrentMenuBarItem (mbiCurrent);
                                   SetCurrentMenuItem (miCurrent);
                                   _lastAction.Text = string.Empty;
                               };

        menuBar.MenuOpened += (s, e) =>
                              {
                                  miCurrent = e.MenuItem;
                                  SetCurrentMenuBarItem (mbiCurrent);
                                  SetCurrentMenuItem (miCurrent);
                              };

        menuBar.MenuClosing += (s, e) =>
                               {
                                   mbiCurrent = null;
                                   miCurrent = null;
                                   SetCurrentMenuBarItem (mbiCurrent);
                                   SetCurrentMenuItem (miCurrent);
                               };

        Application.KeyDown += (s, e) =>
                               {
                                   _lastAction.Text = string.Empty;
                                   _lastKey.Text = e.ToString ();
                               };

        // There's no focus change event, so this is a bit of a hack.
        menuBar.LayoutComplete += (s, e) => { _focusedView.Text = appWindow.MostFocused?.ToString () ?? "None"; };

        var openBtn = new Button { X = Pos.Center (), Y = 4, Text = "_Open Menu", IsDefault = true };
        openBtn.Accept += (s, e) => { menuBar.OpenMenu (); };
        appWindow.Add (openBtn);

        var hideBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (openBtn), Text = "Toggle Menu._Visible" };
        hideBtn.Accept += (s, e) => { menuBar.Visible = !menuBar.Visible; };
        appWindow.Add (hideBtn);

        var enableBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (hideBtn), Text = "_Toggle Menu.Enable" };
        enableBtn.Accept += (s, e) => { menuBar.Enabled = !menuBar.Enabled; };
        appWindow.Add (enableBtn);

        appWindow.Add (menuBar);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void SetCurrentMenuBarItem (MenuItem mbi) { _currentMenuBarItem.Text = mbi != null ? mbi.Title : "Closed"; }
    private void SetCurrentMenuItem (MenuItem mi) { _currentMenuItem.Text = mi != null ? mi.Title : "None"; }
}
