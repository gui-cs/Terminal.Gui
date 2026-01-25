# Popovers Deep Dive

Popovers are transient UI elements that appear above other content to display contextual information, such as menus, tooltips, autocomplete suggestions, and dialog boxes. Terminal.Gui's popover system provides a flexible, non-modal way to present temporary UI without blocking the rest of the application.

## Overview

Normally, Views cannot draw outside of their `Viewport`. To display content that appears to "pop over" other views, Terminal.Gui provides the popover system via @Terminal.Gui.Application.Popover. Popovers differ from alternatives like modifying `Border` or `Margin` behavior because they:

- Are managed centrally by the application
- Support focus and keyboard event routing
- Automatically hide in response to user actions
- Can receive global hotkeys even when not visible

## Creating a Popover

### Using PopoverMenu

The easiest way to create a popover is to use @Terminal.Gui.PopoverMenu, which provides a cascading menu implementation:

```csharp
// Create a popover menu with menu items
PopoverMenu contextMenu = new ([
    new MenuItem ("Cut", Command.Cut),
    new MenuItem ("Copy", Command.Copy),
    new MenuItem ("Paste", Command.Paste),
    new MenuItem ("Select All", Command.SelectAll)
]);

// IMPORTANT: Register before showing
Application.Popover?.Register (contextMenu);

// Show at mouse position or specific location
contextMenu.MakeVisible (); // Uses current mouse position
// OR
contextMenu.MakeVisible (new Point (10, 5)); // Specific location
```

### Creating a Custom Popover

To create a custom popover, inherit from @Terminal.Gui.PopoverBaseImpl:

```csharp
public class MyCustomPopover : PopoverBaseImpl
{
    public MyCustomPopover ()
    {
        // PopoverBaseImpl already sets up required defaults:
        // - ViewportSettings with Transparent and TransparentMouse flags
        // - Command.Quit binding to hide the popover
        // - Width/Height set to Dim.Fill()
        
        // Add your custom content
        Label label = new () { Text = "Custom Popover Content" };
        Add (label);
        
        // Optionally override size
        Width = 40;
        Height = 10;
    }
}

// Usage:
MyCustomPopover myPopover = new ();
Application.Popover?.Register (myPopover);
Application.Popover?.Show (myPopover);
```

## Popover Requirements

A View qualifies as a popover if it:

1. **Implements @Terminal.Gui.IPopover** - Provides the `Current` property for runnable association
2. **Is Focusable** - `CanFocus = true` to receive keyboard input
3. **Is Transparent** - `ViewportSettings` includes both:
   - `ViewportSettings.Transparent` - Allows content beneath to show through
   - `ViewportSettings.TransparentMouse` - Mouse clicks outside subviews pass through
4. **Handles Quit** - Binds `Application.QuitKey` to `Command.Quit` and sets `Visible = false`

@Terminal.Gui.PopoverBaseImpl provides all these requirements by default.

## Registration and Lifecycle

### Registration (REQUIRED)

**All popovers must be registered before they can be shown:**

```csharp
PopoverMenu popover = new ([...]);

// REQUIRED: Register with the application
Application.Popover?.Register (popover);

// Now you can show it
Application.Popover?.Show (popover);
// OR
popover.MakeVisible (); // For PopoverMenu
```

**Why Registration is Required:**
- Enables keyboard event routing to the popover
- Allows global hotkeys to work even when popover is hidden
- Manages popover lifecycle and disposal

### Showing and Hiding

**Show a popover:**
```csharp
Application.Popover?.Show (popover);
```

**Hide a popover:**
```csharp
// Method 1: Via ApplicationPopover
Application.Popover?.Hide (popover);

// Method 2: Set Visible property
popover.Visible = false;

// Automatic hiding occurs when:
// - User presses Application.QuitKey (typically Esc)
// - User clicks outside the popover (not on a subview)
// - Another popover is shown
```

### Lifecycle Management

**Registered popovers:**
- Have their lifetime managed by the application
- Are automatically disposed when `Application.Shutdown ()` is called
- Receive keyboard events based on their associated runnable

**To manage lifetime manually:**
```csharp
// Deregister to take ownership of disposal
Application.Popover?.DeRegister (popover);

// Now you're responsible for disposal
popover.Dispose ();
```

## Keyboard Event Routing

### Global Hotkeys

Registered popovers receive keyboard events even when not visible, enabling global hotkey support:

```csharp
PopoverMenu menu = new ([...]);
menu.Key = Key.F10.WithShift; // Default hotkey

Application.Popover?.Register (menu);

// Now pressing Shift+F10 anywhere in the app will show the menu
```

### Runnable Association

The @Terminal.Gui.IPopover.Current property associates a popover with a specific @Terminal.Gui.IRunnable:

- If `null`: Popover receives all keyboard events from the application
- If set: Popover only receives events when the associated runnable is active
- Automatically set to `Application.TopRunnableView` during registration

```csharp
// Associate with a specific runnable
myPopover.Current = myWindow; // Only active when myWindow is the top runnable
```

## Focus and Input

**When visible:**
- Popovers receive focus automatically
- All keyboard input goes to the popover until hidden
- Mouse clicks on subviews are captured
- Mouse clicks outside subviews pass through (due to `TransparentMouse`)

