# CLAUDE.md

> **Guidance for AI agents working with Terminal.Gui.**
> For humans, see [CONTRIBUTING.md](./CONTRIBUTING.md).
> See also: [llms.txt](./llms.txt) for machine-readable context.

## Quick Reference: What Are You Doing?

| Your Task | Go Here |
|-----------|---------|
| **"Build me an app that..."** | [.claude/tasks/build-app.md](.claude/tasks/build-app.md) |
| **"Add a feature to Terminal.Gui..."** | Continue below (Contributor Guide) |
| **"Fix a bug in Terminal.Gui..."** | Continue below (Contributor Guide) |

### App Builder Quick Start
```bash
dotnet new install Terminal.Gui.Templates@2.0.0-alpha.*
dotnet new tui-simple -n myapp
cd myapp
dotnet run
```

See [.claude/tasks/build-app.md](.claude/tasks/build-app.md) for complete app development guide.
See [.claude/cookbook/common-patterns.md](.claude/cookbook/common-patterns.md) for UI recipes.

---

# Contributor Guide

**The rest of this file is for contributors modifying Terminal.Gui itself.**

## Before Every File Edit

**READ `.claude/REFRESH.md` first.** It contains a quick checklist to prevent common mistakes.

## After Writing/Modifying Code

**USE `.claude/POST-GENERATION-VALIDATION.md` to validate ALL code.** This catches the most common formatting violations AI agents make.

## Detailed Rules

See `.claude/rules/` for detailed guidance:
- `formatting.md` - **SPACING, BRACES, BLANK LINES** (most commonly violated!)
- `type-declarations.md` - **No var** except built-in types
- `target-typed-new.md` - Use `new ()` not `new TypeName()`
- `terminology.md` - **SubView/SuperView**, never "child/parent"
- `event-patterns.md` - Lambdas, closures, handlers
- `collection-expressions.md` - Use `[...]` syntax
- `cwp-pattern.md` - Cancellable Workflow Pattern
- `code-layout.md` - Backing fields, member ordering
- `api-documentation.md` - XML documentation requirements
- `testing-patterns.md` - Test patterns and requirements

## Task-Specific Guides

See `.claude/tasks/` for task checklists:
- `scenario-modernization.md` - Upgrading UICatalog scenarios
- `clean-code-review.md` - Creating clean git commit histories
- `build-app.md` - Building applications with Terminal.Gui

---

## Project Overview

**Terminal.Gui** - Cross-platform .NET console UI toolkit

- **Language**: C# (net8.0)
- **Branch**: `v2_develop`
- **Version**: v2 (Alpha)

## Build & Test

```bash
dotnet restore
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build
dotnet test Tests/UnitTests --no-build
```

## Key Concepts

| Concept | Documentation |
|---------|--------------|
| Application Lifecycle | `docfx/docs/application.md` |
| View Hierarchy | `docfx/docs/View.md` |
| Layout (Pos/Dim) | `docfx/docs/layout.md` |
| CWP Events | `docfx/docs/cancellable-work-pattern.md` |
| Terminology | `docfx/docs/lexicon.md` |

## Critical Rules (Summary)

1. **Space BEFORE `()` and `[]`** - `Method ()` not `Method()`, `array [i]` not `array[i]` (MOST VIOLATED!)
2. **Braces on NEXT line** - ALL opening braces use Allman style
3. **Blank lines** - before `return`/`break`/`continue`, after control blocks
4. **No `var`** except: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`
5. **Use `new ()`** not `new TypeName()`
6. **Use `[...]`** not `new () { ... }` for collections
7. **SubView/SuperView** for containment (Parent/Child only for non-containment refs)
8. **Unused lambda params** - use `_`: `(_, _) => { }`

## Testing

- Prefer `UnitTestsParallelizable` over `UnitTests`
- Add comment: `// Claude - Opus 4.5`
- Never decrease coverage
- Avoid `Application.Init` in tests

## Repository Structure

```
/Terminal.Gui/     - Core library
/Tests/            - Unit tests
/Examples/UICatalog/ - Demo app
/docfx/docs/       - Documentation
/.claude/          - AI agent guidance
```

## What NOT to Do

- Don't forget space before `()` and `[]` - this is the #1 mistake!
- Don't put braces on same line (use Allman style)
- Don't skip blank lines before returns or after control blocks
- Don't use `var` for non-built-in types
- Don't use redundant type names with `new`
- Don't say "child/parent" for containment (use SubView/SuperView)
- Don't modify unrelated code
- Don't introduce new warnings
- Don't skip POST-GENERATION-VALIDATION.md after writing code
