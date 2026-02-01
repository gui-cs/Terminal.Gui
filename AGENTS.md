# Terminal.Gui - AI Agent Instructions

> **📘 Source of Truth: [CONTRIBUTING.md](CONTRIBUTING.md)**
>
> This file provides quick-reference conventions for AI agents.
> See also: [llms.txt](llms.txt) for machine-readable context.

## Are You Building an App or Contributing?

| Task | Start Here |
|------|------------|
| **Building an app** with Terminal.Gui | [.claude/tasks/build-app.md](.claude/tasks/build-app.md) |
| **Contributing** to the library | Continue reading below |

---

## For App Builders

### Quick Start
```bash
dotnet new install Terminal.Gui.Templates@2.0.0-alpha.*
dotnet new tui-simple -n myproj
cd myproj
dotnet run
```

### Key Resources
- **App Building Guide**: [.claude/tasks/build-app.md](.claude/tasks/build-app.md)
- **Common Patterns**: [.claude/cookbook/common-patterns.md](.claude/cookbook/common-patterns.md)
- **Examples**: `Examples/Example/` (minimal), `Examples/UICatalog/` (comprehensive)

### API Reference (Compressed)
| Namespace | Contents |
|-----------|----------|
| [namespace-app.md](docfx/apispec/namespace-app.md) | Application lifecycle, IApplication |
| [namespace-views.md](docfx/apispec/namespace-views.md) | All UI controls (Button, Label, ListView, etc.) |
| [namespace-viewbase.md](docfx/apispec/namespace-viewbase.md) | View, Pos, Dim, Adornments |
| [namespace-drawing.md](docfx/apispec/namespace-drawing.md) | Colors, LineStyle, rendering |
| [namespace-input.md](docfx/apispec/namespace-input.md) | Keyboard, mouse handling |
| [namespace-text.md](docfx/apispec/namespace-text.md) | Text manipulation |
| [namespace-configuration.md](docfx/apispec/namespace-configuration.md) | Configuration, themes |

---

## For Library Contributors

### Project Essentials

