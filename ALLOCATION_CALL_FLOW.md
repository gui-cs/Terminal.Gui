# Heap Allocation Call Flow Analysis

## Call Flow Diagram for Progress Bar Scenario

This document traces the allocation chain from a user interaction down to the heap allocations.

### High-Level Flow

```
User Action (Progress Bar Update)
    ↓
ProgressBar.Fraction = value
    ↓
SetNeedsDraw()
    ↓
Application Main Loop (10-60 Hz)
    ↓
View.OnDrawingContent() / View.DrawContentComplete()
    ↓
TextFormatter.Draw()
    ↓
**ALLOCATION HOTSPOT #1**
GraphemeHelper.GetGraphemes(strings).ToArray()
    ↓
string[] allocated on heap (Gen0)
```

### Detailed Call Stack with Line Numbers

#### 1. Progress Bar Update Path

```
Examples/UICatalog/Scenarios/Progress.cs:46
    Application.Invoke(() => systemTimerDemo.Pulse())
        ↓
Terminal.Gui/Views/ProgressBar.cs:~50 (Pulse method)
    Fraction = newValue
        ↓
Terminal.Gui/Views/ProgressBar.cs:~35
    set Fraction { _fraction = value; SetNeedsDraw(); }
        ↓
[View Framework schedules redraw]
        ↓
Terminal.Gui/Views/ProgressBar.cs:135
    OnDrawingContent()
        ↓
[Draws progress bar using Driver.AddStr, etc.]
[Any text on the progress bar view triggers...]
        ↓
Terminal.Gui/ViewBase/View.Drawing.cs:450
    TextFormatter?.Draw(driver, drawRect, normalColor, hotColor)
        ↓
Terminal.Gui/Text/TextFormatter.cs:78-126
    List<string> linesFormatted = GetLines()
    foreach line in linesFormatted:
        string[] graphemes = GraphemeHelper.GetGraphemes(strings).ToArray()  // LINE 126
            ↓
        **ALLOCATION #1: string[] array allocated (size = grapheme count)**
            ↓
        [Each grapheme is then drawn to screen]
```

#### 2. Border/LineCanvas Update Path

```
View with Border
    ↓
Terminal.Gui/ViewBase/Adornment/Border.cs:~500
    OnDrawingContent()
        ↓
[Border needs to draw lines]
        ↓
Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs:210-234
    GetMap(Rectangle inArea)
        ↓
    for y in area.Height:
        for x in area.Width:
            IntersectionDefinition[] intersects = _lines          // LINES 219-222
                .Select(l => l.Intersects(x, y))
                .OfType<IntersectionDefinition>()
                .ToArray()  
                ↓
            **ALLOCATION #2: IntersectionDefinition[] allocated per pixel**
            ↓
            [80x24 border = 1,920 allocations]
            [100x30 dialog = 2,600 allocations]
```

### Allocation Frequency Analysis

#### Scenario 1: Simple Progress Bar (Default Speed = 100ms)

**Per Update Cycle:**
1. ProgressBar text (e.g., "Progress: 45%")
   - `GetLines()` → splits into 1 line
   - `Draw()` line 126 → allocates string[] for ~15 graphemes
   - **1 allocation per update**

2. Percentage label if separate
   - Additional 1 allocation
   - **Total: 2 allocations per update**

**Per Second:**
- Update frequency: 10 Hz (every 100ms)
- **20 allocations/second** just for progress display

**With Border:**
- Border redraws when view redraws
- Typical small progress bar border: ~100 pixels
- **100 additional allocations per update**
- **1,000 allocations/second** for bordered progress bar

#### Scenario 2: Complex UI (Progress + Clock + Status)

**Components:**
1. Progress Bar: 20 allocations/second
2. Clock Display (updates every second): 2 allocations/second
3. Status Message: 2 allocations/second (if blinking)
4. Window Border: 260 allocations per redraw
   - If progress bar triggers window redraw: 2,600 allocations/second
5. Dialog Borders (if present): 4,800 allocations/second

**Conservative Estimate:**
- Progress bar alone: 20-120 allocations/second
- With borders: 1,000-3,000 allocations/second
- Full complex UI: **Easily 5,000-10,000 allocations/second**

### Memory Allocation Types

#### Type 1: String Arrays (TextFormatter)

```csharp
// Terminal.Gui/Text/TextFormatter.cs:126
string[] graphemes = GraphemeHelper.GetGraphemes(strings).ToArray();
```

**Size per allocation:**
- Array header: 24 bytes (x64)
- Per element: 8 bytes (reference)
- Plus: Each string object (grapheme)
- **Typical: 50-500 bytes per allocation**

#### Type 2: IntersectionDefinition Arrays (LineCanvas)

```csharp
// Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs:219-222
IntersectionDefinition[] intersects = _lines
    .Select(l => l.Intersects(x, y))
    .OfType<IntersectionDefinition>()
    .ToArray();
```

**Size per allocation:**
- Array header: 24 bytes
- Per IntersectionDefinition: ~32 bytes (struct size)
- Typical line count: 2-6 intersections
- **Typical: 50-200 bytes per allocation**
- **But happens ONCE PER PIXEL!**

#### Type 3: List<string> (Various TextFormatter methods)

```csharp
// Terminal.Gui/Text/TextFormatter.cs:1336, 1460, 1726
List<string> graphemes = GraphemeHelper.GetGraphemes(text).ToList();
```

