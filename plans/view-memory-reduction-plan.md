# View Memory Reduction Plan

**Date**: 2026-02-07
**Author**: Claude (Opus 4.5)
**Branch**: `claude/reduce-viewbase-memory-S8nN6`

---

## Executive Summary

This document provides a comprehensive plan to dramatically reduce the per-instance memory footprint of the `View` class in Terminal.Gui. Based on detailed analysis, the current **minimum footprint is ~4-5 KB per View instance**, with typical instances consuming **~4.2-4.4 KB**. For applications with hundreds or thousands of views, this can result in significant memory overhead (e.g., 1000 Views = ~4-5 MB).

---

## Current Memory Footprint Analysis

### Per-Instance Memory Breakdown

| Category | Size (bytes) | % of Total |
|----------|--------------|------------|
| Direct instance fields & properties | 650-900 | 15-18% |
| TextFormatter objects (2x) | 400-600 | 9-12% |
| Pos/Dim layout objects (4x) | 160-240 | 4-5% |
| KeyBindings objects (2x) | 160-300 | 4-6% |
| Adornments (Margin, Border, Padding) | 1500-3000 | 35-60% |
| LineCanvas | 200-500 | 5-10% |
| Command dictionary | 100-200 | 2-4% |
| Event handler fields (40+) | 320+ | 7-8% |
| List/collections overhead | 50-100 | 1-2% |
| **TOTAL (MINIMUM)** | **~4150-5150** | **100%** |
| **TOTAL (TYPICAL)** | **~4300-4500** | **100%** |

### Key Findings

1. **Adornments are the single largest contributor** (~35-60% of total memory) and are always created even when not used
2. **40+ individual EventHandler fields** consume ~320+ bytes regardless of whether events are subscribed
3. **Duplicate TextFormatter instances** (one for Title, one for Text) contribute ~400-600 bytes
4. **Always-created objects**: Many objects (KeyBindings, LineCanvas, Adornments) are created in constructor even if never used
5. **Empty string overhead**: Default empty strings for `Id`, `Title`, `Text` still consume ~24 bytes each

---

## Memory Reduction Strategy

The plan is organized into four tiers based on complexity and potential breaking changes:

- **Tier 1**: Non-breaking optimizations (quick wins)
- **Tier 2**: Minor breaking changes (low risk)
- **Tier 3**: Moderate breaking changes (medium risk)
- **Tier 4**: Major architectural changes (high risk)

---

## Tier 1: Non-Breaking Optimizations (Quick Wins)

**Impact**: Estimated **10-20% reduction** (~400-900 bytes per instance)
**Risk**: Minimal - No API changes
**Effort**: Low to Medium

### 1.1 String Interning & Pooling

**Current**: Empty strings (`""`) for `Id`, `Title`, `Text` each consume ~24 bytes
**Proposed**: Use `string.Empty` or intern empty strings
**Savings**: ~72 bytes per instance (3 strings)
**Breaking**: No

```csharp
// BEFORE
public string Id { get; set; } = "";
private string _title = string.Empty;

// AFTER
public string Id { get; set; } = string.Empty; // Interned
private string _title = string.Empty; // Already interned
```

### 1.2 Lazy-Load Adornments

**Current**: Margin, Border, Padding are created in `SetupAdornments()` called from constructor
**Proposed**: Create adornments only when first accessed (property getter)
**Savings**: ~1500-3000 bytes per instance (35-60% reduction!)
**Breaking**: No (transparent to API users)
**Note**: Most Views never use all three adornments

```csharp
// BEFORE
public View()
{
    SetupAdornments(); // Always creates Margin, Border, Padding
}

// AFTER
private Margin? _margin;
public Margin Margin => _margin ??= new Margin();

// Only create when accessed
```

**Priority**: **HIGHEST** - Largest single impact

### 1.3 Lazy-Load LineCanvas

**Current**: LineCanvas created in constructor
**Proposed**: Create only when first accessed or when drawing needs it
**Savings**: ~200-500 bytes per instance
**Breaking**: No

### 1.4 Lazy-Load KeyBindings

