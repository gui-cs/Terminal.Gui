namespace Terminal.Gui.Drivers;

/// <summary>
///     Wraps IConsoleInput for Unix console input events (char). Needed to support Mocking in tests.
/// </summary>
internal interface IUnixInput : IInput<char>;
