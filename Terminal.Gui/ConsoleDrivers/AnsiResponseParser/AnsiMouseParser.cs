#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

/// <summary>
///     Parses mouse ansi escape sequences into <see cref="MouseEventArgs"/>
///     including support for pressed, released and mouse wheel.
/// </summary>
public class AnsiMouseParser
{
    // Regex patterns for button press/release, wheel scroll, and mouse position reporting
    private readonly Regex _mouseEventPattern = new (@"\u001b\[<(\d+);(\d+);(\d+)(M|m)", RegexOptions.Compiled);

    /// <summary>
    ///     Returns true if it is a mouse event
    /// </summary>
    /// <param name="cur"></param>
    /// <returns></returns>
    public bool IsMouse (string cur)
    {
        // Typically in this format
        // ESC [ < {button_code};{x_pos};{y_pos}{final_byte}
        return cur.EndsWith ('M') || cur.EndsWith ('m');
    }

    /// <summary>
    ///     Parses a mouse ansi escape sequence into a mouse event. Returns null if input
    ///     is not a mouse event or its syntax is not understood.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public MouseEventArgs? ProcessMouseInput (string input)
    {
        // Match mouse wheel events first
        Match match = _mouseEventPattern.Match (input);

        if (match.Success)
        {
            int buttonCode = int.Parse (match.Groups [1].Value);

            // The top-left corner of the terminal corresponds to (1, 1) for both X (column) and Y (row) coordinates.
            // ANSI standards and terminal conventions historically treat screen positions as 1 - based.

            int x = int.Parse (match.Groups [2].Value) - 1;
            int y = int.Parse (match.Groups [3].Value) - 1;
            char terminator = match.Groups [4].Value.Single ();

            var m = new MouseEventArgs
            {
                Position = new (x, y),
                Flags = GetFlags (buttonCode, terminator)
            };

            Logging.Trace ($"{nameof (AnsiMouseParser)} handled as {input} mouse {m.Flags} at {m.Position}");

            return m;
        }

        // its some kind of odd mouse event that doesn't follow expected format?
        return null;
    }

    private static MouseFlags GetFlags (int buttonCode, char terminator)
    {
        MouseFlags buttonState = 0;

        switch (buttonCode)
        {
            case 0:
            case 8:
            case 16:
            case 24:
            case 32:
            case 36:
            case 40:
            case 48:
            case 56:
                buttonState = terminator == 'M'
                                  ? MouseFlags.Button1Pressed
                                  : MouseFlags.Button1Released;

                break;
            case 1:
            case 9:
            case 17:
            case 25:
            case 33:
            case 37:
            case 41:
            case 45:
            case 49:
            case 53:
            case 57:
            case 61:
                buttonState = terminator == 'M'
                                  ? MouseFlags.Button2Pressed
                                  : MouseFlags.Button2Released;

                break;
            case 2:
            case 10:
            case 14:
            case 18:
            case 22:
            case 26:
            case 30:
            case 34:
            case 42:
            case 46:
            case 50:
            case 54:
            case 58:
            case 62:
                buttonState = terminator == 'M'
                                  ? MouseFlags.Button3Pressed
                                  : MouseFlags.Button3Released;

                break;
            case 35:
            //// Needed for Windows OS
            //if (isButtonPressed && c == 'm'
            //	&& (lastMouseEvent.ButtonState == MouseFlags.Button1Pressed
            //	|| lastMouseEvent.ButtonState == MouseFlags.Button2Pressed
            //	|| lastMouseEvent.ButtonState == MouseFlags.Button3Pressed)) {

            //	switch (lastMouseEvent.ButtonState) {
            //	case MouseFlags.Button1Pressed:
            //		buttonState = MouseFlags.Button1Released;
            //		break;
            //	case MouseFlags.Button2Pressed:
            //		buttonState = MouseFlags.Button2Released;
            //		break;
            //	case MouseFlags.Button3Pressed:
            //		buttonState = MouseFlags.Button3Released;
            //		break;
            //	}
            //} else {
            //	buttonState = MouseFlags.ReportMousePosition;
            //}
            //break;
            case 39:
            case 43:
            case 47:
            case 51:
            case 55:
            case 59:
            case 63:
                buttonState = MouseFlags.ReportMousePosition;

                break;
            case 64:
                buttonState = MouseFlags.WheeledUp;

                break;
            case 65:
                buttonState = MouseFlags.WheeledDown;

                break;
            case 68:
            case 72:
            case 80:
                buttonState = MouseFlags.WheeledLeft; // Shift/Ctrl+WheeledUp

                break;
            case 69:
            case 73:
            case 81:
                buttonState = MouseFlags.WheeledRight; // Shift/Ctrl+WheeledDown

                break;
        }

        // Modifiers.
        switch (buttonCode)
        {
            case 8:
            case 9:
            case 10:
            case 43:
                buttonState |= MouseFlags.ButtonAlt;

                break;
            case 14:
            case 47:
                buttonState |= MouseFlags.ButtonAlt | MouseFlags.ButtonShift;

                break;
            case 16:
            case 17:
            case 18:
            case 51:
                buttonState |= MouseFlags.ButtonCtrl;

                break;
            case 22:
            case 55:
                buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonShift;

                break;
            case 24:
            case 25:
            case 26:
            case 59:
                buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt;

                break;
            case 30:
            case 63:
                buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonShift | MouseFlags.ButtonAlt;

                break;
            case 32:
            case 33:
            case 34:
                buttonState |= MouseFlags.ReportMousePosition;

                break;
            case 36:
            case 37:
                buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonShift;

                break;
            case 39:
            case 68:
            case 69:
                buttonState |= MouseFlags.ButtonShift;

                break;
            case 40:
            case 41:
            case 42:
                buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonAlt;

                break;
            case 45:
            case 46:
                buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonAlt | MouseFlags.ButtonShift;

                break;
            case 48:
            case 49:
            case 50:
                buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl;

                break;
            case 53:
            case 54:
                buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonShift;

                break;
            case 56:
            case 57:
            case 58:
                buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt;

                break;
            case 61:
            case 62:
                buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonShift | MouseFlags.ButtonAlt;

                break;
        }

        return buttonState;
    }
}
