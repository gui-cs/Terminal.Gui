#nullable enable

namespace Terminal.Gui;
class AnsiResponseParser
{

    /*
     * ANSI Input Sequences
     *
     * \x1B[A   // Up Arrow key pressed
     * \x1B[B   // Down Arrow key pressed
     * \x1B[C   // Right Arrow key pressed
     * \x1B[D   // Left Arrow key pressed
     * \x1B[3~  // Delete key pressed
     * \x1B[2~  // Insert key pressed
     * \x1B[5~  // Page Up key pressed
     * \x1B[6~  // Page Down key pressed
     * \x1B[1;5D // Ctrl + Left Arrow
     * \x1B[1;5C // Ctrl + Right Arrow
     * \x1B[0;10;20M // Mouse button pressed at position (10, 20)
     * \x1B[0c  // Device Attributes Response (e.g., terminal identification)
     */

    private bool inResponse = false;


    public bool ConsumeInput (char character, out string? released)
    {
        // if character is escape

        // start consuming till we see terminator

        released = null;
        return false;
    }

    public void ExpectResponse (char terminator, Action<string> response)
    {
    }
}
