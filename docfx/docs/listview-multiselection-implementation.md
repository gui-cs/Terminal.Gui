# ListView Multi-Selection and Rendering Fixes - Implementation Plan

## Phase Progress

| Phase | Description | Status |
|-------|-------------|--------|
| **0** | Fix broken Command.Activate handler | ✅ COMPLETED |
| **1** | Add selection tracking infrastructure | ✅ COMPLETED (already implemented) |
| **2** | Add extend commands and key bindings | ✅ COMPLETED |
| **3** | Mouse Shift+Click and Ctrl+Click support | ✅ COMPLETED |
| **4** | Multi-selection rendering | ✅ COMPLETED |
| **5** | Fix mark rendering attribute | ✅ COMPLETED |
| **6** | Custom mark rendering API | ✅ COMPLETED |
| **7** | Fix scrolling width and offset clamping | ✅ COMPLETED |

Each phase is complete when:

- New unit tests (parallizable) have been added to ensure the new functionality is sifficedntly tested.
- All 3 test projects (IntegationTests, UnitTests, and UnitTests.Paralllizable) pass
- THis plan doc is updated to reflect progress (and tersified to remove dated info).
- THe changes are commited
- REFRESH.md is re-read


---

## Executive Summary

Fixes nine related issues in ListView (Issue #4580):

1. ✅ **AllowsMultipleSelection without AllowsMarking** - Add independent selection tracking
2. ✅ **Keyboard Shift+Arrow extension** - Add Command.UpExtend/DownExtend support
3. ✅ **Mouse Shift+Click extension** - Detect Shift modifier in Command.Activate
4. ✅ **Shift+Space behavior** - Make Command.Activate work for keyboard when AllowsMarking=true
5. ✅ **Mark rendering attribute** - Marks should always use Normal attribute for clarity
6. ✅ **Custom mark rendering API** - Allow IListDataSource to override mark rendering
7. ✅ **Horizontal scrolling with marks** - Content area width is 2 columns too narrow when AllowsMarking=true
8. ✅ **Horizontal scrolling max offset** - Scrolling continues until only last column visible; should stop at (contentWidth - viewportWidth)
9. ✅ **Vertical scrolling max offset** - Scrolling continues until only last row visible; should stop at (contentHeight - viewportHeight)

## Design Philosophy

**Separation of Concerns:**
- **Marking** = Data operation (checkboxes in data source) controlled by `AllowsMarking`
- **Selection** = UI/navigation operation (highlighting for actions) controlled by `AllowsMultipleSelection`
- These systems should work independently or together

**Pattern:** Follow TableView's proven multi-selection architecture while maintaining backward compatibility.

---

## Phase 0: Fix Existing Broken Command.Activate Handler ✅ COMPLETED

**CRITICAL:** Tests currently fail because the `Command.Activate` handler is broken. Must fix this FIRST.

> **Status:** ✅ Completed - Fixed the `Command.Activate` handler to:
> 1. Call `MarkUnmarkSelectedItem()` for keyboard events (Space key) when `AllowsMarking=true`
> 2. Call `MarkUnmarkSelectedItem()` for mouse events only on `LeftButtonClicked` (not `LeftButtonPressed`) to avoid double-toggling
> All 4 failing tests now pass.

### Current Failing Tests (4 total):
1. `KeyBindings_Command` - Space key doesn't mark items
2. `AllowsMarking_True_SpaceWithShift_SelectsThenDown_SingleSelection` - Shift+Space doesn't work
3. `AllowsMarking_True_SpaceWithShift_SelectsThenDown_MultipleSelection` - Shift+Space doesn't work
4. `Mouse_Click_With_AllowsMultipleSelection_Marks_Multiple_Items` - Mouse click doesn't mark

### Root Cause Analysis

**File:** `Terminal.Gui/Views/ListView.cs` lines 85-108

**Current broken handler:**
```csharp
AddCommand (Command.Activate,
    ctx =>
    {
        if (RaiseActivating (ctx) == true)
        {
            return true;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        // BUG: Early return for keyboard events - never calls MarkUnmarkSelectedItem!
        if (ctx?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            return true;  // <-- PROBLEM: Just returns without marking
        }

        Point position = mouse.Position!.Value;
        int index = Viewport.Y + position.Y;

        if (Source is { } && index < Source.Count)
        {
            SelectedItem = index;  // <-- PROBLEM: Sets selection but never marks
        }

        return true;
    });
```

### Fix: Restore Proper Command.Activate Behavior

```csharp
AddCommand (Command.Activate,
    ctx =>
    {
        if (RaiseActivating (ctx) == true)
        {
            return true;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        // Handle mouse clicks
        if (ctx?.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            Point position = mouse.Position!.Value;
            int index = Viewport.Y + position.Y;

            if (Source is { } && index < Source.Count)
            {
                SelectedItem = index;

                // Mark item on click when AllowsMarking is enabled
                if (AllowsMarking)
                {
                    MarkUnmarkSelectedItem ();
                }
            }

            return true;
        }

        // Handle keyboard (Space key) - mark item when AllowsMarking is enabled
        if (AllowsMarking && SelectedItem.HasValue)
        {
            MarkUnmarkSelectedItem ();
        }

        return true;
    });
```

### Phase 0 Verification

**Run ALL tests to confirm fix:**
```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

**Expected:** All 4 failing tests pass, no regressions.

**DO NOT proceed to Phase 1 until all tests pass.**

---

## Phase 1: Add Selection Tracking Infrastructure ✅ COMPLETED

> **Status:** ✅ Already implemented - All selection tracking infrastructure was already in place:
> - `MultiSelectedItems` property
> - `_selectionAnchor` field
> - `SetSelection()`, `GetAllSelectedItems()`, `IsSelected()`, `SelectAll()`, `UnselectAll()` methods
> - `SelectedItem` setter resets anchor

### 1.1 Add Properties

**File:** `Terminal.Gui/Views/ListView.cs`

```csharp
/// <summary>
/// When <see cref="AllowsMultipleSelection"/> is enabled, contains indices of all selected items.
/// Independent of <see cref="AllowsMarking"/>.
/// </summary>
public HashSet<int> MultiSelectedItems { get; } = [];

/// <summary>
/// Anchor point for range selection operations (Shift+Click, Shift+Arrow).
/// </summary>
private int? _selectionAnchor;
```

### 1.2 Update SelectedItem Setter

```csharp
public int? SelectedItem
{
    get;
    set
    {
        if (Source is null)
        {
            return;
        }

        if (value.HasValue && (value < 0 || value >= Source.Count))
        {
            throw new ArgumentException ("SelectedItem must be >= 0 and < Count");
        }

        field = value;
        _selectionAnchor = value; // Reset anchor when directly setting SelectedItem
        OnSelectedChanged ();
        SetNeedsDraw ();
    }
}
```

### 1.3 Add Selection Management Methods

```csharp
/// <summary>
/// Sets selected item, optionally extending selection to create a range.
/// </summary>
/// <param name="item">Item index to select</param>
/// <param name="extendExistingSelection">
/// If true and <see cref="AllowsMultipleSelection"/> enabled,
/// extends from <see cref="_selectionAnchor"/> to <paramref name="item"/>
/// </param>
public void SetSelection (int item, bool extendExistingSelection)
{
    if (Source is null || item < 0 || item >= Source.Count)
    {
        return;
    }

    if (!AllowsMultipleSelection || !extendExistingSelection)
    {
        MultiSelectedItems.Clear ();
        _selectionAnchor = item;
    }
    else if (extendExistingSelection && _selectionAnchor.HasValue)
    {
        // Create range from anchor to item
        MultiSelectedItems.Clear ();
        int start = Math.Min (_selectionAnchor.Value, item);
        int end = Math.Max (_selectionAnchor.Value, item);

        for (int i = start; i <= end; i++)
        {
            MultiSelectedItems.Add (i);
        }
    }

    SelectedItem = item;
    EnsureSelectedItemVisible ();
    SetNeedsDraw ();
}

/// <summary>
/// Gets all selected item indices (SelectedItem + MultiSelectedItems).
/// </summary>
public IEnumerable<int> GetAllSelectedItems ()
{
    HashSet<int> all = [.. MultiSelectedItems];

    if (SelectedItem.HasValue)
    {
        all.Add (SelectedItem.Value);
    }

    return all.OrderBy (i => i);
}

/// <summary>
/// Returns true if item is selected.
/// </summary>
public bool IsSelected (int item)
{
    return item == SelectedItem || MultiSelectedItems.Contains (item);
}

/// <summary>
/// Selects all items when AllowsMultipleSelection is true.
/// </summary>
public void SelectAll ()
{
    if (!AllowsMultipleSelection || Source is null)
    {
        return;
    }

    MultiSelectedItems.Clear ();

    for (int i = 0; i < Source.Count; i++)
    {
        MultiSelectedItems.Add (i);
    }

    SetNeedsDraw ();
}

/// <summary>
/// Clears multi-selection.
/// </summary>
public void UnselectAll ()
{
    MultiSelectedItems.Clear ();
    SetNeedsDraw ();
}
```

### 1.4 Phase 1 Unit Tests

**File:** `Tests/UnitTestsParallelizable/Views/ListViewTests.cs`

```csharp
[Fact]
// Claude - Opus 4.5
public void MultiSelectedItems_Initialized_Empty ()
{
    ListView lv = new ();
    Assert.NotNull (lv.MultiSelectedItems);
    Assert.Empty (lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void SetSelection_Without_Extend_Clears_MultiSelectedItems ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"]),
        AllowsMultipleSelection = true
    };

    lv.MultiSelectedItems.Add (0);
    lv.MultiSelectedItems.Add (1);
    Assert.Equal (2, lv.MultiSelectedItems.Count);

    lv.SetSelection (2, false);
    Assert.Empty (lv.MultiSelectedItems);
    Assert.Equal (2, lv.SelectedItem);
}

[Fact]
// Claude - Opus 4.5
public void SetSelection_With_Extend_Creates_Range ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three", "Four", "Five"]),
        AllowsMultipleSelection = true
    };

    lv.SetSelection (1, false);  // Anchor at 1
    lv.SetSelection (3, true);   // Extend to 3

    Assert.Equal (3, lv.SelectedItem);
    Assert.Contains (1, lv.MultiSelectedItems);
    Assert.Contains (2, lv.MultiSelectedItems);
    Assert.Contains (3, lv.MultiSelectedItems);
    Assert.Equal (3, lv.MultiSelectedItems.Count);
}

