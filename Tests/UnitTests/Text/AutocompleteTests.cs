using System.Text.RegularExpressions;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.TextTests;

public class AutocompleteTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void CursorLeft_CursorRight_Mouse_Button_Pressed_Does_Not_Show_Popup ()
    {
        var tv = new TextView { Width = 50, Height = 5, Text = "This a long line and against TextView." };

        var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;

        g.AllSuggestions = Regex.Matches (tv.Text, "\\w+")
                                .Select (s => s.Value)
                                .Distinct ()
                                .ToList ();
        Toplevel top = new ();
        top.Add (tv);
        RunState rs = Application.Begin (top);

        for (var i = 0; i < 7; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
            top.SetNeedsDraw ();
            Application.RunIteration (ref rs);

            if (i < 4 || i > 5)
            {
                DriverAssert.AssertDriverContentsWithFrameAre (
                                                               @"
This a long line and against TextView.",
                                                               output
                                                              );
            }
            else
            {
                DriverAssert.AssertDriverContentsWithFrameAre (
                                                               @"
This a long line and against TextView.
     and                              
     against                          ",
                                                               output
                                                              );
            }
        }

        Assert.True (
                     tv.NewMouseEvent (
                                       new () { Position = new (6, 0), Flags = MouseFlags.Button1Pressed }
                                      )
                    );
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This a long line and against TextView.
     and                              
     against                          ",
                                                       output
                                                      );

        Assert.True (tv.NewKeyDownEvent (Key.G));
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This ag long line and against TextView.
     against                           ",
                                                       output
                                                      );

        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft));
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This ag long line and against TextView.
     against                           ",
                                                       output
                                                      );

        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft));
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This ag long line and against TextView.
     against                           ",
                                                       output
                                                      );

        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft));
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This ag long line and against TextView.",
                                                       output
                                                      );

        for (var i = 0; i < 3; i++)
        {
            Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
            top.SetNeedsDraw ();
            Application.RunIteration (ref rs);

            DriverAssert.AssertDriverContentsWithFrameAre (
                                                           @"
This ag long line and against TextView.
     against                           ",
                                                           output
                                                          );
        }

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This a long line and against TextView.
     and                              
     against                          ",
                                                       output
                                                      );

        Assert.True (tv.NewKeyDownEvent (Key.N));
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This an long line and against TextView.
     and                               ",
                                                       output
                                                      );

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
This an long line and against TextView.",
                                                       output
                                                      );
        Assert.Empty (tv.Autocomplete.Suggestions);
        Assert.False (((PopupAutocomplete)tv.Autocomplete)._popup.Visible);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Command ()
    {
        var tv = new TextView { Width = 10, Height = 2, Text = " Fortunately super feature." };
        Toplevel top = new ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (Point.Empty, tv.CursorPosition);
        Assert.NotNull (tv.Autocomplete);
        var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;

        Assert.Empty (g.AllSuggestions);

        g.AllSuggestions = Regex.Matches (tv.Text, "\\w+")
                                .Select (s => s.Value)
                                .Distinct ()
                                .ToList ();
        Assert.Equal (3, g.AllSuggestions.Count);
        Assert.Equal ("Fortunately", g.AllSuggestions [0]);
        Assert.Equal ("super", g.AllSuggestions [1]);
        Assert.Equal ("feature", g.AllSuggestions [^1]);
        Assert.Equal (0, tv.Autocomplete.SelectedIdx);
        Assert.Empty (tv.Autocomplete.Suggestions);
        Assert.True (tv.NewKeyDownEvent (Key.F.WithShift));
        top.Draw ();
        Assert.Equal ("F Fortunately super feature.", tv.Text);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0].Replacement);
        Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1].Replacement);
        Assert.Equal (0, tv.Autocomplete.SelectedIdx);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx].Replacement);
        Assert.True (tv.NewKeyDownEvent (Key.CursorDown));
        top.Draw ();
        Assert.Equal ("F Fortunately super feature.", tv.Text);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0].Replacement);
        Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1].Replacement);
        Assert.Equal (1, tv.Autocomplete.SelectedIdx);
        Assert.Equal ("feature", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx].Replacement);
        Assert.True (tv.NewKeyDownEvent (Key.CursorDown));
        top.Draw ();
        Assert.Equal ("F Fortunately super feature.", tv.Text);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0].Replacement);
        Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1].Replacement);
        Assert.Equal (0, tv.Autocomplete.SelectedIdx);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx].Replacement);
        Assert.True (tv.NewKeyDownEvent (Key.CursorUp));
        top.Draw ();
        Assert.Equal ("F Fortunately super feature.", tv.Text);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0].Replacement);
        Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1].Replacement);
        Assert.Equal (1, tv.Autocomplete.SelectedIdx);
        Assert.Equal ("feature", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx].Replacement);
        Assert.True (tv.NewKeyDownEvent (Key.CursorUp));
        top.Draw ();
        Assert.Equal ("F Fortunately super feature.", tv.Text);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [0].Replacement);
        Assert.Equal ("feature", tv.Autocomplete.Suggestions [^1].Replacement);
        Assert.Equal (0, tv.Autocomplete.SelectedIdx);
        Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions [tv.Autocomplete.SelectedIdx].Replacement);
        Assert.True (tv.Autocomplete.Visible);
        top.Draw ();
        Assert.True (tv.NewKeyDownEvent (new (tv.Autocomplete.CloseKey)));
        Assert.Equal ("F Fortunately super feature.", tv.Text);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.Empty (tv.Autocomplete.Suggestions);
        Assert.Equal (3, g.AllSuggestions.Count);
        Assert.False (tv.Autocomplete.Visible);
        tv.PositionCursor ();
        Assert.True (tv.NewKeyDownEvent (new (tv.Autocomplete.Reopen)));
        Assert.Equal ("F Fortunately super feature.", tv.Text);
        Assert.Equal (new (1, 0), tv.CursorPosition);
        Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
        Assert.Equal (3, g.AllSuggestions.Count);
        Assert.True (tv.NewKeyDownEvent (new (tv.Autocomplete.SelectionKey)));
        tv.PositionCursor ();
        Assert.Equal ("Fortunately Fortunately super feature.", tv.Text);
        Assert.Equal (new (11, 0), tv.CursorPosition);
        Assert.Empty (tv.Autocomplete.Suggestions);
        Assert.Equal (3, g.AllSuggestions.Count);
        Assert.False (tv.Autocomplete.Visible);
        top.Dispose ();
    }

    [Fact]
    public void Test_GenerateSuggestions_Simple ()
    {
        var ac = new TextViewAutocomplete ();

        ((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions =
            new () { "fish", "const", "Cobble" };

        var tv = new TextView ();
        tv.InsertText ("co");

        ac.HostControl = tv;

        ac.GenerateSuggestions (
                                new (
                                     Cell.ToCellList (tv.Text),
                                     2
                                    )
                               );

        Assert.Equal (2, ac.Suggestions.Count);
        Assert.Equal ("const", ac.Suggestions [0].Title);
        Assert.Equal ("Cobble", ac.Suggestions [1].Title);
    }

    [Fact]
    public void TestSettingSchemeOnAutocomplete ()
    {
        var tv = new TextView ();

        // to begin with we should be using the default menu scheme
        Assert.Same (SchemeManager.GetSchemes () ["Menu"], tv.Autocomplete.Scheme);

        // allocate a new custom scheme
        tv.Autocomplete.Scheme = new ()
        {
            Normal = new (Color.Black, Color.Blue), Focus = new (Color.Black, Color.Cyan)
        };

        // should be separate instance
        Assert.NotSame (SchemeManager.GetSchemes () ["Menu"], tv.Autocomplete.Scheme);

        // with the values we set on it
        Assert.Equal (new (Color.Black), tv.Autocomplete.Scheme.Normal.Foreground);
        Assert.Equal (new (Color.Blue), tv.Autocomplete.Scheme.Normal.Background);

        Assert.Equal (new (Color.Black), tv.Autocomplete.Scheme.Focus.Foreground);
        Assert.Equal (new (Color.Cyan), tv.Autocomplete.Scheme.Focus.Background);
    }
}
