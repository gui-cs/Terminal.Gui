public class ViewScrollBarTests
{
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
}
