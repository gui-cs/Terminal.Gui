﻿using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class RuneCellTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var rc = new RuneCell ();
        Assert.NotNull (rc);
        Assert.Equal (0, rc.Rune.Value);
        Assert.Null (rc.ColorScheme);
    }

    [Fact]
    public void Equals_False ()
    {
        var rc1 = new RuneCell ();

        var rc2 = new RuneCell
        {
            Rune = new ('a'), ColorScheme = new() { Normal = new (Color.Red) }
        };
        Assert.False (rc1.Equals (rc2));
        Assert.False (rc2.Equals (rc1));

        rc1.Rune = new ('a');
        rc1.ColorScheme = new ();
        Assert.Equal (rc1.Rune, rc2.Rune);
        Assert.False (rc1.Equals (rc2));
        Assert.False (rc2.Equals (rc1));
    }

    [Fact]
    public void Equals_True ()
    {
        var rc1 = new RuneCell ();
        var rc2 = new RuneCell ();
        Assert.True (rc1.Equals (rc2));
        Assert.True (rc2.Equals (rc1));

        rc1.Rune = new ('a');
        rc1.ColorScheme = new ();
        rc2.Rune = new ('a');
        rc2.ColorScheme = new ();
        Assert.True (rc1.Equals (rc2));
        Assert.True (rc2.Equals (rc1));
    }

    [Fact]
    [SetupFakeDriver]
    public void RuneCell_LoadRuneCells_InheritsPreviousColorScheme ()
    {
        List<RuneCell> runeCells = new ();

        foreach (KeyValuePair<string, ColorScheme> color in Colors.ColorSchemes)
        {
            string csName = color.Key;

            foreach (Rune rune in csName.EnumerateRunes ())
            {
                runeCells.Add (new() { Rune = rune, ColorScheme = color.Value });
            }

            runeCells.Add (new() { Rune = (Rune)'\n', ColorScheme = color.Value });
        }

        TextView tv = CreateTextView ();
        tv.Load (runeCells);
        Application.Top = new Toplevel ();
        Application.Top.Add (tv);
        Application.Top.BeginInit();
        Application.Top.EndInit();

        Application.Top.Draw ();

        Assert.True (tv.InheritsPreviousColorScheme);

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
            Colors.ColorSchemes ["TopLevel"].Focus,

            // 1
            Colors.ColorSchemes ["Base"].Focus,

            // 2
            Colors.ColorSchemes ["Dialog"].Focus,

            // 3
            Colors.ColorSchemes ["Menu"].Focus,

            // 4
            Colors.ColorSchemes ["Error"].Focus
        };

        var expectedColor = @"
0000000000
1111000000
2222220000
3333000000
4444400000";
        TestHelpers.AssertDriverAttributesAre (expectedColor, Application.Driver, attributes);

        tv.WordWrap = true;
        Application.Top.Draw ();
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

        Application.Top.Draw ();

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
0000000000
1111000000
2222220000
3333000000
4444444444
4444000000
4444444440";
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
        Application.Top.Draw ();

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
0000000000
1111000000
2222220000
3333000000
4444444444
4444000000
4444440000
4440000000";
        TestHelpers.AssertDriverAttributesAre (expectedColor, Application.Driver, attributes);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void RuneCell_LoadRuneCells_Without_ColorScheme_Is_Never_Null ()
    {
        List<RuneCell> cells = new ()
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
            List<RuneCell> line = tv.GetLine (i);

            foreach (RuneCell rc in line)
            {
                Assert.NotNull (rc.ColorScheme);
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void RuneCellEventArgs_WordWrap_True ()
    {
        var eventCount = 0;

        List<List<RuneCell>> text = new ()
        {
            TextModel.ToRuneCells (
                                   "This is the first line.".ToRunes ()
                                  ),
            TextModel.ToRuneCells (
                                   "This is the second line.".ToRunes ()
                                  )
        };
        TextView tv = CreateTextView ();
        tv.DrawNormalColor += _textView_DrawColor;
        tv.DrawReadOnlyColor += _textView_DrawColor;
        tv.DrawSelectionColor += _textView_DrawColor;
        tv.DrawUsedColor += _textView_DrawColor;

        void _textView_DrawColor (object sender, RuneCellEventArgs e)
        {
            Assert.Equal (e.Line [e.Col], text [e.UnwrappedPosition.Row] [e.UnwrappedPosition.Col]);
            eventCount++;
        }

        tv.Text = $"{TextModel.ToString (text [0])}\n{TextModel.ToString (text [1])}\n";
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
        var rc1 = new RuneCell ();

        var rc2 = new RuneCell
        {
            Rune = new ('a'), ColorScheme = new() { Normal = new (Color.Red) }
        };
        Assert.Equal ("U+0000 '\0'; null", rc1.ToString ());

        Assert.Equal (
                      "U+0061 'a'; Normal: [Red,Red]; Focus: [White,Black]; HotNormal: [White,Black]; HotFocus: [White,Black]; Disabled: [White,Black]",
                      rc2.ToString ()
                     );
    }

    // TODO: Move the tests below to View or Color - they test ColorScheme, not RuneCell primitives.
    private TextView CreateTextView () { return new() { Width = 30, Height = 10 }; }
}
