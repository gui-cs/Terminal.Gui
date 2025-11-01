namespace UnitTests_Parallelizable.ConsoleDriverTests;

public class ConsoleKeyMappingTests
{
    [Theory]
    [InlineData ('a', ConsoleKey.A, false, false, false, (KeyCode)'a')]
    [InlineData ('A', ConsoleKey.A, true, false, false, KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('á', ConsoleKey.A, false, false, false, (KeyCode)'á')]
    [InlineData ('Á', ConsoleKey.A, true, false, false, (KeyCode)'Á' | KeyCode.ShiftMask)]
    [InlineData ('à', ConsoleKey.A, false, false, false, (KeyCode)'à')]
    [InlineData ('À', ConsoleKey.A, true, false, false, (KeyCode)'À' | KeyCode.ShiftMask)]
    [InlineData ('5', ConsoleKey.D5, false, false, false, KeyCode.D5)]
    [InlineData ('%', ConsoleKey.D5, true, false, false, (KeyCode)'%' | KeyCode.ShiftMask)]
    [InlineData ('€', ConsoleKey.D5, false, true, true, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask)]
    [InlineData ('?', ConsoleKey.Oem4, true, false, false, (KeyCode)'?' | KeyCode.ShiftMask)]
    [InlineData ('\'', ConsoleKey.Oem4, false, false, false, (KeyCode)'\'')]
    [InlineData ('q', ConsoleKey.Q, false, false, false, (KeyCode)'q')]
    [InlineData ('\0', ConsoleKey.F2, false, false, false, KeyCode.F2)]
    [InlineData ('英', ConsoleKey.None, false, false, false, (KeyCode)'英')]
    [InlineData ('\r', ConsoleKey.Enter, false, false, false, KeyCode.Enter)]
    public void MapConsoleKeyInfoToKeyCode_Also_Return_Modifiers (
        char keyChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool control,
        KeyCode expectedKeyCode
    )
    {
        var consoleKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);
        KeyCode keyCode = ConsoleKeyMapping.MapConsoleKeyInfoToKeyCode (consoleKeyInfo);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    [Theory]
    [InlineData ('a', false, false, false, (KeyCode)'a')]
    [InlineData ('A', true, false, false, KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('á', false, false, false, (KeyCode)'á')]
    [InlineData ('Á', true, false, false, (KeyCode)'Á' | KeyCode.ShiftMask)]
    [InlineData ('à', false, false, false, (KeyCode)'à')]
    [InlineData ('À', true, false, false, (KeyCode)'À' | KeyCode.ShiftMask)]
    [InlineData ('5', false, false, false, KeyCode.D5)]
    [InlineData ('%', true, false, false, (KeyCode)'%' | KeyCode.ShiftMask)]
    [InlineData ('€', false, true, true, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask)]
    [InlineData ('?', true, false, false, (KeyCode)'?' | KeyCode.ShiftMask)]
    [InlineData ('\'', false, false, false, (KeyCode)'\'')]
    [InlineData ('q', false, false, false, (KeyCode)'q')]
    [InlineData ((uint)KeyCode.F2, false, false, false, KeyCode.F2)]
    [InlineData ('英', false, false, false, (KeyCode)'英')]
    [InlineData ('\r', false, false, false, KeyCode.Enter)]
    [InlineData ('\n', false, false, false, (KeyCode)'\n')]
    public void MapToKeyCodeModifiers_Tests (
        uint keyChar,
        bool shift,
        bool alt,
        bool control,
        KeyCode expectedKeyCode
    )
    {
        ConsoleModifiers modifiers = ConsoleKeyMapping.GetModifiers (shift, alt, control);
        var keyCode = (KeyCode)keyChar;
        keyCode = ConsoleKeyMapping.MapToKeyCodeModifiers (modifiers, keyCode);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    [Theory]
    [MemberData (nameof (GetScanCodeData))]
    public void GetScanCodeFromConsoleKeyInfo_Tests (
        char keyChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool control,
        uint expectedScanCode
    )
    {
        var consoleKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);
        uint scanCode = ConsoleKeyMapping.GetScanCodeFromConsoleKeyInfo (consoleKeyInfo);

        Assert.Equal (scanCode, expectedScanCode);
    }

    public static IEnumerable<object []> GetScanCodeData ()
    {
        yield return ['a', ConsoleKey.A, false, false, false, 30];
        yield return ['A', ConsoleKey.A, true, false, false, 30];
        yield return ['á', ConsoleKey.A, false, false, false, 30];
        yield return ['Á', ConsoleKey.A, true, false, false, 30];
        yield return ['à', ConsoleKey.A, false, false, false, 30];
        yield return ['À', ConsoleKey.A, true, false, false, 30];
        yield return ['0', ConsoleKey.D0, false, false, false, 11];
        yield return ['=', ConsoleKey.D0, true, false, false, 11];
        yield return ['}', ConsoleKey.D0, false, true, true, 11];
        yield return ['1', ConsoleKey.D1, false, false, false, 2];
        yield return ['!', ConsoleKey.D1, true, false, false, 2];
        yield return ['2', ConsoleKey.D2, false, false, false, 3];
        yield return ['"', ConsoleKey.D2, true, false, false, 3];
        yield return ['@', ConsoleKey.D2, false, true, true, 3];
        yield return ['3', ConsoleKey.D3, false, false, false, 4];
        yield return ['#', ConsoleKey.D3, true, false, false, 4];
        yield return ['£', ConsoleKey.D3, false, true, true, 4];
        yield return ['4', ConsoleKey.D4, false, false, false, 5];
        yield return ['$', ConsoleKey.D4, true, false, false, 5];
        yield return ['§', ConsoleKey.D4, false, true, true, 5];
        yield return ['5', ConsoleKey.D5, false, false, false, 6];
        yield return ['%', ConsoleKey.D5, true, false, false, 6];
        yield return ['€', ConsoleKey.D5, false, true, true, 6];
        yield return ['6', ConsoleKey.D6, false, false, false, 7];
        yield return ['&', ConsoleKey.D6, true, false, false, 7];
        yield return ['7', ConsoleKey.D7, false, false, false, 8];
        yield return ['/', ConsoleKey.D7, true, false, false, 8];
        yield return ['{', ConsoleKey.D7, false, true, true, 8];
        yield return ['8', ConsoleKey.D8, false, false, false, 9];
        yield return ['(', ConsoleKey.D8, true, false, false, 9];
        yield return ['[', ConsoleKey.D8, false, true, true, 9];
        yield return ['9', ConsoleKey.D9, false, false, false, 10];
        yield return [')', ConsoleKey.D9, true, false, false, 10];
        yield return [']', ConsoleKey.D9, false, true, true, 10];
        yield return ['´', ConsoleKey.Oem1, false, false, false, 27];
        yield return ['`', ConsoleKey.Oem1, true, false, false, 27];
        yield return ['~', ConsoleKey.Oem2, false, false, false, 43];
        yield return ['^', ConsoleKey.Oem2, true, false, false, 43];
        yield return ['ç', ConsoleKey.Oem3, false, false, false, 39];
        yield return ['Ç', ConsoleKey.Oem3, true, false, false, 39];
        yield return ['\'', ConsoleKey.Oem4, false, false, false, 12];
        yield return ['?', ConsoleKey.Oem4, true, false, false, 12];
        yield return ['\\', ConsoleKey.Oem5, false, true, true, 41];
        yield return ['|', ConsoleKey.Oem5, true, false, false, 41];
        yield return ['«', ConsoleKey.Oem6, false, true, true, 13];
        yield return ['»', ConsoleKey.Oem6, true, false, false, 13];
        yield return ['º', ConsoleKey.Oem7, false, true, true, 40];
        yield return ['ª', ConsoleKey.Oem7, true, false, false, 40];
        yield return ['+', ConsoleKey.OemPlus, false, true, true, 26];
        yield return ['*', ConsoleKey.OemPlus, true, false, false, 26];
        yield return ['¨', ConsoleKey.OemPlus, false, true, true, 26];
        yield return [',', ConsoleKey.OemComma, false, true, true, 51];
        yield return [';', ConsoleKey.OemComma, true, false, false, 51];
        yield return ['.', ConsoleKey.OemPeriod, false, true, true, 52];
        yield return [':', ConsoleKey.OemPeriod, true, false, false, 52];
        yield return ['-', ConsoleKey.OemMinus, false, true, true, 53];
        yield return ['_', ConsoleKey.OemMinus, true, false, false, 53];
        yield return ['q', ConsoleKey.Q, false, false, false, 16];
        yield return ['\0', ConsoleKey.F2, false, false, false, 60];
        yield return ['英', ConsoleKey.None, false, false, false, 0];
        yield return ['英', ConsoleKey.None, true, false, false, 0];
    }

    [Theory]
    [MemberData (nameof (UnShiftedChars))]
    public void GetKeyChar_Shifted_Char_From_UnShifted_Char (
        char unicodeChar,
        char expectedKeyChar,
        KeyCode expectedKeyCode
    )
    {
        ConsoleModifiers modifiers = ConsoleKeyMapping.GetModifiers (true, false, false);
        uint keyChar = ConsoleKeyMapping.GetKeyChar (unicodeChar, modifiers);
        Assert.Equal (keyChar, expectedKeyChar);

        var keyCode = (KeyCode)keyChar;
        keyCode = ConsoleKeyMapping.MapToKeyCodeModifiers (modifiers, keyCode);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    public static IEnumerable<object []> UnShiftedChars =>
        new List<object []>
        {
            new object [] { 'a', 'A', KeyCode.A | KeyCode.ShiftMask },
            new object [] { 'z', 'Z', KeyCode.Z | KeyCode.ShiftMask },
            new object [] { 'á', 'Á', (KeyCode)'Á' | KeyCode.ShiftMask },
            new object [] { 'à', 'À', (KeyCode)'À' | KeyCode.ShiftMask },
            new object [] { 'ý', 'Ý', (KeyCode)'Ý' | KeyCode.ShiftMask },
            new object [] { '1', '!', (KeyCode)'!' | KeyCode.ShiftMask },
            new object [] { '2', '"', (KeyCode)'"' | KeyCode.ShiftMask },
            new object [] { '3', '#', (KeyCode)'#' | KeyCode.ShiftMask },
            new object [] { '4', '$', (KeyCode)'$' | KeyCode.ShiftMask },
            new object [] { '5', '%', (KeyCode)'%' | KeyCode.ShiftMask },
            new object [] { '6', '&', (KeyCode)'&' | KeyCode.ShiftMask },
            new object [] { '7', '/', (KeyCode)'/' | KeyCode.ShiftMask },
            new object [] { '8', '(', (KeyCode)'(' | KeyCode.ShiftMask },
            new object [] { '9', ')', (KeyCode)')' | KeyCode.ShiftMask },
            new object [] { '0', '=', (KeyCode)'=' | KeyCode.ShiftMask },
            new object [] { '\\', '|', (KeyCode)'|' | KeyCode.ShiftMask },
            new object [] { '\'', '?', (KeyCode)'?' | KeyCode.ShiftMask },
            new object [] { '«', '»', (KeyCode)'»' | KeyCode.ShiftMask },
            new object [] { '+', '*', (KeyCode)'*' | KeyCode.ShiftMask },
            new object [] { '´', '`', (KeyCode)'`' | KeyCode.ShiftMask },
            new object [] { 'º', 'ª', (KeyCode)'ª' | KeyCode.ShiftMask },
            new object [] { '~', '^', (KeyCode)'^' | KeyCode.ShiftMask },
            new object [] { '<', '>', (KeyCode)'>' | KeyCode.ShiftMask },
            new object [] { ',', ';', (KeyCode)';' | KeyCode.ShiftMask },
            new object [] { '.', ':', (KeyCode)':' | KeyCode.ShiftMask },
            new object [] { '-', '_', (KeyCode)'_' | KeyCode.ShiftMask }
        };

    [Theory]
    [MemberData (nameof (ShiftedChars))]
    public void GetKeyChar_UnShifted_Char_From_Shifted_Char (
        char unicodeChar,
        char expectedKeyChar,
        KeyCode expectedKeyCode
    )
    {
        ConsoleModifiers modifiers = ConsoleKeyMapping.GetModifiers (false, false, false);
        uint keyChar = ConsoleKeyMapping.GetKeyChar (unicodeChar, modifiers);
        Assert.Equal (keyChar, expectedKeyChar);

        var keyCode = (KeyCode)keyChar;
        keyCode = ConsoleKeyMapping.MapToKeyCodeModifiers (modifiers, keyCode);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    public static IEnumerable<object []> ShiftedChars =>
        new List<object []>
        {
            new object [] { 'A', 'a', (KeyCode)'a' },
            new object [] { 'Z', 'z', (KeyCode)'z' },
            new object [] { 'Á', 'á', (KeyCode)'á' },
            new object [] { 'À', 'à', (KeyCode)'à' },
            new object [] { 'Ý', 'ý', (KeyCode)'ý' },
            new object [] { '!', '1', KeyCode.D1 },
            new object [] { '"', '2', KeyCode.D2 },
            new object [] { '#', '3', KeyCode.D3 },
            new object [] { '$', '4', KeyCode.D4 },
            new object [] { '%', '5', KeyCode.D5 },
            new object [] { '&', '6', KeyCode.D6 },
            new object [] { '/', '7', KeyCode.D7 },
            new object [] { '(', '8', KeyCode.D8 },
            new object [] { ')', '9', KeyCode.D9 },
            new object [] { '=', '0', KeyCode.D0 },
            new object [] { '|', '\\', (KeyCode)'\\' },
            new object [] { '?', '\'', (KeyCode)'\'' },
            new object [] { '»', '«', (KeyCode)'«' },
            new object [] { '*', '+', (KeyCode)'+' },
            new object [] { '`', '´', (KeyCode)'´' },
            new object [] { 'ª', 'º', (KeyCode)'º' },
            new object [] { '^', '~', (KeyCode)'~' },
            new object [] { '>', '<', (KeyCode)'<' },
            new object [] { ';', ',', (KeyCode)',' },
            new object [] { ':', '.', (KeyCode)'.' },
            new object [] { '_', '-', (KeyCode)'-' }
        };
}
