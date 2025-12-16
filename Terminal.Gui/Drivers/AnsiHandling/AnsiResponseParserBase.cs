using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
/// </summary>
internal abstract class AnsiResponseParserBase (IHeld heldContent, ITimeProvider timeProvider) : IAnsiResponseParser
{
    #region Fields and State Management

    private const char ESCAPE = '\x1B';

    protected object _lockExpectedResponses = new ();
    protected object _lockState = new ();
    protected readonly IHeld _heldContent = heldContent;
    protected readonly ITimeProvider _timeProvider = timeProvider;

    /// <summary>
    ///     Responses we are expecting to come in (one-time expectations).
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _expectedResponses = [];

    /// <summary>
    ///     Collection of responses that have been stopped via <see cref="StopExpecting"/>.
    ///     These are swallowed but do not invoke callbacks to avoid corrupting downstream processing.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _lateResponses = [];

    /// <summary>
    ///     Persistent expectations that remain active across multiple responses (e.g., continuous mouse events).
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _persistentExpectations = [];

    // Valid ANSI response terminators per CSI specification
    // See https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
    // Note: N and O are intentionally excluded as they have special handling
    protected readonly HashSet<char> _knownTerminators =
    [
        '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'P', 'Q', 'R', 'S', 'T', 'W', 'X', 'Z',
        '^', '`', '~',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
        'l', 'm', 'n',
        'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
    ];

    private AnsiResponseParserState _state = AnsiResponseParserState.Normal;

    /// <inheritdoc/>
    public AnsiResponseParserState State
    {
        get => _state;
        protected set
        {
            StateChangedAt = _timeProvider.Now;
            _state = value;
        }
    }

    /// <summary>
    ///     Timestamp when <see cref="State"/> was last changed. Used to detect stale escape sequences.
    /// </summary>
    public DateTime StateChangedAt { get; private set; }

    #endregion

    #region Constructor and State Management

    protected void ResetState ()
    {
        State = AnsiResponseParserState.Normal;

        lock (_lockState)
        {
            _heldContent.ClearHeld ();
        }
    }

    #endregion

    #region Input Processing

    /// <summary>
    ///     Processes an input collection of objects <paramref name="inputLength"/> long.
    ///     Parses ANSI escape sequences and routes them to appropriate handlers (mouse, keyboard, expected responses).
    /// </summary>
    /// <param name="getCharAtIndex">Function to get the character representation of element i in the input collection.</param>
    /// <param name="getObjectAtIndex">Function to get the actual element at index i (e.g., char or Tuple&lt;char,T&gt;).</param>
    /// <param name="appendOutput">
    ///     Action invoked when the parser confirms an element should be appended to the output stream
    ///     (i.e., it's not part of a recognized escape sequence).
    /// </param>
    /// <param name="inputLength">The total number of elements in the input collection.</param>
    protected void ProcessInputBase (
        Func<int, char> getCharAtIndex,
        Func<int, object> getObjectAtIndex,
        Action<object> appendOutput,
        int inputLength
    )
    {
        lock (_lockState)
        {
            ProcessInputBaseImpl (getCharAtIndex, getObjectAtIndex, appendOutput, inputLength);
        }
    }

    private void ProcessInputBaseImpl (
        Func<int, char> getCharAtIndex,
        Func<int, object> getObjectAtIndex,
        Action<object> appendOutput,
        int inputLength
    )
    {
        var index = 0; // Tracks position in the input string

        while (index < inputLength)
        {
            char currentChar = getCharAtIndex (index);
            object currentObj = getObjectAtIndex (index);

            bool isEscape = currentChar == ESCAPE;

            // Logging.Trace($"Processing character '{currentChar}' (isEscape: {isEscape})");
            switch (State)
            {
                case AnsiResponseParserState.Normal:
                    if (isEscape)
                    {
                        // Escape character detected, move to ExpectingBracket state
                        State = AnsiResponseParserState.ExpectingEscapeSequence;
                        _heldContent.AddToHeld (currentObj); // Hold the escape character
                    }
                    else
                    {
                        // Normal character, append to output
                        appendOutput (currentObj);
                    }

                    break;

                case AnsiResponseParserState.ExpectingEscapeSequence:
                    if (isEscape)
                    {
                        // Second escape so we must release first
                        ReleaseHeld (appendOutput, AnsiResponseParserState.ExpectingEscapeSequence);
                        _heldContent.AddToHeld (currentObj); // Hold the new escape
                    }
                    else if (_heldContent.Length == 1)
                    {
                        //We need O for SS3 mode F1-F4 e.g. "<esc>OP" => F1
                        //We need any letter or digit for Alt+Letter (see EscAsAltPattern)
                        //In fact lets just always see what comes after esc

                        // Detected '[' or 'O', transition to InResponse state
                        State = AnsiResponseParserState.InResponse;
                        _heldContent.AddToHeld (currentObj); // Hold the letter
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        ReleaseHeld (appendOutput);
                        appendOutput (currentObj); // Add current character
                    }

                    break;

                case AnsiResponseParserState.InResponse:

                    // if seeing another esc, we must resolve the current one first
                    if (isEscape)
                    {
                        ReleaseHeld (appendOutput);
                        State = AnsiResponseParserState.ExpectingEscapeSequence;
                        _heldContent.AddToHeld (currentObj);
                    }
                    else
                    {
                        // Non esc, so continue to build sequence
                        _heldContent.AddToHeld (currentObj);

                        // Check if the held content should be released
                        if (ShouldReleaseHeldContent ())
                        {
                            ReleaseHeld (appendOutput);
                        }
                    }

                    break;
            }

            index++;
        }
    }

