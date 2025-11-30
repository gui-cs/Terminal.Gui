#nullable enable

using System.Reflection;
using System.Text;
using Moq;

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
    [InlineData (true)]
    [InlineData (false)]
    public void ToAnsi_WithAttribute_AppendsCorrectColorSequence_BasedOnVirtualTerminal (bool isVirtualTerminal)
    {
        // Arrange
        var output = new TestFakeOutput (isVirtualTerminal);
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
        if (isVirtualTerminal)
        {
            Assert.Contains ("\u001b[38;2;1;2;3m", ansi);
        }
        else
        {
            var expected16 = EscSeqUtils.CSI_SetForegroundColor (fg.GetAnsiColorCode ());
            Assert.Contains (expected16, ansi);
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

    [Fact]
    public void Write_NonVirtual_UsesWriteToConsoleAndClearsDirtyFlags ()
    {
        // Arrange
        var output = new FakeOutput ();
        IOutputBuffer buffer = output.LastBuffer!;
        buffer.SetSize (3, 1);

        // Write 'A' at col 0 and 'C' at col 2; leave col 1 untouched (not dirty)
        buffer.Move (0, 0);
        buffer.AddStr ("A");
        buffer.Move (2, 0);
        buffer.AddStr ("C");

        // Confirm some dirtiness before the write
        Assert.True (buffer.Contents! [0, 0].IsDirty);
        Assert.True (buffer.Contents! [0, 2].IsDirty);

        // Set IsVirtualTerminal = false (FakeOutput exposes this because it's in test scope)
        output.IsVirtualTerminal = false;

        // Act
        output.Write (buffer);

        // Assert: both characters were written (use Contains to avoid CI side-effects)
        Assert.Contains ("A", output.Output);
        Assert.Contains ("C", output.Output);

        // Dirty flags cleared for the written cells
        Assert.False (buffer.Contents! [0, 0].IsDirty);
        Assert.False (buffer.Contents! [0, 2].IsDirty);

        // Verify SetCursorPositionImpl was invoked by WriteToConsole (cursor set to a written column)
        Assert.Equal (new Point (0, 0), output.GetCursorPosition ());
    }

    [Fact]
    public void Write_EmitsSixelDataAndPositionsCursor ()
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

        // Mock IDriver and set Sixel list
        var driverMock = new Mock<IDriver> ();
        driverMock.SetupGet (d => d.Sixel).Returns (new List<SixelToRender> { s });

        // Attach mock driver to output via internal property (use reflection if necessary)
        var driverProp = output.GetType ().GetProperty (
                                                     "Driver",
                                                     BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy
                                                    );
        Assert.NotNull (driverProp);
        driverProp.SetValue (output, driverMock.Object);

        // Act
        output.Write (buffer);

        // Assert: Sixel data was emitted (use Contains to avoid equality/side-effects)
        Assert.Contains ("SIXEL-DATA", output.Output);

        // Cursor was moved to Sixel position
        Assert.Equal (s.ScreenPosition, output.GetCursorPosition ());
    }

    /// <summary>
    ///     Test FakeOutput variant that lets the test opt into emitting true-color RGB CSI sequences
    ///     or 16-color SGR sequences for attributes without touching static <see cref="Application"/> state.
    /// </summary>
    private class TestFakeOutput : FakeOutput
    {
        private readonly bool _isVirtualTerminal;

        public TestFakeOutput (bool isVirtualTerminal)
        {
            _isVirtualTerminal = isVirtualTerminal;
            IsVirtualTerminal = isVirtualTerminal;
        }

        protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
        {
            if (_isVirtualTerminal)
            {
                // True color path (RGB CSI)
                EscSeqUtils.CSI_AppendForegroundColorRGB (output, attr.Foreground.R, attr.Foreground.G, attr.Foreground.B);
                EscSeqUtils.CSI_AppendBackgroundColorRGB (output, attr.Background.R, attr.Background.G, attr.Background.B);
                EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
            }
            else
            {
                // 16-color SGR path (emit SGR codes for closest 16 colors)
                output.Append (EscSeqUtils.CSI_SetForegroundColor (attr.Foreground.GetAnsiColorCode ()));
                output.Append (EscSeqUtils.CSI_SetBackgroundColor (attr.Background.GetAnsiColorCode ()));
                EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
            }
        }
    }
}