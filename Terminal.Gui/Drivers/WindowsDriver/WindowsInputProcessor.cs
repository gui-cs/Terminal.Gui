using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

using InputRecord = WindowsConsole.InputRecord;

/// <summary>
///     Input processor for <see cref="WindowsInput"/>, deals in <see cref="WindowsConsole.InputRecord"/> stream.
/// </summary>
internal class WindowsInputProcessor : InputProcessorImpl<InputRecord>
{
    private readonly bool [] _lastWasPressed = new bool [4];

    /// <inheritdoc/>
    public WindowsInputProcessor (ConcurrentQueue<InputRecord> inputBuffer) : base (inputBuffer, new WindowsKeyConverter ())
    {
    }

    /// <inheritdoc />
    public override void EnqueueMouseEvent (IApplication? app, Mouse mouse)
    {
        InputQueue.Enqueue (new ()
        {
            EventType = WindowsConsole.EventType.Mouse,
            MouseEvent = ToMouseEventRecord (mouse)
        });
    }

    /// <inheritdoc/>
    protected override void Process (InputRecord inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case WindowsConsole.EventType.Key:

                // TODO: v1 supported distinct key up/down events on Windows.
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
                // This follows convention in DotNetDriver

                break;

            case WindowsConsole.EventType.Mouse:
                Mouse me = ToMouseEvent (inputEvent.MouseEvent);

                RaiseSyntheticMouseEvent (me);

                break;
        }
    }

    /// <summary>
    ///     Converts a Windows-specific mouse event to a <see cref="Mouse"/>.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public Mouse ToMouseEvent (WindowsConsole.MouseEventRecord e)
    {
        var mouseFlags = MouseFlags.None;

        mouseFlags = UpdateMouseFlags (
                                       mouseFlags,
                                       e.ButtonState,
                                       WindowsConsole.ButtonState.Button1Pressed,
                                       MouseFlags.LeftButtonPressed,
                                       MouseFlags.LeftButtonReleased,
                                       0);

        mouseFlags = UpdateMouseFlags (
                                       mouseFlags,
                                       e.ButtonState,
                                       WindowsConsole.ButtonState.Button2Pressed,
                                       MouseFlags.MiddleButtonPressed,
                                       MouseFlags.MiddleButtonReleased,
                                       1);

        mouseFlags = UpdateMouseFlags (
                                       mouseFlags,
                                       e.ButtonState,
                                       WindowsConsole.ButtonState.Button4Pressed,
                                       MouseFlags.Button4Pressed,
                                       MouseFlags.Button4Released,
                                       3);

        // Deal with button 3 separately because it is considered same as 'rightmost button'
        if (e.ButtonState.HasFlag (WindowsConsole.ButtonState.Button3Pressed) || e.ButtonState.HasFlag (WindowsConsole.ButtonState.RightmostButtonPressed))
        {
            mouseFlags |= MouseFlags.RightButtonPressed;
            _lastWasPressed [2] = true;
        }
        else
        {
            if (_lastWasPressed [2])
            {
                mouseFlags |= MouseFlags.RightButtonReleased;

                _lastWasPressed [2] = false;
            }
        }

        if (e.EventFlags == WindowsConsole.EventFlags.MouseWheeled)
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

        if (e.EventFlags != WindowsConsole.EventFlags.NoEvent)
        {
            switch (e.EventFlags)
            {
                case WindowsConsole.EventFlags.MouseMoved:
                    mouseFlags |= MouseFlags.PositionReport;

                    break;
            }
        }

        if (e.ControlKeyState != WindowsConsole.ControlKeyState.NoControlKeyPressed)
        {
            switch (e.ControlKeyState)
            {
                case WindowsConsole.ControlKeyState.RightAltPressed:
                case WindowsConsole.ControlKeyState.LeftAltPressed:
                    mouseFlags |= MouseFlags.Alt;

                    break;
                case WindowsConsole.ControlKeyState.RightControlPressed:
                case WindowsConsole.ControlKeyState.LeftControlPressed:
                    mouseFlags |= MouseFlags.Ctrl;

                    break;
                case WindowsConsole.ControlKeyState.ShiftPressed:
                    mouseFlags |= MouseFlags.Shift;

                    break;
            }
        }

        var result = new Mouse
        {
            Timestamp = DateTime.Now,
            Position = new (e.MousePosition.X, e.MousePosition.Y),
            ScreenPosition = new (e.MousePosition.X, e.MousePosition.Y),
            Flags = mouseFlags
        };

        return result;
    }

    private MouseFlags UpdateMouseFlags (
        MouseFlags current,
        WindowsConsole.ButtonState newState,
        WindowsConsole.ButtonState pressedState,
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

    /// <summary>
    ///     Converts a <see cref="Mouse"/> to a Windows-specific <see cref="WindowsConsole.MouseEventRecord"/>.
    /// </summary>
    /// <param name="mouse"></param>
    /// <returns></returns>
    public WindowsConsole.MouseEventRecord ToMouseEventRecord (Mouse mouse)
    {
        var buttonState = WindowsConsole.ButtonState.NoButtonPressed;
        var eventFlags = WindowsConsole.EventFlags.NoEvent;
        var controlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed;

        // Convert button states
        if (mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button1Pressed;
        }

        if (mouse.Flags.HasFlag (MouseFlags.MiddleButtonPressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button2Pressed;
        }

        if (mouse.Flags.HasFlag (MouseFlags.RightButtonPressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button3Pressed;
        }

        if (mouse.Flags.HasFlag (MouseFlags.Button4Pressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button4Pressed;
        }

        // Convert mouse wheel events
        if (mouse.Flags.HasFlag (MouseFlags.WheeledUp))
        {
            eventFlags = WindowsConsole.EventFlags.MouseWheeled;
            buttonState = (WindowsConsole.ButtonState)0x00780000; // Positive value for wheel up
        }
        else if (mouse.Flags.HasFlag (MouseFlags.WheeledDown))
        {
            eventFlags = WindowsConsole.EventFlags.MouseWheeled;
            buttonState = (WindowsConsole.ButtonState)unchecked((int)0xFF880000); // Negative value for wheel down
        }

        // Convert movement flag
        if (mouse.Flags.HasFlag (MouseFlags.PositionReport))
        {
            eventFlags |= WindowsConsole.EventFlags.MouseMoved;
        }

        // Convert modifier keys
        if (mouse.Flags.HasFlag (MouseFlags.Alt))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftAltPressed;
        }

        if (mouse.Flags.HasFlag (MouseFlags.Ctrl))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftControlPressed;
        }

        if (mouse.Flags.HasFlag (MouseFlags.Shift))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.ShiftPressed;
        }

        return new ()
        {
            MousePosition = new ((short)mouse.ScreenPosition.X, (short)mouse.ScreenPosition.Y),
            ButtonState = buttonState,
            ControlKeyState = controlKeyState,
            EventFlags = eventFlags
        };
    }
}