**Terminal.Gui** - Cross-platform console UI toolkit for .NET (C# 12, net8.0)

**Build:** `dotnet restore && dotnet build --no-restore`
**Test:** `dotnet test --no-build`
**Details:** [Build & Test Workflow](.claude/workflows/build-test-workflow.md)

## Quick Rules

**⚠️ READ THIS BEFORE MODIFYING ANY FILE - These are Terminal.Gui-specific conventions:**

1. **No `var`** - Use explicit types except for: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`
2. **Use `new ()`** - Target-typed new when type is on left side (not `new TypeName()`)
3. **Use `[...]`** - Collection expressions, not `new () { ... }`
4. **SubView/SuperView** - Never say "child", "parent", or "container"
5. **Unused lambda params** - Use `_` discard: `(_, _) => { }`
6. **Local functions** - Use camelCase: `void myLocalFunc ()`
7. **Backing fields** - Place immediately before their property

## Detailed Coding Rules

Consult these files in `.claude/rules/` before editing code:

- [Type Declarations](/.claude/rules/type-declarations.md) - `var` vs explicit types
- [Target-Typed New](/.claude/rules/target-typed-new.md) - `new()` syntax
- [Collection Expressions](/.claude/rules/collection-expressions.md) - `[...]` syntax
- [Terminology](/.claude/rules/terminology.md) - SubView/SuperView terms
- [Event Patterns](/.claude/rules/event-patterns.md) - Lambdas, handlers, closures
- [CWP Pattern](/.claude/rules/cwp-pattern.md) - Cancellable Workflow Pattern
- [Code Layout](/.claude/rules/code-layout.md) - Member ordering, backing fields
- [Testing Patterns](/.claude/rules/testing-patterns.md) - Test writing conventions
- [API Documentation](/.claude/rules/api-documentation.md) - XML doc requirements

## Workflows

Process guides in `.claude/workflows/`:

- [Build & Test Workflow](/.claude/workflows/build-test-workflow.md) - Build, test, and troubleshooting
- [PR Workflow](/.claude/workflows/pr-workflow.md) - Submitting pull requests

## Task-Specific Guides

See `.claude/tasks/` for specialized checklists:
- [build-app.md](.claude/tasks/build-app.md) - Building apps with Terminal.Gui

See `.claude/cookbook/` for common UI patterns:
- [common-patterns.md](.claude/cookbook/common-patterns.md) - Forms, lists, menus, dialogs, etc.

---

## Documentation Index (Compressed)

> **IMPORTANT**: Use retrieval-led reasoning. Read full docs before making changes.
> Detailed index: [.tg-docs/INDEX.md](.tg-docs/INDEX.md) (~530 types across 12 namespaces)

### Deep Dives (docfx/docs/)

```
[Core Architecture]
|application.md|IApplication,SessionStack,Run/Dispose,View.App,instance-based pattern
|View.md|SuperView/SubView,Frame/Viewport/ContentArea,composition layers
|drivers.md|IDriver,DriverRegistry,ANSI/Windows/Unix,platform abstraction
|navigation.md|Focus,TabStop/TabGroup,Tab/F6 keys,HasFocus,ApplicationNavigation

[Layout & Arrangement]
|layout.md|Pos/Dim,absolute/relative positioning,SetNeedsLayout
|arrangement.md|ViewArrangement,Movable/Resizable/Overlapped,tiled vs overlapped
|dimauto.md|Dim.Auto,content-based sizing,DimAutoStyle
|scrolling.md|Viewport vs ContentSize,scroll events

[Commands & Events]
|command.md|Command enum,AddCommand,KeyBindings/MouseBindings,Activate/Accept/HotKey
|events.md|Event categories,CWP integration,binding types (KeyBinding/MouseBinding)
|cancellable-work-pattern.md|CWP: Work→Virtual→Event,OnXxx methods,Raise pattern

[Input]
|keyboard.md|Key class,KeyBindings,key processing order,IKeyboard
|mouse.md|MouseFlags,MouseBindings,grab/release
|input-injection.md|VirtualTimeProvider,InjectKey/InjectMouse,testing

[Visual]
|drawing.md|Move/AddStr/AddRune,Attribute,LineCanvas
|scheme.md|Scheme,VisualRole,theming
|cursor.md|View.Cursor,CursorVisibility
|Popovers.md|Drawing outside viewport,modal behavior

[Components]
|views.md|Complete catalog of built-in views
|menus.md|MenuBar,ContextMenu,MenuItem
|tableview.md|TableView data binding
|treeview.md|TreeView hierarchical data
|prompt.md|MessageBox,input dialogs

[Config & Advanced]
|config.md|ConfigurationManager,themes,JSON config
|multitasking.md|Background ops,Invoke,threading
|logging.md|ILogger,debug output
|ansihandling.md|ANSI escape parsing

[Migration]
|newinv2.md|v2 changes,new features
|migratingfromv1.md|Migration guide,API changes
|lexicon.md|Terminology definitions
```

### API Namespaces (docfx/apispec/)

```
|namespace-app.md|Application,IApplication,IRunnable,SessionToken
|namespace-viewbase.md|View,Adornment,Border,Margin,Padding
|namespace-views.md|Button,Label,TextField,ListView,CheckBox,etc.
|namespace-input.md|Key,Mouse,Command,ICommandContext
|namespace-drawing.md|Attribute,Color,LineCanvas,Cell
|namespace-drivers.md|IDriver,DriverRegistry
|namespace-configuration.md|ConfigurationManager,themes
|namespace-text.md|Text processing,autocomplete
|namespace-fileservices.md|File dialogs
```

### Source Code (Terminal.Gui/)

```
[Key Directories]
|Application/|IApplication,ApplicationImpl,SessionStack
|View/|View base,layout,drawing,focus
|Views/|All built-in views
|Input/|Key,Mouse,Command,bindings
|Drawing/|Attribute,Color,LineCanvas
|Drivers/|IDriver implementations
|Configuration/|ConfigurationManager,themes

[Critical Files]
|View/View.cs|Core View class
|View/View.Layout.cs|Layout implementation
|View/View.Drawing.cs|Drawing implementation
|View/View.Navigation.cs|Focus and navigation
|Application/ApplicationImpl.cs|IApplication implementation
|Input/Command.cs|Command enum
|Input/Key.cs|Key class
```

---

## Compressed API Type Index

> Quick reference for key types. Full list: [.tg-docs/INDEX.md](.tg-docs/INDEX.md)
> Format: `|Type|Category|Key members/notes`

### Terminal.Gui.App (35 types)
```
|Application|Class|Static facade (obsolete),Init,Run,Shutdown,Top
|IApplication|Interface|Instance-based,SessionStack,Run,Dispose
|SessionToken|Class|Session lifecycle,IDisposable
|Clipboard|Class|GetText,SetText,TryGetText
|IRunnable|Interface|Run view modal,used by Dialog
|ITimedEvents|Interface|AddTimeout,AddIdle,RemoveTimeout
|CancelEventArgs<T>|Class|Cancel property,cancellable events
|ValueChangingEventArgs<T>|Class|OldValue,NewValue,Cancel
|ApplicationNavigation|Class|Focus management,GetFocused,AdvanceFocus
|ApplicationPopover|Class|Popover management,Show,Hide
```

### Terminal.Gui.ViewBase (70 types)
```
|View|Class|Base class,Add,Remove,Frame,Viewport,Draw
|Pos|Class|Position:Absolute,Percent,Center,AnchorEnd,Func
|PosAbsolute|Class|Pos.At(n),absolute coordinate
|PosPercent|Class|Pos.Percent(n),percentage of SuperView
|PosCenter|Class|Pos.Center(),centered
|PosAnchorEnd|Class|Pos.AnchorEnd(n),from right/bottom
|PosView|Class|Pos.Left/Right/Top/Bottom(view)
|Dim|Class|Dimension:Absolute,Auto,Fill,Percent,Func
|DimAbsolute|Class|Dim.Absolute(n),fixed size
|DimAuto|Class|Dim.Auto(),content-based sizing
|DimFill|Class|Dim.Fill(margin),fill remaining
|DimPercent|Class|Dim.Percent(n),percentage
|Adornment|Class|Base for Border,Margin,Padding
|Border|Class|View border,Title,LineStyle
|Margin|Class|View outer margin
|Padding|Class|View inner padding
|Alignment|Enum|Start,Center,End,Fill
|Orientation|Enum|Horizontal,Vertical
|TabBehavior|Enum|NoStop,TabStop,TabGroup
|ViewArrangement|Enum|Movable,Resizable,Overlapped
```

### Terminal.Gui.Views (180+ types)
```
[Core Controls]
|Button|Class|Text,Accept event,IsDefault
|Label|Class|Text display,TextAlignment
|TextField|Class|Single-line input,Text,Secret
|TextView|Class|Multi-line editor,Text,ReadOnly
|CheckBox|Class|CheckedState,AllowCheckStateNone
|ComboBox|Class|Dropdown,Source,SelectedItem
|ProgressBar|Class|Fraction,BidirectionalMarquee
|ScrollBar|Class|Position,Size,Orientation
|NumericUpDown<T>|Class|Value,Increment,Min,Max

[Containers]
|Window|Class|Top-level,Title,MenuBar support
|Dialog|Class|Modal,Buttons,AddButton
|Dialog<T>|Class|Modal with result
|FrameView|Class|Titled frame container
|TabView|Class|Tabs,AddTab,SelectedTab
|Wizard|Class|Multi-step,AddStep,CurrentStep

[Lists & Data]
|ListView|Class|Source,SelectedItem,AllowsMarking
|TableView|Class|Table,SelectedRow,SelectedColumn
|TreeView|Class|Objects,AddObject,SelectedObject
|TreeView<T>|Class|Generic tree

[Menus]
|MenuBar|Class|Menus,UseKeysUpDownAsKeysLeftRight
|MenuItem|Class|Title,Action,Shortcut,SubMenu
|MenuBarItem|Class|Title,Children array
|Menu|Class|Popup menu display
|PopoverMenu|Class|Context menu,Show(items)
|StatusBar|Class|Items,Visible

[File Dialogs]
|FileDialog|Class|Base,Path,AllowedFileTypes
|OpenDialog|Class|OpenFile,AllowsMultipleSelection
|SaveDialog|Class|SaveFile,FileName

[Specialized]
|ColorPicker|Class|SelectedColor,Style
|GraphView|Class|Series,Annotations,AxisX/Y
|HexView|Class|Source,Position,Edits
|CharMap|Class|SelectedCodePoint,Start/End
|SpinnerView|Class|SpinnerStyle,AutoSpin
|MessageBox|Class|Query,ErrorQuery,static methods
```

### Terminal.Gui.Input (18 types)
```
|Key|Class|KeyCode,Modifiers,IsCtrl,IsAlt,IsShift
|KeyBindings|Class|Add,Get,TryGet,Remove,GetCommands
|KeyBinding|Struct|Commands[],Scope,Target
|Mouse|Class|Position,Flags,View
|MouseBindings|Class|Add,Get,TryGet,Remove
|MouseBinding|Struct|Commands[],Scope
|MouseFlags|Enum|Button1Clicked,Button1DoubleClicked,WheeledUp/Down
|Command|Enum|Accept,Cancel,Select,HotKey,ScrollUp/Down
|CommandContext|Struct|Command,KeyBinding,Source
```

### Terminal.Gui.Drawing (40 types)
```
|Attribute|Struct|Foreground,Background,constructor(fg,bg)
|Color|Struct|R,G,B,Parse,TryParse,FromArgb
|Scheme|Class|Normal,Focus,HotNormal,HotFocus,Disabled
|LineCanvas|Class|AddLine,GetMap,Merge
|LineStyle|Enum|None,Single,Double,Rounded,Heavy
|Glyphs|Class|Bullet,CheckMark,Diamond,etc.
|Cell|Struct|Rune,Attribute
|Thickness|Struct|Top,Left,Bottom,Right,Vertical,Horizontal
|Region|Class|Clipping,Union,Intersect,Exclude
|Gradient|Class|Colors[],Spectrum
```

### Terminal.Gui.Drivers (80+ types)
```
|IDriver|Interface|Init,End,Refresh,AddStr,Move
|Driver|Class|Base implementation
|DriverRegistry|Class|GetDrivers,Get,MakeDriver
|KeyCode|Enum|Key constants,A-Z,F1-F12,Enter,Esc
|CursorVisibility|Enum|Default,Invisible,Underline,Box
|IOutput|Interface|Terminal output
|IInputProcessor|Interface|Input processing
```

### Terminal.Gui.Configuration (15 types)
```
|ConfigurationManager|Class|Settings,Themes,Apply,Reset
|SchemeManager|Class|GetScheme,Schemes dictionary
|ThemeManager|Class|Theme,Themes,SelectedTheme
|ConfigLocations|Enum|Default,Global,App,Runtime
```

### Terminal.Gui.Testing (8 types)
```
|InputInjector|Class|InjectKey,InjectMouse,InjectChar
|IInputInjector|Interface|Injection interface
|VirtualTimeProvider|Class|Testing time control
```

### Terminal.Gui.Text (4 types)
```
|TextFormatter|Class|Text,Format,Size,Draw
|TextDirection|Enum|LeftRight_TopBottom,RightLeft,etc.
```

### Terminal.Gui.Time (4 types)
```
|ITimeProvider|Interface|Now,UtcNow,CreateTimer
|VirtualTimeProvider|Class|Testing,Advance,SetTime
|SystemTimeProvider|Class|Real system time
```

### Terminal.Gui.FileServices (5 types)
```
|IFileOperations|Interface|GetFiles,GetDirectories,Exists
|FileSystemTreeBuilder|Class|Build file trees
```
