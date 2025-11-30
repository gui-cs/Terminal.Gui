#nullable enable

using System.Text;

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

    [Fact]
    public void ToAnsi_WithAttribute_AppendsAnsiColorSequence ()
    {
        // Arrange
        var output = new TestFakeOutput ();
        IOutputBuffer buffer = output.LastBuffer!;
        buffer.SetSize (1, 1);

        // Set an RGB attribute; TestFakeOutput will always emit RGB CSI sequences regardless of global state
        buffer.CurrentAttribute = new Attribute (new Color (1, 2, 3), new Color (4, 5, 6));
        buffer.AddStr ("X");

        // Act
        string ansi = output.ToAnsi (buffer);

        // Assert: foreground RGB sequence must be included (ESC[38;2;r;g;bm)
        Assert.Contains ("\u001b[38;2;1;2;3m", ansi);
        // and the grapheme and newline remain present
        Assert.Contains ("X" + Environment.NewLine, ansi);
    }

    // Derived FakeOutput that avoids reading/modifying any static Application state.
    // It always emits RGB CSI sequences for attributes so tests remain instance-scoped and deterministic.
    private class TestFakeOutput : FakeOutput
    {
        protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
        {
            // Always append RGB sequences (same as non-force16 path)
            EscSeqUtils.CSI_AppendForegroundColorRGB (output, attr.Foreground.R, attr.Foreground.G, attr.Foreground.B);
            EscSeqUtils.CSI_AppendBackgroundColorRGB (output, attr.Background.R, attr.Background.G, attr.Background.B);
            EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
        }
    }
}