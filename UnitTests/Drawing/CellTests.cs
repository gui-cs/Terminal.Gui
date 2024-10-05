using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class CellTests (ITestOutputHelper output)
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
    public void Equals_True ()
    {
        var c1 = new Cell ();
        var c2 = new Cell ();
        Assert.True (c1.Equals (c2));
        Assert.True (c2.Equals (c1));

        c1.Rune = new ('a');
        c1.Attribute = new ();
        c2.Rune = new ('a');
        c2.Attribute = new ();
        Assert.True (c1.Equals (c2));
        Assert.True (c2.Equals (c1));
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigurationManager.ConfigLocations.DefaultOnly)]
    public void Cell_LoadCells_InheritsPreviousAttribute ()
    {
        List<Cell> cells = [];

        foreach (KeyValuePair<string, ColorScheme> color in Colors.ColorSchemes)
        {
            string csName = color.Key;

            foreach (Rune rune in csName.EnumerateRunes ())
            {
                cells.Add (new() { Rune = rune, Attribute = color.Value.Normal });
            }

            cells.Add (new() { Rune = (Rune)'\n', Attribute = color.Value.Focus });
        }

        TextView tv = CreateTextView ();
        tv.Load (cells);
        var top = new Toplevel ();
        top.Add (tv);
        RunState rs = Application.Begin (top);
        Assert.True (tv.InheritsPreviousAttribute);

        var expectedText = @"
TopLevel
Base    
Dialog  
Menu    
Error   ";
        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, output);

        Attribute [] attributes =
        {
            // 0
            Colors.ColorSchemes ["TopLevel"].Normal,

            // 1
            Colors.ColorSchemes ["Base"].Normal,

            // 2
            Colors.ColorSchemes ["Dialog"].Normal,

            // 3
            Colors.ColorSchemes ["Menu"].Normal,

            // 4
            Colors.ColorSchemes ["Error"].Normal,

            // 5
            tv.ColorScheme!.Focus
        };

        var expectedColor = @"
0000000055
1111555555
2222225555
3333555555
4444455555";
        TestHelpers.AssertDriverAttributesAre (expectedColor, Application.Driver, attributes);

        tv.WordWrap = true;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, output);
        TestHelpers.AssertDriverAttributesAre (expectedColor, Application.Driver, attributes);

        tv.CursorPosition = new (6, 2);
        tv.SelectionStartColumn = 0;
        tv.SelectionStartRow = 0;
        Assert.Equal ($"TopLevel{Environment.NewLine}Base{Environment.NewLine}Dialog", tv.SelectedText);
        tv.Copy ();
        tv.Selecting = false;
        tv.CursorPosition = new (2, 4);
        tv.Paste ();
        Application.Refresh ();

        expectedText = @"
TopLevel  
Base      
Dialog    
Menu      
ErTopLevel
Base      
Dialogror ";
        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, output);

        expectedColor = @"
0000000055
1111555555
2222225555
3333555555
4455555555
5555555555
5555544445";
        TestHelpers.AssertDriverAttributesAre (expectedColor, Application.Driver, attributes);

        tv.Undo ();
        tv.CursorPosition = new (0, 3);
        tv.SelectionStartColumn = 0;
        tv.SelectionStartRow = 0;

        Assert.Equal (
                      $"TopLevel{Environment.NewLine}Base{Environment.NewLine}Dialog{Environment.NewLine}",
                      tv.SelectedText
                     );
        tv.Copy ();
        tv.Selecting = false;
        tv.CursorPosition = new (2, 4);
        tv.Paste ();
        Application.Refresh ();

        expectedText = @"
TopLevel  
Base      
Dialog    
Menu      
ErTopLevel
Base      
Dialog    
ror       ";
        TestHelpers.AssertDriverContentsWithFrameAre (expectedText, output);

        expectedColor = @"
0000000055
1111555555
2222225555
3333555555
4444444444
4444555555
4444445555
4445555555";
        TestHelpers.AssertDriverAttributesAre (expectedColor, Application.Driver, attributes);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    public void Cell_LoadCells_Without_ColorScheme_Is_Never_Null ()
    {
        List<Cell> cells = new ()
        {
            new() { Rune = new ('T') },
            new() { Rune = new ('e') },
            new() { Rune = new ('s') },
            new() { Rune = new ('t') }
        };
        TextView tv = CreateTextView ();
        var top = new Toplevel ();
        top.Add (tv);
        tv.Load (cells);

        for (var i = 0; i < tv.Lines; i++)
        {
            List<Cell> line = tv.GetLine (i);

            foreach (Cell c in line)
            {
                Assert.NotNull (c.Attribute);
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void CellEventArgs_WordWrap_True ()
    {
        var eventCount = 0;

        List<List<Cell>> text =
        [
            Cell.ToCells (
                          "This is the first line.".ToRunes ()
                         ),

            Cell.ToCells (
                          "This is the second line.".ToRunes ()
                         )
        ];
        TextView tv = CreateTextView ();
        tv.DrawNormalColor += _textView_DrawColor;
        tv.DrawReadOnlyColor += _textView_DrawColor;
        tv.DrawSelectionColor += _textView_DrawColor;
        tv.DrawUsedColor += _textView_DrawColor;

        void _textView_DrawColor (object sender, CellEventArgs e)
        {
            Assert.Equal (e.Line [e.Col], text [e.UnwrappedPosition.Row] [e.UnwrappedPosition.Col]);
            eventCount++;
        }

        tv.Text = $"{Cell.ToString (text [0])}\n{Cell.ToString (text [1])}\n";
        Assert.False (tv.WordWrap);
        var top = new Toplevel ();
        top.Add (tv);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is the first line. 
This is the second line.",
                                                      output
                                                     );

        tv.Width = 10;
        tv.Height = 25;
        tv.WordWrap = true;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is
the    
first  
line.  
This is
the    
second 
line.  ",
                                                      output
                                                     );

        Assert.Equal (eventCount, (text [0].Count + text [1].Count) * 2);
        top.Dispose ();
    }

    [Fact]
    public void ToString_Override ()
    {
        var c1 = new Cell ();

        var c2 = new Cell
        {
            Rune = new ('a'), Attribute = new (Color.Red)
        };
        Assert.Equal ("[\0, ]", c1.ToString ());

        Assert.Equal (
                      "[a, [Red,Red]]",
                      c2.ToString ()
                     );
    }

    // TODO: Move the tests below to View or Color - they test ColorScheme, not Cell primitives.
    private TextView CreateTextView () { return new() { Width = 30, Height = 10 }; }
}
