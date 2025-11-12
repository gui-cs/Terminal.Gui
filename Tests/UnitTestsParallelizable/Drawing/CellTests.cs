using System.Text;

namespace UnitTests_Parallelizable.DrawingTests;

public class CellTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var c = new Cell ();
        Assert.True (c is { });
        Assert.Empty (c.Runes);
        Assert.Null (c.Attribute);
        Assert.False (c.IsDirty);
        Assert.Null (c.Grapheme);
    }

    [Theory]
    [InlineData (null, new uint [] { })]
    [InlineData ("", new uint [] { })]
    [InlineData ("a", new uint [] { 0x0061 })]
    [InlineData ("👩‍❤️‍💋‍👨", new uint [] { 0x1F469, 0x200D, 0x2764, 0xFE0F, 0x200D, 0x1F48B, 0x200D, 0x1F468 })]
    public void Runes_From_Grapheme (string grapheme, uint [] expected)
    {
        // Arrange
        var c = new Cell { Grapheme = grapheme };

        // Act
        Rune [] runes = expected.Select (u => new Rune (u)).ToArray ();

        // Assert
        Assert.Equal (grapheme, c.Grapheme);
        Assert.Equal (runes, c.Runes);
    }

    [Fact]
    public void Equals_False ()
    {
        var c1 = new Cell ();

        var c2 = new Cell
        {
            Grapheme = "a", Attribute = new (Color.Red)
        };
        Assert.False (c1.Equals (c2));
        Assert.False (c2.Equals (c1));

        c1.Grapheme = "a";
        c1.Attribute = new ();
        Assert.Equal (c1.Grapheme, c2.Grapheme);
        Assert.False (c1.Equals (c2));
        Assert.False (c2.Equals (c1));
    }

    [Fact]
    public void Set_Text_With_Invalid_Grapheme_Throws ()
    {
        Assert.Throws<InvalidOperationException> (() => new Cell { Grapheme = "ab" });
        Assert.Throws<InvalidOperationException> (() => new Cell { Grapheme = "\u0061\u0062" }); // ab
    }

    [Theory]
    [MemberData (nameof (ToStringTestData))]
    public void ToString_Override (string text, Attribute? attribute, string expected)
    {
        var c = new Cell (attribute, true, text);
        string result = c.ToString ();

        Assert.Equal (expected, result);
    }

    public static IEnumerable<object []> ToStringTestData ()
    {
        yield return ["", null, "[\"\":]"];
        yield return ["a", null, "[\"a\":]"];
        yield return ["\t", null, "[\"\\t\":]"];
        yield return ["\r", null, "[\"\\r\":]"];
        yield return ["\n", null, "[\"\\n\":]"];
        yield return ["\r\n", null, "[\"\\r\\n\":]"];
        yield return ["\f", null, "[\"\\f\":]"];
        yield return ["\v", null, "[\"\\v\":]"];
        yield return ["\x1B", null, "[\"\\u001B\":]"];
        yield return ["\\", new Attribute (Color.Blue), "[\"\\\":[Blue,Blue,None]]"];
        yield return ["😀", null, "[\"😀\":]"];
        yield return ["👨‍👩‍👦‍👦", null, "[\"👨‍👩‍👦‍👦\":]"];
        yield return ["A", new Attribute (Color.Red) { Style = TextStyle.Blink }, "[\"A\":[Red,Red,Blink]]"];
        yield return ["\U0001F469\u200D\u2764\uFE0F\u200D\U0001F48B\u200D\U0001F468", null, "[\"👩‍❤️‍💋‍👨\":]"];
    }
}
