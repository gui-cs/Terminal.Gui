using System.Collections.ObjectModel;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public class ListViewTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void Clicking_On_Border_Is_Ignored ()
    {
        var selected = "";

        var lv = new ListView
        {
            Height = 5,
            Width = 7,
            BorderStyle = LineStyle.Single
        };
        lv.SetSource (["One", "Two", "Three", "Four"]);
        lv.SelectedItemChanged += (s, e) => selected = e.Value.ToString ();
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);
        AutoInitShutdownAttribute.RunIteration ();

        Assert.Equal (new (1), lv.Border!.Thickness);
        Assert.Null (lv.SelectedItem);
        Assert.Equal ("", lv.Text);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌─────┐
│One  │
│Two  │
│Three│
└─────┘",
                                                       output);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Clicked });
        Assert.Equal ("", selected);
        Assert.Null (lv.SelectedItem);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Clicked
                                     });
        Assert.Equal ("One", selected);
        Assert.Equal (0, lv.SelectedItem);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (1, 2), Flags = MouseFlags.Button1Clicked
                                     });
        Assert.Equal ("Two", selected);
        Assert.Equal (1, lv.SelectedItem);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (1, 3), Flags = MouseFlags.Button1Clicked
                                     });
        Assert.Equal ("Three", selected);
        Assert.Equal (2, lv.SelectedItem);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (1, 4), Flags = MouseFlags.Button1Clicked
                                     });
        Assert.Equal ("Three", selected);
        Assert.Equal (2, lv.SelectedItem);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Ensures_Visibility_SelectedItem_On_MoveDown_And_MoveUp ()
    {
        ObservableCollection<string> source = [];

        for (var i = 0; i < 20; i++)
        {
            source.Add ($"Line{i}");
        }

        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill (), Source = new ListWrapper<string> (source) };
        var win = new Window ();
        win.Add (lv);
        var top = new Toplevel ();
        top.Add (win);
        SessionToken rs = Application.Begin (top);
        Application.Driver!.SetScreenSize (12, 12);
        AutoInitShutdownAttribute.RunIteration ();

        Assert.Null (lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.ScrollVertical (10));
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Null (lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.MoveDown ());
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.MoveEnd ());
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.ScrollVertical (-20));
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.MoveDown ());
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.ScrollVertical (-20));
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.MoveDown ());
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.MoveHome ());
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.ScrollVertical (20));
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line19    │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
│          │
└──────────┘",
                                                       output
                                                      );

        Assert.True (lv.MoveUp ());
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       output
                                                      );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsureSelectedItemVisible_SelectedItem ()
    {
        ObservableCollection<string> source = [];

        for (var i = 0; i < 10; i++)
        {
            source.Add ($"Item {i}");
        }

        var lv = new ListView { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);
        AutoInitShutdownAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
Item 0
Item 1
Item 2
Item 3
Item 4",
                                                       output
                                                      );

        // EnsureSelectedItemVisible is auto enabled on the OnSelectedChanged
        lv.SelectedItem = 6;
        AutoInitShutdownAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
Item 2
Item 3
Item 4
Item 5
Item 6",
                                                       output
                                                      );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsureSelectedItemVisible_Top ()
    {
        ObservableCollection<string> source = ["First", "Second"];
        var lv = new ListView { Width = Dim.Fill (), Height = 1, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 1;
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);
        AutoInitShutdownAttribute.RunIteration ();

        Assert.Equal ("Second ", GetContents (0));
        Assert.Equal (new (' ', 7), GetContents (1));

        lv.MoveUp ();
        lv.Draw ();

        Assert.Equal ("First  ", GetContents (0));
        Assert.Equal (new (' ', 7), GetContents (1));

        string GetContents (int line)
        {
            var item = "";

            for (var i = 0; i < 7; i++)
            {
                item += Application.Driver?.Contents [line, i].Rune;
            }

            return item;
        }

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void LeftItem_TopItem_Tests ()
    {
        ObservableCollection<string> source = [];

        for (var i = 0; i < 5; i++)
        {
            source.Add ($"Item {i}");
        }

        var lv = new ListView
        {
            X = 1,
            Source = new ListWrapper<string> (source)
        };
        lv.Height = lv.Source.Count;
        lv.Width = lv.MaxLength;
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);
        AutoInitShutdownAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 Item 0
 Item 1
 Item 2
 Item 3
 Item 4",
                                                       output);

        lv.LeftItem = 1;
        lv.TopItem = 1;
        AutoInitShutdownAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 tem 1
 tem 2
 tem 3
 tem 4",
                                                       output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void RowRender_Event ()
    {
        var rendered = false;
        ObservableCollection<string> source = ["one", "two", "three"];
        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill () };
        lv.RowRender += (s, _) => rendered = true;
        var top = new Toplevel ();
        top.Add (lv);
        Application.Begin (top);
        Assert.False (rendered);

        lv.SetSource (source);
        lv.Draw ();
        Assert.True (rendered);
        top.Dispose ();
    }
}
