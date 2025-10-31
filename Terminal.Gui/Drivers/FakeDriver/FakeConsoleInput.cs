#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implements a fake console input for testing purposes. It will return predefined input if provided.
/// </summary>
public class FakeConsoleInput : ConsoleInputImpl<ConsoleKeyInfo>
{
    private readonly ConcurrentQueue<ConsoleKeyInfo>? _predefinedInput;

    /// <summary>
    /// Creates a new FakeConsoleInput with optional predefined input.
    /// </summary>
    /// <param name="predefinedInput">Optional queue of predefined input to return.</param>
    public FakeConsoleInput (ConcurrentQueue<ConsoleKeyInfo>? predefinedInput = null)
    {
        _predefinedInput = predefinedInput;
    }

    /// <inheritdoc/>
    protected override bool Peek ()
    {
        if (_predefinedInput is { IsEmpty: false })
        {
            return true;
        }

        // No input available
        return false;
    }

    /// <inheritdoc/>
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        if (_predefinedInput is { } && _predefinedInput.TryDequeue (out ConsoleKeyInfo key))
        {
            yield return key;
        }
    }
}
