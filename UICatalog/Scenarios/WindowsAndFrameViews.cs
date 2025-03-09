using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Windows & FrameViews", "Stress Tests Windows, sub-Windows, and FrameViews.")]
[ScenarioCategory ("Layout")]
public class WindowsAndFrameViews : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

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

        var win = new Window
        {
            Title = $"{listWin.Count} - Scenario: {GetName ()}",
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Fill (15),
            Height = 10,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable
        };
        win.Padding.Thickness = new (padding);
        win.Margin.Thickness = new (margin);

        var paddingButton = new Button
        {
            X = Pos.Center (),
            Y = 0,
            ColorScheme = Colors.ColorSchemes ["Error"],
            Text = $"Padding of container is {padding}"
        };
        paddingButton.Accepting += (s, e) => About ();
        win.Add (paddingButton);

        win.Add (
                 new Button
                 {
                     X = Pos.Center (),
                     Y = Pos.AnchorEnd (),
                     ColorScheme = Colors.ColorSchemes ["Error"],
                     Text = "Press ME! (Y = Pos.AnchorEnd(1))"
                 }
                );
        app.Add (win);

        // add it to our list
        listWin.Add (win);

        // create 3 more Windows in a loop, adding them Application.Top
        // Each with a
        //	button
        //  sub Window with
        //		TextField
        //	sub FrameView with
        // 
        for (var pad = 0; pad < 3; pad++)
        {
            Window loopWin = null;

            loopWin = new()
            {
                Title = $"{listWin.Count} - Window Loop - padding = {pad}",
                X = margin,
                Y = Pos.Bottom (listWin.Last ()) + margin,
                Width = Dim.Fill (margin),
                Height = contentHeight + pad * 2 + 2,
                Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable
            };
            loopWin.Padding.Thickness = new (pad);

            loopWin.ColorScheme = Colors.ColorSchemes ["Dialog"];

            var pressMeButton = new Button
            {
                X = Pos.Center (), Y = 0, ColorScheme = Colors.ColorSchemes ["Error"], Text = "Press me! (Y = 0)",
            };

            pressMeButton.Accepting += (s, e) =>
                                        MessageBox.ErrorQuery (loopWin.Title, "Neat?", "Yes", "No");
            loopWin.Add (pressMeButton);

            var subWin = new Window
            {
                Title = "Sub Window",
                X = Pos.Percent (0),
                Y = 1,
                Width = Dim.Percent (50),
                Height = 5,
                ColorScheme = Colors.ColorSchemes ["Base"],
                Text = "The Text in the Window",
                Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable

            };

            subWin.Add (
                        new TextField { Y = 1, ColorScheme = Colors.ColorSchemes ["Error"], Text = "Edit me! " + loopWin.Title }
                       );
            loopWin.Add (subWin);

            var frameView = new FrameView
            {
                X = Pos.Percent (50),
                Y = 1,
                Width = Dim.Percent (100, DimPercentMode.Position), // Or Dim.Percent (50)
                Height = 5,
                ColorScheme = Colors.ColorSchemes ["Base"],
                Text = "The Text in the FrameView",
                Title = "This is a Sub-FrameView"
            };

            frameView.Add (
                           new TextField { Y = 1, Text = "Edit Me!" }
                          );
            loopWin.Add (frameView);

            app.Add (loopWin);
            listWin.Add (loopWin);
        }

        FrameView frame = null;

        frame = new()
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
            Text = "The Text in the Window",
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable

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

        subFrameViewofFV.Add (new CheckBox { Y = 2, Text = "Or, Check me" });

        frame.Add (
                   new CheckBox { X = 0, Y = Pos.AnchorEnd (), Text = "Btn1 (Y = Pos.AnchorEnd ())" }
                  );
        var c = new CheckBox { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Btn2 (Y = Pos.AnchorEnd ())" };
        frame.Add (c);

        frame.Add (subFrameViewofFV);

        app.Add (frame);
        listWin.Add (frame);

        app.ColorScheme = Colors.ColorSchemes ["Base"];

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
