using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Bars", "Illustrates Bar views (e.g. StatusBar)")]
[ScenarioCategory ("Controls")]
public class Bars : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        Window app = new ();

        app.Loaded += App_Loaded;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }


    // Setting everything up in Loaded handler because we change the
    // QuitKey and it only sticks if changed after init
    private void App_Loaded (object sender, EventArgs e)
    {
        Application.QuitKey = Key.Z.WithCtrl;
        Application.Top.Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}";

        List<string> eventSource = new ();
        ListView eventLog = new ListView ()
        {
            X = Pos.AnchorEnd (),
            Width = 50,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper (eventSource)
        };
        Application.Top.Add (eventLog);

        var shortcut1 = new Shortcut
        {
            Title = "_Zigzag",
            Key = Key.Z.WithAlt,
            Text = "Gonna zig zag",
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept,
        };
        shortcut1.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };
        Application.Top.Add (shortcut1);
        shortcut1.SetFocus ();

        //var shortcut2 = new Shortcut
        //{
        //    Title = "Za_G",
        //    Text = "Gonna zag",
        //    Key = Key.G.WithAlt,
        //    KeyBindingScope = KeyBindingScope.HotKey,
        //    Command = Command.Accept,
        //    X = Pos.Left (shortcut1),
        //    Y = Pos.Bottom (shortcut1),
        //    //Width = 50,
        //};


        //var shortcut3 = new Shortcut
        //{
        //    Title = "Shortcut3",
        //    Key = Key.D3.WithCtrl,
        //    Text = "Number Three",
        //    KeyBindingScope = KeyBindingScope.Application,
        //    Command = Command.Accept,
        //};

        //shortcut3.Accept += (s, e) =>
        //                    {
        //                        eventSource.Add ($"Accept: {s}");
        //                        eventLog.MoveDown ();
        //                    };

        //var shortcut4 = new Shortcut
        //{
        //    Title = "Shortcut4",
        //    Text = "Number 4",
        //    Key = Key.F4,
        //    KeyBindingScope = KeyBindingScope.Application,
        //    Command = Command.Accept,
        //};

        //var cb = new CheckBox ()
        //{
        //    Title = "Hello",// shortcut4.Text
        //};

        //cb.Toggled += (s, e) =>
        //             {
        //                 eventSource.Add ($"Toggled: {s}");
        //                 eventLog.MoveDown ();
        //             };

        //shortcut4.CommandView = cb;

        //shortcut4.Accept += (s, e) =>
        //                    {
        //                        eventSource.Add ($"Accept: {s}");
        //                        eventLog.MoveDown ();
        //                    };

        //var bar = new Bar
        //{
        //    X = 2,
        //    Y = Pos.Bottom(shortcut1),
        //    Orientation = Orientation.Vertical,
        //    StatusBarStyle = false,
        //    Width = Dim.Percent(40)
        //};
        //bar.Add (shortcut3, shortcut4);

        ////CheckBox hello = new ()
        ////{
        ////    Title = "Hello",
        ////    X = 0,
        ////    Y = 1,
        ////};
        ////Application.Top.Add (hello);
        ////hello.Toggled += (s, e) =>
        ////                 {
        ////                     eventSource.Add ($"Toggled: {s}");
        ////                     eventLog.MoveDown ();
        ////                 };

        //Application.Top.Add (bar);

        // BUGBUG: This should not be needed
        //Application.Top.LayoutSubviews ();

        //SetupMenuBar ();
        //SetupContentMenu ();
       // SetupStatusBar ();
    }

    private void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }

    //private void SetupContentMenu ()
    //{
    //    Application.Top.Add (new Label { Text = "Right Click for Context Menu", X = Pos.Center (), Y = 4 });
    //    Application.Top.MouseClick += ShowContextMenu;
    //}

    //private void ShowContextMenu (object s, MouseEventEventArgs e)
    //{
    //    if (e.MouseEvent.Flags != MouseFlags.Button3Clicked)
    //    {
    //        return;
    //    }

    //    var contextMenu = new Bar
    //    {
    //        Id = "contextMenu",
    //        X = e.MouseEvent.Position.X,
    //        Y = e.MouseEvent.Position.Y,
    //        Width = Dim.Auto (DimAutoStyle.Content),
    //        Height = Dim.Auto (DimAutoStyle.Content),
    //        Orientation = Orientation.Vertical,
    //        StatusBarStyle = false,
    //        BorderStyle = LineStyle.Rounded,
    //        Modal = true,
    //    };

    //    var newMenu = new Shortcut
    //    {
    //        Title = "_New...",
    //        Text = "Create a new file",
    //        Key = Key.N.WithCtrl,
    //        CanFocus = true
    //    };

    //    newMenu.Accept += (s, e) =>
    //                      {
    //                          contextMenu.RequestStop ();

    //                          Application.AddTimeout (
    //                                                  new TimeSpan (0),
    //                                                  () =>
    //                                                  {
    //                                                      MessageBox.Query ("File", "New");

    //                                                      return false;
    //                                                  });
    //                      };

    //    var open = new Shortcut
    //    {
    //        Title = "_Open...",
    //        Text = "Show the File Open Dialog",
    //        Key = Key.O.WithCtrl,
    //        CanFocus = true
    //    };

    //    open.Accept += (s, e) =>
    //                   {
    //                       contextMenu.RequestStop ();

    //                       Application.AddTimeout (
    //                                               new TimeSpan (0),
    //                                               () =>
    //                                               {
    //                                                   MessageBox.Query ("File", "Open");

    //                                                   return false;
    //                                               });
    //                   };

    //    var save = new Shortcut
    //    {
    //        Title = "_Save...",
    //        Text = "Save",
    //        Key = Key.S.WithCtrl,
    //        CanFocus = true
    //    };

    //    save.Accept += (s, e) =>
    //                   {
    //                       contextMenu.RequestStop ();

    //                       Application.AddTimeout (
    //                                               new TimeSpan (0),
    //                                               () =>
    //                                               {
    //                                                   MessageBox.Query ("File", "Save");

    //                                                   return false;
    //                                               });
    //                   };

    //    var saveAs = new Shortcut
    //    {
    //        Title = "Save _As...",
    //        Text = "Save As",
    //        Key = Key.A.WithCtrl,
    //        CanFocus = true
    //    };

    //    saveAs.Accept += (s, e) =>
    //                     {
    //                         contextMenu.RequestStop ();

    //                         Application.AddTimeout (
    //                                                 new TimeSpan (0),
    //                                                 () =>
    //                                                 {
    //                                                     MessageBox.Query ("File", "Save As");

    //                                                     return false;
    //                                                 });
    //                     };

    //    contextMenu.Add (newMenu, open, save, saveAs);

    //    contextMenu.KeyBindings.Add (Key.Esc, Command.QuitToplevel);

    //    contextMenu.Initialized += Menu_Initialized;

    //    void Application_MouseEvent (object sender, MouseEvent e)
    //    {
    //        // If user clicks outside of the menuWindow, close it
    //        if (!contextMenu.Frame.Contains (e.Position.X, e.Position.Y))
    //        {
    //            if (e.Flags is (MouseFlags.Button1Clicked or MouseFlags.Button3Clicked))
    //            {
    //                contextMenu.RequestStop ();
    //            }
    //        }
    //    }

    //    Application.MouseEvent += Application_MouseEvent;

    //    Application.Run (contextMenu);
    //    contextMenu.Dispose ();

    //    Application.MouseEvent -= Application_MouseEvent;
    //}

    private void Menu_Initialized (object sender, EventArgs e)
    {
        // BUGBUG: this should not be needed    

        ((View)(sender)).LayoutSubviews ();
    }

    //private void SetupMenuBar ()
    //{
    //    var menuBar = new Bar
    //    {
    //        Id = "menuBar",

    //        X = 0,
    //        Y = 0,
    //        Width = Dim.Fill (),
    //        Height = Dim.Auto (DimAutoStyle.Content),
    //        StatusBarStyle = true
    //    };

    //    var fileMenu = new Shortcut
    //    {
    //        Title = "_File",
    //        Key = Key.F.WithAlt,
    //        KeyBindingScope = KeyBindingScope.HotKey,
    //        Command = Command.Accept,
    //    };
    //    fileMenu.HelpView.Visible = false;
    //    fileMenu.KeyView.Visible = false;

    //    fileMenu.Accept += (s, e) =>
    //                       {
    //                           fileMenu.SetFocus ();

    //                           if (s is View view)
    //                           {
    //                               var menu = new Bar
    //                               {
    //                                   X = view.Frame.X + 1,
    //                                   Y = view.Frame.Y + 1,
    //                                   ColorScheme = view.ColorScheme,
    //                                   Orientation = Orientation.Vertical,
    //                                   StatusBarStyle = false,
    //                                   BorderStyle = LineStyle.Dotted,
    //                                   Width = Dim.Auto (DimAutoStyle.Content),
    //                                   Height = Dim.Auto (DimAutoStyle.Content),
    //                               };

    //                               menu.KeyBindings.Add (Key.Esc, Command.QuitToplevel);

    //                               var newMenu = new Shortcut
    //                               {
    //                                   Title = "_New...",
    //                                   Text = "Create a new file",
    //                                   Key = Key.N.WithCtrl
    //                               };

    //                               var open = new Shortcut
    //                               {
    //                                   Title = "_Open...",
    //                                   Text = "Show the File Open Dialog",
    //                                   Key = Key.O.WithCtrl
    //                               };

    //                               var save = new Shortcut
    //                               {
    //                                   Title = "_Save...",
    //                                   Text = "Save",
    //                                   Key = Key.S.WithCtrl
    //                               };

    //                               menu.Add (newMenu, open, save);

    //                               // BUGBUG: this is all bad
    //                               menu.Initialized += Menu_Initialized;
    //                               open.Initialized += Menu_Initialized;
    //                               save.Initialized += Menu_Initialized;
    //                               newMenu.Initialized += Menu_Initialized;

    //                               Application.Run (menu);
    //                               menu.Dispose ();
    //                               Application.Refresh ();
    //                           }
    //                       };

    //    var editMenu = new Shortcut
    //    {
    //        Title = "_Edit",

    //        //Key = Key.E.WithAlt,
    //        KeyBindingScope = KeyBindingScope.HotKey,
    //        Command = Command.Accept
    //    };

    //    editMenu.Accept += (s, e) => { };
    //    editMenu.HelpView.Visible = false;
    //    editMenu.KeyView.Visible = false;

    //    menuBar.Add (fileMenu, editMenu);

    //    menuBar.Initialized += Menu_Initialized;

    //    Application.Top.Add (menuBar);
    //}

    private void SetupStatusBar ()
    {
        var statusBar = new Bar
        {
            Id = "statusBar",
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
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

        statusBar.Add (shortcut);

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
            Y = Pos.Top (statusBar) - 1,
            Text = "Help"
        };
        Application.Top.Add (labelHelp);

        shortcut.Accept += (s, e) =>
                           {
                               labelHelp.Text = labelHelp.Text + "!";
                               e.Handled = true;
                           };

        statusBar.Add (shortcut);

        shortcut = new Shortcut
        {
            Title = "_Show/Hide",
            Key = Key.F10,
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.ToggleExpandCollapse,
            CommandView = new CheckBox
            {
                Text = "_Show/Hide"
            },
            CanFocus = false
        };

        statusBar.Add (shortcut);

        var button1 = new Button
        {
            Text = "I'll Hide",
            Visible = false
        };
        button1.Accept += Button_Clicked;
        statusBar.Add (button1);

        ((CheckBox)shortcut.CommandView).Toggled += (s, e) =>
                                                    {
                                                        button1.Visible = !button1.Visible;
                                                        button1.Enabled = button1.Visible;
                                                    };

        statusBar.Add (new Label { HotKeySpecifier = new Rune ('_'), Text = "Fo_cusLabel", CanFocus = true });

        var button2 = new Button
        {
            Text = "Or me!",
        };
        button2.Accept += (s, e) => Application.RequestStop ();

        statusBar.Add (button2);

        statusBar.Initialized += Menu_Initialized;

        Application.Top.Add (statusBar);


    }

}
