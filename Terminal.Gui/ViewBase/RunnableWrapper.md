# RunnableWrapper - Making Any View Runnable

> **?? Note**: This feature is part of **Phase 1 of Issue #4148** and requires the `POST_4148` preprocessor symbol to be defined.
> Until Phase 1 is complete, these features are not available in the standard build.

## Overview

The `RunnableWrapper<TView, TResult>` pattern enables **any View** to be run as a blocking session with typed results, without requiring the View to implement `IRunnable<TResult>` or derive from `Runnable<TResult>`.

This follows the same pattern as `FlagSelector<TFlagsEnum>` wrapping `FlagSelector` to provide type safety.

## Key Benefits

? **No ViewBase pollution** - `Result` stays in `IRunnable<TResult>`, not on every View  
? **Type safety** - Generics provide compile-time type checking  
? **Works with ANY View** - TextField, ColorPicker, custom Views, anything  
? **Opt-in** - Views that want permanent runnable behavior can still implement `IRunnable<T>`  
? **Clean API** - Wrapper is only created when needed  
? **Follows Terminal.Gui patterns** - Same as `FlagSelector<T>` wrapping `FlagSelector`  

## Prerequisites

To use these features, you must:
1. Define the `POST_4148` preprocessor symbol in your project
2. Wait for Phase 1 of Issue #4148 to be merged into main

```xml
<!-- In your .csproj file -->
<PropertyGroup>
    <DefineConstants>$(DefineConstants);POST_4148</DefineConstants>
</PropertyGroup>
```

## Three Ways to Use

### 1. Extension Method: `AsRunnable<TView, TResult>()`

Use when you want fluent API style with explicit result extraction:

```csharp
#if POST_4148
var textField = new TextField { Width = 40 };
var runnable = textField.AsRunnable(tf => tf.Text);

app.Run(runnable);

if (runnable.Result is { } text)
{
    Console.WriteLine($"You entered: {text}");
}
runnable.Dispose();
#endif
```

### 2. Application Method: `IApplication.RunView<TView, TResult>()`

Use for one-liner execution with automatic result extraction:

```csharp
#if POST_4148
var color = app.RunView(
    new ColorPicker(),
    cp => cp.SelectedColor);

Console.WriteLine($"Selected: {color}");
#endif
```

### 3. Direct Wrapper: `new RunnableWrapper<TView, TResult>(view)`

Use when you need manual control over the wrapper:

```csharp
#if POST_4148
var textField = new TextField { Width = 40 };
var wrapper = new RunnableWrapper<TextField, string>(textField);

wrapper.IsRunningChanging += (s, e) =>
{
    if (!e.NewValue) // Stopping
    {
        wrapper.Result = textField.Text;
    }
};

app.Run(wrapper);
Console.WriteLine($"Result: {wrapper.Result}");
wrapper.Dispose();
#endif
```

## Comparison: Traditional vs Wrapper Approach

### Traditional Approach (View implements IRunnable<T>)

**When to use:**
- View is **always** runnable (like Dialog, MessageBox)
- View is part of Terminal.Gui library
- Result extraction logic is complex and view-specific

**Example:**
```csharp
#if POST_4148
public class ColorPickerDialog : Runnable<Color?>
{
    private ColorPicker _picker;
    
    public ColorPickerDialog()
    {
        _picker = new ColorPicker();
        Add(_picker);
    }
    
    protected override bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning)
    {
        if (!newIsRunning)
        {
            Result = _picker.SelectedColor;
        }
        return base.OnIsRunningChanging(oldIsRunning, newIsRunning);
    }
}

// Usage
var dialog = new ColorPickerDialog();
app.Run(dialog);
Console.WriteLine($"Selected: {dialog.Result}");
dialog.Dispose();
#endif
```

### Wrapper Approach (View stays plain, wrapped when needed)

**When to use:**
- View is **sometimes** runnable (like TextField, ColorPicker)
- View is from user code or third-party
- Want to make ANY view runnable on-the-fly
- Result extraction is simple

**Example:**
```csharp
#if POST_4148
// ColorPicker is just a plain View
var picker = new ColorPicker();

// Make it runnable only when needed
var color = app.RunView(picker, cp => cp.SelectedColor);

Console.WriteLine($"Selected: {color}");
#endif
```

## Real-World Examples

All examples require `#if POST_4148` / `#endif` guards.

### Example 1: TextField Input

```csharp
#if POST_4148
// Quick text input dialog
var name = app.RunView(
    new TextField { Width = 40, Title = "Enter Name", BorderStyle = LineStyle.Single },
    tf => tf.Text);

Console.WriteLine($"Hello, {name}!");
#endif
```

### Example 2: FlagSelector with Enum

