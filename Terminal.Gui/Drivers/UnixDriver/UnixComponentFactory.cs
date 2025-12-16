using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IComponentFactory{T}"/> implementation for native unix console I/O.
///     This factory creates instances of internal classes <see cref="UnixInput"/>, <see cref="UnixOutput"/> etc.
/// </summary>
public class UnixComponentFactory : ComponentFactoryImpl<char>
{
    /// <inheritdoc/>
    public override string? GetDriverName () { return DriverRegistry.Names.UNIX; }

    /// <inheritdoc/>
    public override IInput<char> CreateInput () { return new UnixInput (); }

    /// <inheritdoc/>
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer) { return new UnixInputProcessor (inputBuffer); }

    /// <inheritdoc/>
    public override IOutput CreateOutput () { return new UnixOutput (); }
}
