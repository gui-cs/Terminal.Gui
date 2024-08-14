#nullable enable
namespace Terminal.Gui;

#pragma warning disable CS1711
/// <summary>
///     <see cref="EventArgs"/> for events that convey changes to a property of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value that was part of the change being canceled.</typeparam>
public class EventArgs<T> : EventArgs where T : notnull
{
    /// <summary>Initializes a new instance of the <see cref="EventArgs{T}"/> class.</summary>
    /// <param name="currentValue">The current value of the property.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    public EventArgs (in T currentValue) { CurrentValue = currentValue; }

    /// <summary>The current value of the property.</summary>
    public T CurrentValue { get; }
}
