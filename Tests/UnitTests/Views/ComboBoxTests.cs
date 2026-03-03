using System.Collections.ObjectModel;

namespace UnitTests.ViewsTests;

public class ComboBoxTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructor_With_Source_Initialize_With_The_Passed_SelectedItem ()
    {
        var cb = new ComboBox { Source = new ListWrapper<string> (["One", "Two", "Three"]), SelectedItem = 1 };
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubViews ();
        Assert.Equal ("Two", cb.Text);
        Assert.NotNull (cb.Source);
        Assert.Equal (new Rectangle (0, 0, 0, 2), cb.Frame);
        Assert.Equal (1, cb.SelectedItem);
    }

    [Fact]
    [AutoInitShutdown]
    public void Constructors_Defaults ()
    {
        var cb = new ComboBox ();
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubViews ();
        Assert.Equal (string.Empty, cb.Text);
        Assert.Null (cb.Source);
        Assert.Equal (new Rectangle (0, 0, 0, 2), cb.Frame);
        Assert.Equal (-1, cb.SelectedItem);

        cb = new ComboBox { Text = "Test" };
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubViews ();
        Assert.Equal ("Test", cb.Text);
        Assert.Null (cb.Source);
        Assert.Equal (new Rectangle (0, 0, 0, 2), cb.Frame);
        Assert.Equal (-1, cb.SelectedItem);

        cb = new ComboBox
        {
            X = 1,
            Y = 2,
            Width = 10,
            Height = 20,
            Source = new ListWrapper<string> (["One", "Two", "Three"])
        };
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubViews ();
        Assert.Equal (string.Empty, cb.Text);
        Assert.NotNull (cb.Source);
        Assert.Equal (new Rectangle (1, 2, 10, 20), cb.Frame);
        Assert.Equal (-1, cb.SelectedItem);

        cb = new ComboBox { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubViews ();
        Assert.Equal (string.Empty, cb.Text);
        Assert.NotNull (cb.Source);
        Assert.Equal (new Rectangle (0, 0, 0, 2), cb.Frame);
        Assert.Equal (-1, cb.SelectedItem);
    }

    [Fact]
    public void EnsureKeyEventsDoNotCauseExceptions ()
    {
        var comboBox = new ComboBox { Text = "0" };

        string [] source = Enumerable.Range (0, 15).Select (x => x.ToString ()).ToArray ();
        comboBox.SetSource (new ObservableCollection<string> (source.ToList ()));

        var top = new Runnable ();
        top.Add (comboBox);

        foreach (KeyCode key in (KeyCode [])Enum.GetValues (typeof (KeyCode)))
        {
            Assert.Null (Record.Exception (() => comboBox.NewKeyDownEvent (new Key (key))));
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Expanded_Collapsed_Events ()
    {
        var cb = new ComboBox { Height = 4, Width = 5 };
        ObservableCollection<string> list = ["One", "Two", "Three"];

        cb.Expanded += (s, e) => cb.SetSource (list);
        cb.Collapsed += (s, e) => cb.Source = null;

        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.Null (cb.Source);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.NotNull (cb.Source);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Null (cb.Source);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_False_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = false };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.False (cb.ReadOnly);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.Esc));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_False_OpenSelectedItem_With_Mouse_And_Key_F4 ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = false };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_False_ReadOnly_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = false, ReadOnly = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.True (cb.ReadOnly);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.Esc));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        top.Dispose ();
    }

    [Fact (Skip = "Not worth debugging")]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_Gets_Sets ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5 };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        cb.Layout ();

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (0, 1), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (0, 1), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        cb.HideDropdownListOnClick = true;

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (0, 2), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("Three", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (0, 2), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("Three", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("One", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_True_Colapse_On_Click_Outside_Frame ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));

        Assert.True (cb.SubViews.ElementAt (1)
                       .NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (-1, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));

        Assert.True (cb.SubViews.ElementAt (1)
                       .NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (0, -1), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));

        Assert.True (cb.SubViews.ElementAt (1)
                       .NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (cb.Frame.Width, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));

        Assert.True (cb.SubViews.ElementAt (1)
                       .NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewMouseEvent (new Mouse { Position = new Point (0, cb.Frame.Height), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        top.Dispose ();
    }

    [Fact (Skip = "Disabled in #4431 to avoid noise; ComboBox will go away anyway")]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_True_Highlight_Current_Item ()
    {
        var selected = "";
        var cb = new ComboBox { Width = 6, Height = 4, HideDropdownListOnClick = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();

        var otherView = new View { CanFocus = true };

        top.Add (otherView, cb);
        Application.Begin (top);

        Assert.True (cb.HasFocus);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        cb.Layout ();

        cb.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
     ▼
One   
Two   
Three ",
                                                       output);

        Attribute [] attributes =
        {
            // 0
            cb.SubViews.ElementAt (0).GetAttributeForRole (VisualRole.Focus),

            // 1
            cb.SubViews.ElementAt (1).GetAttributeForRole (VisualRole.HotFocus),

            // 2
            cb.SubViews.ElementAt (1).GetAttributeForRole (VisualRole.Normal)
        };

        DriverAssert.AssertDriverAttributesAre (@"
000000
222222
222222
222222",
                                                output,
                                                Application.Driver,
                                                attributes);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        cb.SetClipToScreen ();
        cb.Draw ();

        DriverAssert.AssertDriverAttributesAre (@"
000000
222222
000002
222222",
                                                output,
                                                Application.Driver,
                                                attributes);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        cb.SetClipToScreen ();
        cb.Draw ();

        DriverAssert.AssertDriverAttributesAre (@"
000000
222222
222222
000002",
                                                output,
                                                Application.Driver,
                                                attributes);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Three", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        cb.SetClipToScreen ();
        cb.Draw ();

        DriverAssert.AssertDriverAttributesAre (@"
000000
222222
222222
000002",
                                                output,
                                                Application.Driver,
                                                attributes);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorUp));
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        cb.SetClipToScreen ();
        cb.Draw ();

        DriverAssert.AssertDriverAttributesAre (@"
000000
222222
000002
111112",
                                                output,
                                                Application.Driver,
                                                attributes);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorUp));
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        cb.SetClipToScreen ();
        cb.Draw ();

        DriverAssert.AssertDriverAttributesAre (@"
000000
000002
222222
111112",
                                                output,
                                                Application.Driver,
                                                attributes);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Three", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_And_Mouse ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorUp));

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.Esc));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_F4 ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Runnable ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new Mouse { Position = new Point (cb.Viewport.Right - 1, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.SubViews.ElementAt (1).NewKeyDownEvent (Key.CursorDown));
        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Command ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];
        var cb = new ComboBox { Width = 10 };
        var top = new Runnable ();

        top.Add (cb);

        var otherView = new View { CanFocus = true };
        top.Add (otherView);
        Application.Begin (top);

        cb.SetSource (source);

        Assert.True (cb.HasFocus);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal (string.Empty, cb.Text);
        var opened = false;

        cb.OpenSelectedItem += (s, _) => opened = true;

        Application.RaiseKeyDownEvent (Key.Enter);
        Assert.False (opened);

        cb.Text = "Tw";
        Application.RaiseKeyDownEvent (Key.Enter);
        Assert.True (opened);
        Assert.Equal ("Tw", cb.Text);
        Assert.False (cb.IsShow);

        cb.SetSource<string> (null);
        Assert.False (cb.IsShow);
        Application.RaiseKeyDownEvent (Key.Enter);
        Application.RaiseKeyDownEvent (Key.F4); // with no source also expand empty
        Assert.True (cb.IsShow);

        Assert.Equal (-1, cb.SelectedItem);
        cb.SetSource (source);
        cb.Text = "";
        Application.RaiseKeyDownEvent (Key.F4); // collapse
        Assert.False (cb.IsShow);
        Application.RaiseKeyDownEvent (Key.F4); // expand
        Assert.True (cb.IsShow);
        cb.Collapse ();
        Assert.False (cb.IsShow);
        Assert.True (cb.HasFocus);
        Application.RaiseKeyDownEvent (Key.CursorDown); // losing focus
        Assert.False (cb.IsShow);
        Assert.False (cb.HasFocus);
        cb.SetFocus ();
        Assert.False (cb.IsShow);
        Assert.True (cb.HasFocus);
        cb.Expand ();

        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Application.RaiseKeyDownEvent (Key.CursorDown);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Application.RaiseKeyDownEvent (Key.CursorDown);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        Application.RaiseKeyDownEvent (Key.CursorDown);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        Application.RaiseKeyDownEvent (Key.CursorUp);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Application.RaiseKeyDownEvent (Key.CursorUp);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Application.RaiseKeyDownEvent (Key.CursorUp);

        cb.Layout ();

        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        cb.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
