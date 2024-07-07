namespace Terminal.Gui.TextEffects;

public abstract class BaseEffectIterator<T>  where T : EffectConfig, new()
{
    protected T Config { get; set; }
    protected TerminalA Terminal { get; set; }
    protected List<EffectCharacter> ActiveCharacters { get; set; } = new List<EffectCharacter> ();

    protected BaseEffect<T> Effect { get; }



    public BaseEffectIterator (BaseEffect<T> effect)
    {
        Effect = effect;
        Config = effect.EffectConfig;
        Terminal = new TerminalA (effect.InputData, effect.TerminalConfig);

    }

    public void Update ()
    {
        foreach (var character in ActiveCharacters)
        {
            character.Tick ();
        }
        ActiveCharacters.RemoveAll (character => !character.IsActive);
    }

}

public abstract class BaseEffect<T> where T : EffectConfig, new()
{
    public string InputData { get; set; }
    public T EffectConfig { get; set; }
    public TerminalConfig TerminalConfig { get; set; }

    protected BaseEffect (string inputData)
    {
        InputData = inputData;
        EffectConfig = new T ();
        TerminalConfig = new TerminalConfig ();
    }

    /*
    public IDisposable TerminalOutput (string endSymbol = "\n")
    {
        var terminal = new Terminal (InputData, TerminalConfig);
        terminal.PrepCanvas ();
        try
        {
            return terminal;
        }
        finally
        {
            terminal.RestoreCursor (endSymbol);
        }
    }*/
}

