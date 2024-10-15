using System.Collections.ObjectModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ComboBoxTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructor_With_Source_Initialize_With_The_Passed_SelectedItem ()
    {
        var cb = new ComboBox
        {
            Source = new ListWrapper<string> (["One", "Two", "Three"]), SelectedItem = 1
        };
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubviews ();
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
        cb.LayoutSubviews ();
        Assert.Equal (string.Empty, cb.Text);
        Assert.Null (cb.Source);
        Assert.Equal (new Rectangle (0, 0, 0, 2), cb.Frame);
        Assert.Equal (-1, cb.SelectedItem);

        cb = new ComboBox { Text = "Test" };
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubviews ();
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
        cb.LayoutSubviews ();
        Assert.Equal (string.Empty, cb.Text);
        Assert.NotNull (cb.Source);
        Assert.Equal (new Rectangle (1, 2, 10, 20), cb.Frame);
        Assert.Equal (-1, cb.SelectedItem);

        cb = new ComboBox { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        cb.BeginInit ();
        cb.EndInit ();
        cb.LayoutSubviews ();
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

        var top = new Toplevel ();
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

        var top = new Toplevel ();
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
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.False (cb.ReadOnly);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.NewMouseEvent (new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
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
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void
        HideDropdownListOnClick_False_ReadOnly_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5, HideDropdownListOnClick = false, ReadOnly = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.True (cb.ReadOnly);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
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

    [Fact]
    [AutoInitShutdown]
    public void HideDropdownListOnClick_Gets_Sets ()
    {
        var selected = "";
        var cb = new ComboBox { Height = 4, Width = 5 };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.False (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (0, 1), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (0, 1), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        cb.HideDropdownListOnClick = true;

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (0, 2), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("Three", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (0, 2), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("Three", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
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
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (-1, 0), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (0, -1), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Frame.Width, 0), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.Subviews [1]
                       .NewMouseEvent (
                                    new MouseEventArgs { Position = new (0, cb.Frame.Height), Flags = MouseFlags.Button1Clicked }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigurationManager.ConfigLocations.DefaultOnly)]
    public void HideDropdownListOnClick_True_Highlight_Current_Item ()
    {
        var selected = "";
        var cb = new ComboBox { Width = 6, Height = 4, HideDropdownListOnClick = true };
        cb.SetSource (["One", "Two", "Three"]);
        cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
        var top = new Toplevel ();

        View otherView = new View () { CanFocus = true };

        top.Add (otherView, cb);
        Application.Begin (top);

        Assert.True (cb.HasFocus);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        cb.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ▼
One   
Two   
Three ",
                                                      output
                                                     );

        Attribute [] attributes =
        {
            // 0
            cb.Subviews [0].ColorScheme.Focus,

            // 1
            cb.Subviews [1].ColorScheme.HotFocus,

            // 2
            cb.Subviews [1].GetNormalColor ()
        };

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000
222222
222222
222222",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        cb.Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000
222222
000002
222222",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);
        cb.Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000
222222
222222
000002",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Three", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        cb.Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000
222222
222222
000002",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorUp));
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        cb.Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000
222222
000002
111112",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorUp));
        Assert.Equal ("Three", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        cb.Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000
000002
222222
111112",
                                               Application.Driver,
                                               attributes
                                              );

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
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorUp));

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
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
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.Enter));
        Assert.Equal ("Two", selected);
        Assert.False (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);

        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.Equal ("Two", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
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
        var top = new Toplevel ();
        top.Add (cb);
        Application.Begin (top);

        Assert.True (cb.HideDropdownListOnClick);
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (
                     cb.NewMouseEvent (
                                    new MouseEventArgs { Position = new (cb.Viewport.Right - 1, 0), Flags = MouseFlags.Button1Pressed }
                                   )
                    );
        Assert.Equal ("", selected);
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Assert.True (cb.Subviews [1].NewKeyDownEvent (Key.CursorDown));
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
        var top = new Toplevel ();

        top.Add (cb);

        var otherView = new View () { CanFocus = true };
        top.Add (otherView);
        Application.Begin (top);

        cb.SetSource (source);

        Assert.True (cb.HasFocus);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal (string.Empty, cb.Text);
        var opened = false;

        cb.OpenSelectedItem += (s, _) => opened = true;

        Assert.False (Application.RaiseKeyDownEvent (Key.Enter));
        Assert.False (opened);

        cb.Text = "Tw";
        Assert.False (Application.RaiseKeyDownEvent (Key.Enter));
        Assert.True (opened);
        Assert.Equal ("Tw", cb.Text);
        Assert.False (cb.IsShow);

        cb.SetSource<string> (null);
        Assert.False (cb.IsShow);
        Assert.False (Application.RaiseKeyDownEvent (Key.Enter));
        Assert.True (Application.RaiseKeyDownEvent (Key.F4)); // with no source also expand empty
        Assert.True (cb.IsShow);

        Assert.Equal (-1, cb.SelectedItem);
        cb.SetSource (source);
        cb.Text = "";
        Assert.True (Application.RaiseKeyDownEvent (Key.F4)); // collapse
        Assert.False (cb.IsShow);
        Assert.True (Application.RaiseKeyDownEvent (Key.F4)); // expand
        Assert.True (cb.IsShow);
        cb.Collapse ();
        Assert.False (cb.IsShow);
        Assert.True (cb.HasFocus);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown)); // losing focus
        Assert.False (cb.IsShow);
        Assert.False (cb.HasFocus);
        cb.SetFocus ();
        Assert.False (cb.IsShow);
        Assert.True (cb.HasFocus);
        cb.Expand ();

        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);

        cb.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
