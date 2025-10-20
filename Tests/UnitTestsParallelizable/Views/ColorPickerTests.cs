namespace Terminal.Gui.ViewsTests;

/// <summary>
/// Pure unit tests for <see cref="ColorPicker"/> that don't require Application.Driver or View context.
/// These tests can run in parallel without interference.
/// </summary>
public class ColorPickerTests : UnitTests.Parallelizable.ParallelizableBase
{
    [Fact]
    public void ColorPicker_ChangedEvent_Fires ()
    {
        Color newColor = default;
        var count = 0;

        var cp = new ColorPicker ();

        cp.ColorChanged += (s, e) =>
                           {
                               count++;
                               newColor = e.Result;

                               Assert.Equal (cp.SelectedColor, e.Result);
                           };

        cp.SelectedColor = new (1, 2, 3);
        Assert.Equal (1, count);
        Assert.Equal (new (1, 2, 3), newColor);

        cp.SelectedColor = new (2, 3, 4);

        Assert.Equal (2, count);
        Assert.Equal (new (2, 3, 4), newColor);

        // Set to same value
        cp.SelectedColor = new (2, 3, 4);

        // Should have no effect
        Assert.Equal (2, count);
    }
}
