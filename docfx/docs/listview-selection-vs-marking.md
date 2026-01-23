# ListView: Selection vs Marking

## Overview

`ListView` supports two distinct but complementary concepts for user interaction with list items:

1. **Selection** - The currently highlighted/focused item
2. **Marking** - Items that have been "checked" by the user

This document explains why both concepts exist and how they work together.

## Selection (SelectedItem)

**Selection** represents the currently highlighted or focused item in the list. This is the item that:
- Is visually highlighted with a different color scheme
- Will be activated when the user presses ENTER
- Changes as the user navigates with arrow keys
- Is tracked by the `SelectedItem` property

### Key Characteristics:
- **Exactly ONE item** can be selected at a time (or none if `SelectedItem` is null)
- Selection changes as the user navigates through the list
- Selection is independent of marking
- Used for navigation and determining which item to activate

### Example:
```csharp
var listView = new ListView
{
    Source = new ListWrapper<string>(["Apple", "Banana", "Cherry"])
};

// Only one item can be selected at a time
listView.SelectedItem = 0; // Apple is highlighted
listView.SelectedItem = 1; // Now Banana is highlighted (Apple is no longer selected)
```

## Marking (IsMarked/SetMark)

**Marking** represents items that have been "checked" or flagged by the user. When `AllowsMarking` is enabled, marked items are shown with `[x]` and unmarked items with `[ ]`. This allows users to:
- Flag items for batch operations
- Select multiple items for processing
- Build a collection of items to act upon

### Key Characteristics:
- **Zero or more items** can be marked simultaneously
- Marking persists as the user navigates through the list
- Items are marked/unmarked by pressing SPACE on the selected item
- The marked state is tracked by `IListDataSource.IsMarked()`

### Example:
```csharp
var listView = new ListView
{
    Source = new ListWrapper<string>(["File1.txt", "File2.txt", "File3.txt"]),
    AllowsMarking = true,
    AllowsMultipleSelection = true
};

// User can mark multiple items:
// - Navigate to File1.txt with arrow keys (SelectedItem = 0)
// - Press SPACE to mark it (IsMarked(0) = true)
// - Navigate to File3.txt with arrow keys (SelectedItem = 2)
// - Press SPACE to mark it (IsMarked(2) = true)
// Now items 0 and 2 are marked, while item 2 is currently selected
```

## AllowsMultipleSelection Property

Despite its name, `AllowsMultipleSelection` controls **marking** behavior, not selection. This property determines whether multiple items can be marked simultaneously:

- **When `false` (default):** Only one item can be marked at a time. Marking a new item automatically unmarks the previously marked item (single-mark mode).
- **When `true`:** Multiple items can be marked simultaneously (multi-mark mode).

This setting only has an effect when `AllowsMarking` is also `true`.

### Example - Single Mark Mode:
```csharp
var listView = new ListView
{
    Source = new ListWrapper<string>(["Item1", "Item2", "Item3"]),
    AllowsMarking = true,
    AllowsMultipleSelection = false  // Single-mark mode
};

// User marks Item1 (IsMarked(0) = true)
// User navigates to Item2 and presses SPACE
// Result: Only Item2 is marked (IsMarked(0) = false, IsMarked(1) = true)
```

### Example - Multi Mark Mode:
```csharp
var listView = new ListView
{
    Source = new ListWrapper<string>(["Item1", "Item2", "Item3"]),
    AllowsMarking = true,
    AllowsMultipleSelection = true  // Multi-mark mode
};

// User marks Item1 (IsMarked(0) = true)
// User navigates to Item2 and presses SPACE
// Result: Both items are marked (IsMarked(0) = true, IsMarked(1) = true)
```

## Why Both Concepts Exist

The separation of selection and marking serves different but complementary purposes:

### Selection is for Navigation
- Provides visual feedback about the current focus
- Determines which item will be activated on ENTER
- Changes frequently as the user browses the list
- Always points to exactly one item (or none)

### Marking is for Batch Operations
- Allows users to flag items for later processing
- Persists as the user navigates
- Supports multi-select scenarios (when `AllowsMultipleSelection = true`)
- Enables "select all", "select none", "invert selection" operations

### Real-World Analogy
Think of a file manager:
- **Selection** = The file you're currently hovering over with the keyboard/mouse (highlighted)
- **Marking** = Files you've checked for deletion, copying, or other batch operations

You can navigate through files (changing selection) while keeping certain files marked for later action. The marking persists even as you move the selection highlight to other items.

## Common Use Cases

### Use Case 1: File Manager
```csharp
// Users can browse files (selection) while maintaining a set of
// marked files for batch deletion or copying
var fileList = new ListView
{
    AllowsMarking = true,
    AllowsMultipleSelection = true
};
```

### Use Case 2: Email Inbox
```csharp
// Users can read emails (selection) while marking messages
// for archiving or deletion
var emailList = new ListView
{
    AllowsMarking = true,
    AllowsMultipleSelection = true
};
```

