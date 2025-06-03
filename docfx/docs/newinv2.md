# Terminal.Gui v2

This document provides an in-depth overview of the new features, improvements, and architectural changes in Terminal.Gui v2 compared to v1.

For information on how to port code from v1 to v2, see the [v1 To v2 Migration Guide](migratingfromv1.md).

## Architectural Overhaul and Design Philosophy

Terminal.Gui v2 represents a fundamental rethinking of the library's architecture, driven by the need for better maintainability, performance, and developer experience. The primary design goals in v2 include:

- **Decoupling of Concepts**: In v1, many concepts like focus management, layout, and input handling were tightly coupled, leading to fragile and hard-to-predict behavior. v2 explicitly separates these concerns, resulting in a more modular and testable codebase.
- **Performance Optimization**: v2 reduces overhead in rendering, event handling, and view management by streamlining internal data structures and algorithms.
- **Modern .NET Practices**: The API has been updated to align with contemporary .NET conventions, such as using events with `EventHandler<T>` and leveraging modern C# features like target-typed `new` and file-scoped namespaces.
- **Accessibility and Usability**: v2 places a stronger emphasis on ensuring that terminal applications are accessible, with improved keyboard navigation and visual feedback.

This architectural shift has resulted in the removal of thousands of lines of redundant or overly complex code from v1, replaced with cleaner, more focused implementations.

## Modern Look & Feel - Technical Details

### TrueColor Support
- **Implementation**: v2 introduces 24-bit color support by extending the `Attribute` class to handle RGB values, with fallback to 16-color mode for older terminals. This is evident in the `ConsoleDriver` implementations, which now map colors to the appropriate terminal escape sequences.
- **Impact**: Developers can now use a full spectrum of colors without manual palette management, as seen in v1. The `Color` struct in v2 supports direct RGB input, and drivers handle the translation to terminal capabilities.
- **Usage**: See the `ColorPicker` view for an example of how TrueColor is leveraged to provide a rich color selection UI.

### Enhanced Borders and Padding (Adornments)
- **Implementation**: v2 introduces a new `Adornment` class hierarchy, with `Margin`, `Border`, and `Padding` as distinct view-like entities that wrap content. This is a significant departure from v1, where borders were often hardcoded or required custom drawing.
- **Code Change**: In v1, `View` had rudimentary border support via properties like `BorderStyle`. In v2, `View` has a `Border` property of type `Border`, which is itself a configurable entity with properties like `Thickness` and `Effect3D`.
- **Impact**: This allows for consistent border rendering across all views and simplifies custom view development by providing a reusable adornment framework.

### User Configurable Color Themes
- **Implementation**: v2 adds a `ConfigurationManager` that supports loading and saving color schemes from configuration files. Themes are applied via `ColorScheme` objects, which can be customized per view or globally.
- **Impact**: Unlike v1, where color schemes were static or required manual override, v2 enables end-users to personalize the UI without code changes, enhancing accessibility and user preference support.

### Enhanced Unicode/Wide Character Support
- **Implementation**: v2 improves Unicode handling by correctly managing wide characters in text rendering and input processing. The `TextFormatter` class now accounts for Unicode width in layout calculations.
- **Impact**: This fixes v1 issues where wide characters (e.g., CJK scripts) could break layout or input handling, making Terminal.Gui v2 suitable for international applications.

### LineCanvas
- **Implementation**: A new `LineCanvas` class provides a drawing API for creating lines and shapes using box-drawing characters. It includes logic for auto-joining lines at intersections, selecting appropriate glyphs dynamically.
- **Code Example**: In v2, `LineCanvas` is used internally by views like `Border` to draw clean, connected lines, a feature absent in v1.
- **Impact**: Developers can create complex diagrams or UI elements with minimal effort, improving the visual fidelity of terminal applications.

## Simplified API - Under the Hood

### API Consistency and Reduction
- **Change**: v2 revisits every public API, consolidating redundant methods and properties. For example, v1 had multiple focus-related methods scattered across `View` and `Application`; v2 centralizes these in `ApplicationNavigation`.
- **Impact**: This reduces the learning curve for new developers and minimizes the risk of using deprecated or inconsistent APIs.
- **Example**: The v1 `View.MostFocused` property is replaced by `Application.Navigation.GetFocused()`, reducing traversal overhead and clarifying intent.

### Modern .NET Standards
- **Change**: Events in v2 use `EventHandler<T>` instead of v1's custom delegate types. Methods follow consistent naming (e.g., `OnHasFocusChanged` vs. v1's varied naming).
- **Impact**: Developers familiar with .NET conventions will find v2 more intuitive, and tools like IntelliSense provide better support due to standardized signatures.

