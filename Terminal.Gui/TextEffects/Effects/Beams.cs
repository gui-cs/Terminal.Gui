/*namespace Terminal.Gui.TextEffects.Effects;

public class BeamsConfig : EffectConfig
{
    public string [] BeamRowSymbols { get; set; } = { "▂", "▁", "_" };
    public string [] BeamColumnSymbols { get; set; } = { "▌", "▍", "▎", "▏" };
    public int BeamDelay { get; set; } = 10;
    public (int, int) BeamRowSpeedRange { get; set; } = (10, 40);
    public (int, int) BeamColumnSpeedRange { get; set; } = (6, 10);
    public Color [] BeamGradientStops { get; set; } = { new Color ("ffffff"), new Color ("00D1FF"), new Color ("8A008A") };
    public int [] BeamGradientSteps { get; set; } = { 2, 8 };
    public int BeamGradientFrames { get; set; } = 2;
    public Color [] FinalGradientStops { get; set; } = { new Color ("8A008A"), new Color ("00D1FF"), new Color ("ffffff") };
    public int [] FinalGradientSteps { get; set; } = { 12 };
    public int FinalGradientFrames { get; set; } = 5;
    public GradientDirection FinalGradientDirection { get; set; } = GradientDirection.Vertical;
    public int FinalWipeSpeed { get; set; } = 1;
}

public class Beams : BaseEffect<BeamsConfig>
{
    public Beams (string inputData) : base (inputData)
    {
    }

    protected override BaseEffectIterator<BeamsConfig> CreateIterator ()
    {
        return new BeamsIterator (this);
    }
}


public class BeamsIterator : BaseEffectIterator<BeamsConfig>
{
    private class Group
    {
        public List<EffectCharacter> Characters { get; private set; }
        public string Direction { get; private set; }
        private Terminal Terminal;
        private BeamsConfig Config;
        private double Speed;
        private float NextCharacterCounter;
        private List<EffectCharacter> SortedCharacters;

        public Group (List<EffectCharacter> characters, string direction, Terminal terminal, BeamsConfig config)
        {
            Characters = characters;
            Direction = direction;
            Terminal = terminal;
            Config = config;
            Speed = new Random ().Next (config.BeamRowSpeedRange.Item1, config.BeamRowSpeedRange.Item2) * 0.1;
            NextCharacterCounter = 0;
            SortedCharacters = direction == "row"
                ? characters.OrderBy (c => c.InputCoord.Column).ToList ()
                : characters.OrderBy (c => c.InputCoord.Row).ToList ();

            if (new Random ().Next (0, 2) == 0)
            {
                SortedCharacters.Reverse ();
            }
        }

        public void IncrementNextCharacterCounter ()
        {
            NextCharacterCounter += (float)Speed;
        }

        public EffectCharacter GetNextCharacter ()
        {
            NextCharacterCounter -= 1;
            var nextCharacter = SortedCharacters.First ();
            SortedCharacters.RemoveAt (0);
            if (nextCharacter.Animation.ActiveScene != null)
            {
                nextCharacter.Animation.ActiveScene.ResetScene ();
                return null;
            }

            Terminal.SetCharacterVisibility (nextCharacter, true);
            nextCharacter.Animation.ActivateScene (nextCharacter.Animation.QueryScene ("beam_" + Direction));
            return nextCharacter;
        }

        public bool Complete ()
        {
            return !SortedCharacters.Any ();
        }
    }

    private List<Group> PendingGroups = new List<Group> ();
    private Dictionary<EffectCharacter, Color> CharacterFinalColorMap = new Dictionary<EffectCharacter, Color> ();
    private List<Group> ActiveGroups = new List<Group> ();
    private int Delay = 0;
    private string Phase = "beams";
    private List<List<EffectCharacter>> FinalWipeGroups;

    public BeamsIterator (Beams effect) : base (effect)
    {
        Build ();
    }

    private void Build ()
    {
        var finalGradient = new Gradient (Effect.Config.FinalGradientStops, Effect.Config.FinalGradientSteps);
        var finalGradientMapping = finalGradient.BuildCoordinateColorMapping (
            Effect.Terminal.Canvas.Top,
            Effect.Terminal.Canvas.Right,
            Effect.Config.FinalGradientDirection
        );

        foreach (var character in Effect.Terminal.GetCharacters (fillChars: true))
        {
            CharacterFinalColorMap [character] = finalGradientMapping [character.InputCoord];
        }

        var beamGradient = new Gradient (Effect.Config.BeamGradientStops, Effect.Config.BeamGradientSteps);
        var groups = new List<Group> ();

        foreach (var row in Effect.Terminal.GetCharactersGrouped (Terminal.CharacterGroup.RowTopToBottom, fillChars: true))
        {
            groups.Add (new Group (row, "row", Effect.Terminal, Effect.Config));
        }

        foreach (var column in Effect.Terminal.GetCharactersGrouped (Terminal.CharacterGroup.ColumnLeftToRight, fillChars: true))
        {
            groups.Add (new Group (column, "column", Effect.Terminal, Effect.Config));
        }

        foreach (var group in groups)
        {
            foreach (var character in group.Characters)
            {
                var beamRowScene = character.Animation.NewScene (id: "beam_row");
                var beamColumnScene = character.Animation.NewScene (id: "beam_column");
                beamRowScene.ApplyGradientToSymbols (
                    beamGradient, Effect.Config.BeamRowSymbols, Effect.Config.BeamGradientFrames);
                beamColumnScene.ApplyGradientToSymbols (
                    beamGradient, Effect.Config.BeamColumnSymbols, Effect.Config.BeamGradientFrames);

                var fadedColor = character.Animation.AdjustColorBrightness (CharacterFinalColorMap [character], 0.3f);
                var fadeGradient = new Gradient (CharacterFinalColorMap [character], fadedColor, steps: 10);
                beamRowScene.ApplyGradientToSymbols (fadeGradient, character.InputSymbol, 5);
                beamColumnScene.ApplyGradientToSymbols (fadeGradient, character.InputSymbol, 5);

                var brightenGradient = new Gradient (fadedColor, CharacterFinalColorMap [character], steps: 10);
                var brightenScene = character.Animation.NewScene (id: "brighten");
                brightenScene.ApplyGradientToSymbols (
                    brightenGradient, character.InputSymbol, Effect.Config.FinalGradientFrames);
            }
        }

        PendingGroups = groups;
        new Random ().Shuffle (PendingGroups);
    }

    public override bool MoveNext ()
    {
        if (Phase != "complete" || ActiveCharacters.Any ())
        {
            if (Phase == "beams")
            {
                if (Delay == 0)
                {
                    if (PendingGroups.Any ())
                    {
                        for (int i = 0; i < new Random ().Next (1, 6); i++)
                        {
                            if (PendingGroups.Any ())
                            {
                                ActiveGroups.Add (PendingGroups.First ());
                                PendingGroups.RemoveAt (0);
                            }
                        }
                    }
                    Delay = Effect.Config.BeamDelay;
                }
                else
                {
                    Delay--;
                }

                foreach (var group in ActiveGroups)
                {
                    group.IncrementNextCharacterCounter ();
                    if ((int)group.NextCharacterCounter > 1)
                    {
                        for (int i = 0; i < (int)group.NextCharacterCounter; i++)
                        {
                            if (!group.Complete ())
                            {
                                var nextChar = group.GetNextCharacter ();
                                if (nextChar != null)
                                {
                                    ActiveCharacters.Add (nextChar);
                                }
                            }
                        }
                    }
                }

                ActiveGroups = ActiveGroups.Where (g => !g.Complete ()).ToList ();
                if (!PendingGroups.Any () && !ActiveGroups.Any () && !ActiveCharacters.Any ())
                {
                    Phase = "final_wipe";
                }
            }
            else if (Phase == "final_wipe")
            {
                if (FinalWipeGroups.Any ())
                {
                    for (int i = 0; i < Effect.Config.FinalWipeSpeed; i++)
                    {
                        if (!FinalWipeGroups.Any ()) break;

                        var nextGroup = FinalWipeGroups.First ();
                        FinalWipeGroups.RemoveAt (0);

                        foreach (var character in nextGroup)
                        {
                            character.Animation.ActivateScene (character.Animation.QueryScene ("brighten"));
                            Effect.Terminal.SetCharacterVisibility (character, true);
                            ActiveCharacters.Add (character);
                        }
                    }
                }
                else
                {
                    Phase = "complete";
                }
            }

            Update ();
            return true;
        }
        else
        {
            return false;
        }
    }
}
*/