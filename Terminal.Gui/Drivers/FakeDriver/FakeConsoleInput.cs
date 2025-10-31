#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// Fake console input for testing that can return predefined input or wait indefinitely.
/// </summary>
public class FakeConsoleInput : ConsoleInput<ConsoleKeyInfo>, IFakeConsoleInput
{
    private readonly FakeInput<ConsoleKeyInfo>? _predefinedInput;

    /// <summary>
    /// Creates a new FakeConsoleInput with optional predefined input.
    /// </summary>
    /// <param name="predefinedInput">Optional queue of predefined input to return.</param>
    public FakeConsoleInput (FakeInput<ConsoleKeyInfo>? predefinedInput = null)
    {
        _predefinedInput = predefinedInput;
    }

    /// <inheritdoc/>
    protected override bool Peek ()
    {
        if (_predefinedInput is { InputBuffer.IsEmpty: false })
        {
            return true;
        }

        // No input available
        return false;
    }

    /// <inheritdoc/>
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        if (_predefinedInput is { InputBuffer: { } } && _predefinedInput.InputBuffer.TryDequeue (out ConsoleKeyInfo key))
        {
            yield return key;
        }
    }
}
