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

public class NamedEffectIterator : BaseEffectIterator<EffectConfig>
{
    public NamedEffectIterator (NamedEffect effect) : base (effect)
    {
        Build ();
    }

    private void Build ()
    {
        var finalGradient = new Gradient (Config.FinalGradientStops, Config.FinalGradientSteps);
        foreach (var character in Terminal.GetCharacters ())
        {
            CharacterFinalColorMap [character] = finalGradient.GetColorAtFraction (
                character.InputCoord.Row / (float)Terminal.Canvas.Top
            );
        }
    }

    public override string Next ()
    {
        if (PendingChars.Any () || ActiveCharacters.Any ())
        {
            Update ();
            return Frame;
        }
        else
        {
            throw new InvalidOperationException ("No more elements in effect iterator.");
        }
    }

    private List<EffectCharacter> PendingChars = new List<EffectCharacter> ();
    private Dictionary<EffectCharacter, Color> CharacterFinalColorMap = new Dictionary<EffectCharacter, Color> ();
}

public class NamedEffect : BaseEffect<EffectConfig>
{
    public NamedEffect (string inputData) : base (inputData)
    {
    }

    protected override Type ConfigCls => typeof (EffectConfig);
    protected override Type IteratorCls => typeof (NamedEffectIterator);
}
