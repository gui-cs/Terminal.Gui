using System.Globalization;
using Wcwidth;

namespace Terminal.Gui;

/// <summary>Extends <see cref="System.Text.Rune"/> to support TUI text manipulation.</summary>
public static class RuneExtensions {
    /// <summary>Maximum Unicode code point.</summary>
    public static int MaxUnicodeCodePoint = 0x10FFFF;

    /// <summary>Reports if the provided array of bytes can be encoded as UTF-8.</summary>
    /// <param name="buffer">The byte array to probe.</param>
    /// <value><c>true</c> if is valid; otherwise, <c>false</c>.</value>
    public static bool CanBeEncodedAsRune (byte[] buffer) {
        string str = Encoding.Unicode.GetString (buffer);
        foreach (Rune rune in str.EnumerateRunes ()) {
            if (rune == Rune.ReplacementChar) {
                return false;
            }
        }

        return true;
    }

    /// <summary>Attempts to decode the rune as a surrogate pair to UTF-16.</summary>
    /// <remarks>This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.</remarks>
    /// <param name="rune">The rune to decode.</param>
    /// <param name="chars">The chars if the rune is a surrogate pair. Null otherwise.</param>
    /// <returns><see langword="true"/> if the rune is a valid surrogate pair; <see langword="false"/> otherwise.</returns>
    public static bool DecodeSurrogatePair (this Rune rune, out char[] chars) {
        if (rune.IsSurrogatePair ()) {
            chars = rune.ToString ().ToCharArray ();

            return true;
        }

        chars = null;

        return false;
    }

    /// <summary>Writes into the destination buffer starting at offset the UTF8 encoded version of the rune.</summary>
    /// <remarks>This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.</remarks>
    /// <param name="rune">The rune to encode.</param>
    /// <param name="dest">The destination buffer.</param>
    /// <param name="start">Starting offset to look into.</param>
    /// <param name="count">Number of bytes valid in the buffer, or -1 to make it the length of the buffer.</param>
    /// <returns>he number of bytes written into the destination buffer.</returns>
    public static int Encode (this Rune rune, byte[] dest, int start = 0, int count = -1) {
        byte[] bytes = Encoding.UTF8.GetBytes (rune.ToString ());
        var length = 0;
        for (var i = 0; i < (count == -1 ? bytes.Length : count); i++) {
            if (bytes[i] == 0) {
                break;
            }

            dest[start + i] = bytes[i];
            length++;
        }

        return length;
    }

    /// <summary>Attempts to encode (as UTF-16) a surrogate pair.</summary>
    /// <param name="highSurrogate">The high surrogate code point.</param>
    /// <param name="lowSurrogate">The low surrogate code point.</param>
    /// <param name="result">The encoded rune.</param>
    /// <returns><see langword="true"/> if the encoding succeeded; <see langword="false"/> otherwise.</returns>
    public static bool EncodeSurrogatePair (char highSurrogate, char lowSurrogate, out Rune result) {
        result = default (Rune);
        if (char.IsSurrogatePair (highSurrogate, lowSurrogate)) {
            result = (Rune)char.ConvertToUtf32 (highSurrogate, lowSurrogate);

            return true;
        }

        return false;
    }

    /// <summary>Gets the number of columns the rune occupies in the terminal.</summary>
    /// <remarks>This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.</remarks>
    /// <param name="rune">The rune to measure.</param>
    /// <returns>
    ///     The number of columns required to fit the rune, 0 if the argument is the null character, or -1 if the value is
    ///     not printable, otherwise the number of columns that the rune occupies.
    /// </returns>
    public static int GetColumns (this Rune rune) { return UnicodeCalculator.GetWidth (rune); }

    /// <summary>Get number of bytes required to encode the rune, based on the provided encoding.</summary>
    /// <remarks>This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.</remarks>
    /// <param name="rune">The rune to probe.</param>
    /// <param name="encoding">The encoding used; the default is UTF8.</param>
    /// <returns>The number of bytes required.</returns>
    public static int GetEncodingLength (this Rune rune, Encoding encoding = null) {
        encoding ??= Encoding.UTF8;
        byte[] bytes = encoding.GetBytes (rune.ToString ().ToCharArray ());
        var offset = 0;
        if (bytes[^1] == 0) {
            offset++;
        }

        return bytes.Length - offset;
    }

    /// <summary>Returns <see langword="true"/> if the rune is a combining character.</summary>
    /// <remarks>This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.</remarks>
    /// <param name="rune"></param>
    /// <returns></returns>
    public static bool IsCombiningMark (this Rune rune) {
        UnicodeCategory category = Rune.GetUnicodeCategory (rune);

        return (Rune.GetUnicodeCategory (rune) == UnicodeCategory.NonSpacingMark)
               || (category == UnicodeCategory.SpacingCombiningMark)
               || (category == UnicodeCategory.EnclosingMark);
    }

    /// <summary>Reports whether a rune is a surrogate code point.</summary>
    /// <remarks>This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.</remarks>
    /// <param name="rune">The rune to probe.</param>
    /// <returns><see langword="true"/> if the rune is a surrogate code point; <see langword="false"/> otherwise.</returns>
    public static bool IsSurrogatePair (this Rune rune) { return char.IsSurrogatePair (rune.ToString (), 0); }

    /// <summary>
    ///     Ensures the rune is not a control character and can be displayed by translating characters below 0x20 to
    ///     equivalent, printable, Unicode chars.
    /// </summary>
    /// <remarks>This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.</remarks>
    /// <param name="rune"></param>
    /// <returns></returns>
    public static Rune MakePrintable (this Rune rune) {
        return Rune.IsControl (rune) ? new Rune (rune.Value + 0x2400) : rune;
    }
}
