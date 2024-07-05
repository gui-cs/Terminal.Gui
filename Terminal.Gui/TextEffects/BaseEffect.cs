namespace Terminal.Gui.TextEffects;

public abstract class BaseEffectIterator<T> : IEnumerable<string> where T : EffectConfig
{
    protected T Config { get; set; }
    protected Terminal Terminal { get; set; }
    protected List<EffectCharacter> ActiveCharacters { get; set; } = new List<EffectCharacter> ();

    public BaseEffectIterator (BaseEffect<T> effect)
    {
        Config = effect.EffectConfig;
        Terminal = new Terminal (effect.InputData, effect.TerminalConfig);
    }

    public string Frame => Terminal.GetFormattedOutputString ();

    public void Update ()
    {
        foreach (var character in ActiveCharacters)
        {
            character.Tick ();
        }
        ActiveCharacters.RemoveAll (character => !character.IsActive);
    }

    public IEnumerator<string> GetEnumerator ()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator ()
    {
        return GetEnumerator ();
    }

    public abstract string Next ();
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

    public abstract Type IteratorClass { get; }

    public IEnumerator<string> GetEnumerator ()
    {
        var iterator = (BaseEffectIterator<T>)Activator.CreateInstance (IteratorClass, this);
        return iterator;
    }

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
    }
}

