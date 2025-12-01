using System.Collections.Concurrent;
using Terminal.Gui.Examples;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IComponentFactory{T}"/> implementation for fake/mock console I/O used in unit tests.
///     This factory creates instances that simulate console behavior without requiring a real terminal.
/// </summary>
public class FakeComponentFactory : ComponentFactoryImpl<ConsoleKeyInfo>
{
    private readonly FakeInput? _input;
    private readonly IOutput? _output;
    private readonly ISizeMonitor? _sizeMonitor;

    /// <summary>
    ///     Creates a new FakeComponentFactory with optional output capture.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output">Optional fake output to capture what would be written to console.</param>
    /// <param name="sizeMonitor"></param>
    public FakeComponentFactory (FakeInput? input = null, IOutput? output = null, ISizeMonitor? sizeMonitor = null)
    {
        _input = input;
        _output = output;
        _sizeMonitor = sizeMonitor;
    }


    /// <inheritdoc/>
    public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return _sizeMonitor ?? new SizeMonitorImpl (consoleOutput);
    }

    /// <inheritdoc/>
    public override IInput<ConsoleKeyInfo> CreateInput ()
    {
        // Use provided input instance or create a new one if none was provided
        FakeInput fakeInput = _input ?? new FakeInput ();

        // Check for test context in environment variable
        string? contextJson = Environment.GetEnvironmentVariable (ExampleContext.EnvironmentVariableName);

        if (!string.IsNullOrEmpty (contextJson))
        {
            ExampleContext? context = ExampleContext.FromJson (contextJson);

            if (context is { })
            {
                foreach (string keyStr in context.KeysToInject)
                {
                    if (Input.Key.TryParse (keyStr, out Input.Key? key) && key is { })
                    {
                        ConsoleKeyInfo consoleKeyInfo = ConvertKeyToConsoleKeyInfo (key);
                        fakeInput.AddInput (consoleKeyInfo);
                    }
                }
            }
        }

        return fakeInput;
    }

    private static ConsoleKeyInfo ConvertKeyToConsoleKeyInfo (Input.Key key)
    {
        ConsoleModifiers modifiers = 0;

        if (key.IsShift)
        {
            modifiers |= ConsoleModifiers.Shift;
        }

        if (key.IsAlt)
        {
            modifiers |= ConsoleModifiers.Alt;
        }

        if (key.IsCtrl)
        {
            modifiers |= ConsoleModifiers.Control;
        }

        // Remove the modifier masks to get the base key code
        KeyCode baseKeyCode = key.KeyCode & KeyCode.CharMask;

        // Map KeyCode to ConsoleKey
        ConsoleKey consoleKey = baseKeyCode switch
        {
            KeyCode.A => ConsoleKey.A,
            KeyCode.B => ConsoleKey.B,
            KeyCode.C => ConsoleKey.C,
            KeyCode.D => ConsoleKey.D,
            KeyCode.E => ConsoleKey.E,
            KeyCode.F => ConsoleKey.F,
            KeyCode.G => ConsoleKey.G,
            KeyCode.H => ConsoleKey.H,
            KeyCode.I => ConsoleKey.I,
            KeyCode.J => ConsoleKey.J,
            KeyCode.K => ConsoleKey.K,
            KeyCode.L => ConsoleKey.L,
            KeyCode.M => ConsoleKey.M,
            KeyCode.N => ConsoleKey.N,
            KeyCode.O => ConsoleKey.O,
            KeyCode.P => ConsoleKey.P,
            KeyCode.Q => ConsoleKey.Q,
            KeyCode.R => ConsoleKey.R,
            KeyCode.S => ConsoleKey.S,
            KeyCode.T => ConsoleKey.T,
            KeyCode.U => ConsoleKey.U,
            KeyCode.V => ConsoleKey.V,
            KeyCode.W => ConsoleKey.W,
            KeyCode.X => ConsoleKey.X,
            KeyCode.Y => ConsoleKey.Y,
            KeyCode.Z => ConsoleKey.Z,
            KeyCode.D0 => ConsoleKey.D0,
            KeyCode.D1 => ConsoleKey.D1,
            KeyCode.D2 => ConsoleKey.D2,
            KeyCode.D3 => ConsoleKey.D3,
            KeyCode.D4 => ConsoleKey.D4,
            KeyCode.D5 => ConsoleKey.D5,
            KeyCode.D6 => ConsoleKey.D6,
            KeyCode.D7 => ConsoleKey.D7,
            KeyCode.D8 => ConsoleKey.D8,
            KeyCode.D9 => ConsoleKey.D9,
            KeyCode.Enter => ConsoleKey.Enter,
            KeyCode.Esc => ConsoleKey.Escape,
            KeyCode.Space => ConsoleKey.Spacebar,
            KeyCode.Tab => ConsoleKey.Tab,
            KeyCode.Backspace => ConsoleKey.Backspace,
            KeyCode.Delete => ConsoleKey.Delete,
            KeyCode.Home => ConsoleKey.Home,
            KeyCode.End => ConsoleKey.End,
            KeyCode.PageUp => ConsoleKey.PageUp,
            KeyCode.PageDown => ConsoleKey.PageDown,
            KeyCode.CursorUp => ConsoleKey.UpArrow,
            KeyCode.CursorDown => ConsoleKey.DownArrow,
            KeyCode.CursorLeft => ConsoleKey.LeftArrow,
            KeyCode.CursorRight => ConsoleKey.RightArrow,
            KeyCode.F1 => ConsoleKey.F1,
            KeyCode.F2 => ConsoleKey.F2,
            KeyCode.F3 => ConsoleKey.F3,
            KeyCode.F4 => ConsoleKey.F4,
            KeyCode.F5 => ConsoleKey.F5,
            KeyCode.F6 => ConsoleKey.F6,
            KeyCode.F7 => ConsoleKey.F7,
            KeyCode.F8 => ConsoleKey.F8,
            KeyCode.F9 => ConsoleKey.F9,
            KeyCode.F10 => ConsoleKey.F10,
            KeyCode.F11 => ConsoleKey.F11,
            KeyCode.F12 => ConsoleKey.F12,
            _ => (ConsoleKey)0
        };

        var keyChar = '\0';
        Rune rune = key.AsRune;

        if (Rune.IsValid (rune.Value))
        {
            keyChar = (char)rune.Value;
        }

        return new (keyChar, consoleKey, key.IsShift, key.IsAlt, key.IsCtrl);
    }

    /// <inheritdoc/>
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) { return new FakeInputProcessor (inputBuffer); }

    /// <inheritdoc/>
    public override IOutput CreateOutput ()
    {
        return _output ?? new FakeOutput ();
    }
}
