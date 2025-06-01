using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MenuBar", "Demonstrates the MenuBar using the demo menu.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public class MenuBarScenario : Scenario
{
    private Label _currentMenuBarItem;
    private Label _currentMenuItem;
    private Label _focusedView;
    private Label _lastAction;
    private Label _lastKey;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
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

        MenuBar menuBar = new MenuBar ();
        menuBar.UseKeysUpDownAsKeysLeftRight = true;
        menuBar.Key = KeyCode.F9;
        menuBar.Title = "TestMenuBar";

        bool FnAction (string s)
        {
            _lastAction.Text = s;

            return true;
        }
        
        // Declare a variable for the function
        Func<string, bool> fnActionVariable = FnAction;

        menuBar.EnableForDesign (ref fnActionVariable);

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
        menuBar.SubViewsLaidOut += (s, e) => { _focusedView.Text = appWindow.MostFocused?.ToString () ?? "None"; };

        var openBtn = new Button { X = Pos.Center (), Y = 4, Text = "_Open Menu", IsDefault = true };
        openBtn.Accepting += (s, e) => { menuBar.OpenMenu (); };
        appWindow.Add (openBtn);

        var hideBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (openBtn), Text = "Toggle Menu._Visible" };
        hideBtn.Accepting += (s, e) => { menuBar.Visible = !menuBar.Visible; };
        appWindow.Add (hideBtn);

        var enableBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (hideBtn), Text = "_Toggle Menu.Enable" };
        enableBtn.Accepting += (s, e) => { menuBar.Enabled = !menuBar.Enabled; };
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
