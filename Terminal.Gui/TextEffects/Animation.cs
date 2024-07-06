
using static Terminal.Gui.TextEffects.EventHandler;

namespace Terminal.Gui.TextEffects;

public enum SyncMetric
{
    Distance,
    Step
}
public class CharacterVisual
{
    public string Symbol { get; set; }
    public bool Bold { get; set; }
    public bool Dim { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Blink { get; set; }
    public bool Reverse { get; set; }
    public bool Hidden { get; set; }
    public bool Strike { get; set; }
    public Color Color { get; set; }
    public string FormattedSymbol { get; private set; }
    private string _colorCode;  // Holds the ANSI color code or similar string directly

    public string ColorCode => _colorCode;

    public CharacterVisual (string symbol, bool bold = false, bool dim = false, bool italic = false, bool underline = false, bool blink = false, bool reverse = false, bool hidden = false, bool strike = false, Color color = null, string colorCode = null)
    {
        Symbol = symbol;
        Bold = bold;
        Dim = dim;
        Italic = italic;
        Underline = underline;
        Blink = blink;
        Reverse = reverse;
        Hidden = hidden;
        Strike = strike;
        Color = color;
        _colorCode = colorCode;  // Initialize _colorCode from the constructor argument
        FormattedSymbol = FormatSymbol ();
    }

    private string FormatSymbol ()
    {
        string formattingString = "";
        if (Bold) formattingString += Ansitools.ApplyBold ();
        if (Italic) formattingString += Ansitools.ApplyItalic ();
        if (Underline) formattingString += Ansitools.ApplyUnderline ();
        if (Blink) formattingString += Ansitools.ApplyBlink ();
        if (Reverse) formattingString += Ansitools.ApplyReverse ();
        if (Hidden) formattingString += Ansitools.ApplyHidden ();
        if (Strike) formattingString += Ansitools.ApplyStrikethrough ();
        if (_colorCode != null) formattingString += Colorterm.Fg (_colorCode);  // Use the direct color code

        return $"{formattingString}{Symbol}{(formattingString != "" ? Ansitools.ResetAll () : "")}";
    }

    public void DisableModes ()
    {
        Bold = false;
        Dim = false;
        Italic = false;
        Underline = false;
        Blink = false;
        Reverse = false;
        Hidden = false;
        Strike = false;
    }
}


public class Frame
{
    public CharacterVisual CharacterVisual { get; }
    public int Duration { get; }
    public int TicksElapsed { get; set; }

    public Frame (CharacterVisual characterVisual, int duration)
    {
        CharacterVisual = characterVisual;
        Duration = duration;
        TicksElapsed = 0;
    }

    public void IncrementTicks ()
    {
        TicksElapsed++;
    }
}

public class Scene
{
    public string SceneId { get; }
    public bool IsLooping { get; }
    public SyncMetric? Sync { get; }
    public EasingFunction Ease { get; }
    public bool NoColor { get; set; }
    public bool UseXtermColors { get; set; }
    public List<Frame> Frames { get; } = new List<Frame> ();
    public List<Frame> PlayedFrames { get; } = new List<Frame> ();
    public Dictionary<int, Frame> FrameIndexMap { get; } = new Dictionary<int, Frame> ();
    public int EasingTotalSteps { get; set; }
    public int EasingCurrentStep { get; set; }
    public static Dictionary<string, int> XtermColorMap { get; } = new Dictionary<string, int> ();

    public Scene (string sceneId, bool isLooping = false, SyncMetric? sync = null, EasingFunction ease = null, bool noColor = false, bool useXtermColors = false)
    {
        SceneId = sceneId;
        IsLooping = isLooping;
        Sync = sync;
        Ease = ease;
        NoColor = noColor;
        UseXtermColors = useXtermColors;
        EasingTotalSteps = 0;
        EasingCurrentStep = 0;
    }

