using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling", "Content scrolling, IScrollBars, etc...")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Tests")]
public class Scrolling : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        var app = new Window
        {
            Title = GetQuitKeyAndName ()
        };

        var label = new Label { X = 0, Y = 0 };
        app.Add (label);

        var demoView = new DemoView
        {
            Id = "demoView",
            X = 2,
            Y = Pos.Bottom (label) + 1,
            Width = 60,
            Height = 20
        };

        label.Text =
            $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";

        demoView.ViewportChanged += (_, _) =>
                                    {
                                        label.Text =
                                            $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";
                                    };

        app.Add (demoView);

        //// NOTE: This call to EnableScrollBar is technically not needed because the reference
        //// NOTE: to demoView.HorizontalScrollBar below will cause it to be lazy created.
        //// NOTE: The call included in this sample to for illustration purposes.
        //demoView.EnableScrollBar (Orientation.Horizontal);
        var hCheckBox = new CheckBox
        {
            X = Pos.X (demoView),
            Y = Pos.Bottom (demoView),
            Text = "_HorizontalScrollBar.Visible",
            CheckedState = demoView.HorizontalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (hCheckBox);
        hCheckBox.CheckedStateChanged += (sender, args) => { demoView.HorizontalScrollBar.Visible = args.CurrentValue == CheckState.Checked; };

        //// NOTE: This call to EnableScrollBar is technically not needed because the reference
        //// NOTE: to demoView.HorizontalScrollBar below will cause it to be lazy created.
        //// NOTE: The call included in this sample to for illustration purposes.
        //demoView.EnableScrollBar (Orientation.Vertical);
        var vCheckBox = new CheckBox
        {
            X = Pos.Right (hCheckBox) + 3,
            Y = Pos.Bottom (demoView),
            Text = "_VerticalScrollBar.Visible",
            CheckedState = demoView.VerticalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (vCheckBox);
        vCheckBox.CheckedStateChanged += (sender, args) => { demoView.VerticalScrollBar.Visible = args.CurrentValue == CheckState.Checked; };

        var ahCheckBox = new CheckBox
        {
            X = Pos.Left (demoView),
            Y = Pos.Bottom (hCheckBox),
            Text = "_AutoShow (both)",
            CheckedState = demoView.HorizontalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked
        };

        ahCheckBox.CheckedStateChanging += (s, e) =>
                                           {
                                               demoView.HorizontalScrollBar.AutoShow = e.NewValue == CheckState.Checked;
                                               demoView.VerticalScrollBar.AutoShow = e.NewValue == CheckState.Checked;
                                           };
        app.Add (ahCheckBox);

        demoView.VerticalScrollBar.VisibleChanging += (sender, args) => { vCheckBox.CheckedState = args.NewValue ? CheckState.Checked : CheckState.UnChecked; };

        demoView.HorizontalScrollBar.VisibleChanging += (sender, args) =>
                                                        {
                                                            hCheckBox.CheckedState = args.NewValue ? CheckState.Checked : CheckState.UnChecked;
                                                        };

        var count = 0;

        var mousePos = new Label
        {
            X = Pos.Right (demoView) + 1,
            Y = Pos.AnchorEnd (1),

            Width = 50,
            Text = "Mouse: "
        };
        app.Add (mousePos);
        Application.MouseEvent += (sender, a) => { mousePos.Text = $"Mouse: ({a.Position}) - {a.Flags} {count++}"; };

        // Add a progress bar to cause constant redraws
        var progress = new ProgressBar { X = Pos.Right (demoView) + 1, Y = Pos.AnchorEnd (2), Width = 50 };

        app.Add (progress);

        var pulsing = true;

        bool TimerFn ()
        {
            progress.Pulse ();

            return pulsing;
        }

        Application.AddTimeout (TimeSpan.FromMilliseconds (300), TimerFn);

        app.Unloaded += AppUnloaded;

        Application.Run (app);
        app.Unloaded -= AppUnloaded;
        app.Dispose ();
        Application.Shutdown ();

        return;

        void AppUnloaded (object sender, EventArgs args) { pulsing = false; }
    }
}

public class DemoView : View
{
    public DemoView ()
    {
        base.ColorScheme = Colors.ColorSchemes ["TopLevel"];
        CanFocus = true;
        BorderStyle = LineStyle.Heavy;
        Arrangement = ViewArrangement.Resizable;
        Initialized += OnInitialized;
        HorizontalScrollBar.AutoShow = true;
        VerticalScrollBar.AutoShow = true;
    }

    private void OnInitialized (object sender, EventArgs e)
    {
        SetContentSize (new (80, 25));

        var rulerView = new View
        {
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        rulerView.Border!.Thickness = new (1);
        rulerView.Border.LineStyle = LineStyle.None;
        rulerView.Border.Diagnostics = ViewDiagnosticFlags.Ruler;
        rulerView.Border.ColorScheme = Colors.ColorSchemes ["Error"];

        Add (rulerView);

        var centeredLabel = new Label ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Center,
            Text = $"This label is centred.\nContentSize is {GetContentSize ()}"
        };
        Add (centeredLabel);

        var pressMeButton = new Button
        {
            X = 1,
            Y = 1,
            Text = "Press me!"
        };
        pressMeButton.Accepting += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
        Add (pressMeButton);

        var aLongButton = new Button
        {
            X = Pos.Right (pressMeButton),
            Y = Pos.Bottom (pressMeButton),

            Text = "A very long button. Should be wide enough to demo clipping!"
        };
        aLongButton.Accepting += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
        Add (aLongButton);

        Add (
             new TextField
             {
                 X = Pos.Left (pressMeButton),
                 Y = Pos.Bottom (aLongButton) + 1,
                 Width = 50,
                 ColorScheme = Colors.ColorSchemes ["Dialog"],
                 Text = "This is a test of..."
             }
            );

        Add (
             new TextField
             {
                 X = Pos.Left (pressMeButton),
                 Y = Pos.Bottom (aLongButton) + 3,
                 Width = 50,
                 ColorScheme = Colors.ColorSchemes ["Dialog"],
                 Text = "... the emergency broadcast system."
             }
            );

        Add (
             new TextField
             {
                 X = Pos.Left (pressMeButton),
                 Y = 40,
                 Width = 50,
                 ColorScheme = Colors.ColorSchemes ["Error"],
                 Text = "Last line - Beyond content area @ Y = 40"
             }
            );

        // Demonstrate AnchorEnd - Button is anchored to bottom/right
        var anchorButton = new Button
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Text = "Bottom Right"
        };

        anchorButton.Accepting += (s, e) =>
                                  {
                                      // This demonstrates how to have a dynamically sized button
                                      // Each time the button is clicked the button's text gets longer
                                      anchorButton.Text += "!";
                                  };
        Add (anchorButton);
    }

    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags == MouseFlags.WheeledDown)
        {
            ScrollVertical (1);
            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags == MouseFlags.WheeledUp)
        {
            ScrollVertical (-1);
            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags == MouseFlags.WheeledRight)
        {
            ScrollHorizontal (1);
            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags == MouseFlags.WheeledLeft)
        {
            ScrollHorizontal (-1);
            return mouseEvent.Handled = true;
        }

        return false;
    }
}