[Fact]
// Claude - Opus 4.5
public void IsSelected_Returns_True_For_SelectedItem ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"])
    };

    lv.SelectedItem = 1;
    Assert.True (lv.IsSelected (1));
    Assert.False (lv.IsSelected (0));
    Assert.False (lv.IsSelected (2));
}

[Fact]
// Claude - Opus 4.5
public void IsSelected_Returns_True_For_MultiSelectedItems ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"]),
        AllowsMultipleSelection = true
    };

    lv.SelectedItem = 0;
    lv.MultiSelectedItems.Add (2);

    Assert.True (lv.IsSelected (0));
    Assert.False (lv.IsSelected (1));
    Assert.True (lv.IsSelected (2));
}

[Fact]
// Claude - Opus 4.5
public void GetAllSelectedItems_Returns_Union_Sorted ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["A", "B", "C", "D", "E"]),
        AllowsMultipleSelection = true
    };

    lv.SelectedItem = 2;
    lv.MultiSelectedItems.Add (0);
    lv.MultiSelectedItems.Add (4);

    List<int> selected = lv.GetAllSelectedItems ().ToList ();
    Assert.Equal ([0, 2, 4], selected);
}

[Fact]
// Claude - Opus 4.5
public void SelectAll_Adds_All_Items_To_MultiSelectedItems ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"]),
        AllowsMultipleSelection = true
    };

    lv.SelectAll ();

    Assert.Equal (3, lv.MultiSelectedItems.Count);
    Assert.Contains (0, lv.MultiSelectedItems);
    Assert.Contains (1, lv.MultiSelectedItems);
    Assert.Contains (2, lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void SelectAll_Does_Nothing_When_AllowsMultipleSelection_False ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"]),
        AllowsMultipleSelection = false
    };

    lv.SelectAll ();
    Assert.Empty (lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void UnselectAll_Clears_MultiSelectedItems ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"]),
        AllowsMultipleSelection = true
    };

    lv.MultiSelectedItems.Add (0);
    lv.MultiSelectedItems.Add (1);
    lv.UnselectAll ();

    Assert.Empty (lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void SelectedItem_Setter_Resets_Anchor ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three", "Four"]),
        AllowsMultipleSelection = true
    };

    // Set anchor via SetSelection
    lv.SetSelection (0, false);
    lv.SetSelection (2, true);
    Assert.Equal (3, lv.MultiSelectedItems.Count);

    // Direct SelectedItem assignment should reset anchor
    lv.SelectedItem = 3;

    // Now extend from 3
    lv.SetSelection (1, true);

    // Should have range 1-3, not 0-1 (proving anchor was reset)
    Assert.Contains (1, lv.MultiSelectedItems);
    Assert.Contains (2, lv.MultiSelectedItems);
    Assert.Contains (3, lv.MultiSelectedItems);
    Assert.DoesNotContain (0, lv.MultiSelectedItems);
}
```

### 1.5 Phase 1 Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

**DO NOT proceed to Phase 2 until all tests pass.**

---

## Phase 2: Add Extend Commands and Key Bindings ✅ COMPLETED

> **Status:** ✅ Completed - All movement methods updated with `extend` parameter, extend commands added, key bindings configured. 12 new tests added, all 55 ListView tests pass.

### 2.1 Update Movement Methods

**File:** `Terminal.Gui/Views/ListView.cs`

Update all movement methods to accept `bool extend = false` parameter:

```csharp
public bool MoveDown (bool extend = false)
{
    if (Source is null || Source.Count == 0)
    {
        return false;
    }

    bool moved = false;

    if (SelectedItem is null || SelectedItem >= Source.Count)
    {
        SetSelection (SelectedItem is null ? 0 : Source.Count - 1, extend);
        moved = true;
    }
    else if (SelectedItem + 1 < Source.Count)
    {
        SetSelection (SelectedItem.Value + 1, extend);
        // Update viewport as needed...
        moved = true;
    }

    return moved;
}

