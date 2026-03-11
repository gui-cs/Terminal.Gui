// Copilot

#nullable enable

using System.Collections.Frozen;
using System.Collections.ObjectModel;
using Terminal.Gui.Configuration;
using Terminal.Gui.Views;

namespace ViewTests;

/// <summary>
///     Tests for DefaultKeyBindings static properties and binding consistency on view types.
///     These tests verify that each view's configurable key binding infrastructure works correctly.
/// </summary>
public class DefaultKeyBindingsTests
{
    #region Static Property Validation

    [Fact]
    public void TextField_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (TextField.DefaultKeyBindings);
        Assert.NotEmpty (TextField.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = TextField.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Left"));
        Assert.True (bindings.ContainsKey ("Right"));
        Assert.True (bindings.ContainsKey ("DeleteCharLeft"));
        Assert.True (bindings.ContainsKey ("Undo"));
        Assert.True (bindings.ContainsKey ("Redo"));
        Assert.True (bindings.ContainsKey ("SelectAll"));
    }

    [Fact]
    public void TextView_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (TextView.DefaultKeyBindings);
        Assert.NotEmpty (TextView.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = TextView.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Left"));
        Assert.True (bindings.ContainsKey ("Right"));
        Assert.True (bindings.ContainsKey ("DeleteCharLeft"));
        Assert.True (bindings.ContainsKey ("Undo"));
        Assert.True (bindings.ContainsKey ("Redo"));
    }

    [Fact]
    public void ListView_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (ListView.DefaultKeyBindings);
        Assert.NotEmpty (ListView.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = ListView.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Up"));
        Assert.True (bindings.ContainsKey ("Down"));
        Assert.True (bindings.ContainsKey ("PageUp"));
        Assert.True (bindings.ContainsKey ("PageDown"));
        Assert.True (bindings.ContainsKey ("Start"));
        Assert.True (bindings.ContainsKey ("End"));
    }

    [Fact]
    public void TableView_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (TableView.DefaultKeyBindings);
        Assert.NotEmpty (TableView.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = TableView.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Left"));
        Assert.True (bindings.ContainsKey ("Right"));
        Assert.True (bindings.ContainsKey ("Up"));
        Assert.True (bindings.ContainsKey ("Down"));
        Assert.True (bindings.ContainsKey ("SelectAll"));
    }

    [Fact]
    public void TabView_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (TabView.DefaultKeyBindings);
        Assert.NotEmpty (TabView.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = TabView.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Left"));
        Assert.True (bindings.ContainsKey ("Right"));
        Assert.True (bindings.ContainsKey ("Up"));
        Assert.True (bindings.ContainsKey ("Down"));
    }

    [Fact]
    public void HexView_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (HexView.DefaultKeyBindings);
        Assert.NotEmpty (HexView.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = HexView.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Left"));
        Assert.True (bindings.ContainsKey ("Right"));
        Assert.True (bindings.ContainsKey ("DeleteCharLeft"));
        Assert.True (bindings.ContainsKey ("Insert"));
    }

    [Fact]
    public void DropDownList_DefaultKeyBindings_Is_Not_Null_And_Has_Toggle ()
    {
        Assert.NotNull (DropDownList.DefaultKeyBindings);
        Assert.NotEmpty (DropDownList.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = DropDownList.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Toggle"));
        Assert.Contains ("F4", bindings ["Toggle"]);
    }

    [Fact]
    public void TreeView_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (TreeView.DefaultKeyBindings);
        Assert.NotEmpty (TreeView.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = TreeView.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Up"));
        Assert.True (bindings.ContainsKey ("Down"));
        Assert.True (bindings.ContainsKey ("Expand"));
        Assert.True (bindings.ContainsKey ("Collapse"));
        Assert.True (bindings.ContainsKey ("SelectAll"));
    }

    [Fact]
    public void NumericUpDown_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (NumericUpDown.DefaultKeyBindings);
        Assert.NotEmpty (NumericUpDown.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = NumericUpDown.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("Up"));
        Assert.True (bindings.ContainsKey ("Down"));
    }

    [Fact]
    public void LinearRange_DefaultKeyBindings_Is_Not_Null_And_Has_Expected_Commands ()
    {
        Assert.NotNull (LinearRange.DefaultKeyBindings);
        Assert.NotEmpty (LinearRange.DefaultKeyBindings!);

        Dictionary<string, string []> bindings = LinearRange.DefaultKeyBindings!;
        Assert.True (bindings.ContainsKey ("LeftStart"));
        Assert.True (bindings.ContainsKey ("RightEnd"));
    }

    #endregion

    #region Key String Validation — All Key Strings Must Parse

