﻿using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Line View", "Demonstrates drawing lines using the LineView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("LineView")]
[ScenarioCategory ("Borders")]
public class LineViewExample : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        // Setup - Create a top-level application window and configure it.
        Toplevel top = new ();

        var menu = new MenuBar
        {
            Menus =
            [
                new ("_File", new MenuItem [] { new ("_Quit", "", () => Quit ()) })
            ]
        };
        top.Add (menu);

        var appWindow = new Window ();
        appWindow.Add (new Label { Y = 1, Text = "Regular Line" });

        // creates a horizontal line
        var line = new LineView { Y = 2 };

        appWindow.Add (line);

        appWindow.Add (new Label { Y = 3, Text = "Double Width Line" });

        // creates a horizontal line
        var doubleLine = new LineView { Y = 4, LineRune = (Rune)'\u2550' };

        appWindow.Add (doubleLine);

        appWindow.Add (new Label { Y = 5, Text = "Short Line" });

        // creates a horizontal line
        var shortLine = new LineView { Y = 5, Width = 10 };

        appWindow.Add (shortLine);

        appWindow.Add (new Label { Y = 7, Text = "Arrow Line" });

        // creates a horizontal line
        var arrowLine = new LineView
        {
            Y = 8, Width = 10, StartingAnchor = CM.Glyphs.LeftTee, EndingAnchor = (Rune)'>'
        };

        appWindow.Add (arrowLine);

        appWindow.Add (new Label { Y = 10, X = 11, Text = "Vertical Line" });

        // creates a horizontal line
        var verticalLine = new LineView (Orientation.Vertical) { X = 25 };

        appWindow.Add (verticalLine);

        appWindow.Add (new Label { Y = 12, X = 28, Text = "Vertical Arrow" });

        // creates a horizontal line
        var verticalArrow = new LineView (Orientation.Vertical)
        {
            X = 27, StartingAnchor = CM.Glyphs.TopTee, EndingAnchor = (Rune)'V'
        };

        appWindow.Add (verticalArrow);

        var statusBar = new StatusBar (
                                       new Shortcut []
                                       {
                                           new (Application.QuitKey, "Quit", Quit)
                                       }
                                      );
        top.Add (statusBar);
        top.Add (appWindow);

        // Run - Start the application.
        Application.Run (top);
        top.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void Quit () { Application.RequestStop (); }
}
