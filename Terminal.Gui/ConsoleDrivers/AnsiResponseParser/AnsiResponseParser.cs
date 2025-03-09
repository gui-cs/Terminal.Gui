#nullable enable

using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

internal abstract class AnsiResponseParserBase : IAnsiResponseParser
{
    private const char Escape = '\x1B';
    private readonly AnsiMouseParser _mouseParser = new ();
    protected readonly AnsiKeyboardParser _keyboardParser = new ();
    protected object _lockExpectedResponses = new ();

    protected object _lockState = new ();

    /// <summary>
    ///     Event raised when mouse events are detected - requires setting <see cref="HandleMouse"/> to true
    /// </summary>
    public event EventHandler<MouseEventArgs>? Mouse;

    /// <summary>
    ///     Event raised when keyboard event is detected (e.g. cursors) - requires setting <see cref="HandleKeyboard"/>
    /// </summary>
    public event EventHandler<Key>? Keyboard;

    /// <summary>
    ///     True to explicitly handle mouse escape sequences by passing them to <see cref="Mouse"/> event.
    ///     Defaults to <see langword="false"/>
    /// </summary>
    public bool HandleMouse { get; set; } = false;

    /// <summary>
    ///     True to explicitly handle keyboard escape sequences (such as cursor keys) by passing them to <see cref="Keyboard"/>
    ///     event
    /// </summary>
    public bool HandleKeyboard { get; set; } = false;

    /// <summary>
    ///     Responses we are expecting to come in.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _expectedResponses = [];

    /// <summary>
    ///     Collection of responses that we <see cref="StopExpecting"/>.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _lateResponses = [];

    /// <summary>
    ///     Responses that you want to look out for that will come in continuously e.g. mouse events.
    ///     Key is the terminator.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _persistentExpectations = [];

    private AnsiResponseParserState _state = AnsiResponseParserState.Normal;

    /// <inheritdoc/>
    public AnsiResponseParserState State
    {
        get => _state;
        protected set
        {
            StateChangedAt = DateTime.Now;
            _state = value;
        }
    }

    protected readonly IHeld _heldContent;

    /// <summary>
    ///     When <see cref="State"/> was last changed.
    /// </summary>
    public DateTime StateChangedAt { get; private set; } = DateTime.Now;

    // These all are valid terminators on ansi responses,
    // see CSI in https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
    // No - N or O
    protected readonly HashSet<char> _knownTerminators = new (
                                                              [
                                                                  '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'G', 'H', 'I', 'J', 'K', 'L', 'M',

                                                                  // No - N or O
                                                                  'P', 'Q', 'R', 'S', 'T', 'W', 'X', 'Z',
                                                                  '^', '`', '~',
                                                                  'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
                                                                  'l', 'm', 'n',
                                                                  'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
                                                              ]);

    protected AnsiResponseParserBase (IHeld heldContent) { _heldContent = heldContent; }

    protected void ResetState ()
    {
        State = AnsiResponseParserState.Normal;

        lock (_lockState)
        {
            _heldContent.ClearHeld ();
        }
    }

    /// <summary>
    ///     Processes an input collection of objects <paramref name="inputLength"/> long.
    ///     You must provide the indexers to return the objects and the action to append
    ///     to output stream.
    /// </summary>
    /// <param name="getCharAtIndex">The character representation of element i of your input collection</param>
    /// <param name="getObjectAtIndex">The actual element in the collection (e.g. char or Tuple&lt;char,T&gt;)</param>
    /// <param name="appendOutput">
    ///     Action to invoke when parser confirms an element of the current collection or a previous
    ///     call's collection should be appended to the current output (i.e. append to your output List/StringBuilder).
    /// </param>
    /// <param name="inputLength">The total number of elements in your collection</param>
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

            bool isEscape = currentChar == Escape;

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
    ///     Checks current held chars against any sequences that have
    ///     conflicts with longer sequences e.g. Esc as Alt sequences
    ///     which can conflict if resolved earlier e.g. with EscOP ss3
    ///     sequences.
    /// </summary>
    protected void TryLastMinuteSequences ()
    {
        lock (_lockState)
        {
            string cur = _heldContent.HeldToString ();

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
            if (cur.Length >= 2 && cur [0] == Escape)
            {
                // Maybe swallow anyway if user has custom delegate
                bool swallow = ShouldSwallowUnexpectedResponse ();

                if (swallow)
                {
                    _heldContent.ClearHeld ();

                    Logging.Trace ($"AnsiResponseParser last minute swallowed '{cur}'");
                }
            }
        }
    }

    // Common response handler logic
    protected bool ShouldReleaseHeldContent ()
    {
        lock (_lockState)
        {
            string cur = _heldContent.HeldToString ();

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
            if (_knownTerminators.Contains (cur.Last ()) && cur.StartsWith (EscSeqUtils.CSI))
            {
                // We have found a terminator so bail
                State = AnsiResponseParserState.Normal;

                // Maybe swallow anyway if user has custom delegate
                bool swallow = ShouldSwallowUnexpectedResponse ();

                if (swallow)
                {
                    _heldContent.ClearHeld ();

                    Logging.Trace ($"AnsiResponseParser swallowed '{cur}'");

                    // Do not send back to input stream
                    return false;
                }

                // Do release back to input stream
                return true;
            }
        }

        return false; // Continue accumulating
    }

