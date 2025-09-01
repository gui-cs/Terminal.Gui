#nullable enable
namespace UnitTests.ConsoleDrivers;

public class AnsiKeyboardParserTests
{
    private readonly AnsiKeyboardParser _parser = new ();

    public static IEnumerable<object? []> GetKeyboardTestData ()
    {
        // Test data for various ANSI escape sequences and their expected Key values
        yield return new object [] { "\u001b[A", Key.CursorUp };
        yield return new object [] { "\u001b[B", Key.CursorDown };
        yield return new object [] { "\u001b[C", Key.CursorRight };
        yield return new object [] { "\u001b[D", Key.CursorLeft };

        // Valid inputs with modifiers
        yield return new object [] { "\u001b[1;2A", Key.CursorUp.WithShift };
        yield return new object [] { "\u001b[1;3A", Key.CursorUp.WithAlt };
        yield return new object [] { "\u001b[1;4A", Key.CursorUp.WithAlt.WithShift };
        yield return new object [] { "\u001b[1;5A", Key.CursorUp.WithCtrl };
        yield return new object [] { "\u001b[1;6A", Key.CursorUp.WithCtrl.WithShift };
        yield return new object [] { "\u001b[1;7A", Key.CursorUp.WithCtrl.WithAlt };
        yield return new object [] { "\u001b[1;8A", Key.CursorUp.WithCtrl.WithAlt.WithShift };

        yield return new object [] { "\u001b[1;2B", Key.CursorDown.WithShift };
        yield return new object [] { "\u001b[1;3B", Key.CursorDown.WithAlt };
        yield return new object [] { "\u001b[1;4B", Key.CursorDown.WithAlt.WithShift };
        yield return new object [] { "\u001b[1;5B", Key.CursorDown.WithCtrl };
        yield return new object [] { "\u001b[1;6B", Key.CursorDown.WithCtrl.WithShift };
        yield return new object [] { "\u001b[1;7B", Key.CursorDown.WithCtrl.WithAlt };
        yield return new object [] { "\u001b[1;8B", Key.CursorDown.WithCtrl.WithAlt.WithShift };

        yield return new object [] { "\u001b[1;2C", Key.CursorRight.WithShift };
        yield return new object [] { "\u001b[1;3C", Key.CursorRight.WithAlt };
        yield return new object [] { "\u001b[1;4C", Key.CursorRight.WithAlt.WithShift };
        yield return new object [] { "\u001b[1;5C", Key.CursorRight.WithCtrl };
        yield return new object [] { "\u001b[1;6C", Key.CursorRight.WithCtrl.WithShift };
        yield return new object [] { "\u001b[1;7C", Key.CursorRight.WithCtrl.WithAlt };
        yield return new object [] { "\u001b[1;8C", Key.CursorRight.WithCtrl.WithAlt.WithShift };

        yield return new object [] { "\u001b[1;2D", Key.CursorLeft.WithShift };
        yield return new object [] { "\u001b[1;3D", Key.CursorLeft.WithAlt };
        yield return new object [] { "\u001b[1;4D", Key.CursorLeft.WithAlt.WithShift };
        yield return new object [] { "\u001b[1;5D", Key.CursorLeft.WithCtrl };
        yield return new object [] { "\u001b[1;6D", Key.CursorLeft.WithCtrl.WithShift };
        yield return new object [] { "\u001b[1;7D", Key.CursorLeft.WithCtrl.WithAlt };
        yield return new object [] { "\u001b[1;8D", Key.CursorLeft.WithCtrl.WithAlt.WithShift };

        // Invalid inputs
        yield return new object [] { "\u001b[Z", null! };
        yield return new object [] { "\u001b[invalid", null! };
        yield return new object [] { "\u001b[1", null! };
        yield return new object [] { "\u001b[AB", null! };
        yield return new object [] { "\u001b[;A", null! };


        // Test data for various ANSI escape sequences and their expected Key values
        yield return new object [] { "\u001b[3;5~", Key.Delete.WithCtrl };

        // Additional special keys
        yield return new object [] { "\u001b[H", Key.Home };
        yield return new object [] { "\u001b[F", Key.End };
        yield return new object [] { "\u001b[2~", Key.InsertChar };
        yield return new object [] { "\u001b[5~", Key.PageUp };
        yield return new object [] { "\u001b[6~", Key.PageDown };

        // Home, End with modifiers
        yield return new object [] { "\u001b[1;2H", Key.Home.WithShift };
        yield return new object [] { "\u001b[1;3H", Key.Home.WithAlt };
        yield return new object [] { "\u001b[1;5F", Key.End.WithCtrl };

        // Insert with modifiers
        yield return new object [] { "\u001b[2;2~", Key.InsertChar.WithShift };
        yield return new object [] { "\u001b[2;3~", Key.InsertChar.WithAlt };
        yield return new object [] { "\u001b[2;5~", Key.InsertChar.WithCtrl };

        // PageUp/PageDown with modifiers
        yield return new object [] { "\u001b[5;2~", Key.PageUp.WithShift };
        yield return new object [] { "\u001b[6;3~", Key.PageDown.WithAlt };
        yield return new object [] { "\u001b[6;5~", Key.PageDown.WithCtrl };

        // Function keys F1-F4 (common ESC O sequences)
        yield return new object [] { "\u001bOP", Key.F1 };
        yield return new object [] { "\u001bOQ", Key.F2 };
        yield return new object [] { "\u001bOR", Key.F3 };
        yield return new object [] { "\u001bOS", Key.F4 };

        // Extended function keys F1-F12 with CSI sequences
        yield return new object [] { "\u001b[11~", Key.F1 };
        yield return new object [] { "\u001b[12~", Key.F2 };
        yield return new object [] { "\u001b[13~", Key.F3 };
        yield return new object [] { "\u001b[14~", Key.F4 };
        yield return new object [] { "\u001b[15~", Key.F5 };
        yield return new object [] { "\u001b[17~", Key.F6 };
        yield return new object [] { "\u001b[18~", Key.F7 };
        yield return new object [] { "\u001b[19~", Key.F8 };
        yield return new object [] { "\u001b[20~", Key.F9 };
        yield return new object [] { "\u001b[21~", Key.F10 };
        yield return new object [] { "\u001b[23~", Key.F11 };
        yield return new object [] { "\u001b[24~", Key.F12 };

        // Function keys with modifiers
        yield return new object [] { "\u001b[1;2P", Key.F1.WithShift };
        yield return new object [] { "\u001b[1;3Q", Key.F2.WithAlt };
        yield return new object [] { "\u001b[1;5R", Key.F3.WithCtrl };
        
    }

    // Consolidated test for all keyboard events (e.g., arrow keys)
    [Theory]
    [MemberData (nameof (GetKeyboardTestData))]
    public void ProcessKeyboardInput_ReturnsCorrectKey (string? input, Key? expectedKey)
    {
        // Act
        Key? result = _parser.IsKeyboard (input)?.GetKey (input);

        // Assert
        Assert.Equal (expectedKey, result); // Verify the returned key matches the expected one
    }
}
