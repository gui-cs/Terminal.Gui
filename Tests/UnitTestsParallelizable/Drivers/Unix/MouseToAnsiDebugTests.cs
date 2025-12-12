using System.Reflection;
using Xunit.Abstractions;

namespace DriverTests.Unix;

public class MouseToAnsiDebugTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (MouseFlags.WheeledLeft, "68")]
    [InlineData (MouseFlags.WheeledRight, "69")]
    [InlineData (MouseFlags.WheeledUp, "64")]
    [InlineData (MouseFlags.WheeledDown, "65")]
    public void DebugAnsiSequenceGeneration (MouseFlags wheelFlag, string expectedButtonCode)
    {
        // Arrange
        Mouse mouse = new ()
        {
            Flags = wheelFlag,
            ScreenPosition = new (10, 20)
        };

        // Act - Call the internal method via reflection
        MethodInfo? method = typeof (UnixInputProcessor).GetMethod ("MouseToAnsiSequence",
                                                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        string? ansiSeq = method?.Invoke (null, [mouse]) as string;

        // Assert
        _output.WriteLine ($"MouseFlags: {wheelFlag}");
        _output.WriteLine ($"Generated ANSI sequence: {ansiSeq?.Replace ("\u001B", "ESC")}");
        _output.WriteLine ($"Expected button code: {expectedButtonCode}");

        Assert.NotNull (ansiSeq);
        Assert.Contains ($"<{expectedButtonCode};", ansiSeq);
    }
}
