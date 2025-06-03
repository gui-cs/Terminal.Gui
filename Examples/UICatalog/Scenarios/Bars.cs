using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

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
        Application.Top!.Title = GetQuitKeyAndName ();

        ObservableCollection<string> eventSource = new ();
        ListView eventLog = new ListView ()
        {
            Title = "Event Log",
            X = Pos.AnchorEnd (),
            Width = Dim.Auto (),
            Height = Dim.Fill (), // Make room for some wide things
            SchemeName = "Toplevel",
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
            Height = Dim.Percent(33),
        };
        Application.Top.Add (menuBarLikeExamples);

        Label label = new Label ()
        {
            Title = "      Bar:",
            X = 0,
            Y = 0,
        };
        menuBarLikeExamples.Add (label);

        Bar bar = new Bar
        {
            Id = "menuBar-like",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill (),
        };

        ConfigMenuBar (bar);
        menuBarLikeExamples.Add (bar);

        label = new Label ()
        {
            Title = "  MenuBar:",
            X = 0,
            Y = Pos.Bottom (bar) + 1
        };
        menuBarLikeExamples.Add (label);

        //bar = new MenuBarv2
        //{
        //    Id = "menuBar",
        //    X = Pos.Right (label),
        //    Y = Pos.Top (label),
        //};

        //ConfigMenuBar (bar);
        //menuBarLikeExamples.Add (bar);

        FrameView menuLikeExamples = new ()
        {
            Title = "Menu-Like Examples",
            X = 0,
            Y = Pos.Center (),
            Width = Dim.Fill () - Dim.Width (eventLog),
            Height = Dim.Percent (33),
        };
        Application.Top.Add (menuLikeExamples);

        label = new Label ()
        {
            Title = "Bar:",
            X = 0,
            Y = 0,
        };
        menuLikeExamples.Add (label);

        bar = new Bar
        {
            Id = "menu-like",
            X = 0,
            Y = Pos.Bottom(label),
            //Width = Dim.Percent (40),
            Orientation = Orientation.Vertical,
        };
        ConfigureMenu (bar);

        menuLikeExamples.Add (bar);

        label = new Label ()
        {
            Title = "Menu:",
            X = Pos.Right(bar) + 1,
            Y = Pos.Top (label),
        };
        menuLikeExamples.Add (label);

        bar = new Menuv2
        {
            Id = "menu",
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
        };
        ConfigureMenu (bar);
        bar.Arrangement = ViewArrangement.RightResizable;

        menuLikeExamples.Add (bar);

        label = new Label ()
        {
            Title = "PopOver Menu (Right click to show):",
            X = Pos.Right (bar) + 1,
            Y = Pos.Top (label),
        };
        menuLikeExamples.Add (label);

        Menuv2 popOverMenu  = new Menuv2
        {
            Id = "popupMenu",
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
        };
        ConfigureMenu (popOverMenu);

        popOverMenu.Arrangement = ViewArrangement.Overlapped;
        popOverMenu.Visible = false;
        //popOverMenu.Enabled = false;

        var toggleShortcut = new Shortcut
        {
            Title = "Toggle Hide",
            Text = "App",
            BindKeyToApplication = true,
            Key = Key.F4.WithCtrl,
        };
        popOverMenu.Add (toggleShortcut);

        popOverMenu.Accepting += PopOverMenuOnAccept;

        void PopOverMenuOnAccept (object o, CommandEventArgs args)
        {
            if (popOverMenu.Visible)
            {
                popOverMenu.Visible = false;
            }
            else
            {
                popOverMenu.Visible = true;
                popOverMenu.SetFocus ();
            }
        }

        menuLikeExamples.Add (popOverMenu);

        menuLikeExamples.MouseClick += MenuLikeExamplesMouseClick;

        void MenuLikeExamplesMouseClick (object sender, MouseEventArgs e)
        {
            if (e.Flags.HasFlag (MouseFlags.Button3Clicked))
            {
                popOverMenu.X = e.Position.X;
                popOverMenu.Y = e.Position.Y;
                popOverMenu.Visible = true;
                //popOverMenu.Enabled = popOverMenu.Visible;
                popOverMenu.SetFocus ();
            }
            else
            {
                popOverMenu.Visible = false;
                //popOverMenu.Enabled = popOverMenu.Visible;
            }
        }

        FrameView statusBarLikeExamples = new ()
        {
            Title = "StatusBar-Like Examples",
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Width (menuLikeExamples),
            Height = Dim.Percent (33),
        };
        Application.Top.Add (statusBarLikeExamples);

        label = new Label ()
        {
            Title = "      Bar:",
            X = 0,
            Y = 0,
        };
        statusBarLikeExamples.Add (label);
        bar = new Bar
        {
            Id = "statusBar-like",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Orientation = Orientation.Horizontal,
        };
        ConfigStatusBar (bar);
        statusBarLikeExamples.Add (bar);

        label = new Label ()
        {
            Title = "StatusBar:",
            X = 0,
            Y = Pos.Bottom (bar) + 1,
        };
        statusBarLikeExamples.Add (label);
        bar = new StatusBar ()
        {
            Id = "statusBar",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill (),
        };
        ConfigStatusBar (bar);
        statusBarLikeExamples.Add (bar);

        foreach (FrameView frameView in Application.Top.SubViews.Where (f => f is FrameView)!)
        {
            foreach (Bar barView in frameView.SubViews.Where (b => b is Bar)!)
            {
                foreach (Shortcut sh in barView.SubViews.Where (s => s is Shortcut)!)
                {
                    sh.Accepting += (o, args) =>
                                 {
                                     eventSource.Add ($"Accept: {sh!.SuperView.Id} {sh!.CommandView.Text}");
                                     eventLog.MoveDown ();
                                     //args.Handled = true;
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
    //    if (e.Flags != MouseFlags.Button3Clicked)
    //    {
    //        return;
    //    }

    //    var contextMenu = new Bar
    //    {
    //        Id = "contextMenu",
    //        X = e.Position.X,
    //        Y = e.Position.Y,
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

    //    void Application_MouseEvent (object sender, MouseEventArgs e)
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


    private void ConfigMenuBar (Bar bar)
    {
        var fileMenuBarItem = new Shortcut
        {
            Title = "_File",
            HelpText = "File Menu",
            Key = Key.D0.WithAlt,
            HighlightStates = MouseState.In
        };

        var editMenuBarItem = new Shortcut
        {
            Title = "_Edit",
            HelpText = "Edit Menu",
            Key = Key.D1.WithAlt,
            HighlightStates = MouseState.In
        };

        var helpMenuBarItem = new Shortcut
        {
            Title = "_Help",
            HelpText = "Halp Menu",
            Key = Key.D2.WithAlt,
            HighlightStates = MouseState.In
        };

        bar.Add (fileMenuBarItem, editMenuBarItem, helpMenuBarItem);
    }

    private void ConfigureMenu (Bar bar)
    {

        var shortcut1 = new Shortcut
        {
            Title = "Z_igzag",
            Key = Key.I.WithCtrl,
            Text = "Gonna zig zag",
            HighlightStates = MouseState.In
        };

        var shortcut2 = new Shortcut
        {
            Title = "Za_G",
            Text = "Gonna zag",
            Key = Key.G.WithAlt,
            HighlightStates = MouseState.In
        };

        var shortcut3 = new Shortcut
        {
            Title = "_Three",
            Text = "The 3rd item",
            Key = Key.D3.WithAlt,
            HighlightStates = MouseState.In
        };

        var line = new Line ()
        {
            X = -1,
            Width = Dim.Fill ()! + 1
        };

        var shortcut4 = new Shortcut
        {
            Title = "_Four",
            Text = "Below the line",
            Key = Key.D3.WithAlt,
            HighlightStates = MouseState.In
        };

        shortcut4.CommandView = new CheckBox ()
        {
            Title = shortcut4.Title,
            HighlightStates = MouseState.None,
            CanFocus = false
        };
        // This ensures the checkbox state toggles when the hotkey of Title is pressed.
        shortcut4.Accepting += (sender, args) => args.Handled = true;

        bar.Add (shortcut1, shortcut2, shortcut3, line, shortcut4);
    }

    public void ConfigStatusBar (Bar bar)
    {
        var shortcut = new Shortcut
        {
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
        button1.Accepting += Button_Clicked;
        bar.Add (button1);

        shortcut.Accepting += (s, e) =>
                                                    {
                                                        button1.Visible = !button1.Visible;
                                                        button1.Enabled = button1.Visible;
                                                        e.Handled = true;
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
        button2.Accepting += (s, e) => Application.RequestStop ();

        bar.Add (button2);

        return;

        void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }

    }

}
