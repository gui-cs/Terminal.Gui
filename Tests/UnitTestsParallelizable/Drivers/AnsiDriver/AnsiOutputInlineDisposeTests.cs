// Claude - Opus 4.7

namespace DriverTests.AnsiDriver;

/// <summary>
///     Tests for <see cref="AnsiOutput.Dispose"/> in <see cref="AppModel.Inline"/> mode.
///     Verifies that the cursor is parked on the last row of the inline region (regression
///     guard for issue #5166: <c>lastInlineRow</c> off-by-one parked cursor one row past
///     the region, scrolling the host terminal on shutdown).
/// </summary>
[Collection ("Driver Tests")]
public class AnsiOutputInlineDisposeTests
{
    [Theory]
    [Trait ("Category", "LowLevelDriver")]
    [InlineData (1, 1)]
    [InlineData (1, 5)]
    [InlineData (4, 6)]
    [InlineData (10, 1)]
    [InlineData (10, 24)]
    public void GetInlineLastRow_ReturnsLastRowOfRegion_NotOnePast (int regionY, int regionHeight)
    {
        // Arrange
        Rectangle appScreen = new (0, regionY, 80, regionHeight);
        int expected = regionY + regionHeight - 1;
        int forbidden = regionY + regionHeight;

        // Act
        int actual = AnsiOutput.GetInlineLastRow (appScreen);

        // Assert
        Assert.Equal (expected, actual);
        Assert.NotEqual (forbidden, actual);
    }

    [Theory]
    [Trait ("Category", "LowLevelDriver")]
    [InlineData (1, 1)]
    [InlineData (1, 5)]
    [InlineData (4, 6)]
    [InlineData (10, 1)]
    [InlineData (10, 24)]
    public void Dispose_Inline_ParksCursorOnLastRowOfRegion (int regionY, int regionHeight)
    {
        // Arrange
        Rectangle appScreen = new (0, regionY, 80, regionHeight);
        AnsiOutput output = new (AppModel.Inline)
        {
            AppScreenGetter = () => appScreen
        };

        // Act
        output.Dispose ();
        string written = output.GetLastOutput ();

        // Assert
        // ANSI cursor positioning is 1-indexed. appScreen.Y is already the 1-indexed
        // terminal row offset for the inline region, so the last row is Y + Height - 1.
        int expectedAnsiRow = regionY + regionHeight - 1;
        int forbiddenAnsiRow = regionY + regionHeight;
        string expectedSequence = EscSeqUtils.CSI_SetCursorPosition (expectedAnsiRow, 1);
        string forbiddenSequence = EscSeqUtils.CSI_SetCursorPosition (forbiddenAnsiRow, 1);

        Assert.Contains (expectedSequence, written);
        Assert.DoesNotContain (forbiddenSequence, written);
    }
}
