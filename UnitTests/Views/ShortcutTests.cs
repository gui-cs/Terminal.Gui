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
    }

    [Fact]
    public void Size_Defaults ()
    {
        var shortcut = new Shortcut ();

        shortcut.SetRelativeLayout (new (100, 100));
        Assert.Equal (2, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (2, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (0, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (0, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // "   0  A "
        shortcut = new ()
        {
            Key = Key.A,
            HelpText = "0"
        };
        shortcut.SetRelativeLayout (new (100, 100));
        Assert.Equal (8, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (8, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // " C  0  A "
        shortcut = new ()
        {
            Title = "C",
            Key = Key.A,
            HelpText = "0"
        };
        shortcut.SetRelativeLayout (new (100, 100));
        Assert.Equal (9, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (9, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (1, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);
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
        var shortcut = new Shortcut ();

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
        var shortcut = new Shortcut ();

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
    public void MouseClick_Raises_Accepted (int x, int expectedAccepted)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0",
            Title = "C"
        };
        Application.Top.Add (shortcut);
        Application.Top.SetRelativeLayout (new (100, 100));
        Application.Top.LayoutSubviews ();

        var accepted = 0;
        shortcut.Accepting += (s, e) => accepted++;

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (x, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedAccepted, accepted);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]

    //  0123456789
    // " C  0  A "
    [InlineData (-1, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1)] // mouseX = 0 is on the CommandView.Margin, so Shortcut will get MouseClick
    [InlineData (1, 0, 1, 1, 1)] // mouseX = 1 is on the CommandView, so CommandView will get MouseClick
    [InlineData (2, 0, 1, 1, 1)] // mouseX = 2 is on the CommandView.Margin, so Shortcut will get MouseClick
    [InlineData (3, 0, 1, 1, 1)]
    [InlineData (4, 0, 1, 1, 1)]
    [InlineData (5, 0, 1, 1, 1)]
    [InlineData (6, 0, 1, 1, 1)]
    [InlineData (7, 0, 1, 1, 1)]
    [InlineData (8, 0, 1, 1, 1)]
    [InlineData (9, 0, 0, 0, 0)]

    public void MouseClick_Default_CommandView_Raises_Accepted_Selected_Correctly (
        int mouseX,
        int expectedCommandViewAccepted,
        int expectedCommandViewSelected,
        int expectedShortcutAccepted,
        int expectedShortcutSelected
    )
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Title = "C",
            Key = Key.A,
            HelpText = "0"
        };

        var commandViewAcceptCount = 0;
        shortcut.CommandView.Accepting += (s, e) => { commandViewAcceptCount++; };
        var commandViewSelectCount = 0;
        shortcut.CommandView.Selecting += (s, e) => { commandViewSelectCount++; };

        var shortcutAcceptCount = 0;
        shortcut.Accepting += (s, e) => { shortcutAcceptCount++; };
        var shortcutSelectCount = 0;
        shortcut.Selecting += (s, e) => { shortcutSelectCount++; };

        Application.Top.Add (shortcut);
        Application.Top.SetRelativeLayout (new (100, 100));
        Application.Top.LayoutSubviews ();

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (mouseX, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedShortcutAccepted, shortcutAcceptCount);
        Assert.Equal (expectedShortcutSelected, shortcutSelectCount);
        Assert.Equal (expectedCommandViewAccepted, commandViewAcceptCount);
        Assert.Equal (expectedCommandViewSelected, commandViewSelectCount);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]

    //  0123456789
    // " C  0  A "
    [InlineData (-1, 0, 0)]
    [InlineData (0, 1, 0)]
    [InlineData (1, 1, 0)]
    [InlineData (2, 1, 0)]
    [InlineData (3, 1, 0)]
    [InlineData (4, 1, 0)]
    [InlineData (5, 1, 0)]
    [InlineData (6, 1, 0)]
    [InlineData (7, 1, 0)]
    [InlineData (8, 1, 0)]
    [InlineData (9, 0, 0)]
    public void MouseClick_Button_CommandView_Raises_Shortcut_Accepted (int mouseX, int expectedAccept, int expectedButtonAccept)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0"
        };

        shortcut.CommandView = new Button
        {
            Title = "C",
            NoDecorations = true,
            NoPadding = true,
            CanFocus = false
        };
        var buttonAccepted = 0;
        shortcut.CommandView.Accepting += (s, e) => { buttonAccepted++; };
        Application.Top.Add (shortcut);
        Application.Top.SetRelativeLayout (new (100, 100));
        Application.Top.LayoutSubviews ();

        var accepted = 0;
        shortcut.Accepting += (s, e) => { accepted++; };

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (mouseX, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedAccept, accepted);
        Assert.Equal (expectedButtonAccept, buttonAccepted);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]

    //  01234567890
    // " ☑C  0  A "
    [InlineData (-1, 0, 0)]
    [InlineData (0, 1, 0)]
    [InlineData (1, 1, 0)]
    [InlineData (2, 1, 0)]
    [InlineData (3, 1, 0)]
    [InlineData (4, 1, 0)]
    [InlineData (5, 1, 0)]
    [InlineData (6, 1, 0)]
    [InlineData (7, 1, 0)]
    [InlineData (8, 1, 0)]
    [InlineData (9, 1, 0)]
    [InlineData (10, 1, 0)]
    public void MouseClick_CheckBox_CommandView_Raises_Shortcut_Accepted_Selected_Correctly (int mouseX, int expectedAccepted, int expectedCheckboxAccepted)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0"
        };

        shortcut.CommandView = new CheckBox
        {
            Title = "C",
            CanFocus = false
        };
        var checkboxAccepted = 0;
        shortcut.CommandView.Accepting += (s, e) => { checkboxAccepted++; };

        var checkboxSelected = 0;
        shortcut.CommandView.Selecting += (s, e) =>
                                         {
                                             if (e.Cancel)
                                             {
                                                 return;
                                             }
                                             checkboxSelected++;
                                         };

        Application.Top.Add (shortcut);
        Application.Top.SetRelativeLayout (new (100, 100));
        Application.Top.LayoutSubviews ();

        var selected = 0;
        shortcut.Selecting += (s, e) =>
        {
            selected++;
        };

        var accepted = 0;
        shortcut.Accepting += (s, e) =>
                             {
                                 accepted++;
                                 e.Cancel = true;
                             };

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (mouseX, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedAccepted, accepted);
        Assert.Equal (expectedAccepted, selected);
        Assert.Equal (expectedCheckboxAccepted, checkboxAccepted);
        Assert.Equal (expectedCheckboxAccepted, checkboxSelected);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (true, KeyCode.A, 1, 1)]
    [InlineData (true, KeyCode.C, 1, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (true, KeyCode.Enter, 1, 1)]
    [InlineData (true, KeyCode.Space, 1, 1)]
    [InlineData (true, KeyCode.F1, 0, 0)]
    [InlineData (false, KeyCode.A, 1, 1)]
    [InlineData (false, KeyCode.C, 1, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (false, KeyCode.Enter, 0, 0)]
    [InlineData (false, KeyCode.Space, 0, 0)]
    [InlineData (false, KeyCode.F1, 0, 0)]
    public void KeyDown_Raises_Accepted_Selected (bool canFocus, KeyCode key, int expectedAccept, int expectedSelect)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus
        };
        Application.Top.Add (shortcut);
        shortcut.SetFocus ();

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepted = 0;
        shortcut.Accepting += (s, e) => accepted++;

        var selected = 0;
        shortcut.Selecting += (s, e) => selected++;

        Application.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepted);
        Assert.Equal (expectedSelect, selected);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }


    [Theory]
    [InlineData (true, KeyCode.A, 1, 1)]
    [InlineData (true, KeyCode.C, 1, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (true, KeyCode.Enter, 1, 1)]
    [InlineData (true, KeyCode.Space, 1, 1)]
    [InlineData (true, KeyCode.F1, 0, 0)]
    [InlineData (false, KeyCode.A, 1, 1)]
    [InlineData (false, KeyCode.C, 1, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (false, KeyCode.Enter, 0, 0)]
    [InlineData (false, KeyCode.Space, 0, 0)]
    [InlineData (false, KeyCode.F1, 0, 0)]
    public void KeyDown_CheckBox_Raises_Accepted_Selected (bool canFocus, KeyCode key, int expectedAccept, int expectedSelect)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0",
            CommandView = new CheckBox ()
            {
                Title = "_C"
            },
            CanFocus = canFocus
        };
        Application.Top.Add (shortcut);
        shortcut.SetFocus ();

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepted = 0;
        shortcut.Accepting += (s, e) =>
                             {
                                 accepted++;
                                 e.Cancel = true;
                             };

        var selected = 0;
        shortcut.Selecting += (s, e) => selected++;

        Application.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepted);
        Assert.Equal (expectedSelect, selected);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }
    [Theory]
    [InlineData (KeyCode.A, 1)]
    [InlineData (KeyCode.C, 1)]
    [InlineData (KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (KeyCode.Enter, 1)]
    [InlineData (KeyCode.Space, 1)]
    [InlineData (KeyCode.F1, 0)]
    public void KeyDown_App_Scope_Invokes_Accept (KeyCode key, int expectedAccept)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            KeyBindingScope = KeyBindingScope.Application,
            Text = "0",
            Title = "_C"
        };
        Application.Top.Add (shortcut);
        Application.Top.SetFocus ();

        var accepted = 0;
        shortcut.Accepting += (s, e) => accepted++;

        Application.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepted);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (true, KeyCode.A, 1)]
    [InlineData (true, KeyCode.C, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (true, KeyCode.Enter, 1)]
    [InlineData (true, KeyCode.Space, 1)]
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

        Application.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAction, action);

        current.Dispose ();
    }

    [Theory]
    [InlineData (true, KeyCode.A, 1)]
    [InlineData (true, KeyCode.C, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (true, KeyCode.Enter, 1)]
    [InlineData (true, KeyCode.Space, 1)]
    [InlineData (true, KeyCode.F1, 0)]
    [InlineData (false, KeyCode.A, 1)]
    [InlineData (false, KeyCode.C, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (false, KeyCode.Enter, 0)]
    [InlineData (false, KeyCode.Space, 0)]
    [InlineData (false, KeyCode.F1, 0)]
    public void KeyDown_App_Scope_Invokes_Action (bool canFocus, KeyCode key, int expectedAction)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            KeyBindingScope = KeyBindingScope.Application,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus
        };

        Application.Top.Add (shortcut);
        Application.Top.SetFocus ();

        var action = 0;
        shortcut.Action += () => action++;

        Application.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAction, action);

        Application.Top.Dispose ();
        Application.ResetState (true);
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
        Application.Top = new ();
        Application.Navigation = new ();
        var shortcut = new Shortcut ();

        Application.Top.ColorScheme = null;

        Assert.Null (shortcut.ColorScheme);

        shortcut.HasFocus = true;

        Assert.NotNull (shortcut.ColorScheme);

        Application.Top.Dispose ();
        Application.ResetState ();
    }
}
