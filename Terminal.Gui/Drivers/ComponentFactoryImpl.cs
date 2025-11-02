#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// Abstract base class implementation of <see cref="IComponentFactory{T}"/>
/// </summary>
/// <typeparam name="TKeyInfo"></typeparam>
public abstract class ComponentFactoryImpl<TKeyInfo> : IComponentFactory<TKeyInfo>
{
    /// <inheritdoc />
    public abstract IConsoleInput<TKeyInfo> CreateInput ();

    /// <inheritdoc />
    public abstract IInputProcessor CreateInputProcessor (ConcurrentQueue<TKeyInfo> inputBuffer);

    /// <inheritdoc />
    public virtual IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return new ConsoleSizeMonitorImpl (consoleOutput);
    }

    /// <inheritdoc />
    public abstract IConsoleOutput CreateOutput ();
}
