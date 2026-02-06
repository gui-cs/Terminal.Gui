---
uid: Terminal.Gui.ViewBase
summary: The `ViewBase` namespace contains the foundational view system and core UI building blocks.
---

@Terminal.Gui.ViewBase.View provides the fundamental view architecture that forms the foundation of all Terminal.Gui user interface elements. This namespace contains the base View class, adornment system, layout primitives, and core view behaviors that enable the rich UI capabilities of Terminal.Gui.

The View system implements the complete view lifecycle, coordinate systems, event handling, focus management, and the innovative adornment system that separates content from visual decoration.

## Key Components

- @Terminal.Gui.ViewBase.View - Base class for all UI elements with complete lifecycle management
- @Terminal.Gui.ViewBase.Adornment - Visual decorations (Margin, Border, Padding) outside content area
- @Terminal.Gui.ViewBase.View.Viewport - Scrollable window into view content with built-in scrolling support
- @Terminal.Gui.ViewBase.ViewArrangement - Flags controlling user interaction (Movable, Resizable, etc.)
- @Terminal.Gui.ViewBase.Pos and @Terminal.Gui.ViewBase.Dim - Flexible positioning and sizing system with relative and absolute options

## View Architecture

- **Hierarchy**: SuperView/SubView relationships with automatic lifecycle management
- **Coordinate Systems**: Multiple coordinate spaces (Frame, Viewport, Content, Screen)
- **Layout Engine**: Automatic positioning and sizing with constraint-based layout
- **Event System**: Comprehensive event handling with cancellation support
- **Focus Management**: Built-in keyboard navigation and focus chain management

## Example Usage

```csharp
// Create a view with a Border adornment and Title
var view = new View()
{
    X = Pos.Center(),
    Y = Pos.Center(),
    Width = Dim.Percent(50),
    Height = Dim.Percent(30),
    Title = "My View",
    BorderStyle = LineStyle.Rounded
};

// Enable user arrangement
view.Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable;

// Add to SuperView 
superView.Add(view);
```

## See Also

- [View Deep Dive](~/docs/View.md) - Comprehensive view system documentation
- [List of Views](~/docs/views.md) - List of all built-in views
- [Layout Deep Dive](~/docs/layout.md) - Layout system and coordinate spaces
- [Arrangement Deep Dive](~/docs/arrangement.md) - User interaction and view arrangement
- [Navigation Deep Dive](~/docs/navigation.md) - Focus management and keyboard navigation
- [Scrolling Deep Dive](~/docs/scrolling.md) - Built-in scrolling capabilities
- [Events Deep Dive](~/docs/events.md) - Event handling patterns 