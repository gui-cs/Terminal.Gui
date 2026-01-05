#nullable enable
namespace ViewBaseTests;

/// <summary>
///     Tests for the automatic hotkey assignment feature in <see cref="View"/>.
///     This feature was added to address Issue #4145.
/// </summary>
// Claude - Opus 4.5
public class AutoHotKeyAssignmentTests
{
    #region Default Values

    [Fact]
    public void AssignHotKeys_Defaults_To_False ()
    {
        View view = new ();
        Assert.False (view.AssignHotKeys);
    }

    [Fact]
    public void UsedHotKeys_Defaults_To_Empty ()
    {
        View view = new ();
        Assert.Empty (view.UsedHotKeys);
    }

    #endregion

    #region AssignHotKeys Property Tests

    [Fact]
    public void AssignHotKeys_True_AssignsHotKeysToSubViews ()
    {
        View container = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        container.Add (button1, button2);

        Assert.NotEqual (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeys_True_AddsHotKeySpecifierToTitles ()
    {
        View container = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        container.Add (button1, button2);

        Assert.Contains ("_", button1.Title);
        Assert.Contains ("_", button2.Title);
    }

    [Fact]
    public void AssignHotKeys_True_AssignsUniqueHotKeys ()
    {
        View container = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Send" };
        View button3 = new () { Title = "Submit" };

        container.Add (button1, button2, button3);

        // All should have unique hotkeys
        HashSet<Key> hotKeys = [button1.HotKey, button2.HotKey, button3.HotKey];
        Assert.Equal (3, hotKeys.Count);
    }

    [Fact]
    public void AssignHotKeys_False_DoesNotAssignHotKeys ()
    {
        View container = new () { AssignHotKeys = false };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        container.Add (button1, button2);

        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.Equal (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeys_PreservesExistingHotKeysInTitle ()
    {
        View container = new () { AssignHotKeys = true };

        View button = new () { Title = "_Alt Option" };

        container.Add (button);

        // Should use 'A' from "_Alt"
        Assert.Equal (Key.A, button.HotKey);
    }

    [Fact]
    public void AssignHotKeys_PreservesProgrammaticHotKey ()
    {
        View container = new () { AssignHotKeys = true };

        View button = new () { Title = "Option", HotKey = Key.X };

        container.Add (button);

        // Should preserve the programmatically set hotkey
        Assert.Equal (Key.X, button.HotKey);
        Assert.Contains (Key.X, container.UsedHotKeys);
    }

    [Fact]
    public void AssignHotKeys_SetTrue_AssignsToExistingSubViews ()
    {
        View container = new ();

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        container.Add (button1, button2);

        // Initially no hotkeys
        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.Equal (Key.Empty, button2.HotKey);

        // Enable auto-assignment
        container.AssignHotKeys = true;

        // Now hotkeys should be assigned
        Assert.NotEqual (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    #endregion

    #region UsedHotKeys Tests

    [Fact]
    public void UsedHotKeys_SkipsMarkedKeys ()
    {
        View container = new () { AssignHotKeys = true };
        container.UsedHotKeys.Add (Key.S); // Mark 'S' as used

        View button = new () { Title = "Save" };

        container.Add (button);

        // Should skip 'S' and use next available character
        Assert.NotEqual (Key.S, button.HotKey);
        Assert.Equal (Key.A, button.HotKey); // 'a' from "Save"
    }

    [Fact]
    public void UsedHotKeys_PopulatedWhenHotKeysAssigned ()
    {
        View container = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };

        container.Add (button1, button2);

        // UsedHotKeys should contain the assigned hotkeys
        Assert.NotEmpty (container.UsedHotKeys);
        Assert.Contains (button1.HotKey, container.UsedHotKeys);
        Assert.Contains (button2.HotKey, container.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_ClearedWhenSubViewRemoved ()
    {
        View container = new () { AssignHotKeys = true };

        View button = new () { Title = "Save" };
        container.Add (button);

        Key assignedKey = button.HotKey;
        Assert.Contains (assignedKey, container.UsedHotKeys);

        container.Remove (button);

        Assert.DoesNotContain (assignedKey, container.UsedHotKeys);
    }

    [Fact]
    public void UsedHotKeys_ClearedWhenRemoveAllCalled ()
    {
        View container = new () { AssignHotKeys = true };

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };
        container.Add (button1, button2);

        Assert.NotEmpty (container.UsedHotKeys);

        foreach (View removed in container.RemoveAll ())
        {
            removed.Dispose ();
        }

        Assert.Empty (container.UsedHotKeys);
    }

    #endregion

    #region AssignHotKeysToSubViews Tests

    [Fact]
    public void AssignHotKeysToSubViews_ManualCall_AssignsHotKeys ()
    {
        View container = new ();

        View button1 = new () { Title = "Save" };
        View button2 = new () { Title = "Cancel" };
        container.Add (button1, button2);

        // Initially no hotkeys
        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.Equal (Key.Empty, button2.HotKey);

        // Enable and manually call
        container.AssignHotKeys = true;
        container.AssignHotKeysToSubViews ();

        Assert.NotEqual (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeysToSubViews_SkipsViewsWithEmptyTitle ()
    {
        View container = new () { AssignHotKeys = true };

        View button1 = new () { Title = "" };
        View button2 = new () { Title = "Cancel" };

        container.Add (button1, button2);

        Assert.Equal (Key.Empty, button1.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AssignHotKeys_HandlesConflictingExistingHotKey ()
    {
        View container = new () { AssignHotKeys = true };

        View button1 = new () { Title = "_Save" }; // Uses 'S'
        container.Add (button1);

        View button2 = new () { Title = "_Save Again" }; // Also uses 'S'
        container.Add (button2);

        // First button should keep 'S', second should get reassigned
        Assert.Equal (Key.S, button1.HotKey);
        Assert.NotEqual (Key.S, button2.HotKey);
        Assert.NotEqual (Key.Empty, button2.HotKey);
    }

    [Fact]
    public void AssignHotKeys_SkipsSpaceAndControlCharacters ()
    {
        View container = new () { AssignHotKeys = true };

        View button = new () { Title = "  A B C  " }; // Spaces before valid characters

        container.Add (button);

        // Should skip spaces and use first valid character
        Assert.Equal (Key.A, button.HotKey);
    }

    [Fact]
    public void AssignHotKeys_WorksWithUnicodeCharacters ()
    {
        View container = new () { AssignHotKeys = true };

        View button = new () { Title = "Ökologie" }; // German umlaut

        container.Add (button);

        // Should assign a hotkey from the title
        Assert.NotEqual (Key.Empty, button.HotKey);
    }

    [Fact]
    public void AssignHotKeys_NoHotKeyWhenAllCharactersUsed ()
    {
        View container = new () { AssignHotKeys = true };

        // Pre-mark all letters in "AB" as used
        container.UsedHotKeys.Add (Key.A);
        container.UsedHotKeys.Add (Key.B);

        View button = new () { Title = "AB" };
        container.Add (button);

        // No hotkey can be assigned
        Assert.Equal (Key.Empty, button.HotKey);
    }

    #endregion
}
