using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using static Terminal.Gui.Dialog;

namespace UICatalog.Scenarios;

/// <summary>
///     This Scenario demonstrates how to use Termina.gui's Dim and Pos Layout System. [x] - Using Dim.Fill to fill a
///     window [x] - Using Dim.Fill and Dim.Pos to automatically align controls based on an initial control [ ] - ...
/// </summary>
[ScenarioMetadata ("Computed Layout", "Demonstrates the Computed (Dim and Pos) Layout System.")]
[ScenarioCategory ("Layout")]
public class ComputedLayout : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
        };

        // Demonstrate using Dim to create a horizontal ruler that always measures the parent window's width
        const string rule = "|123456789";

        var horizontalRuler = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = 1,
            ColorScheme = Colors.ColorSchemes ["Error"],
            Text = rule
        };

        app.Add (horizontalRuler);

        // Demonstrate using Dim to create a vertical ruler that always measures the parent window's height
        const string vrule = "|\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

        var verticalRuler = new Label
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Error"],
            Text = vrule
        };

        app.LayoutComplete += (s, a) =>
                                          {
                                              horizontalRuler.Text =
                                                  rule.Repeat ((int)Math.Ceiling (horizontalRuler.Viewport.Width / (double)rule.Length)) [
                                                   ..horizontalRuler.Viewport.Width];

                                              verticalRuler.Text =
                                                  vrule.Repeat ((int)Math.Ceiling (verticalRuler.Viewport.Height * 2 / (double)rule.Length))
                                                      [..(verticalRuler.Viewport.Height * 2)];
                                          };

        app.Add (verticalRuler);

        // Demonstrate At - Using Pos.At to locate a view in an absolute location
        var atButton = new Button { Text = "At(2,1)", X = Pos.At (2), Y = Pos.At (1) };
        app.Add (atButton);

        // Throw in a literal absolute - Should function identically to above
        var absoluteButton = new Button { Text = "X = 30, Y = 1", X = 30, Y = 1 };
        app.Add (absoluteButton);

        // Demonstrate using Dim to create a window that fills the parent with a margin
        var margin = 10;
        var subWin = new Window { X = Pos.Center (), Y = 2, Width = Dim.Fill (margin), Height = 7 };

        subWin.Initialized += (s, a) =>
                              {
                                  subWin.Title =
                                      $"{subWin.GetType ().Name} {{X={subWin.X},Y={subWin.Y},Width={subWin.Width},Height={subWin.Height}}}";
                              };
        app.Add (subWin);

        var i = 1;
        var txt = "Resize the terminal to see computed layout in action.";
        List<Label> labelList = new ();
        labelList.Add (new Label { Text = "The lines below show different justification" });

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Left,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Right,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Centered,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Justified,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );
        subWin.Add (labelList.ToArray ());

        var frameView = new FrameView { X = 2, Y = Pos.Bottom (subWin), Width = 30, Height = 7 };

        frameView.Initialized += (sender, args) =>
                                 {
                                     var fv = sender as FrameView;

                                     fv.Title =
                                         $"{frameView.GetType ().Name} {{X={fv.X},Y={fv.Y},Width={fv.Width},Height={fv.Height}}}";
                                 };
        i = 1;
        labelList = new List<Label> ();
        labelList.Add (new Label { Text = "The lines below show different justifications" });

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Left,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Right,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Centered,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );

        labelList.Add (
                       new Label
                       {
                           Justification = Justification.Justified,
                           Width = Dim.Fill (),
                           X = 0,
                           Y = Pos.Bottom (labelList.LastOrDefault ()),
                           ColorScheme = Colors.ColorSchemes ["Dialog"],
                           Text = $"{i++}-{txt}"
                       }
                      );
        frameView.Add (labelList.ToArray ());
        app.Add (frameView);

        frameView = new FrameView
        {
            X = Pos.Right (frameView), Y = Pos.Top (frameView), Width = Dim.Fill (), Height = 7
        };

        frameView.Initialized += (sender, args) =>
                                 {
                                     var fv = sender as FrameView;

                                     fv.Title =
                                         $"{frameView.GetType ().Name} {{X={fv.X},Y={fv.Y},Width={fv.Width},Height={fv.Height}}}";
                                 };
        app.Add (frameView);

        // Demonstrate Dim & Pos using percentages - a TextField that is 30% height and 80% wide
        var textView = new TextView
        {
            X = Pos.Center (),
            Y = Pos.Percent (50),
            Width = Dim.Percent (80),
            Height = Dim.Percent (10),
            ColorScheme = Colors.ColorSchemes ["TopLevel"]
        };

        textView.Text =
            "This TextView should horizontally & vertically centered and \n10% of the screeen height, and 80% of its width.";
        app.Add (textView);

        var oddballButton = new Button
        {
            Text = "These buttons demo convoluted PosCombine scenarios",
            X = Pos.Center (),
            Y = Pos.Bottom (textView) + 1
        };
        app.Add (oddballButton);

        #region Issue2358

        // Demonstrate odd-ball Combine scenarios
        // Until https://github.com/gui-cs/Terminal.Gui/issues/2358 is fixed these won't work right

        oddballButton = new Button { Text = "Center + 0", X = Pos.Center () + 0, Y = Pos.Bottom (oddballButton) };
        app.Add (oddballButton);

        oddballButton = new Button { Text = "Center + 1", X = Pos.Center () + 1, Y = Pos.Bottom (oddballButton) };
        app.Add (oddballButton);

        oddballButton = new Button { Text = "0 + Center", X = 0 + Pos.Center (), Y = Pos.Bottom (oddballButton) };
        app.Add (oddballButton);

        oddballButton = new Button { Text = "1 + Center", X = 1 + Pos.Center (), Y = Pos.Bottom (oddballButton) };
        app.Add (oddballButton);

        oddballButton = new Button { Text = "Center - 1", X = Pos.Center () - 1, Y = Pos.Bottom (oddballButton) };
        app.Add (oddballButton);

        // This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
        // The `- Pos.Percent(5)` is there so at least something is visible
        oddballButton = new Button
        {
            Text = "Center + Center - Percent(50)",
            X = Pos.Center () + Pos.Center () - Pos.Percent (50),
            Y = Pos.Bottom (oddballButton)
        };
        app.Add (oddballButton);

        // This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
        // The `- Pos.Percent(5)` is there so at least something is visible
        oddballButton = new Button
        {
            Text = "Percent(50) + Center - Percent(50)",
            X = Pos.Percent (50) + Pos.Center () - Pos.Percent (50),
            Y = Pos.Bottom (oddballButton)
        };
        app.Add (oddballButton);

        // This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
        // The `- Pos.Percent(5)` is there so at least something is visible
        oddballButton = new Button
        {
            Text = "Center + Percent(50) - Percent(50)",
            X = Pos.Center () + Pos.Percent (50) - Pos.Percent (50),
            Y = Pos.Bottom (oddballButton)
        };
        app.Add (oddballButton);

        #endregion

        // This demonstrates nonsense: Same as At(0)
        oddballButton = new Button
        {
            Text = "Center - Center - Percent(50)",
            X = Pos.Center () + Pos.Center () - Pos.Percent (50),
            Y = Pos.Bottom (oddballButton)
        };
        app.Add (oddballButton);

        // This demonstrates combining Percents)
        oddballButton = new Button
        {
            Text = "Percent(40) + Percent(10)", X = Pos.Percent (40) + Pos.Percent (10), Y = Pos.Bottom (oddballButton)
        };
        app.Add (oddballButton);

        // Demonstrate AnchorEnd - Button is anchored to bottom/right
        var anchorButton = new Button { Text = "Button using AnchorEnd", Y = Pos.AnchorEnd ()};
        anchorButton.X = Pos.AnchorEnd ();

        anchorButton.Accept += (s, e) =>
                                {
                                    // This demonstrates how to have a dynamically sized button
                                    // Each time the button is clicked the button's text gets longer
                                    // The call to app.LayoutSubviews causes the Computed layout to
                                    // get updated. 
                                    anchorButton.Text += "!";
                                    app.LayoutSubviews ();
                                };
        app.Add (anchorButton);

        // Demonstrate AnchorEnd(n) 
        // This is intentionally convoluted to illustrate potential bugs.
        var anchorEndLabel1 = new Label
        {
            Text = "This Label should be the 3rd to last line (AnchorEnd (3)).",
            Justification = Justification.Centered,
            ColorScheme = Colors.ColorSchemes ["Menu"],
            Width = Dim.Fill (5),
            X = 5,
            Y = Pos.AnchorEnd (3)
        };
        app.Add (anchorEndLabel1);

        // Demonstrate DimCombine (via AnchorEnd(n) - 1)
        // This is intentionally convoluted to illustrate potential bugs.
        var anchorEndLabel2 = new TextField
        {
            Text =
                "This TextField should be the 4th to last line (AnchorEnd (3) - 1).",
            Justification = Justification.Left,
            ColorScheme = Colors.ColorSchemes ["Menu"],
            Width = Dim.Fill (5),
            X = 5,
            Y = Pos.AnchorEnd (3) - 1 // Pos.Combine
        };
        app.Add (anchorEndLabel2);

        var leftButton = new Button
        {
            Text = "Left", Y = Pos.AnchorEnd () - 1
        };

        leftButton.Accept += (s, e) =>
                              {
                                  // This demonstrates how to have a dynamically sized button
                                  // Each time the button is clicked the button's text gets longer
                                  // The call to app.LayoutSubviews causes the Computed layout to
                                  // get updated. 
                                  leftButton.Text += "!";
                                  app.LayoutSubviews ();
                              };

        // show positioning vertically using Pos.AnchorEnd
        var centerButton = new Button
        {
            Text = "Center", Y = Pos.AnchorEnd (2) // Pos.AnchorEnd(1)
        };

        centerButton.Accept += (s, e) =>
                                {
                                    // This demonstrates how to have a dynamically sized button
                                    // Each time the button is clicked the button's text gets longer
                                    // The call to app.LayoutSubviews causes the Computed layout to
                                    // get updated. 
                                    centerButton.Text += "!";
                                    app.LayoutSubviews ();
                                };

        // show positioning vertically using another window and Pos.Bottom
        var rightButton = new Button { Text = "Right", Y = Pos.Y (centerButton) };

        rightButton.Accept += (s, e) =>
                               {
                                   // This demonstrates how to have a dynamically sized button
                                   // Each time the button is clicked the button's text gets longer
                                   // The call to app.LayoutSubviews causes the Computed layout to
                                   // get updated. 
                                   rightButton.Text += "!";
                                   app.LayoutSubviews ();
                               };

        View [] buttons = { leftButton, centerButton, rightButton };
        app.Add (leftButton);
        app.Add (centerButton);
        app.Add (rightButton);


        // Center three buttons with 
        leftButton.X = Pos.Justify (Justification.Centered);
        centerButton.X = Pos.Justify (Justification.Centered);
        rightButton.X = Pos.Justify (Justification.Centered);

        Application.Run (app);
        app.Dispose ();
    }
}
