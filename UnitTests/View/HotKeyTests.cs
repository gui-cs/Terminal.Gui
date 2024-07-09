﻿using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class HotKeyTests
{
    private readonly ITestOutputHelper _output;
    public HotKeyTests (ITestOutputHelper output) { _output = output; }

    [Theory]
    [InlineData (KeyCode.A)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask)]
    [InlineData (KeyCode.D1)]
    [InlineData (KeyCode.D1 | KeyCode.ShiftMask)] // '!'
    [InlineData ((KeyCode)'х')] // Cyrillic x
    [InlineData ((KeyCode)'你')] // Chinese ni
    public void AddKeyBindingsForHotKey_Sets (KeyCode key)
    {
        var view = new View ();
        view.HotKey = KeyCode.Z;
        Assert.Equal (string.Empty, view.Title);
        Assert.Equal (KeyCode.Z, view.HotKey);

        view.AddKeyBindingsForHotKey (KeyCode.Null, key);

        // Verify key bindings were set

        // As passed
        Command [] commands = view.KeyBindings.GetCommands (key);
        Assert.Contains (Command.HotKey, commands);
        commands = view.KeyBindings.GetCommands (key | KeyCode.AltMask);
        Assert.Contains (Command.HotKey, commands);

        KeyCode baseKey = key & ~KeyCode.ShiftMask;

        // If A...Z, with and without shift
        if (baseKey is >= KeyCode.A and <= KeyCode.Z)
        {
            commands = view.KeyBindings.GetCommands (key | KeyCode.ShiftMask);
            Assert.Contains (Command.HotKey, commands);
            commands = view.KeyBindings.GetCommands (key & ~KeyCode.ShiftMask);
            Assert.Contains (Command.HotKey, commands);
            commands = view.KeyBindings.GetCommands (key | KeyCode.AltMask);
            Assert.Contains (Command.HotKey, commands);
            commands = view.KeyBindings.GetCommands ((key & ~KeyCode.ShiftMask) | KeyCode.AltMask);
            Assert.Contains (Command.HotKey, commands);
        }
        else
        {
            // Non A..Z keys should not have shift bindings
            if (key.HasFlag (KeyCode.ShiftMask))
            {
                commands = view.KeyBindings.GetCommands (key & ~KeyCode.ShiftMask);
                Assert.Empty (commands);
            }
            else
            {
                commands = view.KeyBindings.GetCommands (key | KeyCode.ShiftMask);
                Assert.Empty (commands);
            }
        }
    }

    [Fact]
    public void Defaults ()
    {
        var view = new View ();
        Assert.Equal (string.Empty, view.Title);
        Assert.Equal (KeyCode.Null, view.HotKey);

        // Verify key bindings were set
        Command [] commands = view.KeyBindings.GetCommands (KeyCode.Null);
        Assert.Empty (commands);
    }

    [Theory]
    [InlineData (KeyCode.Null, true)] // non-shift
    [InlineData (KeyCode.ShiftMask, true)]
    [InlineData (KeyCode.AltMask, true)]
    [InlineData (KeyCode.ShiftMask | KeyCode.AltMask, true)]
    [InlineData (KeyCode.CtrlMask, false)]
    [InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask, false)]
    public void NewKeyDownEvent_Runs_Default_HotKey_Command (KeyCode mask, bool expected)
    {
        var view = new View { HotKeySpecifier = (Rune)'^', Title = "^Test" };
        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.NewKeyDownEvent (KeyCode.T | mask);
        Assert.Equal (expected, view.HasFocus);
    }

    [Fact]
    public void NewKeyDownEvent_Ignores_Focus_KeyBindings_SuperView ()
    {
        var view = new View ();
        view.KeyBindings.Add (Key.A, Command.HotKey); // implies KeyBindingScope.Focused - so this should not be invoked
        view.InvokingKeyBindings += (s, e) => { Assert.Fail (); };

        var superView = new View ();
        superView.Add (view);

        var ke = Key.A;
        superView.NewKeyDownEvent (ke);
    }

    [Fact]
    public void NewKeyDownEvent_Honors_HotKey_KeyBindings_SuperView ()
    {
        var view = new View ();
        view.KeyBindings.Add (Key.A, KeyBindingScope.HotKey, Command.HotKey); 
        bool invoked = false;
        view.InvokingKeyBindings += (s, e) => { invoked = true; };

        var superView = new View ();
        superView.Add (view);

        var ke = Key.A;
        superView.NewKeyDownEvent (ke);

        Assert.True (invoked);
    }


    [Fact]
    public void NewKeyDownEvent_InNewKeyDownEventvokes_HotKey_Command_With_SuperView ()
    {
        var view = new View { HotKeySpecifier = (Rune)'^', Title = "^Test" };

        var superView = new View ();
        superView.Add (view);

        view.CanFocus = true;
        Assert.False (view.HasFocus);

        var ke = Key.T;
        superView.NewKeyDownEvent (ke);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void Set_RemovesOldKeyBindings ()
    {
        var view = new View ();
        view.HotKey = KeyCode.A;
        Assert.Equal (string.Empty, view.Title);
        Assert.Equal (KeyCode.A, view.HotKey);

        // Verify key bindings were set
        Command [] commands = view.KeyBindings.GetCommands (KeyCode.A);
        Assert.Contains (Command.HotKey, commands);

        commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask);
        Assert.Contains (Command.HotKey, commands);

        commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.AltMask);
        Assert.Contains (Command.HotKey, commands);

        commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask);
        Assert.Contains (Command.HotKey, commands);

        // Now set again
        view.HotKey = KeyCode.B;
        Assert.Equal (string.Empty, view.Title);
        Assert.Equal (KeyCode.B, view.HotKey);

        commands = view.KeyBindings.GetCommands (KeyCode.A);
        Assert.DoesNotContain (Command.HotKey, commands);

        commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask);
        Assert.DoesNotContain (Command.HotKey, commands);

        commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.AltMask);
        Assert.DoesNotContain (Command.HotKey, commands);

        commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask);
        Assert.DoesNotContain (Command.HotKey, commands);
    }

    [Theory]
    [InlineData (KeyCode.A)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask)]
    [InlineData (KeyCode.D1)]
    [InlineData (KeyCode.D1 | KeyCode.ShiftMask)]
    [InlineData ((KeyCode)'!')]
    [InlineData ((KeyCode)'х')] // Cyrillic x
    [InlineData ((KeyCode)'你')] // Chinese ni
    [InlineData ((KeyCode)'ö')] // German o umlaut
    [InlineData (KeyCode.Null)]
    public void Set_Sets_WithValidKey (KeyCode key)
    {
        var view = new View ();
        view.HotKey = key;
        Assert.Equal (key, view.HotKey);
    }

    [Theory]
    [InlineData (KeyCode.A)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask)]
    [InlineData (KeyCode.D1)]
    [InlineData (KeyCode.D1 | KeyCode.ShiftMask)] // '!'
    [InlineData ((KeyCode)'х')] // Cyrillic x
    [InlineData ((KeyCode)'你')] // Chinese ni
    [InlineData ((KeyCode)'ö')] // German o umlaut
    public void Set_SetsKeyBindings (KeyCode key)
    {
        var view = new View ();
        view.HotKey = key;
        Assert.Equal (string.Empty, view.Title);
        Assert.Equal (key, view.HotKey);

        // Verify key bindings were set

        // As passed
        Command [] commands = view.KeyBindings.GetCommands (view.HotKey);
        Assert.Contains (Command.HotKey, commands);

        Key baseKey = view.HotKey.NoShift;

        // If A...Z, with and without shift
        if (baseKey.IsKeyCodeAtoZ)
        {
            commands = view.KeyBindings.GetCommands (view.HotKey.WithShift);
            Assert.Contains (Command.HotKey, commands);
            commands = view.KeyBindings.GetCommands (view.HotKey.NoShift);
            Assert.Contains (Command.HotKey, commands);
            commands = view.KeyBindings.GetCommands (view.HotKey.WithAlt);
            Assert.Contains (Command.HotKey, commands);
            commands = view.KeyBindings.GetCommands (view.HotKey.NoShift.WithAlt);
            Assert.Contains (Command.HotKey, commands);
        }
        else
        {
            // Non A..Z keys should not have shift bindings
            if (view.HotKey.IsShift)
            {
                commands = view.KeyBindings.GetCommands (view.HotKey.NoShift);
                Assert.Empty (commands);
            }
            else
            {
                commands = view.KeyBindings.GetCommands (view.HotKey.WithShift);
                Assert.Empty (commands);
            }
        }
    }

    [Fact]
    public void Set_Throws_If_Modifiers_Are_Included ()
    {
        var view = new View ();

        // A..Z must be naked (Alt is assumed)
        view.HotKey = Key.A.WithAlt;
        Assert.Throws<ArgumentException> (() => view.HotKey = Key.A.WithCtrl);

        Assert.Throws<ArgumentException> (
                                          () =>
                                              view.HotKey =
                                                  KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask
                                         );

        // All others must not have Ctrl (Alt is assumed)
        view.HotKey = Key.D1.WithAlt;
        Assert.Throws<ArgumentException> (() => view.HotKey = Key.D1.WithCtrl);

        Assert.Throws<ArgumentException> (
                                          () =>
                                              view.HotKey =
                                                  KeyCode.D1 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask
                                         );

        // Shift is ok (e.g. this is '!')
        view.HotKey = Key.D1.WithShift;
    }

    [Theory]
    [InlineData (KeyCode.Delete)]
    [InlineData (KeyCode.Backspace)]
    [InlineData (KeyCode.Tab)]
    [InlineData (KeyCode.Enter)]
    [InlineData (KeyCode.Esc)]
    [InlineData (KeyCode.Space)]
    [InlineData (KeyCode.CursorLeft)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.Null | KeyCode.ShiftMask)]
    public void Set_Throws_With_Invalid_Key (KeyCode key)
    {
        var view = new View ();
        Assert.Throws<ArgumentException> (() => view.HotKey = key);
    }

    [Theory]
    [InlineData ("Test", KeyCode.Null)]
    [InlineData ("^Test", KeyCode.T)]
    [InlineData ("T^est", KeyCode.E)]
    [InlineData ("Te^st", KeyCode.S)]
    [InlineData ("Tes^t", KeyCode.T)]
    [InlineData ("other", KeyCode.Null)]
    [InlineData ("oTher", KeyCode.Null)]
    [InlineData ("^Öther", (KeyCode)'Ö')]
    [InlineData ("^öther", (KeyCode)'ö')]

    // BUGBUG: '!' should be supported. Line 968 of TextFormatter filters on char.IsLetterOrDigit 
    //[InlineData ("Test^!", (Key)'!')]
    public void Title_Change_Sets_HotKey (string title, KeyCode expectedHotKey)
    {
        var view = new View { HotKeySpecifier = new Rune ('^'), Title = "^Hello" };
        Assert.Equal (KeyCode.H, view.HotKey);

        view.Title = title;
        Assert.Equal (expectedHotKey, view.HotKey);
    }

    [Theory]
    [InlineData ("^Test")]
    public void Title_Empty_Sets_HotKey_To_Null (string title)
    {
        var view = new View { HotKeySpecifier = (Rune)'^', Title = title };

        Assert.Equal (title, view.Title);
        Assert.Equal (KeyCode.T, view.HotKey);

        view.Title = string.Empty;
        Assert.Equal ("", view.Title);
        Assert.Equal (KeyCode.Null, view.HotKey);
    }
}