    public void AddFrame (string symbol, int duration, Color color = null, bool bold = false, bool dim = false, bool italic = false, bool underline = false, bool blink = false, bool reverse = false, bool hidden = false, bool strike = false)
    {
        string charVisColor = null;
        if (color != null)
        {
            if (NoColor)
            {
                charVisColor = null;
            }
            else if (UseXtermColors && color.XtermColor.HasValue)
            {
                charVisColor = color.XtermColor.Value.ToString ();
            }
            else if (color.RgbColor != null && XtermColorMap.ContainsKey (color.RgbColor))
            {
                charVisColor = XtermColorMap [color.RgbColor].ToString ();
            }
            else
            {
                charVisColor = color.RgbColor;
            }
        }

        if (duration < 1)
            throw new ArgumentException ("Duration must be greater than 0.");

        var characterVisual = new CharacterVisual (symbol, bold, dim, italic, underline, blink, reverse, hidden, strike, color, charVisColor);
        var frame = new Frame (characterVisual, duration);
        Frames.Add (frame);
        for (int i = 0; i < frame.Duration; i++)
        {
            FrameIndexMap [EasingTotalSteps] = frame;
            EasingTotalSteps++;
        }
    }

    public CharacterVisual Activate ()
    {
        if (Frames.Count == 0)
            throw new InvalidOperationException ("Scene has no frames.");
        EasingCurrentStep = 0;
        return Frames [0].CharacterVisual;
    }

    public CharacterVisual GetNextVisual ()
    {
        if (Frames.Count == 0)
            return null;

        var frame = Frames [0];
        if (++EasingCurrentStep >= frame.Duration)
        {
            EasingCurrentStep = 0;
            PlayedFrames.Add (frame);
            Frames.RemoveAt (0);
            if (IsLooping && Frames.Count == 0)
            {
                Frames.AddRange (PlayedFrames);
                PlayedFrames.Clear ();
            }
            if (Frames.Count > 0)
                return Frames [0].CharacterVisual;
        }
        return frame.CharacterVisual;
    }

    public void ApplyGradientToSymbols (Gradient gradient, IList<string> symbols, int duration)
    {
        int lastIndex = 0;
        for (int symbolIndex = 0; symbolIndex < symbols.Count; symbolIndex++)
        {
            var symbol = symbols [symbolIndex];
            double symbolProgress = (symbolIndex + 1) / (double)symbols.Count;
            int gradientIndex = (int)(symbolProgress * gradient.Spectrum.Count);
            foreach (var color in gradient.Spectrum.GetRange (lastIndex, Math.Max (gradientIndex - lastIndex, 1)))
            {
                AddFrame (symbol, duration, color);
            }
            lastIndex = gradientIndex;
        }
    }

    public void ResetScene ()
    {
        EasingCurrentStep = 0;
        Frames.Clear ();
        Frames.AddRange (PlayedFrames);
        PlayedFrames.Clear ();
    }

    public override bool Equals (object obj)
    {
        return obj is Scene other && SceneId == other.SceneId;
    }

    public override int GetHashCode ()
    {
        return SceneId.GetHashCode ();
    }
}

public class Animation
{
    public Dictionary<string, Scene> Scenes { get; } = new Dictionary<string, Scene> ();
    public EffectCharacter Character { get; }
    public Scene ActiveScene { get; private set; }
    public bool UseXtermColors { get; set; } = false;
    public bool NoColor { get; set; } = false;
    public Dictionary<string, int> XtermColorMap { get; } = new Dictionary<string, int> ();
    public int ActiveSceneCurrentStep { get; private set; } = 0;
    public CharacterVisual CurrentCharacterVisual { get; private set; }

    public Animation (EffectCharacter character)
    {
        Character = character;
        CurrentCharacterVisual = new CharacterVisual (character.InputSymbol);
    }

