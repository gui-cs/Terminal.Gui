using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Input processor for <see cref="NetInput"/>, deals in <see cref="ConsoleKeyInfo"/> stream
/// </summary>
public class NetInputProcessor : InputProcessor<ConsoleKeyInfo>
{
#pragma warning disable CA2211
    /// <summary>
    ///     Set to true to generate code in <see cref="Logging"/> (verbose only) for test cases in NetInputProcessorTests.
    ///     <remarks>
    ///         This makes the task of capturing user/language/terminal specific keyboard issues easier to
    ///         diagnose. By turning this on and searching logs user can send us exactly the input codes that are released
    ///         to input stream.
    ///     </remarks>
    /// </summary>
    public static bool GenerateTestCasesForKeyPresses = false;
#pragma warning restore CA2211

    /// <inheritdoc/>
    public NetInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) : base (inputBuffer, new NetKeyConverter ()) { }

    /// <inheritdoc/>
    protected override void Process (ConsoleKeyInfo consoleKeyInfo)
    {
        // For building test cases
        if (GenerateTestCasesForKeyPresses)
        {
            Logging.Trace (FormatConsoleKeyInfoForTestCase (consoleKeyInfo));
        }

        foreach (Tuple<char, ConsoleKeyInfo> released in Parser.ProcessInput (Tuple.Create (consoleKeyInfo.KeyChar, consoleKeyInfo)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }

    /// <inheritdoc/>
    protected override void ProcessAfterParsing (ConsoleKeyInfo input)
    {
        var key = KeyConverter.ToKey (input);
        OnKeyDown (key);
        OnKeyUp (key);
    }

    /* For building test cases */
    private static string FormatConsoleKeyInfoForTestCase (ConsoleKeyInfo input)
    {
        string charLiteral = input.KeyChar == '\0' ? @"'\0'" : $"'{input.KeyChar}'";
        var expectedLiteral = "new Rune('todo')";

        return $"new ConsoleKeyInfo({charLiteral}, ConsoleKey.{input.Key}, "
               + $"{input.Modifiers.HasFlag (ConsoleModifiers.Shift).ToString ().ToLower ()}, "
               + $"{input.Modifiers.HasFlag (ConsoleModifiers.Alt).ToString ().ToLower ()}, "
               + $"{input.Modifiers.HasFlag (ConsoleModifiers.Control).ToString ().ToLower ()}), {expectedLiteral}";
    }
}
