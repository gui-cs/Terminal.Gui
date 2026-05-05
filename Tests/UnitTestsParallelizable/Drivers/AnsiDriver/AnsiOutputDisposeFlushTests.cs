// Claude - Opus 4.7

namespace DriverTests.AnsiDriver;

/// <summary>
///     Tests for <see cref="AnsiOutput.Dispose"/> verifying that the cleanup ANSI choreography
///     (mouse-off, SGR reset, alt-buffer restore / inline cursor park, show cursor) is fully
///     written to the output buffer before <see cref="AnsiOutput.Dispose"/> returns.
/// </summary>
/// <remarks>
///     <para>
///         Regression guard for issue #5165: <see cref="AnsiOutput.Dispose"/> wrote cleanup
///         ANSI sequences but did not flush the underlying native stdout before returning,
///         allowing buffered cleanup bytes to be discarded on process exit. Also verifies
///         that every cleanup sequence is emitted (none are skipped or short-circuited).
///     </para>
///     <para>
///         The native stdout flush itself is a no-op when <c>_platform</c> is
///         <see cref="AnsiPlatform.Degraded"/> (the only platform in headless test
///         environments), so the closest practical proxy is to assert that every cleanup
///         sequence is observable via <see cref="OutputBase.GetLastOutput"/>.
///     </para>
/// </remarks>
[Collection ("Driver Tests")]
public class AnsiOutputDisposeFlushTests
{
    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void Dispose_FullScreen_WritesAllCleanupSequences ()
    {
        // Arrange
        AnsiOutput output = new (AppModel.FullScreen);

        // Act
        output.Dispose ();
        string written = output.GetLastOutput ();

        // Assert — every FullScreen cleanup sequence must be observable.
        Assert.Contains (EscSeqUtils.CSI_DisableMouseEvents, written);
        Assert.Contains (EscSeqUtils.CSI_ResetAttributes, written);
        Assert.Contains (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll, written);
        Assert.Contains (EscSeqUtils.CSI_ShowCursor, written);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void Dispose_Inline_WritesAllCleanupSequences ()
    {
        // Arrange
        Rectangle appScreen = new (0, 1, 80, 5);
        AnsiOutput output = new (AppModel.Inline)
        {
            AppScreenGetter = () => appScreen
        };

        // Act
        output.Dispose ();
        string written = output.GetLastOutput ();

        // Assert — every Inline cleanup sequence must be observable.
        Assert.Contains (EscSeqUtils.CSI_DisableMouseEvents, written);
        Assert.Contains (EscSeqUtils.CSI_ResetAttributes, written);
        Assert.Contains (EscSeqUtils.CSI_ShowCursor, written);

        // Inline mode parks cursor on the last row of the region, then writes a newline.
        int lastInlineRow = AnsiOutput.GetInlineLastRow (appScreen);
        Assert.Contains (EscSeqUtils.CSI_SetCursorPosition (lastInlineRow, 1), written);
        Assert.Contains ("\n", written);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void Dispose_FullScreen_EmitsCleanupSequencesInExpectedOrder ()
    {
        // Arrange
        AnsiOutput output = new (AppModel.FullScreen);

        // Act
        output.Dispose ();
        string written = output.GetLastOutput ();

        // Assert — sequences appear in the documented choreography order.
        // Find the index of each cleanup sequence written during Dispose. Init writes
        // (mouse enable, alt buffer activate, etc.) come before any of these in the buffer,
        // so we look for the *last* occurrence of each cleanup sequence.
        int mouseOff = written.LastIndexOf (EscSeqUtils.CSI_DisableMouseEvents, StringComparison.Ordinal);
        int sgrReset = written.LastIndexOf (EscSeqUtils.CSI_ResetAttributes, StringComparison.Ordinal);
        int altRestore = written.LastIndexOf (
                                              EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll,
                                              StringComparison.Ordinal);
        int showCursor = written.LastIndexOf (EscSeqUtils.CSI_ShowCursor, StringComparison.Ordinal);

        Assert.True (mouseOff >= 0, "Mouse-off sequence not written during Dispose.");
        Assert.True (sgrReset >= 0, "SGR reset sequence not written during Dispose.");
        Assert.True (altRestore >= 0, "Alt-buffer restore sequence not written during Dispose.");
        Assert.True (showCursor >= 0, "Show-cursor sequence not written during Dispose.");

        Assert.True (mouseOff < sgrReset, "Mouse-off must be written before SGR reset.");
        Assert.True (sgrReset < altRestore, "SGR reset must be written before alt-buffer restore.");
        Assert.True (altRestore < showCursor, "Alt-buffer restore must be written before show-cursor.");
    }
}
