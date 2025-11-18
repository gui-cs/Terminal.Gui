#nullable disable
﻿using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="FakeInput"/>, deals in <see cref="ConsoleKeyInfo"/> stream
/// </summary>
public class FakeInputProcessor : InputProcessorImpl<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    public FakeInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) : base (inputBuffer, new NetKeyConverter ())
    {
        DriverName = "fake";
    }

    /// <inheritdoc/>
    protected override void Process (ConsoleKeyInfo input)
    {
        Logging.Trace ($"input: {input.KeyChar}");

        foreach (Tuple<char, ConsoleKeyInfo> released in Parser.ProcessInput (Tuple.Create (input.KeyChar, input)))
        {
            Logging.Trace($"released: {released.Item1}");
            ProcessAfterParsing (released.Item2);
        }
    }

    /// <inheritdoc />
    public override void EnqueueMouseEvent (MouseEventArgs mouseEvent)
    {
        // FakeDriver uses ConsoleKeyInfo as its input record type, which cannot represent mouse events.

        // If Application.Invoke is available (running in Application context), defer to next iteration
        // to ensure proper timing - the event is raised after views are laid out.
        // Otherwise (unit tests), raise immediately so tests can verify synchronously.
        if (Application.MainThreadId is { })
        {
            // Application is running - use Invoke to defer to next iteration
            ApplicationImpl.Instance.Invoke ((_) => RaiseMouseEvent (mouseEvent));
        }
        else
        {
            // Not in Application context (unit tests) - raise immediately
            RaiseMouseEvent (mouseEvent);
        }
    }
}
