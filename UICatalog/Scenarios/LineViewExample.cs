using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Line View", "Demonstrates drawing lines using the LineView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("LineView")]
[ScenarioCategory ("Borders")]
public class LineViewExample : Scenario
{
    public override void Setup ()
    {
        Win.Title = GetName ();
        Win.Y = 1; // menu
        Win.Height = Dim.Fill (1); // status bar

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("_File", new MenuItem [] { new ("_Quit", "", () => Quit ()) })
            ]
        };
        Top.Add (menu);

        Win.Add (new Label { Y = 0, Text = "Regular Line" });

        // creates a horizontal line
        var line = new LineView { Y = 1 };

        Win.Add (line);

        Win.Add (new Label { Y = 2, Text = "Double Width Line" });

        // creates a horizontal line
        var doubleLine = new LineView { Y = 3, LineRune = (Rune)'\u2550' };

        Win.Add (doubleLine);

        Win.Add (new Label { Y = 4, Text = "Short Line" });

        // creates a horizontal line
        var shortLine = new LineView { Y = 5, Width = 10 };

        Win.Add (shortLine);

        Win.Add (new Label { Y = 6, Text = "Arrow Line" });

        // creates a horizontal line
        var arrowLine = new LineView
        {
            Y = 7, Width = 10, StartingAnchor = CM.Glyphs.LeftTee, EndingAnchor = (Rune)'>'
        };

        Win.Add (arrowLine);

        Win.Add (new Label { Y = 9, X = 11, Text = "Vertical Line" });

        // creates a horizontal line
        var verticalLine = new LineView (Orientation.Vertical) { X = 25 };

        Win.Add (verticalLine);

        Win.Add (new Label { Y = 11, X = 28, Text = "Vertical Arrow" });

        // creates a horizontal line
        var verticalArrow = new LineView (Orientation.Vertical)
        {
            X = 27, StartingAnchor = CM.Glyphs.TopTee, EndingAnchor = (Rune)'V'
        };

        Win.Add (verticalArrow);

        var statusBar = new StatusBar (
                                       new StatusItem []
                                       {
                                           new (
                                                Application.QuitKey,
                                                $"{Application.QuitKey} to Quit",
                                                () => Quit ()
                                               )
                                       }
                                      );
        Top.Add (statusBar);
    }

    private void Quit () { Application.RequestStop (); }
}