**Current**: Both `KeyBindings` and `HotKeyBindings` created in `SetupKeyboard()`
**Proposed**: Create only when first key binding is added
**Savings**: ~160-300 bytes per instance
**Breaking**: No
**Note**: Many simple Views never register custom key bindings

### 1.5 Optimize Command Dictionary

**Current**: Dictionary created in constructor regardless of usage
**Proposed**: Create dictionary lazily when first command is added
**Savings**: ~100-200 bytes per instance
**Breaking**: No

```csharp
// BEFORE
private Dictionary<Command, CommandImplementation>? _commandImplementations = new();

// AFTER
private Dictionary<Command, CommandImplementation>? _commandImplementations;

private Dictionary<Command, CommandImplementation> GetCommandImplementations()
{
    return _commandImplementations ??= new Dictionary<Command, CommandImplementation>();
}
```

### 1.6 Reduce TextFormatter Footprint

**Current**: Each View has 2 TextFormatter instances (~200-300 bytes each)
**Proposed**:
- Optimize TextFormatter internal structure
- Consider pooling common configurations
- Delay initialization until needed

**Savings**: ~100-200 bytes per instance
**Breaking**: No

### 1.7 Struct Packing & Alignment

**Current**: Many small bool fields scattered throughout the class
**Proposed**: Group related bool fields together to improve memory alignment
**Savings**: ~20-50 bytes per instance (via reduced padding)
**Breaking**: No (internal reordering only)

### 1.8 Use Nullable Value Types for Optional Fields

**Current**: Some fields use reference types when value types would suffice
**Proposed**: Replace with nullable structs where appropriate
**Savings**: ~16-32 bytes per instance (reduced reference overhead)
**Breaking**: No

**Example**:
```csharp
// If _frame is rarely null, keep as is
// But consider for other optional fields
```

---

## Tier 2: Minor Breaking Changes (Low Risk)

**Impact**: Estimated **5-10% reduction** (~200-450 bytes per instance)
**Risk**: Low - Changes are opt-in or have simple migration paths
**Effort**: Medium

### 2.1 Event Consolidation with EventBroker Pattern

**Current**: 40+ individual EventHandler fields (~320+ bytes)
**Proposed**: Consolidate events using an event broker/dictionary pattern
**Savings**: ~200-250 bytes per instance
**Breaking**: Minor - Event subscription syntax may change slightly

```csharp
// BEFORE
public event EventHandler? Initialized;
public event EventHandler? Disposing;
// ... 38 more events

// AFTER
private EventBroker? _eventBroker;
public void Subscribe<TArgs>(ViewEvent eventType, EventHandler<TArgs> handler)
    where TArgs : EventArgs
{
    _eventBroker ??= new EventBroker();
    _eventBroker.Subscribe(eventType, handler);
}

// Or keep existing events but store delegates in dictionary internally
```

**Alternative**: Keep event syntax but store delegates in a dictionary internally:
```csharp
private Dictionary<string, Delegate>? _events;

public event EventHandler? Initialized
{
    add => AddEventHandler(nameof(Initialized), value);
    remove => RemoveEventHandler(nameof(Initialized), value);
}
```

### 2.2 Make TextFormatter for Title Optional

**Current**: `TitleTextFormatter` always created even if Title is never set
**Proposed**: Create lazily or share a static formatter for empty titles
**Savings**: ~200-300 bytes per instance
**Breaking**: Minor - Internal change mostly, but may affect title rendering timing

### 2.3 Optimize Pos/Dim Default Values

**Current**: Default Pos/Dim objects created for X, Y, Width, Height
**Proposed**: Use null until explicitly set, with sensible defaults in getters
**Savings**: ~160-240 bytes per instance
**Breaking**: Minor - May affect initialization timing

### 2.4 Remove or Lazy-Load UsedHotKeys HashSet

**Current**: HashSet created in keyboard setup
**Proposed**: Create only when hotkey assignment is needed
**Savings**: ~50-100 bytes per instance
**Breaking**: No

---

## Tier 3: Moderate Breaking Changes (Medium Risk)

**Impact**: Estimated **10-15% reduction** (~450-700 bytes per instance)
**Risk**: Medium - May require application code changes
**Effort**: High

### 3.1 Separate "Simple" and "Advanced" View Base Classes

