#nullable enable

namespace Terminal.Gui;

    // Enum to manage the parser's state
    internal enum ParserState
    {
        Normal,
        ExpectingBracket,
        InResponse
    }

    public interface IAnsiResponseParser
    {
        void ExpectResponse (string terminator, Action<string> response);
    }

    internal abstract class AnsiResponseParserBase : IAnsiResponseParser
    {
        protected readonly List<(string terminator, Action<string> response)> expectedResponses = new ();
        private ParserState _state = ParserState.Normal;

        // Current state of the parser
        public ParserState State
        {
            get => _state;
            protected set
            {
                StateChangedAt = DateTime.Now;
                _state = value;
            }
        }

        /// <summary>
        /// When <see cref="State"/> was last changed.
        /// </summary>
        public DateTime StateChangedAt { get; private set; } = DateTime.Now;

        protected readonly HashSet<char> _knownTerminators = new ();

        public AnsiResponseParserBase ()
        {

        // These all are valid terminators on ansi responses,
        // see CSI in https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
        _knownTerminators.Add ('@');
        _knownTerminators.Add ('A');
        _knownTerminators.Add ('B');
        _knownTerminators.Add ('C');
        _knownTerminators.Add ('D');
        _knownTerminators.Add ('E');
        _knownTerminators.Add ('F');
        _knownTerminators.Add ('G');
        _knownTerminators.Add ('G');
        _knownTerminators.Add ('H');
        _knownTerminators.Add ('I');
        _knownTerminators.Add ('J');
        _knownTerminators.Add ('K');
        _knownTerminators.Add ('L');
        _knownTerminators.Add ('M');

        // No - N or O
        _knownTerminators.Add ('P');
        _knownTerminators.Add ('Q');
        _knownTerminators.Add ('R');
        _knownTerminators.Add ('S');
        _knownTerminators.Add ('T');
        _knownTerminators.Add ('W');
        _knownTerminators.Add ('X');
        _knownTerminators.Add ('Z');

        _knownTerminators.Add ('^');
        _knownTerminators.Add ('`');
        _knownTerminators.Add ('~');

        _knownTerminators.Add ('a');
        _knownTerminators.Add ('b');
        _knownTerminators.Add ('c');
        _knownTerminators.Add ('d');
        _knownTerminators.Add ('e');
        _knownTerminators.Add ('f');
        _knownTerminators.Add ('g');
        _knownTerminators.Add ('h');
        _knownTerminators.Add ('i');

        _knownTerminators.Add ('l');
        _knownTerminators.Add ('m');
        _knownTerminators.Add ('n');

        _knownTerminators.Add ('p');
        _knownTerminators.Add ('q');
        _knownTerminators.Add ('r');
        _knownTerminators.Add ('s');
        _knownTerminators.Add ('t');
        _knownTerminators.Add ('u');
        _knownTerminators.Add ('v');
        _knownTerminators.Add ('w');
        _knownTerminators.Add ('x');
        _knownTerminators.Add ('y');
        _knownTerminators.Add ('z');
        }

        protected void ResetState ()
        {
            State = ParserState.Normal;
            ClearHeld ();
        }

        public abstract void ClearHeld ();
        protected abstract string HeldToString ();
        protected abstract IEnumerable<object> HeldToObjects ();
        protected abstract void AddToHeld (object o);

        // Base method for processing input
        public void ProcessInputBase (
            Func<int, char> getCharAtIndex,
            Func<int, object> getObjectAtIndex,
            Action<object> appendOutput,
            int inputLength)
        {
            var index = 0; // Tracks position in the input string

            while (index < inputLength)
            {
                var currentChar = getCharAtIndex (index);
                var currentObj = getObjectAtIndex (index);

                bool isEscape = currentChar == '\x1B';

                switch (State)
                {
                    case ParserState.Normal:
                        if (isEscape)
                        {
                            // Escape character detected, move to ExpectingBracket state
                            State = ParserState.ExpectingBracket;
                            AddToHeld (currentObj); // Hold the escape character
                        }
                        else
                        {
                            // Normal character, append to output
                            appendOutput (currentObj);
                        }
                        break;

                    case ParserState.ExpectingBracket:
                        if (isEscape)
                        {
                            // Second escape so we must release first
                            ReleaseHeld (appendOutput, ParserState.ExpectingBracket);
                            AddToHeld (currentObj); // Hold the new escape
                        }
                        else
                        if (currentChar == '[')
                        {
                            // Detected '[', transition to InResponse state
                            State = ParserState.InResponse;
                            AddToHeld (currentObj); // Hold the '['
                        }
                        else
                        {
                            // Invalid sequence, release held characters and reset to Normal
                            ReleaseHeld (appendOutput);
                            appendOutput (currentObj); // Add current character
                        }
                        break;

                    case ParserState.InResponse:
                        AddToHeld (currentObj);

                        // Check if the held content should be released
                        if (ShouldReleaseHeldContent ())
                        {
                            ReleaseHeld (appendOutput);
                        }
                        break;
                }

                index++;
            }
        }


        private void ReleaseHeld (Action<object> appendOutput, ParserState newState = ParserState.Normal)
        {
            foreach (var o in HeldToObjects ())
            {
                appendOutput (o);
            }

            State = newState;
            ClearHeld ();
    }

        // Common response handler logic
        protected bool ShouldReleaseHeldContent ()
        {
            string cur = HeldToString ();

            // Check for expected responses
            (string terminator, Action<string> response) matchingResponse = expectedResponses.FirstOrDefault (r => cur.EndsWith (r.terminator));

            if (matchingResponse.response != null)
            {
                DispatchResponse (matchingResponse.response);
                expectedResponses.Remove (matchingResponse);
                return false;
            }

            if (_knownTerminators.Contains (cur.Last ()) && cur.StartsWith (EscSeqUtils.CSI))
            {
                // Detected a response that was not expected
                return true;
            }

            return false; // Continue accumulating
        }


        protected void DispatchResponse (Action<string> response)
        {
            response?.Invoke (HeldToString ());
            ResetState ();
        }

        /// <summary>
        ///     Registers a new expected ANSI response with a specific terminator and a callback for when the response is completed.
        /// </summary>
        public void ExpectResponse (string terminator, Action<string> response) => expectedResponses.Add ((terminator, response));
    }

    internal class AnsiResponseParser<T> : AnsiResponseParserBase
    {
        private readonly List<Tuple<char, T>> held = new ();

        public IEnumerable<Tuple<char, T>> ProcessInput (params Tuple<char, T> [] input)
        {
            var output = new List<Tuple<char, T>> ();
            ProcessInputBase (
                              i => input [i].Item1,
                              i => input [i],
                              c => output.Add ((Tuple<char, T>)c),
                              input.Length);
            return output;
        }

        public IEnumerable<Tuple<char, T>> Release ()
        {
            foreach (var h in held)
            {
                yield return h;
            }
            ResetState ();
        }

        public override void ClearHeld () => held.Clear ();

        protected override string HeldToString () => new string (held.Select (h => h.Item1).ToArray ());

        protected override IEnumerable<object> HeldToObjects () => held;

        protected override void AddToHeld (object o) => held.Add ((Tuple<char, T>)o);


}

    internal class AnsiResponseParser : AnsiResponseParserBase
    {
        private readonly StringBuilder held = new ();

        public string ProcessInput (string input)
        {
            var output = new StringBuilder ();
            ProcessInputBase (
                              i => input [i],
                              i => input [i], // For string there is no T so object is same as char
                              c => output.Append ((char)c),
                              input.Length);
            return output.ToString ();
        }
        public string Release ()
        {
            var output = held.ToString ();
            ResetState ();

            return output;
        }
        public override void ClearHeld () => held.Clear ();

        protected override string HeldToString () => held.ToString ();

        protected override IEnumerable<object> HeldToObjects () => held.ToString().Select(c => (object) c).ToArray ();
        protected override void AddToHeld (object o) => held.Append ((char)o);
    }