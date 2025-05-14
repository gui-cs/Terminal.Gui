using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class CellTests ()
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var c = new Cell ();
        Assert.True (c is { });
        Assert.Equal (0, c.Rune.Value);
        Assert.Null (c.Attribute);
    }

    [Fact]
    public void Equals_False ()
    {
        var c1 = new Cell ();

        var c2 = new Cell
        {
            Rune = new ('a'), Attribute = new (Color.Red)
        };
        Assert.False (c1.Equals (c2));
        Assert.False (c2.Equals (c1));

        c1.Rune = new ('a');
        c1.Attribute = new ();
        Assert.Equal (c1.Rune, c2.Rune);
        Assert.False (c1.Equals (c2));
        Assert.False (c2.Equals (c1));
    }

    [Fact]
    public void ToString_Override ()
    {
        var c1 = new Cell ();

        var c2 = new Cell
        {
            Rune = new ('a'), Attribute = new (Color.Red)
        };
        Assert.Equal ("['\0':]", c1.ToString ());

        Assert.Equal (
                      "['a':[Red,Red,None]]",
                      c2.ToString ()
                     );
    }
}
