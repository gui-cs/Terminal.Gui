# View Deep Dive

[View](~/api/Terminal.Gui.ViewBase.View.yml) is the base class for all visible UI elements in Terminal.Gui. View provides core functionality for layout, drawing, input handling, navigation, and scrolling. All interactive controls, windows, and dialogs derive from View.

See the [Views Overview](views.md) for a catalog of all built-in View subclasses.

## Table of Contents

- [View Hierarchy](#view-hierarchy)
- [View Composition](#view-composition)
- [Core Concepts](#core-concepts)
- [View Lifecycle](#view-lifecycle)
- [Subsystems](#subsystems)
- [Common View Patterns](#common-view-patterns)
- [Runnable](#runnable-views)

---

## View Hierarchy

### Terminology

- **[View](~/api/Terminal.Gui.ViewBase.View.yml)** - The base class for all visible UI elements
- **SubView** - A View that is contained in another View and rendered as part of the containing View's content area. SubViews are added via <xref:Terminal.Gui.ViewBase.View.Add(Terminal.Gui.ViewBase.View)>
- **SuperView** - The View that contains SubViews. Each View has a <xref:Terminal.Gui.ViewBase.View.SuperView> property that references its container
- **Child View** - A view that holds a reference to another view in a parent/child relationship (used sparingly; generally SubView/SuperView is preferred)
- **Parent View** - A view that holds a reference to another view but is NOT a SuperView (used sparingly)

### Key Properties

- <xref:Terminal.Gui.ViewBase.View.SubViews> - Read-only list of all SubViews added to this View
- <xref:Terminal.Gui.ViewBase.View.SuperView> - The View's container (null if the View has no container)
- <xref:Terminal.Gui.ViewBase.View.Id> - Unique identifier for the View (should be unique among siblings)
- <xref:Terminal.Gui.ViewBase.View.Data> - Arbitrary data attached to the View
- <xref:Terminal.Gui.ViewBase.View.App> - The application context this View belongs to
- <xref:Terminal.Gui.ViewBase.View.Driver> - The driver used for rendering (derived from App). This is a shortcut to `App.Driver` for convenience.

---

## View Composition

Views are composed of several nested layers that define how they are positioned, drawn, and scrolled:

[!INCLUDE [View Composition](~/includes/view-composition.md)]

### The Layers

1. **<xref:Terminal.Gui.ViewBase.View.Frame>** - The outermost rectangle defining the View's location and size relative to the SuperView's content area
2. **[Margin](~/api/Terminal.Gui.ViewBase.Margin.yml)** - Adornment that provides spacing between the View and other SubViews
3. **[Border](~/api/Terminal.Gui.ViewBase.Border.yml)** - Adornment that draws the visual border and title
4. **[Padding](~/api/Terminal.Gui.ViewBase.Padding.yml)** - Adornment that provides spacing between the border and the viewport
5. **<xref:Terminal.Gui.ViewBase.View.Viewport>** - Rectangle describing the visible portion of the content area
6. **Content Area** - The total area where content can be drawn (defined by <xref:Terminal.Gui.ViewBase.View.GetContentSize>)

See the [Layout Deep Dive](layout.md) for complete details on View composition and layout.

---

## Core Concepts

### Frame vs. Viewport

- **Frame** - The View's location and size in SuperView-relative coordinates. Frame includes all adornments (Margin, Border, Padding)
- **Viewport** - The visible "window" into the View's content, located inside the adornments. Viewport coordinates are always relative to (0,0) of the content area

```csharp
// Frame is SuperView-relative
view.Frame = new Rectangle(10, 5, 50, 20);

// Viewport is content-relative (the visible portal)
view.Viewport = new Rectangle(0, 0, 45, 15); // Adjusted for adornments
```

### Content Area and Scrolling

The **Content Area** is where the View's content is drawn. By default, the content area size matches the Viewport size. To enable scrolling:

1. Call <xref:Terminal.Gui.ViewBase.View.SetContentSize(System.Nullable{System.Drawing.Size})> with a size larger than the Viewport
2. Change `Viewport.Location` to scroll the content

See the [Scrolling Deep Dive](scrolling.md) for complete details.

### Adornments

[Adornments](~/api/Terminal.Gui.ViewBase.Adornment.yml) are special Views that surround the content:

- **[Margin](~/api/Terminal.Gui.ViewBase.Margin.yml)** - Transparent spacing outside the Border
- **[Border](~/api/Terminal.Gui.ViewBase.Border.yml)** - Visual frame with [LineStyle](~/api/Terminal.Gui.Drawing.LineStyle.yml), title, and arrangement UI
- **[Padding](~/api/Terminal.Gui.ViewBase.Padding.yml)** - Spacing inside the Border, outside the Viewport

Each adornment has a [Thickness](~/api/Terminal.Gui.Drawing.Thickness.yml) that defines the width of each side (Top, Right, Bottom, Left).

See the [Layout Deep Dive](layout.md) for complete details on adornments.

---

## View Lifecycle

### Initialization

Views implement [ISupportInitializeNotification](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.isupportinitializenotification):

1. **Constructor** - Creates the View and sets up default state
2. **<xref:Terminal.Gui.ViewBase.View.BeginInit>** - Signals initialization is starting
3. **<xref:Terminal.Gui.ViewBase.View.EndInit>** - Signals initialization is complete; raises <xref:Terminal.Gui.ViewBase.View.Initialized> event
4. **<xref:Terminal.Gui.ViewBase.View.IsInitialized>** - Property indicating if initialization is complete

During initialization, <xref:Terminal.Gui.ViewBase.View.App> is set to reference the application context, enabling views to access application services like the driver and current session.

### Disposal

Views are [IDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable):

- Call <xref:Terminal.Gui.ViewBase.View.Dispose> to clean up resources
- The <xref:Terminal.Gui.ViewBase.View.Disposing> event is raised when disposal begins
- Automatically disposes SubViews, adornments, and scroll bars

---

## Subsystems

View is organized as a partial class across multiple files, each handling a specific subsystem:
  
### Commands

See the [Command Deep Dive](command.md).

- [View.AddCommand](~/api/Terminal.Gui.ViewBase.View.yml) - Declares commands the View supports
- [View.InvokeCommand](~/api/Terminal.Gui.ViewBase.View.yml) - Invokes a command
- [Command](~/api/Terminal.Gui.Input.Command.yml) enum - Standard set of commands (Accept, Activate, HotKey, etc.)

### Input Handling

#### Keyboard

See the [Keyboard Deep Dive](keyboard.md).

- <xref:Terminal.Gui.ViewBase.View.KeyBindings> - Maps keys to Commands
- <xref:Terminal.Gui.ViewBase.View.HotKey> - The hot key for the View
- <xref:Terminal.Gui.ViewBase.View.HotKeySpecifier> - Character used to denote hot keys in text (default: '_')
- Events: `KeyDown`, `InvokingKeyBindings`

#### Mouse

See the [Mouse Deep Dive](mouse.md).

- <xref:Terminal.Gui.ViewBase.View.MouseBindings> - Maps mouse events to Commands
- <xref:Terminal.Gui.ViewBase.View.MouseHoldRepeat> - Enables continuous button press events
- View.Highlight event - Event for visual feedback on mouse hover/click
- View.HighlightStyle - Visual style when highlighted
- Events: `MouseEnter`, `MouseLeave`, `MouseEvent`

### Layout and Arrangement

See the [Layout Deep Dive](layout.md) and [Arrangement Deep Dive](arrangement.md).

#### Position and Size

- <xref:Terminal.Gui.ViewBase.View.X> - Horizontal position using [Pos](~/api/Terminal.Gui.ViewBase.Pos.yml)
- <xref:Terminal.Gui.ViewBase.View.Y> - Vertical position using [Pos](~/api/Terminal.Gui.ViewBase.Pos.yml)
- <xref:Terminal.Gui.ViewBase.View.Width> - Width using [Dim](~/api/Terminal.Gui.ViewBase.Dim.yml)
- <xref:Terminal.Gui.ViewBase.View.Height> - Height using [Dim](~/api/Terminal.Gui.ViewBase.Dim.yml)

#### Layout Features

- <xref:Terminal.Gui.ViewBase.Dim.Auto*> - Automatic sizing based on content
- <xref:Terminal.Gui.ViewBase.Pos.AnchorEnd*> - Anchor to right/bottom edges
- [Pos.Align](~/api/Terminal.Gui.ViewBase.Pos.yml) - Align views relative to each other
- [Pos.Center](~/api/Terminal.Gui.ViewBase.Pos.yml) - Center within SuperView
- [Dim.Fill](~/api/Terminal.Gui.ViewBase.Dim.yml) - Fill available space

#### Arrangement

- <xref:Terminal.Gui.ViewBase.View.Arrangement> - Controls if View is movable/resizable
- [ViewArrangement](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) - Flags: Fixed, Movable, Resizable, Overlapped

#### Events

- `LayoutStarted` - Before layout begins
- `LayoutComplete` - After layout completes
- `FrameChanged` - When Frame changes
- `ViewportChanged` - When Viewport changes

### Drawing

See the [Drawing Deep Dive](drawing.md).

#### Color and Style

- [View.Scheme](~/api/Terminal.Gui.ViewBase.View.yml) - Color scheme for the View
- [View.SetAttribute](~/api/Terminal.Gui.ViewBase.View.yml) - Sets the attribute for subsequent drawing
- [View.SetAttributeForRole](~/api/Terminal.Gui.ViewBase.View.yml) - Sets attribute based on [VisualRole](~/api/Terminal.Gui.Drawing.VisualRole.yml)

See the [Scheme Deep Dive](scheme.md) for details on color theming.

#### Drawing Methods

- [View.Draw](~/api/Terminal.Gui.ViewBase.View.yml) - Main drawing method
- [View.AddRune](~/api/Terminal.Gui.ViewBase.View.yml) - Draws a single Rune
- [View.AddStr](~/api/Terminal.Gui.ViewBase.View.yml) - Draws a string
- [View.Move](~/api/Terminal.Gui.ViewBase.View.yml) - Positions the **Draw Cursor** (internal rendering position, NOT the visible Terminal Cursor)
- [View.Clear](~/api/Terminal.Gui.ViewBase.View.yml) - Clears the View's content

> [!WARNING]
> `Move()` sets the **Draw Cursor** (where next character renders), NOT the **Terminal Cursor** (visible cursor indicator).
> To position the Terminal Cursor, use the `View.Cursor` property. See [Cursor Management](cursor.md) for details.

#### Drawing Events

- `DrawingContent` - Before content is drawn
- `DrawingContentComplete` - After content is drawn
- `DrawingAdornments` - Before adornments are drawn
- `DrawingAdornmentsComplete` - After adornments are drawn

#### Invalidation

- [View.SetNeedsDraw](~/api/Terminal.Gui.ViewBase.View.yml) - Marks View as needing redraw
- [View.NeedsDraw](~/api/Terminal.Gui.ViewBase.View.yml) - Property indicating if View needs redraw

### Navigation

See the [Navigation Deep Dive](navigation.md).

- <xref:Terminal.Gui.ViewBase.View.CanFocus> - Whether the View can receive keyboard focus
- <xref:Terminal.Gui.ViewBase.View.HasFocus> - Whether the View currently has focus
- <xref:Terminal.Gui.ViewBase.View.TabStop> - [TabBehavior](~/api/Terminal.Gui.ViewBase.TabBehavior.yml) for tab navigation
- View.ZOrder - Order in tab navigation
- [View.SetFocus](~/api/Terminal.Gui.ViewBase.View.yml) - Gives focus to the View

Events:
- `HasFocusChanging` - Before focus changes (cancellable)
- `HasFocusChanged` - After focus changes
- `Accepting` - When Command.Accept is invoked (typically Enter key)
- `Accepted` - After Command.Accept completes
- `Activating` - When Command.Activate is invoked (typically Space or mouse click)
- `Activated` - After Command.Activate completes

### Scrolling

See the [Scrolling Deep Dive](scrolling.md).

- <xref:Terminal.Gui.ViewBase.View.Viewport> - Visible portion of the content area
- <xref:Terminal.Gui.ViewBase.View.GetContentSize> - Returns size of scrollable content
- <xref:Terminal.Gui.ViewBase.View.SetContentSize(System.Nullable{System.Drawing.Size})> - Sets size of scrollable content
- <xref:Terminal.Gui.ViewBase.View.ScrollHorizontal(System.Int32)> - Scrolls content horizontally
- <xref:Terminal.Gui.ViewBase.View.ScrollVertical(System.Int32)> - Scrolls content vertically
- <xref:Terminal.Gui.ViewBase.View.VerticalScrollBar> - Built-in vertical scrollbar
- <xref:Terminal.Gui.ViewBase.View.HorizontalScrollBar> - Built-in horizontal scrollbar
- <xref:Terminal.Gui.ViewBase.View.ViewportSettings> - [ViewportSettingsFlags](~/api/Terminal.Gui.ViewBase.ViewportSettingsFlags.yml) controlling scroll behavior

### Text

- <xref:Terminal.Gui.ViewBase.View.Text> - The View's text content
- <xref:Terminal.Gui.ViewBase.View.Title> - The View's title (shown in Border)
- <xref:Terminal.Gui.ViewBase.View.TextFormatter> - Handles text formatting and alignment
- <xref:Terminal.Gui.ViewBase.View.TextDirection> - Text direction (LeftRight, RightToLeft, TopToBottom)
- <xref:Terminal.Gui.ViewBase.View.TextAlignment> - Text alignment (Left, Centered, Right, Justified)
- <xref:Terminal.Gui.ViewBase.View.VerticalTextAlignment> - Vertical alignment (Top, Middle, Bottom, Justified)

---

## View Lifecycle

### 1. Creation

```csharp
View view = new ()
{
    X = Pos.Center(),
    Y = Pos.Center(),
    Width = Dim.Percent(50),
    Height = Dim.Fill()
};
```

### 2. Initialization

When a View is added to a SuperView or when Application.Run is called:

1. <xref:Terminal.Gui.ViewBase.View.BeginInit> is called
2. <xref:Terminal.Gui.ViewBase.View.EndInit> is called
3. <xref:Terminal.Gui.ViewBase.View.IsInitialized> becomes true
4. [Initialized](~/api/Terminal.Gui.ViewBase.View.yml) event is raised

### 3. Layout

Layout happens automatically when needed:

1. [View.SetNeedsLayout](~/api/Terminal.Gui.ViewBase.View.yml) marks View as needing layout
2. [View.Layout](~/api/Terminal.Gui.ViewBase.View.yml) calculates position and size
3. `LayoutStarted` event is raised
4. Frame and Viewport are calculated based on X, Y, Width, Height
5. SubViews are laid out
6. `LayoutComplete` event is raised

### 4. Drawing

Drawing happens automatically when needed:

1. [View.SetNeedsDraw](~/api/Terminal.Gui.ViewBase.View.yml) marks View as needing redraw
2. [View.Draw](~/api/Terminal.Gui.ViewBase.View.yml) renders the View
3. `DrawingContent` event is raised
4. [View.OnDrawingContent](~/api/Terminal.Gui.ViewBase.View.yml) is called (override to draw custom content)
5. `DrawingContentComplete` event is raised
6. Adornments are drawn
7. SubViews are drawn

### 5. Input Processing

Input is processed in this order:

1. **Keyboard**: Key → KeyBindings → Command → Command Handlers → Events
2. **Mouse**: MouseEvent → MouseBindings → Command → Command Handlers → Events

### 6. Disposal

```csharp
view.Dispose();
```

- Raises [View.Disposing](~/api/Terminal.Gui.ViewBase.View.yml) event
- Disposes adornments, scrollbars, SubViews
- Cleans up event handlers and resources

---

## Subsystems

### Commands

See the [Command Deep Dive](command.md) for complete details.

Views use a command pattern for handling input:

```csharp
// Add a command the view supports
view.AddCommand (Command.Accept, () => 
{
    // Handle the Accept command
    return true;
});

// Bind a key to the command
view.KeyBindings.Add (Key.Enter, Command.Accept);

// Bind a mouse action to the command
view.MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);
```

### Input

#### Keyboard

See the [Keyboard Deep Dive](keyboard.md) for complete details.

The keyboard subsystem processes key presses through:

1. [View.KeyDown](~/api/Terminal.Gui.ViewBase.View.yml) event (cancellable)
2. [View.OnKeyDown](~/api/Terminal.Gui.ViewBase.View.yml) virtual method
3. <xref:Terminal.Gui.ViewBase.View.KeyBindings> - Converts keys to commands
4. Command handlers (registered via [View.AddCommand](~/api/Terminal.Gui.ViewBase.View.yml))

#### Mouse

See the [Mouse Deep Dive](mouse.md) for complete details.

The mouse subsystem processes mouse events through:

1. [View.MouseEvent](~/api/Terminal.Gui.ViewBase.View.yml) event (low-level)
2. [View.OnMouseEvent](~/api/Terminal.Gui.ViewBase.View.yml) virtual method
3. [View.MouseEnter](~/api/Terminal.Gui.ViewBase.View.yml) / [View.MouseLeave](~/api/Terminal.Gui.ViewBase.View.yml) events
4. [View.MouseBindings](~/api/Terminal.Gui.ViewBase.View.yml) - Converts mouse actions to commands
5. Command handlers

### Layout

See the [Layout Deep Dive](layout.md) for complete details.

Layout is declarative using [Pos](~/api/Terminal.Gui.ViewBase.Pos.yml) and [Dim](~/api/Terminal.Gui.ViewBase.Dim.yml):

```csharp
var label = new Label { Text = "Name:" };
var textField = new TextField 
{ 
    X = Pos.Right(label) + 1,
    Y = Pos.Top(label),
    Width = Dim.Fill()
};
```

The layout system automatically:
- Calculates Frame based on X, Y, Width, Height
- Handles Adornment thickness
- Calculates Viewport
- Lays out SubViews recursively

### Drawing

See the [Drawing Deep Dive](drawing.md) for complete details.

Views draw themselves using viewport-relative coordinates:

```csharp
protected override bool OnDrawingContent()
{
    // Draw at viewport coordinates (0,0)
    Move(0, 0);
    SetAttribute(new Attribute(Color.White, Color.Blue));
    AddStr("Hello, Terminal.Gui!");
    
    return true;
}
```

Key drawing concepts:
- [LineCanvas](~/api/Terminal.Gui.Drawing.LineCanvas.yml) - For drawing lines with auto-joining
- [Attribute](~/api/Terminal.Gui.Drawing.Attribute.yml) - Color and text style
- [TextStyle](~/api/Terminal.Gui.Drawing.TextStyle.yml) - Bold, Italic, Underline, etc.
- [Gradient](~/api/Terminal.Gui.Drawing.Gradient.yml) / [GradientFill](~/api/Terminal.Gui.Drawing.GradientFill.yml) - Color gradients

### Navigation

See the [Navigation Deep Dive](navigation.md) for complete details.

Navigation controls keyboard focus movement:

- <xref:Terminal.Gui.ViewBase.View.CanFocus> - Whether View can receive focus
- <xref:Terminal.Gui.ViewBase.View.TabStop> - [TabBehavior](~/api/Terminal.Gui.ViewBase.TabBehavior.yml) (NoStop, TabStop, TabGroup)
- View.TabIndex - Tab order within SuperView
- [View.SetFocus](~/api/Terminal.Gui.ViewBase.View.yml) - Requests focus
- [View.AdvanceFocus](~/api/Terminal.Gui.ViewBase.View.yml) - Moves focus to next/previous View

### Scrolling

See the [Scrolling Deep Dive](scrolling.md) for complete details.

Scrolling is built into every View:

```csharp
// Set content size larger than viewport
view.SetContentSize(new Size(100, 100));

// Scroll the content
view.Viewport = view.Viewport with { Location = new Point(10, 10) };

// Or use helper methods
view.ScrollVertical(5);
view.ScrollHorizontal(3);

// Enable scrollbars
view.VerticalScrollBar.Visible = true;
view.HorizontalScrollBar.Visible = true;
```

---

## Common View Patterns

### Creating a Custom View

```csharp
public class MyCustomView : View
{
    public MyCustomView()
    {
        // Set up default size
        Width = Dim.Auto();
        Height = Dim.Auto();
        
        // Can receive focus
        CanFocus = true;
        
        // Add supported commands
        AddCommand(Command.Accept, HandleAccept);
        
        // Configure key bindings
        KeyBindings.Add(Key.Enter, Command.Accept);
    }
    
    protected override bool OnDrawingContent()
    {
        // Draw custom content using viewport coordinates
        Move(0, 0);
        SetAttributeForRole(VisualRole.Normal);
        AddStr("My custom content");
        
        return true; // Handled
    }
    
    private bool HandleAccept()
    {
        // Handle the Accept command
        // Raise events, update state, etc.
        return true; // Handled
    }
}
```

### Adding SubViews

```csharp
var container = new View
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var LeftButton = new Button { Text = "OK", X = 2, Y = 2 };
var MiddleButton = new Button { Text = "Cancel", X = Pos.Right(LeftButton) + 2, Y = 2 };

container.Add(LeftButton, MiddleButton);
```

### Using Adornments

```csharp
var view = new View
{
    BorderStyle = LineStyle.Double,
    Title = "My View"
};

// Configure border
view.Border.Thickness = new Thickness(1);
view.Border.Settings = BorderSettings.Title;

// Add padding
view.Padding.Thickness = new Thickness(1);

// Add margin
view.Margin.Thickness = new Thickness(2);
```

### Implementing Scrolling

```csharp
var view = new View
{
    Width = 40,
    Height = 20
};

// Set content larger than viewport
view.SetContentSize(new Size(100, 100));

// Enable scrollbars with auto-show
view.VerticalScrollBar.AutoShow = true;
view.HorizontalScrollBar.AutoShow = true;

// Add key bindings for scrolling
view.KeyBindings.Add(Key.CursorUp, Command.ScrollUp);
view.KeyBindings.Add(Key.CursorDown, Command.ScrollDown);
view.KeyBindings.Add(Key.CursorLeft, Command.ScrollLeft);
view.KeyBindings.Add(Key.CursorRight, Command.ScrollRight);

// Add command handlers
view.AddCommand(Command.ScrollUp, () => { view.ScrollVertical(-1); return true; });
view.AddCommand(Command.ScrollDown, () => { view.ScrollVertical(1); return true; });
```

---

## Runnable Views

Views can implement [IRunnable](~/api/Terminal.Gui.App.IRunnable.yml) to run as independent, blocking sessions with typed results. This decouples runnability from inheritance, allowing any View to participate in session management.

### IRunnable Architecture

The **IRunnable** pattern provides:

- **Interface-Based**: Implement `IRunnable<TResult>` instead of inheriting from `Runnable`
- **Type-Safe Results**: Generic `TResult` parameter for compile-time type safety
- **Fluent API**: Chain `Init()`, `Run()`, and `Shutdown()` for concise code
- **Automatic Disposal**: Framework manages lifecycle of created runnables
- **CWP Lifecycle Events**: `IsRunningChanging/Changed`, `IsModalChanging/Changed`

### Creating a Runnable View

Derive from [Runnable<TResult>](~/api/Terminal.Gui.Views.Runnable-1.yml) or implement [IRunnable<TResult>](~/api/Terminal.Gui.App.IRunnable-1.yml):

```csharp
public class ColorPickerDialog : Runnable<Color?>
{
    private ColorPicker16 _colorPicker;
    
    public ColorPickerDialog()
    {
        Title = "Select a Color";
        
        _colorPicker = new ColorPicker16 { X = Pos.Center(), Y = 2 };
        
        var okButton = new Button { Text = "OK", IsDefault = true };
        okButton.Accepting += (s, e) => {
            Result = _colorPicker.SelectedColor;
            Application.RequestStop();
        };
        
        Add(_colorPicker, okButton);
    }
}
```

### Running with Fluent API

The fluent API enables elegant, concise code with automatic disposal:

```csharp
// Framework creates, runs, and disposes the runnable automatically
Color? result = Application.Create()
                           .Init()
                           .Run<ColorPickerDialog>()
                           .Shutdown() as Color?;

if (result is { })
{
    Console.WriteLine($"Activated: {result}");
}
```

### Running with Explicit Control

For more control over the lifecycle:

```csharp
var app = Application.Create();
app.Init();

var dialog = new ColorPickerDialog();
app.Run(dialog);

// Extract result after Run returns
Color? result = dialog.Result;

// Caller is responsible for disposal
dialog.Dispose();

app.Shutdown();
```

### Disposal Semantics

**"Whoever creates it, owns it":**

- `Run<TRunnable>()`: Framework creates → Framework disposes (in `Shutdown()`)
- `Run(IRunnable)`: Caller creates → Caller disposes

### Result Extraction

Extract the result in `OnIsRunningChanging` when stopping:

```csharp
protected override bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning)
{
    if (!newIsRunning)  // Stopping - extract result before disposal
    {
        Result = _colorPicker.SelectedColor;
        
        // Optionally cancel stop (e.g., prompt to save)
        if (HasUnsavedChanges())
        {
            return true;  // Cancel stop
        }
    }
    
    return base.OnIsRunningChanging(oldIsRunning, newIsRunning);
}
```

### Lifecycle Properties

- **`IsRunning`** - True when on the `RunnableSessionStack`
- **`IsModal`** - True when at the top of the stack (receiving all input)
- **`Result`** - The typed result value (set before stopping)

### Lifecycle Events (CWP-Compliant)

- **`IsRunningChanging`** - Cancellable event before added/removed from stack
- **`IsRunningChanged`** - Non-cancellable event after stack change
- **`IsModalChanged`** - Non-cancellable event after modal state change

---

## Modal Views (Legacy)

Views can run modally (exclusively capturing all input until closed). See [Runnable](~/api/Terminal.Gui.Views.Runnable.yml) for the legacy pattern.

**Note:** New code should use `IRunnable<TResult>` pattern (see above) for better type safety and lifecycle management.

### Running a View Modally (Legacy)

```csharp
var dialog = new Dialog
{
    Title = "Confirmation",
    Width = Dim.Percent(50),
    Height = Dim.Percent(50)
};

// Add content...
var label = new Label { Text = "Are you sure?", X = Pos.Center(), Y = 1 };
dialog.Add(label);

// Run modally - blocks until closed
Application.Run(dialog);

// Dialog has been closed
dialog.Dispose();
```

### Modal View Types (Legacy)

- **[Runnable](~/api/Terminal.Gui.Views.Runnable.yml)** - Base class for modal views, can fill entire screen
- **[Window](~/api/Terminal.Gui.Views.Window.yml)** - Overlapped container with border and title
- **[Dialog](~/api/Terminal.Gui.Views.Dialog.yml)** - Modal Window, centered with button support
- **[Wizard](~/api/Terminal.Gui.Views.Wizard.yml)** - Multi-step modal dialog

### Dialog Example (Legacy)

[Dialogs](~/api/Terminal.Gui.Views.Dialog.yml) are Modal [Windows](~/api/Terminal.Gui.Views.Window.yml) centered on screen:

```csharp
bool okPressed = false;
var ok = new Button { Text = "Ok" };
ok.Accepting += (s, e) => { okPressed = true; Application.RequestStop(); };

var cancel = new Button { Text = "Cancel" };
cancel.Accepting += (s, e) => Application.RequestStop();

var dialog = new Dialog 
{ 
    Title = "Quit",
    Width = 50,
    Height = 10
};
dialog.Add(new Label { Text = "Are you sure you want to quit?", X = Pos.Center(), Y = 2 });
dialog.AddButton(ok);
dialog.AddButton(cancel);

Application.Run(dialog);

if (okPressed)
{
    // User clicked OK
}
```

Which displays:

```
╔═ Quit ═══════════════════════════════════════════╗
║                                                  ║
║          Are you sure you want to quit?         ║
║                                                  ║
║                                                  ║
║                                                  ║
║                [ Ok ]  [ Cancel ]                ║
╚══════════════════════════════════════════════════╝
```

### Wizard Example

[Wizards](~/api/Terminal.Gui.Views.Wizard.yml) let users step through multiple pages:

```csharp
var wizard = new Wizard { Title = "Setup Wizard" };

var step1 = new WizardStep { Title = "Welcome" };
step1.Add(new Label { Text = "Welcome to the wizard!", X = 1, Y = 1 });

var step2 = new WizardStep { Title = "Configuration" };
step2.Add(new TextField { X = 1, Y = 1, Width = 30 });

wizard.AddStep(step1);
wizard.AddStep(step2);

Application.Run(wizard);
```

---

## Advanced Topics

### View Diagnostics

<xref:Terminal.Gui.ViewBase.View.Diagnostics> - [ViewDiagnosticFlags](~/api/Terminal.Gui.ViewBase.ViewDiagnosticFlags.yml) for debugging:

- `Ruler` - Shows a ruler around the View
- `DrawIndicator` - Shows an animated indicator when drawing
- `FramePadding` - Highlights the Frame with color

### View States

- <xref:Terminal.Gui.ViewBase.View.Enabled> - Whether the View is enabled
- <xref:Terminal.Gui.ViewBase.View.Visible> - Whether the View is visible
- <xref:Terminal.Gui.ViewBase.View.CanFocus> - Whether the View can receive focus
- <xref:Terminal.Gui.ViewBase.View.HasFocus> - Whether the View currently has focus

### Shadow Effects

<xref:Terminal.Gui.ViewBase.View.ShadowStyle> - [ShadowStyle](~/api/Terminal.Gui.ViewBase.ShadowStyle.yml) for drop shadows:

```csharp
view.ShadowStyle = ShadowStyle.Transparent;
```

---

## See Also

- **[Application Deep Dive](application.md)** - Instance-based application architecture
- **[Views Overview](views.md)** - Complete list of all built-in Views
- **[Layout Deep Dive](layout.md)** - Detailed layout system documentation
- **[Drawing Deep Dive](drawing.md)** - Drawing system and color management
- **[Keyboard Deep Dive](keyboard.md)** - Keyboard input handling
- **[Mouse Deep Dive](mouse.md)** - Mouse input handling
- **[Navigation Deep Dive](navigation.md)** - Focus and navigation system
- **[Scrolling Deep Dive](scrolling.md)** - Scrolling and viewport management
- **[Command Deep Dive](command.md)** - Command pattern and bindings
- **[Arrangement Deep Dive](arrangement.md)** - Movable and resizable views
- **[Configuration Deep Dive](config.md)** - Configuration and persistence
- **[Scheme Deep Dive](scheme.md)** - Color theming
