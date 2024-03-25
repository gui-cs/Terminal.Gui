using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Windows & FrameViews", "Stress Tests Windows, sub-Windows, and FrameViews.")]
[ScenarioCategory ("Layout")]
public class WindowsAndFrameViews : Scenario
{
    public override void Setup ()
    {
        static int About ()
        {
            return MessageBox.Query (
                                     "About UI Catalog",
                                     "UI Catalog is a comprehensive sample library for Terminal.Gui",
                                     "Ok"
                                    );
        }

        var margin = 2;
        var padding = 1;
        var contentHeight = 7;

        // list of Windows we create
        List<View> listWin = new ();

        //Ignore the Win that UI Catalog created and create a new one
        Top.Remove (Win);
        Win?.Dispose ();

        Win = new Window
        {
            Title = $"{listWin.Count} - Scenario: {GetName ()}",
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Fill (15),
            Height = 10,
            ColorScheme = Colors.ColorSchemes ["Dialog"]
        };
        Win.Padding.Thickness = new Thickness (padding);
        Win.Margin.Thickness = new Thickness (margin);

        var paddingButton = new Button
        {
            X = Pos.Center (),
            Y = 0,
            ColorScheme = Colors.ColorSchemes ["Error"],
            Text = $"Padding of container is {padding}"
        };
        paddingButton.Accept += (s, e) => About ();
        Win.Add (paddingButton);

        Win.Add (
                 new Button
                 {
                     X = Pos.Center (),
                     Y = Pos.AnchorEnd (1),
                     ColorScheme = Colors.ColorSchemes ["Error"],
                     Text = "Press ME! (Y = Pos.AnchorEnd(1))"
                 }
                );
        Top.Add (Win);

        // add it to our list
        listWin.Add (Win);

        // create 3 more Windows in a loop, adding them Application.Top
        // Each with a
        //	button
        //  sub Window with
        //		TextField
        //	sub FrameView with
        // 
        for (var pad = 0; pad < 3; pad++)
        {
            Window win = null;

            win = new Window
            {
                Title = $"{listWin.Count} - Window Loop - padding = {pad}",
                X = margin,
                Y = Pos.Bottom (listWin.Last ()) + margin,
                Width = Dim.Fill (margin),
                Height = contentHeight + pad * 2 + 2
            };
            win.Padding.Thickness = new Thickness (pad);

            win.ColorScheme = Colors.ColorSchemes ["Dialog"];

            var pressMeButton = new Button
            {
                X = Pos.Center (), Y = 0, ColorScheme = Colors.ColorSchemes ["Error"], Text = "Press me! (Y = 0)"
            };

            pressMeButton.Accept += (s, e) =>
                                         MessageBox.ErrorQuery (win.Title, "Neat?", "Yes", "No");
            win.Add (pressMeButton);

            var subWin = new Window
            {
                Title = "Sub Window",
                X = Pos.Percent (0),
                Y = 1,
                Width = Dim.Percent (50),
                Height = 5,
                ColorScheme = Colors.ColorSchemes ["Base"],
                Text = "The Text in the Window"
            };

            subWin.Add (
                        new TextField { Y = 1, ColorScheme = Colors.ColorSchemes ["Error"], Text = "Edit me! " + win.Title }
                       );
            win.Add (subWin);

            var frameView = new FrameView
            {
                X = Pos.Percent (50),
                Y = 1,
                Width = Dim.Percent (100, true), // Or Dim.Percent (50)
                Height = 5,
                ColorScheme = Colors.ColorSchemes ["Base"],
                Text = "The Text in the FrameView",
                Title = "This is a Sub-FrameView"
            };

            frameView.Add (
                           new TextField { Y = 1, Text = "Edit Me!" }
                          );
            win.Add (frameView);

            Top.Add (win);
            listWin.Add (win);
        }

        // Add a FrameView (frame) to Application.Top
        // Position it at Bottom, using the list of Windows we created above.
        // Fill it with
        //   a label
        //   a SubWindow containing (subWinofFV)
        //      a TextField
        //	    two checkboxes
        //   a Sub FrameView containing (subFrameViewofFV)
        //      a TextField
        //      two CheckBoxes	
        //   a checkbox
        //   a checkbox
        FrameView frame = null;

        frame = new FrameView
        {
            X = margin,
            Y = Pos.Bottom (listWin.Last ()) + margin / 2,
            Width = Dim.Fill (margin),
            Height = contentHeight + 2, // 2 for default padding
            Title = "This is a FrameView"
        };
        frame.ColorScheme = Colors.ColorSchemes ["Dialog"];

        frame.Add (
                   new Label
                   {
                       X = Pos.Center (), Y = 0, ColorScheme = Colors.ColorSchemes ["Error"], Text = "This is a Label! (Y = 0)"
                   }
                  );

        var subWinofFV = new Window
        {
            Title = "This is a Sub-Window",
            X = Pos.Percent (0),
            Y = 1,
            Width = Dim.Percent (50),
            Height = Dim.Fill () - 1,
            ColorScheme = Colors.ColorSchemes ["Base"],
            Text = "The Text in the Window"
        };

        subWinofFV.Add (
                        new TextField { ColorScheme = Colors.ColorSchemes ["Error"], Text = "Edit Me" }
                       );

        subWinofFV.Add (new CheckBox { Y = 1, Text = "Check me" });
        subWinofFV.Add (new CheckBox { Y = 2, Text = "Or, Check me" });

        frame.Add (subWinofFV);

        var subFrameViewofFV = new FrameView
        {
            X = Pos.Percent (50),
            Y = 1,
            Width = Dim.Percent (100),
            Height = Dim.Fill () - 1,
            ColorScheme = Colors.ColorSchemes ["Base"],
            Text = "The Text in the FrameView",
            Title = "this is a Sub-FrameView"
        };
        subFrameViewofFV.Add (new TextField { Width = 15, Text = "Edit Me" });

        subFrameViewofFV.Add (new CheckBox { Y = 1, Text = "Check me" });

        // BUGBUG: This checkbox is not shown even though frameViewFV has 3 rows in 
        // its client area. #522
        subFrameViewofFV.Add (new CheckBox { Y = 2, Text = "Or, Check me" });

        frame.Add (
                   new CheckBox { X = 0, Y = Pos.AnchorEnd (1), Text = "Btn1 (Y = Pos.AnchorEnd (1))" }
                  );
        var c = new CheckBox { Y = Pos.AnchorEnd (1), Text = "Btn2 (Y = Pos.AnchorEnd (1))" };
        c.X = Pos.AnchorEnd () - (Pos.Right (c) - Pos.Left (c));
        frame.Add (c);

        frame.Add (subFrameViewofFV);

        Top.Add (frame);
        listWin.Add (frame);

        Top.ColorScheme = Colors.ColorSchemes ["Base"];
    }
}
