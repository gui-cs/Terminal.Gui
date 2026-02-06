---
uid: Terminal.Gui.Text
summary: The `Text` namespace provides advanced text processing, formatting, and Unicode handling capabilities.
---

@Terminal.Gui.Text contains the text processing and formatting system for Terminal.Gui applications. This namespace handles Unicode text rendering, text measurement, formatting with alignment and wrapping, and hot key processing for accessible user interfaces.

The text system supports complex text scenarios including bidirectional text, combining characters, wide characters, and sophisticated formatting options with both horizontal and vertical alignment capabilities.

## Key Components

- **TextFormatter**: Advanced text formatting with alignment and wrapping
- **Rune**: Unicode character representation and processing
- **TextDirection**: Support for left-to-right and right-to-left text
- **TextAlignment**: Horizontal and vertical text positioning

## Text Processing Features

- **Unicode Support**: Full Unicode character set including combining characters
- **Text Formatting**: Word wrapping, alignment, and hot key processing
- **Accessibility**: Hot key support for keyboard navigation

## Example Usage 

```csharp
// Create a text formatter with default settings
var formatter = new TextFormatter();

// Format a string with alignment
var formatted = formatter.Format("Hello, World!", 10, 10, TextAlignment.Center);
```

## Deep Dive

- [Text Formatting](~/docs/drawing.md) - Comprehensive text formatting documentation
