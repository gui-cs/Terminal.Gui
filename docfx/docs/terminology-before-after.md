# Before and After Comparison

## API Naming Comparison

### Current (Confusing) API

```csharp
// What is "Top"? Top of what?
Application.Top?.SetNeedsDraw();

// How does "Top" relate to "TopLevels"?
if (Application.TopLevels.Count > 0)
{
    var current = Application.Top;
}

// Is this the top view or something else?
var focused = Application.Top.MostFocused;
```

**Problems:**
- "Top" is ambiguous - top of what?
- Relationship between `Top` and `TopLevels` is unclear
- Doesn't convey that it's the "currently running" view

### Proposed (Clear) API

```csharp
// Immediately clear: the currently active view
Application.Current?.SetNeedsDraw();

// Clear relationship: Current is from the SessionStack
if (Application.SessionStack.Count > 0)
{
    var current = Application.Current;
}

// Self-documenting: working with the current view
var focused = Application.Current.MostFocused;
```

**Benefits:**
- `Current` is immediately understandable
- `SessionStack` describes both structure (stack) and content (running views)
- Clear relationship: `Current` is the top item in `SessionStack`

## Real-World Code Examples

### Example 1: Modal Dialog

**Before:**
```csharp
public static void ShowError(string message)
{
    var dialog = new Dialog("Error", message);
    Application.Run(dialog);
    
    // Wait, is Application.Top now the dialog or the original window?
    Application.Top?.SetNeedsDraw();
}
```

**After:**
```csharp
public static void ShowError(string message)
{
    var dialog = new Dialog("Error", message);
    Application.Run(dialog);
    
    // Clear: we're working with whatever is currently active
    Application.Current?.SetNeedsDraw();
}
```

### Example 2: Checking Active Views

**Before:**
```csharp
// Confusing: TopLevels vs Top
public bool HasModalDialog()
{
    return Application.TopLevels.Count > 1 
        && Application.Top?.Modal == true;
}
```

**After:**
```csharp
// Clear: multiple items in the SessionStack means we have modals/overlays
public bool HasModalDialog()
{
    return Application.SessionStack.Count > 1 
        && Application.Current?.Modal == true;
}
```

### Example 3: Refreshing the Screen

**Before:**
```csharp
// What does "Top" mean here? 
public void RefreshUI()
{
    Application.Top?.SetNeedsDraw();
    Application.Top?.LayoutSubviews();
}
```

**After:**
```csharp
// Clear: refresh the currently active view
public void RefreshUI()
{
    Application.Current?.SetNeedsDraw();
    Application.Current?.LayoutSubviews();
}
```

## Documentation Clarity

### XML Documentation Comparison

**Before:**
```csharp
/// <summary>The Toplevel that is currently active.</summary>
/// <value>The top.</value>
public static Toplevel? Top { get; }
```
*Question: What does "The top" mean? Top of what?*

**After:**
```csharp
/// <summary>
/// Gets the currently active view with its own run loop.
/// This is the view at the top of the <see cref="SessionStack"/>.
/// </summary>
/// <remarks>
/// The current view receives all keyboard and mouse input and is 
/// responsible for rendering its portion of the screen. When multiple 
/// views are running (e.g., dialogs over windows), this represents 
/// the topmost, active view.
/// </remarks>
public static Toplevel? Current { get; }
```
*Clear: Self-explanatory with helpful context*

## Consistency with .NET Ecosystem

### Similar Patterns in .NET

```csharp
// Threading
Thread.CurrentThread          // NOT Thread.Top
Thread.CurrentContext         // NOT Thread.TopContext

// Web Development  
HttpContext.Current           // NOT HttpContext.Top
SynchronizationContext.Current // NOT SynchronizationContext.Top

// Terminal.Gui (Proposed)
Application.Current           // Follows the pattern!
```

### Breaking the Pattern

If Terminal.Gui used the .NET pattern:
- `Application.CurrentToplevel` - Too verbose, redundant
- `Application.Current` - Perfect! Type provides the context

## Summary

| Aspect | Before (Top) | After (Current) |
|--------|--------------|-----------------|
| **Clarity** | Ambiguous | Clear |
| **Intuitiveness** | Requires explanation | Self-documenting |
| **Consistency** | Unique to Terminal.Gui | Follows .NET patterns |
| **Verbosity** | Short but unclear | Short and clear |
| **Learnability** | Requires documentation | Obvious from name |
| **Maintenance** | Confusing for new devs | Easy to understand |

## Conclusion

The proposed terminology (`Application.Current` and `Application.SessionStack`) provides:
- **Immediate clarity** without needing to read documentation
- **Consistency** with established .NET patterns
- **Better code readability** through self-documenting names
- **Easier maintenance** for both Terminal.Gui and applications using it

The old names (`Application.Top` and `Application.TopLevels`) will remain available during a deprecation period, ensuring backward compatibility while encouraging migration to the clearer terminology.