### Use Case 3: Single-Choice List (No Marking)
```csharp
// Users navigate and select one item to proceed
var optionList = new ListView
{
    AllowsMarking = false  // No marking needed
};
// User presses ENTER on selected item to activate
```

### Use Case 4: Task List with Single Active Task
```csharp
// Only one task can be marked as "in progress" at a time,
// but user can browse all tasks
var taskList = new ListView
{
    AllowsMarking = true,
    AllowsMultipleSelection = false  // Only one task marked
};
```

## Best Practices

1. **Use marking for batch operations:** If users need to select multiple items for processing, enable `AllowsMarking = true` and `AllowsMultipleSelection = true`.

2. **Disable marking for simple lists:** If the list is just for browsing and selecting a single item to activate, keep `AllowsMarking = false`.

3. **Provide visual feedback:** When marking is enabled, ensure your UI clearly shows what happens to marked items (the default `[x]` and `[ ]` indicators help with this).

4. **Handle marked items in events:** When processing batch operations, iterate through all items and check `Source.IsMarked(i)` to find marked items.

## Code Example: Processing Marked Items

```csharp
var listView = new ListView
{
    Source = new ListWrapper<string>(["File1.txt", "File2.txt", "File3.txt"]),
    AllowsMarking = true,
    AllowsMultipleSelection = true
};

// Button to process marked items
var processButton = new Button("Delete Marked Files");
processButton.Accepting += (s, e) =>
{
    List<string> markedFiles = new ();
    
    for (int i = 0; i < listView.Source.Count; i++)
    {
        if (listView.Source.IsMarked(i))
        {
            markedFiles.Add((string)listView.Source.ToList()[i]);
        }
    }
    
    // Process the marked files
    foreach (string file in markedFiles)
    {
        // Delete or process the file
        Console.WriteLine($"Processing: {file}");
    }
};
```

## Comparison with Other UI Libraries

Terminal.Gui's approach to ListView is similar to patterns found in other popular UI frameworks, though implementations vary. Understanding these similarities helps clarify the design rationale.

### WPF (Windows Presentation Foundation)

**ListBox Control:**
- **Selection:** Uses `SelectedItems` property (collection) for multi-selection
- **SelectionMode:** `Single`, `Multiple`, or `Extended` (with Ctrl/Shift modifiers)
- **Per-Item Selection:** Items have `IsSelected` property for MVVM binding
- **No Built-in Marking:** No separate "checked" state; use custom templates if needed

**Key Difference:** WPF allows multiple items to be *selected* (highlighted) simultaneously when `SelectionMode` is `Multiple` or `Extended`. Terminal.Gui only allows one selected item but supports multiple marked items.

```xml
<!-- WPF: Multiple highlighted selections -->
<ListBox SelectionMode="Extended">
  <!-- User can Ctrl+Click to highlight multiple items -->
</ListBox>
```

### WinForms

**ListBox vs CheckedListBox:**
- **ListBox:** 
  - `SelectionMode`: `One`, `MultiSimple`, or `MultiExtended`
  - Can highlight multiple items with `MultiSimple`/`MultiExtended`
  - No checkboxes
