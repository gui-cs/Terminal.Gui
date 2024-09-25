using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Terminal.Gui;

/// <summary>
///     Json converter for <see cref="Rune"/>.
///     <para>
///         If the Rune is printable, it will be serialized as the glyph; otherwise the \u format (e.g. "\\u2611") is used.
///     </para>
///     <para>
///         Supports deserializing as one of:
///         - unicode glyph in a string (e.g. "☑")
///         - U+hex format in a string  (e.g. "U+2611")
///         - \u format in a string (e.g. "\\u2611")
///         - A decimal number (e.g. 97 for "a")
///     </para>
/// </summary>
internal class RuneJsonConverter : JsonConverter<Rune>
{
    public override Rune Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
            {
                string value = reader.GetString ();
                int first = RuneExtensions.MaxUnicodeCodePoint + 1;
                int second = RuneExtensions.MaxUnicodeCodePoint + 1;

                if (value.StartsWith ("U+", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith ("\\U", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle encoded single char, surrogate pair, or combining mark + char
                    uint [] codePoints = Regex.Matches (value, @"(?:\\[uU]\+?|U\+)([0-9A-Fa-f]{1,8})")
                                              .Select (
                                                       match => uint.Parse (
                                                                            match.Groups [1].Value,
                                                                            NumberStyles.HexNumber
                                                                           )
                                                      )
                                              .ToArray ();

                    if (codePoints.Length == 0 || codePoints.Length > 2)
                    {
                        throw new JsonException ($"Invalid Rune: {value}.");
                    }

                    if (codePoints.Length > 0)
                    {
                        first = (int)codePoints [0];
                    }

                    if (codePoints.Length == 2)
                    {
                        second = (int)codePoints [1];
                    }
                }
                else
                {
                    // Handle single character, surrogate pair, or combining mark + char
                    if (value.Length == 0 || value.Length > 2)
                    {
                        throw new JsonException ($"Invalid Rune: {value}.");
                    }

                    if (value.Length > 0)
                    {
                        first = value [0];
                    }

                    if (value.Length == 2)
                    {
                        second = value [1];
                    }
                }

                Rune result;

                if (second == RuneExtensions.MaxUnicodeCodePoint + 1)
                {
                    // Single codepoint
                    if (!Rune.TryCreate (first, out result))
                    {
                        throw new JsonException ($"Invalid Rune: {value}.");
                    }

                    return result;
                }

                // Surrogate pair?
                if (Rune.TryCreate ((char)first, (char)second, out result))
                {
                    return result;
                }

                if (!Rune.IsValid (second))
                {
                    throw new JsonException ($"The second codepoint is not valid: {second} in ({value})");
                }

                var cm = new Rune (second);

                if (!cm.IsCombiningMark ())
                {
                    throw new JsonException ($"The second codepoint is not a combining mark: {cm} in ({value})");
                }

                // not a surrogate pair, so a combining mark + char?
                string combined = string.Concat ((char)first, (char)second).Normalize ();

                if (!Rune.IsValid (combined [0]))
                {
                    throw new JsonException ($"Invalid combined Rune ({value})");
                }

                return new (combined [0]);
            }
            case JsonTokenType.Number:
            {
                uint num = reader.GetUInt32 ();

                if (Rune.IsValid (num))
                {
                    return new (num);
                }

                throw new JsonException ($"Invalid Rune (not a scalar Unicode value): {num}.");
            }
            default:
                throw new JsonException ($"Unexpected token when parsing Rune: {reader.TokenType}.");
        }
    }

    public override void Write (Utf8JsonWriter writer, Rune value, JsonSerializerOptions options)
    {
        Rune printable = value.MakePrintable ();
        if (printable == Rune.ReplacementChar)
        {
            // Write as /u string
            writer.WriteRawValue ($"\"{value}\"");
        }
        else
        {
            // Write as the actual glyph
            writer.WriteStringValue (value.ToString ());
        }
    }
}
#pragma warning restore 1591
