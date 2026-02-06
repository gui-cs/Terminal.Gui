---
uid: Terminal.Gui.Drawing
summary: The `Drawing` namespace provides comprehensive text rendering, color management, and visual styling for Terminal.Gui applications.
---

@Terminal.Gui.Drawing contains the core drawing primitives and visual styling system for Terminal.Gui. This namespace handles everything from basic color and attribute management to complex line drawing and text formatting capabilities.

The drawing system supports both simple and advanced scenarios, including Unicode text rendering, automatic line joining for complex shapes, thickness-based adornments, and comprehensive color schemes with semantic visual roles.

## Key Components

- **Attribute**: Defines visual styling (foreground, background, text style)
- **Color**: Terminal color support including TrueColor and named colors
- **Scheme**: Maps semantic visual roles to concrete attributes
- **LineCanvas**: Advanced line drawing with automatic glyph joining
- **Thickness**: Framework for defining border and adornment widths
- **Glyphs**: Standard set of drawing characters for UI elements

## Example Usage 

```csharp
// Get the attribute for a Focused view
Attribute? focusedAttribute = GetAttributeForRole(VisualRole.Focused);

// Move to 5,5 and output a string with the focused attribute
Move (5,5);
AddStr("Hello");
```
