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

---

## Complete API Type Reference

> Generated from `docfx/api/toc.yml` - ~530 types across 12 namespaces

### Terminal.Gui.App (35 types)

| Type | Category | Description |
|------|----------|-------------|
| Application | Class | Static application gateway (obsolete facade) |
| ApplicationMainLoop\<T\> | Class | Main loop implementation for input processing |
| ApplicationModelUsage | Enum | Specifies static vs instance-based model |
| ApplicationNavigation | Class | Manages focus navigation between views |
| ApplicationPopover | Class | Manages popover display and lifecycle |
| CWPEventHelper | Class | Helper for CWP event pattern |
| CWPPropertyHelper | Class | Helper for CWP property pattern |
| CWPWorkflowHelper | Class | Helper for CWP workflow pattern |
| CancelEventArgs\<T\> | Class | Event args with cancellation support |
| Clipboard | Class | System clipboard access |
| ClipboardBase | Class | Base class for clipboard implementations |
| EventArgs\<T\> | Class | Generic event args with value |
| GlobalResources | Class | Global resource management |
| IApplication | Interface | Instance-based application interface |
| IApplicationMainLoop\<T\> | Interface | Main loop interface |
| IClipboard | Interface | Clipboard abstraction |
| IKeyboard | Interface | Keyboard input abstraction |
| IMainLoopCoordinator | Interface | Coordinates main loop execution |
| IMouse | Interface | Mouse input abstraction |
| IMouseGrabHandler | Interface | Handles mouse grab state |
| IPopover | Interface | Popover behavior interface |
| IRunnable | Interface | Runnable view interface |
| IRunnable\<T\> | Interface | Runnable with result type |
| ITimedEvents | Interface | Timer and idle event management |
| LogarithmicTimeout | Class | Logarithmic backoff timeout |
| Logging | Class | Logging configuration |
| NotInitializedException | Class | Thrown when app not initialized |
| PopoverBaseImpl | Class | Base popover implementation |
| ResultEventArgs\<T\> | Class | Event args with result value |
| SessionToken | Class | Represents an application session |
| SessionTokenEventArgs | Class | Session lifecycle events |
| SmoothAcceleratingTimeout | Class | Smooth acceleration timeout |
| TimedEvents | Class | Timer and idle implementation |
| Timeout | Class | Timeout configuration |
| TimeoutEventArgs | Class | Timeout event arguments |
| ValueChangedEventArgs\<T\> | Class | Value changed notification |
| ValueChangingEventArgs\<T\> | Class | Value changing (cancellable) |

### Terminal.Gui.Configuration (15 types)

| Type | Category | Description |
|------|----------|-------------|
| AppSettingsScope | Class | Application settings scope |
| ConfigLocations | Enum | Configuration file locations |
| ConfigProperty | Class | Configuration property descriptor |
| ConfigurationManager | Class | Central configuration management |
| ConfigurationManagerEventArgs | Class | Config change events |
| ConfigurationManagerNotEnabledException | Class | Config not enabled exception |
| ConfigurationPropertyAttribute | Class | Marks config properties |
| DeepCloner | Class | Deep cloning utility |
| KeyJsonConverter | Class | JSON converter for Key type |
| SchemeManager | Class | Color scheme management |
| Scope\<T\> | Class | Generic configuration scope |
| SettingsScope | Class | Settings scope base |
| SourcesManager | Class | Configuration sources |
| ThemeManager | Class | Theme management |
| ThemeScope | Class | Theme configuration scope |

### Terminal.Gui.Drawing (40 types)

