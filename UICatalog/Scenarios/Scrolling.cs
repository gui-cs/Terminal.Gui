using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling", "Demonstrates scrolling etc...")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Tests")]
public class Scrolling : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public override void Main ()
    {
        Application.Init ();
        _diagnosticFlags = View.Diagnostics;
        View.Diagnostics = ViewDiagnosticFlags.Ruler;

        var app = new Window
        {
            Title = GetQuitKeyAndName (),

            // Offset to stress clipping
            X = 3,
            Y = 3,
            Width = Dim.Fill (3),
            Height = Dim.Fill (3)
        };

        var label = new Label { X = 0, Y = 0 };
        app.Add (label);

        var scrollView = new ScrollView
        {
            Id = "scrollView",
            X = 2,
            Y = Pos.Bottom (label) + 1,
            Width = 60,
            Height = 20,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],

            //ContentOffset = Point.Empty,
            ShowVerticalScrollIndicator = true,
            ShowHorizontalScrollIndicator = true
        };
        // BUGBUG: set_ContentSize is supposed to be `protected`. 
        scrollView.SetContentSize (new (120, 40));
        scrollView.Padding.Thickness = new (1);

        label.Text = $"{scrollView}\nContentSize: {scrollView.GetContentSize ()}\nContentOffset: {scrollView.ContentOffset}";

        const string rule = "0123456789";

        var horizontalRuler = new Label
        {
            X = 0,
            Y = 0,

            Width = Dim.Fill (),
            Height = 2,
            ColorScheme = Colors.ColorSchemes ["Error"]
        };
        scrollView.Add (horizontalRuler);

        const string vrule = "0\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

        var verticalRuler = new Label
        {
            X = 0,
            Y = 0,

            Width = 1,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Error"]
        };
        scrollView.Add (verticalRuler);

        var pressMeButton = new Button { X = 3, Y = 3, Text = "Press me!" };
        pressMeButton.Accepting += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
        scrollView.Add (pressMeButton);

        var aLongButton = new Button
        {
            X = 3,
            Y = 4,

            Width = Dim.Fill (3),
            Text = "A very long button. Should be wide enough to demo clipping!"
        };
        aLongButton.Accepting += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
        scrollView.Add (aLongButton);

        scrollView.Add (
                        new TextField
                        {
                            X = 3,
                            Y = 5,
                            Width = 50,
                            ColorScheme = Colors.ColorSchemes ["Dialog"],
                            Text = "This is a test of..."
                        }
                       );

        scrollView.Add (
                        new TextField
                        {
                            X = 3,
                            Y = 10,
                            Width = 50,
                            ColorScheme = Colors.ColorSchemes ["Dialog"],
                            Text = "... the emergency broadcast system."
                        }
                       );

        scrollView.Add (
                        new TextField
                        {
                            X = 3,
                            Y = 99,
                            Width = 50,
                            ColorScheme = Colors.ColorSchemes ["Dialog"],
                            Text = "Last line"
                        }
                       );

        // Demonstrate AnchorEnd - Button is anchored to bottom/right
        var anchorButton = new Button { Y = Pos.AnchorEnd (0) - 1, Text = "Bottom Right" };

        // TODO: Use Pos.Width instead of (Right-Left) when implemented (#502)
        anchorButton.X = Pos.AnchorEnd (0) - (Pos.Right (anchorButton) - Pos.Left (anchorButton));

        anchorButton.Accepting += (s, e) =>
                               {
                                   // This demonstrates how to have a dynamically sized button
                                   // Each time the button is clicked the button's text gets longer
                                   anchorButton.Text += "!";

                               };
        scrollView.Add (anchorButton);

        app.Add (scrollView);

        var hCheckBox = new CheckBox
        {
            X = Pos.X (scrollView),
            Y = Pos.Bottom (scrollView),
            Text = "Horizontal Scrollbar",
            CheckedState = scrollView.ShowHorizontalScrollIndicator ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (hCheckBox);

        var vCheckBox = new CheckBox
        {
            X = Pos.Right (hCheckBox) + 3,
            Y = Pos.Bottom (scrollView),
            Text = "Vertical Scrollbar",
            CheckedState = scrollView.ShowVerticalScrollIndicator ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (vCheckBox);

        var t = "Auto Hide Scrollbars";

        var ahCheckBox = new CheckBox
        {
            X = Pos.Left (scrollView), Y = Pos.Bottom (hCheckBox), Text = t, CheckedState = scrollView.AutoHideScrollBars ? CheckState.Checked : CheckState.UnChecked
        };
        var k = "Keep Content Always In Viewport";

        var keepCheckBox = new CheckBox
        {
            X = Pos.Left (scrollView), Y = Pos.Bottom (ahCheckBox), Text = k, CheckedState = scrollView.AutoHideScrollBars ? CheckState.Checked : CheckState.UnChecked
        };

        hCheckBox.CheckedStateChanging += (s, e) =>
                             {
                                 if (ahCheckBox.CheckedState == CheckState.UnChecked)
                                 {
                                     scrollView.ShowHorizontalScrollIndicator = e.NewValue == CheckState.Checked;
                                 }
                                 else
                                 {
                                     hCheckBox.CheckedState = CheckState.Checked;
                                     MessageBox.Query ("Message", "Disable Auto Hide Scrollbars first.", "Ok");
                                 }
                             };

        vCheckBox.CheckedStateChanging += (s, e) =>
                             {
                                 if (ahCheckBox.CheckedState == CheckState.UnChecked)
                                 {
                                     scrollView.ShowVerticalScrollIndicator = e.NewValue == CheckState.Checked;
                                 }
                                 else
                                 {
                                     vCheckBox.CheckedState = CheckState.Checked;
                                     MessageBox.Query ("Message", "Disable Auto Hide Scrollbars first.", "Ok");
                                 }
                             };

        ahCheckBox.CheckedStateChanging += (s, e) =>
                              {
                                  scrollView.AutoHideScrollBars = e.NewValue == CheckState.Checked;
                                  hCheckBox.CheckedState = CheckState.Checked;
                                  vCheckBox.CheckedState = CheckState.Checked;
                              };
        app.Add (ahCheckBox);

        keepCheckBox.CheckedStateChanging += (s, e) => scrollView.KeepContentAlwaysInViewport = e.NewValue == CheckState.Checked;
        app.Add (keepCheckBox);

        var count = 0;

        var mousePos = new Label
        {
            X = Pos.Right (scrollView) + 1,
            Y = Pos.AnchorEnd (1),

            Width = 50,
            Text = "Mouse: "
        };
        app.Add (mousePos);
        Application.MouseEvent += (sender, a) => { mousePos.Text = $"Mouse: ({a.Position}) - {a.Flags} {count++}"; };

        // Add a progress bar to cause constant redraws
        var progress = new ProgressBar { X = Pos.Right (scrollView) + 1, Y = Pos.AnchorEnd (2), Width = 50 };

        app.Add (progress);

        var pulsing = true;

        bool timer ()
        {
            progress.Pulse ();

            return pulsing;
        }

        Application.AddTimeout (TimeSpan.FromMilliseconds (300), timer);

        app.Loaded += App_Loaded;
        app.Unloaded += app_Unloaded;

        Application.Run (app);
        app.Loaded -= App_Loaded;
        app.Unloaded -= app_Unloaded;
        app.Dispose ();
        Application.Shutdown ();

        return;

        // Local functions
        void App_Loaded (object sender, EventArgs args)
        {
            horizontalRuler.Text =
                rule.Repeat ((int)Math.Ceiling (horizontalRuler.Viewport.Width / (double)rule.Length)) [
                                                                                                        ..horizontalRuler.Viewport.Width]
                + "\n"
                + "|         ".Repeat (
                                       (int)Math.Ceiling (horizontalRuler.Viewport.Width / (double)rule.Length)
                                      ) [
                                         ..horizontalRuler.Viewport.Width];

            verticalRuler.Text =
                vrule.Repeat ((int)Math.Ceiling (verticalRuler.Viewport.Height * 2 / (double)rule.Length))
                    [..(verticalRuler.Viewport.Height * 2)];
        }

        void app_Unloaded (object sender, EventArgs args)
        {
            View.Diagnostics = _diagnosticFlags;
            pulsing = false;
        }
    }
}
