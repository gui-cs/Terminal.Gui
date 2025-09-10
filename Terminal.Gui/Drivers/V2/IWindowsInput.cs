namespace Terminal.Gui.Drivers;

/// <summary>
/// Interface for windows only input which uses low level win32 apis (v2win)
/// </summary>
public interface IWindowsInput : IConsoleInput<WindowsConsole.InputRecord>
{ }
