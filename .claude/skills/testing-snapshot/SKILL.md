---
name: testing-snapshot
description: Use Verify for snapshot testing in .NET. Approve API surfaces, rendered output, and serialized data. Potentially useful for Terminal.Gui to validate console output rendering.
---

# Snapshot Testing with Verify

## When to Use This Skill

Use snapshot testing when:
- Verifying rendered output (console screens, reports)
- Approving public API surfaces for breaking change detection
- Validating serialization output (config files, state)
- Catching unintended changes in complex objects

---

## What is Snapshot Testing?

Snapshot testing captures output and compares it against a human-approved baseline:

1. **First run**: Test generates a `.received.` file with actual output
2. **Human review**: Developer approves it, creating a `.verified.` file
3. **Subsequent runs**: Test compares output against `.verified.` file
4. **Changes detected**: Test fails, diff tool shows differences for review

---

## Installation

```bash
dotnet add package Verify.Xunit
```

### Configure ModuleInitializer

```csharp
using System.Runtime.CompilerServices;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyBase.UseProjectRelativeDirectory("Snapshots");
    }
}
```

---

## Basic Usage

### Simple Object Verification

```csharp
// Claude - Opus 4.5
[Fact]
public Task VerifyViewState()
{
    View view = new ()
    {
        X = 0,
        Y = 0,
        Width = 10,
        Height = 5,
        Title = "Test"
    };

    return Verify(view);
}
```

Creates `VerifyViewState.verified.txt`:
```json
{
  X: 0,
  Y: 0,
  Width: 10,
  Height: 5,
  Title: Test
}
```

---

## Potential Terminal.Gui Use Cases

### Console Output Verification

```csharp
// Claude - Opus 4.5
[Fact]
public Task VerifyButtonRendering()
{
    Button button = new () { Text = "OK" };
    button.SetRelativeLayout(new Rectangle(0, 0, 10, 1));

    // Capture rendered output
    string rendered = RenderToString(button);

    return Verify(rendered, extension: "txt");
}
```

### API Surface Approval

```csharp
// Claude - Opus 4.5
[Fact]
public Task ApprovePublicApi()
{
    Assembly assembly = typeof(View).Assembly;

    IEnumerable<object> publicApi = assembly.GetExportedTypes()
        .OrderBy(t => t.FullName)
        .Select(t => new
        {
            Type = t.FullName,
            Members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.DeclaringType == t)
                .OrderBy(m => m.Name)
                .Select(m => m.ToString())
        });

    return Verify(publicApi);
}
```

### Configuration State Verification

```csharp
// Claude - Opus 4.5
[Fact]
public Task VerifyDefaultConfiguration()
{
    ConfigurationManager config = ConfigurationManager.GetDefault();

    return Verify(config);
}
```

---

## Scrubbing Dynamic Values

Handle timestamps, GUIDs, and other dynamic content:

```csharp
// Claude - Opus 4.5
[Fact]
public Task VerifyViewWithDynamicData()
{
    View view = new ()
    {
        Id = Guid.NewGuid(),  // Different every run
        Title = "Test"
    };

    return Verify(view)
        .ScrubMember("Id");  // Replace with placeholder
}
```

Output:
```json
{
  Id: Guid_1,
  Title: Test
}
```

---

## File Organization

### Recommended Structure

```
Tests/
  UnitTestsParallelizable/
    Snapshots/
      ViewTests/
        VerifyViewState.verified.txt
        VerifyButtonRendering.verified.txt
    ViewTests.cs
    ModuleInitializer.cs
```

### .gitignore

```gitignore
# Verify - ignore received files (only commit verified)
*.received.*
```

---

## When to Use Snapshot Testing

| Scenario | Use Snapshot Testing? | Why |
|----------|----------------------|-----|
| Console rendering output | Yes | Catches visual regressions |
| API surfaces | Yes | Prevents accidental breaks |
| Serialization output | Yes | Validates format stability |
| Configuration defaults | Yes | Detects unintended changes |
| Simple value checks | No | Use regular assertions |
| Business logic | No | Use explicit assertions |

---

## Best Practices

### DO

```csharp
// Use descriptive test names - they become file names
[Fact]
public Task Button_WithText_RendersCorrectly()

// Scrub dynamic values consistently
VerifierSettings.ScrubMembersWithType<Guid>();

// Keep verified files in source control
git add *.verified.*
```

### DON'T

```csharp
// Don't verify random/dynamic data without scrubbing
View view = new () { Id = Guid.NewGuid() };  // Fails every run!
await Verify(view);

// Don't commit .received files
git add *.received.*  // Wrong!

// Don't use for simple assertions
await Verify(result.Count);  // Just use Assert.Equal(5, result.Count)
```

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
