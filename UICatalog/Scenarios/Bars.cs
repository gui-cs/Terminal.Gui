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
        Toplevel app = new ();

        app.Loaded += App_Loaded;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }


    // Setting everything up in Loaded handler because we change the
    // QuitKey and it only sticks if changed after init
    private void App_Loaded (object sender, EventArgs e)
    {
        Application.Top.Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}";

        ObservableCollection<string> eventSource = new ();
        ListView eventLog = new ListView ()
        {
            Title = "Event Log",
            X = Pos.AnchorEnd (),
            Width = Dim.Auto(),
            Height = Dim.Fill (), // Make room for some wide things
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper<string> (eventSource)
        };
        eventLog.Border.Thickness = new (0, 1, 0, 0);
        Application.Top.Add (eventLog);

        FrameView menuBarLikeExamples = new ()
        {
            Title = "MenuBar-Like Examples",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (eventLog),
            Height = 10,
        };
        Application.Top.Add (menuBarLikeExamples);

        Label label = new Label ()
        {
            Title = "      Bar:",
            X = 0,
            Y = Pos.AnchorEnd () - 6
        };
        menuBarLikeExamples.Add (label);

        Bar bar = new Bar
        {
            Id = "menuBar-like",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 1,//Dim.Auto (DimAutoStyle.Content),
            Orientation = Orientation.Horizontal,
        };

        ConfigMenuBar (bar);
        menuBarLikeExamples.Add (bar);

        label = new Label ()
        {
            Title = "  MenuBar:",
            X = 0,
            Y = Pos.Bottom(bar)
        };
        menuBarLikeExamples.Add (label);

        //bar = new MenuBarv2
        //{
        //    Id = "menuBar",
        //    Width = Dim.Fill (),
        //    Height = 1,//Dim.Auto (DimAutoStyle.Content),
        //    Orientation = Orientation.Horizontal,
        //};

        //ConfigMenuBar (bar);
        //menuBarLikeExamples.Add (bar);

        FrameView menuLikeExamples = new ()
        {
            Title = "Menu-Like Examples",
            X = 0,
            Y = Pos.Bottom (menuBarLikeExamples),
            Width = Dim.Fill () - Dim.Width (eventLog),
            Height = 10,
        };
        Application.Top.Add (menuLikeExamples);

        var shortcut1 = new Shortcut
        {
            Title = "_Zigzag",
            Key = Key.G.WithCtrl,
            Text = "Gonna zig zag",
        };

        var shortcut2 = new Shortcut
        {
            Title = "Za_G",
            Text = "Gonna zag",
            Key = Key.G.WithAlt,
        };

        var vBar = new Bar
        {
            X = 2,
            Y = 2,
            Orientation = Orientation.Vertical,
            BorderStyle = LineStyle.Rounded
        };
        vBar.Add (shortcut1, shortcut2);

        menuLikeExamples.Add (vBar);

        // BUGBUG: This should not be needed
        menuLikeExamples.LayoutSubviews ();

        // SetupMenuBar ();
        //SetupContentMenu ();

        FrameView statusBarLikeExamples = new ()
        {
            Title = "StatusBar-Like Examples",
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Width (menuLikeExamples),
            Height = 10,
        };
        Application.Top.Add (statusBarLikeExamples);

        label = new Label ()
        {
            Title = "      Bar:",
            X = 0,
            Y = Pos.AnchorEnd () - 6
        };
        statusBarLikeExamples.Add (label);
        //bar = new Bar
        //{
        //    Id = "statusBar-like",
        //    X = Pos.Right (label),
        //    Y = Pos.Top (label),
        //    Width = Dim.Fill (),
        //    Orientation = Orientation.Horizontal,
        //};
        //ConfigStatusBar (bar);
        //statusBarLikeExamples.Add (bar);

        label = new Label ()
        {
            Title = "StatusBar:",
            X = 0,
            Y = Pos.AnchorEnd () - 3
        };
        statusBarLikeExamples.Add (label);
        bar = new StatusBar ()
        {
            Id = "statusBar",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Orientation = Orientation.Horizontal,
        };
        ConfigStatusBar (bar);
        statusBarLikeExamples.Add (bar);

        foreach (FrameView frameView in Application.Top.Subviews.Where (f => f is FrameView)!)
        {
            foreach (Bar barView in frameView.Subviews.Where (b => b is Bar)!)
            {
                foreach (Shortcut sh in barView.Subviews.Where (s => s is Shortcut)!)
                {
                    sh.Accept += (o, args) =>
                                 {
                                     eventSource.Add ($"Accept: {sh!.SuperView.Id} {sh!.CommandView.Text}");
                                     eventLog.MoveDown ();
                                 };
                }
            }
        }
    }


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

    private void ConfigMenuBar (Bar bar)
    {
        var fileMenuBarItem = new Shortcut
        {
            Title = "_File",
        };
        fileMenuBarItem.KeyView.Visible = false;

        var editMenuBarItem = new Shortcut
        {
            Title = "_Edit",
        };

        bar.Add (fileMenuBarItem, editMenuBarItem);
    }

    private void ConfigStatusBar (Bar bar)
    {
        var shortcut = new Shortcut
        {
            Height = Dim.Auto (DimAutoStyle.Content, 3),
            Text = "Quit",
            Title = "Q_uit",
            Key = Key.Z.WithCtrl,
        };

        bar.Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1,
        };

        bar.Add (shortcut);

        shortcut = new Shortcut
        {
            Title = "_Show/Hide",
            Key = Key.F10,
            CommandView = new CheckBox
            {
                CanFocus = false,
                Text = "_Show/Hide"
            },
        };

        bar.Add (shortcut);

        var button1 = new Button
        {
            Text = "I'll Hide",
            // Visible = false
        };
        button1.Accept += Button_Clicked;
        bar.Add (button1);

        shortcut.Accept += (s, e) =>
                                                    {
                                                        button1.Visible = !button1.Visible;
                                                        button1.Enabled = button1.Visible;
                                                        e.Cancel = false;
                                                    };

        bar.Add (new Label
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

        bar.Add (button2);

        return;

        void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }

    }

}