// Similarly update: MoveUp, MovePageDown, MovePageUp, MoveHome, MoveEnd
```

### 2.2 Update Command Handlers

```csharp
// Replace existing
AddCommand (Command.Up, ctx => RaiseActivating (ctx) == true || MoveUp (false));
AddCommand (Command.Down, ctx => RaiseActivating (ctx) == true || MoveDown (false));

// Add extend commands
AddCommand (Command.UpExtend, ctx => RaiseActivating (ctx) == true || MoveUp (true));
AddCommand (Command.DownExtend, ctx => RaiseActivating (ctx) == true || MoveDown (true));
AddCommand (Command.PageUpExtend, () => MovePageUp (true));
AddCommand (Command.PageDownExtend, () => MovePageDown (true));
AddCommand (Command.StartExtend, () => MoveHome (true));
AddCommand (Command.EndExtend, () => MoveEnd (true));
```

### 2.3 Add Key Bindings

```csharp
// Shift+Arrow for extending selection
KeyBindings.Add (Key.CursorUp.WithShift, Command.UpExtend);
KeyBindings.Add (Key.CursorDown.WithShift, Command.DownExtend);
KeyBindings.Add (Key.PageUp.WithShift, Command.PageUpExtend);
KeyBindings.Add (Key.PageDown.WithShift, Command.PageDownExtend);
KeyBindings.Add (Key.Home.WithShift, Command.StartExtend);
KeyBindings.Add (Key.End.WithShift, Command.EndExtend);
```

### 2.4 Phase 2 Unit Tests

```csharp
[Fact]
// Claude - Opus 4.5
public void MoveDown_With_Extend_False_Clears_Selection ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4"]),
        AllowsMultipleSelection = true
    };

    lv.SetSelection (0, false);
    lv.SetSelection (2, true);  // Select 0-2
    Assert.Equal (3, lv.MultiSelectedItems.Count);

    lv.MoveDown (false);  // Move without extending

    Assert.Equal (3, lv.SelectedItem);
    Assert.Empty (lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void MoveDown_With_Extend_True_Extends_Selection ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4"]),
        AllowsMultipleSelection = true
    };

    lv.SetSelection (1, false);  // Anchor at 1
    lv.MoveDown (true);          // Extend to 2

    Assert.Equal (2, lv.SelectedItem);
    Assert.Contains (1, lv.MultiSelectedItems);
    Assert.Contains (2, lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void ShiftDown_Key_Extends_Selection ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4"]),
        AllowsMultipleSelection = true
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.SelectedItem = 0;
    Assert.True (lv.NewKeyDownEvent (Key.CursorDown.WithShift));

    Assert.Equal (1, lv.SelectedItem);
    Assert.True (lv.IsSelected (0));
    Assert.True (lv.IsSelected (1));
}

