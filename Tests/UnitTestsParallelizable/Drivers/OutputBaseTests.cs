namespace DriverTests;

public class OutputBaseTests
{
    [Fact]
    public void ToAnsi_SingleCell_NoAttribute_ReturnsGraphemeAndNewline ()
    {
        // Arrange
        var output = new FakeOutput ();
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
        var output = new FakeOutput { IsLegacyConsole = isLegacyConsole };

        // Create DriverImpl and associate it with the FakeOutput to test Sixel output
        IDriver driver = new DriverImpl (
                                         new FakeInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser ()),
                                         new SizeMonitorImpl (output));

        driver.Force16Colors = force16Colors;

        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Use a known RGB color and attribute
        var fg = new Color (1, 2, 3);
        var bg = new Color (4, 5, 6);
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
    }

    [Fact]
    public void Write_WritesDirtyCellsAndClearsDirtyFlags ()
    {
        // Arrange
        var output = new FakeOutput ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (2, 1);

        // Mark two characters as dirty by writing them into the buffer
        buffer.AddStr ("AB");

        // Sanity: ensure cells are dirty before calling Write
        Assert.True (buffer.Contents! [0, 0].IsDirty);
        Assert.True (buffer.Contents! [0, 1].IsDirty);

        // Act
        output.Write (buffer); // calls OutputBase.Write via FakeOutput

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
        // FakeOutput exposes this because it's in test scope
        var output = new FakeOutput { IsLegacyConsole = isLegacyConsole };
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

        // Verify SetCursorPositionImpl was invoked by WriteToConsole (cursor set to a written column)
        Assert.Equal (new (0, 0), output.GetCursorPosition ());

        // Now write 'X' at col 0 to verify subsequent writes also work
        buffer.Move (0, 0);
        buffer.AddStr ("X");

        // Confirm dirtiness state before to write
        Assert.True (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        output.Write (buffer);

        // Assert: both characters were written (use Contains to avoid CI side effects)
        Assert.Contains ("A", output.GetLastOutput ());
        Assert.Contains ("C", output.GetLastOutput ());

        // Dirty flags cleared for the written cells
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        // Verify SetCursorPositionImpl was invoked by WriteToConsole (cursor set to a written column)
        Assert.Equal (new (2, 0), output.GetCursorPosition ());
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Write_Virtual_Or_NonVirtual_Uses_WriteToConsole_And_Clears_Dirty_Flags_Mixed_Graphemes (bool isLegacyConsole)
    {
        // Arrange
        // FakeOutput exposes this because it's in test scope
        var output = new FakeOutput { IsLegacyConsole = isLegacyConsole };
        IOutputBuffer buffer = output.GetLastBuffer ()!;
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

        // Column 1 was skipped by OutputBase.Write because column 0 had a wide glyph
        // So its dirty flag remains true (it was initialized as dirty by ClearContents)
        Assert.False (buffer.Contents! [0, 1].IsDirty);

        // Column 2 was written ('A')
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        Assert.Equal (new (0, 0), output.GetCursorPosition ());

        // Now write 'X' at col 1 which invalidates the wide glyph at col 0
        buffer.Move (1, 0);
        buffer.AddStr ("X");

        // Confirm dirtiness state before to write
        Assert.True (buffer.Contents! [0, 0].IsDirty); // Invalidated by writing at col 1
        Assert.True (buffer.Contents! [0, 1].IsDirty); // Just written
        Assert.True (buffer.Contents! [0, 2].IsDirty); // Marked dirty by writing at col 1

        output.Write (buffer);

        Assert.Contains ("�", output.GetLastOutput ());
        Assert.Contains ("X", output.GetLastOutput ());

        // Dirty flags cleared for the written cells
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 1].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        // Verify SetCursorPositionImpl was invoked by WriteToConsole (cursor set to a written column)
        Assert.Equal (new (0, 0), output.GetCursorPosition ());
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Write_EmitsSixelDataAndPositionsCursor (bool isLegacyConsole)
    {
        // Arrange
        var output = new FakeOutput ();
        IOutputBuffer buffer = output.GetLastBuffer ()!;
        buffer.SetSize (1, 1);

        // Ensure the buffer has some content so Write traverses rows
        buffer.AddStr (".");

        // Create a Sixel to render
        var s = new SixelToRender
        {
            SixelData = "SIXEL-DATA",
            ScreenPosition = new (4, 2)
        };

        // Create DriverImpl and associate it with the FakeOutput to test Sixel output
        IDriver driver = new DriverImpl (
                                         new FakeInputProcessor (null!),
                                         new OutputBufferImpl (),
                                         output,
                                         new (new AnsiResponseParser ()),
                                         new SizeMonitorImpl (output));

        // Add the Sixel to the driver
        driver.GetSixels ().Enqueue (s);

        // FakeOutput exposes this because it's in test scope
        output.IsLegacyConsole = isLegacyConsole;

        // Act
        output.Write (buffer);

        if (!isLegacyConsole)
        {
            // Assert: Sixel data was emitted (use Contains to avoid equality/side-effects)
            Assert.Contains ("SIXEL-DATA", output.GetLastOutput ());

            // Cursor was moved to Sixel position
            Assert.Equal (s.ScreenPosition, output.GetCursorPosition ());
        }
        else
        {
            // Assert: Sixel data was NOT emitted
            Assert.DoesNotContain ("SIXEL-DATA", output.GetLastOutput ());

            // Cursor was NOT moved to Sixel position
            Assert.NotEqual (s.ScreenPosition, output.GetCursorPosition ());
        }

        IApplication app = Application.Create ();
        app.Driver = driver;

        Assert.Equal (driver.GetSixels (), app.Driver.GetSixels ());

        app.Dispose ();
    }
}
