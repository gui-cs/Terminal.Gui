using System.Buffers;

namespace Terminal.Gui.Text;

/// <summary>Extensions to <see cref="string"/> to support TUI text manipulation.</summary>
public static class StringExtensions
{
    /// <param name="str">The string to decode.</param>
    extension (string str)
    {
        /// <summary>Unpacks the last UTF-8 encoding in the string.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <param name="end">Index in string to stop at; if -1, use the buffer length.</param>
        /// <returns></returns>
        public (Rune rune, int size) DecodeLastRune (int end = -1)
        {
            if (string.IsNullOrEmpty (str))
            {
                return (Rune.ReplacementChar, 1);
            }

            // Find the target rune index without allocating
            var currentIndex = 0;
            Rune targetRune = Rune.ReplacementChar;

            foreach (Rune rune in str.EnumerateRunes ())
            {
                if (end == -1 || currentIndex <= end)
                {
                    targetRune = rune;
                }
                else
                {
                    break;
                }
                currentIndex++;
            }

            // Get UTF-8 byte size using stackalloc
            Span<byte> utf8Bytes = stackalloc byte [4];
            int bytesWritten = targetRune.EncodeToUtf8 (utf8Bytes);

            return (targetRune, bytesWritten);
        }

        /// <summary>Unpacks the first UTF-8 encoding in the string and returns the rune and its width in bytes.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <param name="start">Starting offset.</param>
        /// <param name="count">Number of bytes in the buffer, or -1 to make it the length of the buffer.</param>
        /// <returns></returns>
        public (Rune Rune, int Size) DecodeRune (int start = 0, int count = -1)
        {
            if (string.IsNullOrEmpty (str))
            {
                return (Rune.ReplacementChar, 1);
            }

            // Find the rune at start index without allocating
            var currentIndex = 0;
            Rune targetRune = Rune.ReplacementChar;

            foreach (Rune rune in str.EnumerateRunes ())
            {
                if (currentIndex == start)
                {
                    targetRune = rune;

                    break;
                }
                currentIndex++;
            }

            // Get UTF-8 byte size using stackalloc
            Span<byte> utf8Bytes = stackalloc byte [4];
            int bytesWritten = targetRune.EncodeToUtf8 (utf8Bytes);

            if (count != -1 && bytesWritten < count)
            {
                return (Rune.ReplacementChar, 1);
            }

            return (targetRune, bytesWritten);
        }

        /// <summary>Gets the number of columns the string occupies in the terminal.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <param name="ignoreLessThanZero">Indicates whether to ignore values ​​less than zero, such as control keys.</param>
        /// <returns></returns>
        public int GetColumns (bool ignoreLessThanZero = true)
        {
            if (string.IsNullOrEmpty (str))
            {
                return 0;
            }

            var total = 0;

            foreach (string grapheme in GraphemeHelper.GetGraphemes (str))
            {
                // Get the maximum rune width within this grapheme cluster
                int clusterWidth = grapheme.EnumerateRunes ()
                                           .Sum (r =>
                                                 {
                                                     int w = r.GetColumns ();

                                                     return ignoreLessThanZero && w < 0 ? 0 : w;
                                                 });

                // Clamp to realistic max display width
                if (clusterWidth > 2)
                {
                    clusterWidth = 2;
                }

                total += clusterWidth;
            }

            return total;
        }

        /// <summary>Gets the number of runes in the string.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <returns></returns>
        public int GetRuneCount () => str.EnumerateRunes ().Count ();
    }

    /// <param name="stringSpan">A <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to check.</param>
    extension (ReadOnlySpan<char> stringSpan)
    {
        /// <summary>
        ///     Determines if this <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> is composed entirely of ASCII
        ///     digits.
        /// </summary>
        /// <returns>
        ///     A <see langword="bool"/> indicating if all elements of the <see cref="ReadOnlySpan{T}"/> are ASCII digits (
        ///     <see langword="true"/>) or not (<see langword="false"/>
        /// </returns>
        public bool IsAllAsciiDigits () => !stringSpan.IsEmpty && stringSpan.IndexOfAnyExcept ("0123456789") == -1;

        /// <summary>
        ///     Determines if this <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> is composed entirely of ASCII
        ///     hex digits.
        /// </summary>
        /// <returns>
        ///     A <see langword="bool"/> indicating if all elements of the <see cref="ReadOnlySpan{T}"/> are ASCII hex digits (
        ///     <see langword="true"/>) or not (<see langword="false"/>
        /// </returns>
        public bool IsAllAsciiHexDigits () =>

            // ReSharper disable once StringLiteralTypo
            !stringSpan.IsEmpty && stringSpan.IndexOfAnyExcept ("0123456789ABCDEFabcdef") == -1;
    }

