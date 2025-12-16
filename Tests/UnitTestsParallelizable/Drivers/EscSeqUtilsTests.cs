using System.Text;

// ReSharper disable HeuristicUnreachableCode

namespace DriverTests;

public class EscSeqUtilsTests
{
    [Fact]
    public void Defaults_Values ()
    {
        Assert.Equal ('\x1b', EscSeqUtils.KeyEsc);
        Assert.Equal ("\x1b[", EscSeqUtils.CSI);
        Assert.Equal ("\x1b[?1003h", EscSeqUtils.CSI_EnableAnyEventMouse);
        Assert.Equal ("\x1b[?1006h", EscSeqUtils.CSI_EnableSgrExtModeMouse);
        Assert.Equal ("\x1b[?1015h", EscSeqUtils.CSI_EnableUrxvtExtModeMouse);
        Assert.Equal ("\x1b[?1003l", EscSeqUtils.CSI_DisableAnyEventMouse);
        Assert.Equal ("\x1b[?1006l", EscSeqUtils.CSI_DisableSgrExtModeMouse);
        Assert.Equal ("\x1b[?1015l", EscSeqUtils.CSI_DisableUrxvtExtModeMouse);
        Assert.Equal ("\x1b[?1003h\x1b[?1015h\u001b[?1006h", EscSeqUtils.CSI_EnableMouseEvents);
        Assert.Equal ("\x1b[?1003l\x1b[?1015l\u001b[?1006l", EscSeqUtils.CSI_DisableMouseEvents);
    }

    [Fact]
    public void GetConsoleInputKey_ConsoleKeyInfo ()
    {
        var cki = new ConsoleKeyInfo ('r', 0, false, false, false);
        var expectedCki = new ConsoleKeyInfo ('r', ConsoleKey.R, false, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, true, false, false);
        expectedCki = new ('r', ConsoleKey.R, true, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, false, true, false);
        expectedCki = new ('r', ConsoleKey.R, false, true, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, false, false, true);
        expectedCki = new ('r', ConsoleKey.R, false, false, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('r', 0, true, true, false);
        expectedCki = new ('r', ConsoleKey.R, true, true, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, false, true, true);
        expectedCki = new ('r', ConsoleKey.R, false, true, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('r', 0, true, true, true);
        expectedCki = new ('r', ConsoleKey.R, true, true, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\u0012', 0, false, false, false);
        expectedCki = new ('\u0012', ConsoleKey.R, false, false, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\0', (ConsoleKey)64, false, false, true);
        expectedCki = new ('\0', ConsoleKey.Spacebar, false, false, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\r', 0, false, false, false);
        expectedCki = new ('\r', ConsoleKey.Enter, false, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\u007f', 0, false, false, false);
        expectedCki = new ('\u007f', ConsoleKey.Backspace, false, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('R', 0, false, false, false);
        expectedCki = new ('R', ConsoleKey.R, true, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());
    }

    [Theory]
    [InlineData (0, 0, $"{EscSeqUtils.CSI}0;0H")]
    [InlineData (int.MaxValue, int.MaxValue, $"{EscSeqUtils.CSI}2147483647;2147483647H")]
    [InlineData (int.MinValue, int.MinValue, $"{EscSeqUtils.CSI}-2147483648;-2147483648H")]
    public void CSI_WriteCursorPosition_ReturnsCorrectEscSeq (int row, int col, string expected)
    {
        StringBuilder builder = new();
        using StringWriter writer = new(builder);

        EscSeqUtils.CSI_WriteCursorPosition (writer, row, col);

        string actual = builder.ToString();
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ('\u001B', KeyCode.Esc)]
    [InlineData ('\r', KeyCode.Enter)]
    [InlineData ('1', KeyCode.D1)]
    [InlineData ('!', (KeyCode)'!')]
    [InlineData ('a', KeyCode.A)]
    [InlineData ('A', KeyCode.A | KeyCode.ShiftMask)]
    public void MapChar_Returns_Modifiers_If_Needed (char ch, KeyCode keyCode)
    {
        ConsoleKeyInfo cki = EscSeqUtils.MapChar (ch);
        Key key = EscSeqUtils.MapKey (cki);
        Key expectedKey = keyCode;

        Assert.Equal (key, expectedKey);
    }
}