**When hidden:**
- Only registered hotkeys are processed
- Other keyboard input is not captured

## Layout and Positioning

### Default Layout

@Terminal.Gui.PopoverBaseImpl sets `Width = Dim.Fill ()` and `Height = Dim.Fill ()`, making the popover fill the screen by default. The transparent viewport settings allow content beneath to remain visible.

### Custom Sizing

Override `Width` and `Height` to customize size:

```csharp
public class MyPopover : PopoverBaseImpl
{
    public MyPopover ()
    {
        Width = 40;  // Fixed width
        Height = Dim.Auto (); // Auto height based on content
    }
}
```

### Positioning with PopoverMenu

@Terminal.Gui.PopoverMenu provides positioning helpers:

```csharp
// Position at specific screen coordinates
menu.SetPosition (new Point (10, 5));

// Show and position in one call
menu.MakeVisible (new Point (10, 5));

// Uses mouse position if null
menu.MakeVisible (); // Uses Application.Mouse.LastMousePosition
```

The menu automatically adjusts position to ensure it remains fully visible on screen.

## Built-in Popover Types

### PopoverMenu

@Terminal.Gui.PopoverMenu is a sophisticated cascading menu implementation used for:
- Context menus
- @Terminal.Gui.MenuBar drop-down menus
- Custom menu scenarios

**Key Features:**
- Cascading submenus with automatic positioning
- Keyboard navigation (arrow keys, hotkeys)
- Automatic key binding from Commands
- Mouse support
- Separator lines via `new Line ()`

**Example with submenus:**
```csharp
PopoverMenu fileMenu = new ([
    new MenuItem ("New", Command.New),
    new MenuItem ("Open", Command.Open),
    new MenuItem {
        Title = "Recent",
        SubMenu = new Menu ([
            new MenuItem ("File1.txt", Command.Open),
            new MenuItem ("File2.txt", Command.Open)
        ])
    },
    new Line (),
    new MenuItem ("Exit", Command.Quit)
]);

Application.Popover?.Register (fileMenu);
fileMenu.MakeVisible ();
```

## Mouse Event Handling

Popovers use `ViewportSettings.TransparentMouse`, which means:

- **Clicks on popover subviews**: Captured and handled normally
- **Clicks outside subviews**: Pass through to views beneath
- **Clicks on background**: Automatically hide the popover

This creates the expected behavior where clicking outside a menu or dialog closes it.

## Best Practices

1. **Always Register First**
   ```csharp
   // WRONG - Will throw InvalidOperationException
   PopoverMenu menu = new ([...]);
   menu.MakeVisible ();
   
   // CORRECT
   PopoverMenu menu = new ([...]);
   Application.Popover?.Register (menu);
   menu.MakeVisible ();
   ```

2. **Use PopoverMenu for Menus**
   - Don't reinvent the wheel for standard menu scenarios
   - Leverage built-in keyboard navigation and positioning

3. **Manage Lifecycle Appropriately**
   - Let the application manage disposal for long-lived popovers
   - Deregister and manually dispose short-lived or conditional popovers

4. **Test Global Hotkeys**
   - Ensure hotkeys don't conflict with application-level keys
   - Consider providing configuration for custom hotkeys

5. **Handle Edge Cases**
   - Test positioning near screen edges
   - Verify behavior with multiple runnables
   - Test with keyboard-only navigation

## Common Scenarios

### Context Menu on Right-Click

```csharp
PopoverMenu contextMenu = new ([...]);
contextMenu.MouseFlags = MouseFlags.Button3Clicked; // Right-click
Application.Popover?.Register (contextMenu);

myView.MouseClick += (s, e) =>
{
    if (e.MouseEvent.Flags == MouseFlags.Button3Clicked)
    {
        contextMenu.MakeVisible (myView.ScreenToViewport (e.MouseEvent.Position));
        e.Handled = true;
    }
};
```

### Autocomplete Popup

```csharp
public class AutocompletePopover : PopoverBaseImpl
{
    private ListView _listView;
    
    public AutocompletePopover ()
    {
        Width = 30;
        Height = 10;
        
        _listView = new ListView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Add (_listView);
    }
    
    public void ShowSuggestions (IEnumerable<string> suggestions, Point position)
    {
        _listView.SetSource (suggestions.ToList ());
        // Position below the text entry field
        X = position.X;
        Y = position.Y + 1;
        Visible = true;
    }
}
```

### Global Command Palette

```csharp
PopoverMenu commandPalette = new (GetAllCommands ());
commandPalette.Key = Key.P.WithCtrl; // Ctrl+P to show

Application.Popover?.Register (commandPalette);

// Now Ctrl+P anywhere in the app shows the command palette
```

## API Reference

- @Terminal.Gui.IPopover - Interface for popover views
- @Terminal.Gui.PopoverBaseImpl - Abstract base class for custom popovers
- @Terminal.Gui.PopoverMenu - Cascading menu implementation
- @Terminal.Gui.ApplicationPopover - Popover manager (accessed via `Application.Popover`)

## See Also

- [Keyboard Deep Dive](keyboard.md) - Understanding keyboard event routing
- [Mouse Deep Dive](mouse.md) - Mouse event handling
- [View List](views.md) - Full list of views including MenuBar