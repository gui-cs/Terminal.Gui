using System.Collections.Generic;
using Terminal.Gui;
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

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            TabStop = TabBehavior.TabGroup
        };

        var adornmentsEditor = new AdornmentsEditor
        {
            X = 0,
            Y = 0,
            AutoSelectViewToEdit = true,
            TabStop = TabBehavior.NoStop,
            ShowViewIdentifier = true
        };

        app.Add (adornmentsEditor);

        adornmentsEditor.ExpanderButton.Orientation = Orientation.Horizontal;

        //  adornmentsEditor.ExpanderButton!.Collapsed = true;

        var arrangementEditor = new ArrangementEditor
        {
            X = Pos.Right (adornmentsEditor),
            Y = 0,
            AutoSelectViewToEdit = true,
            TabStop = TabBehavior.NoStop
        };
        app.Add (arrangementEditor);

        FrameView testFrame = new ()
        {
            Title = "_1 Test Frame",
            Text = "This is the text of the Test Frame.\nLine 2.\nLine 3.",
            X = Pos.Right (arrangementEditor),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        testFrame.TextAlignment = Alignment.Center;
        testFrame.VerticalTextAlignment = Alignment.Center;

        app.Add (testFrame);

        View tiledView1 = CreateTiledView (0, 2, 1);
        View tiledView2 = CreateTiledView (1, Pos.Right (tiledView1) - 1, Pos.Top (tiledView1));
        tiledView2.Height = Dim.Height (tiledView1);
        View tiledView3 = CreateTiledView (2, Pos.Right (tiledView2) - 1, Pos.Top (tiledView2));
        tiledView3.Height = Dim.Height (tiledView1);
        View tiledView4 = CreateTiledView (3, Pos.Left (tiledView1), Pos.Bottom (tiledView1) - 1);
        tiledView4.Width = Dim.Func (() => tiledView3.Frame.Width + tiledView2.Frame.Width + tiledView1.Frame.Width - 2);

        View movableSizeableWithProgress = CreateOverlappedView (2, 10, 8);
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

        timer.Elapsed += (o, args) =>
                         {
                             if (progressBar!.Fraction == 1.0)
                             {
                                 progressBar.Fraction = 0;
                             }

                             progressBar.Fraction += 0.01f;

                             Application.Wakeup ();

                             progressBar.SetNeedsDraw ();
                         };
        timer.Start ();

        View overlappedView2 = CreateOverlappedView (3, 4, 15);
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

        colorPicker.SelectedColor = testFrame.ColorScheme!.Normal.Background;
        colorPicker.ColorChanged += ColorPicker_ColorChanged;
        overlappedView2.Add (colorPicker);
        overlappedView2.Width = 50;

        DatePicker datePicker = new ()
        {
            X = 30,
            Y = 17,
            Id = "datePicker",
            Title = "Not _Sizeable",
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
        };

        TransparentView transparentView = new ()
        {
            Id = "transparentView",
            X = 30,
            Y = 5,
            Width = 35,
            Height = 15
        };

        testFrame.Add (tiledView4, tiledView3, tiledView2, tiledView1);
        testFrame.Add (overlappedView2);
        testFrame.Add (datePicker);
        testFrame.Add (movableSizeableWithProgress);
        testFrame.Add (transparentView);


        testFrame.Add (new TransparentView ());

        adornmentsEditor.AutoSelectSuperView = testFrame;
        arrangementEditor.AutoSelectSuperView = testFrame;

        testFrame.SetFocus ();

        Application.Run (app);
        timer.Close ();
        app.Dispose ();
        Application.Shutdown ();

        return;

        void ColorPicker_ColorChanged (object sender, ColorEventArgs e)
        {
            testFrame.ColorScheme = testFrame.ColorScheme! with { Normal = new (testFrame.ColorScheme.Normal.Foreground, e.CurrentValue) };
        }
    }

    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        var overlapped = new View
        {
            X = x,
            Y = y,
            Width = Dim.Auto (minimumContentDim: 15),
            Height = Dim.Auto (minimumContentDim: 3),
            Title = $"Overlapped{id} _{GetNextHotKey ()}",
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
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
            Height = Dim.Auto (minimumContentDim: 3),
            Title = $"Tiled{id} _{GetNextHotKey ()}",
            Id = $"Tiled{id}",
            BorderStyle = LineStyle.Single,
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Resizable

            //            SuperViewRendersLineCanvas = true
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

    public class TransparentView : FrameView
    {
        public TransparentView()
        {
            Title = "Transparent";
            Text = "Text";
            X = 0;
            Y = 0;
            Width = 30;
            Height = 10;
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
            ViewportSettings |= Terminal.Gui.ViewportSettings.Transparent;

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

public class TransparentView : FrameView
{
    public TransparentView ()
    {
        Title = "Transparent View";
        base.Text = "View.Text.\nThis should be opaque.\nNote how clipping works?";
        TextFormatter.Alignment = Alignment.Center;
        TextFormatter.VerticalAlignment = Alignment.Center;
        Arrangement = ViewArrangement.Overlapped | ViewArrangement.Resizable | ViewArrangement.Movable;
        ViewportSettings |= Terminal.Gui.ViewportSettings.Transparent | Terminal.Gui.ViewportSettings.TransparentMouse;
        BorderStyle = LineStyle.RoundedDotted;
        base.ColorScheme = Colors.ColorSchemes ["Menu"];

        var transparentSubView = new View ()
        {
            Text = "Sizable/Movable View with border. Should be opaque. The shadow should be semi-opaque.",
            Id = "transparentSubView",
            X = 4,
            Y = 8,
            Width = 20,
            Height = 8,
            BorderStyle = LineStyle.Dashed,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            ShadowStyle = ShadowStyle.Transparent,
            //ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };
        transparentSubView.Border.Thickness = new (1, 1, 1, 1);
        transparentSubView.ColorScheme = Colors.ColorSchemes ["Dialog"];

        Button button = new Button ()
        {
            Title = "_Opaque Shadows No Worky",
            X = Pos.Center (),
            Y = 4,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
        };

        button.ClearingViewport += (sender, args) =>
                                   {
                                       args.Cancel = true;
                                   };


        base.Add (button);
        base.Add (transparentSubView);
    }

    /// <inheritdoc />
    protected override bool OnClearingViewport () { return false; }

    /// <inheritdoc />
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent) { return false; }
}