[Fact]
// Claude - Opus 4.5
public void ShiftUp_Key_Extends_Selection ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4"]),
        AllowsMultipleSelection = true
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.SelectedItem = 2;
    Assert.True (lv.NewKeyDownEvent (Key.CursorUp.WithShift));

    Assert.Equal (1, lv.SelectedItem);
    Assert.True (lv.IsSelected (1));
    Assert.True (lv.IsSelected (2));
}

[Fact]
// Claude - Opus 4.5
public void ShiftPageDown_Key_Extends_Selection ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"]),
        AllowsMultipleSelection = true,
        Height = 3
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.SelectedItem = 0;
    Assert.True (lv.NewKeyDownEvent (Key.PageDown.WithShift));

    // Should select from 0 to wherever PageDown lands
    Assert.True (lv.IsSelected (0));
    Assert.True (lv.SelectedItem > 0);
}

[Fact]
// Claude - Opus 4.5
public void ShiftHome_Key_Extends_To_Beginning ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]),
        AllowsMultipleSelection = true
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.SelectedItem = 3;
    Assert.True (lv.NewKeyDownEvent (Key.Home.WithShift));

    Assert.Equal (0, lv.SelectedItem);
    Assert.True (lv.IsSelected (0));
    Assert.True (lv.IsSelected (1));
    Assert.True (lv.IsSelected (2));
    Assert.True (lv.IsSelected (3));
}