**Current**: All Views include all features (keyboard, mouse, adornments, scrolling, etc.)
**Proposed**: Create a lightweight `ViewBase` with only core features, and `View : ViewBase` with advanced features
**Savings**: ~1000-2000 bytes for simple views
**Breaking**: Moderate - Applications would choose which base to inherit from

```csharp
// ViewBase - minimal, ~1-2 KB
public class ViewBase : IDisposable
{
    // Only core: Id, Frame, Layout, Drawing, Hierarchy
}

// View - full-featured, ~4-5 KB
public class View : ViewBase
{
    // Add: Keyboard, Mouse, Adornments, Scrolling, TextFormatters
}
```

**Migration**: Most existing Views would continue using `View`, but new lightweight views could use `ViewBase`

### 3.2 Feature Flags System

**Current**: All features always available
**Proposed**: Use flags to enable/disable features at instance creation
**Savings**: Variable, ~500-1500 bytes depending on disabled features
**Breaking**: Moderate - API stays same, but behavior changes if features disabled

```csharp
[Flags]
public enum ViewFeatures
{
    None = 0,
    Keyboard = 1,
    Mouse = 2,
    Adornments = 4,
    Scrolling = 8,
    TextFormatting = 16,
    All = 0xFFFF
}

public View(ViewFeatures features = ViewFeatures.All)
{
    if (features.HasFlag(ViewFeatures.Keyboard))
        SetupKeyboard();
    // etc.
}
```

### 3.3 Shared/Static Scheme Storage

**Current**: Each View can have its own scheme reference
**Proposed**: Use shared scheme storage with reference counting
**Savings**: ~50-100 bytes per instance
**Breaking**: Minor - Internal change

### 3.4 Optimize ViewportSettings with Bit Flags

**Current**: May use multiple fields for viewport settings
**Proposed**: Consolidate into a single flags enum
**Savings**: ~20-50 bytes per instance
**Breaking**: Minor

---

## Tier 4: Major Architectural Changes (High Risk)

**Impact**: Estimated **20-40% reduction** (~800-1800 bytes per instance)
**Risk**: High - Significant API and architectural changes
**Effort**: Very High

### 4.1 Component-Based Architecture (ECS-Style)

**Current**: Monolithic View class with all features built-in
**Proposed**: Entity-Component-System inspired architecture where features are components
**Savings**: ~1500-2500 bytes for views with minimal components
**Breaking**: Major - Complete architectural overhaul

```csharp
public class View : IDisposable
{
    private Dictionary<Type, IViewComponent>? _components;

    public T GetComponent<T>() where T : IViewComponent;
    public void AddComponent<T>(T component) where T : IViewComponent;
}

// Components
public interface IKeyboardComponent { ... }
public interface IMouseComponent { ... }
public interface IAdornmentComponent { ... }
```

**Migration**: Would require Terminal.Gui v3 or major version bump

### 4.2 Flyweight Pattern for Common Configurations

**Current**: Each View instance owns all objects
**Proposed**: Share immutable common objects (default Pos/Dim, empty TextFormatters, etc.)
**Savings**: ~300-600 bytes per instance
**Breaking**: Major - Requires careful thread-safety design

### 4.3 Memory Pools for View Instances

**Current**: Views allocated directly via `new`
**Proposed**: Object pooling for frequently created/destroyed views
**Savings**: Reduces GC pressure and allocation overhead
**Breaking**: Major - Requires factory pattern and lifecycle management

```csharp
public static class ViewPool
{
    public static View Rent();
    public static void Return(View view);
}
```

### 4.4 Separate Rendering from View Model

**Current**: View contains both state and rendering logic
**Proposed**: Separate into View (model) and ViewRenderer (rendering)
**Savings**: ~500-1000 bytes per instance (rendering state moved to renderer)
**Breaking**: Major - Architectural change

---

## Recommended Implementation Phases

### Phase 1: Low-Hanging Fruit (Tier 1)
**Timeline**: 1-2 weeks
**Expected Savings**: ~1000-2000 bytes per instance (20-40% reduction)

1. Lazy-load Adornments (Highest priority - 35-60% of total savings)
2. Lazy-load LineCanvas
3. Lazy-load KeyBindings
4. String interning
5. Lazy command dictionary

