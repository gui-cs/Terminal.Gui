---
name: testing-crap-analysis
description: Analyze code coverage and CRAP (Change Risk Anti-Patterns) scores to identify high-risk code. Use OpenCover format with ReportGenerator. Adapted for Terminal.Gui's existing coverage infrastructure.
---

# CRAP Score Analysis

> **Terminal.Gui Context:** Terminal.Gui requires 70%+ coverage for new code. Coverage is collected on Linux runners and uploaded to Codecov. See `.claude/rules/testing-patterns.md` for testing requirements.

## When to Use This Skill

Use this skill when:
- Evaluating code quality and test coverage before changes
- Identifying high-risk code that needs refactoring or testing
- Prioritizing which code to test based on risk

---

## What is CRAP?

**CRAP Score = Complexity x (1 - Coverage)^2**

The CRAP (Change Risk Anti-Patterns) score combines cyclomatic complexity with test coverage to identify risky code.

| CRAP Score | Risk Level | Action Required |
|------------|------------|-----------------|
| **< 5** | Low | Well-tested, maintainable code |
| **5-30** | Medium | Acceptable but watch complexity |
| **> 30** | High | Needs tests or refactoring |

### Why CRAP Matters

- **High complexity + low coverage = danger**: Code that's hard to understand AND untested is risky to modify
- **Complexity alone isn't enough**: A complex method with 100% coverage is safer than a simple method with 0%
- **Focuses effort**: Prioritize testing on complex code, not simple getters/setters

---

## Running Coverage in Terminal.Gui

### Quick Commands

```bash
# Run parallelizable tests with coverage
dotnet test Tests/UnitTestsParallelizable --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Run non-parallel tests with coverage
dotnet test Tests/UnitTests --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report (requires ReportGenerator)
dotnet reportgenerator \
  -reports:"TestResults/**/coverage.opencover.xml" \
  -targetdir:"coverage" \
  -reporttypes:"Html;TextSummary"

# View summary
cat coverage/Summary.txt

# Open HTML report
xdg-open coverage/index.html  # Linux
open coverage/index.html       # macOS
```

### Install ReportGenerator

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
```

---

## Reading the Report

### Risk Hotspots Section

The HTML report includes a **Risk Hotspots** section showing methods sorted by complexity:

```
Risk Hotspots
-------------
Method                          Complexity  Coverage  Crap Score
----------------------------------------------------------------
View.Draw()                     54          52%       12.4
Application.ProcessKey()        32          0%        32.0   <- HIGH RISK
Layout.Calculate()              28          85%       1.3
TextField.SetText()             15          100%      0.0
```

**Action items:**
- Methods with CRAP > 30 and 0% coverage - **test immediately or refactor**
- Complex methods with decent coverage - acceptable
- Well-tested methods - safe to modify

---

## Terminal.Gui Coverage Standards

| Metric | New Code | Legacy Code |
|--------|----------|-------------|
| Line Coverage | 70%+ | 60%+ (improve gradually) |
| Branch Coverage | 60%+ | 40%+ (improve gradually) |
| Maximum CRAP | 30 | Document exceptions |

### Test Project Guidelines

- **Prefer `UnitTestsParallelizable`** - Add new tests here unless they need static state
- **Use `UnitTests`** only for tests requiring `Application.Init` or static state
- **Add AI attribution comment**: `// Claude - Opus 4.5`

---

## Identifying High-Risk Code

### Signs of Risky Code

1. **High cyclomatic complexity (>20)** - Many branches, nested conditions
2. **Low coverage (<50%)** - Untested code paths
3. **Large methods (>100 lines)** - Hard to understand and test
4. **Many dependencies** - Hard to isolate for testing

### Refactoring Strategies

```csharp
// BAD: High complexity, hard to test
public void ProcessInput(Key key)
{
    if (key == Key.Enter)
    {
        if (_mode == Mode.Edit)
        {
            if (_validator.Validate(_text))
            {
                // 20 more nested conditions...
            }
        }
    }
}

// GOOD: Extract methods, reduce complexity
public void ProcessInput(Key key)
{
    Action? handler = key switch
    {
        Key.Enter => HandleEnter,
        Key.Escape => HandleEscape,
        Key.Tab => HandleTab,
        _ => null
    };

    handler?.Invoke();
}

private void HandleEnter()
{
    if (_mode != Mode.Edit) return;
    if (!_validator.Validate(_text)) return;

    CommitEdit();
}
```

---

## What Gets Excluded

Terminal.Gui's coverage configuration typically excludes:

| Pattern | Reason |
|---------|--------|
| Test assemblies | Not production code |
| Generated code | `*.g.cs`, `*.designer.cs` |
| Obsolete members | Being phased out |
| Debug-only code | Not in release builds |

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