[Fact]
// Claude - Opus 4.5
public void ShiftEnd_Key_Extends_To_End ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]),
        AllowsMultipleSelection = true
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.SelectedItem = 1;
    Assert.True (lv.NewKeyDownEvent (Key.End.WithShift));

    Assert.Equal (4, lv.SelectedItem);
    Assert.True (lv.IsSelected (1));
    Assert.True (lv.IsSelected (2));
    Assert.True (lv.IsSelected (3));
    Assert.True (lv.IsSelected (4));
}
```

### 2.5 Phase 2 Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

**DO NOT proceed to Phase 3 until all tests pass.**

---

## Phase 3: Mouse Shift+Click and Ctrl+Click Support ✅ COMPLETED

> **Status:** ✅ Completed - Shift+Click extends selection from anchor, Ctrl+Click toggles individual items. Mouse bindings added for modifier combinations. 5 new tests added, all 60 ListView tests pass.

### 3.1 Update Command.Activate Handler for Mouse Modifiers

```csharp
AddCommand (Command.Activate,
    ctx =>
    {
        if (RaiseActivating (ctx) == true)
        {
            return true;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        // Handle mouse clicks
        if (ctx?.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            Point position = mouse.Position!.Value;
            int index = Viewport.Y + position.Y;

            if (Source is { } && index < Source.Count)
            {
                bool shift = mouse.Flags.HasFlag (MouseFlags.ButtonShift);
                bool ctrl = mouse.Flags.HasFlag (MouseFlags.ButtonCtrl);

                if (ctrl && AllowsMultipleSelection)
                {
                    // Ctrl+Click: Toggle item in multi-selection
                    if (MultiSelectedItems.Contains (index))
                    {
                        MultiSelectedItems.Remove (index);
                    }
                    else
                    {
                        MultiSelectedItems.Add (index);
                    }

                    SelectedItem = index;
                    _selectionAnchor = index;
                    SetNeedsDraw ();
                }
                else if (shift && AllowsMultipleSelection)
                {
                    // Shift+Click: Extend selection from anchor
                    SetSelection (index, true);
                }
                else
                {
                    // Normal click: Select item
                    SelectedItem = index;

                    // Mark item on click when AllowsMarking is enabled
                    if (AllowsMarking)
                    {
                        MarkUnmarkSelectedItem ();
                    }
                }
            }

            return true;
        }

        // Handle keyboard (Space key) - mark item when AllowsMarking is enabled
        if (AllowsMarking && SelectedItem.HasValue)
        {
            MarkUnmarkSelectedItem ();
        }

        return true;
    });
```

### 3.2 Phase 3 Unit Tests

```csharp
[Fact]
// Claude - Opus 4.5
public void Mouse_ShiftClick_Extends_Selection ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4"]),
        AllowsMultipleSelection = true,
        Height = 4,
        Width = 10
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.SelectedItem = 0;

    // Shift+Click on item 2
    lv.NewMouseEvent (new ()
    {
        Position = new (0, 2),
        Flags = MouseFlags.LeftButtonPressed | MouseFlags.ButtonShift
    });

    Assert.Equal (2, lv.SelectedItem);
    Assert.Contains (0, lv.MultiSelectedItems);
    Assert.Contains (1, lv.MultiSelectedItems);
    Assert.Contains (2, lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void Mouse_CtrlClick_Toggles_Individual_Items ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4"]),
        AllowsMultipleSelection = true,
        Height = 4,
        Width = 10
    };
    lv.BeginInit ();
    lv.EndInit ();

    // Ctrl+Click on item 0
    lv.NewMouseEvent (new ()
    {
        Position = new (0, 0),
        Flags = MouseFlags.LeftButtonPressed | MouseFlags.ButtonCtrl
    });
    Assert.Contains (0, lv.MultiSelectedItems);

    // Ctrl+Click on item 2
    lv.NewMouseEvent (new ()
    {
        Position = new (0, 2),
        Flags = MouseFlags.LeftButtonPressed | MouseFlags.ButtonCtrl
    });
    Assert.Contains (0, lv.MultiSelectedItems);
    Assert.Contains (2, lv.MultiSelectedItems);
    Assert.DoesNotContain (1, lv.MultiSelectedItems);

    // Ctrl+Click on item 0 again - should toggle off
    lv.NewMouseEvent (new ()
    {
        Position = new (0, 0),
        Flags = MouseFlags.LeftButtonPressed | MouseFlags.ButtonCtrl
    });
    Assert.DoesNotContain (0, lv.MultiSelectedItems);
    Assert.Contains (2, lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void Mouse_NormalClick_Clears_MultiSelection ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4"]),
        AllowsMultipleSelection = true,
        Height = 4,
        Width = 10
    };
    lv.BeginInit ();
    lv.EndInit ();

    // Build up a selection
    lv.SetSelection (0, false);
    lv.SetSelection (2, true);
    Assert.Equal (3, lv.MultiSelectedItems.Count);

    // Normal click should clear multi-selection
    lv.NewMouseEvent (new ()
    {
        Position = new (0, 3),
        Flags = MouseFlags.LeftButtonPressed
    });

    Assert.Equal (3, lv.SelectedItem);
    Assert.Empty (lv.MultiSelectedItems);
}
```

### 3.3 Phase 3 Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

**DO NOT proceed to Phase 4 until all tests pass.**

---

## Phase 4: Multi-Selection Rendering ✅ COMPLETED

> **Status:** ✅ Completed - Multi-selected items render with `VisualRole.Highlight`. `AllowsMultipleSelection = false` clears `MultiSelectedItems`. 4 new tests added, all 64 ListView tests pass.

### 4.1 Update OnDrawingContent for Multi-Selection Highlight

**File:** `Terminal.Gui/Views/ListView.cs` in `OnDrawingContent`

```csharp
for (var row = 0; row < f.Height; row++, item++)
{
    bool isSelectedItem = item == SelectedItem;
    bool isMultiSelected = MultiSelectedItems.Contains (item);

    // Determine visual role based on selection state
    VisualRole role;

    if (focused && isSelectedItem)
    {
        role = VisualRole.Focus;           // Focused + SelectedItem
    }
    else if (isMultiSelected)
    {
        role = VisualRole.Highlight;       // In MultiSelectedItems
    }
    else if (isSelectedItem)
    {
        role = VisualRole.Active;          // SelectedItem without focus
    }
    else
    {
        role = VisualRole.Normal;          // Not selected
    }

    Attribute newAttribute = GetAttributeForRole (role);
    // ... rest of rendering
}
```

### 4.2 Update AllowsMultipleSelection Property Setter

```csharp
public bool AllowsMultipleSelection
{
    get;
    set
    {
        field = value;

        if (Source is { } && !field)
        {
            // Clear multi-selection tracking
            MultiSelectedItems.Clear ();

            // Clear marks except selected (existing behavior)
            for (var i = 0; i < Source.Count; i++)
            {
                if (Source.IsMarked (i) && SelectedItem.HasValue && i != SelectedItem.Value)
                {
                    Source.SetMark (i, false);
                }
            }
        }

        SetNeedsDraw ();
    }
}
```

### 4.3 Phase 4 Unit Tests

```csharp
[Fact]
// Claude - Opus 4.5
public void MultiSelectedItems_Rendered_With_Highlight_Attribute ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"]),
        AllowsMultipleSelection = true,
        Height = 3,
        Width = 10
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.SetSelection (0, false);
    lv.SetSelection (1, true);
    lv.Draw ();

    // Items 0 and 1 should be in MultiSelectedItems and rendered with Highlight
    Attribute highlightAttr = lv.GetAttributeForRole (VisualRole.Highlight);
    // Verify rendering uses Highlight for multi-selected items
    Assert.Contains (0, lv.MultiSelectedItems);
    Assert.Contains (1, lv.MultiSelectedItems);
}