| Type | Category | Description |
|------|----------|-------------|
| AnsiColorCode | Enum | ANSI color codes |
| Attribute | Struct | Foreground/background color pair |
| Cell | Struct | Single terminal cell (rune + attribute) |
| CellEventArgs | Struct | Cell-related event args |
| Color | Struct | RGB color representation |
| ColorModel | Enum | Color model (RGB, HSL, etc.) |
| ColorName16 | Enum | 16-color palette names |
| ColorParseException | Class | Color parsing error |
| ColorQuantizer | Class | Reduces colors to palette |
| ColorStrings | Class | Color name/string conversion |
| EuclideanColorDistance | Class | Euclidean color distance metric |
| FillPair | Class | Fill pattern pair |
| Glyphs | Class | Unicode glyph constants |
| Gradient | Class | Color gradient generation |
| GradientDirection | Enum | Gradient direction |
| GradientFill | Class | Gradient fill pattern |
| GraphemeHelper | Class | Grapheme cluster handling |
| IColorDistance | Interface | Color distance metric |
| IColorNameResolver | Interface | Resolves color names |
| ICustomColorFormatter | Interface | Custom color formatting |
| IFill | Interface | Fill pattern interface |
| IPaletteBuilder | Interface | Palette construction |
| LineCanvas | Class | Line/box drawing canvas |
| LineStyle | Enum | Line style (Single, Double, etc.) |
| PopularityPaletteWithThreshold | Class | Popularity-based palette |
| Region | Class | Clipping region |
| RegionOp | Enum | Region operations |
| Scheme | Class | Color scheme (Normal, Focus, etc.) |
| Schemes | Enum | Built-in scheme names |
| SixelEncoder | Class | Sixel graphics encoder |
| SixelSupportDetector | Class | Detects sixel support |
| SixelSupportResult | Class | Sixel detection result |
| SixelToRender | Class | Sixel rendering data |
| SolidFill | Class | Solid color fill |
| StandardColor | Enum | Standard color names |
| StandardColorsNameResolver | Class | Standard color resolver |
| StraightLine | Class | Straight line definition |
| StraightLineExtensions | Class | Line extension methods |
| TextStyle | Enum | Text styling (Bold, Italic, etc.) |
| Thickness | Struct | Border/margin thickness |
| VisualRole | Enum | Visual role for theming |
| VisualRoleEventArgs | Class | Visual role events |

### Terminal.Gui.Drivers (80+ types)

| Type | Category | Description |
|------|----------|-------------|
| AnsiComponentFactory | Class | ANSI component factory |
| AnsiEscapeSequence | Class | ANSI escape sequence |
| AnsiEscapeSequenceRequest | Class | ANSI sequence request |
| AnsiEventRecord | Class | ANSI event record |
| AnsiInput | Class | ANSI input processing |
| AnsiInputProcessor | Class | ANSI input processor |
| AnsiKeyboardEncoder | Class | ANSI keyboard encoder |
| AnsiKeyboardParser | Class | ANSI keyboard parser |
| AnsiKeyboardParserPattern | Class | Keyboard parse pattern |
| AnsiMouseEncoder | Class | ANSI mouse encoder |
| AnsiMouseParser | Class | ANSI mouse parser |
| AnsiOutput | Class | ANSI output handling |
| AnsiPlatform | Enum | ANSI platform type |
| AnsiRequestScheduler | Class | ANSI request scheduler |
| AnsiResponseParserState | Enum | Parser state |
| ComponentFactoryImpl\<T\> | Class | Component factory impl |
| ConsoleInputSource | Class | Console input source |
| ConsoleKeyInfoExtensions | Class | ConsoleKeyInfo extensions |
| ConsoleKeyMapping | Class | Console key mapping |
| CsiCursorPattern | Class | CSI cursor pattern |
| CsiKeyPattern | Class | CSI key pattern |
| Cursor | Class | Cursor state |
| CursorStyle | Enum | Cursor appearance |
| CursorVisibility | Enum | Cursor visibility state |
| Driver | Class | Driver base class |
| DriverRegistry | Class | Registers available drivers |
| DriverRegistry.DriverDescriptor | Class | Driver description |
| DriverRegistry.Names | Class | Driver name constants |
| EscSeqReqStatus | Class | Escape sequence status |
| EscSeqRequests | Class | Escape sequence requests |
| EscSeqUtils | Class | Escape sequence utilities |
| EscSeqUtils.ClearScreenOptions | Enum | Screen clear options |
| FakeClipboard | Class | Fake clipboard for testing |
| IAnsiResponseParser | Interface | ANSI response parsing |
| IComponentFactory | Interface | Component factory |
| IComponentFactory\<T\> | Interface | Generic component factory |
| IDriver | Interface | Terminal driver interface |
| IInputProcessor | Interface | Input processing |
| IInputSource | Interface | Input source |
| IInput\<T\> | Interface | Generic input |
| IKeyConverter\<T\> | Interface | Key conversion |
| IOutput | Interface | Output interface |
| IOutputBuffer | Interface | Output buffering |
| ISizeMonitor | Interface | Size monitoring |
| ITestableInput\<T\> | Interface | Testable input |
| IWindowsInput | Interface | Windows-specific input |
| InputEventRecord | Class | Input event record |
| InputImpl\<T\> | Class | Input implementation |
| InputProcessorImpl\<T\> | Class | Input processor impl |
| KeyCode | Enum | Key code constants |
| KeyboardEventRecord | Class | Keyboard event |
| MouseEventRecord | Class | Mouse event |
| NetComponentFactory | Class | .NET component factory |
| NetInput | Class | .NET input handler |
| NetInputProcessor | Class | .NET input processor |
| NetOutput | Class | .NET output handler |
| OutputBase | Class | Output base class |
| OutputBufferImpl | Class | Output buffer impl |
| PlatformDetection | Class | Platform detection |
| Ss3Pattern | Class | SS3 escape pattern |
| TestInputSource | Class | Test input source |
| UnixComponentFactory | Class | Unix component factory |
| VK | Enum | Virtual key codes |
| WindowsComponentFactory | Class | Windows component factory |
| WindowsConsole | Class | Windows console interop |
| WindowsConsole.* | Various | Windows console structs/enums |