### Performance Gains
- **Change**: v2 optimizes rendering by minimizing unnecessary redraws through a smarter `NeedsDisplay` system and reducing object allocations in hot paths like event handling.
- **Impact**: Applications built with v2 will feel snappier, especially in complex UIs with many views or frequent updates, addressing v1 performance bottlenecks.

## View Improvements - Deep Dive

### Deterministic View Lifetime Management
- **v1 Issue**: Lifetime rules for `View` objects were unclear, leading to memory leaks or premature disposal, especially with `Application.Run`.
- **v2 Solution**: v2 defines explicit rules for view disposal and ownership, enforced by unit tests. `Application.Run` now clearly manages the lifecycle of `Toplevel` views, ensuring deterministic cleanup.
- **Impact**: Developers can predict when resources are released, reducing bugs related to dangling references or uninitialized states.

### Adornments Framework
- **Technical Detail**: Adornments are implemented as nested views that surround the content area, each with its own drawing and layout logic. For instance, `Border` can draw 3D effects or custom glyphs.
- **Code Change**: In v2, `View` has properties like `Margin`, `Border`, and `Padding`, each configurable independently, unlike v1's limited border support.
- **Impact**: This modular approach allows for reusable UI elements and simplifies creating visually consistent applications.

### Built-in Scrolling/Virtual Content Area
- **v1 Issue**: Scrolling required using `ScrollView` or manual offset management, which was error-prone.
- **v2 Solution**: Every `View` in v2 has a `Viewport` rectangle representing the visible portion of a potentially larger content area defined by `GetContentSize()`. Changing `Viewport.Location` scrolls the content.
- **Code Example**: In v2, `TextView` uses this to handle large text buffers without additional wrapper views.
- **Impact**: Simplifies implementing scrollable content and reduces the need for specialized container views.

### Improved ScrollBar
- **Change**: v2 replaces `ScrollBarView` with `ScrollBar`, a cleaner implementation integrated with the built-in scrolling system. `VerticalScrollBar` and `HorizontalScrollBar` properties on `View` enable scroll bars with minimal code.
- **Impact**: Developers can add scroll bars to any view without managing separate view hierarchies, a significant usability improvement over v1.

### DimAuto, PosAnchorEnd, and PosAlign
- **DimAuto**: Automatically sizes views based on content or subviews, reducing manual layout calculations.
- **PosAnchorEnd**: Allows anchoring to the right or bottom of a superview, enabling flexible layouts not easily achievable in v1.
- **PosAlign**: Provides alignment options (left, center, right) for multiple views, streamlining UI design.
- **Impact**: These features reduce boilerplate layout code and support responsive designs in terminal constraints.

### View Arrangement
- **Technical Detail**: The `Arrangement` property on `View` supports flags like `Movable`, `Resizable`, and `Overlapped`, enabling dynamic UI interactions via keyboard and mouse.
- **Code Example**: `Window` in v2 uses `Arrangement` to allow dragging and resizing, a feature requiring custom logic in v1.
- **Impact**: Developers can create desktop-like experiences in the terminal with minimal effort.

### Keyboard Navigation Overhaul
- **v1 Issue**: Navigation was inconsistent, with coupled concepts like `CanFocus` and `TabStop` leading to unpredictable focus behavior.
- **v2 Solution**: v2 decouples these concepts, introduces `TabBehavior` enum for clearer intent (`TabStop`, `TabGroup`, `NoStop`), and centralizes navigation logic in `ApplicationNavigation`.
- **Impact**: Ensures accessibility by guaranteeing keyboard access to all focusable elements, with unit tests enforcing navigation keys on built-in views.

### Sizable/Movable Views
- **Implementation**: Any view can be made resizable or movable by setting `Arrangement` flags, with built-in mouse and keyboard handlers for interaction.
- **Impact**: Enhances user experience by allowing runtime UI customization, a feature limited to specific views like `Window` in v1.

## New and Improved Built-in Views - Detailed Analysis

### New Views
- **DatePicker**: Provides a calendar-based date selection UI, leveraging v2's improved drawing and navigation systems.
- **Slider**: A new control for range selection, using `LineCanvas` for smooth rendering and supporting TrueColor for visual feedback.
- **Shortcut**: An opinionated view for command display with key bindings, simplifying status bar or toolbar creation.
- **Bar**: A foundational view for horizontal or vertical layouts of `Shortcut` or other items, used in `StatusBar`, `MenuBar`, and `PopoverMenu`.
- **FileDialog**: Modernized with a `TreeView` for navigation, icons using Unicode glyphs, and search functionality, far surpassing v1's basic dialog.
- **ColorPicker**: Leverages TrueColor for a comprehensive color selection experience, supporting multiple color models (HSV, RGB, HSL).