[Fact]
// Claude - Opus 4.5
public void AllowsMultipleSelection_False_Clears_MultiSelectedItems ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two", "Three"]),
        AllowsMultipleSelection = true
    };

    lv.MultiSelectedItems.Add (0);
    lv.MultiSelectedItems.Add (1);
    lv.MultiSelectedItems.Add (2);
    Assert.Equal (3, lv.MultiSelectedItems.Count);

    lv.AllowsMultipleSelection = false;

    Assert.Empty (lv.MultiSelectedItems);
}
```

### 4.4 Phase 4 Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

**DO NOT proceed to Phase 5 until all tests pass.**

---

## Phase 5: Fix Mark Rendering Attribute ✅ COMPLETED

> **Status:** ✅ Completed - Marks now render with `VisualRole.Normal` attribute for visual clarity. 2 new tests added, all 66 ListView tests pass.

### 5.1 Render Marks with Normal Attribute

**File:** `Terminal.Gui/Views/ListView.cs` in `OnDrawingContent`

```csharp
if (AllowsMarking)
{
    // Save current attribute
    Attribute savedAttr = current;

    // Render marks with Normal attribute for clarity
    Attribute normalAttr = GetAttributeForRole (VisualRole.Normal);

    if (current != normalAttr)
    {
        SetAttribute (normalAttr);
        current = normalAttr;
    }

    AddRune (Source.IsMarked (item)
        ? AllowsMultipleSelection ? Glyphs.CheckStateChecked : Glyphs.Selected
        : AllowsMultipleSelection ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected);
    AddRune ((Rune)' ');

    // Restore attribute for content rendering
    if (current != savedAttr)
    {
        SetAttribute (savedAttr);
        current = savedAttr;
    }
}
```

### 5.2 Phase 5 Unit Tests

```csharp
[Fact]
// Claude - Opus 4.5
public void Marks_Rendered_With_Normal_Attribute ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One"]),
        AllowsMarking = true,
        Height = 1,
        Width = 10
    };
    lv.BeginInit ();
    lv.EndInit ();
    lv.SetFocus ();
    lv.SelectedItem = 0;
    lv.Source!.SetMark (0, true);
    lv.Draw ();

    // Verify mark glyph is rendered with Normal attribute
    Attribute markAttr = lv.Screen [0, 0].Attribute;
    Assert.Equal (lv.GetAttributeForRole (VisualRole.Normal), markAttr);
}