    [Fact]
    public void All_DefaultKeyBindings_Key_Strings_Are_Parseable ()
    {
        Dictionary<string, Dictionary<string, string []>?> allBindings = new ()
        {
            { "TextField", TextField.DefaultKeyBindings },
            { "TextView", TextView.DefaultKeyBindings },
            { "ListView", ListView.DefaultKeyBindings },
            { "TableView", TableView.DefaultKeyBindings },
            { "TabView", TabView.DefaultKeyBindings },
            { "HexView", HexView.DefaultKeyBindings },
            { "DropDownList", DropDownList.DefaultKeyBindings },
            { "TreeView", TreeView.DefaultKeyBindings },
            { "NumericUpDown", NumericUpDown.DefaultKeyBindings },
            { "LinearRange", LinearRange.DefaultKeyBindings }
        };

        foreach ((string viewName, Dictionary<string, string []>? bindings) in allBindings)
        {
            Assert.NotNull (bindings);

            foreach ((string commandName, string [] keyStrings) in bindings!)
            {
                Assert.True (
                    Enum.TryParse<Command> (commandName, out _),
                    $"{viewName}: invalid command name '{commandName}'");

                foreach (string keyString in keyStrings)
                {
                    Assert.True (
                        Key.TryParse (keyString, out _),
                        $"{viewName}.{commandName}: unparseable key string '{keyString}'");
                }
            }
        }
    }

    #endregion

    #region CM Discovery

    [Fact]
    public void DefaultKeyBindings_PropertyValue_Matches_Static_Property ()
    {
        FrozenDictionary<string, ConfigProperty> props = ConfigurationManager.GetHardCodedConfigPropertyCache ();
        ConfigProperty textFieldProp = props ["TextField.DefaultKeyBindings"];

        Assert.Same (TextField.DefaultKeyBindings, textFieldProp.PropertyValue);
    }

    #endregion

    #region Binding Consistency — Verify Each View Has The Keys It Declares

    [Fact]
    public void TextField_Has_All_Declared_Bindings ()
    {
        TextField tf = new () { Width = 20, Text = "" };
        tf.BeginInit ();
        tf.EndInit ();

        foreach ((string commandName, string [] keyStrings) in TextField.DefaultKeyBindings!)
        {
            foreach (string keyString in keyStrings)
            {
                if (!Key.TryParse (keyString, out Key? key))
                {
                    continue;
                }

                Assert.True (
                    tf.KeyBindings.TryGet (key, out _),
                    $"TextField missing binding for {keyString} (Command.{commandName})");
            }
        }
    }

    [Fact]
    public void ListView_Has_All_Declared_Bindings ()
    {
        ObservableCollection<string> source = ["a", "b", "c"];
        ListView lv = new ()
        {
            Width = 10,
            Height = 5,
            Source = new ListWrapper<string> (source)
        };
        lv.BeginInit ();
        lv.EndInit ();

        foreach ((string commandName, string [] keyStrings) in ListView.DefaultKeyBindings!)
        {
            foreach (string keyString in keyStrings)
            {
                if (!Key.TryParse (keyString, out Key? key))
                {
                    continue;
                }

                Assert.True (
                    lv.KeyBindings.TryGet (key, out _),
                    $"ListView missing binding for {keyString} (Command.{commandName})");
            }
        }
    }

    [Fact]
    public void TableView_Has_All_Declared_Bindings ()
    {
        TableView tv = new () { Width = 40, Height = 10 };
        tv.BeginInit ();
        tv.EndInit ();

        foreach ((string commandName, string [] keyStrings) in TableView.DefaultKeyBindings!)
        {
            foreach (string keyString in keyStrings)
            {
                if (!Key.TryParse (keyString, out Key? key))
                {
                    continue;
                }

                Assert.True (
                    tv.KeyBindings.TryGet (key, out _),
                    $"TableView missing binding for {keyString} (Command.{commandName})");
            }
        }
    }

    [Fact]
    public void TreeView_Has_All_Declared_Bindings ()
    {
        TreeView tv = new () { Width = 40, Height = 10 };
        tv.BeginInit ();
        tv.EndInit ();

        foreach ((string commandName, string [] keyStrings) in TreeView.DefaultKeyBindings!)
        {
            foreach (string keyString in keyStrings)
            {
                if (!Key.TryParse (keyString, out Key? key))
                {
                    continue;
                }

                Assert.True (
                    tv.KeyBindings.TryGet (key, out _),
                    $"TreeView missing binding for {keyString} (Command.{commandName})");
            }
        }
    }

