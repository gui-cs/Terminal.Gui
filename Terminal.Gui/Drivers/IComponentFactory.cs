using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Base untyped interface for <see cref="IComponentFactory{T}"/> for methods that are not templated on low level
///     console input type.
/// </summary>
public interface IComponentFactory
{
    /// <summary>
    ///     Create the <see cref="IOutput"/> class for the current driver implementation i.e. the class responsible for
    ///     rendering <see cref="IOutputBuffer"/> into the console.
    /// </summary>
    /// <returns></returns>
    IOutput CreateOutput ();
}

/// <summary>
///     Creates driver specific subcomponent classes (<see cref="IInput{TInputRecord}"/>, <see cref="IInputProcessor"/>
///     etc) for a
///     <see cref="IMainLoopCoordinator"/>.
/// </summary>
/// <typeparam name="TInputRecord">
///     The platform specific console input type. Must be a value type (struct).
///     Valid types are <see cref="ConsoleKeyInfo"/>, <see cref="WindowsConsole.InputRecord"/>, and <see cref="char"/>.
/// </typeparam>
public interface IComponentFactory<TInputRecord> : IComponentFactory
    where TInputRecord : struct
{
    /// <summary>
    ///     Create <see cref="IInput{T}"/> class for the current driver implementation i.e. the class responsible for reading
    ///     user input from the console.
    /// </summary>
    /// <returns></returns>
    IInput<TInputRecord> CreateInput ();

    /// <summary>
    ///     Creates the <see cref="InputProcessorImpl{T}"/> class for the current driver implementation i.e. the class
    ///     responsible for
    ///     translating raw console input into Terminal.Gui common event <see cref="Key"/> and <see cref="MouseEventArgs"/>.
    /// </summary>
    /// <param name="inputQueue">
    ///     The input queue containing raw console input events, populated by <see cref="IInput{TInputRecord}"/>
    ///     implementations on the input thread and
    ///     read by <see cref="IInputProcessor"/> on the main loop thread.
    /// </param>
    /// <returns></returns>
    IInputProcessor CreateInputProcessor (ConcurrentQueue<TInputRecord> inputQueue);

    /// <summary>
    ///     Creates <see cref="ISizeMonitor"/> class for the current driver implementation i.e. the class responsible for
    ///     reporting the current size of the terminal.
    /// </summary>
    /// <param name="consoleOutput"></param>
    /// <param name="outputBuffer"></param>
    /// <returns></returns>
    ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer);
}
