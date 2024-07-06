using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Terminal.Gui;
using Terminal.Gui.TextEffects;
using static UICatalog.Scenario;

using Color = Terminal.Gui.TextEffects.Color;
using Animation = Terminal.Gui.TextEffects.Animation;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Effects", "Text Effects.")]
[ScenarioCategory ("Colors")]
public class TextEffectsScenario : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var top = Application.Top;

        // Creates a window that occupies the entire terminal with a title.
        var window = new Window ()
        {
            X = 0,
            Y = 1, // Leaves one row for the toplevel menu

            // By using Dim.Fill(), it will automatically resize without manual intervention
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = "Text Effects Scenario"
        };

        // Create a large empty view.
        var emptyView = new TextEffectsExampleView ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };

        window.Add (emptyView);

        // Create a label in the center of the window.
        var label = new Label ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 10,
            Height = 1,
            Text = "Hello"
        };
        window.Add (label);

        Application.Run (window);
        Application.Shutdown ();
    }
}

internal class TextEffectsExampleView : View
{
    Ball? _ball;
    private bool resized;

    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);
        resized = true;
    }

    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        if (
            // First time
            (_ball == null && viewport.Width > 0 && viewport.Height > 0)
            || resized)
        {
            _ball = new Ball (this);
            _ball.Start ();
            resized = false;
        }

        DrawTopLineGradient (viewport);
        DrawRadialGradient (viewport);

        _ball?.Draw ();
    }

    private void DrawRadialGradient (Rectangle viewport)
    {
        // Define the colors of the gradient stops
        var stops = new List<Color>
        {
            Color.FromRgb(255, 0, 0),    // Red
            Color.FromRgb(0, 255, 0),    // Green
            Color.FromRgb(238, 130, 238)  // Violet
        };

        // Define the number of steps between each color
        var steps = new List<int> { 10, 10 }; // 10 steps between Red -> Green, and Green -> Blue

        // Create the gradient
        var radialGradient = new Gradient (stops, steps, loop: false);

        // Define the size of the rectangle
        int maxRow = 20;
        int maxColumn = 40;

        // Build the coordinate-color mapping for a radial gradient
        var gradientMapping = radialGradient.BuildCoordinateColorMapping (maxRow, maxColumn, Gradient.Direction.Radial);

        // Print the gradient
        for (int row = 0; row <= maxRow; row++)
        {
            for (int col = 0; col <= maxColumn; col++)
            {
                var coord = new Coord (col, row);
                var color = gradientMapping [coord];
                
                SetColor (color);

                AddRune (col+2, row+3, new Rune ('█'));
            }
        }
    }

    private void DrawTopLineGradient (Rectangle viewport)
    {

        // Define the colors of the rainbow
        var stops = new List<Color>
        {
            Color.FromRgb(255, 0, 0),     // Red
            Color.FromRgb(255, 165, 0),   // Orange
            Color.FromRgb(255, 255, 0),   // Yellow
            Color.FromRgb(0, 128, 0),     // Green
            Color.FromRgb(0, 0, 255),     // Blue
            Color.FromRgb(75, 0, 130),    // Indigo
            Color.FromRgb(238, 130, 238)  // Violet
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

    public class Ball
    {
        public Animation Animation { get; private set; }
        public Scene BouncingScene { get; private set; }
        public View Viewport { get; private set; }
        public EffectCharacter Character { get; private set; }

        public Ball (View viewport)
        {
            Viewport = viewport;
            Character = new EffectCharacter (1, "O", 0, 0);
            Animation = Character.Animation;
            CreateBouncingScene ();
            CreateMotionPath ();
        }

        private void CreateBouncingScene ()
        {
            BouncingScene = Animation.NewScene (isLooping: true);
            int width = Viewport.Frame.Width;
            int height = Viewport.Frame.Height;
            double frequency = 4 * Math.PI / width; // Double the frequency

            for (int x = 0; x < width; x++)
            {
                int y = (int)((height) / 2 * (1 + Math.Sin (frequency * x))); // Decrease amplitude
                BouncingScene.AddFrame ("O", 1);
            }

            for (int x = width - 1; x >= 0; x--)
            {
                int y = (int)((height) / 2 * (1 + Math.Sin (frequency * x))); // Decrease amplitude
                BouncingScene.AddFrame ("O", 1);
            }
        }

        private void CreateMotionPath ()
        {
            int width = Viewport.Frame.Width;
            int height = Viewport.Frame.Height;
            double frequency = 4 * Math.PI / width; // Double the frequency

            var path = Character.Motion.CreatePath ("sineWavePath", speed: 1, loop: true);

            for (int x = 0; x < width; x++)
            {
                int y = (int)((height) / 2 * (1 + Math.Sin (frequency * x))); // Decrease amplitude
                path.AddWaypoint (new Waypoint ($"waypoint_{x}", new Coord (x, y)));
            }

            for (int x = width - 1; x >= 0; x--)
            {
                int y = (int)((height) / 2 * (1 + Math.Sin (frequency * x))); // Decrease amplitude
                path.AddWaypoint (new Waypoint ($"waypoint_{x}", new Coord (x, y)));
            }

            Character.Motion.ActivatePath (path);
        }

        public void Start ()
        {
            Animation.ActivateScene (BouncingScene);
            new Thread (() =>
            {
                while (true)
                {
                    Thread.Sleep (10); // Adjust the speed of animation
                    Character.Tick ();

                    Application.Invoke (() => Viewport.SetNeedsDisplay ());
                }
            })
            { IsBackground = true }.Start ();
        }

        public void Draw ()
        {
            Driver.SetAttribute (Viewport.ColorScheme.Normal);
            Viewport.AddRune (Character.Motion.CurrentCoord.Column, Character.Motion.CurrentCoord.Row, new Rune ('O'));
        }
    }
}