### Improved Views
- **ScrollView**: Deprecated in favor of built-in scrolling on `View`, reducing complexity and view hierarchy depth.
- **TableView**: Now supports generic collections, checkboxes, and tree structures, moving beyond v1's `DataTable` limitation, with improved rendering performance.
- **StatusBar**: Rebuilt on `Bar`, providing a more flexible and visually appealing status display.

## Beauty - Visual Enhancements

### Borders
- **Implementation**: Uses the `Border` adornment to render 3D effects or custom styles, configurable per view.
- **Impact**: Adds visual depth to UI elements, making applications feel more polished compared to v1's flat borders.

### Gradient
- **Implementation**: A new `Gradient` API allows rendering color transitions across view elements, using TrueColor for smooth effects.
- **Impact**: Enables modern-looking UI elements like gradient borders or backgrounds, not possible in v1 without custom drawing.

## Configuration Manager - Persistence and Customization
- **Technical Detail**: `ConfigurationManager` in v2 uses JSON or other formats to persist settings like themes, key bindings, and view properties to disk.
- **Code Change**: Unlike v1, where settings were ephemeral or hardcoded, v2 provides a centralized system for loading/saving configurations.
- **Impact**: Allows for user-specific customizations and library-wide settings without recompilation, enhancing flexibility.

## Logging & Metrics - Debugging and Performance
- **Implementation**: v2 introduces a multi-level logging system for internal operations (e.g., rendering, input handling) and metrics for performance tracking (e.g., frame rate, redraw times).
- **Impact**: Developers can diagnose issues like slow redraws or terminal compatibility problems, a capability absent in v1, reducing guesswork in debugging.

## Sixel Image Support - Graphics in Terminal
- **Technical Detail**: v2 supports the Sixel protocol for rendering images and animations directly in compatible terminals (e.g., Windows Terminal, xterm).
- **Code Change**: New rendering logic in console drivers detects terminal support and handles Sixel data transmission.
- **Impact**: Brings graphical capabilities to terminal applications, far beyond v1's text-only rendering, opening up new use cases like image previews.

## Updated Keyboard API - Comprehensive Input Handling

### Key Class
- **Change**: Replaces v1's `KeyEvent` struct with a `Key` class, providing a high-level abstraction over raw key codes with properties for modifiers and key type.
- **Impact**: Simplifies keyboard handling by abstracting platform differences, making code more portable and readable.

### Key Bindings
- **Implementation**: v2 introduces a binding system mapping keys to `Command` enums via `View.KeyBindings`, with scopes (`Application`, `Focused`, `HotKey`) for priority.
- **Impact**: Replaces v1's ad-hoc key handling with a structured approach, allowing views to declare supported commands and customize responses easily.
- **Example**: `TextField` in v2 binds `Key.Tab` to text insertion rather than focus change, customizable by developers.

### Default Close Key
- **Change**: Changed from `Ctrl+Q` in v1 to `Esc` in v2 for closing apps or `Toplevel` views.
- **Impact**: Aligns with common user expectations, improving UX consistency across terminal applications.

## Updated Mouse API - Enhanced Interaction

### MouseEvent Class
- **Change**: Replaces `MouseEventEventArgs` with `MouseEvent`, providing a cleaner structure for mouse data (position, flags).
- **Impact**: Simplifies event handling with a more intuitive API, reducing errors in mouse interaction logic.

### Granular Mouse Handling
- **Implementation**: v2 offers specific events for clicks, double-clicks, and movement, with flags for button states.
- **Impact**: Developers can handle complex mouse interactions (e.g., drag-and-drop) more easily than in v1.

### Highlight Event and Continuous Button Presses
- **Highlight**: Views can visually respond to mouse hover or click via the `Highlight` event.
- **Continuous Presses**: Setting `WantContinuousButtonPresses = true` repeats `Command.Accept` during button hold, useful for sliders or buttons.
- **Impact**: Enhances interactive feedback, making terminal UIs feel more responsive.

## AOT Support - Deployment and Performance
- **Implementation**: v2 ensures compatibility with Ahead-of-Time compilation and single-file applications by avoiding reflection patterns problematic for AOT.
- **Impact**: Simplifies deployment for environments requiring AOT (e.g., .NET Native), a feature not explicitly supported in v1, reducing runtime overhead.

## Conclusion

Terminal.Gui v2 is a transformative update, addressing core limitations of v1 through architectural redesign, performance optimizations, and feature enhancements. From TrueColor and adornments for visual richness to decoupled navigation and modern input APIs for usability, v2 provides a robust foundation for building sophisticated terminal applications. The detailed changes in view management, configuration, and debugging tools empower developers to create more maintainable and user-friendly applications.