### Terminal.Gui.FileServices (5 types)

| Type | Category | Description |
|------|----------|-------------|
| FileSystemColorProvider | Class | File type coloring |
| FileSystemIconProvider | Class | File type icons |
| FileSystemTreeBuilder | Class | File tree construction |
| IFileOperations | Interface | File operations |
| ISearchMatcher | Interface | Search matching |

### Terminal.Gui.Input (18 types)

| Type | Category | Description |
|------|----------|-------------|
| Command | Enum | Commands (Accept, Cancel, etc.) |
| CommandContext | Struct | Command execution context |
| CommandEventArgs | Class | Command event args |
| GrabMouseEventArgs | Class | Mouse grab events |
| ICommandContext | Interface | Command context |
| IInputBinding | Interface | Input binding |
| InputBinding | Struct | Input binding data |
| InputBindings\<E,B\> | Class | Generic input bindings |
| Key | Class | Keyboard key representation |
| KeyBinding | Struct | Key-to-command binding |
| KeyBindings | Class | Key binding collection |
| KeyChangedEventArgs | Class | Key change events |
| KeystrokeNavigatorEventArgs | Class | Keystroke nav events |
| Mouse | Class | Mouse event data |
| MouseBinding | Struct | Mouse-to-command binding |
| MouseBindings | Class | Mouse binding collection |
| MouseFlags | Enum | Mouse button/state flags |
| MouseFlagsChangedEventArgs | Class | Mouse flags change |

### Terminal.Gui.Resources (1 type)

| Type | Category | Description |
|------|----------|-------------|
| Strings | Class | Localized string resources |

### Terminal.Gui.Testing (8 types)

| Type | Category | Description |
|------|----------|-------------|
| IInputInjector | Interface | Input injection interface |
| InputInjectionEvent | Class | Injection event base |
| InputInjectionExtensions | Class | Injection helpers |
| InputInjectionMode | Enum | Injection mode |
| InputInjectionOptions | Class | Injection options |
| InputInjector | Class | Injects keyboard/mouse input |
| KeyInjectionEvent | Class | Key injection event |
| MouseInjectionEvent | Class | Mouse injection event |

### Terminal.Gui.Text (4 types)

| Type | Category | Description |
|------|----------|-------------|
| RuneExtensions | Class | Rune extension methods |
| StringExtensions | Class | String extension methods |
| TextDirection | Enum | Text direction (LTR, RTL) |
| TextFormatter | Class | Text formatting/wrapping |

### Terminal.Gui.Time (4 types)

| Type | Category | Description |
|------|----------|-------------|
| ITimeProvider | Interface | Time abstraction |
| ITimer | Interface | Timer interface |
| SystemTimeProvider | Class | Real system time |
| VirtualTimeProvider | Class | Virtual time for testing |

### Terminal.Gui.ViewBase (70 types)

