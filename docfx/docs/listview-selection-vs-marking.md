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
// Now items 0 and 2 are marked, while item 1 is currently selected
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
    List<string> markedFiles = [];
    
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
