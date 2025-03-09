#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Implementation of <see cref="IHeld"/> for <see cref="AnsiResponseParser{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
internal class GenericHeld<T> : IHeld
{
    private readonly List<Tuple<char, T>> held = new ();

    public void ClearHeld () { held.Clear (); }

    public string HeldToString () { return new (held.Select (h => h.Item1).ToArray ()); }

    public IEnumerable<object> HeldToObjects () { return held; }

    public void AddToHeld (object o) { held.Add ((Tuple<char, T>)o); }

    /// <inheritdoc/>
    public int Length => held.Count;
}
