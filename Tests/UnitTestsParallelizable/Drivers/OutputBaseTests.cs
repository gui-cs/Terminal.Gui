#nullable enable

namespace UnitTests_Parallelizable.DriverTests;

public class OutputBaseTests
{
    [Fact]
    public void ToAnsi_SingleCell_NoAttribute_ReturnsGraphemeAndNewline ()
    {
        // Arrange
        var output = new FakeOutput ();
        IOutputBuffer buffer = output.LastBuffer!;
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
    public void ToAnsi_WithAttribute_AppendsCorrectColorSequence_BasedOnVirtualTerminal_And_Force16Colors (bool isVirtualTerminal, bool force16Colors)
    {
        // Arrange
        var output = new FakeOutput { IsVirtualTerminal = isVirtualTerminal };

        // Create DriverImpl and associate it with the FakeOutput to test Sixel output
        DriverImpl driver = new (
                                 new FakeInputProcessor (null!),
                                 new OutputBufferImpl (),
                                 output,
                                 new (new AnsiResponseParser ()),
                                 new SizeMonitorImpl (output));

        driver.Force16Colors = force16Colors;

        Assert.Equal (output.Driver, driver);

        IOutputBuffer buffer = output.LastBuffer!;
        buffer.SetSize (1, 1);

        // Use a known RGB color and attribute
        var fg = new Color (1, 2, 3);
        var bg = new Color (4, 5, 6);
        buffer.CurrentAttribute = new Attribute (fg, bg);
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: when true color expected, we should see the RGB CSI; otherwise we should see the 16-color CSI
        if (isVirtualTerminal && !force16Colors)
        {
            Assert.Contains ("\u001b[38;2;1;2;3m", ansi);
        }
        else if (isVirtualTerminal && force16Colors)
        {
            var expected16 = EscSeqUtils.CSI_SetForegroundColor (fg.GetAnsiColorCode ());
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
        IOutputBuffer buffer = output.LastBuffer!;
        buffer.SetSize (2, 1);

        // Mark two characters as dirty by writing them into the buffer
        buffer.AddStr ("AB");

        // Sanity: ensure cells are dirty before calling Write
        Assert.True (buffer.Contents! [0, 0].IsDirty);
        Assert.True (buffer.Contents! [0, 1].IsDirty);

        // Act
        output.Write (buffer); // calls OutputBase.Write via FakeOutput

        // Assert: content was written to the fake output and dirty flags cleared
        Assert.Contains ("AB", output.Output);
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 1].IsDirty);
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Write_Virtual_Or_NonVirtual_Uses_WriteToConsole_And_Clears_Dirty_Flags (bool isVirtualTerminal)
    {
        // Arrange
        // FakeOutput exposes this because it's in test scope
        var output = new FakeOutput { IsVirtualTerminal = isVirtualTerminal };
        IOutputBuffer buffer = output.LastBuffer!;
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
        Assert.Contains ("A", output.Output);
        Assert.Contains ("C", output.Output);

        // Dirty flags cleared for the written cells
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        // Verify SetCursorPositionImpl was invoked by WriteToConsole (cursor set to a written column)
        Assert.Equal (new Point (0, 0), output.GetCursorPosition ());

        // Now write 'X' at col 0 to verify subsequent writes also work
        buffer.Move (0, 0);
        buffer.AddStr ("X");

        // Confirm dirtiness state before to write
        Assert.True (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        output.Write (buffer);

        // Assert: both characters were written (use Contains to avoid CI side effects)
        Assert.Contains ("A", output.Output);
        Assert.Contains ("C", output.Output);

        // Dirty flags cleared for the written cells
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        // Verify SetCursorPositionImpl was invoked by WriteToConsole (cursor set to a written column)
        Assert.Equal (new Point (0, 0), output.GetCursorPosition ());
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Write_EmitsSixelDataAndPositionsCursor (bool isVirtualTerminal)
    {
        // Arrange
        var output = new FakeOutput ();
        IOutputBuffer buffer = output.LastBuffer!;
        buffer.SetSize (1, 1);

        // Ensure the buffer has some content so Write traverses rows
        buffer.AddStr (".");

        // Create a Sixel to render
        var s = new SixelToRender
        {
            SixelData = "SIXEL-DATA",
            ScreenPosition = new Point (4, 2)
        };

        // Create DriverImpl and associate it with the FakeOutput to test Sixel output
        DriverImpl driver = new (
                                 new FakeInputProcessor (null!),
                                 new OutputBufferImpl (),
                                 output,
                                 new (new AnsiResponseParser ()),
                                 new SizeMonitorImpl (output));

        Assert.Equal (output.Driver, driver);

        // Add the Sixel to the driver
        driver.Sixel.Add (s);

        // FakeOutput exposes this because it's in test scope
        output.IsVirtualTerminal = isVirtualTerminal;

        // Act
        output.Write (buffer);

        if (isVirtualTerminal)
        {
            // Assert: Sixel data was emitted (use Contains to avoid equality/side-effects)
            Assert.Contains ("SIXEL-DATA", output.Output);

            // Cursor was moved to Sixel position
            Assert.Equal (s.ScreenPosition, output.GetCursorPosition ());
        }
        else
        {
            // Assert: Sixel data was NOT emitted
            Assert.DoesNotContain ("SIXEL-DATA", output.Output);

            // Cursor was NOT moved to Sixel position
            Assert.NotEqual (s.ScreenPosition, output.GetCursorPosition ());
        }
    }
}