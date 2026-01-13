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
- Uses `--include` parameter to target specific files
- Respects all project code style settings
- Command: `jb cleanupcode Terminal.sln --profile="Full Cleanup" --include="path/to/file.cs" --no-build`

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

## ReSharper Command Line Usage

### cleanupcode - Apply Code Formatting

The `--include` parameter **WORKS CORRECTLY** for single-file cleanup:

```bash
# Cleanup a single file
jb cleanupcode Terminal.sln \
    --profile="Full Cleanup" \
    --include="Terminal.Gui/Views/TableView/TableView.cs" \
    --no-build \
    --verbosity=WARN
```

**Key Parameters:**
- `--profile="Full Cleanup"` - Uses the profile defined in Terminal.sln.DotSettings
- `--include="path/to/file.cs"` - Relative path from solution root (forward slashes recommended)
- `--no-build` - Skips building before cleanup (recommended, saves time)
- `--verbosity=WARN` - Recommended (ERROR=minimal, WARN=moderate, VERBOSE=detailed, use VERBOSE for full debugging)

**Verified Behavior (Tested 2026-01-13):**
- ✅ Only the specified file is modified (--include works correctly)
- ⚠️ May display exceptions in output, but cleanup completes successfully

### inspectcode - Identify Code Issues

```bash
# Inspect a single file for warnings
jb inspectcode Terminal.sln \
    --output="inspect-report.xml" \
    --include="Terminal.Gui/Views/TableView/TableView.cs" \
    --severity=WARNING \
    --no-build \
    --format=Xml
```

**Key Parameters:**
- `--output="file.xml"` - Path to output XML report
- `--include="path/to/file.cs"` - Filter to specific file (same format as cleanupcode)
- `--severity=WARNING` - Minimum severity level (INFO, HINT, SUGGESTION, WARNING, ERROR)
- `--format=Xml` - Output format (Xml, Html, Text, Sarif)

## Known Limitations

### ReSharper Exceptions in Output

**Observation:** ReSharper cleanupcode displays exceptions during execution, but completes successfully (exit code 0) and correctly modifies the target file.

Common exceptions observed:

**1. NuGet Version Parsing:**
```
Unable to parse version string 8.
--- EXCEPTION #1/2 [InvalidOperationException]
Message = "Unable to parse version string 8."
```

**2. Component Initialization:**
```
Must not be called inside a component constructor: Use GetLazyProvider
--- EXCEPTION #1/1 [LoggerException]
```

**3. ConfigFileCache Warnings:**
```
Warning: <ConfigFileCache> Attention! Removing a mount point before rebuilding goes inconsistent.
```

**What We Know:**
- ✅ Command completes with exit code 0
- ✅ Target files are correctly modified with expected formatting changes
- ✅ Only files matching --include pattern are modified
- ❌ Exceptions appear in output (unclear if expected behavior or bugs)

**Verification Steps:**
1. Check exit code: `Main method ... returned exit code 0`
2. Verify changes: `git diff` to see actual modifications
3. Look for: `Saving document <filename>` in verbose output

**Unknown:** Whether these exceptions indicate bugs in ReSharper CLI or are expected internal logging. If concerned, consider reporting to JetBrains support.

### Performance Characteristics

ReSharper cleanupcode loads the entire solution model even when `--include` specifies one file:
- **First run on solution**: 30-60 seconds (builds caches, loads solution model)
- **Subsequent runs**: 15-30 seconds (uses cached solution model)
- **Large solutions**: May take 1-2 minutes

This is expected behavior - ReSharper needs the full solution context for accurate code analysis, but **only modifies files matching the --include pattern**.

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