    /// <param name="str">The text to repeat.</param>
    extension (string str)
    {
        /// <summary>Repeats the string <paramref name="n"/> times.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <param name="n">Number of times to repeat the text.</param>
        /// <returns>The text repeated if <paramref name="n"/> is greater than zero, otherwise <see langword="null"/>.</returns>
        public string? Repeat (int n)
        {
            if (n <= 0)
            {
                return null;
            }

            if (string.IsNullOrEmpty (str) || n == 1)
            {
                return str;
            }

            return new StringBuilder (str.Length * n).Insert (0, str, n).ToString ();
        }

        /// <summary>Converts the string into a <see cref="List{Rune}"/>.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <returns></returns>
        public List<Rune> ToRuneList () => str.EnumerateRunes ().ToList ();

        /// <summary>Converts the string into a <see cref="Rune"/> array.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <returns></returns>
        public Rune [] ToRunes () => str.EnumerateRunes ().ToArray ();
    }

    /// <summary>Converts a <see cref="Rune"/> generic collection into a string.</summary>
    /// <param name="runes">The enumerable rune to convert.</param>
    /// <returns></returns>
    public static string ToString (IEnumerable<Rune> runes)
    {
        const int MAX_CHARS_PER_RUNE = 2;
        const int MAX_STACKALLOC_TEXT_BUFFER_SIZE = 1048; // ~2 kB

        // If rune count is easily available use stackalloc buffer or alternatively rented array.
        if (runes.TryGetNonEnumeratedCount (out int count))
        {
            if (count == 0)
            {
                return string.Empty;
            }

            char []? rentedBufferArray = null;

            try
            {
                int maxRequiredTextBufferSize = count * MAX_CHARS_PER_RUNE;

                Span<char> textBuffer = maxRequiredTextBufferSize <= MAX_STACKALLOC_TEXT_BUFFER_SIZE
                                            ? stackalloc char [maxRequiredTextBufferSize]
                                            : rentedBufferArray = ArrayPool<char>.Shared.Rent (maxRequiredTextBufferSize);

                Span<char> remainingBuffer = textBuffer;

                foreach (Rune rune in runes)
                {
                    int charsWritten = rune.EncodeToUtf16 (remainingBuffer);
                    remainingBuffer = remainingBuffer [charsWritten..];
                }

                ReadOnlySpan<char> text = textBuffer [..^remainingBuffer.Length];

                return text.ToString ();
            }
            finally
            {
                if (rentedBufferArray != null)
                {
                    ArrayPool<char>.Shared.Return (rentedBufferArray);
                }
            }
        }

        // Fallback to StringBuilder append.
        StringBuilder stringBuilder = new ();
        Span<char> runeBuffer = stackalloc char [MAX_CHARS_PER_RUNE];

        foreach (Rune rune in runes)
        {
            int charsWritten = rune.EncodeToUtf16 (runeBuffer);
            ReadOnlySpan<char> runeChars = runeBuffer [..charsWritten];
            stringBuilder.Append (runeChars);
        }

        return stringBuilder.ToString ();
    }

    /// <summary>Converts a byte generic collection into a string in the provided encoding (default is UTF8)</summary>
    /// <param name="bytes">The enumerable byte to convert.</param>
    /// <param name="encoding">The encoding to be used.</param>
    /// <returns></returns>
    public static string ToString (IEnumerable<byte> bytes, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        return encoding.GetString (bytes.ToArray ());
    }

    /// <summary>Converts a <see cref="string"/> generic collection into a string.</summary>
    /// <param name="strings">The enumerable string to convert.</param>
    /// <returns></returns>
    public static string ToString (IEnumerable<string> strings) => string.Concat (strings);

    /// <param name="str">The string to convert.</param>
    extension (string str)
    {
        /// <summary>Converts the string into a <see cref="List{String}"/>.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <returns></returns>
        public List<string> ToStringList ()
        {
            List<string> strings = [];

            foreach (string grapheme in GraphemeHelper.GetGraphemes (str))
            {
                strings.Add (grapheme);
            }

            return strings;
        }

        /// <summary>Reports whether a string is a surrogate code point.</summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <returns><see langword="true"/> if the string is a surrogate code point; <see langword="false"/> otherwise.</returns>
        public bool IsSurrogatePair ()
        {
            if (str.Length != 2)
            {
                return false;
            }

            var rune = Rune.GetRuneAt (str, 0);

            return rune.IsSurrogatePair ();
        }

        /// <summary>
        ///     Ensures the text is not a control character and can be displayed by translating characters below 0x20 to
        ///     equivalent, printable, Unicode chars.
        /// </summary>
        /// <remarks>This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.</remarks>
        /// <returns></returns>
        public string MakePrintable ()
        {
            if (str.Length > 1)
            {
                return str;
            }

            char ch = str [0];

            return char.IsControl (ch) ? new string ((char)(ch + 0x2400), 1) : str;
        }
    }
}
