﻿#region

using System.Text;
using Terminal.Gui;

#endregion

namespace UICatalog.Scenarios {
    [ScenarioMetadata (Name: "Line View", Description: "Demonstrates drawing lines using the LineView control.")]
    [ScenarioCategory ("Controls"), ScenarioCategory ("LineView"), ScenarioCategory ("Borders")]
    public class LineViewExample : Scenario {
        public override void Setup () {
            Win.Title = this.GetName ();
            Win.Y = 1; // menu
            Win.Height = Dim.Fill (1); // status bar

            var menu = new MenuBar (
                                    new MenuBarItem[] {
                                                          new MenuBarItem (
                                                                           "_File",
                                                                           new MenuItem[] {
                                                                               new MenuItem (
                                                                                "_Quit",
                                                                                "",
                                                                                () => Quit ()),
                                                                           })
                                                      });
            Application.Top.Add (menu);

            Win.Add (new Label ("Regular Line") { Y = 0 });

            // creates a horizontal line
            var line = new LineView () {
                                           Y = 1,
                                       };

            Win.Add (line);

            Win.Add (new Label ("Double Width Line") { Y = 2 });

            // creates a horizontal line
            var doubleLine = new LineView () {
                                                 Y = 3,
                                                 LineRune = (Rune)'\u2550'
                                             };

            Win.Add (doubleLine);

            Win.Add (new Label ("Short Line") { Y = 4 });

            // creates a horizontal line
            var shortLine = new LineView () {
                                                Y = 5,
                                                Width = 10
                                            };

            Win.Add (shortLine);

            Win.Add (new Label ("Arrow Line") { Y = 6 });

            // creates a horizontal line
            var arrowLine = new LineView () {
                                                Y = 7,
                                                Width = 10,
                                                StartingAnchor = CM.Glyphs.LeftTee,
                                                EndingAnchor = (Rune)'>'
                                            };

            Win.Add (arrowLine);

            Win.Add (new Label ("Vertical Line") { Y = 9, X = 11 });

            // creates a horizontal line
            var verticalLine = new LineView (Orientation.Vertical) {
                                                                       X = 25,
                                                                   };

            Win.Add (verticalLine);

            Win.Add (new Label ("Vertical Arrow") { Y = 11, X = 28 });

            // creates a horizontal line
            var verticalArrow = new LineView (Orientation.Vertical) {
                                                                        X = 27,
                                                                        StartingAnchor = CM.Glyphs.TopTee,
                                                                        EndingAnchor = (Rune)'V'
                                                                    };

            Win.Add (verticalArrow);

            var statusBar = new StatusBar (
                                           new StatusItem[] {
                                                                new StatusItem (
                                                                                Application.QuitKey,
                                                                                $"{Application.QuitKey} to Quit",
                                                                                () => Quit ()),
                                                            });
            Application.Top.Add (statusBar);
        }

        private void Quit () { Application.RequestStop (); }
    }
}
