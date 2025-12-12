using Xunit.Abstractions;

namespace DriverTests.Ansi;

/// <summary>
///     Tests for <see cref="AnsiKeyboardEncoder"/> - verifies Key → ANSI sequence conversion.
/// </summary>
/// <remarks>
///     CoPilot - GitHub Copilot (GPT-4)
/// </remarks>
[Trait ("Category", "AnsiHandling")]
public class AnsiKeyboardEncoderTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Special Keys Tests

    [Theory]
    [InlineData (KeyCode.CursorUp, "\u001B[A")]
    [InlineData (KeyCode.CursorDown, "\u001B[B")]
    [InlineData (KeyCode.CursorRight, "\u001B[C")]
    [InlineData (KeyCode.CursorLeft, "\u001B[D")]
    [InlineData (KeyCode.Home, "\u001B[H")]
    [InlineData (KeyCode.End, "\u001B[F")]
    public void Encode_CursorKeys_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    [InlineData (KeyCode.F1, "\u001BOP")]   // SS3 format
    [InlineData (KeyCode.F2, "\u001BOQ")]
    [InlineData (KeyCode.F3, "\u001BOR")]
    [InlineData (KeyCode.F4, "\u001BOS")]
    [InlineData (KeyCode.F5, "\u001B[15~")] // CSI format
    [InlineData (KeyCode.F6, "\u001B[17~")]
    [InlineData (KeyCode.F7, "\u001B[18~")]
    [InlineData (KeyCode.F8, "\u001B[19~")]
    [InlineData (KeyCode.F9, "\u001B[20~")]
    [InlineData (KeyCode.F10, "\u001B[21~")]
    [InlineData (KeyCode.F11, "\u001B[23~")]
    [InlineData (KeyCode.F12, "\u001B[24~")]
    public void Encode_FunctionKeys_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    [InlineData (KeyCode.Insert, "\u001B[2~")]
    [InlineData (KeyCode.Delete, "\u001B[3~")]
    [InlineData (KeyCode.PageUp, "\u001B[5~")]
    [InlineData (KeyCode.PageDown, "\u001B[6~")]
    public void Encode_EditingKeys_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    [InlineData (KeyCode.Tab, "\t")]
    [InlineData (KeyCode.Enter, "\r")]
    [InlineData (KeyCode.Backspace, "\x7F")]
    [InlineData (KeyCode.Esc, "\u001B")]
    public void Encode_SpecialCharacters_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → Hex: {BitConverter.ToString (System.Text.Encoding.ASCII.GetBytes (result))}");
        Assert.Equal (expectedSequence, result);
    }

    #endregion

    #region Regular Character Tests

    [Theory]
    [InlineData (KeyCode.A, "a")]
    [InlineData (KeyCode.B, "b")]
    [InlineData (KeyCode.Z, "z")]
    public void Encode_LettersWithoutShift_ProducesLowercase (KeyCode keyCode, string expectedChar)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result}");
        Assert.Equal (expectedChar, result);
    }

    [Theory]
    [InlineData (KeyCode.A, "A")]
    [InlineData (KeyCode.B, "B")]
    [InlineData (KeyCode.Z, "Z")]
    public void Encode_LettersWithShift_ProducesUppercase (KeyCode keyCode, string expectedChar)
    {
        // Arrange
        Key key = new Key (keyCode).WithShift;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} + Shift → {result}");
        Assert.Equal (expectedChar, result);
    }

    #endregion

    #region Modifier Tests

    [Theory]
    [InlineData (KeyCode.A, 1)]   // Ctrl+A = 0x01
    [InlineData (KeyCode.B, 2)]   // Ctrl+B = 0x02
    [InlineData (KeyCode.C, 3)]   // Ctrl+C = 0x03
    [InlineData (KeyCode.Z, 26)]  // Ctrl+Z = 0x1A
    public void Encode_CtrlLetters_ProducesControlCode (KeyCode keyCode, int expectedControlCode)
    {
        // Arrange
        Key key = new Key (keyCode).WithCtrl;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Ctrl+{keyCode} → 0x{(int)result [0]:X2}");
        Assert.Single (result);
        Assert.Equal (expectedControlCode, result [0]);
    }

    [Theory]
    [InlineData (KeyCode.A, "a")]
    [InlineData (KeyCode.B, "b")]
    [InlineData (KeyCode.Z, "z")]
    public void Encode_AltLetters_ProducesEscPrefixed (KeyCode keyCode, string expectedChar)
    {
        // Arrange
        Key key = new Key (keyCode).WithAlt;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Alt+{keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (2, result.Length);
        Assert.Equal ('\u001B', result [0]);
        Assert.Equal (expectedChar, result [1].ToString ());
    }

    [Theory]
    [InlineData (KeyCode.A, 1)]
    [InlineData (KeyCode.C, 3)]
    public void Encode_CtrlAltLetters_ProducesEscPrefixedControlCode (KeyCode keyCode, int expectedControlCode)
    {
        // Arrange
        Key key = new Key (keyCode).WithCtrl.WithAlt;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Ctrl+Alt+{keyCode} → ESC + 0x{(int)result [1]:X2}");
        Assert.Equal (2, result.Length);
        Assert.Equal ('\u001B', result [0]);
        Assert.Equal (expectedControlCode, result [1]);
    }

    [Theory]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.Delete)]
    public void Encode_AltSpecialKeys_ProducesDoubleEscPrefixed (KeyCode keyCode)
    {
        // Arrange
        Key key = new Key (keyCode).WithAlt;
        string baseSequence = AnsiKeyboardEncoder.Encode (new (keyCode));

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Alt+{keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.StartsWith ("\u001B", result);
        Assert.Equal ($"\u001B{baseSequence}", result);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.CursorDown)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.F5)]
    [InlineData (KeyCode.F12)]
    [InlineData (KeyCode.Delete)]
    [InlineData (KeyCode.Home)]
    [InlineData (KeyCode.End)]
    public void Encode_RoundTrip_MatchesParserOutput (KeyCode keyCode)
    {
        // Arrange
        Key originalKey = new (keyCode);
        AnsiKeyboardParser parser = new ();

        // Act - Encode Key → ANSI
        string ansiSequence = AnsiKeyboardEncoder.Encode (originalKey);
        _output.WriteLine ($"{keyCode} → {ansiSequence.Replace ("\u001B", "ESC")}");

        // Act - Parse ANSI → Key
        AnsiKeyboardParserPattern? pattern = parser.IsKeyboard (ansiSequence);
        Key? parsedKey = pattern?.GetKey (ansiSequence);

        // Assert
        Assert.NotNull (parsedKey);
        Assert.Equal (originalKey.KeyCode, parsedKey.KeyCode);
    }

    #endregion
}