| Type | Category | Description |
|------|----------|-------------|
| AddOrSubtract | Enum | Add/subtract operation |
| AddOrSubtractExtensions | Class | AddOrSubtract extensions |
| Adornment | Class | View adornment base |
| AdvanceFocusEventArgs | Class | Focus advance events |
| Aligner | Class | Content alignment |
| Alignment | Enum | Alignment options |
| AlignmentExtensions | Class | Alignment extensions |
| AlignmentModes | Enum | Alignment modes |
| AlignmentModesExtensions | Class | Mode extensions |
| ArrangeButtons | Enum | Button arrangement |
| Border | Class | View border adornment |
| BorderSettings | Enum | Border settings flags |
| BorderSettingsExtensions | Class | Border extensions |
| Dim | Class | Dimension base class |
| DimAbsolute | Class | Absolute dimension |
| DimAuto | Class | Auto-sizing dimension |
| DimAutoStyle | Enum | Auto dimension style |
| DimAutoStyleExtensions | Class | Auto style extensions |
| DimCombine | Class | Combined dimensions |
| DimFill | Class | Fill remaining space |
| DimFunc | Class | Function-based dimension |
| DimPercent | Class | Percentage dimension |
| DimPercentMode | Enum | Percent mode |
| DimPercentModeExtensions | Class | Percent mode extensions |
| DimView | Class | View-based dimension |
| Dimension | Enum | Width/Height dimension |
| DimensionExtensions | Class | Dimension extensions |
| DrawAdornmentsEventArgs | Class | Adornment draw events |
| DrawContext | Class | Drawing context |
| DrawEventArgs | Class | Draw event args |
| HasFocusEventArgs | Class | Focus change events |
| IDesignable | Interface | Designer support |
| IMouseHoldRepeater | Interface | Mouse hold repeat |
| IOrientation | Interface | Orientation support |
| IValue | Interface | Value holder |
| IValue\<T\> | Interface | Generic value holder |
| LayoutEventArgs | Class | Layout event args |
| LayoutException | Class | Layout error |
| Margin | Class | View margin adornment |
| MouseState | Enum | Mouse state |
| NavigationDirection | Enum | Navigation direction |
| Orientation | Enum | Horizontal/Vertical |
| OrientationHelper | Class | Orientation utilities |
| Padding | Class | View padding adornment |
| Pos | Class | Position base class |
| PosAbsolute | Class | Absolute position |
| PosAlign | Class | Alignment-based position |
| PosAnchorEnd | Class | Anchored to end |
| PosCenter | Class | Centered position |
| PosCombine | Class | Combined positions |
| PosFunc | Class | Function-based position |
| PosPercent | Class | Percentage position |
| PosView | Class | View-based position |
| ShadowStyle | Enum | Shadow appearance |
| Side | Enum | Top/Bottom/Left/Right |
| SideExtensions | Class | Side extensions |
| SizeChangedEventArgs | Class | Size change events |
| StackExtensions | Class | Stack extensions |
| SuperViewChangedEventArgs | Class | SuperView change events |
| TabBehavior | Enum | Tab key behavior |
| View | Class | Base view class |
| View.CommandImplementation | Delegate | Command handler delegate |
| ViewArrangement | Enum | Arrangement flags |
| ViewDiagnosticFlags | Enum | Diagnostic flags |
| ViewDiagnosticFlagsExtensions | Class | Diagnostic extensions |
| ViewEventArgs | Class | View event args |
| ViewManipulator | Class | View manipulation |
| ViewportSettingsFlags | Enum | Viewport settings |

### Terminal.Gui.Views (180+ types, excluding 70+ SpinnerStyles)

#### Core Controls
| Type | Category | Description |
|------|----------|-------------|
| Button | Class | Push button |
| CheckBox | Class | Check box with tri-state |
| ComboBox | Class | Drop-down combo box |
| DateField | Class | Date input field |
| DatePicker | Class | Date picker dialog |
| FlagSelector | Class | Flag selection |
| FlagSelector\<T\> | Class | Generic flag selector |
| Label | Class | Text label |
| NumericUpDown | Class | Numeric spinner |
| NumericUpDown\<T\> | Class | Generic numeric spinner |
| OptionSelector | Class | Option selection |
| OptionSelector\<T\> | Class | Generic option selector |
| ProgressBar | Class | Progress indicator |
| ScrollBar | Class | Scroll bar |
| ScrollSlider | Class | Scroll slider |
| Shortcut | Class | Keyboard shortcut display |
| TextField | Class | Single-line text input |
| TextValidateField | Class | Validated text input |
| TextView | Class | Multi-line text editor |
| TimeField | Class | Time input field |

#### Container Views
| Type | Category | Description |
|------|----------|-------------|
| Dialog | Class | Modal dialog |
| Dialog\<T\> | Class | Dialog with result |
| FrameView | Class | Framed container |
| TabView | Class | Tabbed container |
| Window | Class | Top-level window |
| Wizard | Class | Multi-step wizard |
| WizardStep | Class | Wizard step |

