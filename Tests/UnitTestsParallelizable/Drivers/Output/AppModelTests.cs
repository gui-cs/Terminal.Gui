// Copilot

namespace DriverTests.Output;

/// <summary>
///     Tests for <see cref="AppModel"/> enum and <see cref="OutputBufferImpl.ClearContents(bool)"/> overload.
/// </summary>
[Collection ("Driver Tests")]
public class AppModelTests
{
    [Fact]
    public void AppModel_FullScreen_IsDefaultValue ()
    {
        // Assert
        Assert.Equal (AppModel.FullScreen, default (AppModel));
    }

    [Fact]
    public void AppModel_Inline_IsDefined ()
    {
        // Assert
        Assert.True (Enum.IsDefined (typeof (AppModel), AppModel.Inline));
    }

    [Fact]
    public void AppModel_FullScreen_IsDefined ()
    {
        // Assert
        Assert.True (Enum.IsDefined (typeof (AppModel), AppModel.FullScreen));
    }

    [Fact]
    public void OutputBufferImpl_ClearContents_DefaultMarksAllCellsDirty ()
    {
        // Arrange
        OutputBufferImpl buffer = new ();
        buffer.SetSize (3, 2);

        // Act — parameterless overload delegates to initiallyDirty: true
        buffer.ClearContents ();

        // Assert
        for (var row = 0; row < 2; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                Assert.True (buffer.Contents! [row, col].IsDirty);
            }

            Assert.True (buffer.DirtyLines [row]);
        }
    }

    [Fact]
    public void OutputBufferImpl_ClearContents_InitiallyDirtyTrue_MarksAllCellsDirty ()
    {
        // Arrange
        OutputBufferImpl buffer = new ();
        buffer.SetSize (3, 2);

        // Act
        buffer.ClearContents (initiallyDirty: true);

        // Assert
        for (var row = 0; row < 2; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                Assert.True (buffer.Contents! [row, col].IsDirty);
            }

            Assert.True (buffer.DirtyLines [row]);
        }
    }

    [Fact]
    public void OutputBufferImpl_ClearContents_InitiallyDirtyFalse_LeavesAllCellsClean ()
    {
        // Arrange
        OutputBufferImpl buffer = new ();
        buffer.SetSize (3, 2);

        // Act
        buffer.ClearContents (initiallyDirty: false);

        // Assert
        for (var row = 0; row < 2; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                Assert.False (buffer.Contents! [row, col].IsDirty);
            }

            Assert.False (buffer.DirtyLines [row]);
        }
    }

    [Fact]
    public void OutputBufferImpl_ClearContents_InitiallyDirtyFalse_CellsStillHaveContent ()
    {
        // Arrange
        OutputBufferImpl buffer = new ();
        buffer.SetSize (2, 1);

        // Act
        buffer.ClearContents (initiallyDirty: false);

        // Assert — cells should still have default space grapheme and attribute
        Assert.Equal (" ", buffer.Contents! [0, 0].Grapheme);
        Assert.Equal (" ", buffer.Contents! [0, 1].Grapheme);
    }
}