    public Scene NewScene (bool isLooping = false, SyncMetric? sync = null, EasingFunction ease = null, string id = "")
    {
        if (string.IsNullOrEmpty (id))
        {
            bool foundUnique = false;
            int currentId = Scenes.Count;
            while (!foundUnique)
            {
                id = $"{Scenes.Count}";
                if (!Scenes.ContainsKey (id))
                {
                    foundUnique = true;
                }
                else
                {
                    currentId++;
                }
            }
        }

        var newScene = new Scene (id, isLooping, sync, ease);
        Scenes [id] = newScene;
        newScene.NoColor = NoColor;
        newScene.UseXtermColors = UseXtermColors;
        return newScene;
    }

    public Scene QueryScene (string sceneId)
    {
        if (!Scenes.TryGetValue (sceneId, out var scene))
        {
            throw new ArgumentException ($"Scene {sceneId} does not exist.");
        }
        return scene;
    }

    public bool ActiveSceneIsComplete ()
    {
        if (ActiveScene == null)
        {
            return true;
        }
        return ActiveScene.Frames.Count == 0 && !ActiveScene.IsLooping;
    }

    public void SetAppearance (string symbol, Color? color = null)
    {
        string charVisColor = null;
        if (color != null)
        {
            if (NoColor)
            {
                charVisColor = null;
            }
            else if (UseXtermColors)
            {
                charVisColor = color.XtermColor.ToString();
            }
            else
            {
                charVisColor = color.RgbColor;
            }
        }
        CurrentCharacterVisual = new CharacterVisual (symbol, color: color, colorCode: charVisColor);
    }

    public static Color RandomColor ()
    {
        var random = new Random ();
        var colorHex = random.Next (0, 0xFFFFFF).ToString ("X6");
        return new Color (colorHex);
    }

    public static Color AdjustColorBrightness (Color color, float brightness)
    {
        float HueToRgb (float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1 / 6f) return p + (q - p) * 6 * t;
            if (t < 1 / 2f) return q;
            if (t < 2 / 3f) return p + (q - p) * (2 / 3f - t) * 6;
            return p;
        }

