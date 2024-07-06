namespace Terminal.Gui.TextEffects;

public class EffectConfig
{
    public Color ColorSingle { get; set; }
    public List<Color> ColorList { get; set; }
    public Color FinalColor { get; set; }
    public List<Color> FinalGradientStops { get; set; }
    public List<int> FinalGradientSteps { get; set; }
    public int FinalGradientFrames { get; set; }
    public float MovementSpeed { get; set; }
    public EasingFunction Easing { get; set; }
}
