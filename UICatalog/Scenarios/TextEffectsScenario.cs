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
            Height = Dim.Fill () ,
        };

        window.Add (emptyView);

        // Create a label in the center of the window.
        var label = new Label ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 10,
            Height = 1,
            Title = "Hello"            
        };
        window.Add (label);

        Application.Run (window);
        Application.Shutdown ();
    }
}

internal class TextEffectsExampleView : View
{
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

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


        for (int x = 0 ; x < viewport.Width; x++)
        {
            double fraction = (double)x / (viewport.Width - 1);
            Color color = rainbowGradient.GetColorAtFraction (fraction);

            // Assuming AddRune is a method you have for drawing at specific positions
            Application.Driver.SetAttribute (
                
                new Attribute (
                    new Terminal.Gui.Color(color.R, color.G, color.B),
                    new Terminal.Gui.Color (color.R, color.G, color.B)
                    )); // Setting color based on RGB


            AddRune (x, 0, new Rune ('█'));
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
        }

        private void CreateBouncingScene ()
        {
            BouncingScene = Animation.NewScene (isLooping: true);
            int width = Viewport.Frame.Width;
            int height = Viewport.Frame.Height;
            double frequency = 2 * Math.PI / width;

            for (int x = 0; x < width; x++)
            {
                int y = (int)((height - 1) / 2 * (1 + Math.Sin (frequency * x)));
                BouncingScene.AddFrame ("O", 1);
                BouncingScene.Frames [BouncingScene.Frames.Count - 1].CharacterVisual.Position = new Coord (x, y);
            }

            for (int x = width - 1; x >= 0; x--)
            {
                int y = (int)((height - 1) / 2 * (1 + Math.Sin (frequency * x)));
                BouncingScene.AddFrame ("O", 1);
                BouncingScene.Frames [BouncingScene.Frames.Count - 1].CharacterVisual.Position = new Coord (x, y);
            }
        }

        public void Start ()
        {
            Animation.ActivateScene (BouncingScene);
            new Thread (() =>
            {
                while (true)
                {
                    Draw ();
                    Thread.Sleep (100); // Adjust the speed of animation
                    Animation.StepAnimation ();
                }
            })
            { IsBackground = true }.Start ();
        }

        private void Draw ()
        {
            var characterVisual = Animation.CurrentCharacterVisual;
            var coord = characterVisual.Position;
            Application.MainLoop.Invoke (() =>
            {
                Viewport.Clear ();
                Viewport.AddRune (coord.X, coord.Y, new Rune ('O'));
                Application.Refresh ();
            });
        }
    }
}
