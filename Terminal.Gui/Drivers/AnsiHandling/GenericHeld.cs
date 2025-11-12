#nullable enable
namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IHeld"/> for <see cref="AnsiResponseParser{TInputRecord}"/>
/// </summary>
/// <typeparam name="TInputRecord"></typeparam>
internal class GenericHeld<TInputRecord> : IHeld
{
    private readonly List<Tuple<char, TInputRecord>> held = [];

    public void ClearHeld () { held.Clear (); }

    public string? HeldToString () { return new (held.Select (h => h.Item1).ToArray ()); }

    public IEnumerable<object> HeldToObjects () { return held; }

    public void AddToHeld (object o) { held.Add ((Tuple<char, TInputRecord>)o); }

    /// <inheritdoc/>
    public int Length => held.Count;
}
