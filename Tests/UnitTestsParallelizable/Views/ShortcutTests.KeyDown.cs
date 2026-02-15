using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public partial class ShortcutTests
{
    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing various keys invokes the <see cref="Shortcut.Action"/> delegate.
    ///     Action is invoked via <see cref="Shortcut.OnActivated"/> (for Activate) or
    ///     <see cref="Shortcut.OnAccepted"/> (for Accept).
    /// </summary>
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
    public void KeyDown_Invokes_Action (bool canFocus, KeyCode key, int expectedAction)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        var action = 0;

        Shortcut shortcut = new ()
        {
            Key = Key.A,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus,
            Action = () => action++
        };

        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAction, action);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing various keys invokes the <see cref="Shortcut.Action"/> delegate
    ///     when <see cref="Shortcut.BindKeyToApplication"/> is <see langword="true"/>.
    ///     The Shortcut's Key is bound at the application level so it works regardless of focus.
    /// </summary>
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
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        var action = 0;

        Shortcut shortcut = new ()
        {
            App = app,
            Key = Key.A,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus,
            BindKeyToApplication = true,
            Action = () => action++
        };

        runnable.Add (shortcut);
        runnable.SetFocus ();

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAction, action);
    }

    [Theory]
    [CombinatorialData]
    public void KeyDown_Key_Raises_HandlingHotKey_And_Accepting (bool canFocus)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.A, Text = "0", Title = "_C", CanFocus = canFocus };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, e) => { accepting++; };

        var activated = 0;

        shortcut.Activating += (_, e) => { activated++; };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, e) => { handlingHotKey++; };

        app.Keyboard.RaiseKeyDownEvent (shortcut.Key);

        Assert.Equal (0, accepting);
        Assert.Equal (1, handlingHotKey);
        Assert.Equal (1, activated);
    }

    [Theory]
    [CombinatorialData]
    public void CheckBox_KeyDown_Key_Raises_HandlingHotKey_And_Accepting (bool canFocus)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.F4, Text = "0", CanFocus = canFocus, CommandView = new CheckBox { Title = "_Test", CanFocus = canFocus } };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, e) => { accepting++; };

        var activated = 0;

        shortcut.Activating += (_, e) => { activated++; };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, e) => { handlingHotKey++; };

        app.Keyboard.RaiseKeyDownEvent (shortcut.Key);

        Assert.Equal (0, accepting);
        Assert.Equal (1, handlingHotKey);
        Assert.Equal (1, activated);
    }

    [Theory]
    [InlineData (true, KeyCode.A, 0, 1)]
    [InlineData (true, KeyCode.C, 0, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 0, 1)]
    [InlineData (true, KeyCode.Enter, 1, 0)]
    [InlineData (true, KeyCode.Space, 0, 1)]
    [InlineData (true, KeyCode.F1, 0, 0)]
    [InlineData (false, KeyCode.A, 0, 1)]
    [InlineData (false, KeyCode.C, 0, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 0, 1)]
    [InlineData (false, KeyCode.Enter, 0, 0)]
    [InlineData (false, KeyCode.Space, 0, 0)]
    [InlineData (false, KeyCode.F1, 0, 0)]
    public void KeyDown_Valid_Keys_Raises_Accepted_Activated_Correctly (bool canFocus, KeyCode key, int expectedAccept, int expectedActivate)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.A, Text = "0", Title = "_C", CanFocus = canFocus };

        // The default CommandView does not have a HotKey, so only the Shortcut's Key should trigger activation, not the CommandView's HotKey
        Assert.Equal (Key.A, shortcut.Key);
        Assert.Equal (Key.C, shortcut.HotKey);
        Assert.Equal (Key.Empty, shortcut.CommandView.HotKey);

        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, e) =>
                              {
                                  accepting++;

                                  //e.Handled = true;
                              };

        var activated = 0;

        shortcut.Activating += (_, e) =>
                               {
                                   activated++;

                                   //e.Handled = true;
                               };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, e) =>
                                   {
                                       handlingHotKey++;

                                       //e.Handled = true;
                                   };

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepting);
        Assert.Equal (expectedActivate, activated);
    }
}
