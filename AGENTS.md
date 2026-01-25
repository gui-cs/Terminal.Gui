# Terminal.Gui - AI Agent Instructions

> **📘 Source of Truth: [CONTRIBUTING.md](CONTRIBUTING.md)**
> 
> This file provides quick-reference coding conventions for AI agents.
> For complete guidelines, architecture concepts, and workflows, see [CONTRIBUTING.md](CONTRIBUTING.md).

## Project Essentials

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

Check `.claude/tasks/` for specialized task checklists before starting work.
