# Terminal.Gui Documentation Index

> **IMPORTANT**: Use retrieval-led reasoning. Read full documentation files before making changes.
> Root: `./docfx/`

## Deep Dives (./docfx/docs/)

### Core Architecture
|application.md|IApplication lifecycle, instance-based pattern, SessionStack, Run/Dispose, View.App property|
|View.md|View hierarchy, Frame/Viewport/ContentArea, SuperView/SubView relationships, composition layers|
|drivers.md|IDriver interface, DriverRegistry, platform abstraction, ANSI/Windows/Unix drivers|
|navigation.md|Focus system, TabStop/TabGroup, keyboard nav (Tab/F6), HasFocus, ApplicationNavigation|

### Layout & Arrangement
|layout.md|Pos/Dim system, absolute/relative positioning, layout lifecycle, SetNeedsLayout|
|arrangement.md|ViewArrangement flags, Movable/Resizable/Overlapped, tiled vs overlapped views|
|dimauto.md|Dim.Auto behavior, content-based sizing, DimAutoStyle options|
|scrolling.md|Content scrolling, Viewport vs ContentSize, scroll events|

### Commands & Events
|command.md|Command enum, AddCommand, KeyBindings/MouseBindings, Activate/Accept/HotKey distinction|
|command-diagrams.md|Visual diagrams of command flow and propagation|
|events.md|Event categories, CWP integration, event propagation, binding types|
|cancellable-work-pattern.md|CWP structure: Work→Virtual→Event, OnXxx methods, Raise pattern|

### Input Handling
|keyboard.md|Key class, KeyBindings, key processing order, IKeyboard interface|
|mouse.md|MouseFlags, MouseBindings, mouse events, grab/release|
|input-injection.md|VirtualTimeProvider, InjectKey/InjectMouse, testing with input simulation|

### Visual & Rendering
|drawing.md|Draw lifecycle, Move/AddStr/AddRune, Attribute, LineCanvas|
|scheme.md|Scheme class, VisualRole, color attributes, theming|
|cursor.md|View.Cursor property, CursorVisibility, cursor positioning|
|Popovers.md|Drawing outside viewport, popover architecture, modal behavior|

### UI Components
|views.md|Complete catalog of built-in views with descriptions|
|menus.md|MenuBar, ContextMenu, MenuItem, menu architecture|
|tableview.md|TableView data binding, columns, selection|
|treeview.md|TreeView hierarchical data, nodes, expansion|
|prompt.md|MessageBox, input dialogs, modal prompts|

### Configuration & Advanced
|config.md|ConfigurationManager, themes, persistent settings, JSON config|
|multitasking.md|Background operations, Invoke, threading considerations|
|logging.md|ILogger integration, debug output, performance tracing|
|ansihandling.md|ANSI escape parsing, AnsiResponseParser, terminal compatibility|

### Migration & Reference
|newinv2.md|v2 changes, breaking changes, new features|
|migratingfromv1.md|Migration guide, API changes, patterns to update|
|lexicon.md|Terminology definitions, naming conventions|
|getting-started.md|Quick start, first application, basic patterns|

## API Namespace Specs (./docfx/apispec/)

|namespace-app.md|Application, IApplication, IRunnable, SessionToken|
|namespace-viewbase.md|View, Adornment, Border, Margin, Padding|
|namespace-views.md|Button, Label, TextField, ListView, etc.|
|namespace-input.md|Key, Mouse, Command, ICommandContext, bindings|
|namespace-drawing.md|Attribute, Color, LineCanvas, Cell, drawing primitives|
|namespace-drivers.md|IDriver, DriverRegistry, platform drivers|
|namespace-configuration.md|ConfigurationManager, themes, settings|
|namespace-text.md|Text processing, autocomplete, history|
|namespace-fileservices.md|File dialogs, path handling|

## Shared Definitions (./docfx/includes/)

|events-lexicon.md|Event terminology: Handled, Cancel, propagation|
|navigation-lexicon.md|Focus terms: TabStop, TabGroup, CanFocus|
|layout-lexicon.md|Layout terms: Frame, Viewport, ContentArea|
|drawing-lexicon.md|Drawing terms: Attribute, Glyph, Cell|
|arrangement-lexicon.md|Arrangement terms: Overlapped, Tiled, Modal|
|scrolling-lexicon.md|Scrolling terms: Viewport, scroll offset|
|config-lexicon.md|Config terms: Theme, Settings, Scope|
|view-composition.md|View layer composition diagram|
|scheme-overview.md|Scheme system overview|

## Source Code (./Terminal.Gui/)

### Key Directories
|Application/|IApplication, ApplicationImpl, SessionStack|
|View/|View base class, layout, drawing, focus|
|Views/|All built-in view implementations|
|Input/|Key, Mouse, Command, bindings|
|Drawing/|Attribute, Color, LineCanvas, Cell|
|Drivers/|IDriver implementations, DriverRegistry|
|Configuration/|ConfigurationManager, themes|

### Critical Files
|View/View.cs|Core View class (~4000 lines)|
|View/View.Layout.cs|Layout implementation|
|View/View.Drawing.cs|Drawing implementation|
|View/View.Navigation.cs|Focus and navigation|
|Application/Application.cs|Static Application (obsolete facade)|
|Application/ApplicationImpl.cs|IApplication implementation|
|Input/Command.cs|Command enum and handling|
|Input/Key.cs|Key class and key handling|
