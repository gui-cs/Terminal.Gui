# Code Cleanup Agent

## Purpose
Automated code cleanup, modernization, and refactoring for Terminal.Gui C# files.

## Components

1. **PartialSplitter** - Roslyn tool that splits large files (>1000 lines) into semantic partials
2. **BackingFieldReorderer** - Roslyn tool that fixes ReSharper bug RSRP-484963 (backing fields before properties)
3. **CleanupAgent.ps1** - PowerShell orchestrator that coordinates the cleanup process

## Usage

### Basic Invocation

```powershell
.\Scripts\CleanupAgent.ps1 -Files @("Terminal.Gui\Views\MessageBox.cs")
```

### Options

```powershell
# Skip ReSharper cleanup (for testing other features)
.\Scripts\CleanupAgent.ps1 -Files @("file.cs") -SkipReSharper

# Skip tests (for quick iteration)
.\Scripts\CleanupAgent.ps1 -Files @("file.cs") -SkipTests

# Skip partial splitting
.\Scripts\CleanupAgent.ps1 -Files @("file.cs") -SkipPartialSplit

# Combine flags
.\Scripts\CleanupAgent.ps1 -Files @("file.cs") -SkipReSharper -SkipTests
```

## Prerequisites

- **ReSharper Command Line Tools**:
  ```powershell
  dotnet tool install -g JetBrains.ReSharper.GlobalTools
  ```
- **.NET 8 SDK** (already required for Terminal.Gui)
- **Git** (for rollback functionality)

## What It Does

### 1. File Splitting (>1000 lines)
- Analyzes class members by naming patterns
- Groups related functionality:
  - `*Mouse*` → ClassName.Mouse.cs
  - `*Keyboard*` → ClassName.Keyboard.cs
  - `*Draw*` → ClassName.Drawing.cs
  - `*Layout*` → ClassName.Layout.cs
  - etc.
- **Preserves**:
  - Base classes and interfaces (in Core file)
  - Namespace-level delegates and types (in Core file)
  - Backing field-property relationships
- **Minimum**: 100 lines per partial (avoids over-splitting)

### 2. ReSharper Full Cleanup
- Applies "Full Cleanup" profile from `Terminal.sln.DotSettings`
- ⚠️ **WARNING**: May process entire solution (ReSharper CLI limitation)
- Respects all project code style settings

### 3. Backing Field Reordering
- Places backing fields immediately before their properties
- Fixes ReSharper bug RSRP-484963

### 4. Nullable Directive Management
- Adds `#nullable enable` if:
  - File doesn't have it
  - Project doesn't have it globally
  - File doesn't have `#nullable disable`

### 5. CWP Pattern Detection
- Identifies virtual `On*` methods without `Raise*` counterparts
- Identifies events without `Raise*` methods
- Adds TODO comments for manual CWP refactoring

### 6. Testing & Validation
- Rebuilds solution
- Runs UnitTestsParallelizable test suite
- Rolls back on failure (except for partial splits)

## Known Limitations

### ReSharper CLI Issue

**Problem**: ReSharper `cleanupcode` command may ignore the `--include` parameter and process the entire solution.

**Impact**: Running cleanup on a single file may modify hundreds of files across the codebase.

**Workarounds**:
1. **Recommended**: Use `--SkipReSharper` flag and run Full Cleanup manually in Rider/Visual Studio
2. Run cleanup on entire solution once, then use other agent features
3. Review all changes carefully before committing

### Partial Splitting Limitations

- Heuristic-based grouping may not always be perfect
- Review generated partials before committing
- Some classes may not split well semantically

## Examples

### Split Large File

```powershell
# Split and clean TableView.cs (2399 lines)
.\Scripts\CleanupAgent.ps1 -Files @("Terminal.Gui\Views\TableView\TableView.cs")

# Result: TableView.cs + TableView.Mouse.cs + TableView.Drawing.cs + etc.
```

### Just Backing Fields + CWP Detection

```powershell
# Skip ReSharper and tests for quick iteration
.\Scripts\CleanupAgent.ps1 -Files @("file.cs") -SkipReSharper -SkipTests
```

### Manual ReSharper Workflow

```powershell
# 1. Split file only
.\Scripts\CleanupAgent.ps1 -Files @("file.cs") -SkipReSharper -SkipTests -SkipPartialSplit:$false

# 2. Manually run ReSharper Full Cleanup in Rider

# 3. Run backing field reorder on partials
dotnet run --project Scripts/BackingFieldReorderer -- Terminal.Gui/Views/File.cs
dotnet run --project Scripts/BackingFieldReorderer -- Terminal.Gui/Views/File.Mouse.cs
# etc.
```

## Acceptance Criteria

Successfully cleaned files meet:
- ✓ Splits >1000 lines into logical partials
- ✓ Backing fields immediately before properties
- ✓ Zero ReSharper warnings
- ✓ `#nullable enable` directive (if appropriate) - all code should support nullable.
- ✓ CWP candidates identified with TODO comments
- ✓ Solution builds
- ✓ All tests pass

## File Organization

```
Scripts/
├── CleanupAgent.ps1              # Main orchestrator
├── BackingFieldReorderer/
│   ├── BackingFieldReorderer.csproj
│   └── Program.cs                # Roslyn tool
└── PartialSplitter/
    ├── PartialSplitter.csproj
    └── Program.cs                # Roslyn tool

.claude/
└── agents/
    └── cleanup-agent.md          # This file
```

## Troubleshooting

### "Build failed after cleanup"
- ReSharper may have introduced issues
- Review changes and fix manually
- Consider using `--SkipReSharper`

### "Tests failed"
- Changes rolled back automatically (except partial splits)
- Review test failures to determine cause
- May need test updates for structural changes

### "Hundreds of files modified"
- ReSharper CLI processed entire solution
- Use `git checkout -- .` to revert
- Next time: use `--SkipReSharper` or manual cleanup

### "Partial split created confusing files"
- Adjust grouping heuristics in PartialSplitter/Program.cs
- Delete partials and restore from `.backup` file
- Use `--SkipPartialSplit` to bypass

## Best Practices

1. **Test on small files first** - Verify agent behavior
2. **Use feature flags** - `-SkipReSharper` while iterating
3. **Review all changes** - Use `git diff` before committing
4. **Work on branches** - Create `cleanup/batch-1` branches
5. **Manual ReSharper recommended** - Due to CLI limitations

## Future Improvements

- [ ] Custom Roslyn analyzers instead of ReSharper CLI
- [ ] Parallel file processing
- [ ] Resume capability
- [ ] Integration with CI/CD
- [ ] Web UI for progress monitoring