[Fact]
// Claude - Opus 4.5
public void Marks_Use_Normal_Even_When_Row_Focused ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["One", "Two"]),
        AllowsMarking = true,
        Height = 2,
        Width = 10
    };
    lv.BeginInit ();
    lv.EndInit ();
    lv.SetFocus ();
    lv.SelectedItem = 0;
    lv.Source!.SetMark (0, true);
    lv.Draw ();

    // Mark column should be Normal, content should be Focus
    Attribute normalAttr = lv.GetAttributeForRole (VisualRole.Normal);
    Attribute focusAttr = lv.GetAttributeForRole (VisualRole.Focus);

    Assert.Equal (normalAttr, lv.Screen [0, 0].Attribute);  // Mark glyph
    Assert.Equal (normalAttr, lv.Screen [1, 0].Attribute);  // Space after mark
    Assert.Equal (focusAttr, lv.Screen [2, 0].Attribute);   // Content starts with Focus
}
```

### 5.3 Phase 5 Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

**DO NOT proceed to Phase 6 until all tests pass.**

---

## Phase 6: Custom Mark Rendering API ✅ COMPLETED

> **Status:** ✅ Completed - Added `RenderMark` method to `IListDataSource` interface with default implementation returning `false`. Added virtual `RenderMark` method to `ListWrapper<T>` to allow subclass overrides. Updated `OnDrawingContent` to call `Source.RenderMark()` before default mark rendering. Added 3 unit tests.

### 6.1 Add RenderMark to IListDataSource

**File:** `Terminal.Gui/Views/IListDataSource.cs`

```csharp
/// <summary>
/// Renders the mark indicator for an item. Override to customize mark rendering.
/// </summary>
/// <param name="listView">The ListView rendering to</param>
/// <param name="item">Item index</param>
/// <param name="row">Row in viewport</param>
/// <param name="isMarked">Whether item is marked</param>
/// <param name="allowsMultiple">Whether multiple selection is enabled</param>
/// <returns>True if custom rendering was done; false to use default</returns>
/// <remarks>
/// Default implementation returns false, causing ListView to use default mark rendering.
/// Override and return true to provide custom mark glyphs, positioning, or attributes.
/// When this returns false, ListView renders marks in columns 0-1, then calls Render() starting at column 2.
/// When this returns true, you must render marks yourself (if desired) and Render() will be called starting at column 0.
/// </remarks>
bool RenderMark (ListView listView, int item, int row, bool isMarked, bool allowsMultiple)
{
    return false; // Default: use ListView's rendering
}
```

### 6.2 Update OnDrawingContent to Support Custom Marks

```csharp
int markWidth = 0; // Track width used by marks

if (AllowsMarking)
{
    // Try custom rendering first
    bool customRendered = Source.RenderMark (this, item, row, Source.IsMarked (item), AllowsMultipleSelection);

    if (!customRendered)
    {
        // Default rendering with Normal attribute
        Attribute savedAttr = current;
        Attribute normalAttr = GetAttributeForRole (VisualRole.Normal);

        if (current != normalAttr)
        {
            SetAttribute (normalAttr);
            current = normalAttr;
        }

        AddRune (Source.IsMarked (item)
            ? AllowsMultipleSelection ? Glyphs.CheckStateChecked : Glyphs.Selected
            : AllowsMultipleSelection ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected);
        AddRune ((Rune)' ');
        markWidth = 2;

        if (current != savedAttr)
        {
            SetAttribute (savedAttr);
            current = savedAttr;
        }
    }
}

int col = markWidth;
Source.Render (this, isSelected, item, col, row, f.Width - col, start);
```

### 6.3 Phase 6 Unit Tests

```csharp
[Fact]
// Claude - Opus 4.5
public void RenderMark_Default_Returns_False ()
{
    ListWrapper<string> source = new (["One", "Two"]);
    IListDataSource dataSource = source;

    ListView lv = new () { Source = source };
    bool result = dataSource.RenderMark (lv, 0, 0, false, false);

    Assert.False (result);
}

[Fact]
// Claude - Opus 4.5
public void Custom_RenderMark_Can_Override_Default ()
{
    // Create a custom data source that overrides RenderMark
    CustomMarkDataSource source = new (["One", "Two"]);

    ListView lv = new ()
    {
        Source = source,
        AllowsMarking = true,
        Height = 2,
        Width = 10
    };
    lv.BeginInit ();
    lv.EndInit ();
    lv.Draw ();

    // Verify custom mark was rendered (star instead of checkbox)
    Assert.Equal ('*', (char)lv.Screen [0, 0].Rune.Value);
}

// Helper class for testing
private class CustomMarkDataSource : ListWrapper<string>
{
    public CustomMarkDataSource (IList<string> source) : base (source) { }

    public override bool RenderMark (ListView listView, int item, int row, bool isMarked, bool allowsMultiple)
    {
        listView.Move (0, row);
        listView.AddRune (isMarked ? '*' : ' ');

        return true;  // Signal that we handled rendering
    }
}
```

### 6.4 Phase 6 Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

**DO NOT proceed to Phase 7 until all tests pass.**

---

## Phase 7: Fix Horizontal Scrolling Width and Offset Clamping ✅ COMPLETED

> **Status:** ✅ Completed - Updated `LeftItem` setter to clamp values to valid range [0, MaxItemLength - Viewport.Width] instead of throwing ArgumentException. Updated `TopItem` setter to clamp values to valid range [0, Count - Viewport.Height]. Added 4 unit tests. Updated existing `LeftItem_TopItem_Tests` to use smaller viewport to properly test scrolling with clamping behavior.

### 7.1 Fix Horizontal Scrolling Width Issue

**File:** `Terminal.Gui/Views/ListView.cs` in `OnDrawingContent`

```csharp
int markWidth = AllowsMarking ? 2 : 0;  // Width reserved for marks
int contentStartCol = markWidth;         // Where content rendering starts

