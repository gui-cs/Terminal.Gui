namespace Terminal.Gui.TextEffects;

public class EffectCharacter
{
    public int CharacterId { get; }
    public string InputSymbol { get; }
    public Coord InputCoord { get; }
    public bool IsVisible { get; set; }
    public Animation Animation { get; }
    public Motion Motion { get; }
    public EventHandler EventHandler { get; }
    public int Layer { get; set; }
    public bool IsFillCharacter { get; set; }

    public EffectCharacter (int characterId, string symbol, int inputColumn, int inputRow)
    {
        CharacterId = characterId;
        InputSymbol = symbol;
        InputCoord = new Coord (inputColumn, inputRow);
        IsVisible = false;
        Animation = new Animation (this);
        Motion = new Motion (this);
        EventHandler = new EventHandler (this);
        Layer = 0;
        IsFillCharacter = false;
    }

    public bool IsActive => !Animation.ActiveSceneIsComplete() || !Motion.MovementIsComplete ();

    public void Tick ()
    {
        Motion.Move ();
        Animation.StepAnimation ();
    }
}

public class EventHandler
{
    public EffectCharacter Character { get; }
    public Dictionary<(Event, object), List<(Action, object)>> RegisteredEvents { get; }

    public EventHandler (EffectCharacter character)
    {
        Character = character;
        RegisteredEvents = new Dictionary<(Event, object), List<(Action, object)>> ();
    }

    public void RegisterEvent (Event @event, object caller, Action action, object target)
    {
        var key = (@event, caller);
        if (!RegisteredEvents.ContainsKey (key))
            RegisteredEvents [key] = new List<(Action, object)> ();

        RegisteredEvents [key].Add ((action, target));
    }

    public void HandleEvent (Event @event, object caller)
    {
        var key = (@event, caller);
        if (!RegisteredEvents.ContainsKey (key))
            return;

        foreach (var (action, target) in RegisteredEvents [key])
        {
            switch (action)
            {
                case Action.ActivatePath:
                    Character.Motion.ActivatePath (target as Path);
                    break;
                case Action.DeactivatePath:
                    Character.Motion.DeactivatePath (target as Path);
                    break;
                case Action.SetLayer:
                    Character.Layer = (int)target;
                    break;
                case Action.SetCoordinate:
                    Character.Motion.CurrentCoord = (Coord)target;
                    break;
                case Action.Callback:
                    (target as Action)?.Invoke ();
                    break;
                default:
                    throw new ArgumentOutOfRangeException (nameof (action), "Unhandled action.");
            }
        }
    }

    public enum Event
    {
        SegmentEntered,
        SegmentExited,
        PathActivated,
        PathComplete,
        PathHolding,
        SceneActivated,
        SceneComplete
    }

    public enum Action
    {
        ActivatePath,
        DeactivatePath,
        SetLayer,
        SetCoordinate,
        Callback
    }
}