    #endregion

    #region Held Content Management

    private void ReleaseHeld (Action<object> appendOutput, AnsiResponseParserState newState = AnsiResponseParserState.Normal)
    {
        TryLastMinuteSequences ();

        foreach (object o in _heldContent.HeldToObjects ())
        {
            appendOutput (o);
        }

        State = newState;
        _heldContent.ClearHeld ();
    }

    /// <summary>
    ///     Checks currently held characters against sequences that have conflicts with longer sequences
    ///     (e.g., Esc as Alt sequences which can conflict with ESC O P SS3 sequences).
    /// </summary>
    /// <remarks>
    ///     This is called as a last resort before releasing held content to handle ambiguous sequences
    ///     where shorter patterns might match but we need to wait to see if a longer pattern emerges.
    /// </remarks>
    protected void TryLastMinuteSequences ()
    {
        lock (_lockState)
        {
            string? cur = _heldContent.HeldToString ();

            if (HandleKeyboard)
            {
                AnsiKeyboardParserPattern? pattern = _keyboardParser.IsKeyboard (cur, true);

                if (pattern != null)
                {
                    RaiseKeyboardEvent (pattern, cur);
                    _heldContent.ClearHeld ();

                    return;
                }
            }

            // We have something totally unexpected, not a CSI and
            // still Esc+<something>. So give last minute swallow chance
            if (cur!.Length >= 2 && cur [0] == ESCAPE)
            {
                // Maybe swallow anyway if user has custom delegate
                bool swallow = ShouldSwallowUnexpectedResponse ();

                if (swallow)
                {
                    _heldContent.ClearHeld ();

                    //Logging.Trace ($"AnsiResponseParser last minute swallowed '{cur}'");
                }
            }
        }
    }

    /// <summary>
    ///     Determines whether currently held content should be released based on accumulated escape sequence.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> to release held content to output stream;
    ///     <see langword="false"/> to continue accumulating or if content was handled internally.
    /// </returns>
    protected bool ShouldReleaseHeldContent ()
    {
        lock (_lockState)
        {
            string? cur = _heldContent.HeldToString ();

            if (HandleMouse && IsMouse (cur))
            {
                RaiseMouseEvent (cur);
                ResetState ();

                return false;
            }

            if (HandleKeyboard)
            {
                AnsiKeyboardParserPattern? pattern = _keyboardParser.IsKeyboard (cur);

                if (pattern != null)
                {
                    RaiseKeyboardEvent (pattern, cur);
                    ResetState ();

                    return false;
                }
            }

            lock (_lockExpectedResponses)
            {
                // Look for an expected response for what is accumulated so far (since Esc)
                if (MatchResponse (
                                   cur,
                                   _expectedResponses,
                                   true,
                                   true))
                {
                    return false;
                }

                // Also try looking for late requests - in which case we do not invoke but still swallow content to avoid corrupting downstream
                if (MatchResponse (
                                   cur,
                                   _lateResponses,
                                   false,
                                   true))
                {
                    return false;
                }

                // Look for persistent requests
                if (MatchResponse (
                                   cur,
                                   _persistentExpectations,
                                   true,
                                   false))
                {
                    return false;
                }
            }

            // Finally if it is a valid ansi response but not one we are expect (e.g. its mouse activity)
            // then we can release it back to input processing stream
            if (_knownTerminators.Contains (cur!.Last ()) && cur!.StartsWith (EscSeqUtils.CSI))
            {
                // We have found a terminator so bail
                State = AnsiResponseParserState.Normal;

                // Maybe swallow anyway if user has custom delegate
                bool swallow = ShouldSwallowUnexpectedResponse ();

                if (swallow)
                {
                    _heldContent.ClearHeld ();

                    //Logging.Trace ($"AnsiResponseParser swallowed '{cur}'");

                    // Do not send back to input stream
                    return false;
                }

                // Do release back to input stream
                return true;
            }
        }

        return false; // Continue accumulating
    }

    /// <summary>
    ///     When overridden in a derived class, determines whether an unexpected but valid ANSI response
    ///     should be swallowed (not released to output) or released to the input stream.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is only called for complete ANSI responses (sequences ending with a known terminator
    ///         from <see cref="_knownTerminators"/>) that don't match any expected response patterns.
    ///     </para>
    ///     <para>
    ///         Implement this to provide custom handling for unexpected escape sequences.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> to swallow the sequence (prevent it from reaching output stream);
    ///     <see langword="false"/> to release it to the output stream.
    /// </returns>
    protected abstract bool ShouldSwallowUnexpectedResponse ();

    #endregion

