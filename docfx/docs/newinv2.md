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

See the [Drawing Deep Dive](drawing.md) for complete details on the color system.

- **Implementation**: v2 introduces 24-bit color support by extending the [Attribute](~/api/Terminal.Gui.Drawing.Attribute.yml) class to handle RGB values, with fallback to 16-color mode for older terminals. This is evident in the [IConsoleDriver](~/api/Terminal.Gui.Drivers.IConsoleDriver.yml) implementations, which now map colors to the appropriate terminal escape sequences.
- **Impact**: Developers can now use a full spectrum of colors without manual palette management, as seen in v1. The [Color](~/api/Terminal.Gui.Drawing.Color.yml) struct in v2 supports direct RGB input, and drivers handle the translation to terminal capabilities via [IConsoleDriver.SupportsTrueColor](~/api/Terminal.Gui.Drivers.IConsoleDriver.yml#Terminal_Gui_Drivers_IConsoleDriver_SupportsTrueColor).
- **Usage**: See the [ColorPicker](~/api/Terminal.Gui.Views.ColorPicker.yml) view for an example of how TrueColor is leveraged to provide a rich color selection UI.

### Enhanced Borders and Padding (Adornments)

See the [Layout Deep Dive](layout.md) for complete details on the adornments system.

- **Implementation**: v2 introduces a new [Adornment](~/api/Terminal.Gui.ViewBase.Adornment.yml) class hierarchy, with [Margin](~/api/Terminal.Gui.ViewBase.Margin.yml), [Border](~/api/Terminal.Gui.ViewBase.Border.yml), and [Padding](~/api/Terminal.Gui.ViewBase.Padding.yml) as distinct view-like entities that wrap content. This is a significant departure from v1, where borders were often hardcoded or required custom drawing.
- **Code Change**: In v1, [View](~/api/Terminal.Gui.ViewBase.View.yml) had rudimentary border support via properties like `BorderStyle`. In v2, [View](~/api/Terminal.Gui.ViewBase.View.yml) has a [View.Border](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Border) property of type [Border](~/api/Terminal.Gui.ViewBase.Border.yml), which is itself a configurable entity with properties like [Thickness](~/api/Terminal.Gui.Drawing.Thickness.yml), [Border.LineStyle](~/api/Terminal.Gui.ViewBase.Border.yml#Terminal_Gui_ViewBase_Border_LineStyle), and [Border.Settings](~/api/Terminal.Gui.ViewBase.Border.yml#Terminal_Gui_ViewBase_Border_Settings).
- **Impact**: This allows for consistent border rendering across all views and simplifies custom view development by providing a reusable adornment framework.

### User Configurable Color Themes and Text Styles

See the [Configuration Deep Dive](config.md) and [Scheme Deep Dive](scheme.md) for complete details.

- **Implementation**: v2 adds a [ConfigurationManager](~/api/Terminal.Gui.Configuration.ConfigurationManager.yml) that supports loading and saving color schemes from configuration files. Themes are applied via [Scheme](~/api/Terminal.Gui.Drawing.Scheme.yml) objects, which can be customized per view or globally. Each [Attribute](~/api/Terminal.Gui.Drawing.Attribute.yml) in a [Scheme](~/api/Terminal.Gui.Drawing.Scheme.yml) now includes a [TextStyle](~/api/Terminal.Gui.Drawing.TextStyle.yml) property supporting Bold, Italic, Underline, Strikethrough, Blink, Reverse, and Faint text styles.
- **Impact**: Unlike v1, where color schemes were static or required manual override, v2 enables end-users to personalize not just colors but also text styling (bold, italic, underline, etc.) without code changes, significantly enhancing accessibility and user preference support.

### Enhanced Unicode/Wide Character Support
- **Implementation**: v2 improves Unicode handling by correctly managing wide characters in text rendering and input processing. The [TextFormatter](~/api/Terminal.Gui.Text.TextFormatter.yml) class now accounts for Unicode width in layout calculations.
- **Impact**: This fixes v1 issues where wide characters (e.g., CJK scripts) could break layout or input handling, making Terminal.Gui v2 suitable for international applications.

### LineCanvas

See the [Drawing Deep Dive](drawing.md) for complete details on LineCanvas and the drawing system.

- **Implementation**: A new [LineCanvas](~/api/Terminal.Gui.Drawing.LineCanvas.yml) class provides a drawing API for creating lines and shapes using box-drawing characters. It includes logic for auto-joining lines at intersections, selecting appropriate glyphs dynamically.
- **Code Example**: In v2, [LineCanvas](~/api/Terminal.Gui.Drawing.LineCanvas.yml) is used internally by views like [Border](~/api/Terminal.Gui.ViewBase.Border.yml) and [Line](~/api/Terminal.Gui.Views.Line.yml) to draw clean, connected lines, a feature absent in v1.
- **Impact**: Developers can create complex diagrams or UI elements with minimal effort, improving the visual fidelity of terminal applications.

## Simplified API - Under the Hood

### API Consistency and Reduction
- **Change**: v2 revisits every public API, consolidating redundant methods and properties. For example, v1 had multiple focus-related methods scattered across [View](~/api/Terminal.Gui.ViewBase.View.yml) and [Application](~/api/Terminal.Gui.App.Application.yml); v2 centralizes these in [ApplicationNavigation](~/api/Terminal.Gui.App.ApplicationNavigation.yml).
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

See the [Layout Deep Dive](layout.md) and [View Deep Dive](View.md) for complete details.

- **Technical Detail**: Adornments are implemented as nested views that surround the content area, each with its own drawing and layout logic. [Border](~/api/Terminal.Gui.ViewBase.Border.yml) supports multiple [LineStyle](~/api/Terminal.Gui.Drawing.LineStyle.yml) options (Single, Double, Heavy, Rounded, Dashed, Dotted) with automatic line intersection handling via [LineCanvas](~/api/Terminal.Gui.Drawing.LineCanvas.yml).
- **Code Change**: In v2, [View](~/api/Terminal.Gui.ViewBase.View.yml) has properties [View.Margin](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Margin), [View.Border](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Border), and [View.Padding](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Padding), each configurable independently, unlike v1's limited border support.
- **Impact**: This modular approach allows for reusable UI elements and simplifies creating visually consistent applications.

### Built-in Scrolling/Virtual Content Area

See the [Scrolling Deep Dive](scrolling.md) and [Layout Deep Dive](layout.md) for complete details.

- **v1 Issue**: Scrolling required using `ScrollView` or manual offset management, which was error-prone.
- **v2 Solution**: Every [View](~/api/Terminal.Gui.ViewBase.View.yml) in v2 has a [View.Viewport](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Viewport) rectangle representing the visible portion of a potentially larger content area defined by [View.GetContentSize](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_GetContentSize). Changing `Viewport.Location` scrolls the content.
- **Code Example**: In v2, [TextView](~/api/Terminal.Gui.Views.TextView.yml) uses this to handle large text buffers without additional wrapper views.
- **Impact**: Simplifies implementing scrollable content and reduces the need for specialized container views.

### Improved ScrollBar
- **Change**: v2 replaces `ScrollBarView` with [ScrollBar](~/api/Terminal.Gui.Views.ScrollBar.yml), a cleaner implementation integrated with the built-in scrolling system. [View.VerticalScrollBar](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_VerticalScrollBar) and [View.HorizontalScrollBar](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_HorizontalScrollBar) properties enable scroll bars with minimal code.
- **Impact**: Developers can add scroll bars to any view without managing separate view hierarchies, a significant usability improvement over v1.

### DimAuto, PosAnchorEnd, and PosAlign

See the [Layout Deep Dive](layout.md) and [DimAuto Deep Dive](dimauto.md) for complete details.

- **[Dim.Auto](~/api/Terminal.Gui.Dim.yml#Terminal_Gui_Dim_Auto_Terminal_Gui_DimAutoStyle_Terminal_Gui_Dim_Terminal_Gui_Dim_)**: Automatically sizes views based on content or subviews, reducing manual layout calculations.
- **[Pos.AnchorEnd](~/api/Terminal.Gui.Pos.yml#Terminal_Gui_Pos_AnchorEnd_System_Int32_)**: Allows anchoring to the right or bottom of a superview, enabling flexible layouts not easily achievable in v1.
- **[Pos.Align](~/api/Terminal.Gui.Pos.yml)**: Provides alignment options (left, center, right) for multiple views, streamlining UI design.
- **Impact**: These features reduce boilerplate layout code and support responsive designs in terminal constraints.

### View Arrangement

See the [Arrangement Deep Dive](arrangement.md) for complete details.

- **Technical Detail**: The [View.Arrangement](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Arrangement) property supports flags like [ViewArrangement.Movable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml), [ViewArrangement.Resizable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml), and [ViewArrangement.Overlapped](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml), enabling dynamic UI interactions via keyboard and mouse.
- **Code Example**: [Window](~/api/Terminal.Gui.Views.Window.yml) in v2 uses [View.Arrangement](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Arrangement) to allow dragging and resizing, a feature requiring custom logic in v1.
- **Impact**: Developers can create desktop-like experiences in the terminal with minimal effort.

### Keyboard Navigation Overhaul

See the [Navigation Deep Dive](navigation.md) for complete details on the navigation system.

- **v1 Issue**: Navigation was inconsistent, with coupled concepts like [View.CanFocus](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_CanFocus) and `TabStop` leading to unpredictable focus behavior.
- **v2 Solution**: v2 decouples these concepts, introduces [TabBehavior](~/api/Terminal.Gui.Input.TabBehavior.yml) enum for clearer intent (`TabStop`, `TabGroup`, `NoStop`), and centralizes navigation logic in [ApplicationNavigation](~/api/Terminal.Gui.App.ApplicationNavigation.yml).
- **Impact**: Ensures accessibility by guaranteeing keyboard access to all focusable elements, with unit tests enforcing navigation keys on built-in views.

### Sizable/Movable Views
- **Implementation**: Any view can be made resizable or movable by setting [View.Arrangement](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_Arrangement) flags, with built-in mouse and keyboard handlers for interaction.
- **Impact**: Enhances user experience by allowing runtime UI customization, a feature limited to specific views like [Window](~/api/Terminal.Gui.Views.Window.yml) in v1.

## New and Improved Built-in Views - Detailed Analysis

See the [Views Overview](views.md) for a complete catalog of all built-in views.

### New Views

v2 introduces many new View subclasses that were not present in v1:

- **Bar**: A foundational view for horizontal or vertical layouts of `Shortcut` or other items, used in `StatusBar`, `MenuBarv2`, and `PopoverMenu`.
- **CharMap**: A scrollable, searchable Unicode character map with support for the Unicode Character Database (UCD) API, enabling users to browse and select from all Unicode codepoints with detailed character information. See [Character Map Deep Dive](CharacterMap.md).
- **ColorPicker**: Leverages TrueColor for a comprehensive color selection experience, supporting multiple color models (HSV, RGB, HSL, Grayscale) with interactive color bars.
- **DatePicker**: Provides a calendar-based date selection UI with month/year navigation, leveraging v2's improved drawing and navigation systems.
- **FlagSelector**: Enables selection of non-mutually-exclusive flags with checkbox-based UI, supporting both dictionary-based and enum-based flag definitions.
- **GraphView**: Displays graphs (bar charts, scatter plots, line graphs) with flexible axes, labels, scaling, scrolling, and annotations - bringing data visualization to the terminal.
- **Line**: Draws single horizontal or vertical lines using the `LineCanvas` system with automatic intersection handling and multiple line styles (Single, Double, Heavy, Rounded, Dashed, Dotted).
- **Menuv2 System** (MenuBarv2, PopoverMenu): A completely redesigned menu system built on the `Bar` infrastructure, providing a more flexible and visually appealing menu experience.
- **NumericUpDown<T>**: Type-safe numeric input with increment/decrement buttons, supporting `int`, `long`, `float`, `double`, and `decimal` types.
- **OptionSelector**: Displays a list of mutually-exclusive options with checkbox-style UI (radio button equivalent), supporting both horizontal and vertical orientations.
- **Shortcut**: An opinionated view for displaying commands with key bindings, simplifying status bar and toolbar creation with consistent visual presentation.
- **Slider**: A sophisticated control for range selection with multiple styles (horizontal/vertical bars, indicators), multiple options per slider, configurable legends, and event-driven value changes.
- **SpinnerView**: Displays animated spinner glyphs to indicate progress or activity, with multiple built-in styles (Line, Dots, Bounce, etc.) and support for auto-spin or manual animation control.

### Improved Views

Many existing views from v1 have been significantly enhanced in v2:

- **FileDialog** (OpenDialog, SaveDialog): Completely modernized with `TreeView` for hierarchical file navigation, Unicode glyphs for icons, search functionality, and history tracking - far surpassing v1's basic file dialogs.
- **ScrollBar**: Replaces v1's `ScrollBarView` with a cleaner implementation featuring automatic show/hide, proportional sizing with `ScrollSlider`, and seamless integration with View's built-in scrolling system.
- **StatusBar**: Rebuilt on the `Bar` infrastructure, providing more flexible item management, automatic sizing, and better visual presentation.
- **TableView**: Massively enhanced with support for generic collections (via `IEnumerableTableSource`), checkboxes with `CheckBoxTableSourceWrapper`, tree structures via `TreeTableSource`, custom cell rendering, and significantly improved performance. See [TableView Deep Dive](tableview.md).
- **ScrollView**: Deprecated in favor of View's built-in scrolling capabilities, eliminating the need for wrapper views and simplifying scrollable content implementation.

## Beauty - Visual Enhancements

### Borders

See the [Drawing Deep Dive](drawing.md) for complete details on borders and LineCanvas.

- **Implementation**: Uses the [Border](~/api/Terminal.Gui.ViewBase.Border.yml) adornment with [LineStyle](~/api/Terminal.Gui.Drawing.LineStyle.yml) options and [LineCanvas](~/api/Terminal.Gui.Drawing.LineCanvas.yml) for automatic line intersection handling.
- **Impact**: Adds visual polish to UI elements, making applications feel more refined compared to v1's basic borders.

### Gradient

See the [Drawing Deep Dive](drawing.md) for complete details on gradients and fills.

- **Implementation**: [Gradient](~/api/Terminal.Gui.Drawing.Gradient.yml) and [GradientFill](~/api/Terminal.Gui.Drawing.GradientFill.yml) APIs allow rendering color transitions across view elements, using TrueColor for smooth effects.
- **Impact**: Enables modern-looking UI elements like gradient borders or backgrounds, not possible in v1 without custom drawing.

## Configuration Manager - Persistence and Customization

See the [Configuration Deep Dive](config.md) for complete details on the configuration system.

- **Technical Detail**: [ConfigurationManager](~/api/Terminal.Gui.Configuration.ConfigurationManager.yml) in v2 uses JSON to persist settings like themes, key bindings, and view properties to disk via [SettingsScope](~/api/Terminal.Gui.Configuration.SettingsScope.yml) and [ConfigLocations](~/api/Terminal.Gui.Configuration.ConfigLocations.yml).
- **Code Change**: Unlike v1, where settings were ephemeral or hardcoded, v2 provides a centralized system for loading/saving configurations using the [ConfigurationManagerAttribute](~/api/Terminal.Gui.Configuration.ConfigurationManagerAttribute.yml).
- **Impact**: Allows for user-specific customizations and library-wide settings without recompilation, enhancing flexibility.

## Logging & Metrics - Debugging and Performance

See the [Logging Deep Dive](logging.md) for complete details on the logging and metrics system.

- **Implementation**: v2 introduces a multi-level logging system via [Logging](~/api/Terminal.Gui.App.Logging.yml) for internal operations (e.g., rendering, input handling) using Microsoft.Extensions.Logging.ILogger, and metrics for performance tracking via [Logging.Meter](~/api/Terminal.Gui.App.Logging.yml#Terminal_Gui_App_Logging_Meter) (e.g., frame rate, redraw times, iteration timing).
- **Impact**: Developers can diagnose issues like slow redraws or terminal compatibility problems using standard .NET logging frameworks (Serilog, NLog, etc.) and metrics tools (dotnet-counters), a capability absent in v1, reducing guesswork in debugging.

## Sixel Image Support - Graphics in Terminal
- **Technical Detail**: v2 supports the Sixel protocol for rendering images and animations directly in compatible terminals (e.g., Windows Terminal, xterm) via [SixelEncoder](~/api/Terminal.Gui.Drawing.SixelEncoder.yml).
- **Code Change**: New rendering logic in console drivers detects terminal support via [SixelSupportDetector](~/api/Terminal.Gui.Drawing.SixelSupportDetector.yml) and handles Sixel data transmission through [SixelToRender](~/api/Terminal.Gui.Drawing.SixelToRender.yml).
- **Impact**: Brings graphical capabilities to terminal applications, far beyond v1's text-only rendering, opening up new use cases like image previews.

## Updated Keyboard API - Comprehensive Input Handling

See the [Keyboard Deep Dive](keyboard.md) and [Command Deep Dive](command.md) for complete details.

### Key Class
- **Change**: Replaces v1's `KeyEvent` struct with a [Key](~/api/Terminal.Gui.Input.Key.yml) class, providing a high-level abstraction over raw key codes with properties for modifiers and key type.
- **Impact**: Simplifies keyboard handling by abstracting platform differences, making code more portable and readable.

### Key Bindings
- **Implementation**: v2 introduces a binding system mapping keys to [Command](~/api/Terminal.Gui.Input.Command.yml) enums via [View.KeyBindings](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_KeyBindings), with scopes (`Application`, `Focused`, `HotKey`) for priority.
- **Impact**: Replaces v1's ad-hoc key handling with a structured approach, allowing views to declare supported commands via [View.AddCommand](~/api/Terminal.Gui.ViewBase.View.yml) and customize responses easily.
- **Example**: [TextField](~/api/Terminal.Gui.Views.TextField.yml) in v2 binds `Key.Tab` to text insertion rather than focus change, customizable by developers.

### Default Close Key
- **Change**: Changed from `Ctrl+Q` in v1 to `Esc` in v2 for closing apps or [Toplevel](~/api/Terminal.Gui.Views.Toplevel.yml) views, accessible via [Application.QuitKey](~/api/Terminal.Gui.App.Application.yml#Terminal_Gui_App_Application_QuitKey).
- **Impact**: Aligns with common user expectations, improving UX consistency across terminal applications.

## Updated Mouse API - Enhanced Interaction

See the [Mouse Deep Dive](mouse.md) for complete details on mouse handling.

### MouseEventArgs Class
- **Change**: Replaces v1's `MouseEventEventArgs` with [MouseEventArgs](~/api/Terminal.Gui.Input.MouseEventArgs.yml), providing a cleaner structure for mouse data (position, flags via [MouseFlags](~/api/Terminal.Gui.Input.MouseFlags.yml)).
- **Impact**: Simplifies event handling with a more intuitive API, reducing errors in mouse interaction logic.

### Granular Mouse Handling
- **Implementation**: v2 offers specific events for clicks ([View.MouseClick](~/api/Terminal.Gui.ViewBase.View.yml)), double-clicks, and movement, with [MouseFlags](~/api/Terminal.Gui.Input.MouseFlags.yml) for button states.
- **Impact**: Developers can handle complex mouse interactions (e.g., drag-and-drop) more easily than in v1.

### Highlight Event and Continuous Button Presses
- **Highlight**: Views can visually respond to mouse hover or click via the [View.Highlight](~/api/Terminal.Gui.ViewBase.View.yml) event and [View.HighlightStyle](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_HighlightStyle) property.
- **Continuous Presses**: Setting [View.WantContinuousButtonPresses](~/api/Terminal.Gui.ViewBase.View.yml#Terminal_Gui_ViewBase_View_WantContinuousButtonPresses) = true repeats [Command.Accept](~/api/Terminal.Gui.Input.Command.yml) during button hold, useful for sliders or buttons.
- **Impact**: Enhances interactive feedback, making terminal UIs feel more responsive.

## AOT Support - Deployment and Performance
- **Implementation**: v2 ensures compatibility with Ahead-of-Time compilation and single-file applications by avoiding reflection patterns problematic for AOT, using source generators and [SourceGenerationContext](~/api/Terminal.Gui.Configuration.SourceGenerationContext.yml) for JSON serialization.
- **Impact**: Simplifies deployment for environments requiring Native AOT (see `Examples/NativeAot`), a feature not explicitly supported in v1, reducing runtime overhead and enabling faster startup times.

## Conclusion

Terminal.Gui v2 is a transformative update, addressing core limitations of v1 through architectural redesign, performance optimizations, and feature enhancements. From TrueColor and adornments for visual richness to decoupled navigation and modern input APIs for usability, v2 provides a robust foundation for building sophisticated terminal applications. The detailed changes in view management, configuration, and debugging tools empower developers to create more maintainable and user-friendly applications.