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
    private TabView tabView;

    public override void Main ()
    {
        Application.Init ();
        var w = new Window
        {
            Width = Dim.Fill(),
            Height = Dim.Fill (),
        };

        w.Loaded += (s, e) =>
        {
            SetupGradientLineCanvas (w, w.Frame.Size);
            // TODO: Does not work
            //  SetupGradientLineCanvas (tabView, tabView.Frame.Size);
        };
        w.SizeChanging += (s,e)=>
        {
            SetupGradientLineCanvas (w, e.Size);
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
        var t2 = new Tab ()
        {
            View = new BallsView ()
            {
                Width = Dim.Fill (),
                Height = Dim.Fill (),
            },
            DisplayText = "Ball"
        };

        tabView.AddTab (t1,false);
        tabView.AddTab (t2,false);

        w.Add (tabView);

        Application.Run (w);
        w.Dispose ();

        Application.Shutdown ();
        this.Dispose ();
    }


    private void SetupGradientLineCanvas (View w, Size? size)
    {
        GetAppealingGradientColors (out var stops, out var steps);

        var g = new Gradient (stops, steps);

        var fore = new GradientFill (
            new Rectangle (0, 0, size.Value.Width, size.Value.Height), g, Gradient.Direction.Diagonal);
        var back = new SolidFill (new Terminal.Gui.Color (ColorName.Black));

        w.LineCanvas.Fill = new FillPair (
            fore,
            back);
    }

    private void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        // Define the colors of the gradient stops with more appealing colors
        stops = new List<Color>
        {
            Color.FromRgb(0, 128, 255),    // Bright Blue
            Color.FromRgb(0, 255, 128),    // Bright Green
            Color.FromRgb(255, 255, 0),    // Bright Yellow
            Color.FromRgb(255, 128, 0),    // Bright Orange
            Color.FromRgb(255, 0, 128)     // Bright Pink
        };

        // Define the number of steps between each color for smoother transitions
        steps = new List<int> { 15, 15, 15, 15 }; // 15 steps between each color
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
            Color.FromRgb(255, 0, 0),    // Red
            Color.FromRgb(0, 255, 0),    // Green
            Color.FromRgb(238, 130, 238)  // Violet
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
                var coord = new Coord (col, row);
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
}

internal class BallsView : View
{
    private Ball? _ball;
    private bool _resized;

    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);
        _resized = true;
    }

    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        if ((_ball == null && viewport.Width > 0 && viewport.Height > 0) || _resized)
        {
            _ball = new Ball (this);
            _ball.Start ();
            _resized = false;
        }

        _ball?.Draw ();
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
                int y = (int)((height - 1) / 2 * (1 + Math.Sin (frequency * x) * 0.8)); // Decrease amplitude
                BouncingScene.AddFrame ("O", 1);
            }

            for (int x = width - 1; x >= 0; x--)
            {
                int y = (int)((height - 1) / 2 * (1 + Math.Sin (frequency * x) * 0.8)); // Decrease amplitude
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
                int y = (int)((height - 1) / 2 * (1 + Math.Sin (frequency * x) * 0.8)); // Decrease amplitude
                path.AddWaypoint (new Waypoint ($"waypoint_{x}", new Coord (x, y)));
            }

            for (int x = width - 1; x >= 0; x--)
            {
                int y = (int)((height - 1) / 2 * (1 + Math.Sin (frequency * x) * 0.8)); // Decrease amplitude
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
        Application.Driver.SetAttribute (Viewport.ColorScheme.Normal);
        Viewport.AddRune (Character.Motion.CurrentCoord.Column, Character.Motion.CurrentCoord.Row, new Rune ('O'));
    }
}

