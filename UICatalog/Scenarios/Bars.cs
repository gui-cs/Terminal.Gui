using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Bars", "Illustrates Bar views (e.g. StatusBar)")]
[ScenarioCategory ("Controls")]
public class bars : Scenario
{
    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
        Application.Top.Loaded += Top_Initialized;
    }

    private void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }

    private void SetupContentMenu ()
    {
        Application.Top.Add (new Label { Text = "Right Click for Context Menu", X = Pos.Center (), Y = 4 });

        Application.Top.MouseClick += (s, e) =>
                                      {
                                          if (e.MouseEvent.Flags != MouseFlags.Button3Clicked)
                                          {
                                              return;
                                          }

                                          var menuWindow = new Window
                                          {
                                              X = e.MouseEvent.X,
                                              Y = e.MouseEvent.Y,
                                              BorderStyle = LineStyle.None
                                          };

                                          var contextMenu = new Bar
                                          {
                                              //Title = "Menu Demo",
                                              Orientation = Orientation.Vertical,
                                              StatusBarStyle = false,
                                              AutoSize = true,
                                              BorderStyle = LineStyle.Single
                                          };

                                          var newMenu = new Shortcut
                                          {
                                              Title = "_New...",
                                              Text = "Create a new file",
                                              Key = Key.N.WithCtrl,
                                              AutoSize = true,
                                              Width = Dim.Fill (),
                                              CanFocus = true
                                          };

                                          newMenu.Accept += (s, e) =>
                                                            {
                                                                menuWindow.RequestStop ();

                                                                Application.AddTimeout (
                                                                                        new TimeSpan (0),
                                                                                        () =>
                                                                                        {
                                                                                            MessageBox.Query ("File", "New");

                                                                                            return false;
                                                                                        });
                                                            };

                                          var open = new Shortcut
                                          {
                                              Title = "_Open...",
                                              Text = "Show the File Open Dialog",
                                              Key = Key.O.WithCtrl,
                                              AutoSize = true,
                                              Width = Dim.Fill (),
                                              CanFocus = true
                                          };

                                          open.Accept += (s, e) =>
                                                         {
                                                             menuWindow.RequestStop ();

                                                             Application.AddTimeout (
                                                                                     new TimeSpan (0),
                                                                                     () =>
                                                                                     {
                                                                                         MessageBox.Query ("File", "Open");

                                                                                         return false;
                                                                                     });
                                                         };

                                          var save = new Shortcut
                                          {
                                              Title = "_Save...",
                                              Text = "Save",
                                              Key = Key.S.WithCtrl,
                                              AutoSize = true,
                                              Width = Dim.Fill (),
                                              CanFocus = true
                                          };

                                          save.Accept += (s, e) =>
                                                         {
                                                             menuWindow.RequestStop ();

                                                             Application.AddTimeout (
                                                                                     new TimeSpan (0),
                                                                                     () =>
                                                                                     {
                                                                                         MessageBox.Query ("File", "Save");

                                                                                         return false;
                                                                                     });
                                                         };

                                          var saveAs = new Shortcut
                                          {
                                              Title = "Save _As...",
                                              Text = "Save As",
                                              Key = Key.S.WithCtrl,
                                              AutoSize = true,
                                              Width = Dim.Fill (),
                                              CanFocus = true
                                          };

                                          saveAs.Accept += (s, e) =>
                                                           {
                                                               menuWindow.RequestStop ();

                                                               Application.AddTimeout (
                                                                                       new TimeSpan (0),
                                                                                       () =>
                                                                                       {
                                                                                           MessageBox.Query ("File", "Save As");

                                                                                           return false;
                                                                                       });
                                                           };

                                          contextMenu.Add (newMenu, open, save, saveAs);

                                          menuWindow.KeyBindings.Add (Key.Esc, Command.QuitToplevel);

                                          menuWindow.LayoutComplete += (s, e) =>
                                                                       {
                                                                           menuWindow.Width = contextMenu.Frame.Width;
                                                                           menuWindow.Height = contextMenu.Frame.Height;
                                                                       };
                                          menuWindow.Add (contextMenu);

                                          void Application_MouseEvent (object sender, MouseEventEventArgs e)
                                          {
                                              // If user clicks outside of the menuWindow, close it
                                              if (!menuWindow.Frame.Contains (e.MouseEvent.X, e.MouseEvent.Y))
                                              {
                                                  if (e.MouseEvent.Flags is (MouseFlags.Button1Clicked or MouseFlags.Button3Clicked))
                                                  {
                                                      menuWindow.RequestStop ();
                                                  }
                                              }
                                          }

                                          Application.MouseEvent += Application_MouseEvent;

                                          Application.Run (menuWindow);

                                          Application.MouseEvent -= Application_MouseEvent;
                                      };
    }

    private void SetupMenuBar ()
    {
        var bar = new Bar
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = 1,
            StatusBarStyle = true
        };

        var fileMenu = new Shortcut
        {
            Title = "_File",
            Key = Key.F.WithAlt,
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept
        };
        fileMenu.HelpView.Visible = false;
        fileMenu.KeyView.Visible = false;

        fileMenu.Accept += (s, e) =>
                           {
                               fileMenu.SetFocus ();

                               if (s is View view)
                               {
                                   var menuWindow = new Window
                                   {
                                       X = view.Frame.X + 1,
                                       Y = view.Frame.Y + 1,
                                       Width = 40,
                                       Height = 10,
                                       ColorScheme = view.ColorScheme
                                   };

                                   menuWindow.KeyBindings.Add (Key.Esc, Command.QuitToplevel);

                                   var menu = new Bar
                                   {
                                       Orientation = Orientation.Vertical,
                                       StatusBarStyle = false,
                                       X = 0,
                                       Y = 0,
                                       Width = Dim.Fill (),
                                       Height = Dim.Fill ()
                                   };

                                   var newMenu = new Shortcut
                                   {
                                       Title = "_New...",
                                       Text = "Create a new file",
                                       Key = Key.N.WithCtrl
                                   };

                                   var open = new Shortcut
                                   {
                                       Title = "_Open...",
                                       Text = "Show the File Open Dialog",
                                       Key = Key.O.WithCtrl
                                   };

                                   var save = new Shortcut
                                   {
                                       Title = "_Save...",
                                       Text = "Save",
                                       Key = Key.S.WithCtrl
                                   };

                                   menu.Add (newMenu, open, save);
                                   menuWindow.Add (menu);

                                   Application.Run (menuWindow);
                                   Application.Refresh ();
                               }
                           };

        var editMenu = new Shortcut
        {
            Title = "_Edit",

            //Key = Key.E.WithAlt,
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept
        };

        editMenu.Accept += (s, e) => { };
        editMenu.HelpView.Visible = false;
        editMenu.KeyView.Visible = false;

        bar.Add (fileMenu, editMenu);
        Application.Top.Add (bar);
    }

    private void SetupStatusBar ()
    {
        var bar = new Bar
        {
            X = 0,
            Y = Pos.AnchorEnd (1),
            Width = Dim.Fill (),
            Height = 1
        };

        var shortcut = new Shortcut
        {
            Text = "Quit Application",
            Title = "Q_uit",
            Key = Application.QuitKey,
            KeyBindingScope = KeyBindingScope.Application,
            Command = Command.QuitToplevel,
            CanFocus = false
        };

        bar.Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1,
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept,
            CanFocus = false
        };

        var labelHelp = new Label
        {
            X = Pos.Center (),
            Y = Pos.Top (bar) - 1,
            Text = "Help"
        };
        Application.Top.Add (labelHelp);

        shortcut.Accept += (s, e) =>
                           {
                               labelHelp.Text = labelHelp.Text + "!";
                               e.Handled = true;
                           };

        bar.Add (shortcut);

        shortcut = new Shortcut
        {
            Title = "_Show/Hide",
            Key = Key.F10,
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.ToggleChecked,
            CommandView = new CheckBox
            {
                Text = "_Show/Hide"
            },
            CanFocus = false
        };

        bar.Add (shortcut);

        var button1 = new Button
        {
            Text = "I'll Hide",
            AutoSize = true,
            Visible = false
        };
        button1.Clicked += Button_Clicked;
        bar.Add (button1);

        ((CheckBox)shortcut.CommandView).Toggled += (s, e) =>
                                                    {
                                                        button1.Visible = !button1.Visible;
                                                        button1.Enabled = button1.Visible;
                                                    };

        bar.Add (new Label { HotKeySpecifier = new Rune ('_'), Text = "Fo_cusLabel", CanFocus = true });

        var button2 = new Button
        {
            Text = "Or me!",
            AutoSize = true
        };
        button2.Clicked += (s, e) => Application.RequestStop ();

        bar.Add (button2);

        Application.Top.Add (bar);
    }

    // Setting everything up in Initialized handler because we change the
    // QuitKey and it only sticks if changed after init
    private void Top_Initialized (object sender, EventArgs e)
    {
        Application.QuitKey = Key.Z.WithCtrl;

        var shortcut1 = new Shortcut
        {
            Title = "_Zigzag",
            Key = Key.Z.WithAlt,
            Text = "Gonna zig zag",
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept,
            X = Pos.Center (),
            Y = Pos.Center (),
        };

        var shortcut2 = new Shortcut
        {
            Title = "Za_G",
            Text = "Gonna zag",
            Key = Key.G.WithAlt,
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept,
            X = Pos.Left (shortcut1),
            Y = Pos.Bottom (shortcut1),
        };

        Application.Top.Add (shortcut1, shortcut2);
        shortcut1.SetFocus ();

        SetupMenuBar ();
        SetupContentMenu ();
        SetupStatusBar ();
    }
}