        float r = int.Parse (color.RgbColor.Substring (0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float g = int.Parse (color.RgbColor.Substring (2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float b = int.Parse (color.RgbColor.Substring (4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;

        float max = Math.Max (r, Math.Max (g, b));
        float min = Math.Min (r, Math.Min (g, b));
        float h, s, l = (max + min) / 2f;

        if (max == min)
        {
            h = s = 0; // achromatic
        }
        else
        {
            float d = max - min;
            s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
            if (max == r)
            {
                h = (g - b) / d + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
                h = (b - r) / d + 2;
            }
            else
            {
                h = (r - g) / d + 4;
            }
            h /= 6;
        }

        l = Math.Max (Math.Min (l * brightness, 1), 0);

        if (s == 0)
        {
            r = g = b = l; // achromatic
        }
        else
        {
            float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
            float p = 2 * l - q;
            r = HueToRgb (p, q, h + 1 / 3f);
            g = HueToRgb (p, q, h);
            b = HueToRgb (p, q, h - 1 / 3f);
        }

        var adjustedColor = $"{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
        return new Color (adjustedColor);
    }

    private float EaseAnimation (EasingFunction easingFunc)
    {
        if (ActiveScene == null)
        {
            return 0;
        }
        float elapsedStepRatio = ActiveScene.EasingCurrentStep / (float)ActiveScene.EasingTotalSteps;
        return easingFunc (elapsedStepRatio);
    }

    public void StepAnimation ()
    {
        if (ActiveScene != null && ActiveScene.Frames.Count > 0)
        {
            if (ActiveScene.Sync != null)
            {
                if (Character.Motion.ActivePath != null)
                {
                    int sequenceIndex = 0;
                    if (ActiveScene.Sync == SyncMetric.Step)
                    {
                        sequenceIndex = (int)Math.Round ((ActiveScene.Frames.Count - 1) *
                            (Math.Max (Character.Motion.ActivePath.CurrentStep, 1) /
                            (float)Math.Max (Character.Motion.ActivePath.MaxSteps, 1)));
                    }
                    else if (ActiveScene.Sync == SyncMetric.Distance)
                    {
                        sequenceIndex = (int)Math.Round ((ActiveScene.Frames.Count - 1) *
                            (Math.Max (Math.Max (Character.Motion.ActivePath.TotalDistance, 1) -
                            Math.Max (Character.Motion.ActivePath.TotalDistance -
                            Character.Motion.ActivePath.LastDistanceReached, 1), 1) /
                            (float)Math.Max (Character.Motion.ActivePath.TotalDistance, 1)));
                    }
                    try
                    {
                        CurrentCharacterVisual = ActiveScene.Frames [sequenceIndex].CharacterVisual;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        CurrentCharacterVisual = ActiveScene.Frames [^1].CharacterVisual;
                    }
                }
                else
                {
                    CurrentCharacterVisual = ActiveScene.Frames [^1].CharacterVisual;
                    ActiveScene.PlayedFrames.AddRange (ActiveScene.Frames);
                    ActiveScene.Frames.Clear ();
                }
            }
            else if (ActiveScene.Ease != null)
            {
                float easingFactor = EaseAnimation (ActiveScene.Ease);
                int frameIndex = (int)Math.Round (easingFactor * Math.Max (ActiveScene.EasingTotalSteps - 1, 0));
                frameIndex = Math.Max (Math.Min (frameIndex, ActiveScene.EasingTotalSteps - 1), 0);
                Frame frame = ActiveScene.FrameIndexMap [frameIndex];
                CurrentCharacterVisual = frame.CharacterVisual;
                ActiveScene.EasingCurrentStep++;
                if (ActiveScene.EasingCurrentStep == ActiveScene.EasingTotalSteps)
                {
                    if (ActiveScene.IsLooping)
                    {
                        ActiveScene.EasingCurrentStep = 0;
                    }
                    else
                    {
                        ActiveScene.PlayedFrames.AddRange (ActiveScene.Frames);
                        ActiveScene.Frames.Clear ();
                    }
                }
            }
            else
            {
                CurrentCharacterVisual = ActiveScene.GetNextVisual ();
            }
            if (ActiveSceneIsComplete ())
            {
                var completedScene = ActiveScene;
                if (!ActiveScene.IsLooping)
                {
                    ActiveScene.ResetScene ();
                    ActiveScene = null;
                }
                Character.EventHandler.HandleEvent (Event.SceneComplete, completedScene);
            }
        }
    }

    public void ActivateScene (Scene scene)
    {
        ActiveScene = scene;
        ActiveSceneCurrentStep = 0;
        CurrentCharacterVisual = ActiveScene.Activate ();
        Character.EventHandler.HandleEvent (Event.SceneActivated, scene);
    }

    public void DeactivateScene (Scene scene)
    {
        if (ActiveScene == scene)
        {
            ActiveScene = null;
        }
    }
}


// Dummy classes for Ansitools, Colorterm, and Hexterm as placeholders
public static class Ansitools
{
    public static string ApplyBold () => "\x1b[1m";
    public static string ApplyItalic () => "\x1b[3m";
    public static string ApplyUnderline () => "\x1b[4m";
    public static string ApplyBlink () => "\x1b[5m";
    public static string ApplyReverse () => "\x1b[7m";
    public static string ApplyHidden () => "\x1b[8m";
    public static string ApplyStrikethrough () => "\x1b[9m";
    public static string ResetAll () => "\x1b[0m";
}

public static class Colorterm
{
    public static string Fg (string colorCode) => $"\x1b[38;5;{colorCode}m";
}

public static class Hexterm
{
    public static string HexToXterm (string hex)
    {
        // Convert hex color to xterm color code (0-255)
        return "15"; // Example output
    }
}
