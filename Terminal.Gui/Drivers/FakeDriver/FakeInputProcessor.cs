using System.Collections.Concurrent;

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
        foreach (Tuple<char, ConsoleKeyInfo> released in Parser.ProcessInput (Tuple.Create (input.KeyChar, input)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }

    /// <inheritdoc />
    public override void EnqueueMouseEvent (MouseEventArgs mouseEvent)
    {
        // FakeDriver uses ConsoleKeyInfo as its input record type, which cannot represent mouse events.
        // Use Application.Invoke to defer the event to the next main loop iteration.
        // This ensures proper timing - the event is raised after the current iteration completes
        // and views are properly laid out.
        Application.Invoke (() => RaiseMouseEvent (mouseEvent));
    }
}
