#nullable enable

using System.Diagnostics;

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

    private StringBuilder held = new StringBuilder();

    /// <summary>
    /// <para>
    /// Processes input which may be a single character or multiple.
    /// Returns what should be passed on to any downstream input processing
    /// (i.e. removes expected Ansi responses from the input stream
    /// </para>
    /// <para>
    /// This method is designed to be called iteratively and as such may
    /// return more characters than were passed in depending on previous
    /// calls (e.g. if it was in the middle of an unrelated ANSI response.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public string ProcessInput (string input)
    {

        if (inResponse)
        {
            if (currentTerminator != null && input.StartsWith (currentTerminator))
            {
                // Consume terminator and release the event
                held.Append (currentTerminator);
                currentResponse?.Invoke (held.ToString());

                // clear the state
                held.Clear ();
                currentResponse = null;

                // recurse
                return ProcessInput (input.Substring (currentTerminator.Length));
            }

            // we are in a response but have not reached terminator yet
            held.Append (input [0]);
            return ProcessInput (input.Substring (1));
        }


        // if character is escape
        if (input.StartsWith ('\x1B'))
        {
            // We shouldn't get an escape in the middle of a response - TODO: figure out how to handle that
            Debug.Assert (!inResponse);


            // consume the escape
            held.Append (input [0]);
            inResponse = true;
            return ProcessInput (input.Substring (1));
        }

        return input[0] + ProcessInput (input.Substring (1));
    }

    private string? currentTerminator = null;
    private Action<string>? currentResponse = null;

    public void ExpectResponse (string terminator, Action<string> response)
    {
        currentTerminator = terminator;
        currentResponse = response;
        
    }
}
