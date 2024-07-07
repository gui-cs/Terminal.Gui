using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Terminal.Gui;

using Terminal.Gui.Drawing;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Effects", "Text Effects.")]
[ScenarioCategory ("Colors")]
public class TextEffectsScenario : Scenario
{
    private TabView tabView;

    public override void Main ()
    {
        Application.Init ();
        var w = new Window
        {
            Width = Dim.Fill(),
            Height = Dim.Fill (),
            Title = "Text Effects Scenario"
        };

        w.Loaded += (s, e) =>
        {
            SetupGradientLineCanvas (w, w.Frame.Size);
            // TODO: Does not work
            //  SetupGradientLineCanvas (tabView, tabView.Frame.Size);
        };
        w.SizeChanging += (s,e)=>
        {
            if(e.Size.HasValue)
            {
                SetupGradientLineCanvas (w, e.Size.Value);
            }
            
            // TODO: Does not work
            //SetupGradientLineCanvas (tabView, tabView.Frame.Size);
        };

        w.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute (ColorName.White, ColorName.Black),
            Focus = new Terminal.Gui.Attribute (ColorName.Black,ColorName.White),
            HotNormal = new Terminal.Gui.Attribute (ColorName.White, ColorName.Black),
            HotFocus = new Terminal.Gui.Attribute (ColorName.White, ColorName.Black),
            Disabled = new Terminal.Gui.Attribute (ColorName.Gray, ColorName.Black)
        };

        // Creates a window that occupies the entire terminal with a title.
        tabView = new TabView ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };

        var t1 = new Tab ()
        {
            View = new GradientsView ()
            {
                Width = Dim.Fill (),
                Height = Dim.Fill (),
            },
            DisplayText = "Gradients"
        };

        tabView.AddTab (t1,false);

        w.Add (tabView);

        Application.Run (w);
        w.Dispose ();

        Application.Shutdown ();
        this.Dispose ();
    }


    private void SetupGradientLineCanvas (View w, Size size)
    {
        GetAppealingGradientColors (out var stops, out var steps);

        var g = new Gradient (stops, steps);

        var fore = new GradientFill (
            new Rectangle (0, 0, size.Width, size.Height), g, Gradient.Direction.Diagonal);
        var back = new SolidFill (new Terminal.Gui.Color (ColorName.Black));

        w.LineCanvas.Fill = new FillPair (
            fore,
            back);
    }

    public static void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        // Define the colors of the gradient stops with more appealing colors
        stops = new List<Color>
        {
            new Color(0, 128, 255),    // Bright Blue
            new Color(0, 255, 128),    // Bright Green
            new Color(255, 255, 0),    // Bright Yellow
            new Color(255, 128, 0),    // Bright Orange
            new Color(255, 0, 128)     // Bright Pink
        };

        // Define the number of steps between each color for smoother transitions
        steps = new List<int> { 15,15, 15, 15 };
    }
}


internal class GradientsView : View
{
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        DrawTopLineGradient (viewport);

        int x = 2;
        int y = 3;

        if (viewport.Height < 25) // Not enough space, render in a single line
        {
            DrawGradientArea (Gradient.Direction.Horizontal, x, y);
            DrawGradientArea (Gradient.Direction.Vertical, x + 32, y);
            DrawGradientArea (Gradient.Direction.Radial, x + 64, y);
            DrawGradientArea (Gradient.Direction.Diagonal, x + 96, y);
        }
        else // Enough space, render in two lines
        {
            DrawGradientArea (Gradient.Direction.Horizontal, x, y);
            DrawGradientArea (Gradient.Direction.Vertical, x + 32, y);
            DrawGradientArea (Gradient.Direction.Radial, x, y + 17);
            DrawGradientArea (Gradient.Direction.Diagonal, x + 32, y + 17);
        }
    }




    private void DrawGradientArea (Gradient.Direction direction, int xOffset, int yOffset)
    {
        // Define the colors of the gradient stops
        var stops = new List<Color>
        {
            new Color(255, 0, 0),    // Red
            new Color(0, 255, 0),    // Green
            new Color(238, 130, 238)  // Violet
        };

        // Define the number of steps between each color
        var steps = new List<int> { 10, 10 }; // 10 steps between Red -> Green, and Green -> Blue

        // Create the gradient
        var radialGradient = new Gradient (stops, steps, loop: false);

        // Define the size of the rectangle
        int maxRow = 15; // Adjusted to keep aspect ratio
        int maxColumn = 30;

        // Build the coordinate-color mapping for a radial gradient
        var gradientMapping = radialGradient.BuildCoordinateColorMapping (maxRow, maxColumn, direction);

        // Print the gradient
        for (int row = 0; row <= maxRow; row++)
        {
            for (int col = 0; col <= maxColumn; col++)
            {
                var coord = new Point (col, row);
                var color = gradientMapping [coord];

                SetColor (color);

                AddRune (col + xOffset, row + yOffset, new Rune ('█'));
            }
        }
    }

    private void DrawTopLineGradient (Rectangle viewport)
    {
        // Define the colors of the rainbow
        var stops = new List<Color>
        {
            new Color(255, 0, 0),     // Red
            new Color(255, 165, 0),   // Orange
            new Color(255, 255, 0),   // Yellow
            new Color(0, 128, 0),     // Green
            new Color(0, 0, 255),     // Blue
            new Color(75, 0, 130),    // Indigo
            new Color(238, 130, 238)  // Violet
        };

        // Define the number of steps between each color
        var steps = new List<int>
        {
            20, // between Red and Orange
            20, // between Orange and Yellow
            20, // between Yellow and Green
            20, // between Green and Blue
            20, // between Blue and Indigo
            20  // between Indigo and Violet
        };

        // Create the gradient
        var rainbowGradient = new Gradient (stops, steps, loop: true);

        for (int x = 0; x < viewport.Width; x++)
        {
            double fraction = (double)x / (viewport.Width - 1);
            Color color = rainbowGradient.GetColorAtFraction (fraction);

            SetColor (color);

            AddRune (x, 0, new Rune ('█'));
        }
    }

    private void SetColor (Color color)
    {
        // Assuming AddRune is a method you have for drawing at specific positions
        Application.Driver.SetAttribute (
            new Attribute (
                new Terminal.Gui.Color (color.R, color.G, color.B),
                new Terminal.Gui.Color (color.R, color.G, color.B)
            )); // Setting color based on RGB
    }
}