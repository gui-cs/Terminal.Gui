using System.Text;

namespace DriverTests.Output;

[Collection ("Driver Tests")]
public class OutputBaseTests
{
    [Fact]
    public void ToAnsi_SingleCell_NoAttribute_ReturnsGraphemeAndNewline ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Act
        buffer.AddStr ("A");
        string ansi = output.ToAnsi (buffer);

        // Assert: single grapheme plus newline (BuildAnsiForRegion appends a newline per row)
        Assert.Contains ("A" + Environment.NewLine, ansi);
    }

    [Theory]
    [InlineData (true, false)]
    [InlineData (true, true)]
    [InlineData (false, false)]
    [InlineData (false, true)]
    public void ToAnsi_WithAttribute_AppendsCorrectColorSequence_BasedOnIsLegacyConsole_And_Force16Colors (bool isLegacyConsole, bool force16Colors)
    {
        // Arrange
        AnsiOutput output = new () { IsLegacyConsole = isLegacyConsole };

        // Create DriverImpl and associate it with the ANSIOutput to test Sixel output
        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        // Set Force16Colors on the driver (which propagates to output)
        driver.Force16Colors = force16Colors;

        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Use a known RGB color and attribute
        Color fg = new (1, 2, 3);
        Color bg = new (4, 5, 6);
        buffer.CurrentAttribute = new (fg, bg);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: when true color expected, we should see the RGB CSI; otherwise we should see the 16-color CSI
        if (!isLegacyConsole && !force16Colors)
        {
            Assert.Contains ("\u001b[38;2;1;2;3m", ansi);
        }
        else if (!isLegacyConsole && force16Colors)
        {
            string expected16 = EscSeqUtils.CSI_SetForegroundColor (fg.GetAnsiColorCode ());
            Assert.Contains (expected16, ansi);
        }
        else
        {
            var expected16 = (ConsoleColor)fg.GetClosestNamedColor16 ();
            Assert.Equal (ConsoleColor.Black, expected16);
            Assert.DoesNotContain ('\u001b', ansi);
        }

        // Grapheme and newline should always be present
        Assert.Contains ("X" + Environment.NewLine, ansi);

        driver.Dispose ();
    }

    [Fact]
    public void Write_WritesDirtyCellsAndClearsDirtyFlags ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (2, 1);

        // Mark two characters as dirty by writing them into the buffer
        buffer.AddStr ("AB");

        // Sanity: ensure cells are dirty before calling Write
        Assert.True (buffer.Contents! [0, 0].IsDirty);
        Assert.True (buffer.Contents! [0, 1].IsDirty);

        // Act
        output.Write (buffer); // calls OutputBase.Write via ANSIOutput

        // Assert: content was written to the fake output and dirty flags cleared
        Assert.Contains ("AB", output.GetLastOutput ());
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 1].IsDirty);
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Write_Virtual_Or_NonVirtual_Uses_WriteToConsole_And_Clears_Dirty_Flags (bool isLegacyConsole)
    {
        // Arrange
        // ANSIOutput exposes this because it's in test scope
        AnsiOutput output = new () { IsLegacyConsole = isLegacyConsole };
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (3, 1);

        // Write 'A' at col 0 and 'C' at col 2; leave col 1 untouched (not dirty)
        buffer.Move (0, 0);
        buffer.AddStr ("A");
        buffer.Move (2, 0);
        buffer.AddStr ("C");

        // Confirm some dirtiness before to write
        Assert.True (buffer.Contents! [0, 0].IsDirty);
        Assert.True (buffer.Contents! [0, 2].IsDirty);

        // Act
        output.Write (buffer);

        // Assert: both characters were written (use Contains to avoid CI side effects)
        Assert.Contains ("A", output.GetLastOutput ());
        Assert.Contains ("C", output.GetLastOutput ());

        // Dirty flags cleared for the written cells
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void GetLastOutput_Returns_Only_Most_Recent_Frame (bool isLegacyConsole)
    {
        // Arrange
        AnsiOutput output = new () { IsLegacyConsole = isLegacyConsole };
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (3, 1);

        // Write 'A' at col 0 and 'C' at col 2 in the first frame
        buffer.Move (0, 0);
        buffer.AddStr ("A");
        buffer.Move (2, 0);
        buffer.AddStr ("C");

        output.Write (buffer);

        // Write 'X' at col 0 in the second frame
        buffer.Move (0, 0);
        buffer.AddStr ("X");

        output.Write (buffer);

        // Assert: only the second frame's output is returned, not accumulated history
        Assert.Contains ("X", output.GetLastOutput ());
        Assert.DoesNotContain ("A", output.GetLastOutput ());
        Assert.DoesNotContain ("C", output.GetLastOutput ());
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Write_Virtual_Or_NonVirtual_Uses_WriteToConsole_And_Clears_Dirty_Flags_Mixed_Graphemes (bool isLegacyConsole)
    {
        // Arrange
        // ANSIOutput exposes this because it's in test scope
        AnsiOutput output = new () { IsLegacyConsole = isLegacyConsole };
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetWideGlyphReplacement ((Rune)'①');

        buffer.SetSize (3, 1);

        // Write '🦮' at col 0 and 'A' at col 2
        buffer.Move (0, 0);
        buffer.AddStr ("🦮A");

        // After the fix for https://github.com/gui-cs/Terminal.Gui/issues/4258:
        // Writing a wide glyph at column 0 no longer sets column 1 to IsDirty = false.
        // Column 1 retains whatever state it had (in this case, it was initialized as dirty
        // by ClearContents, but may have been cleared by a previous Write call).
        //
        // What we care about is that wide glyphs work correctly and don't prevent
        // other content from being drawn at odd columns.
        Assert.True (buffer.Contents! [0, 0].IsDirty);

        // Column 1 state depends on whether it was cleared by a previous Write - don't assert
        Assert.True (buffer.Contents! [0, 2].IsDirty);

        // Act
        output.Write (buffer);

        Assert.Contains ("🦮", output.GetLastOutput ());
        Assert.Contains ("A", output.GetLastOutput ());

        // Dirty flags cleared for the written cells
        // Column 0 was written (wide glyph)
        Assert.False (buffer.Contents! [0, 0].IsDirty);

        // Column 1 was marked as clean by OutputBase.Write when it processed the wide glyph at column 0
        // See: https://github.com/gui-cs/Terminal.Gui/issues/4466
        Assert.False (buffer.Contents! [0, 1].IsDirty);

        // Column 2 was written ('A')
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        // Now write 'X' at col 1 which invalidates the wide glyph at col 0
        buffer.Move (1, 0);
        buffer.AddStr ("X");

        // Confirm dirtiness state before to write
        Assert.True (buffer.Contents! [0, 0].IsDirty); // Invalidated by writing at col 1
        Assert.True (buffer.Contents! [0, 1].IsDirty); // Just written
        Assert.True (buffer.Contents! [0, 2].IsDirty); // Marked dirty by writing at col 1

        output.Write (buffer);

        Assert.Contains ("①", output.GetLastOutput ());
        Assert.Contains ("X", output.GetLastOutput ());

        // Dirty flags cleared for the written cells
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 1].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);
    }

    // Copilot
    [Fact]
    public void ToAnsi_CellsWithUrl_EmitsOsc8Sequences ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (5, 1);

        buffer.CurrentUrl = "https://example.com";
        buffer.AddStr ("Hello");
        buffer.CurrentUrl = null;

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert
        string expectedStart = EscSeqUtils.OSC_StartHyperlink ("https://example.com");
        string expectedEnd = EscSeqUtils.OSC_EndHyperlink ();

        Assert.Contains (expectedStart, ansi);
        Assert.Contains (expectedEnd, ansi);
        Assert.Contains ("Hello", ansi);
    }

    // Copilot
    [Fact]
    public void ToAnsi_CellsWithDifferentUrls_EmitsCorrectTransitions ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (6, 1);

        buffer.CurrentUrl = "https://one.com";
        buffer.AddStr ("AB");
        buffer.CurrentUrl = "https://two.com";
        buffer.AddStr ("CD");
        buffer.CurrentUrl = null;
        buffer.AddStr ("EF");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: verify hyperlink transitions are emitted in order
        string startOne = EscSeqUtils.OSC_StartHyperlink ("https://one.com");
        string startTwo = EscSeqUtils.OSC_StartHyperlink ("https://two.com");
        string end = EscSeqUtils.OSC_EndHyperlink ();

        int startOneIdx = ansi.IndexOf (startOne, StringComparison.Ordinal);
        int endOneIdx = ansi.IndexOf (end, startOneIdx + startOne.Length, StringComparison.Ordinal);
        int startTwoIdx = ansi.IndexOf (startTwo, endOneIdx + end.Length, StringComparison.Ordinal);
        int endTwoIdx = ansi.IndexOf (end, startTwoIdx + startTwo.Length, StringComparison.Ordinal);
        int nonUrlTextIdx = ansi.IndexOf ("EF", endTwoIdx + end.Length, StringComparison.Ordinal);

        Assert.True (startOneIdx >= 0, "First OSC 8 start not found");
        Assert.True (endOneIdx > startOneIdx, "First OSC 8 end not found after first start");
        Assert.True (startTwoIdx > endOneIdx, "Second OSC 8 start should appear after first OSC 8 end");
        Assert.True (endTwoIdx > startTwoIdx, "Second OSC 8 end not found after second start");
        Assert.True (nonUrlTextIdx > endTwoIdx, "Non-URL text should appear after second OSC 8 end");
    }

    // Copilot
    [Fact]
    public void ToAnsi_CellsWithUrl_ThenNoUrl_ClosesHyperlink ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (6, 1);

        buffer.CurrentUrl = "https://example.com";
        buffer.AddStr ("Link");
        buffer.CurrentUrl = null;
        buffer.AddStr ("  ");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: hyperlink is opened and closed
        string start = EscSeqUtils.OSC_StartHyperlink ("https://example.com");
        string end = EscSeqUtils.OSC_EndHyperlink ();

        int startIdx = ansi.IndexOf (start, StringComparison.Ordinal);
        int endIdx = ansi.IndexOf (end, startIdx + start.Length, StringComparison.Ordinal);

        Assert.True (startIdx >= 0, "OSC 8 start not found");
        Assert.True (endIdx > startIdx, "OSC 8 end not found after start");

        // "Link" text should be between start and end
        int textIdx = ansi.IndexOf ("Link", startIdx, StringComparison.Ordinal);
        Assert.True (textIdx > startIdx && textIdx < endIdx, "Link text should be between OSC 8 sequences");
    }

    // Copilot
    [Fact]
    public void ToAnsi_UrlAtEndOfRow_ClosedBeforeNewline ()
    {
        // Arrange: 2-row buffer, URL at end of row 0, plain text on row 1
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (4, 2);

        // Row 0: "Link" with URL
        buffer.CurrentUrl = "https://example.com";
        buffer.AddStr ("Link");
        buffer.CurrentUrl = null;

        // Row 1: "Text" without URL
        buffer.Move (0, 1);
        buffer.AddStr ("Text");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: OSC 8 end appears before the newline that precedes row 1 text
        string end = EscSeqUtils.OSC_EndHyperlink ();
        int endIdx = ansi.IndexOf (end, StringComparison.Ordinal);
        int textIdx = ansi.IndexOf ("Text", StringComparison.Ordinal);

        Assert.True (endIdx >= 0, "OSC 8 end not found");
        Assert.True (textIdx > endIdx, "Row 1 text should appear after OSC 8 end (hyperlink closed at row boundary)");

        // Verify no OSC 8 start appears on row 1
        string start = EscSeqUtils.OSC_StartHyperlink ("https://example.com");
        int secondStart = ansi.IndexOf (start, endIdx + end.Length, StringComparison.Ordinal);
        Assert.True (secondStart < 0, "No OSC 8 start should appear on row 1");
    }

    // Copilot
    [Fact]
    public void ToAnsi_LegacyConsole_NoOsc8 ()
    {
        // Arrange
        AnsiOutput output = new () { IsLegacyConsole = true };
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (5, 1);

        buffer.CurrentUrl = "https://example.com";
        buffer.AddStr ("Hello");
        buffer.CurrentUrl = null;

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: legacy console should NOT contain OSC 8 sequences
        Assert.DoesNotContain (EscSeqUtils.OSC_StartHyperlink ("https://example.com"), ansi);
        Assert.DoesNotContain (EscSeqUtils.OSC_EndHyperlink (), ansi);
        Assert.Contains ("Hello", ansi);
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Write_EmitsSixelDataAndPositionsCursor (bool isLegacyConsole)
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Ensure the buffer has some content so Write traverses rows
        buffer.AddStr (".");

        // Create a Sixel to render
        SixelToRender s = new ()
        {
            SixelData = "SIXEL-DATA",
            ScreenPosition = new (4, 2)
        };

        // Create DriverImpl and associate it with the ANSIOutput to test Sixel output
        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        // Add the Sixel to the driver
        driver.GetSixels ().Enqueue (s);

        // ANSIOutput exposes this because it's in test scope
        output.IsLegacyConsole = isLegacyConsole;

        // Act
        output.Write (buffer);

        if (!isLegacyConsole)
        {
            // Assert: Sixel data was emitted (use Contains to avoid equality/side-effects)
            Assert.Contains ("SIXEL-DATA", output.GetLastOutput ());

            // Cursor was moved to Sixel position
            //Assert.Equal (s.ScreenPosition, output.GetCursor ().Position);
        }
        else
        {
            // Assert: Sixel data was NOT emitted
            Assert.DoesNotContain ("SIXEL-DATA", output.GetLastOutput ());

            // Cursor was NOT moved to Sixel position
            //Assert.NotEqual (s.ScreenPosition, output.GetCursor ().Position);
        }

        IApplication app = Application.Create ();
        app.Driver = driver;

        Assert.Equal (driver.GetSixels (), app.Driver.GetSixels ());

        app.Dispose ();
    }

    // Claude - Opus 4.7
    // Regression test for https://github.com/gui-cs/Terminal.Gui/issues/4892
    // When dirty cells with a URL are flushed mid-row because a clean cell follows,
    // the OSC 8 hyperlink remains open in the terminal. If no more dirty cells appear
    // on the row, the end-of-row code must still emit the OSC 8 close sequence so the
    // hyperlink does not bleed into the next row.
    [Fact]
    public void Write_UrlFollowedByCleanCells_ClosesHyperlinkAtRowEnd ()
    {
        // Arrange: 5-col row. URL at cols 0-1, clean cells at cols 2-4.
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (5, 1);

        // First frame: write URL cells then clear dirty by flushing
        buffer.Move (0, 0);
        buffer.CurrentUrl = "https://example.com";
        buffer.AddStr ("AB");
        buffer.CurrentUrl = null;
        output.Write (buffer);

        // Second frame: only re-mark the URL cells dirty so cols 2-4 stay clean.
        buffer.Contents! [0, 0].IsDirty = true;
        buffer.Contents! [0, 1].IsDirty = true;
        buffer.DirtyLines [0] = true;

        // Act
        output.Write (buffer);
        string result = output.GetLastOutput ();

        // Assert: every OSC 8 start sequence is followed by an OSC 8 close before the row ends.
        string start = EscSeqUtils.OSC_StartHyperlink ("https://example.com");
        string end = EscSeqUtils.OSC_EndHyperlink ();
        int startIdx = result.IndexOf (start, StringComparison.Ordinal);
        Assert.True (startIdx >= 0, "OSC 8 start sequence not emitted");

        int endIdx = result.IndexOf (end, startIdx + start.Length, StringComparison.Ordinal);
        Assert.True (endIdx > startIdx, "OSC 8 hyperlink was not closed before the row ended");
    }

    // Claude - Opus 4.7
    // Regression test for https://github.com/gui-cs/Terminal.Gui/issues/4892
    // When a Link's display area shrinks (or a Link is replaced), the cells previously
    // associated with a URL may be overdrawn by content that has no URL. Those cells
    // must be removed from the URL map so OSC 8 sequences are not re-emitted for them.
    [Fact]
    public void AddStr_NoCurrentUrl_ClearsStaleUrlMapping ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (5, 1);

        // First write: cells get associated with a URL
        buffer.Move (0, 0);
        buffer.CurrentUrl = "https://example.com";
        buffer.AddStr ("HELLO");
        buffer.CurrentUrl = null;

        Assert.Equal ("https://example.com", buffer.GetCellUrl (0, 0));
        Assert.Equal ("https://example.com", buffer.GetCellUrl (4, 0));

        // Act: overwrite cells with no CurrentUrl set (simulates a non-link view redrawing)
        buffer.Move (0, 0);
        buffer.AddStr ("WORLD");

        // Assert: stale URL associations are cleared
        Assert.Null (buffer.GetCellUrl (0, 0));
        Assert.Null (buffer.GetCellUrl (1, 0));
        Assert.Null (buffer.GetCellUrl (2, 0));
        Assert.Null (buffer.GetCellUrl (3, 0));
        Assert.Null (buffer.GetCellUrl (4, 0));

        // And the rendered output for the second frame contains no OSC 8 sequences
        string result = output.ToAnsi (buffer);
        Assert.DoesNotContain (EscSeqUtils.OSC_StartHyperlink ("https://example.com"), result);
    }

    // Claude - Opus 4.7
    // Regression test for https://github.com/gui-cs/Terminal.Gui/issues/4892
    // When CurrentUrl changes from one URL to another for the same cell, the URL map
    // should reflect the new URL (verifying the URL clear path does not break re-assignment).
    [Fact]
    public void AddStr_DifferentUrl_OverwritesUrlMapping ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (3, 1);

        buffer.Move (0, 0);
        buffer.CurrentUrl = "https://one.com";
        buffer.AddStr ("ABC");

        // Act: rewrite same cells with a different URL
        buffer.Move (0, 0);
        buffer.CurrentUrl = "https://two.com";
        buffer.AddStr ("ABC");

        // Assert: cells now report the new URL
        Assert.Equal ("https://two.com", buffer.GetCellUrl (0, 0));
        Assert.Equal ("https://two.com", buffer.GetCellUrl (1, 0));
        Assert.Equal ("https://two.com", buffer.GetCellUrl (2, 0));
    }
}
