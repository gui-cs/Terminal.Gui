using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Bars", "Illustrates Bar views (e.g. StatusBar)")]
[ScenarioCategory ("Controls")]
public class Bars : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable mainWindow = new ();

        mainWindow.IsModalChanged += App_Loaded;

        app.Run (mainWindow);
    }

    // Setting everything up in Loaded handler because we change the
    // QuitKey it only sticks if changed after init
    private void App_Loaded (object sender, EventArgs e)
    {
        if (sender is not Runnable mainWindow)
        {
            return;
        }

        ObservableCollection<string> eventSource = new ();

        ListView eventLog = new ()
        {
            Title = "Event Log",
            X = Pos.AnchorEnd (),
            Width = Dim.Auto (),
            Height = Dim.Fill (), // Make room for some wide things
            SchemeName = "Runnable",
            Source = new ListWrapper<string> (eventSource)
        };
        eventLog.Border!.Thickness = new (0, 1, 0, 0);
        mainWindow.Add (eventLog);

        FrameView menuBarLikeExamples = new ()
        {
            Title = "MenuBar-Like Examples",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (eventLog),
            Height = Dim.Percent (33)
        };
        mainWindow.Add (menuBarLikeExamples);

        Label label = new ()
        {
            Title = "      Bar:",
            X = 0,
            Y = 0
        };
        menuBarLikeExamples.Add (label);

        Bar bar = new ()
        {
            Id = "menuBar-like",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill ()
        };

        ConfigMenuBar (bar);
        menuBarLikeExamples.Add (bar);

        label = new ()
        {
            Title = "  MenuBar:",
            X = 0,
            Y = Pos.Bottom (bar) + 1
        };
        menuBarLikeExamples.Add (label);

        //bar = new MenuBar
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
            Height = Dim.Percent (33)
        };
        mainWindow.Add (menuLikeExamples);

        label = new ()
        {
            Title = "Bar:",
            X = 0,
            Y = 0
        };
        menuLikeExamples.Add (label);

        bar = new ()
        {
            Id = "menu-like",
            X = 0,
            Y = Pos.Bottom (label),

            //Width = Dim.Percent (40),
            Orientation = Orientation.Vertical
        };
        ConfigureMenu (bar);

        menuLikeExamples.Add (bar);

        label = new ()
        {
            Title = "Menu:",
            X = Pos.Right (bar) + 1,
            Y = Pos.Top (label)
        };
        menuLikeExamples.Add (label);

        bar = new ()
        {
            Id = "menu",
            X = Pos.Left (label),
            Y = Pos.Bottom (label)
        };
        ConfigureMenu (bar);
        bar.Arrangement = ViewArrangement.RightResizable;

        menuLikeExamples.Add (bar);

        label = new ()
        {
            Title = "PopOver Menu (Right click to show):",
            X = Pos.Right (bar) + 1,
            Y = Pos.Top (label)
        };
        menuLikeExamples.Add (label);

        Menu popOverMenu = new ()
        {
            Id = "popupMenu",
            X = Pos.Left (label),
            Y = Pos.Bottom (label)
        };
        ConfigureMenu (popOverMenu);

        popOverMenu.Arrangement = ViewArrangement.Overlapped;
        popOverMenu.Visible = false;

        //popOverMenu.Enabled = false;

        Shortcut toggleShortcut = new ()
        {
            Title = "Toggle Hide",
            Text = "App",
            BindKeyToApplication = true,
            Key = Key.F4.WithCtrl
        };
        popOverMenu.Add (toggleShortcut);

        popOverMenu.Accepting += PopOverMenuOnAccept;

        menuLikeExamples.Add (popOverMenu);

        menuLikeExamples.MouseEvent += MenuLikeExamplesMouseEvent;

        FrameView statusBarLikeExamples = new ()
        {
            Title = "StatusBar-Like Examples",
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Width (menuLikeExamples),
            Height = Dim.Percent (33)
        };
        mainWindow.Add (statusBarLikeExamples);

        label = new ()
        {
            Title = "      Bar:",
            X = 0,
            Y = 0
        };
        statusBarLikeExamples.Add (label);

        bar = new()
        {
            Id = "statusBar-like",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Orientation = Orientation.Horizontal
        };
        ConfigStatusBar (bar);
        statusBarLikeExamples.Add (bar);

        label = new ()
        {
            Title = "StatusBar:",
            X = 0,
            Y = Pos.Bottom (bar) + 1
        };
        statusBarLikeExamples.Add (label);

        bar = new ()
        {
            Id = "statusBar",
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = Dim.Fill ()
        };
        ConfigStatusBar (bar);
        statusBarLikeExamples.Add (bar);

        foreach (FrameView frameView in mainWindow.SubViews.OfType<FrameView> ())
        {
            foreach (Bar barView in frameView.SubViews.OfType<Bar> ())
            {
                foreach (Shortcut sh in barView.SubViews.OfType<Shortcut> ())
                {
                    sh.Accepting += (_, _) =>
                                    {
                                        eventSource.Add ($"Accept: {sh!.SuperView!.Id} {sh!.CommandView.Text}");
                                        eventLog.MoveDown ();

                                        //args.Handled = true;
                                    };
                }
            }
        }

        return;


        void MenuLikeExamplesMouseEvent (object _, Mouse mouse)
        {
            if (mouse.Flags.HasFlag (MouseFlags.RightButtonClicked))
            {
                popOverMenu.X = mouse.Position!.Value.X;
                popOverMenu.Y = mouse.Position!.Value.Y;
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
    }

    //private void SetupContentMenu ()
    //{
    //    Application.TopRunnable.Add (new Label { Text = "Right Click for Context Menu", X = Pos.Center (), Y = 4 });
    //    Application.TopRunnable.MouseClick += ShowContextMenu;
    //}

    //private void ShowContextMenu (object s, MouseEventEventArgs e)
    //{
    //    if (e.Flags != MouseFlags.RightButtonClicked)
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
    //                                                      MessageBox.Query (App, "File", "New");

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
    //                                                   MessageBox.Query (App, "File", "Open");

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
    //                                                   MessageBox.Query (App, "File", "Save");

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
    //                                                     MessageBox.Query (App, "File", "Save As");

    //                                                     return false;
    //                                                 });
    //                     };

    //    contextMenu.Add (newMenu, open, save, saveAs);

    //    contextMenu.KeyBindings.Add (Key.Esc, Command.Quit);

    //    contextMenu.Initialized += Menu_Initialized;

    //    void Application_MouseEvent (object sender, MouseEventArgs e)
    //    {
    //        // If user clicks outside of the menuWindow, close it
    //        if (!contextMenu.Frame.Contains (e.Position.X, e.Position.Y))
    //        {
    //            if (e.Flags is (MouseFlags.LeftButtonClicked or MouseFlags.RightButtonClicked))
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
        Shortcut fileMenuBarItem = new ()
        {
            Title = Strings.menuFile,
            HelpText = "File Menu",
            Key = Key.D0.WithAlt,
            MouseHighlightStates = MouseState.In
        };

        Shortcut editMenuBarItem = new ()
        {
            Title = "_Edit",
            HelpText = "Edit Menu",
            Key = Key.D1.WithAlt,
            MouseHighlightStates = MouseState.In
        };

        Shortcut helpMenuBarItem = new ()
        {
            Title = Strings.menuHelp,
            HelpText = "Halp Menu",
            Key = Key.D2.WithAlt,
            MouseHighlightStates = MouseState.In
        };

        bar.Add (fileMenuBarItem, editMenuBarItem, helpMenuBarItem);
    }

    private void ConfigureMenu (Bar bar)
    {
        Shortcut shortcut1 = new ()
        {
            Title = "Z_igzag",
            Key = Key.I.WithCtrl,
            Text = "Gonna zig zag",
            MouseHighlightStates = MouseState.In
        };

        Shortcut shortcut2 = new ()
        {
            Title = "Za_G",
            Text = "Gonna zag",
            Key = Key.G.WithAlt,
            MouseHighlightStates = MouseState.In
        };

        Shortcut shortcut3 = new ()
        {
            Title = "_Three",
            Text = "The 3rd item",
            Key = Key.D3.WithAlt,
            MouseHighlightStates = MouseState.In
        };

        Line line = new ()
        {
            X = -1,
            Width = Dim.Fill ()! + 1
        };

        Shortcut shortcut4 = new ()
        {
            Title = "_Four",
            Text = "Below the line",
            Key = Key.D3.WithAlt,
            MouseHighlightStates = MouseState.In
        };

        shortcut4.CommandView = new CheckBox
        {
            Title = shortcut4.Title,
            MouseHighlightStates = MouseState.None,
            CanFocus = false
        };

        // This ensures the checkbox state toggles when the hotkey of Title is pressed.
        shortcut4.Accepting += (_, args) => args.Handled = true;

        bar.Add (shortcut1, shortcut2, shortcut3, line, shortcut4);
    }

    public void ConfigStatusBar (Bar bar)
    {
        Shortcut shortcut = new ()
        {
            Text = "Quit",
            Title = "Q_uit",
            Key = Key.Z.WithCtrl
        };

        bar.Add (shortcut);

        shortcut = new ()
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1
        };

        bar.Add (shortcut);

        shortcut = new ()
        {
            Title = "_Show/Hide",
            Key = Key.F10,
            CommandView = new CheckBox
            {
                CanFocus = false,
                Text = "_Show/Hide"
            }
        };

        bar.Add (shortcut);

        Button button1 = new ()
        {
            Text = "I'll Hide"

            // Visible = false
        };
        button1.Accepting += ButtonClicked;
        bar.Add (button1);

        shortcut.Accepting += (_, e) =>
                              {
                                  button1.Visible = !button1.Visible;
                                  button1.Enabled = button1.Visible;
                                  e.Handled = true;
                              };

        bar.Add (
                 new Label
                 {
                     HotKeySpecifier = new ('_'),
                     Text = "Fo_cusLabel",
                     CanFocus = true
                 });

        Button middleButton = new ()
        {
            Text = "Or me!"
        };
        middleButton.Accepting += (s, _) => (s as View)?.App!.RequestStop ();

        bar.Add (middleButton);

        return;

        static void ButtonClicked (object sender, EventArgs e)
        {
            MessageBox.Query ((sender as View)?.App!, "Hi", $"You clicked {sender}");
        }
    }
}
