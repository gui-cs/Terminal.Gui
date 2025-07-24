using JetBrains.Annotations;
using UnitTests;

namespace Terminal.Gui.ViewsTests;

[TestSubject (typeof (Shortcut))]
public class ShortcutTests
{
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
    public void Button1Clicked_Raises_Activated (int x, int expectedAccepted)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Text = "0",
            Title = "C"
        };
        Application.Top.Add (shortcut);
        Application.Top.Layout ();

        var activating = 0;
        shortcut.Activating += (s, e) => activating++;

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (x, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedAccepted, activating);

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
        var commandViewAcceptingCount = 0;
        shortcut.CommandView.Activating += (s, e) => { commandViewAcceptingCount++; };

        var shortcutAcceptCount = 0;
        shortcut.Accepting += (s, e) => { shortcutAcceptCount++; };
        var shortcutActivatingCount = 0;
        shortcut.Activating += (s, e) => { shortcutActivatingCount++; };

        Application.Top.Add (shortcut);
        Application.Top.SetRelativeLayout (new (100, 100));
        Application.Top.LayoutSubViews ();

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (mouseX, 0),
                                      Flags = MouseFlags.Button1Clicked
                                  });

        Assert.Equal (expectedShortcutAccepted, shortcutAcceptCount);
        Assert.Equal (expectedShortcutSelected, shortcutActivatingCount);
        Assert.Equal (expectedCommandViewAccepted, commandViewAcceptCount);
        Assert.Equal (expectedCommandViewSelected, commandViewAcceptingCount);

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
        Application.Top.LayoutSubViews ();

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
        shortcut.CommandView.Activating += (s, e) =>
                                         {
                                             if (e.Handled)
                                             {
                                                 return;
                                             }
                                             checkboxSelected++;
                                         };

        Application.Top.Add (shortcut);
        Application.Top.SetRelativeLayout (new (100, 100));
        Application.Top.LayoutSubViews ();

        var selected = 0;
        shortcut.Activating += (s, e) =>
        {
            selected++;
        };

        var accepted = 0;
        shortcut.Accepting += (s, e) =>
                             {
                                 accepted++;
                                 e.Handled = true;
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
    [InlineData (true, KeyCode.A, 1, 0)]                    // CanFocus: Shortcut.key should activate and not Accept
    [InlineData (true, KeyCode.C, 1, 0)]                    // CanFocus: CommandView.HotKey should activate and not Accept
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1, 0)]  // CanFocus: CommandView.HotKey should activate and not Accept
    [InlineData (true, KeyCode.Enter, 1, 1)]                // CanFocus: Enter should Activate and Accept
    [InlineData (true, KeyCode.Space, 1, 0)]                // CanFocus: Space should Activate and not Accept
    [InlineData (true, KeyCode.F1, 0, 0)]                   // CanFocus: Other key should do nothing
    [InlineData (false, KeyCode.A, 1, 0)]                   // !CanFocus: Shortcut.key should Activate and not Accept
    [InlineData (false, KeyCode.C, 1, 0)]                   // !CanFocus: CommandView.HotKey should Activate and not Accept
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1, 0)] // !CanFocus: CommandView.HotKey should Activate and not Accept
    [InlineData (false, KeyCode.Enter, 0, 0)]               // !CanFocus: Enter should do nothing
    [InlineData (false, KeyCode.Space, 0, 0)]               // !CanFocus: Space should do nothing
    [InlineData (false, KeyCode.F1, 0, 0)]                  // !CanFocus: Other key should do nothing
    public void KeyDown_Raises_Accepting_And_Activating_Correctly (bool canFocus, KeyCode key, int expectedActivating, int expectedAccepting)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            Title = "_C",
            CanFocus = canFocus
        };
        Application.Top.Add (shortcut);
        shortcut.SetFocus ();

        Assert.Equal (canFocus, shortcut.HasFocus);
        // By default CommandView gets CanFocus set to false, so the CB will never have focus
        Assert.Equal (shortcut, Application.Top.MostFocused);

        var accepting = 0;
        shortcut.Accepting += (s, e) =>
                              {
                                  accepting++;
                                  e.Handled = true;
                              };

        var activating = 0;
        shortcut.Activating += (s, e) => activating++;

        Application.RaiseKeyDownEvent (key);
        Application.Top.Dispose ();
        Application.ResetState (true);

        Assert.Equal (expectedActivating, activating);
        Assert.Equal (expectedAccepting, accepting);
    }


    [Theory]
    [InlineData (true, KeyCode.A, 1, 0)]                    // CanFocus: Shortcut.key should activate and not Accept
    [InlineData (true, KeyCode.C, 1, 0)]                    // CanFocus: CommandView.HotKey should activate and not Accept
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1, 0)]  // CanFocus: CommandView.HotKey should activate and not Accept
    [InlineData (true, KeyCode.Enter, 1, 1)]                // CanFocus: Enter should Activate and Accept
    [InlineData (true, KeyCode.Space, 1, 0)]                // CanFocus: Space should Activate and not Accept
    [InlineData (true, KeyCode.F1, 0, 0)]                   // CanFocus: Other key should do nothing
    [InlineData (false, KeyCode.A, 1, 0)]                   // !CanFocus: Shortcut.key should Activate and not Accept
    [InlineData (false, KeyCode.C, 1, 0)]                   // !CanFocus: CommandView.HotKey should Activate and not Accept
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1, 0)] // !CanFocus: CommandView.HotKey should Activate and not Accept
    [InlineData (false, KeyCode.Enter, 0, 0)]               // !CanFocus: Enter should do nothing
    [InlineData (false, KeyCode.Space, 0, 0)]               // !CanFocus: Space should do nothing
    [InlineData (false, KeyCode.F1, 0, 0)]                  // !CanFocus: Other key should do nothing
    public void With_NotCanFocusCheckBox_CommandView_KeyDown_Raises_Accepting_And_Activating_Correctly (bool canFocus, KeyCode key, int expectedActivating, int expectedAccepting)
    {
        Application.Top = new ();

        // Shortcut with Key = Key.A and CommandView Hotkey = Key.C
        var shortcut = new Shortcut
        {
            Key = Key.A,
            CommandView = new CheckBox ()
            {
                Title = "_C",
            },
            CanFocus = canFocus
        };
        Application.Top.Add (shortcut);
        shortcut.SetFocus ();

        Assert.Equal (canFocus, shortcut.HasFocus);
        // By default CommandView gets CanFocus set to false, so the CB will never have focus
        Assert.Equal (shortcut, Application.Top.MostFocused);

        var accepting = 0;
        shortcut.Accepting += (s, e) =>
                             {
                                 accepting++;
                                 e.Handled = true;
                             };

        var activating = 0;
        shortcut.Activating += (s, e) => activating++;

        Application.RaiseKeyDownEvent (key);
        Application.Top.Dispose ();
        Application.ResetState (true);

        Assert.Equal (expectedActivating, activating);
        Assert.Equal (expectedAccepting, accepting);

    }


    [Theory]
    [InlineData (true, KeyCode.A, 1, 0)]                    // CanFocus: Shortcut.key should activate and not Accept
    [InlineData (true, KeyCode.C, 1, 0)]                    // CanFocus: CommandView.HotKey should activate and not Accept
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1, 0)]  // CanFocus: CommandView.HotKey should activate and not Accept
    [InlineData (true, KeyCode.Enter, 1, 1)]                // CanFocus: Enter should Activate and Accept
    [InlineData (true, KeyCode.Space, 1, 0)]                // CanFocus: Space should Activate and not Accept
    [InlineData (true, KeyCode.F1, 0, 0)]                   // CanFocus: Other key should do nothing
    [InlineData (false, KeyCode.A, 1, 0)]                   // !CanFocus: Shortcut.key should Activate and not Accept
    [InlineData (false, KeyCode.C, 1, 0)]                   // !CanFocus: CommandView.HotKey should Activate and not Accept
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1, 0)] // !CanFocus: CommandView.HotKey should Activate and not Accept
    [InlineData (false, KeyCode.Enter, 0, 0)]               // !CanFocus: Enter should do nothing
    [InlineData (false, KeyCode.Space, 0, 0)]               // !CanFocus: Space should do nothing
    [InlineData (false, KeyCode.F1, 0, 0)]                  // !CanFocus: Other key should do nothing
    public void With_CanFocusCheckBox_CommandView_KeyDown_Raises_Accepting_And_Activating_Correctly (bool canFocus, KeyCode key, int expectedActivating, int expectedAccepting)
    {
        Application.Top = new ();

        // Shortcut with Key = Key.A and CommandView Hotkey = Key.C
        var shortcut = new Shortcut
        {
            Key = Key.A,
            HelpText = "0",
            CommandView = new CheckBox ()
            {
                Title = "_C",
            },
            CanFocus = canFocus
        };
        shortcut.CommandView.CanFocus = true;

        Application.Top.Add (shortcut);
        shortcut.SetFocus ();

        Assert.Equal (canFocus, shortcut.HasFocus);
        Assert.Equal (shortcut.CommandView, Application.Top.MostFocused);

        var accepting = 0;
        shortcut.Accepting += (s, e) =>
        {
            accepting++;
            e.Handled = true;
        };

        var activating = 0;
        shortcut.Activating += (s, e) => activating++;

        Application.RaiseKeyDownEvent (key);
        Application.Top.Dispose ();
        Application.ResetState (true);

        Assert.Equal (expectedActivating, activating);
        Assert.Equal (expectedAccepting, accepting);

    }
    [Theory]
    [InlineData (KeyCode.A, 1)]
    [InlineData (KeyCode.C, 1)]
    [InlineData (KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (KeyCode.Enter, 1)]
    [InlineData (KeyCode.Space, 1)]
    [InlineData (KeyCode.F1, 0)]
    public void KeyDown_App_Scope_Invokes_Activating (KeyCode key, int expectedActivating)
    {
        Application.Top = new ();

        var shortcut = new Shortcut
        {
            Key = Key.A,
            BindKeyToApplication = true,
            Title = "_C"
        };
        Application.Top.Add (shortcut);
        Application.Top.SetFocus ();

        var activating = 0;
        shortcut.Activating += (s, e) => activating++;

        var accepting = 0;
        shortcut.Accepting += (s, e) => accepting++;

        Application.RaiseKeyDownEvent (key);
        Application.Top.Dispose ();
        Application.ResetState (true);

        Assert.Equal (expectedActivating, activating);
        Assert.Equal (0, accepting);

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
            BindKeyToApplication = true,
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

    // https://github.com/gui-cs/Terminal.Gui/issues/3664
    [Fact]
    public void Scheme_SetScheme_Does_Not_Fault_3664 ()
    {
        Application.Top = new ();
        Application.Navigation = new ();
        var shortcut = new Shortcut ();

        Application.Top.SetScheme (null);

        Assert.False (shortcut.HasScheme);
        Assert.NotNull (shortcut.GetScheme ());

        shortcut.HasFocus = true;

        Assert.False (shortcut.HasScheme);
        Assert.NotNull (shortcut.GetScheme ());

        Application.Top.Dispose ();
        Application.ResetState ();
    }
}
