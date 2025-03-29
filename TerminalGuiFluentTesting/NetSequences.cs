namespace TerminalGuiFluentTesting;
class NetSequences
{
    public static ConsoleKeyInfo [] Down = new []
    {
        new ConsoleKeyInfo('\x1B', ConsoleKey.Enter, false, false, false),
        new ConsoleKeyInfo('[', ConsoleKey.None, false, false, false),
        new ConsoleKeyInfo('B', ConsoleKey.None, false, false, false),
    };

    public static ConsoleKeyInfo [] Up = new []
    {
        new ConsoleKeyInfo('\x1B', ConsoleKey.Enter, false, false, false),
        new ConsoleKeyInfo('[', ConsoleKey.None, false, false, false),
        new ConsoleKeyInfo('A', ConsoleKey.None, false, false, false),
    };

    public static ConsoleKeyInfo [] Left = new []
    {
        new ConsoleKeyInfo('\x1B', ConsoleKey.Enter, false, false, false),
        new ConsoleKeyInfo('[', ConsoleKey.None, false, false, false),
        new ConsoleKeyInfo('D', ConsoleKey.None, false, false, false),
    };

    public static ConsoleKeyInfo [] Right = new []
    {
        new ConsoleKeyInfo('\x1B', ConsoleKey.Enter, false, false, false),
        new ConsoleKeyInfo('[', ConsoleKey.None, false, false, false),
        new ConsoleKeyInfo('C', ConsoleKey.None, false, false, false),
    };

    public static IEnumerable<ConsoleKeyInfo> Click (int button, int screenX, int screenY)
    {
        // Adjust for 1-based coordinates
        int adjustedX = screenX + 1;
        int adjustedY = screenY + 1;

        // Mouse press sequence
        var sequence = $"\x1B[<{button};{adjustedX};{adjustedY}M";
        foreach (char c in sequence)
        {
            yield return new ConsoleKeyInfo (c, ConsoleKey.None, false, false, false);
        }

        // Mouse release sequence
        sequence = $"\x1B[<{button};{adjustedX};{adjustedY}m";
        foreach (char c in sequence)
        {
            yield return new ConsoleKeyInfo (c, ConsoleKey.None, false, false, false);
        }
    }

}
