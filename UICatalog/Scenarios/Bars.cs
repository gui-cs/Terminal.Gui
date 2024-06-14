using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        ObservableCollection<string> eventSource = new ();
        ListView eventLog = new ListView ()
        {
            X = Pos.AnchorEnd (),
            Width = 50,
            Height = Dim.Fill (3),
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper<string> (eventSource)
        };
        Application.Top.Add (eventLog);

        var shortcut1 = new Shortcut
        {
            Title = "_Zigzag",
            Key = Key.G.WithCtrl,
            Text = "Gonna zig zag",
        };
        shortcut1.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };

        var shortcut2 = new Shortcut
        {
            Title = "Za_G",
            Text = "Gonna zag",
            Key = Key.G.WithAlt,
        };

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

        var bar = new Bar
        {
            X = 2,
            Y = 2,
            Orientation = Orientation.Vertical,
            StatusBarStyle = false,
            BorderStyle = LineStyle.Rounded
        };
        bar.Add (shortcut1, shortcut2);

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


        Application.Top.Add (bar);

        // BUGBUG: This should not be needed
        Application.Top.LayoutSubviews ();

       // SetupMenuBar ();
        //SetupContentMenu ();
       // SetupStatusBar ();

        foreach (Bar barView in Application.Top.Subviews.Where (b => b is Bar)!)
        {
            foreach (Shortcut sh in barView.Subviews.Where (s => s is Shortcut)!)
            {
                sh.Accept += (o, args) =>
                                   {
                                       eventSource.Add ($"Accept: {sh!.CommandView.Text}");
                                       eventLog.MoveDown ();
                                   };
            }
        }
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

    private void SetupMenuBar ()
    {
        var menuBar = new Bar
        {
            Id = "menuBar",
            Width = Dim.Fill (),
            Height = 1,//Dim.Auto (DimAutoStyle.Content),
            Orientation = Orientation.Horizontal,
            StatusBarStyle = false,
        };

        var fileMenuBarItem = new Shortcut
        {
            Title = "_File",
            KeyBindingScope = KeyBindingScope.Application,
            Key = Key.F.WithAlt,
        };
        fileMenuBarItem.KeyView.Visible = false;
        
        var editMenuBarItem = new Shortcut
        {
            Title = "_Edit",

            KeyBindingScope = KeyBindingScope.HotKey,
        };

        editMenuBarItem.Accept += (s, e) => { };
        //editMenu.HelpView.Visible = false;
        //editMenu.KeyView.Visible = false;

        menuBar.Add (fileMenuBarItem, editMenuBarItem);
        menuBar.Initialized += Menu_Initialized;
        Application.Top.Add (menuBar);

        var fileMenu = new Bar
        {
            X = 1,
            Y = 1,
            Orientation = Orientation.Vertical,
            StatusBarStyle = false,
           // Modal = true,
            Visible = false,
        };

        var newShortcut = new Shortcut
        {
            Title = "_New...",
            Text = "Create a new file",
            Key = Key.N.WithCtrl
        };
        newShortcut.Border.Thickness = new Thickness (0, 1, 0, 0);

        var openShortcut = new Shortcut
        {
            Title = "_Open...",
            Text = "Show the File Open Dialog",
            Key = Key.O.WithCtrl
        };

        var saveShortcut = new Shortcut
        {
            Title = "_Save...",
            Text = "Save",
            Key = Key.S.WithCtrl,
            Enabled = false
        };

        var exitShortcut = new Shortcut
        {
            Title = "E_xit",
            Text = "Exit",
            Key = Key.X.WithCtrl,
        };
        exitShortcut.Border.Thickness = new Thickness (0, 1, 0, 1);

        fileMenu.Add (newShortcut, openShortcut, saveShortcut, exitShortcut);

        View prevFocus = null;
        fileMenuBarItem.Accept += (s, e) =>
                              {
                                  if (fileMenu.Visible)
                                  {
                                     // fileMenu.RequestStop ();
                                      prevFocus?.SetFocus ();
                                      return;
                                  }

                                  //fileMenu.Visible = !fileMenu.Visible;
                                  var sender = s as Shortcut;
                                  var screen = sender.FrameToScreen ();
                                  fileMenu.X = screen.X;
                                  fileMenu.Y = screen.Y + 1;
                                  fileMenu.Visible = true;
                                  prevFocus = Application.Top.Focused;
                                  fileMenuBarItem.SetFocus ();
                                  //Application.Run (fileMenu);
                                  fileMenu.Visible = false;
                              };

        Application.Top.Closed += (s, e) =>
        {
            fileMenu.Dispose ();
        };

    }

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
            Text = "Quit",
            Title = "Q_uit",
            Key = Application.QuitKey,
            KeyBindingScope = KeyBindingScope.Application,
            CanFocus = false
        };

        statusBar.Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1,
            KeyBindingScope = KeyBindingScope.HotKey,
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
            KeyBindingScope = KeyBindingScope.Application,
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
            // Visible = false
        };
        button1.Accept += Button_Clicked;
        statusBar.Add (button1);

        shortcut.Accept += (s, e) =>
                                                    {
                                                        button1.Visible = !button1.Visible;
                                                        button1.Enabled = button1.Visible;
                                                    };

        statusBar.Add (new Label
        {
            HotKeySpecifier = new Rune ('_'),
            Text = "Fo_cusLabel",
            CanFocus = true
        });

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
