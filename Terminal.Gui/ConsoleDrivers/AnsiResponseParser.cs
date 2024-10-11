#nullable enable

namespace Terminal.Gui;

internal class AnsiResponseParser
{
    private readonly StringBuilder held = new ();
    private string? currentTerminator;
    private Action<string>? currentResponse;

    private readonly List<Func<string, bool>> _ignorers = new ();

    // Enum to manage the parser's state
    private enum ParserState
    {
        Normal,
        ExpectingBracket,
        InResponse
    }

    // Current state of the parser
    private ParserState currentState = ParserState.Normal;
    private HashSet<string> _knownTerminators = new HashSet<string> ();

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
        // These all are valid terminators on ansi responses,
        // see CSI in https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
        _knownTerminators.Add ("@");
        _knownTerminators.Add ("A");
        _knownTerminators.Add ("B");
        _knownTerminators.Add ("C");
        _knownTerminators.Add ("D");
        _knownTerminators.Add ("E");
        _knownTerminators.Add ("F");
        _knownTerminators.Add ("G");
        _knownTerminators.Add ("G");
        _knownTerminators.Add ("H");
        _knownTerminators.Add ("I");
        _knownTerminators.Add ("J");
        _knownTerminators.Add ("K");
        _knownTerminators.Add ("L");
        _knownTerminators.Add ("M");
        // No - N or O
        _knownTerminators.Add ("P");
        _knownTerminators.Add ("Q");
        _knownTerminators.Add ("R");
        _knownTerminators.Add ("S");
        _knownTerminators.Add ("T");
        _knownTerminators.Add ("W");
        _knownTerminators.Add ("X");
        _knownTerminators.Add ("Z");

        _knownTerminators.Add ("^");
        _knownTerminators.Add ("`");
        _knownTerminators.Add ("~");

        _knownTerminators.Add ("a");
        _knownTerminators.Add ("b");
        _knownTerminators.Add ("c");
        _knownTerminators.Add ("d");
        _knownTerminators.Add ("e");
        _knownTerminators.Add ("f");
        _knownTerminators.Add ("g");
        _knownTerminators.Add ("h");
        _knownTerminators.Add ("i");


        _knownTerminators.Add ("l");
        _knownTerminators.Add ("m");
        _knownTerminators.Add ("n");

        _knownTerminators.Add ("p");
        _knownTerminators.Add ("q");
        _knownTerminators.Add ("r");
        _knownTerminators.Add ("s");
        _knownTerminators.Add ("t");
        _knownTerminators.Add ("u");
        _knownTerminators.Add ("v");
        _knownTerminators.Add ("w");
        _knownTerminators.Add ("x");
        _knownTerminators.Add ("y");
        _knownTerminators.Add ("z");

        // Add more common ANSI sequences to be ignored
        _ignorers.Add (s => s.StartsWith ("\x1B[<") && s.EndsWith ("M")); // Mouse event

        // Add more if necessary
    }

    /// <summary>
    ///     Processes input which may be a single character or multiple.
    ///     Returns what should be passed on to any downstream input processing
    ///     (i.e., removes expected ANSI responses from the input stream).
    /// </summary>
    public string ProcessInput (string input)
    {
        var output = new StringBuilder (); // Holds characters that should pass through
        var index = 0; // Tracks position in the input string

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
                        held.Append (currentChar); // Hold the escape character
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
                    if (currentChar == '[' )
                    {
                        // Detected '[' , transition to InResponse state
                        currentState = ParserState.InResponse;
                        held.Append (currentChar); // Hold the '['
                        index++;
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        output.Append (held.ToString ());
                        output.Append (currentChar); // Add current character
                        ResetState ();
                        index++;
                    }

                    break;

                case ParserState.InResponse:
                    held.Append (currentChar);

                    // Check if the held content should be released
                    string handled = HandleHeldContent ();

                    if (!string.IsNullOrEmpty (handled))
                    {
                        output.Append (handled);
                        ResetState (); // Exit response mode and reset
                    }

                    index++;

                    break;
            }
        }

        return output.ToString (); // Return all characters that passed through
    }

    /// <summary>
    ///     Resets the parser's state when a response is handled or finished.
    /// </summary>
    private void ResetState ()
    {
        currentState = ParserState.Normal;
        held.Clear ();
    }

    /// <summary>
    ///     Checks the current `held` content to decide whether it should be released, either as an expected or unexpected
    ///     response.
    /// </summary>
    private string HandleHeldContent ()
    {
        var cur = held.ToString ();
        // If we're expecting a specific terminator, check if the content matches
        if (currentTerminator != null && cur.EndsWith (currentTerminator))
        {
            DispatchResponse ();

            return string.Empty;
        }


        if (_knownTerminators.Any (cur.EndsWith) && cur.StartsWith (EscSeqUtils.CSI))
        {
            // Detected a response that we were not expecting
            return held.ToString ();
        }

        // Handle common ANSI sequences (such as mouse input or arrow keys)
        if (_ignorers.Any (m => m.Invoke (held.ToString ())))
        {
            // Detected mouse input, release it without triggering the delegate
            return held.ToString ();
        }

        // Add more cases here for other standard sequences (like arrow keys, function keys, etc.)

        // If no match, continue accumulating characters
        return string.Empty;
    }

    private void DispatchResponse ()
    {
        // If it matches the expected response, invoke the callback and return nothing for output
        currentResponse?.Invoke (held.ToString ());
        currentResponse = null;
        currentTerminator = null;
        ResetState ();
    }

    /// <summary>
    ///     Registers a new expected ANSI response with a specific terminator and a callback for when the response is
    ///     completed.
    /// </summary>
    public void ExpectResponse (string terminator, Action<string> response)
    {
        currentTerminator = terminator;
        currentResponse = response;
    }
}
