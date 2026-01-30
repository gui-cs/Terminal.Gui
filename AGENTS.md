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
> Detailed index: [.tg-docs/INDEX.md](.tg-docs/INDEX.md)

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
