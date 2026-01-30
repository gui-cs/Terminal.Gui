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