    private void RaiseMouseEvent (string cur)
    {
        MouseEventArgs? ev = _mouseParser.ProcessMouseInput (cur);

        if (ev != null)
        {
            Mouse?.Invoke (this, ev);
        }
    }

    private bool IsMouse (string cur) { return _mouseParser.IsMouse (cur); }

    protected void RaiseKeyboardEvent (AnsiKeyboardParserPattern pattern, string cur)
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

    /// <summary>
    ///     <para>
    ///         When overriden in a derived class, indicates whether the unexpected response
    ///         currently in <see cref="_heldContent"/> should be released or swallowed.
    ///         Use this to enable default event for escape codes.
    ///     </para>
    ///     <remarks>
    ///         Note this is only called for complete responses.
    ///         Based on <see cref="_knownTerminators"/>
    ///     </remarks>
    /// </summary>
    /// <returns></returns>
    protected abstract bool ShouldSwallowUnexpectedResponse ();

    private bool MatchResponse (string cur, List<AnsiResponseExpectation> collection, bool invokeCallback, bool removeExpectation)
    {
        // Check for expected responses
        AnsiResponseExpectation? matchingResponse = collection.FirstOrDefault (r => r.Matches (cur));

        if (matchingResponse?.Response != null)
        {
            Logging.Trace ($"AnsiResponseParser processed '{cur}'");

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
    public void ExpectResponse (string terminator, Action<string> response, Action? abandoned, bool persistent)
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
    public bool IsExpecting (string terminator)
    {
        lock (_lockExpectedResponses)
        {
            // If any of the new terminator matches any existing terminators characters it's a collision so true.
            return _expectedResponses.Any (r => r.Terminator.Intersect (terminator).Any ());
        }
    }

    /// <inheritdoc/>
    public void StopExpecting (string terminator, bool persistent)
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
}

internal class AnsiResponseParser<T> : AnsiResponseParserBase
{
    public AnsiResponseParser () : base (new GenericHeld<T> ()) { }

    /// <inheritdoc cref="AnsiResponseParser.UnknownResponseHandler"/>
    public Func<IEnumerable<Tuple<char, T>>, bool> UnexpectedResponseHandler { get; set; } = _ => false;

    public IEnumerable<Tuple<char, T>> ProcessInput (params Tuple<char, T> [] input)
    {
        List<Tuple<char, T>> output = new ();

        ProcessInputBase (
                          i => input [i].Item1,
                          i => input [i],
                          c => AppendOutput (output, c),
                          input.Length);

        return output;
    }

    private void AppendOutput (List<Tuple<char, T>> output, object c)
    {
        Tuple<char, T> tuple = (Tuple<char, T>)c;

        Logging.Trace ($"AnsiResponseParser releasing '{tuple.Item1}'");
        output.Add (tuple);
    }

    public Tuple<char, T> [] Release ()
    {
        // Lock in case Release is called from different Thread from parse
        lock (_lockState)
        {
            TryLastMinuteSequences ();

            Tuple<char, T> [] result = HeldToEnumerable ().ToArray ();

            ResetState ();

            return result;
        }
    }

    private IEnumerable<Tuple<char, T>> HeldToEnumerable () { return (IEnumerable<Tuple<char, T>>)_heldContent.HeldToObjects (); }

    /// <summary>
    ///     'Overload' for specifying an expectation that requires the metadata as well as characters. Has
    ///     a unique name because otherwise most lamdas will give ambiguous overload errors.
    /// </summary>
    /// <param name="terminator"></param>
    /// <param name="response"></param>
    /// <param name="abandoned"></param>
    /// <param name="persistent"></param>
    public void ExpectResponseT (string terminator, Action<IEnumerable<Tuple<char, T>>> response, Action? abandoned, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                _persistentExpectations.Add (new (terminator, h => response.Invoke (HeldToEnumerable ()), abandoned));
            }
            else
            {
                _expectedResponses.Add (new (terminator, h => response.Invoke (HeldToEnumerable ()), abandoned));
            }
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldSwallowUnexpectedResponse () { return UnexpectedResponseHandler.Invoke (HeldToEnumerable ()); }
}

internal class AnsiResponseParser () : AnsiResponseParserBase (new StringHeld ())
{
    /// <summary>
    ///     <para>
    ///         Delegate for handling unrecognized escape codes. Default behaviour
    ///         is to return <see langword="false"/> which simply releases the
    ///         characters back to input stream for downstream processing.
    ///     </para>
    ///     <para>
    ///         Implement a method to handle if you want and return <see langword="true"/> if you want the
    ///         keystrokes 'swallowed' (i.e. not returned to input stream).
    ///     </para>
    /// </summary>
    public Func<string, bool> UnknownResponseHandler { get; set; } = _ => false;

    public string ProcessInput (string input)
    {
        var output = new StringBuilder ();

        ProcessInputBase (
                          i => input [i],
                          i => input [i], // For string there is no T so object is same as char
                          c => AppendOutput (output, (char)c),
                          input.Length);

        return output.ToString ();
    }

    private void AppendOutput (StringBuilder output, char c)
    {
        Logging.Trace ($"AnsiResponseParser releasing '{c}'");
        output.Append (c);
    }

    public string Release ()
    {
        lock (_lockState)
        {
            TryLastMinuteSequences ();

            string output = _heldContent.HeldToString ();
            ResetState ();

            return output;
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldSwallowUnexpectedResponse ()
    {
        lock (_lockState)
        {
            return UnknownResponseHandler.Invoke (_heldContent.HeldToString ());
        }
    }
}
