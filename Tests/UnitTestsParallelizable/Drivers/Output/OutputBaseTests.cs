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

    // Copilot - GPT-5.4
    // Regression coverage for plain-text URL auto-linking used by TextView/Editor.
    // If a previously auto-linked URL is overwritten by non-URL text, the redraw must
    // explicitly clear hyperlink state before writing the replacement text.
    [Fact]
    public void Write_AutoDetectedUrl_ThenPlainText_EmitsOsc8CloseBeforeReplacement ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (24, 1);

        buffer.Move (0, 0);
        buffer.AddStr ("https://example.com");
        output.Write (buffer);

        buffer.Move (0, 0);
        buffer.AddStr ("plain replacement      ");

        // Act
        output.Write (buffer);
        string result = output.GetLastOutput ();

        // Assert
        string start = EscSeqUtils.OSC_StartHyperlink ("https://example.com");
        string end = EscSeqUtils.OSC_EndHyperlink ();
        int replacementIdx = result.IndexOf ("plain replacement", StringComparison.Ordinal);
        int endIdx = result.IndexOf (end, StringComparison.Ordinal);

        Assert.DoesNotContain (start, result);
        Assert.True (replacementIdx >= 0, "Replacement text was not emitted");
        Assert.True (endIdx >= 0 && endIdx < replacementIdx, "OSC 8 close must be emitted before replacement text");
    }

    // Copilot - GPT-5.4
    // Regression coverage for deleting all text in the Editor scenario.
    // Clearing a row that previously contained an auto-detected URL must emit an OSC 8
    // close so terminals do not keep hyperlink metadata at the former URL location.
    [Fact]
    public void Write_AutoDetectedUrl_ThenSpaces_EmitsOsc8CloseBeforeClearingCells ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (24, 1);

        buffer.Move (0, 0);
        buffer.AddStr ("https://example.com");
        output.Write (buffer);

        buffer.Move (0, 0);
        buffer.AddStr ("                        ");

        // Act
        output.Write (buffer);
        string result = output.GetLastOutput ();
        string end = EscSeqUtils.OSC_EndHyperlink ();
        int replacementIdx = result.IndexOf ("                        ", StringComparison.Ordinal);
        int endIdx = result.IndexOf (end, StringComparison.Ordinal);

        // Assert
        Assert.True (replacementIdx >= 0, "Replacement spaces were not emitted");
        Assert.True (endIdx >= 0 && endIdx < replacementIdx, "OSC 8 close must be emitted before replacement spaces");
    }

    // Claude - Opus 4.7
    // Regression coverage for the char-vs-column mismatch in SyncAutoUrlsForRowCore.
    // When a multi-codepoint grapheme (ZWJ emoji, base + combining mark) precedes a URL
    // on the same row, the URL's char offset in the concatenated row text diverges from
    // its column position. The fix builds a char-to-column map so the auto-URL metadata
    // lands on the actual URL cells, not shifted by the extra char count.
    [Fact]
    public void Write_AutoDetectedUrl_AfterMultiCharGrapheme_LinkAlignsWithUrlColumns ()
    {
        // Arrange — combining acute (U+0301) appended to 'e' yields a 2-char grapheme
        // occupying a single column. Without the fix, the URL would be tagged starting
        // one column past where it actually renders.
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (40, 1);

        buffer.Move (0, 0);
        buffer.AddStr ("é https://example.com");

        // Act
        output.Write (buffer);
        string result = output.GetLastOutput ();
        string start = EscSeqUtils.OSC_StartHyperlink ("https://example.com");

        int startIdx = result.IndexOf (start, StringComparison.Ordinal);

        // Assert — the visible URL text must follow the OSC 8 start sequence with no
        // characters between them. Search strictly after the start sequence so we don't
        // match the URL embedded as the OSC parameter inside the start sequence itself.
        Assert.True (startIdx >= 0, "OSC 8 start sequence was not emitted");

        int afterStart = startIdx + start.Length;
        int visibleUrlIdx = result.IndexOf ("https://example.com", afterStart, StringComparison.Ordinal);

        Assert.Equal (afterStart, visibleUrlIdx);
    }

    // Claude - Opus 4.7
    // Regression coverage for stale _rowsWithUrls tracking on resize. After a SetSize call
    // the buffer's URL maps are wiped, so OutputBase must drop its row tracking too.
    // Otherwise the next render emits a spurious OSC 8 close at the start of any row index
    // that previously contained a URL.
    [Fact]
    public void Write_AfterResize_DoesNotEmitSpuriousOsc8Close ()
    {
        // Arrange — render a URL, then resize (which clears URL maps) and render plain text.
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (40, 2);

        buffer.Move (0, 0);
        buffer.AddStr ("https://example.com");
        output.Write (buffer);

        // SetSize wipes URL state; row 0 used to have a URL.
        buffer.SetSize (40, 2);

        buffer.Move (0, 0);
        buffer.AddStr ("plain text only");

        // Act
        output.Write (buffer);
        string result = output.GetLastOutput ();
        string end = EscSeqUtils.OSC_EndHyperlink ();

        // Assert — no OSC 8 close should appear because row 0 no longer has any URL state
        // and the buffer was reset between writes.
        Assert.DoesNotContain (end, result);
    }

    // Claude - Opus 4.7
    // After clearing URL state, GetCellUrl's null fast-path should be re-armed so subsequent
    // cell lookups skip the lock entirely. This guards against the regression where
    // ClearContents called .Clear() on the maps but left them allocated, defeating the
    // fast-path for the lifetime of the buffer.
    [Fact]
    public void GetCellUrl_AfterUrlSetThenCleared_RestoresNullFastPath ()
    {
        // Arrange
        OutputBufferImpl buffer = new () { Rows = 1, Cols = 10 };
        buffer.SetSize (10, 1);
        buffer.Move (0, 0);
        buffer.CurrentUrl = "https://example.com";
        buffer.AddStr ("https://x");
        buffer.CurrentUrl = null;

        Assert.NotNull (buffer.GetCellUrl (0, 0));

        // Act
        buffer.ClearContents (true);

        // Assert — second call should hit the null fast-path. We can't directly observe the
        // lock skip, but we can verify state was wiped and the version counter advanced so
        // OutputBase tracking is invalidated.
        Assert.Null (buffer.GetCellUrl (0, 0));
        Assert.True (buffer.UrlStateVersion > 0);
    }

    // Claude - Opus 4.7
    // Regression coverage for _rowsWithUrls bookkeeping when the per-row flush happens entirely
    // via WriteToConsole (i.e. the end-of-row Write path takes the empty-builder early-exit).
    // Before the fix, the Add/Remove of the row index lived after the early-exit, so a row that
    // lost its URL via overwrite but had clean trailing cells (causing the builder to flush
    // mid-loop and end empty) kept a stale row-tracking entry — leading to a spurious row-start
    // OSC 8 close on subsequent frames.
    [Fact]
    public void Write_RowLosesUrl_BuilderFlushedMidLoop_RemovesRowTracking ()
    {
        // Arrange — buffer with a URL on row 0, then overwrite the URL cells with non-URL
        // content followed by clean cells (so the builder flushes mid-loop and the row exits
        // with an empty builder, taking the early-exit).
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (40, 1);

        // Frame 1: write URL covering cols 0-18, trailing space cells stay as ' '.
        buffer.Move (0, 0);
        buffer.AddStr ("https://example.com");
        output.Write (buffer);

        // Frame 2: overwrite the URL cells with Y characters. Cells 0-19 become dirty (19
        // Y's plus the adjacent-dirty mark on cell 19). Cells 20-39 stay clean — they will
        // trigger the WriteToConsole flush and leave the row with an empty builder.
        buffer.Move (0, 0);
        buffer.AddStr ("YYYYYYYYYYYYYYYYYYY");
        output.Write (buffer);

        // Frame 3: trivial change. With the fix, row 0 has been removed from _rowsWithUrls,
        // so no OSC 8 close is emitted at the start of row 0. Without the fix, the stale
        // entry causes a spurious OSC 8 close.
        buffer.Move (0, 0);
        buffer.AddStr ("X");

        // Act
        output.Write (buffer);
        string result = output.GetLastOutput ();
        string end = EscSeqUtils.OSC_EndHyperlink ();

        // Assert — frame 3 must NOT emit an OSC 8 close because frame 2's row no longer
        // has any URL state, and _rowsWithUrls must reflect that even when the early-exit
        // path is taken.
        Assert.DoesNotContain (end, result);
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

    [Fact]
    public void Write_SkipsSixel_WhenIsDirtyIsFalse ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);
        buffer.AddStr (".");

        SixelToRender s = new ()
        {
            SixelData = "SKIPPED-SIXEL",
            ScreenPosition = new (0, 0),
            IsDirty = false
        };

        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        driver.GetSixels ().Enqueue (s);

        // Act
        output.Write (buffer);

        // Assert: sixel data should NOT have been emitted
        Assert.DoesNotContain ("SKIPPED-SIXEL", output.GetLastOutput ());

        driver.Dispose ();
    }

    [Fact]
    public void Write_ClearsIsDirty_AfterWritingSixel ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);
        buffer.AddStr (".");

        SixelToRender s = new ()
        {
            SixelData = "DIRTY-SIXEL",
            ScreenPosition = new (0, 0),
            IsDirty = true
        };

        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        driver.GetSixels ().Enqueue (s);

        // Act
        output.Write (buffer);

        // Assert: sixel was emitted and IsDirty was cleared
        Assert.Contains ("DIRTY-SIXEL", output.GetLastOutput ());
        Assert.False (s.IsDirty);

        driver.Dispose ();
    }

    [Fact]
    public void Write_SecondFrame_SkipsSixel_WhenIsDirtyWasClearedByFirstFrame ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);
        buffer.AddStr (".");

        SixelToRender s = new ()
        {
            SixelData = "ONCE-SIXEL",
            ScreenPosition = new (0, 0),
            IsDirty = true
        };

        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        driver.GetSixels ().Enqueue (s);

        // Frame 1: should emit
        output.Write (buffer);
        Assert.Contains ("ONCE-SIXEL", output.GetLastOutput ());
        Assert.False (s.IsDirty);

        // Frame 2: re-dirty the buffer so Write traverses rows, but sixel should be skipped
        buffer.Move (0, 0);
        buffer.AddStr ("X");
        output.Write (buffer);
        Assert.DoesNotContain ("ONCE-SIXEL", output.GetLastOutput ());

        driver.Dispose ();
    }

    [Fact]
    public void Write_AlwaysRender_BypassesIsDirtyCheck ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);
        buffer.AddStr (".");

        SixelToRender s = new ()
        {
            SixelData = "ALWAYS-SIXEL",
            ScreenPosition = new (0, 0),
            IsDirty = false,
            AlwaysRender = true
        };

        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        driver.GetSixels ().Enqueue (s);

        // Act
        output.Write (buffer);

        // Assert: sixel was emitted even though IsDirty was false
        Assert.Contains ("ALWAYS-SIXEL", output.GetLastOutput ());

        driver.Dispose ();
    }

    [Fact]
    public void Write_AlwaysRender_EmitsEveryFrame ()
    {
        // Arrange
        AnsiOutput output = new ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);
        buffer.AddStr (".");

        SixelToRender s = new ()
        {
            SixelData = "EVERY-FRAME",
            ScreenPosition = new (0, 0),
            IsDirty = false,
            AlwaysRender = true
        };

        IDriver driver = new DriverImpl (
                                         new AnsiComponentFactory (),
                                         new AnsiInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser (new SystemTimeProvider ())),
                                         new SizeMonitorImpl (output));

        driver.GetSixels ().Enqueue (s);

        // Frame 1
        output.Write (buffer);
        Assert.Contains ("EVERY-FRAME", output.GetLastOutput ());

        // Frame 2: re-dirty buffer so Write traverses, sixel should still emit
        buffer.Move (0, 0);
        buffer.AddStr ("Y");
        output.Write (buffer);
        Assert.Contains ("EVERY-FRAME", output.GetLastOutput ());

        driver.Dispose ();
    }

    [Fact]
    public void DriverImpl_SixelSupport_DefaultsToNull ()
    {
        // Arrange & Act
        DriverImpl driver = new (
                                 new AnsiComponentFactory (),
                                 new AnsiInputProcessor (null!),
                                 new OutputBufferImpl (),
                                 new AnsiOutput (),
                                 new (new AnsiResponseParser (new SystemTimeProvider ())),
                                 new SizeMonitorImpl (new AnsiOutput ()));

        // Assert
        Assert.Null (driver.SixelSupport);

        driver.Dispose ();
    }

    [Fact]
    public void DriverImpl_SetSixelSupport_RaisesSixelSupportChangedEvent ()
    {
        // Arrange
        using DriverImpl driver = new (
                                 new AnsiComponentFactory (),
                                 new AnsiInputProcessor (null!),
                                 new OutputBufferImpl (),
                                 new AnsiOutput (),
                                 new (new AnsiResponseParser (new SystemTimeProvider ())),
                                 new SizeMonitorImpl (new AnsiOutput ()));

        SixelSupportResult firstResult = new ()
        {
            IsSupported = true,
            MaxPaletteColors = 256,
            SupportsTransparency = false
        };

        SixelSupportResult secondResult = new ()
        {
            IsSupported = true,
            MaxPaletteColors = 512,
            SupportsTransparency = true
        };

        List<ValueChangedEventArgs<SixelSupportResult?>> raisedArgs = [];

        driver.SixelSupportChanged += (_, e) => raisedArgs.Add (e);

        // Act 1: first call, old value should be null
        driver.SetSixelSupport (firstResult);

        // Assert 1
        Assert.Single (raisedArgs);
        Assert.Null (raisedArgs [0].OldValue);
        Assert.Same (firstResult, raisedArgs [0].NewValue);

        // Act 2: second call, old value should be firstResult
        driver.SetSixelSupport (secondResult);

        // Assert 2
        Assert.Equal (2, raisedArgs.Count);
        Assert.Same (firstResult, raisedArgs [1].OldValue);
        Assert.Same (secondResult, raisedArgs [1].NewValue);
    }

    [Fact]
    public void DriverImpl_SetSixelSupport_StoresResult ()
    {
        // Arrange
        DriverImpl driver = new (
                                 new AnsiComponentFactory (),
                                 new AnsiInputProcessor (null!),
                                 new OutputBufferImpl (),
                                 new AnsiOutput (),
                                 new (new AnsiResponseParser (new SystemTimeProvider ())),
                                 new SizeMonitorImpl (new AnsiOutput ()));

        SixelSupportResult result = new ()
        {
            IsSupported = true,
            MaxPaletteColors = 512,
            SupportsTransparency = true
        };

        // Act
        driver.SetSixelSupport (result);

        // Assert
        Assert.NotNull (driver.SixelSupport);
        Assert.True (driver.SixelSupport!.IsSupported);
        Assert.Equal (512, driver.SixelSupport.MaxPaletteColors);
        Assert.True (driver.SixelSupport.SupportsTransparency);

        driver.Dispose ();
    }
}
