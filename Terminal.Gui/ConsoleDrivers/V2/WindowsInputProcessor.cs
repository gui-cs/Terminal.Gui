#nullable enable
using System.Collections.Concurrent;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;

using InputRecord = InputRecord;

/// <summary>
///     Input processor for <see cref="WindowsInput"/>, deals in <see cref="WindowsConsole.InputRecord"/> stream.
/// </summary>
internal class WindowsInputProcessor : InputProcessor<InputRecord>
{
    private readonly bool [] _lastWasPressed = new bool[4];

    /// <inheritdoc/>
    public WindowsInputProcessor (ConcurrentQueue<InputRecord> inputBuffer) : base (inputBuffer, new WindowsKeyConverter ()) { }

    /// <inheritdoc/>
    protected override void Process (InputRecord inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case EventType.Key:

                // TODO: For now ignore keyup because ANSI comes in as down+up which is confusing to try and parse/pair these things up
                if (!inputEvent.KeyEvent.bKeyDown)
                {
                    return;
                }

                foreach (Tuple<char, InputRecord> released in Parser.ProcessInput (Tuple.Create (inputEvent.KeyEvent.UnicodeChar, inputEvent)))
                {
                    ProcessAfterParsing (released.Item2);
                }

                /*
                if (inputEvent.KeyEvent.wVirtualKeyCode == (VK)ConsoleKey.Packet)
                {
                    // Used to pass Unicode characters as if they were keystrokes.
                    // The VK_PACKET key is the low word of a 32-bit
                    // Virtual Key value used for non-keyboard input methods.
                    inputEvent.KeyEvent = FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
                }

                WindowsConsole.ConsoleKeyInfoEx keyInfo = ToConsoleKeyInfoEx (inputEvent.KeyEvent);

                //Debug.WriteLine ($"event: KBD: {GetKeyboardLayoutName()} {inputEvent.ToString ()} {keyInfo.ToString (keyInfo)}");

                KeyCode map = MapKey (keyInfo);

                if (map == KeyCode.Null)
                {
                    break;
                }
                */
                // This follows convention in NetDriver

                break;

            case EventType.Mouse:
                MouseEventArgs me = ToDriverMouse (inputEvent.MouseEvent);

                OnMouseEvent (me);

                break;
        }
    }

    /// <inheritdoc/>
    protected override void ProcessAfterParsing (InputRecord input)
    {
        var key = KeyConverter.ToKey (input);

        if (key != (Key)0)
        {
            OnKeyDown (key!);
            OnKeyUp (key!);
        }
    }

    public MouseEventArgs ToDriverMouse (MouseEventRecord e)
    {
        var mouseFlags = MouseFlags.ReportMousePosition;

        mouseFlags = UpdateMouseFlags (mouseFlags, e.ButtonState, ButtonState.Button1Pressed, MouseFlags.Button1Pressed, MouseFlags.Button1Released, 0);
        mouseFlags = UpdateMouseFlags (mouseFlags, e.ButtonState, ButtonState.Button2Pressed, MouseFlags.Button2Pressed, MouseFlags.Button2Released, 1);
        mouseFlags = UpdateMouseFlags (mouseFlags, e.ButtonState, ButtonState.Button4Pressed, MouseFlags.Button4Pressed, MouseFlags.Button4Released, 3);

        // Deal with button 3 separately because it is considered same as 'rightmost button'
        if (e.ButtonState.HasFlag (ButtonState.Button3Pressed) || e.ButtonState.HasFlag (ButtonState.RightmostButtonPressed))
        {
            mouseFlags |= MouseFlags.Button3Pressed;
            _lastWasPressed [2] = true;
        }
        else
        {
            if (_lastWasPressed [2])
            {
                mouseFlags |= MouseFlags.Button3Released;
                _lastWasPressed [2] = false;
            }
        }

        if (e.EventFlags == EventFlags.MouseWheeled)
        {
            switch ((int)e.ButtonState)
            {
                case > 0:
                    mouseFlags = MouseFlags.WheeledUp;

                    break;

                case < 0:
                    mouseFlags = MouseFlags.WheeledDown;

                    break;
            }
        }

        var result = new MouseEventArgs
        {
            Position = new (e.MousePosition.X, e.MousePosition.Y),
            Flags = mouseFlags
        };

        // TODO: Return keys too

        return result;
    }

    private MouseFlags UpdateMouseFlags (
        MouseFlags current,
        ButtonState newState,
        ButtonState pressedState,
        MouseFlags pressedFlag,
        MouseFlags releasedFlag,
        int buttonIndex
    )
    {
        if (newState.HasFlag (pressedState))
        {
            current |= pressedFlag;
            _lastWasPressed [buttonIndex] = true;
        }
        else
        {
            if (_lastWasPressed [buttonIndex])
            {
                current |= releasedFlag;
                _lastWasPressed [buttonIndex] = false;
            }
        }

        return current;
    }
}
