namespace Terminal.Gui.DrawingTests;

/// <summary>
/// Pure unit tests for <see cref="Ruler"/> that don't require Application.Driver or View context.
/// These tests focus on properties and behavior that don't depend on rendering.
///
/// Note: Tests that verify rendered output (Draw methods) require Application.Driver and remain in UnitTests as integration tests.
/// </summary>
public class RulerTests : UnitTests.Parallelizable.ParallelizableBase
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var r = new Ruler ();
        Assert.Equal (0, r.Length);
        Assert.Equal (Orientation.Horizontal, r.Orientation);
    }

    [Fact]
    public void Attribute_Set ()
    {
        var newAttribute = new Attribute (Color.Red, Color.Green);

        var r = new Ruler ();
        r.Attribute = newAttribute;
        Assert.Equal (newAttribute, r.Attribute);
    }

    [Fact]
    public void Length_Set ()
    {
        var r = new Ruler ();
        Assert.Equal (0, r.Length);
        r.Length = 42;
        Assert.Equal (42, r.Length);
    }

    [Fact]
    public void Orientation_Set ()
    {
        var r = new Ruler ();
        Assert.Equal (Orientation.Horizontal, r.Orientation);
        r.Orientation = Orientation.Vertical;
        Assert.Equal (Orientation.Vertical, r.Orientation);
    }
}
