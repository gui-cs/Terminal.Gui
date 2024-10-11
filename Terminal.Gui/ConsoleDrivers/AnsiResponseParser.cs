#nullable enable

using System.Diagnostics;
using System.Text;

namespace Terminal.Gui;
class AnsiResponseParser
{
    private bool inResponse = false;
    private StringBuilder held = new StringBuilder ();
    private string? currentTerminator = null;
    private Action<string>? currentResponse = null;


    private List<Func<string, bool>> _ignorers = new ();

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

    public AnsiResponseParser ()
    {
        // How to spot when you have entered and left an AnsiResponse but not the one we are looking for
        _ignorers.Add (s=>s.StartsWith ("\x1B[<") && s.EndsWith ("M"));
    }

    /// <summary>
    /// Processes input which may be a single character or multiple.
    /// Returns what should be passed on to any downstream input processing
    /// (i.e., removes expected ANSI responses from the input stream).
    /// </summary>
    public string ProcessInput (string input)
    {
        StringBuilder output = new StringBuilder ();  // Holds characters that should pass through
        int index = 0;  // Tracks position in the input string

        while (index < input.Length)
        {
            char currentChar = input [index];

            if (inResponse)
            {
                // If we are in a response, accumulate characters in `held`
                held.Append (currentChar);

                // Handle the current content in `held`
                var handled = HandleHeldContent ();
                if (!string.IsNullOrEmpty (handled))
                {
                    // If content is ready to be released, append it to output and reset state
                    output.Append (handled);
                    inResponse = false;
                    held.Clear ();
                }

                index++;
                continue;
            }

            // If character is the start of an escape sequence
            if (currentChar == '\x1B')
            {
                // Start capturing the ANSI response sequence
                inResponse = true;
                held.Append (currentChar);
                index++;
                continue;
            }

            // If not in an ANSI response, pass the character through as regular input
            output.Append (currentChar);
            index++;
        }

        // Return characters that should pass through as regular input
        return output.ToString ();
    }

    /// <summary>
    /// Checks the current `held` content to decide whether it should be released, either as an expected or unexpected response.
    /// </summary>
    private string HandleHeldContent ()
    {
        // If we're expecting a specific terminator, check if the content matches
        if (currentTerminator != null && held.ToString ().EndsWith (currentTerminator))
        {
            // If it matches the expected response, invoke the callback and return nothing for output
            currentResponse?.Invoke (held.ToString ());
            return string.Empty;
        }

        // Handle common ANSI sequences (such as mouse input or arrow keys)
        if (_ignorers.Any(m=>m.Invoke (held.ToString())))
        {
            // Detected mouse input, release it without triggering the delegate
            return held.ToString ();
        }

        // Add more cases here for other standard sequences (like arrow keys, function keys, etc.)

        // If no match, continue accumulating characters
        return string.Empty;
    }


    /// <summary>
    /// Registers a new expected ANSI response with a specific terminator and a callback for when the response is completed.
    /// </summary>
    public void ExpectResponse (string terminator, Action<string> response)
    {
        currentTerminator = terminator;
        currentResponse = response;
    }
}
