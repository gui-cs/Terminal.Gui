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

    // Enum to manage the parser's state
    private enum ParserState
    {
        Normal,
        ExpectingBracket,
        InResponse
    }

    // Current state of the parser
    private ParserState currentState = ParserState.Normal;

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
        // Add more common ANSI sequences to be ignored
        _ignorers.Add (s => s.StartsWith ("\x1B[<") && s.EndsWith ("M"));  // Mouse event
        _ignorers.Add (s => s.StartsWith ("\x1B[") && s.EndsWith ("A"));   // Up arrow
        _ignorers.Add (s => s.StartsWith ("\x1B[") && s.EndsWith ("B"));   // Down arrow
        _ignorers.Add (s => s.StartsWith ("\x1B[") && s.EndsWith ("C"));   // Right arrow
        _ignorers.Add (s => s.StartsWith ("\x1B[") && s.EndsWith ("D"));   // Left arrow
        _ignorers.Add (s => s.StartsWith ("\x1B[3~"));                     // Delete
        _ignorers.Add (s => s.StartsWith ("\x1B[5~"));                     // Page Up
        _ignorers.Add (s => s.StartsWith ("\x1B[6~"));                     // Page Down
        _ignorers.Add (s => s.StartsWith ("\x1B[2~"));                     // Insert
        // Add more if necessary
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

            switch (currentState)
            {
                case ParserState.Normal:
                    if (currentChar == '\x1B')
                    {
                        // Escape character detected, move to ExpectingBracket state
                        currentState = ParserState.ExpectingBracket;
                        held.Append (currentChar);  // Hold the escape character
                        index++;
                    }
                    else
                    {
                        // Normal character, append to output
                        output.Append (currentChar);
                        index++;
                    }
                    break;

                case ParserState.ExpectingBracket:
                    if (currentChar == '[' || currentChar == ']')
                    {
                        // Detected '[' or ']', transition to InResponse state
                        currentState = ParserState.InResponse;
                        held.Append (currentChar);  // Hold the '[' or ']'
                        index++;
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        output.Append (held.ToString ());
                        output.Append (currentChar);  // Add current character
                        ResetState ();
                        index++;
                    }
                    break;

                case ParserState.InResponse:
                    held.Append (currentChar);

                    // Check if the held content should be released
                    var handled = HandleHeldContent ();
                    if (!string.IsNullOrEmpty (handled))
                    {
                        output.Append (handled);
                        ResetState ();  // Exit response mode and reset
                    }

                    index++;
                    break;
            }
        }

        return output.ToString ();  // Return all characters that passed through
    }


    /// <summary>
    /// Resets the parser's state when a response is handled or finished.
    /// </summary>
    private void ResetState ()
    {
        currentState = ParserState.Normal;
        held.Clear ();
        currentTerminator = null;
        currentResponse = null;
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