    #region Response Expectation Management

    /// <summary>
    ///     Attempts to match the current accumulated input against a collection of expected responses.
    /// </summary>
    /// <param name="cur">The current accumulated input string.</param>
    /// <param name="collection">The collection of expectations to match against.</param>
    /// <param name="invokeCallback">Whether to invoke the response callback if a match is found.</param>
    /// <param name="removeExpectation">Whether to remove the expectation from the collection after matching.</param>
    /// <returns><see langword="true"/> if a match was found; <see langword="false"/> otherwise.</returns>
    private bool MatchResponse (string? cur, List<AnsiResponseExpectation> collection, bool invokeCallback, bool removeExpectation)
    {
        // Check for expected responses
        AnsiResponseExpectation? matchingResponse = collection.FirstOrDefault (r => r.Matches (cur));

        if (matchingResponse?.Response != null)
        {
            //Logging.Trace ($"AnsiResponseParser processed '{cur}'");

            if (invokeCallback)
            {
                matchingResponse.Response.Invoke (_heldContent);
            }

            ResetState ();

            if (removeExpectation)
            {
                collection.Remove (matchingResponse);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void ExpectResponse (string? terminator, Action<string?> response, Action? abandoned, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                _persistentExpectations.Add (new (terminator, h => response.Invoke (h.HeldToString ()), abandoned));
            }
            else
            {
                _expectedResponses.Add (new (terminator, h => response.Invoke (h.HeldToString ()), abandoned));
            }
        }
    }

    /// <inheritdoc/>
    public bool IsExpecting (string? terminator)
    {
        lock (_lockExpectedResponses)
        {
            // If any of the new terminator matches any existing terminators characters it's a collision so true.
            return _expectedResponses.Any (r => r.Terminator!.Intersect (terminator!).Any ());
        }
    }

    /// <inheritdoc/>
    public void StopExpecting (string? terminator, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                AnsiResponseExpectation [] removed = _persistentExpectations.Where (r => r.Matches (terminator)).ToArray ();

                foreach (AnsiResponseExpectation toRemove in removed)
                {
                    _persistentExpectations.Remove (toRemove);
                    toRemove.Abandoned?.Invoke ();
                }
            }
            else
            {
                AnsiResponseExpectation [] removed = _expectedResponses.Where (r => r.Terminator == terminator).ToArray ();

                foreach (AnsiResponseExpectation r in removed)
                {
                    _expectedResponses.Remove (r);
                    _lateResponses.Add (r);
                    r.Abandoned?.Invoke ();
                }
            }
        }
    }

    #endregion

    #region Mouse Handling

    private readonly AnsiMouseParser _mouseParser = new ();

    /// <summary>
    ///     Event raised when mouse events are detected in the input stream.
    /// </summary>
    /// <remarks>
    ///     Requires setting <see cref="HandleMouse"/> to <see langword="true"/>.
    ///     Mouse events follow SGR extended format (ESC[&lt;button;x;yM/m) when
    ///     <see cref="EscSeqUtils.CSI_EnableMouseEvents"/> is enabled.
    /// </remarks>
    public event EventHandler<Mouse>? Mouse;

    /// <summary>
    ///     Gets or sets whether to explicitly handle mouse escape sequences by raising the <see cref="Mouse"/> event.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true"/>, mouse sequences are parsed and raised as events.
    ///     When <see langword="false"/> (default), mouse sequences are treated as regular input.
    /// </remarks>
    public bool HandleMouse { get; set; } = false;

    private void RaiseMouseEvent (string? cur)
    {
        Mouse? ev = _mouseParser.ProcessMouseInput (cur);

        if (ev != null)
        {
            Mouse?.Invoke (this, ev);
        }
    }

    private bool IsMouse (string? cur) { return _mouseParser.IsMouse (cur); }

    #endregion

    #region Keyboard Handling

    protected readonly AnsiKeyboardParser _keyboardParser = new ();

    /// <summary>
    ///     Event raised when keyboard escape sequences are detected (e.g., cursor keys, function keys).
    /// </summary>
    /// <remarks>
    ///     Requires setting <see cref="HandleKeyboard"/> to <see langword="true"/>.
    ///     Handles sequences like ESC[A (cursor up), ESC[1;5A (Ctrl+cursor up), ESC OP (F1 in SS3 mode).
    /// </remarks>
    public event EventHandler<Key>? Keyboard;

    /// <summary>
    ///     Gets or sets whether to explicitly handle keyboard escape sequences by raising the <see cref="Keyboard"/> event.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true"/>, keyboard sequences are parsed and raised as events.
    ///     When <see langword="false"/> (default), keyboard sequences are treated as regular input.
    /// </remarks>
    public bool HandleKeyboard { get; set; } = false;

    protected void RaiseKeyboardEvent (AnsiKeyboardParserPattern pattern, string? cur)
    {
        Key? k = pattern.GetKey (cur);

        if (k is null)
        {
            Logging.Logger.LogError ($"Failed to determine a Key for given Keyboard escape sequence '{cur}'");
        }
        else
        {
            Keyboard?.Invoke (this, k);
        }
    }

    #endregion
}
