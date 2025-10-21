#nullable enable
using System.Collections.Concurrent;
using Terminal.Gui.App;

namespace Terminal.Gui.Drivers;

/// <summary>
/// Base untyped interface for <see cref="IComponentFactory{T}"/> for methods that are not templated on low level
/// console input type.
/// </summary>
public interface IComponentFactory
{
    /// <summary>
    /// Create the <see cref="IConsoleOutput"/> class for the current driver implementation i.e. the class responsible for
    /// rendering <see cref="IOutputBuffer"/> into the console.
    /// </summary>
    /// <returns></returns>
    IConsoleOutput CreateOutput ();
}

/// <summary>
/// Creates driver specific subcomponent classes (<see cref="IConsoleInput{T}"/>, <see cref="IInputProcessor"/> etc) for a
/// <see cref="IMainLoopCoordinator"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IComponentFactory<T> : IComponentFactory
{
    /// <summary>
    /// Create <see cref="IConsoleInput{T}"/> class for the current driver implementation i.e. the class responsible for reading
    /// user input from the console.
    /// </summary>
    /// <returns></returns>
    IConsoleInput<T> CreateInput ();

    /// <summary>
    /// Creates the <see cref="InputProcessor{T}"/> class for the current driver implementation i.e. the class responsible for
    /// translating raw console input into Terminal.Gui common event <see cref="Key"/> and <see cref="MouseEventArgs"/>.
    /// </summary>
    /// <param name="inputBuffer"></param>
    /// <returns></returns>
    IInputProcessor CreateInputProcessor (ConcurrentQueue<T> inputBuffer);

    /// <summary>
    /// Creates <see cref="IWindowSizeMonitor"/> class for the current driver implementation i.e. the class responsible for
    /// reporting the current size of the terminal window.
    /// </summary>
    /// <param name="consoleOutput"></param>
    /// <param name="outputBuffer"></param>
    /// <returns></returns>
    IWindowSizeMonitor CreateWindowSizeMonitor (IConsoleOutput consoleOutput, IOutputBuffer outputBuffer);
}