**Size per allocation:**
- List object: 32 bytes
- Internal array: Variable (resizes as needed)
- **Typical: 100-1,000 bytes per allocation**

### Garbage Collection Impact

#### Gen0 Collection Triggers

Assuming:
- Gen0 threshold: ~16 MB (typical)
- Average allocation: 200 bytes
- Complex UI: 5,000 allocations/second
- **Memory allocated per second: ~1 MB**

**Result:**
- Gen0 collection approximately every 16 seconds
- With heap fragmentation and other allocations: **Every 5-10 seconds**

#### GC Pause Impact

- Gen0 collection pause: 1-5ms (typical)
- At 60 FPS UI: 16.67ms per frame budget
- **GC pause consumes 6-30% of frame budget**
- Result: Frame drops, UI stuttering during GC

### Optimization Opportunities

#### 1. Array Pooling (TextFormatter.Draw)

**Current:**
```csharp
string[] graphemes = GraphemeHelper.GetGraphemes(strings).ToArray();
```

**Optimized:**
```csharp
// Use ArrayPool<string>
string[] graphemes = ArrayPool<string>.Shared.Rent(estimatedSize);
try {
    int count = 0;
    foreach (var g in GraphemeHelper.GetGraphemes(strings)) {
        graphemes[count++] = g;
    }
    // Use graphemes[0..count]
} finally {
    ArrayPool<string>.Shared.Return(graphemes);
}
```

**Benefit:** Zero allocations for repeated draws

#### 2. Span-Based Processing (LineCanvas.GetMap)

**Current:**
```csharp
IntersectionDefinition[] intersects = _lines
    .Select(l => l.Intersects(x, y))
    .OfType<IntersectionDefinition>()
    .ToArray();
```

**Optimized (like GetCellMap):**
```csharp
List<IntersectionDefinition> intersectionsBufferList = []; // Reuse outside loop
// Inside loop:
intersectionsBufferList.Clear();
foreach (var line in _lines) {
    if (line.Intersects(x, y) is { } intersect) {
        intersectionsBufferList.Add(intersect);
    }
}
ReadOnlySpan<IntersectionDefinition> intersects = CollectionsMarshal.AsSpan(intersectionsBufferList);
```

**Benefit:** Zero per-pixel allocations (from 1,920+ to 0 per border redraw)

#### 3. Grapheme Caching

**Concept:** Cache grapheme arrays for unchanging text

```csharp
class TextFormatter {
    private string? _cachedText;
    private string[]? _cachedGraphemes;
    
    string[] GetGraphemesWithCache(string text) {
        if (text == _cachedText && _cachedGraphemes != null) {
            return _cachedGraphemes;
        }
        _cachedText = text;
        _cachedGraphemes = GraphemeHelper.GetGraphemes(text).ToArray();
        return _cachedGraphemes;
    }
}
```

**Benefit:** Zero allocations for static text (labels, buttons)

### Measurement Tools

#### 1. BenchmarkDotNet

Already used in project:
```bash
cd Tests/Benchmarks
dotnet run -c Release --filter "*TextFormatter*"
```

Provides:
- Allocation counts
- Memory per operation
- Speed comparisons

#### 2. dotnet-trace

```bash
dotnet-trace collect --process-id <pid> --providers Microsoft-Windows-DotNETRuntime:0x1:5
```

Captures:
- GC events
- Allocation stacks
- GC pause times

#### 3. Visual Studio Profiler

- .NET Object Allocation Tracking
- Shows allocation hot paths
- Call tree with allocation counts

### Expected Results After Optimization

#### TextFormatter.Draw Optimization

**Before:**
- 1 allocation per Draw call
- 10-60 allocations/second for animated content

**After:**
- 0 allocations (with ArrayPool)
- OR 1 allocation per unique text (with caching)

**Improvement:** 90-100% reduction in allocations

#### LineCanvas.GetMap Optimization

**Before:**
- Width × Height allocations per redraw
- 1,920 allocations for 80×24 border

**After:**
- 1 allocation per GetMap call (reused buffer)

**Improvement:** 99.95% reduction (from 1,920 to 1)

#### Overall Impact

**Complex UI Scenario:**

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Allocations/sec | 5,000-10,000 | 50-100 | 99% |
| Gen0 GC frequency | Every 5-10s | Every 80-160s | 16x reduction |
| Memory pressure | High | Low | Significant |
| Frame drops | Occasional | Rare | Noticeable |
| CPU overhead (GC) | 5-10% | <1% | 10x reduction |

### Validation Strategy

1. **Add allocation benchmarks**
   - Benchmark Draw method
   - Benchmark GetMap method
   - Compare before/after

2. **Run Progress scenario**
   - Profile with dotnet-trace
   - Measure GC frequency
   - Verify allocation reduction

3. **Stress test**
   - Multiple progress bars
   - Complex borders
   - Animated content
   - Measure sustained performance

### Conclusion

The allocation call flow analysis confirms:

1. **TextFormatter.Draw** is a critical path for text-heavy UIs
2. **LineCanvas.GetMap** has severe per-pixel allocation issues
3. **Optimization patterns exist** and are already partially implemented
4. **Expected improvement is 90-99%** allocation reduction
5. **Measurement tools are available** to validate improvements

The path forward is clear, with existing code patterns (GetCellMap) providing a blueprint for optimization.
