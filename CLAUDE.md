# CLAUDE.md

> **Guidance for AI agents working with Terminal.Gui.**
> For humans, see [CONTRIBUTING.md](./CONTRIBUTING.md).

## Before Every File Edit

**READ `.claude/REFRESH.md` first.** It contains a quick checklist to prevent common mistakes.

## Detailed Rules

See `.claude/rules/` for detailed guidance:
- `type-declarations.md` - **No var** except built-in types
- `target-typed-new.md` - Use `new ()` not `new TypeName()`
- `terminology.md` - **SubView/SuperView**, never "child/parent"
- `event-patterns.md` - Lambdas, closures, handlers
- `collection-expressions.md` - Use `[...]` syntax
- `cwp-pattern.md` - Cancellable Workflow Pattern

## Task-Specific Guides

See `.claude/tasks/` for task checklists:
- `scenario-modernization.md` - Upgrading UICatalog scenarios

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

1. **No `var`** except: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`
2. **Use `new ()`** not `new TypeName()`
3. **Use `[...]`** not `new () { ... }` for collections
4. **SubView/SuperView** for containment (Parent/Child only for non-containment refs)
5. **Unused lambda params** - use `_`: `(_, _) => { }`

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

- Don't use `var` for non-built-in types
- Don't use redundant type names with `new`
- Don't say "child/parent" for containment (use SubView/SuperView)
- Don't modify unrelated code
- Don't introduce new warnings