```csharp
#if POST_4148
// Select options with type-safe enum result
var styles = app.RunView(
    new FlagSelector<SelectorStyles>(),
    fs => fs.Value);

Console.WriteLine($"Selected: {styles}");
#endif
```

### Example 3: Custom Form View

```csharp
#if POST_4148
// Complex form with multiple fields
var formView = CreateComplexForm();
var formData = app.RunView(formView, ExtractFormData);

Console.WriteLine($"Name: {formData.Name}, Age: {formData.Age}");
#endif
```

### Example 4: Any View (even Label!)

```csharp
#if POST_4148
// Even a Label can be made runnable
var label = new Label { Text = "Press any key..." };
app.RunView(label);
#endif
```

## How It Works

1. **Wrapper Creation**: `RunnableWrapper<TView, TResult>` inherits from `Runnable<TResult>`
2. **View Composition**: Wrapped view is added as a subview
3. **Result Extraction**: Automatic via `IsRunningChanging` event subscription
4. **Type Safety**: Generic parameters ensure compile-time checking

```
???????????????????????????????????????
? RunnableWrapper<TextField, string>  ?  ? Inherits from Runnable<string>
?  - Result: string?                  ?  ? Type-safe result property
?  - WrappedView: TextField           ?  ? Access to original view
?    ?? TextField                     ?  ? Actual view being wrapped
?    ?? (result extraction logic)     ?  ? Extracted via lambda
???????????????????????????????????????
```

## Pattern Similarity: FlagSelector<T>

This pattern is identical to how `FlagSelector<TFlagsEnum>` works:

```csharp
// FlagSelector<T> wraps FlagSelector for type safety
public sealed class FlagSelector<TFlagsEnum> : FlagSelector where TFlagsEnum : struct, Enum
{
    public new TFlagsEnum? Value
    {
        get => base.Value.HasValue ? (TFlagsEnum)Enum.ToObject(typeof(TFlagsEnum), base.Value.Value) : null;
        set => base.Value = value.HasValue ? Convert.ToInt32(value.Value) : null;
    }
}

// RunnableWrapper<TView, TResult> wraps any View for runnability
#if POST_4148
public class RunnableWrapper<TView, TResult> : Runnable<TResult> where TView : View
{
    public TView WrappedView { get; }
    // Result comes from Runnable<TResult>
}
#endif
```

Both patterns:
- Wrap an existing type for additional functionality
- Provide type safety through generics
- Don't pollute the base type with unnecessary properties
- Allow opt-in usage

## Why Not Put `Result` on ViewBase?

### Problems with ViewBase.Result

? **Loss of type safety** - Would be `object? Result`, requiring casts everywhere  
? **Semantic mismatch** - Most Views are NOT runnable (Label, Button, etc.)  
? **API pollution** - Adds meaningless property to 95% of Views  
? **Lifecycle coupling** - `Result` is tied to runnable lifecycle, not view lifecycle  

### Benefits of Current Design

? **Type safety** - `IRunnable<TResult>` provides compile-time guarantees  
? **Clear intent** - Only runnable Views have `Result`  
? **Clean separation** - View is about UI, IRunnable is about sessions  
? **Flexible** - Use wrapper when needed, implement interface when permanent  

## Migration Guide

### Making Existing Code Use Wrappers

**Before (Current - Toplevel):**
```csharp
// Had to inherit from Toplevel and manually manage Result
public class MyDialog : Toplevel
{
    public string? Result { get; set; }
    private TextField _field;
    
    // Manually extract result in button handler
    okButton.Accepting += (s, e) => { Result = _field.Text; RequestStop(); };
}
```

**After (Phase 1 - RunnableWrapper):**
```csharp
#if POST_4148
// Just wrap the view directly
var result = app.RunView(
    new TextField { Width = 40 },
    tf => tf.Text);
#endif
```

### When to Keep Implementing IRunnable

Keep the traditional approach when:
- View is **always** runnable (Dialog, MessageBox, Wizard)
- Result extraction is complex or multi-step
- View needs custom lifecycle behavior
- View is part of the framework (Terminal.Gui library)

## Phase 1 Implementation Status

- ? `RunnableWrapper<TView, TResult>` class created
- ? `AsRunnable<TView, TResult>()` extension methods
- ? `IApplication.RunView<TView, TResult>()` extension methods
- ? Examples created (behind `POST_4148` guard)
- ? Waiting for Phase 1 merge to enable by default

## Summary

The `RunnableWrapper<TView, TResult>` pattern achieves the goal of **"making any View runnable"** without:
- Polluting `ViewBase` with `Result`
- Losing type safety
- Forcing inheritance from `Runnable<T>`
- Breaking existing code

This is the **have your cake and eat it too** solution!
