using System.Collections.Generic;
using Timer = System.Timers.Timer;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Arrangement", "Arrangement Tester")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Arrangement")]
public class Arrangement : Scenario
{
    private int _hotkeyCount;

    public override void Main ()
    {
        Application.Init ();

        Window mainWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            TabStop = TabBehavior.TabGroup,
            ShadowStyle = ShadowStyle.None
        };

        var adornmentsEditor = new AdornmentsEditor
        {
            X = 0,
            Y = 0,
            AutoSelectViewToEdit = true,
            TabStop = TabBehavior.NoStop,
            ShowViewIdentifier = true
        };

        mainWindow.Add (adornmentsEditor);

        adornmentsEditor!.ExpanderButton!.Orientation = Orientation.Horizontal;

        //  adornmentsEditor.ExpanderButton!.Collapsed = true;

        var arrangementEditor = new ArrangementEditor
        {
            Y = Pos.Bottom (adornmentsEditor) + 1,
            AutoSelectViewToEdit = true,
            TabStop = TabBehavior.NoStop
        };
        mainWindow.Add (arrangementEditor);

        FrameView testFrame = new ()
        {
            Title = "_1 Test Frame",
            Text = "This is the text of the Test Frame.\nLine 2.\nLine 3.",
            X = Pos.Right (adornmentsEditor),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        testFrame.TextAlignment = Alignment.Center;
        testFrame.VerticalTextAlignment = Alignment.Center;

        mainWindow.Add (testFrame);

        FrameView tiledFrame = new ()
        {
            Title = "Frame for Tiled Demo",
            Width = Dim.Fill (),
            Height = Dim.Auto ()
        };
        View tiledView1 = CreateTiledView (0, 2, 1);
        View tiledView2 = CreateTiledView (1, Pos.Right (tiledView1) - 1, Pos.Top (tiledView1));
        tiledView2.Height = Dim.Height (tiledView1);
        View tiledView3 = CreateTiledView (2, Pos.Right (tiledView2) - 1, Pos.Top (tiledView2));
        tiledView3.Height = Dim.Height (tiledView1);
        View tiledView4 = CreateTiledView (3, Pos.Left (tiledView1), Pos.Bottom (tiledView1) - 1);
        tiledView4.Width = Dim.Func (_ => tiledView3.Frame.Width + tiledView2.Frame.Width + tiledView1.Frame.Width - 2);
        tiledView1.SuperViewRendersLineCanvas = true;
        tiledView2.SuperViewRendersLineCanvas = true;
        tiledView3.SuperViewRendersLineCanvas = true;
        tiledView4.SuperViewRendersLineCanvas = true;
        tiledFrame.Add (tiledView4, tiledView3, tiledView2, tiledView1);

        testFrame.Add (tiledFrame);

        View movableSizeableWithProgress = CreateOverlappedView (2, 2, 10);
        movableSizeableWithProgress.Title = "Movable _& Sizable";
        View tiledSubView = CreateTiledView (4, 0, 2);
        tiledSubView.Arrangement = ViewArrangement.Fixed;
        movableSizeableWithProgress.Add (tiledSubView);
        tiledSubView = CreateTiledView (5, Pos.Right (tiledSubView), Pos.Top (tiledSubView));
        tiledSubView.Arrangement = ViewArrangement.Fixed;
        movableSizeableWithProgress.Add (tiledSubView);
        ProgressBar progressBar = new ()
        {
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Id = "progressBar"
        };
        movableSizeableWithProgress.Add (progressBar);
        Timer timer = new (10)
        {
            AutoReset = true
        };
        timer.Elapsed += (_, _) =>
        {
            if (Math.Abs (progressBar.Fraction - 1f) < 0.001)
            {
                progressBar.Fraction = 0;
            }
            progressBar.Fraction += 0.01f;
        };
        timer.Start ();

        View overlappedView2 = CreateOverlappedView (3, 10, 12);
        overlappedView2.Title = "_Not Movable";
        overlappedView2.Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable;
        View overlappedInOverlapped1 = CreateOverlappedView (4, 1, 4);
        overlappedView2.Add (overlappedInOverlapped1);
        View overlappedInOverlapped2 = CreateOverlappedView (5, 10, 7);
        overlappedView2.Add (overlappedInOverlapped2);

        StatusBar statusBar = new ();
        statusBar.Add (
                       new Shortcut
                       {
                           Title = "Hide",
                           Text = "Hotkey",
                           Key = Key.F4,
                           Action = () =>
                           {
                               // TODO: move this logic into `View.ShowHide()` or similar
                               overlappedView2.Visible = false;
                               overlappedView2.Enabled = overlappedView2.Visible;
                           }
                       });

        statusBar.Add (
                       new Shortcut
                       {
                           Title = "Toggle Hide",
                           Text = "App",
                           BindKeyToApplication = true,
                           Key = Key.F4.WithCtrl,
                           Action = () =>
                           {
                               // TODO: move this logic into `View.ShowHide()` or similar
                               overlappedView2.Visible = !overlappedView2.Visible;
                               overlappedView2.Enabled = overlappedView2.Visible;

                               if (overlappedView2.Visible)
                               {
                                   overlappedView2.SetFocus ();
                               }
                           }
                       });
        overlappedView2.Add (statusBar);

        ColorPicker colorPicker = new ()
        {
            Y = 12,
            Width = Dim.Fill (),
            Id = "colorPicker",
            Style = new ()
            {
                ShowTextFields = true,
                ShowColorName = true
            }
        };
        colorPicker.ApplyStyleChanges ();

        colorPicker.SelectedColor = testFrame.GetAttributeForRole (VisualRole.Normal).Background;
        colorPicker.ColorChanged += ColorPickerColorChanged;
        overlappedView2.Add (colorPicker);
        overlappedView2.Width = 50;

        DatePicker datePicker = new ()
        {
            X = 1,
            Y = 15,
            Id = "datePicker",
            Title = "Not _Sizeable",
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
        };

        datePicker.SetScheme (new Scheme (
                                          new Attribute (
                                                         SchemeManager.GetScheme (Schemes.Runnable).Normal.Foreground.GetBrighterColor (),
                                                         SchemeManager.GetScheme (Schemes.Runnable).Normal.Background.GetBrighterColor (),
                                                         SchemeManager.GetScheme (Schemes.Runnable).Normal.Style)));

        TransparentView transparentView = new ()
        {
            Title = "Transparent",
            ViewportSettings = ViewportSettingsFlags.Transparent,
            X = 50,
            Y = Pos.Bottom (tiledFrame),
            Width = 35,
            Height = 15
        };

        testFrame.Add (overlappedView2);
        testFrame.Add (datePicker);
        testFrame.Add (movableSizeableWithProgress);
        testFrame.Add (transparentView);


        testFrame.Add (new TransparentView ()
        {
            X = 50,
            Y = 25,
            Width = 35,
            Height = 15,
            Title = "Transparent|TransparentMouse",
            ViewportSettings = ViewportSettingsFlags.TransparentMouse | ViewportSettingsFlags.Transparent
        });

        mainWindow.Initialized += OnMainWindowInitialized;

        Application.Run (mainWindow);
        timer.Close ();
        mainWindow.Dispose ();
        Application.Shutdown ();

        return;

        void OnMainWindowInitialized (object sender, EventArgs e)
        {
            adornmentsEditor.AutoSelectSuperView = testFrame;
            arrangementEditor.AutoSelectSuperView = testFrame;

            testFrame.MoveSubViewToStart (movableSizeableWithProgress);
            movableSizeableWithProgress.SetFocus ();
        }

        void ColorPickerColorChanged (object sender, ResultEventArgs<Color> e)
        {
            testFrame.SetScheme (testFrame.GetScheme () with { Normal = new (testFrame.GetAttributeForRole (VisualRole.Normal).Foreground, e.Result) });
        }
    }



    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        var overlapped = new View
        {
            X = x,
            Y = y,
            Width = Dim.Auto (minimumContentDim: 20),
            Height = Dim.Auto (minimumContentDim: 3),
            Title = $"Overlapped{id} _{GetNextHotKey ()}",
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Runnable),
            Id = $"Overlapped{id}",
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped | ViewArrangement.Resizable
        };

        return overlapped;
    }

    private View CreateTiledView (int id, Pos x, Pos y)
    {
        var tiled = new View
        {
            X = x,
            Y = y,
            Width = Dim.Auto (minimumContentDim: 15),
            Height = Dim.Auto (minimumContentDim: 2),
            Title = $"Tiled{id} _{GetNextHotKey ()}",
            Id = $"Tiled{id}",
            BorderStyle = LineStyle.Single,
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Resizable
        };

        return tiled;
    }

    private char GetNextHotKey () { return (char)('A' + _hotkeyCount++); }

    public override List<Key> GetDemoKeyStrokes ()
    {
        var keys = new List<Key> ();

        // Select view with progress bar
        keys.Add ((Key)'&');

        keys.Add (Application.ArrangeKey);

        for (int i = 0; i < 8; i++)
        {
            keys.Add (Key.CursorUp);
        }

        for (int i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorRight);
        }

        keys.Add (Application.ArrangeKey);

        keys.Add (Key.S);

        keys.Add (Application.ArrangeKey);

        for (int i = 0; i < 10; i++)
        {
            keys.Add (Key.CursorUp);
        }

        for (int i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        keys.Add (Application.ArrangeKey);

        // Select view with progress bar
        keys.Add ((Key)'&');

        keys.Add (Application.ArrangeKey);

        keys.Add (Key.Tab);

        for (int i = 0; i < 10; i++)
        {
            keys.Add (Key.CursorRight);
            keys.Add (Key.CursorDown);
        }

        return keys;
    }

    public sealed class TransparentView : FrameView
    {
        public TransparentView ()
        {
            Title = "Transparent";
            Text = "TransparentView Text";
            X = 0;
            Y = 0;
            Width = 30;
            Height = 10;
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;

            Padding!.Thickness = new Thickness (1);

            Add (
                 new Button ()
                 {
                     Title = "_Hi",
                     X = Pos.Center (),
                     Y = Pos.Center ()
                 });
        }
    }
}

