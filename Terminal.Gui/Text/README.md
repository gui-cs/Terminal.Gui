# Text Formatting in Terminal.Gui

This directory contains text formatting and processing classes for Terminal.Gui.

## Classes

### TextFormatter

The main text formatting class that handles:
- Text alignment (horizontal and vertical)
- Text direction support (left-to-right, right-to-left, top-to-bottom, etc.)
- Word wrapping
- Multi-line text support
- HotKey processing
- Wide character (Unicode) support

**Known Issues**: The current `TextFormatter` implementation has several architectural problems that are planned to be addressed in a future rewrite:

1. **Format/Draw Coupling**: The `Draw()` method does significant formatting work, making `FormatAndGetSize()` unreliable
2. **Performance**: `Format()` is called multiple times during layout operations
3. **Complex Alignment**: Alignment logic is embedded in drawing code instead of using the `Aligner` engine
4. **Poor Extensibility**: Adding new features requires modifying the monolithic class
5. **No Interface**: Prevents multiple text formatter implementations

See [TextFormatter Rewrite Issue](https://github.com/gui-cs/Terminal.Gui/issues/3469) for details.

### Other Classes

- `TextDirection`: Enumeration for text direction support
- `StringExtensions`: Extension methods for string processing
- `RuneExtensions`: Extension methods for Unicode Rune processing
- `NerdFonts`: Support for Nerd Fonts icons

## Future Plans

A complete rewrite of `TextFormatter` is planned that will:
- Separate formatting from rendering concerns
- Provide an interface-based architecture for extensibility
- Improve performance with better caching
- Support multiple text formats (HTML, Attributed Text, etc.)
- Use the `Aligner` engine for proper alignment

This is a major architectural change planned for a future release.