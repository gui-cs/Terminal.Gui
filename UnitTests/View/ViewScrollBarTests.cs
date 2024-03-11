using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ViewScrollBarTests
{
    public ViewScrollBarTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    [Fact]
    public void EnableScrollBars_Defaults ()
    {
        var view = new View ();
        Assert.False (view.EnableScrollBars);
        Assert.Empty (view.Subviews);
        Assert.False (view.AutoHideScrollBars);
        Assert.False (view.KeepContentAlwaysInContentArea);
        Assert.False (view.ShowVerticalScrollBar);
        Assert.False (view.ShowHorizontalScrollBar);
        Assert.False (view.UseContentOffset);

        view.EnableScrollBars = true;
        Assert.Equal (3, view.Subviews.Count);
        Assert.True (view.AutoHideScrollBars);
        Assert.True (view.KeepContentAlwaysInContentArea);
        Assert.True (view.ShowVerticalScrollBar);
        Assert.True (view.ShowHorizontalScrollBar);
        Assert.False (view.UseContentOffset);

        view.EnableScrollBars = false;
        Assert.Empty (view.Subviews);
        Assert.False (view.AutoHideScrollBars);
        Assert.False (view.KeepContentAlwaysInContentArea);
        Assert.False (view.ShowVerticalScrollBar);
        Assert.False (view.ShowHorizontalScrollBar);
        Assert.False (view.UseContentOffset);
    }

    [Theory]
    [MemberData (nameof (ScrollBarKeyBindings))]
    public void KeyBindings_On_Border (Key firstKey, int expectedFirstX, int expectedFirstY, Key secondKey, int expectedSecondX, int expectedSecondY)
    {
        var view = new View { Width = 10, Height = 10, ContentSize = new (20, 20), UseContentOffset = true };
        view.Border.EnableScrollBars = true;
        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.Border.OnInvokingKeyBindings (firstKey));
        Assert.Equal (new (expectedFirstX, expectedFirstY), view.ContentOffset);
        Assert.True (view.Border.OnInvokingKeyBindings (secondKey));
        Assert.Equal (new (expectedSecondX, expectedSecondY), view.ContentOffset);
    }

    [Theory]
    [MemberData (nameof (ScrollBarKeyBindings))]
    public void KeyBindings_On_ContentArea (Key firstKey, int expectedFirstX, int expectedFirstY, Key secondKey, int expectedSecondX, int expectedSecondY)
    {
        var view = new View { Width = 10, Height = 10, EnableScrollBars = true, ContentSize = new (20, 20), UseContentOffset = true };
        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.OnInvokingKeyBindings (firstKey));
        Assert.Equal (new (expectedFirstX, expectedFirstY), view.ContentOffset);
        Assert.True (view.OnInvokingKeyBindings (secondKey));
        Assert.Equal (new (expectedSecondX, expectedSecondY), view.ContentOffset);
    }

    [Theory]
    [MemberData (nameof (ScrollBarKeyBindings))]
    public void KeyBindings_On_Margin (Key firstKey, int expectedFirstX, int expectedFirstY, Key secondKey, int expectedSecondX, int expectedSecondY)
    {
        var view = new View { Width = 10, Height = 10, ContentSize = new (20, 20), UseContentOffset = true };
        view.Margin.EnableScrollBars = true;
        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.Margin.OnInvokingKeyBindings (firstKey));
        Assert.Equal (new (expectedFirstX, expectedFirstY), view.ContentOffset);
        Assert.True (view.Margin.OnInvokingKeyBindings (secondKey));
        Assert.Equal (new (expectedSecondX, expectedSecondY), view.ContentOffset);
    }

    [Theory]
    [MemberData (nameof (ScrollBarKeyBindings))]
    public void KeyBindings_On_Padding (Key firstKey, int expectedFirstX, int expectedFirstY, Key secondKey, int expectedSecondX, int expectedSecondY)
    {
        var view = new View { Width = 10, Height = 10, ContentSize = new (20, 20), UseContentOffset = true };
        view.Padding.EnableScrollBars = true;
        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.Padding.OnInvokingKeyBindings (firstKey));
        Assert.Equal (new (expectedFirstX, expectedFirstY), view.ContentOffset);
        Assert.True (view.Padding.OnInvokingKeyBindings (secondKey));
        Assert.Equal (new (expectedSecondX, expectedSecondY), view.ContentOffset);
    }

    [Theory]
    [MemberData (nameof (KeyBindingsWithoutScrollBars))]
    public void KeyBindings_Without_EnableScrollBars (
        Key firstKey,
        int expectedFirstX,
        int expectedFirstY,
        Key secondKey,
        int expectedSecondX,
        int expectedSecondY
    )
    {
        var view = new View { Width = 10, Height = 10, ContentSize = new (20, 20), UseContentOffset = true };
        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.OnInvokingKeyBindings (firstKey));
        Assert.Equal (new (expectedFirstX, expectedFirstY), view.ContentOffset);
        Assert.True (view.OnInvokingKeyBindings (secondKey));
        Assert.Equal (new (expectedSecondX, expectedSecondY), view.ContentOffset);
    }

    public static TheoryData<Key, int, int, Key, int, int> KeyBindingsWithoutScrollBars =>
        new ()
        {
            { Key.CursorDown, 0, -1, Key.CursorUp, 0, 0 },
            { Key.End, 0, -10, Key.Home, 0, 0 },
            { Key.PageDown, 0, -10, Key.PageUp, 0, 0 },
            { Key.CursorRight, -1, 0, Key.CursorLeft, 0, 0 },
            { Key.End.WithShift, -10, 0, Key.Home.WithShift, 0, 0 },
            { Key.PageDown.WithShift, -10, 0, Key.PageUp.WithShift, 0, 0 }
        };

    public static TheoryData<Key, int, int, Key, int, int> ScrollBarKeyBindings =>
        new ()
        {
            { Key.CursorDown, 0, -1, Key.CursorUp, 0, 0 },
            { Key.End, 0, -11, Key.Home, 0, 0 },
            { Key.PageDown, 0, -9, Key.PageUp, 0, 0 },
            { Key.CursorRight, -1, 0, Key.CursorLeft, 0, 0 },
            { Key.End.WithShift, -11, 0, Key.Home.WithShift, 0, 0 },
            { Key.PageDown.WithShift, -9, 0, Key.PageUp.WithShift, 0, 0 }
        };

    [Fact]
    [SetupFakeDriver]
    public void Scrolling_Without_ScrollBars ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (15, 11);

        var top = new View { Width = 15, Height = 11, ColorScheme = new (Attribute.Default) };

        var view = new View
        {
            X = Pos.Center (), Y = Pos.Center (), Width = 9, Height = 6,
            Text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line", CanFocus = true,
            UseContentOffset = true
        };
        view.TextFormatter.WordWrap = false;
        view.TextFormatter.MultiLine = true;
        string [] strings = view.Text.Split ("\n").ToArray ();
        view.ContentSize = new (strings.OrderByDescending (s => s.Length).First ().GetColumns (), strings.Length);

        view.ColorScheme = new()
        {
            Normal = new (Color.Green, Color.Red),
            Focus = new (Color.Red, Color.Green)
        };

        var view2 = new View { X = Pos.Center (), Y = Pos.Bottom (view) + 1, Text = "Test", CanFocus = true, AutoSize = true };
        view2.ColorScheme = view.ColorScheme;
        top.Add (view, view2);
        top.BeginInit ();
        top.EndInit ();
        top.FocusFirst ();
        top.LayoutSubviews ();
        top.Draw ();
        Assert.True (view.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (12, view.ContentSize.Width);
        Assert.Equal (7, view.ContentSize.Height);
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=3,Y=2,Width=9,Height=6}", view.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.Frame.ToString ());
        Assert.Equal ("{Width=9, Height=6}", view.TextFormatter.Size.ToString ());

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
   First Lin
   Second Li
   Third Lin
   Fourth Li
   Fifth Lin
   Sixth Lin
            
     Test   ",
                                                      _output);

        Attribute [] attrs =
        [
            Attribute.Default,
            new (Color.Red, Color.Green),
            new (Color.Green, Color.Red),
            new (Color.White, Color.DarkGray),
            new (Color.Black, Color.Gray)
        ];

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000000000000
000000000000000
000111111111000
000111111111000
000111111111000
000111111111000
000111111111000
000111111111000
000000000000000
000002222000000
000000000000000",
                                               null,
                                               attrs);

        Assert.True (view.OnInvokingKeyBindings (Key.End));
        Assert.True (view.OnInvokingKeyBindings (Key.End.WithShift));
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
   ond Line 
   rd Line  
   rth Line 
   th Line  
   th Line  
   enth Line
            
     Test   ",
                                                      _output);
    }
}