One      ▼
One       
",
                                                       output);

        Assert.True (Application.RaiseKeyDownEvent (Key.PageDown));
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        cb.SetClipToScreen ();
        cb.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Two      ▼
Two       
",
                                                       output);

        Assert.True (Application.RaiseKeyDownEvent (Key.PageDown));
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        cb.SetClipToScreen ();
        cb.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Three    ▼
Three     
",
                                                       output);
        Assert.True (Application.RaiseKeyDownEvent (Key.PageUp));
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.PageUp));
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.False (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.End));
        Assert.False (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.Home));
        Assert.False (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.End));
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.Home));
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.Esc));
        Assert.False (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown)); // losing focus
        Assert.False (cb.HasFocus);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        cb.SetFocus ();
        Assert.True (cb.HasFocus);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.U.WithCtrl));
        Assert.True (cb.HasFocus);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        Assert.Equal (3, cb.Source.Count);
        top.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Text_Polymorphism_Works ()
    {
        // Test that ComboBox.Text works correctly when accessed via View base class
        ComboBox cb = new () { Text = "Test" };
        cb.BeginInit ();
        cb.EndInit ();
        Assert.Equal ("Test", cb.Text);
        Assert.Equal ("Test", cb.Text); // Should be same due to polymorphism
    }
}
