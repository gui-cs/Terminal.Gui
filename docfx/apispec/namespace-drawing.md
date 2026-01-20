---
uid: Terminal.Gui.Drawing
summary: Colors, attributes, text styling, and drawing primitives.
---

The `Drawing` namespace provides visual styling, color management, and drawing primitives for Terminal.Gui.

## Key Types

- **Attribute** - Foreground/background color and text style combination
- **Color** - Terminal colors including TrueColor (24-bit) support
- **Scheme** - Maps semantic visual roles to attributes
- **LineCanvas** - Line drawing with automatic glyph joining
- **Thickness** - Border and spacing dimensions
- **Glyphs** - Standard drawing characters for UI elements

## Color Support

```csharp
// Named colors
Color red = Color.Red;

// TrueColor (24-bit RGB)
Color custom = new (128, 64, 255);

// Create an attribute
Attribute attr = new (Color.White, Color.Blue);
```

## Scheme System

Schemes map semantic roles to visual attributes:

| Role | Purpose |
|------|---------|
| Normal | Default appearance |
| Focus | Focused view |
| HotNormal | Hotkey indicator |
| Disabled | Disabled state |

## See Also

- [Drawing Deep Dive](~/docs/drawing.md)
- [Scheme Deep Dive](~/docs/scheme.md)
