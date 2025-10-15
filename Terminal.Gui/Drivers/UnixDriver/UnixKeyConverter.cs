#nullable enable

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IKeyConverter{T}"/> capable of converting the
///     unix native <see cref="char"/> class
///     into Terminal.Gui shared <see cref="Key"/> representation
///     (used by <see cref="View"/> etc).
/// </summary>
internal class UnixKeyConverter : IKeyConverter<char>
{
    /// <inheritdoc />
    public Key ToKey (char value)
    {
        ConsoleKeyInfo adjustedInput = EscSeqUtils.MapChar (value);

        return EscSeqUtils.MapKey (adjustedInput);
    }
}