    [Fact]
    public void HexView_Has_All_Declared_Bindings ()
    {
        MemoryStream stream = new ([0x00]);
        HexView hv = new (stream) { Width = 80, Height = 10 };
        hv.BeginInit ();
        hv.EndInit ();

        foreach ((string commandName, string [] keyStrings) in HexView.DefaultKeyBindings!)
        {
            foreach (string keyString in keyStrings)
            {
                if (!Key.TryParse (keyString, out Key? key))
                {
                    continue;
                }

                Assert.True (
                    hv.KeyBindings.TryGet (key, out _),
                    $"HexView missing binding for {keyString} (Command.{commandName})");
            }
        }
    }

    [Fact]
    public void TabView_Has_All_Declared_Bindings ()
    {
        TabView tv = new () { Width = 40, Height = 10 };
        tv.BeginInit ();
        tv.EndInit ();

        foreach ((string commandName, string [] keyStrings) in TabView.DefaultKeyBindings!)
        {
            foreach (string keyString in keyStrings)
            {
                if (!Key.TryParse (keyString, out Key? key))
                {
                    continue;
                }

                Assert.True (
                    tv.KeyBindings.TryGet (key, out _),
                    $"TabView missing binding for {keyString} (Command.{commandName})");
            }
        }
    }

    #endregion

    #region Behavioral Tests — Verify Bindings Actually Work

    [Fact]
    public void TextField_Home_Moves_To_Start ()
    {
        TextField tf = new () { Width = 20, Text = "Hello" };
        tf.BeginInit ();
        tf.EndInit ();
        tf.InsertionPoint = 5;

        Assert.True (tf.NewKeyDownEvent (Key.Home));

        Assert.Equal (0, tf.InsertionPoint);
    }

    [Fact]
    public void TextField_Ctrl_Z_Triggers_Undo ()
    {
        TextField tf = new () { Width = 20, Text = "Hello" };
        tf.BeginInit ();
        tf.EndInit ();
        tf.InsertionPoint = 5;
        Assert.True (tf.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("Hell", tf.Text);

        Assert.True (tf.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ("Hello", tf.Text);
    }

    [Fact]
    public void ListView_CursorDown_Moves_Selection ()
    {
        ObservableCollection<string> source = ["Item 0", "Item 1", "Item 2"];
        ListView lv = new ()
        {
            Width = 20,
            Height = 5,
            Source = new ListWrapper<string> (source)
        };
        lv.BeginInit ();
        lv.EndInit ();
        lv.SelectedItem = 0;

        Assert.True (lv.NewKeyDownEvent (Key.CursorDown));

        Assert.Equal (1, lv.SelectedItem);
    }

    [Fact]
    public void ListView_CtrlP_Also_Moves_Up ()
    {
        ObservableCollection<string> source = ["Item 0", "Item 1", "Item 2"];
        ListView lv = new ()
        {
            Width = 20,
            Height = 5,
            Source = new ListWrapper<string> (source),
            SelectedItem = 1
        };
        lv.BeginInit ();
        lv.EndInit ();

        Assert.True (lv.NewKeyDownEvent (Key.P.WithCtrl));

        Assert.Equal (0, lv.SelectedItem);
    }

    [Fact]
    public void TabView_CursorRight_Switches_Tab ()
    {
        TabView tv = new () { Width = 40, Height = 10 };
        tv.AddTab (new Tab { DisplayText = "Tab1", View = new View () }, false);
        tv.AddTab (new Tab { DisplayText = "Tab2", View = new View () }, false);
        tv.SelectedTab = tv.Tabs.First ();
        tv.BeginInit ();
        tv.EndInit ();
        Assert.Equal ("Tab1", tv.SelectedTab.DisplayText);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));

        Assert.Equal ("Tab2", tv.SelectedTab!.DisplayText);
    }

    [Fact]
    public void NumericUpDown_CursorUp_Increments ()
    {
        NumericUpDown nud = new () { Width = 10, Height = 1, Value = 5 };
        nud.BeginInit ();
        nud.EndInit ();

        Assert.True (nud.NewKeyDownEvent (Key.CursorUp));

        Assert.Equal (6, nud.Value);
    }

    [Fact]
    public void NumericUpDown_CursorDown_Decrements ()
    {
        NumericUpDown nud = new () { Width = 10, Height = 1, Value = 5 };
        nud.BeginInit ();
        nud.EndInit ();

        Assert.True (nud.NewKeyDownEvent (Key.CursorDown));

        Assert.Equal (4, nud.Value);
    }

    [Fact]
    public void HexView_Has_CursorRight_Binding ()
    {
        MemoryStream stream = new ([0x01, 0x02, 0x03, 0x04]);
        HexView hv = new (stream) { Width = 80, Height = 10 };
        hv.BeginInit ();
        hv.EndInit ();

        // Verify CursorRight is bound to Right command
        Assert.True (hv.KeyBindings.TryGet (Key.CursorRight, out KeyBinding binding));
        Assert.Contains (Command.Right, binding.Commands);
    }

    #endregion
}