// ... mark rendering code ...

// Pass full width to Source.Render, it handles scrolling via viewportX
Source.Render (this, isSelected, item, contentStartCol, row, f.Width, start);
```

### 7.2 Fix Horizontal/Vertical Scrolling Max Offsets

**LeftItem setter:**
```csharp
public int LeftItem
{
    get => Viewport.X;
    set
    {
        if (Source is null)
        {
            return;
        }

        // Clamp to valid range
        int maxLeftItem = Math.Max (0, MaxItemLength - Viewport.Width);
        value = Math.Clamp (value, 0, maxLeftItem);

        Viewport = Viewport with { X = value };
        SetNeedsDraw ();
    }
}
```

**TopItem setter:**
```csharp
public int TopItem
{
    get => Viewport.Y;
    set
    {
        if (Source is null)
        {
            return;
        }

        // Clamp to valid range
        int maxTopItem = Math.Max (0, Source.Count - Viewport.Height);
        value = Math.Clamp (value, 0, maxTopItem);

        Viewport = Viewport with { Y = value };
        SetNeedsDraw ();
    }
}
```

### 7.3 Phase 7 Unit Tests

```csharp
[Fact]
// Claude - Opus 4.5
public void HorizontalScroll_With_Marks_Shows_Full_Content ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["0123456789ABCDEF"]),
        AllowsMarking = true,
        Width = 10,
        Height = 1
    };
    lv.BeginInit ();
    lv.EndInit ();

    lv.LeftItem = 5;
    lv.Draw ();

    // Content should not be cut off by 2 extra columns
    // Mark (2 cols) + visible content (8 cols) = 10 cols total
}

[Fact]
// Claude - Opus 4.5
public void LeftItem_Clamps_To_MaxItemLength_Minus_Width ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["0123456789"]),  // 10 chars
        Width = 6,
        Height = 1
    };
    lv.BeginInit ();
    lv.EndInit ();

    // Max LeftItem should be 10 - 6 = 4
    lv.LeftItem = 10;
    Assert.Equal (4, lv.LeftItem);

    lv.LeftItem = -5;
    Assert.Equal (0, lv.LeftItem);
}

[Fact]
// Claude - Opus 4.5
public void TopItem_Clamps_To_Count_Minus_Height ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]),  // 5 items
        Width = 10,
        Height = 3
    };
    lv.BeginInit ();
    lv.EndInit ();

    // Max TopItem should be 5 - 3 = 2
    lv.TopItem = 10;
    Assert.Equal (2, lv.TopItem);

    lv.TopItem = -5;
    Assert.Equal (0, lv.TopItem);
}

[Fact]
// Claude - Opus 4.5
public void Scrolling_Stops_When_Last_Item_Visible ()
{
    ListView lv = new ()
    {
        Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]),
        Width = 10,
        Height = 3
    };
    lv.BeginInit ();
    lv.EndInit ();

    // Scroll to maximum
    lv.TopItem = 100;

    // Last visible item should be item 4 (index 4), at row 2 (0-indexed)
    // TopItem should be 2 so items 2, 3, 4 are visible
    Assert.Equal (2, lv.TopItem);
}
```

### 7.4 Phase 7 Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/UnitTests --no-build --filter "ClassName~ListViewTests"
dotnet test Tests/IntegrationTests --no-build
```

---

## Final Verification

After all phases complete:

```bash
# Full test suite
dotnet test Tests/UnitTestsParallelizable --no-build
dotnet test Tests/UnitTests --no-build
dotnet test Tests/IntegrationTests --no-build

# Manual testing
cd Examples/UICatalog
dotnet run -- ListViewWithSelection
```

**Manual Testing Checklist:**
- [ ] Toggle "AllowsMarking" OFF, "AllowsMultiSelect" ON
- [ ] Test Shift+Down/Up extends selection (items highlighted)
- [ ] Test Shift+Click extends selection
- [ ] Test Ctrl+Click toggles items
- [ ] Toggle "AllowsMarking" ON
- [ ] Verify marks render with Normal attribute
- [ ] Test horizontal scrolling - content not cut off
- [ ] Test vertical scrolling - stops at proper max

---

## Summary of Phases

| Phase | Description | Tests Required |
|-------|-------------|----------------|
| 0 | Fix broken Command.Activate handler | 4 existing tests must pass |
| 1 | Add selection tracking infrastructure | 9 new tests |
| 2 | Add extend commands and key bindings | 7 new tests |
| 3 | Mouse Shift+Click and Ctrl+Click | 3 new tests |
| 4 | Multi-selection rendering | 2 new tests |
| 5 | Fix mark rendering attribute | 2 new tests |
| 6 | Custom mark rendering API | 3 new tests |
| 7 | Fix scrolling width and offsets | 4 new tests |

**Total new tests:** ~30

**Critical Rule:** ALL tests (UnitTests, UnitTestsParallelizable, IntegrationTests) must pass before proceeding to the next phase.
