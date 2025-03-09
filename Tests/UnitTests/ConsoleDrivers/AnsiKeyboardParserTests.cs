using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.ConsoleDrivers;
public class AnsiKeyboardParserTests
{
    private readonly AnsiKeyboardParser _parser = new ();

    public static IEnumerable<object []> GetKeyboardTestData ()
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
        yield return new object [] { "\u001b[Z", null };
        yield return new object [] { "\u001b[invalid", null };
        yield return new object [] { "\u001b[1", null };
        yield return new object [] { "\u001b[AB", null };
        yield return new object [] { "\u001b[;A", null };
    }

    // Consolidated test for all keyboard events (e.g., arrow keys)
    [Theory]
    [MemberData (nameof (GetKeyboardTestData))]
    public void ProcessKeyboardInput_ReturnsCorrectKey (string input, Key? expectedKey)
    {
        // Act
        Key? result = _parser.IsKeyboard (input)?.GetKey (input);

        // Assert
        Assert.Equal (expectedKey, result); // Verify the returned key matches the expected one
    }
}