#### List & Data Views
| Type | Category | Description |
|------|----------|-------------|
| ListView | Class | List display |
| TableView | Class | Table/grid display |
| TreeView | Class | Hierarchical tree |
| TreeView\<T\> | Class | Generic tree view |

#### Menu System
| Type | Category | Description |
|------|----------|-------------|
| Bar | Class | Generic bar |
| Menu | Class | Menu popup |
| MenuBar | Class | Menu bar |
| MenuBarItem | Class | Menu bar item |
| MenuItem | Class | Menu item |
| PopoverMenu | Class | Context menu |
| StatusBar | Class | Status bar |

#### Specialized Views
| Type | Category | Description |
|------|----------|-------------|
| AttributePicker | Class | Attribute selection |
| CharMap | Class | Character map |
| ColorPicker | Class | Color selection |
| ColorPicker16 | Class | 16-color picker |
| FileDialog | Class | File dialog base |
| GraphView | Class | Graph/chart display |
| HexView | Class | Hex editor |
| Line | Class | Line separator |
| LinearRange | Class | Range selector |
| LinearRange\<T\> | Class | Generic range |
| MessageBox | Class | Message box dialogs |
| OpenDialog | Class | File open dialog |
| SaveDialog | Class | File save dialog |
| SpinnerView | Class | Loading spinner |

#### Autocomplete
| Type | Category | Description |
|------|----------|-------------|
| AppendAutocomplete | Class | Append autocomplete |
| AutocompleteBase | Class | Autocomplete base |
| AutocompleteContext | Class | Autocomplete context |
| IAutocomplete | Interface | Autocomplete interface |
| ISuggestionGenerator | Interface | Suggestion generation |
| PopupAutocomplete | Class | Popup autocomplete |
| SingleWordSuggestionGenerator | Class | Word suggestions |
| Suggestion | Class | Suggestion item |
| TextFieldAutocomplete | Class | TextField autocomplete |
| TextViewAutocomplete | Class | TextView autocomplete |

#### Data Sources
| Type | Category | Description |
|------|----------|-------------|
| DataTableSource | Class | DataTable source |
| EnumerableTableSource\<T\> | Class | Enumerable source |
| IListDataSource | Interface | List data source |
| ITableSource | Interface | Table data source |
| IEnumerableTableSource\<T\> | Interface | Enumerable table |
| ListTableSource | Class | List-based table |
| ListWrapper\<T\> | Class | List wrapper |

#### Tree Infrastructure
| Type | Category | Description |
|------|----------|-------------|
| DelegateTreeBuilder\<T\> | Class | Delegate-based builder |
| FileSystemTreeBuilder | Class | File system tree |
| ITreeBuilder\<T\> | Interface | Tree builder |
| ITreeNode | Interface | Tree node |
| ITreeView | Interface | Tree view interface |
| ITreeViewFilter\<T\> | Interface | Tree filter |
| TreeBuilder\<T\> | Class | Tree builder |
| TreeNode | Class | Tree node |
| TreeNodeBuilder | Class | Node builder |
| TreeStyle | Class | Tree styling |
| TreeTableSource\<T\> | Class | Tree table source |
| TreeViewTextFilter\<T\> | Class | Text filter |

#### Graph Components
| Type | Category | Description |
|------|----------|-------------|
| Axis | Class | Graph axis |
| AxisIncrementToRender | Class | Axis increment |
| BarSeries | Class | Bar chart series |
| BarSeriesBar | Class | Bar in series |
| GraphCellToRender | Class | Graph cell |
| HorizontalAxis | Class | Horizontal axis |
| IAnnotation | Interface | Graph annotation |
| ISeries | Interface | Data series |
| LegendAnnotation | Class | Legend |
| LineF | Class | Float line |
| MultiBarSeries | Class | Multi-bar series |
| PathAnnotation | Class | Path annotation |
| ScatterSeries | Class | Scatter plot |
| TextAnnotation | Class | Text annotation |
| VerticalAxis | Class | Vertical axis |

