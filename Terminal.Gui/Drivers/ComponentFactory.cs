#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// Abstract base class implementation of <see cref="IComponentFactory{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ComponentFactory<T> : IComponentFactory<T>
{
    /// <inheritdoc />
    public abstract IConsoleInput<T> CreateInput ();

    /// <inheritdoc />
    public abstract IInputProcessor CreateInputProcessor (ConcurrentQueue<T> inputBuffer);

    /// <inheritdoc />
    public virtual IConsoleSizeMonitor CreateConsoleSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return new ConsoleSizeMonitor (consoleOutput);
    }

    /// <inheritdoc />
    public abstract IConsoleOutput CreateOutput ();
}
