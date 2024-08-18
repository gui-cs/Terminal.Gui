using JetBrains.Annotations;

namespace Terminal.Gui.ViewsTests;

[TestSubject (typeof (Shortcut))]
public class ShortcutTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var shortcut = new Shortcut ();

        Assert.NotNull (shortcut);
        Assert.True (shortcut.CanFocus);
        Assert.IsType<DimAuto> (shortcut.Width);
        Assert.IsType<DimAuto> (shortcut.Height);

        // TOOD: more
    }

    [Theory]
    [InlineData ("", "", KeyCode.Null, 2)]
    [InlineData ("C", "", KeyCode.Null, 3)]
    [InlineData ("", "H", KeyCode.Null, 5)]
    [InlineData ("", "", KeyCode.K, 5)]
    [InlineData ("C", "", KeyCode.K, 6)]
    [InlineData ("C", "H", KeyCode.Null, 6)]
    [InlineData ("", "H", KeyCode.K, 8)]
    [InlineData ("C", "H", KeyCode.K, 9)]
    public void NaturalSize (string command, string help, KeyCode key, int expectedWidth)
    {
        var shortcut = new Shortcut
        {
            Title = command,
            HelpText = help,
            Key = key
        };

        Assert.IsType<DimAuto> (shortcut.Width);
        Assert.IsType<DimAuto> (shortcut.Height);
        shortcut.SetRelativeLayout (new (100, 100));

        // |0123456789
        // | C  H  K |
        Assert.Equal (expectedWidth, shortcut.Frame.Width);
    }

    [Theory]
    [InlineData (5, 0, 3, 6)]
    [InlineData (6, 0, 3, 6)]
    [InlineData (7, 0, 3, 6)]
    [InlineData (8, 0, 3, 6)]
    [InlineData (9, 0, 3, 6)]
    [InlineData (10, 0, 4, 7)]
    [InlineData (11, 0, 5, 8)]
    public void Set_Width_Layouts_Correctly (int width, int expectedCmdX, int expectedHelpX, int expectedKeyX)
    {
        var shortcut = new Shortcut
        {
            Width = width,
            Title = "C",
            Text = "H",
            Key = Key.K
        };

        shortcut.LayoutSubviews ();
        shortcut.SetRelativeLayout (new (100, 100));

        // 0123456789
        // -C--H--K- 
        Assert.Equal (expectedCmdX, shortcut.CommandView.Frame.X);
        Assert.Equal (expectedHelpX, shortcut.HelpView.Frame.X);
        Assert.Equal (expectedKeyX, shortcut.KeyView.Frame.X);
    }

    [Fact]
    public void CommandView_Text_And_Title_Track ()
    {
        var shortcut = new Shortcut
        {
            Title = "T"
        };

        Assert.Equal (shortcut.Title, shortcut.CommandView.Text);

        shortcut = new ();

        shortcut.CommandView = new ()
        {
            Text = "T"
        };
        Assert.Equal (shortcut.Title, shortcut.CommandView.Text);
    }

    [Fact]
    public void HelpText_And_Text_Are_The_Same ()
    {
        var shortcut = new Shortcut
        {
            Text = "H"
        };

        Assert.Equal (shortcut.Text, shortcut.HelpText);

        shortcut = new ()
        {
            HelpText = "H"
        };

        Assert.Equal (shortcut.Text, shortcut.HelpText);
    }

    [Theory]
    [InlineData (KeyCode.Null, "")]
    [InlineData (KeyCode.F1, "F1")]
    public void KeyView_Text_Tracks_Key (KeyCode key, string expected)
    {
        var shortcut = new Shortcut
        {
            Key = key
        };

        Assert.Equal (expected, shortcut.KeyView.Text);
    }

    // Test Key
    [Fact]
    public void Key_Defaults_To_Empty ()
    {
        var shortcut = new Shortcut ();

        Assert.Equal (Key.Empty, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.F1;

        Assert.Equal (Key.F1, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set_To_Empty ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.Empty;

        Assert.Equal (Key.Empty, shortcut.Key);
    }


    [Fact]
    public void Key_Set_Binds_Key_To_CommandView_Accept ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.F1;

        // TODO:
    }

    [Fact]
    public void Key_Changing_Removes_Previous_Binding ()
    {
        Shortcut shortcut = new Shortcut ();

        shortcut.Key = Key.A;
        Assert.Contains (Key.A, shortcut.KeyBindings.Bindings.Keys);

        shortcut.Key = Key.B;
        Assert.DoesNotContain (Key.A, shortcut.KeyBindings.Bindings.Keys);
        Assert.Contains (Key.B, shortcut.KeyBindings.Bindings.Keys);
    }

    // Test Key gets bound correctly
    [Fact]
    public void KeyBindingScope_Defaults_To_HotKey ()
    {
        var shortcut = new Shortcut ();

        Assert.Equal (KeyBindingScope.HotKey, shortcut.KeyBindingScope);
    }

    [Fact]
    public void KeyBindingScope_Can_Be_Set ()
    {
        var shortcut = new Shortcut ();

        shortcut.KeyBindingScope = KeyBindingScope.Application;

        Assert.Equal (KeyBindingScope.Application, shortcut.KeyBindingScope);
    }

    [Fact]
    public void KeyBindingScope_Changing_Adjusts_KeyBindings ()
    {
        Shortcut shortcut = new Shortcut ();

        shortcut.Key = Key.A;
        Assert.Contains (Key.A, shortcut.KeyBindings.Bindings.Keys);

        shortcut.KeyBindingScope = KeyBindingScope.Application;
        Assert.DoesNotContain (Key.A, shortcut.KeyBindings.Bindings.Keys);
        Assert.Contains (Key.A, Application.KeyBindings.Bindings.Keys);

        shortcut.KeyBindingScope = KeyBindingScope.HotKey;
        Assert.Contains (Key.A, shortcut.KeyBindings.Bindings.Keys);
        Assert.DoesNotContain (Key.A, Application.KeyBindings.Bindings.Keys);
    }

    [Theory]
    [InlineData (Orientation.Horizontal)]
    [InlineData (Orientation.Vertical)]
    public void Orientation_SetsCorrectly (Orientation orientation)
    {
        var shortcut = new Shortcut
        {
            Orientation = orientation
        };

        Assert.Equal (orientation, shortcut.Orientation);
    }

    [Theory]
    [InlineData (AlignmentModes.StartToEnd)]
    [InlineData (AlignmentModes.EndToStart)]
    public void AlignmentModes_SetsCorrectly (AlignmentModes alignmentModes)
    {
        var shortcut = new Shortcut
        {
            AlignmentModes = alignmentModes
        };

        Assert.Equal (alignmentModes, shortcut.AlignmentModes);
    }

    [Fact]
    public void Action_SetsAndGetsCorrectly ()
    {
        var actionInvoked = false;

        var shortcut = new Shortcut
        {
            Action = () => { actionInvoked = true; }
        };

        shortcut.Action.Invoke ();

        Assert.True (actionInvoked);
    }

    [Fact]
    public void Subview_Visibility_Controlled_By_Removal ()
    {
        var shortcut = new Shortcut ();

        Assert.True (shortcut.CommandView.Visible);
        Assert.Contains (shortcut.CommandView, shortcut.Subviews);
        Assert.True (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.Subviews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.Subviews);

        shortcut.HelpText = "help";
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.Subviews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.Subviews);

        shortcut.Key = Key.A;
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.Subviews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.Subviews);

        shortcut.HelpView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.Subviews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.Subviews);

        shortcut.KeyView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.Subviews);
        Assert.False (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.Subviews);
    }

    [Fact]
    public void Focus_CanFocus_Default_Is_True ()
    {
        Shortcut shortcut = new ();
        shortcut.Key = Key.A;
        shortcut.Text = "Help";
        shortcut.Title = "Command";
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);
    }

    [Fact]
    public void Focus_CanFocus_CommandView_Add_Tracks ()
    {
        Shortcut shortcut = new ();
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);

        shortcut.CommandView = new () { CanFocus = true };
        Assert.False (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = true;
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = true;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView.CanFocus);
    }

    [Theory]

    //  0123456789
    // " C  0  A "
    [InlineData (-1, 0)]
    [InlineData (0, 1)]
    [InlineData (1, 1)]
    [InlineData (2, 1)]
    [InlineData (3, 1)]
    [InlineData (4, 1)]
    [InlineData (5, 1)]
    [InlineData (6, 1)]
    [InlineData (7, 1)]
    [InlineData (8, 1)]
    [InlineData (9, 0)]
    [AutoInitShutdown]
    public void MouseClick_Fires_Accept (int x, int expectedAccept)
    {
        var current = new Toplevel ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0",
            Title = "C"
        };
        current.Add (shortcut);

        Application.Begin (current);

        var accepted = 0;
        shortcut.Accept += (s, e) => accepted++;

        Application.OnMouseEvent (
                                  new ()
                                  {
                                      Position = new (x, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedAccept, accepted);

        current.Dispose ();
    }

    [Theory]

    //  0123456789
    // " C  0  A "
    [InlineData (-1, 0, 0)]
    [InlineData (0, 1, 1)]
    [InlineData (1, 1, 1, Skip = "BUGBUG: This breaks. We need to fix the logic in the Shortcut class.")]
    [InlineData (2, 1, 1)]
    [InlineData (3, 1, 1)]
    [InlineData (4, 1, 1)]
    [InlineData (5, 1, 1)]
    [InlineData (6, 1, 1)]
    [InlineData (7, 1, 1)]
    [InlineData (8, 1, 1)]
    [InlineData (9, 0, 0)]
    [AutoInitShutdown]
    public void MouseClick_Button_CommandView_Fires_Accept (int x, int expectedAccept, int expectedButtonAccept)
    {
        var current = new Toplevel ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0"
        };

        shortcut.CommandView = new Button
        {
            Title = "C",
            NoDecorations = true,
            NoPadding = true
        };
        var buttonAccepted = 0;
        shortcut.CommandView.Accept += (s, e) => { buttonAccepted++; };
        current.Add (shortcut);

        Application.Begin (current);

        var accepted = 0;
        shortcut.Accept += (s, e) => accepted++;

        //Assert.True (shortcut.HasFocus);

        Application.OnMouseEvent (
                                  new ()
                                  {
                                      Position = new (x, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedAccept, accepted);
        Assert.Equal (expectedButtonAccept, buttonAccepted);

        current.Dispose ();
    }

    [Theory]
    [InlineData (true, KeyCode.A, 1)]
    [InlineData (true, KeyCode.C, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (true, KeyCode.Enter, 1)]
    [InlineData (true, KeyCode.Space, 0)]
    [InlineData (true, KeyCode.F1, 0)]
    [InlineData (false, KeyCode.A, 1)]
    [InlineData (false, KeyCode.C, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (false, KeyCode.Enter, 0)]
    [InlineData (false, KeyCode.Space, 0)]
    [InlineData (false, KeyCode.F1, 0)]
    [AutoInitShutdown]
    public void KeyDown_Invokes_Accept (bool canFocus, KeyCode key, int expectedAccept)
    {
        var current = new Toplevel ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus
        };
        current.Add (shortcut);

        Application.Begin (current);
        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepted = 0;
        shortcut.Accept += (s, e) => accepted++;

        Application.OnKeyDown (key);

        Assert.Equal (expectedAccept, accepted);

        current.Dispose ();
    }

    [Theory]
    [InlineData (KeyCode.A, 1)]
    [InlineData (KeyCode.C, 1)]
    [InlineData (KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (KeyCode.Enter, 1)]
    [InlineData (KeyCode.Space, 0)]
    [InlineData (KeyCode.F1, 0)]
    [AutoInitShutdown]
    public void KeyDown_App_Scope_Invokes_Accept (KeyCode key, int expectedAccept)
    {
        var current = new Toplevel ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            KeyBindingScope = KeyBindingScope.Application,
            Text = "0",
            Title = "_C"
        };
        current.Add (shortcut);

        Application.Begin (current);

        var accepted = 0;
        shortcut.Accept += (s, e) => accepted++;

        Application.OnKeyDown (key);

        Assert.Equal (expectedAccept, accepted);

        current.Dispose ();
    }

    [Theory]
    [InlineData (true, KeyCode.A, 1)]
    [InlineData (true, KeyCode.C, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (true, KeyCode.Enter, 1)]
    [InlineData (true, KeyCode.Space, 0)]
    [InlineData (true, KeyCode.F1, 0)]
    [InlineData (false, KeyCode.A, 1)]
    [InlineData (false, KeyCode.C, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (false, KeyCode.Enter, 0)]
    [InlineData (false, KeyCode.Space, 0)]
    [InlineData (false, KeyCode.F1, 0)]
    [AutoInitShutdown]
    public void KeyDown_Invokes_Action (bool canFocus, KeyCode key, int expectedAction)
    {
        var current = new Toplevel ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus
        };
        current.Add (shortcut);

        Application.Begin (current);
        Assert.Equal (canFocus, shortcut.HasFocus);

        var action = 0;
        shortcut.Action += () => action++;

        Application.OnKeyDown (key);

        Assert.Equal (expectedAction, action);

        current.Dispose ();
    }

    [Theory]
    [InlineData (true, KeyCode.A, 1)]
    [InlineData (true, KeyCode.C, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (true, KeyCode.Enter, 1)]
    [InlineData (true, KeyCode.Space, 0)]
    [InlineData (true, KeyCode.F1, 0)]
    [InlineData (false, KeyCode.A, 1)]
    [InlineData (false, KeyCode.C, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (false, KeyCode.Enter, 0)]
    [InlineData (false, KeyCode.Space, 0)]
    [InlineData (false, KeyCode.F1, 0)]
    [AutoInitShutdown]
    public void KeyDown_App_Scope_Invokes_Action (bool canFocus, KeyCode key, int expectedAction)
    {
        var current = new Toplevel ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            KeyBindingScope = KeyBindingScope.Application,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus
        };
        current.Add (shortcut);

        Application.Begin (current);
        Assert.Equal (canFocus, shortcut.HasFocus);

        var action = 0;
        shortcut.Action += () => action++;

        Application.OnKeyDown (key);

        Assert.Equal (expectedAction, action);
        current.Dispose ();
    }


    [Fact]
    public void ColorScheme_SetsAndGetsCorrectly ()
    {
        var colorScheme = new ColorScheme ();

        var shortcut = new Shortcut
        {
            ColorScheme = colorScheme
        };

        Assert.Same (colorScheme, shortcut.ColorScheme);
    }

    // https://github.com/gui-cs/Terminal.Gui/issues/3664
    [Fact]
    public void ColorScheme_SetColorScheme_Does_Not_Fault_3664 ()
    {
        Application.Current = new ();
        Application.Navigation = new ();
        Shortcut shortcut = new Shortcut ();

        Application.Current.ColorScheme = null;

        Assert.Null (shortcut.ColorScheme);

        shortcut.HasFocus = true;

        Assert.Null (shortcut.ColorScheme);

        Application.Current.Dispose ();
        Application.ResetState ();
    }

}
