using Terminal.Gui.TextEffects;

namespace Terminal.Gui.TextEffectsTests;
using Color = Terminal.Gui.TextEffects.Color;

public class AnimationTests
{
    private EffectCharacter character;

    public AnimationTests ()
    {
        character = new EffectCharacter (0, "a", 0, 0);
    }

    [Fact]
    public void TestCharacterVisualInit ()
    {
        var visual = new CharacterVisual (
            symbol: "a",
            bold: true,
            dim: false,
            italic: true,
            underline: false,
            blink: true,
            reverse: false,
            hidden: true,
            strike: false,
            color: new Color ("ffffff"),
            colorCode: "ffffff"
        );
        Assert.Equal ("\x1b[1m\x1b[3m\x1b[5m\x1b[8m\x1b[38;2;255;255;255ma\x1b[0m", visual.FormattedSymbol);
        Assert.True (visual.Bold);
        Assert.False (visual.Dim);
        Assert.True (visual.Italic);
        Assert.False (visual.Underline);
        Assert.True (visual.Blink);
        Assert.False (visual.Reverse);
        Assert.True (visual.Hidden);
        Assert.False (visual.Strike);
        Assert.Equal (new Color ("ffffff"), visual.Color);
        Assert.Equal ("ffffff", visual.ColorCode);
    }

    [Fact]
    public void TestFrameInit ()
    {
        var visual = new CharacterVisual (
            symbol: "a",
            bold: true,
            dim: false,
            italic: true,
            underline: false,
            blink: true,
            reverse: false,
            hidden: true,
            strike: false,
            color: new Color ("ffffff")
        );
        var frame = new Frame (characterVisual: visual, duration: 5);
        Assert.Equal (visual, frame.CharacterVisual);
        Assert.Equal (5, frame.Duration);
        Assert.Equal (0, frame.TicksElapsed);
    }

    [Fact]
    public void TestSceneInit ()
    {
        var scene = new Scene (sceneId: "test_scene", isLooping: true, sync: SyncMetric.Step, ease: Easing.InSine);
        Assert.Equal ("test_scene", scene.SceneId);
        Assert.True (scene.IsLooping);
        Assert.Equal (SyncMetric.Step, scene.Sync);
        Assert.Equal (Easing.InSine, scene.Ease);
    }

    [Fact]
    public void TestSceneAddFrame ()
    {
        var scene = new Scene (sceneId: "test_scene");
        scene.AddFrame (symbol: "a", duration: 5, color: new Color ("ffffff"), bold: true, italic: true, blink: true, hidden: true);
        Assert.Single (scene.Frames);
        var frame = scene.Frames [0];
        Assert.Equal ("\x1b[1m\x1b[3m\x1b[5m\x1b[8m\x1b[38;2;255;255;255ma\x1b[0m", frame.CharacterVisual.FormattedSymbol);
        Assert.Equal (5, frame.Duration);
        Assert.Equal (new Color ("ffffff"), frame.CharacterVisual.Color);
        Assert.True (frame.CharacterVisual.Bold);
    }

    [Fact]
    public void TestSceneAddFrameInvalidDuration ()
    {
        var scene = new Scene (sceneId: "test_scene");
        var exception = Assert.Throws<ArgumentException> (() => scene.AddFrame (symbol: "a", duration: 0, color: new Color ("ffffff")));
        Assert.Equal ("duration must be greater than 0", exception.Message);
    }

    [Fact]
    public void TestSceneApplyGradientToSymbolsEqualColorsAndSymbols ()
    {
        var scene = new Scene (sceneId: "test_scene");
        var gradient = new Gradient (new [] { new Color ("000000"), new Color ("ffffff") }, 
            steps: new [] { 2 });
        var symbols = new List<string> { "a", "b", "c" };
        scene.ApplyGradientToSymbols (gradient, symbols, duration: 1);
        Assert.Equal (3, scene.Frames.Count);
        for (int i = 0; i < scene.Frames.Count; i++)
        {
            Assert.Equal (1, scene.Frames [i].Duration);
            Assert.Equal (gradient.Spectrum [i].RgbColor, scene.Frames [i].CharacterVisual.ColorCode);
        }
    }

    [Fact]
    public void TestSceneApplyGradientToSymbolsUnequalColorsAndSymbols ()
    {
        var scene = new Scene (sceneId: "test_scene");
        var gradient = new Gradient (
            new [] { new Color ("000000"), new Color ("ffffff") },
            steps: new [] { 4 });
        var symbols = new List<string> { "q", "z" };
        scene.ApplyGradientToSymbols (gradient, symbols, duration: 1);
        Assert.Equal (5, scene.Frames.Count);
        Assert.Equal (gradient.Spectrum [0].RgbColor, scene.Frames [0].CharacterVisual.ColorCode);
        Assert.Contains ("q", scene.Frames [0].CharacterVisual.Symbol);
        Assert.Equal (gradient.Spectrum [^1].RgbColor, scene.Frames [^1].CharacterVisual.ColorCode);
        Assert.Contains ("z", scene.Frames [^1].CharacterVisual.Symbol);
    }

    [Fact]
    public void TestAnimationInit ()
    {
        var animation = character.Animation;
        Assert.Equal (character, animation.Character);
        Assert.Empty (animation.Scenes);
        Assert.Null (animation.ActiveScene);
        Assert.False (animation.UseXtermColors);
        Assert.False (animation.NoColor);
        Assert.Empty (animation.XtermColorMap);
        Assert.Equal (0, animation.ActiveSceneCurrentStep);
    }

    [Fact]
    public void TestAnimationNewScene ()
    {
        var animation = character.Animation;
        var scene = animation.NewScene (id:"test_scene", isLooping: true);
        Assert.IsType<Scene> (scene);
        Assert.Equal ("test_scene", scene.SceneId);
        Assert.True (scene.IsLooping);
        Assert.True (animation.Scenes.ContainsKey ("test_scene"));
    }

    [Fact]
    public void TestAnimationNewSceneWithoutId ()
    {
        var animation = character.Animation;
        var scene = animation.NewScene ();
        Assert.IsType<Scene> (scene);
        Assert.Equal ("0", scene.SceneId);
        Assert.True (animation.Scenes.ContainsKey ("0"));
    }

    [Fact]
    public void TestAnimationQueryScene ()
    {
        var animation = character.Animation;
        var scene = animation.NewScene (id:"test_scene", isLooping: true);
        Assert.Equal (scene, animation.QueryScene ("test_scene"));
    }

    [Fact]
    public void TestAnimationLoopingActiveSceneIsComplete ()
    {
        var animation = character.Animation;
        var scene = animation.NewScene (id: "test_scene", isLooping: true);
        scene.AddFrame (symbol: "a", duration: 2);
        animation.ActivateScene (scene);
        Assert.True (animation.ActiveSceneIsComplete ());
    }

    [Fact]
    public void TestAnimationNonLoopingActiveSceneIsComplete ()
    {
        var animation = character.Animation;
        var scene = animation.NewScene (id: "test_scene");
        scene.AddFrame (symbol: "a", duration: 1);
        animation.ActivateScene (scene);
        Assert.False (animation.ActiveSceneIsComplete ());
        animation.StepAnimation ();
        Assert.True (animation.ActiveSceneIsComplete ());
    }
}