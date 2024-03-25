using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling", "Demonstrates ScrollView etc...")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ScrollView")]
[ScenarioCategory ("Tests")]
public class Scrolling : Scenario
{
    public override void Setup ()
    {
        // Offset Win to stress clipping
        Win.X = 1;
        Win.Y = 1;
        Win.Width = Dim.Fill (1);
        Win.Height = Dim.Fill (1);
        var label = new Label { X = 0, Y = 0 };
        Win.Add (label);

        var scrollView = new ScrollView
        {
            Id = "scrollView",
            X = 2,
            Y = Pos.Bottom (label) + 1,
            Width = 50,
            Height = 20,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            ContentSize = new (200, 100),

            //ContentOffset = Point.Empty,
            ShowVerticalScrollIndicator = true,
            ShowHorizontalScrollIndicator = true
        };

        label.Text =
            $"{scrollView}\nContentSize: {scrollView.ContentSize}\nContentOffset: {scrollView.ContentOffset}";

        const string rule = "0123456789";

        var horizontalRuler = new Label
        {
            X = 0,
            Y = 0,
            AutoSize = false,
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
            AutoSize = false,
            Width = 1,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Error"]
        };
        scrollView.Add (verticalRuler);

        void Top_Loaded (object sender, EventArgs args)
        {
            horizontalRuler.Text =
                rule.Repeat ((int)Math.Ceiling (horizontalRuler.Bounds.Width / (double)rule.Length)) [
                                                                                                      ..horizontalRuler.Bounds.Width]
                + "\n"
                + "|         ".Repeat (
                                       (int)Math.Ceiling (horizontalRuler.Bounds.Width / (double)rule.Length)
                                      ) [
                                         ..horizontalRuler.Bounds.Width];

            verticalRuler.Text =
                vrule.Repeat ((int)Math.Ceiling (verticalRuler.Bounds.Height * 2 / (double)rule.Length))
                    [..(verticalRuler.Bounds.Height * 2)];
            Top.Loaded -= Top_Loaded;
        }

        Top.Loaded += Top_Loaded;

        var pressMeButton = new Button { X = 3, Y = 3, Text = "Press me!" };
        pressMeButton.Accept += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
        scrollView.Add (pressMeButton);

        var aLongButton = new Button
        {
            X = 3,
            Y = 4,
            AutoSize = false,
            Width = Dim.Fill (3),
            Text = "A very long button. Should be wide enough to demo clipping!"
        };
        aLongButton.Accept += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
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
        var anchorButton = new Button { Y = Pos.AnchorEnd () - 1, Text = "Bottom Right" };

        // TODO: Use Pos.Width instead of (Right-Left) when implemented (#502)
        anchorButton.X = Pos.AnchorEnd () - (Pos.Right (anchorButton) - Pos.Left (anchorButton));

        anchorButton.Accept += (s, e) =>
                                {
                                    // This demonstrates how to have a dynamically sized button
                                    // Each time the button is clicked the button's text gets longer
                                    // The call to Win.LayoutSubviews causes the Computed layout to
                                    // get updated. 
                                    anchorButton.Text += "!";
                                    Win.LayoutSubviews ();
                                };
        scrollView.Add (anchorButton);

        Win.Add (scrollView);

        var hCheckBox = new CheckBox
        {
            X = Pos.X (scrollView),
            Y = Pos.Bottom (scrollView),
            Text = "Horizontal Scrollbar",
            Checked = scrollView.ShowHorizontalScrollIndicator
        };
        Win.Add (hCheckBox);

        var vCheckBox = new CheckBox
        {
            X = Pos.Right (hCheckBox) + 3,
            Y = Pos.Bottom (scrollView),
            Text = "Vertical Scrollbar",
            Checked = scrollView.ShowVerticalScrollIndicator
        };
        Win.Add (vCheckBox);

        var t = "Auto Hide Scrollbars";

        var ahCheckBox = new CheckBox
        {
            X = Pos.Left (scrollView), Y = Pos.Bottom (hCheckBox), Text = t, Checked = scrollView.AutoHideScrollBars
        };
        var k = "Keep Content Always In Viewport";

        var keepCheckBox = new CheckBox
        {
            X = Pos.Left (scrollView), Y = Pos.Bottom (ahCheckBox), Text = k, Checked = scrollView.AutoHideScrollBars
        };

        hCheckBox.Toggled += (s, e) =>
                             {
                                 if (ahCheckBox.Checked == false)
                                 {
                                     scrollView.ShowHorizontalScrollIndicator = (bool)hCheckBox.Checked;
                                 }
                                 else
                                 {
                                     hCheckBox.Checked = true;
                                     MessageBox.Query ("Message", "Disable Auto Hide Scrollbars first.", "Ok");
                                 }
                             };

        vCheckBox.Toggled += (s, e) =>
                             {
                                 if (ahCheckBox.Checked == false)
                                 {
                                     scrollView.ShowVerticalScrollIndicator = (bool)vCheckBox.Checked;
                                 }
                                 else
                                 {
                                     vCheckBox.Checked = true;
                                     MessageBox.Query ("Message", "Disable Auto Hide Scrollbars first.", "Ok");
                                 }
                             };

        ahCheckBox.Toggled += (s, e) =>
                              {
                                  scrollView.AutoHideScrollBars = (bool)ahCheckBox.Checked;
                                  hCheckBox.Checked = true;
                                  vCheckBox.Checked = true;
                              };
        Win.Add (ahCheckBox);

        keepCheckBox.Toggled += (s, e) => scrollView.KeepContentAlwaysInViewport = (bool)keepCheckBox.Checked;
        Win.Add (keepCheckBox);

        //var scrollView2 = new ScrollView (new (55, 2, 20, 8)) {
        //	ContentSize = new (20, 50),
        //	//ContentOffset = Point.Empty,
        //	ShowVerticalScrollIndicator = true,
        //	ShowHorizontalScrollIndicator = true
        //};
        //var filler = new Filler (new (0, 0, 60, 40));
        //scrollView2.Add (filler);
        //scrollView2.DrawContent += (s,e) => {
        //	scrollView2.ContentSize = filler.GetContentSize ();
        //};
        //Win.Add (scrollView2);

        //// This is just to debug the visuals of the scrollview when small
        //var scrollView3 = new ScrollView (new (55, 15, 3, 3)) {
        //	ContentSize = new (100, 100),
        //	ShowVerticalScrollIndicator = true,
        //	ShowHorizontalScrollIndicator = true
        //};
        //scrollView3.Add (new Box10x (0, 0));
        //Win.Add (scrollView3);

        var count = 0;

        var mousePos = new Label
        {
            X = Pos.Right (scrollView) + 1,
            Y = Pos.AnchorEnd (1),
            AutoSize = false,
            Width = 50,
            Text = "Mouse: "
        };
        Win.Add (mousePos);
        Application.MouseEvent += (sender, a) => { mousePos.Text = $"Mouse: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count++}"; };

        var progress = new ProgressBar { X = Pos.Right (scrollView) + 1, Y = Pos.AnchorEnd (2), Width = 50 };
        Win.Add (progress);

        var pulsing = true;

        bool timer ()
        {
            progress.Pulse ();

            return pulsing;
        }

        Application.AddTimeout (TimeSpan.FromMilliseconds (300), timer);

        void Top_Unloaded (object sender, EventArgs args)
        {
            pulsing = false;
            Top.Unloaded -= Top_Unloaded;
        }

        Top.Unloaded += Top_Unloaded;
    }
}
