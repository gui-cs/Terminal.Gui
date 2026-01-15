# Prompt API Design Review - Issue #2443

## Overview

This document proposes a refined design for the generic `Prompt` API based on feedback from PR #4571.

## Design Goals (from Issue #2443)

1. ✅ Build upon existing infrastructure (Runnable, Dialog)
2. ✅ Make Dialog generic with Input/Result flow
3. ✅ Allow customization via handlers
4. ✅ Cross-language usability (C# and PowerShell)
5. ✅ Capture hosting relationship via `runnable.Prompt()`

## Key Changes from PR #4571

| Aspect | Current (PR #4571) | Proposed |
|--------|-------------------|----------|
| **title parameter** | Required in extension method | Removed - set via `Title` property or handler |
| **view parameter** | Required in one overload, auto-created in another | Optional - auto-created if null |
| **WrappedView access** | `required` property (init-only) | `GetWrappedView()` method (get-only) |
| **Handler target** | Customizes Dialog only | Customizes Dialog AND can access view via `GetWrappedView()` |

---

## Proposed API

UPDATE THIS BASED ON CURRENT IMPLEMENTATION.

## Usage Examples

### C# Usage Pattern 1: Auto-Create View, Customize in Handler

UPDATE THIS BASED ON CURRENT IMPLEMENTATION:

```csharp
// Let Prompt create the DatePicker, customize via handler
DateTime? date = mainWindow.Prompt<DatePicker, DateTime>(
    resultExtractor: dp => dp.Date,
    beginInitHandler: dlg =>
    {
        dlg.Title = "Select Date";
        dlg.GetWrappedView().Date = DateTime.Now;
        dlg.BorderStyle = LineStyle.Rounded;
    });

if (date is { } selectedDate)
{
    Console.WriteLine($"Selected: {selectedDate:yyyy-MM-dd}");
}
```

### C# Usage Pattern 2: Pre-Create View, Pass to Prompt

```csharp
// Pre-create and configure the view
DatePicker datePicker = new() { Date = DateTime.Now };

DateTime? date = mainWindow.Prompt<DatePicker, DateTime>(
    resultExtractor: dp => dp.Date,
    view: datePicker,
    beginInitHandler: dlg =>
    {
        dlg.Title = "Select Date";
        dlg.BorderStyle = LineStyle.Rounded;
    });

if (date is { } selectedDate)
{
    Console.WriteLine($"Selected: {selectedDate:yyyy-MM-dd}");
}
```

### C# Usage Pattern 3: Direct Prompt Construction

```csharp
// For maximum control, create Prompt directly
DatePicker datePicker = new() { Date = DateTime.Now };

using Prompt<DatePicker, DateTime> prompt = new(datePicker)
{
    Title = "Select Date",
    ResultExtractor = dp => dp.Date,
    BorderStyle = LineStyle.Rounded
};

DateTime? result = app.Run(prompt);

if (result is { } selectedDate)
{
    Console.WriteLine($"Selected: {selectedDate:yyyy-MM-dd}");
}
```

### PowerShell Usage Pattern 1: Extension Method (Simple)

```powershell
using namespace Terminal.Gui.App
using namespace Terminal.Gui.Views

# Create and configure the view
$datePicker = [DatePicker]::new()
$datePicker.Date = [DateTime]::Now

# Use the extension method on IApplication (no title - simple case)
$accepted = [PromptExtensions]::Prompt($app, $datePicker)

if ($accepted) {
    Write-Output $datePicker.Date.ToString("yyyy-MM-dd")
}
```

### PowerShell Usage Pattern 2: Direct Prompt Construction

```powershell
using namespace Terminal.Gui.App
using namespace Terminal.Gui.Views

# Create and configure the view
$datePicker = [DatePicker]::new()
$datePicker.Date = [DateTime]::Now

# Create Prompt directly for more control (e.g., setting Title)
$prompt = [Prompt[DatePicker,bool]]::new($datePicker)
$prompt.Title = "Select Date"
$prompt.ResultExtractor = [Func[DatePicker,bool]] { param($dp) return $true }

$app.Run($prompt)
$accepted = $prompt.Result

if ($accepted) {
    Write-Output $datePicker.Date.ToString("yyyy-MM-dd")
}
```

---

## Comparison: Handler Usage

### Current Design (PR #4571)

```csharp
// Handler can only customize the Dialog
DateTime? date = mainWindow.Prompt<DatePicker, DateTime>(
    title: "Select Date",
    view: new DatePicker { Date = DateTime.Now },
    resultExtractor: dp => dp.Date,
    beginInitHandler: dlg =>
    {
        dlg.BorderStyle = LineStyle.Rounded;
        // CANNOT access the wrapped view here!
    });
```

### Proposed Design

```csharp
// Handler can customize Dialog AND access wrapped view
DateTime? date = mainWindow.Prompt<DatePicker, DateTime>(
    resultExtractor: dp => dp.Date,
    beginInitHandler: dlg =>
    {
        dlg.Title = "Select Date";
        dlg.BorderStyle = LineStyle.Rounded;
        dlg.GetWrappedView().Date = DateTime.Now;  // ✅ Can access view
    });
```

---

## Benefits

### 1. Flexibility
- **C# devs**: Can auto-create view OR pre-create it
- **PowerShell devs**: Always pre-create and configure (no lambda complexity)

### 2. Simpler API Surface
- **No title parameter** - reduced parameter count, use property instead
- **Single overload** - optional view parameter handles both cases
- **Clear access pattern** - `GetWrappedView()` is explicit

### 3. Better Handler Semantics
- Handler can access **both** Dialog and wrapped View
- C# devs can initialize auto-created views in handler
- No need for separate "view handler" and "dialog handler"

### 4. PowerShell Friendly
- Pre-create view, set properties naturally
- Pass configured view to extension method
- No Action/Func delegate complexity

---

## Design Decisions (Finalized)

1. ✅ **C# parameter order**: `Prompt(resultExtractor, view, input, handler)`
   - `resultExtractor` is first because it's not optional (yet)
   - `view` is second as optional parameter

2. ✅ **PowerShell extension method**: `Prompt(app, view, okButtonText, cancelButtonText)`
   - **No title parameter** - keeps it simple
   - PowerShell users create Prompt directly if they need to set title

3. ✅ **No static Show() method** - extension method only is sufficient
   - Simpler API surface
   - PowerShell users can use `[PromptExtensions]::Prompt()`

---

## Implementation Notes

### Constructor Constraint Challenge

The constructor `Prompt(TView? view = null)` requires `new()` constraint only when `view` is null, but C# doesn't support conditional constraints.

**Solution**: Keep constraint on class level, document that it's only needed for auto-creation:

```csharp
public class Prompt<TView, TResult> : Dialog<TResult>
    where TView : View, new()  // new() only needed if auto-creating
{
    public Prompt(TView? view = null)
    {
        _wrappedView = view ?? new TView();
        // ...
    }
}
```

If a view is provided, the `new()` constraint is technically unnecessary, but harmless.

### Breaking Changes from PR #4571

1. ✅ **Extension method signature changed** - `title` parameter removed
2. ✅ **WrappedView property removed** - replaced with `GetWrappedView()` method
3. ✅ **Prompt constructor signature changed** - now takes optional view

Since this is pre-Beta (v2 Alpha), breaking changes are acceptable.
