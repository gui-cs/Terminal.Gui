// Copilot

#nullable enable

using System.Collections.Frozen;
using System.Runtime.InteropServices;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Tests for <see cref="KeyBindingConfigHelper"/> — the low-level Apply helper.
///     Uses only <see cref="View"/> (from ViewBase) and <see cref="View.CommandNotBound"/> event.
///     No dependencies on Terminal.Gui.Views.
/// </summary>
public class KeyBindingConfigHelperTests
{
    #region Apply — Core Helper Behavior

    [Fact]
    public void Apply_Null_BaseBindings_Does_Nothing ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.BeginInit ();
        view.EndInit ();

        // Act
        KeyBindingConfigHelper.Apply (view, null);

        // Assert — no new bindings for an arbitrary key
        Assert.False (view.KeyBindings.TryGet (Key.F12, out _));
    }

    [Fact]
    public void Apply_Empty_BaseBindings_Does_Nothing ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.BeginInit ();
        view.EndInit ();

        // Act
        KeyBindingConfigHelper.Apply (view, new Dictionary<string, string []> ());

        // Assert
        Assert.False (view.KeyBindings.TryGet (Key.F12, out _));
    }

    [Fact]
    public void Apply_Adds_Binding_For_Known_Command ()
    {
        // Arrange — use CommandNotBound to handle any command
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> bindings = new ()
        {
            { "Up", ["CursorUp"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert
        Assert.True (view.KeyBindings.TryGet (Key.CursorUp, out KeyBinding binding));
        Assert.Contains (Command.Up, binding.Commands);
    }

    [Fact]
    public void Apply_Adds_Multiple_Keys_To_Same_Command ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> bindings = new ()
        {
            { "Up", ["CursorUp", "Ctrl+P"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert
        Assert.True (view.KeyBindings.TryGet (Key.CursorUp, out _));
        Assert.True (view.KeyBindings.TryGet (Key.P.WithCtrl, out _));
    }

    [Fact]
    public void Apply_Adds_Multiple_Commands ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> bindings = new ()
        {
            { "Up", ["CursorUp"] },
            { "Down", ["CursorDown"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert
        Assert.True (view.KeyBindings.TryGet (Key.CursorUp, out _));
        Assert.True (view.KeyBindings.TryGet (Key.CursorDown, out _));
    }

    [Fact]
    public void Apply_Skips_Invalid_Command_Name ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> bindings = new ()
        {
            { "NotARealCommand", ["F12"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert — F12 should NOT be bound
        Assert.False (view.KeyBindings.TryGet (Key.F12, out _));
    }

    [Fact]
    public void Apply_Skips_Unparseable_Key_String ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> bindings = new ()
        {
            { "Up", ["Not+A+Valid+Key!!!"] }
        };

        // Act — should not throw
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert — invalid key not bound
        Assert.False (view.KeyBindings.TryGet (Key.CursorUp, out _));
    }

    [Fact]
    public void Apply_Does_Not_Overwrite_Existing_Binding ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        // Pre-bind CursorUp to Down
        view.KeyBindings.Add (Key.CursorUp, Command.Down);

        Dictionary<string, string []> bindings = new ()
        {
            { "Up", ["CursorUp"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert — still bound to Down, not Up
        Assert.True (view.KeyBindings.TryGet (Key.CursorUp, out KeyBinding unchanged));
        Assert.Contains (Command.Down, unchanged.Commands);
        Assert.DoesNotContain (Command.Up, unchanged.Commands);
    }

    [Fact]
    public void Apply_PlatformBindings_Null_Does_Nothing_Extra ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> baseBindings = new ()
        {
            { "Up", ["CursorUp"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, baseBindings, null);

        // Assert — base binding was applied
        Assert.True (view.KeyBindings.TryGet (Key.CursorUp, out _));
    }

    [Fact]
    public void Apply_PlatformBindings_Are_Applied_On_NonWindows ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> baseBindings = new ()
        {
            { "Up", ["CursorUp"] }
        };

        Dictionary<string, string []> platformBindings = new ()
        {
            { "Down", ["CursorDown"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, baseBindings, platformBindings);

        // Assert — base binding always applied
        Assert.True (view.KeyBindings.TryGet (Key.CursorUp, out _));

        // Platform bindings applied only on non-Windows
        if (!RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            Assert.True (view.KeyBindings.TryGet (Key.CursorDown, out _));
        }
    }

    [Fact]
    public void Apply_With_Modifier_Keys ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> bindings = new ()
        {
            { "SelectAll", ["Ctrl+A"] },
            { "Cut", ["Shift+Delete"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert
        Assert.True (view.KeyBindings.TryGet (Key.A.WithCtrl, out KeyBinding selectAll));
        Assert.Contains (Command.SelectAll, selectAll.Commands);

        Assert.True (view.KeyBindings.TryGet (Key.Delete.WithShift, out KeyBinding cut));
        Assert.Contains (Command.Cut, cut.Commands);
    }

    [Fact]
    public void Apply_Mixed_Valid_And_Invalid_Entries ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 1 };
        view.CommandNotBound += (_, args) => args.Handled = true;
        view.BeginInit ();
        view.EndInit ();

        Dictionary<string, string []> bindings = new ()
        {
            { "Up", ["CursorUp"] },
            { "BogusCommand", ["F5"] },
            { "Down", ["InvalidKey!!!", "CursorDown"] }
        };

        // Act
        KeyBindingConfigHelper.Apply (view, bindings);

        // Assert — valid entries applied, invalid skipped
        Assert.True (view.KeyBindings.TryGet (Key.CursorUp, out _));
        Assert.False (view.KeyBindings.TryGet (Key.F5, out _));
        Assert.True (view.KeyBindings.TryGet (Key.CursorDown, out _));
    }

    #endregion

    #region CM Discovery — Verify ConfigurationManager Finds DefaultKeyBindings Properties

    [Fact]
    public void All_DefaultKeyBindings_Are_Discoverable_By_CM ()
    {
        // This catches the open-generic-type bug: [ConfigurationProperty] on TreeView<T>
        // would crash. Verify CM discovers all view binding properties.
        FrozenDictionary<string, ConfigProperty> props = ConfigurationManager.GetHardCodedConfigPropertyCache ();

        string [] expectedKeys =
        [
            "TextField.DefaultKeyBindings",
            "TextView.DefaultKeyBindings",
            "ListView.DefaultKeyBindings",
            "TableView.DefaultKeyBindings",
            "TabView.DefaultKeyBindings",
            "HexView.DefaultKeyBindings",
            "DropDownList.DefaultKeyBindings",
            "TreeView.DefaultKeyBindings",
            "NumericUpDown.DefaultKeyBindings",
            "LinearRange.DefaultKeyBindings"
        ];

        foreach (string key in expectedKeys)
        {
            Assert.True (props.ContainsKey (key), $"{key} not found in CM cache");
        }
    }

    [Fact]
    public void All_DefaultKeyBindingsUnix_Are_Discoverable_By_CM ()
    {
        FrozenDictionary<string, ConfigProperty> props = ConfigurationManager.GetHardCodedConfigPropertyCache ();

        string [] expectedKeys =
        [
            "TextField.DefaultKeyBindingsUnix",
            "TextView.DefaultKeyBindingsUnix",
            "ListView.DefaultKeyBindingsUnix",
            "TableView.DefaultKeyBindingsUnix",
            "TabView.DefaultKeyBindingsUnix",
            "HexView.DefaultKeyBindingsUnix",
            "DropDownList.DefaultKeyBindingsUnix",
            "TreeView.DefaultKeyBindingsUnix",
            "NumericUpDown.DefaultKeyBindingsUnix",
            "LinearRange.DefaultKeyBindingsUnix"
        ];

        foreach (string key in expectedKeys)
        {
            Assert.True (props.ContainsKey (key), $"{key} not found in CM cache");
        }
    }

    #endregion
}