- **CheckedListBox:**
  - Displays checkboxes for each item
  - `SelectionMode` limited to `One` or `None` (can't highlight multiple)
  - Checked items tracked via `CheckedItems` collection
  - Checking and selection are independent

**Key Similarity:** WinForms' `CheckedListBox` has the closest model to Terminal.Gui ListView - selection (highlight) is separate from checked state, though WinForms only allows single selection when checkboxes are present.

```csharp
// WinForms: Separate controls for different needs
var listBox = new ListBox { SelectionMode = SelectionMode.MultiExtended }; // Multiple highlights, no checks
var checkedListBox = new CheckedListBox(); // Checkboxes, single highlight only
```

### GTK+ (TreeView)

**TreeView with GtkTreeSelection:**
- **Selection Mode:** `NONE`, `SINGLE`, `BROWSE`, or `MULTIPLE`
- **Multiple Selection:** Allows multiple rows to be selected (highlighted) with `GTK_SELECTION_MULTIPLE`
- **No Built-in Checked State:** Must add checkboxes as cell renderers manually
- **Separation:** Selection (highlighting) handled by view; checked state (if added) is in the model

**Key Similarity:** GTK separates visual selection from data state. If you add checkboxes via cell renderers, they function independently from selection - similar to Terminal.Gui's approach.

```c
// GTK: Selection mode controls highlighting
GtkTreeSelection *selection = gtk_tree_view_get_selection(treeview);
gtk_tree_selection_set_mode(selection, GTK_SELECTION_MULTIPLE);
// Checkboxes require custom cell renderer and model column
```

### Qt (QListWidget)

**QListWidget:**
- **Selection Mode:** `SingleSelection`, `MultiSelection`, `ExtendedSelection`, etc.
- **Multiple Selection:** Can highlight multiple items with appropriate mode
- **Check State:** Items can have checkboxes via `setCheckState(Qt.Checked)`
- **Independence:** Selection (highlighting) and check state are completely independent

**Key Similarity:** Qt clearly separates selection from check state, just like Terminal.Gui. You can have items that are checked but not selected, selected but not checked, or both.

```python
# Qt: Independent selection and checking
list_widget.setSelectionMode(QAbstractItemView.ExtendedSelection)
item.setCheckState(Qt.Checked)  # Checking doesn't affect selection
selected = list_widget.selectedItems()  # Different from checked items
```

### Java Swing (JList)

**JList:**
- **Selection Mode:** `SINGLE_SELECTION`, `SINGLE_INTERVAL_SELECTION`, `MULTIPLE_INTERVAL_SELECTION`
- **Multiple Selection:** Supports multiple highlighted items with appropriate mode
- **No Built-in Checkboxes:** Must use custom cell renderers for checkboxes
- **Extension Required:** Checking behavior requires custom implementation

**Key Difference:** Swing's JList focuses on selection only. Any checked state requires custom rendering and state management.

```java
// Swing: Selection mode for multiple highlights
list.setSelectionMode(ListSelectionModel.MULTIPLE_INTERVAL_SELECTION);
// No built-in checked state - custom implementation needed
```

### HTML (Web)

**Select Element:**
- **Multiple Attribute:** `<select multiple>` allows multiple selections
- **User Experience:** Requires Ctrl/Cmd key to select multiple (often confusing)
- **No Visual Checkboxes:** Selected items are highlighted only

**Checkbox Group:**
- **Independent Checkboxes:** Each checkbox is independent
- **Clear UX:** Visually obvious that multiple selections are possible
- **No Single Selection Concept:** No "currently focused" item distinct from checked items

**Key Insight:** Web developers often prefer checkbox groups over `<select multiple>` for better UX, mirroring the preference for distinct selection/marking in desktop applications.

```html
<!-- HTML: Checkbox group is clearer than select multiple -->
<fieldset>
  <legend>Select files</legend>
  <label><input type="checkbox" name="files[]" value="1"> File 1</label>
  <label><input type="checkbox" name="files[]" value="2"> File 2</label>
</fieldset>
```

### Comparison Table

| Library | Multiple Highlighted Selection | Separate Checked/Marked State | Terminal.Gui Equivalent |
|---------|-------------------------------|-------------------------------|------------------------|
| **WPF ListBox** | Yes (SelectionMode) | No (custom only) | Multiple selection ≈ marking |
| **WinForms ListBox** | Yes (SelectionMode) | N/A | Multiple highlights |
| **WinForms CheckedListBox** | No (single only) | Yes (CheckedItems) | ✓ Similar model |
| **GTK TreeView** | Yes (SelectionMode) | Manual (cell renderer) | Flexible approach |
| **Qt QListWidget** | Yes (SelectionMode) | Yes (CheckState) | ✓ Very similar |
| **Swing JList** | Yes (SelectionMode) | No (custom only) | Selection only |
| **HTML Select** | Yes (multiple attr) | N/A | Multiple highlights |
| **HTML Checkboxes** | N/A | Yes (implicit) | Pure marking |
| **Terminal.Gui ListView** | No (single only) | Yes (IsMarked) | ✓ Unique hybrid |

### Why Terminal.Gui's Approach Makes Sense

Terminal.Gui's design reflects the constraints and advantages of console UIs:

1. **Console Navigation:** Terminal UIs naturally have a "current focus" (one highlighted item) due to keyboard-driven navigation. Unlike GUIs with mouse pointers, you can't visually show multiple "highlighted" items effectively in a console.

2. **Clear Visual Language:** Using `[x]` for marked items and highlighting for the selected item provides clear, unambiguous visual feedback in character-based displays.

3. **Familiar Pattern:** The model matches `CheckedListBox` in WinForms and checkbox behavior in Qt - proven patterns from GUI frameworks adapted for console constraints.

4. **Practical Efficiency:** Users can quickly navigate (selection) while building a set of items for batch processing (marking), similar to how file managers work.

5. **Naming Clarification:** While `AllowsMultipleSelection` might be better named `AllowsMultipleMarking`, the underlying dual-concept model aligns with established UI patterns.

The key insight is that Terminal.Gui adapted the best aspects of GUI list controls for console environments, where "selection" naturally means "current focus" rather than "highlighted items," and "marking" provides the multi-item functionality that GUI frameworks achieve through multiple highlights.

## Summary

| Aspect | Selection (SelectedItem) | Marking (IsMarked) |
|--------|-------------------------|-------------------|
| **Quantity** | Exactly one (or none) | Zero or more |
| **Purpose** | Navigation and focus | Batch operations |
| **Persistence** | Changes with navigation | Persists during navigation |
| **Visual Indicator** | Highlight/different color | `[x]` or `[ ]` prefix |
| **Changed By** | Arrow keys, mouse clicks | SPACE key |
| **Controlled By** | Built-in navigation | `AllowsMarking` + `AllowsMultipleSelection` |

The dual concept of selection and marking provides users with a familiar and powerful way to interact with lists, supporting both simple single-item activation and complex multi-item batch operations.