#### SpinnerStyle Variants (70+ nested classes)
The `SpinnerStyle` class contains 70+ nested classes for different animation styles:
`Aesthetic`, `Arc`, `Arrow`, `Balloon`, `BetaWave`, `BluePulse`, `Bounce`, `BouncingBall`, `BouncingBar`, `BoxBounce`, `Christmas`, `Circle`, `CircleHalves`, `CircleQuarters`, `Clock`, `Custom`, `Dots` (1-12), `Dqpb`, `Earth`, `FingerDance`, `FistBump`, `Flip`, `Grenade`, `GrowHorizontal`, `GrowVertical`, `Hamburger`, `Hearts`, `Layer`, `Line`, `Material`, `MindBlown`, `Monkey`, `Moon`, `Noise`, `OrangeBluePulse`, `OrangePulse`, `Pipe`, `Points`, `Pong`, `Runner`, `Shark`, `SimpleDots`, `SimpleDotsScrolling`, `Smiley`, `SoccerHeader`, `Speaker`, `SquareCorners`, `Squish`, `Star`, `TimeTravelClock`, `Toggle` (1-13), `Triangle`, `Weather`

#### Supporting Types
| Type | Category | Description |
|------|----------|-------------|
| AllowedType | Class | Type allowance |
| AllowedTypeAny | Class | Any type allowed |
| CellActivatedEventArgs | Class | Cell activation |
| CellColorGetterArgs | Class | Cell color args |
| CellColorGetterDelegate | Delegate | Cell color getter |
| CellToggledEventArgs | Class | Cell toggle events |
| CheckBoxTableSourceWrapperBase | Class | Checkbox table wrapper |
| CheckState | Enum | Check state |
| ColorPickerStyle | Class | Color picker styling |
| ColumnStyle | Class | Column styling |
| ContentsChangedEventArgs | Class | Content change |
| DefaultFileOperations | Class | Default file ops |
| DrawTreeViewLineEventArgs\<T\> | Class | Tree line draw |
| FileDialogStyle | Class | File dialog styling |
| FilesSelectedEventArgs | Class | File selection |
| HistoryTextItemEventArgs | Class | History events |
| HexViewEditEventArgs | Class | Hex edit events |
| HexViewEventArgs | Class | Hex view events |
| IAllowedType | Interface | Type allowance |
| ICollectionNavigator | Interface | Collection nav |
| ICollectionNavigatorMatcher | Interface | Nav matcher |
| IListCollectionNavigator | Interface | List nav |
| ITextValidateProvider | Interface | Validation |
| LinearRangeAttributes | Class | Range attributes |
| LinearRangeEventArgs\<T\> | Class | Range events |
| LinearRangeOptionEventArgs | Class | Range options |
| LinearRangeOption\<T\> | Class | Range option |
| LinearRangeStyle | Class | Range styling |
| LinearRangeType | Enum | Range type |
| ListColumnStyle | Class | List column style |
| ListViewItemEventArgs | Class | List item events |
| ListViewRowEventArgs | Class | List row events |
| NetMaskedTextProvider | Class | Masked input |
| ObjectActivatedEventArgs\<T\> | Class | Object activation |
| OpenMode | Enum | File open mode |
| ProgressBarFormat | Enum | Progress format |
| ProgressBarStyle | Enum | Progress style |
| Prompt\<V,R\> | Class | Generic prompt |
| PromptExtensions | Class | Prompt extensions |
| ReadOnlyCollectionExtensions | Class | Collection extensions |
| RowColorGetterArgs | Class | Row color args |
| RowColorGetterDelegate | Delegate | Row color getter |
| Runnable | Class | Runnable base |
| Runnable\<T\> | Class | Generic runnable |
| SelectedCellChangedEventArgs | Class | Cell selection |
| SelectionChangedEventArgs\<T\> | Class | Selection change |
| SelectorBase | Class | Selector base |
| SelectorStyles | Enum | Selector styles |
| Tab | Class | Tab item |
| TabChangedEventArgs | Class | Tab change events |
| TabMouseEventArgs | Class | Tab mouse events |
| TabStyle | Class | Tab styling |
| TableSelection | Class | Table selection |
| TableStyle | Class | Table styling |
| TextRegexProvider | Class | Regex validation |
| UnicodeRange | Class | Unicode range |

---

## Type Count Summary

| Namespace | Count |
|-----------|-------|
| Terminal.Gui.App | 35 |
| Terminal.Gui.Configuration | 15 |
| Terminal.Gui.Drawing | 40 |
| Terminal.Gui.Drivers | 80+ |
| Terminal.Gui.FileServices | 5 |
| Terminal.Gui.Input | 18 |
| Terminal.Gui.Resources | 1 |
| Terminal.Gui.Testing | 8 |
| Terminal.Gui.Text | 4 |
| Terminal.Gui.Time | 4 |
| Terminal.Gui.ViewBase | 70 |
| Terminal.Gui.Views | 250+ |
| **Total** | **~530** |
