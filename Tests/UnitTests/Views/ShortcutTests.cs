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
        Application.Top.Layout ();

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
        Application.Top.LayoutSubViews ();

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
        shortcut.CommandView.Selecting += (s, e) =>
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
        shortcut.Selecting += (s, e) =>
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
                                 e.Handled = true;
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
            BindKeyToApplication = true,
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