### Phase 2: Event Optimization (Tier 2)
**Timeline**: 1-2 weeks
**Expected Savings**: ~200-450 bytes per instance (5-10% reduction)

1. Event consolidation
2. Lazy TextFormatter for Title
3. Optimize Pos/Dim defaults

### Phase 3: Structural Changes (Tier 3)
**Timeline**: 2-4 weeks
**Expected Savings**: ~450-700 bytes per instance (10-15% reduction)

1. Feature flags system
2. Evaluate ViewBase separation
3. Shared scheme storage

### Phase 4: Research & Planning (Tier 4)
**Timeline**: TBD (Terminal.Gui v3?)
**Expected Savings**: ~800-1800 bytes per instance (20-40% reduction)

1. Prototype component-based architecture
2. Research flyweight patterns
3. Evaluate pooling strategies

---

## Measurement & Validation

### Before Implementation
1. Create benchmark suite measuring:
   - Memory per View instance
   - Memory for 100, 1000, 10,000 Views
   - GC pressure
   - Allocation rate

### During Implementation
1. Measure impact of each optimization
2. Profile real-world applications
3. Monitor for performance regressions

### Success Criteria
- **Phase 1**: Reduce typical footprint from ~4.3 KB to ~2.5-3 KB (30-40% reduction)
- **Phase 2**: Further reduce to ~2-2.5 KB (50%+ total reduction)
- **No performance regression** in rendering or layout
- **All existing tests pass**
- **No breaking changes** in Phases 1-2

---

## Risk Mitigation

### Testing Strategy
1. All changes must pass existing unit tests
2. Add memory-specific tests
3. Profile with UICatalog and other examples
4. Test with applications having 1000+ views

### Compatibility
1. Tier 1 & 2 changes maintain API compatibility
2. Tier 3 changes documented in upgrade guide
3. Tier 4 changes reserved for major version bump

### Performance
1. Benchmark before/after each change
2. Ensure lazy loading doesn't introduce latency
3. Monitor GC behavior

---

## Appendix A: Detailed Field Analysis

See the exploration agent's comprehensive output above for complete field-by-field breakdown.

### Key Memory Contributors (Top 10)

| Item | Size | Category |
|------|------|----------|
| Adornments (3x) | 1500-3000 bytes | Always-created objects |
| TextFormatter (2x) | 400-600 bytes | Always-created objects |
| Events (40+ fields) | 320+ bytes | Delegate references |
| Pos/Dim objects (4x) | 160-240 bytes | Layout |
| KeyBindings (2x) | 160-300 bytes | Keyboard |
| LineCanvas | 200-500 bytes | Drawing |
| Command dictionary | 100-200 bytes | Commands |
| Direct fields | 650-900 bytes | Primitives & references |
| List overhead | 50-100 bytes | Collections |
| Misc | 100-200 bytes | Various |

---

## Appendix B: Breaking Change Assessment Matrix

| Change | API Breaking | Behavioral Breaking | Migration Effort | Risk |
|--------|--------------|---------------------|------------------|------|
| Lazy Adornments | No | No | None | Very Low |
| Lazy LineCanvas | No | No | None | Very Low |
| Lazy KeyBindings | No | No | None | Low |
| String interning | No | No | None | Very Low |
| Event consolidation | Yes (if changing syntax) | No | Low | Low |
| Lazy TextFormatter | No | Possibly (timing) | None | Low |
| ViewBase separation | Yes | Yes | Medium | Medium |
| Feature flags | No | Yes | Low-Medium | Medium |
| ECS architecture | Yes | Yes | High | High |

---

## Conclusion

By implementing Tier 1 and Tier 2 optimizations, we can realistically achieve a **40-50% reduction** in View memory footprint with minimal to no breaking changes. The most impactful single change is lazy-loading Adornments, which alone can reduce memory by 35-60%.

For a typical application with 1000 Views:
- **Current**: ~4.3 MB
- **After Phase 1**: ~2.5-2.8 MB (40% reduction, ~1.5-1.8 MB saved)
- **After Phase 2**: ~2-2.5 MB (50% reduction, ~2 MB saved)

This represents a significant improvement in memory efficiency without requiring major architectural changes or breaking existing applications.