One      ▼
One       
",
                                                      output
                                                     );

        Assert.True (Application.RaiseKeyDownEvent (Key.PageDown));
        Assert.True (cb.IsShow);
        Assert.Equal (1, cb.SelectedItem);
        Assert.Equal ("Two", cb.Text);
        //        Application.Begin (top);

        cb.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
Two      ▼
Two       
",
                                                      output
                                                     );

        Assert.True (Application.RaiseKeyDownEvent (Key.PageDown));
        Assert.True (cb.IsShow);
        Assert.Equal (2, cb.SelectedItem);
        Assert.Equal ("Three", cb.Text);
        //Application.Begin (top);

        cb.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
Three    ▼
Three     
",
                                                      output
                                                     );
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

    [Fact]
    public void Source_Equal_Null_Or_Count_Equal_Zero_Sets_SelectedItem_Equal_To_Minus_One ()
    {
        Application.Navigation = new ();
        var cb = new ComboBox ();
        var top = new Toplevel ();
        Application.Top = top;

        top.Add (cb);
        top.FocusDeepest (NavigationDirection.Forward, null);
        Assert.Null (cb.Source);
        Assert.Equal (-1, cb.SelectedItem);
        ObservableCollection<string> source = [];
        cb.SetSource (source);
        Assert.NotNull (cb.Source);
        Assert.Equal (0, cb.Source.Count);
        Assert.Equal (-1, cb.SelectedItem);
        source.Add ("One");
        Assert.Equal (1, cb.Source.Count);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.True (Application.RaiseKeyDownEvent (Key.F4));
        Assert.True (cb.IsShow);
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        source.Add ("Two");
        Assert.Equal (0, cb.SelectedItem);
        Assert.Equal ("One", cb.Text);
        cb.Text = "T";
        Assert.True (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("T", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.Enter));
        Assert.False (cb.IsShow);
        Assert.Equal (2, cb.Source.Count);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("T", cb.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.Esc));
        Assert.False (cb.IsShow);
        Assert.Equal (-1, cb.SelectedItem); // retains last accept selected item
        Assert.Equal ("", cb.Text); // clear text
        cb.SetSource (new ObservableCollection<string> ());
        Assert.Equal (0, cb.Source.Count);
        Assert.Equal (-1, cb.SelectedItem);
        Assert.Equal ("", cb.Text);

        Application.Top.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }
}
