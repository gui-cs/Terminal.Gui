# Heap Allocation Optimization Recommendations

## Overview

This document provides actionable recommendations for addressing the intermediate heap allocation issues identified in Terminal.Gui, with specific priorities and implementation guidance.

## Priority Ranking

### P0 - Critical (Must Fix)

These issues cause severe performance degradation in common scenarios.

#### 1. LineCanvas.GetMap() Per-Pixel Allocations

**File:** `Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs`  
**Lines:** 210-234  
**Impact:** 1,920+ allocations per border redraw (80×24 window)

**Problem:**
```csharp
for (int y = inArea.Y; y < inArea.Y + inArea.Height; y++) {
    for (int x = inArea.X; x < inArea.X + inArea.Width; x++) {
        IntersectionDefinition[] intersects = _lines
            .Select(l => l.Intersects(x, y))
            .OfType<IntersectionDefinition>()
            .ToArray();  // ❌ ALLOCATES EVERY PIXEL
    }
}
```

**Solution:** Apply the same pattern already used in `GetCellMap()`:
```csharp
public Dictionary<Point, Rune> GetMap (Rectangle inArea)
{
    Dictionary<Point, Rune> map = new ();
    List<IntersectionDefinition> intersectionsBufferList = [];

    for (int y = inArea.Y; y < inArea.Y + inArea.Height; y++)
    {
        for (int x = inArea.X; x < inArea.X + inArea.Width; x++)
        {
            intersectionsBufferList.Clear();
            foreach (var line in _lines)
            {
                if (line.Intersects(x, y) is { } intersect)
                {
                    intersectionsBufferList.Add(intersect);
                }
            }
            ReadOnlySpan<IntersectionDefinition> intersects = CollectionsMarshal.AsSpan(intersectionsBufferList);
            Rune? rune = GetRuneForIntersects(intersects);

            if (rune is { } && _exclusionRegion?.Contains(x, y) is null or false)
            {
                map.Add(new (x, y), rune.Value);
            }
        }
    }
    return map;
}
```

**Expected Improvement:**
- From 1,920 allocations → 1 allocation per redraw (99.95% reduction)
- From 4,800 allocations → 1 allocation for 100×30 dialog
- Immediate, measurable performance gain

**Effort:** Low (pattern already exists in same class)  
**Risk:** Very Low (straightforward refactoring)

---

#### 2. TextFormatter.Draw() Grapheme Array Allocation

**File:** `Terminal.Gui/Text/TextFormatter.cs`  
**Lines:** 126, 934  
**Impact:** Every text draw operation (10-60+ times/second for animated content)

**Problem:**
```csharp
public void Draw(...) {
    // ...
    for (int line = lineOffset; line < linesFormatted.Count; line++) {
        string strings = linesFormatted[line];
        string[] graphemes = GraphemeHelper.GetGraphemes(strings).ToArray(); // ❌ EVERY DRAW
        // ...
    }
}
```

**Solution Options:**

**Option A: ArrayPool (Immediate Fix)**
```csharp
using System.Buffers;

public void Draw(...) {
    for (int line = lineOffset; line < linesFormatted.Count; line++) {
        string strings = linesFormatted[line];
        
        // Estimate or calculate grapheme count
        int estimatedCount = strings.Length + 10; // Add buffer for safety
        string[] graphemes = ArrayPool<string>.Shared.Rent(estimatedCount);
        int actualCount = 0;
        
        try {
            foreach (string grapheme in GraphemeHelper.GetGraphemes(strings)) {
                if (actualCount >= graphemes.Length) {
                    // Need larger array (rare)
                    string[] larger = ArrayPool<string>.Shared.Rent(graphemes.Length * 2);
                    Array.Copy(graphemes, larger, actualCount);
                    ArrayPool<string>.Shared.Return(graphemes);
                    graphemes = larger;
                }
                graphemes[actualCount++] = grapheme;
            }
            
            // Use graphemes[0..actualCount]
            // ... rest of draw logic ...
            
        } finally {
            ArrayPool<string>.Shared.Return(graphemes, clearArray: true);
        }
    }
}
```

**Option B: Caching (Best for Static Text)**
```csharp
private Dictionary<string, string[]> _graphemeCache = new();
private const int MaxCacheSize = 100;

private string[] GetGraphemesWithCache(string text) {
    if (_graphemeCache.TryGetValue(text, out string[]? cached)) {
        return cached;
    }
    
    string[] graphemes = GraphemeHelper.GetGraphemes(text).ToArray();
    
    if (_graphemeCache.Count >= MaxCacheSize) {
        // Simple LRU: clear cache
        _graphemeCache.Clear();
    }
    
    _graphemeCache[text] = graphemes;
    return graphemes;
}
```

**Expected Improvement:**
- ArrayPool: Zero allocations for all draws
- Caching: Zero allocations for repeated text (buttons, labels)
- 90-100% reduction in TextFormatter allocations

**Effort:** Medium  
**Risk:** Medium (requires careful array size handling)

**Recommendation:** Start with Option A (ArrayPool) as it's more robust

---

### P1 - High Priority

These issues have significant impact in specific scenarios.

#### 3. TextFormatter Helper Methods

**Files:** `Terminal.Gui/Text/TextFormatter.cs`  
**Lines:** 1336, 1407, 1460, 1726, 2191, 2300

**Problem:** Multiple helper methods allocate grapheme arrays/lists

**Affected Methods:**
- `SplitNewLine()` - Line 1336
- `ClipOrPad()` - Line 1407  
- `WordWrapText()` - Line 1460
- `ClipAndJustify()` - Line 1726
- `GetSumMaxCharWidth()` - Line 2191
- `GetMaxColsForWidth()` - Line 2300

**Solution:** 
1. Add overloads that accept `Span<string>` for graphemes
2. Use ArrayPool in calling code
3. Pass pooled arrays to helper methods

**Example:**
```csharp
// New overload
public static string ClipOrPad(ReadOnlySpan<string> graphemes, int width) {
    // Work with span, no allocation
}

// Caller uses ArrayPool
string[] graphemes = ArrayPool<string>.Shared.Rent(estimatedSize);
try {
    int count = FillGraphemes(text, graphemes);
    result = ClipOrPad(graphemes.AsSpan(0, count), width);
} finally {
    ArrayPool<string>.Shared.Return(graphemes);
}
```

**Expected Improvement:** 70-90% reduction in text formatting allocations

**Effort:** High (multiple methods to update)  
**Risk:** Medium (API changes, need careful span handling)

---

#### 4. Cell.Grapheme Validation

**File:** `Terminal.Gui/Drawing/Cell.cs`  
**Line:** 30

**Problem:**
```csharp
if (GraphemeHelper.GetGraphemes(value).ToArray().Length > 1)
```

**Solution:**
```csharp
// Add helper to GraphemeHelper
public static int GetGraphemeCount(string text) {
    if (string.IsNullOrEmpty(text)) return 0;
    
    int count = 0;
    TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
    while (enumerator.MoveNext()) {
        count++;
    }
    return count;
}

// In Cell.cs
if (GraphemeHelper.GetGraphemeCount(value) > 1)
```

**Expected Improvement:** Zero allocations for Cell.Grapheme validation

**Effort:** Low  
**Risk:** Very Low

---

### P2 - Medium Priority

Performance improvements for less frequent code paths.

#### 5. GraphemeHelper API Improvements

**File:** `Terminal.Gui/Drawing/GraphemeHelper.cs`

**Additions:**
```csharp
/// <summary>Counts graphemes without allocation</summary>
public static int GetGraphemeCount(string text);

/// <summary>Fills a span with graphemes, returns actual count</summary>
public static int GetGraphemes(string text, Span<string> destination);

/// <summary>Gets graphemes with a rented array from pool</summary>
public static (string[] array, int count) GetGraphemesPooled(string text);
```

**Benefit:** Provides allocation-free alternatives for all callers

**Effort:** Medium  
**Risk:** Low (additive changes, doesn't break existing API)

---

### P3 - Nice to Have

Optimizations for edge cases or less common scenarios.

#### 6. GetDrawRegion Optimization

**File:** `Terminal.Gui/Text/TextFormatter.cs`  
**Line:** 934

Similar allocation as Draw method. Apply same ArrayPool pattern.

**Effort:** Low (copy Draw optimization)  
**Risk:** Low

---

## Implementation Roadmap

### Phase 1: Quick Wins (1-2 days)

1. ✅ Fix LineCanvas.GetMap() per-pixel allocations
2. ✅ Add GraphemeHelper.GetGraphemeCount()
3. ✅ Fix Cell.Grapheme validation
4. ✅ Add basic benchmarks for measuring improvement

**Expected Result:** 
- Eliminate border/line drawing allocations (99%+ reduction)
- Baseline performance metrics established

### Phase 2: Core Optimization (3-5 days)

1. ✅ Implement ArrayPool in TextFormatter.Draw()
2. ✅ Add comprehensive unit tests
3. ✅ Update GetDrawRegion() similarly
4. ✅ Run Progress scenario profiling
5. ✅ Validate allocation reduction

**Expected Result:**
- TextFormatter allocations reduced 90-100%
- All high-frequency code paths optimized

### Phase 3: Helpers & API (5-7 days)

1. ✅ Add GraphemeHelper span-based APIs
2. ✅ Update TextFormatter helper methods
3. ✅ Add optional caching for static text
4. ✅ Full test coverage
5. ✅ Update documentation

**Expected Result:**
- Complete allocation-free text rendering path
- Public APIs available for consumers

### Phase 4: Validation & Documentation (2-3 days)

1. ✅ Run full benchmark suite
2. ✅ Profile Progress demo with dotnet-trace
3. ✅ Compare before/after metrics
4. ✅ Update performance documentation
5. ✅ Create migration guide for API changes

**Expected Result:**
- Quantified performance improvements
- Clear documentation for maintainers

**Total Time Estimate:** 2-3 weeks for complete implementation

---

## Testing Strategy

### Unit Tests

```csharp
[Theory]
[InlineData("Hello")]
[InlineData("Hello\nWorld")]
[InlineData("Emoji: 👨‍👩‍👧‍👦")]
public void Draw_NoAllocations_WithArrayPool(string text)
{
    var formatter = new TextFormatter { Text = text };
    var driver = new FakeDriver();
    
    // Get baseline allocations
    long before = GC.GetAllocatedBytesForCurrentThread();
    
    formatter.Draw(driver, new Rectangle(0, 0, 100, 10), 
                   Attribute.Default, Attribute.Default);
    
    long after = GC.GetAllocatedBytesForCurrentThread();
    long allocated = after - before;
    
    // Should be zero or minimal (some overhead acceptable)
    Assert.True(allocated < 1000, $"Allocated {allocated} bytes");
}
```

### Benchmarks

```csharp
[MemoryDiagnoser]
[BenchmarkCategory("TextFormatter")]
public class DrawBenchmark
{
    private TextFormatter _formatter;
    private FakeDriver _driver;
    
    [GlobalSetup]
    public void Setup()
    {
        _formatter = new TextFormatter { 
            Text = "Progress: 45%",
            ConstrainToWidth = 80,
            ConstrainToHeight = 1
        };
        _driver = new FakeDriver();
    }
    
    [Benchmark]
    public void DrawProgressText()
    {
        _formatter.Draw(_driver, 
                       new Rectangle(0, 0, 80, 1),
                       Attribute.Default, 
                       Attribute.Default);
    }
}
```

Expected results:
- **Before:** ~500-1000 bytes allocated per draw
- **After:** 0-100 bytes allocated per draw

### Performance Testing

```bash
# Run benchmarks
cd Tests/Benchmarks
dotnet run -c Release --filter "*TextFormatter*"

# Profile with dotnet-trace
dotnet-trace collect --process-id <pid> \
    --providers Microsoft-Windows-DotNETRuntime:0x1:5

# Analyze in PerfView or VS
```

### Integration Testing

Run Progress scenario for 60 seconds:
- Monitor GC collections
- Track allocation rate
- Measure frame times
- Compare before/after

---

## Breaking Changes

### None for Phase 1-2

All optimizations are internal implementation changes.

### Possible for Phase 3

If adding span-based APIs:
- New overloads (non-breaking)
- Possible deprecation of allocation-heavy methods

**Mitigation:** Use `[Obsolete]` attributes with migration guidance

---

## Documentation Updates Required

1. **CONTRIBUTING.md**
   - Add section on allocation-aware coding
   - ArrayPool usage guidelines
   - Span<T> patterns

2. **Performance.md** (new file)
   - Allocation best practices
   - Profiling guide
   - Benchmark results

3. **API Documentation**
   - Document allocation behavior
   - Note pooled array usage
   - Warn about concurrent access (if using pooling)

---

## Monitoring & Validation

### Success Metrics

| Metric | Baseline | Target | Measurement |
|--------|----------|--------|-------------|
| Allocations/sec (Progress) | 1,000-5,000 | <100 | Profiler |
| Gen0 GC frequency | Every 5-10s | Every 60s+ | GC.CollectionCount() |
| Frame drops (animated UI) | Occasional | Rare | Frame time monitoring |
| Memory usage (sustained) | Growing | Stable | dotnet-counters |

### Regression Testing

Add to CI pipeline:
```yaml
- name: Run allocation benchmarks
  run: |
    cd Tests/Benchmarks
    dotnet run -c Release --filter "*Allocation*" --exporters json
    # Parse JSON and fail if allocations exceed baseline
```

---

## Risk Assessment

### Low Risk
- LineCanvas.GetMap() fix (proven pattern exists)
- Cell validation fix (simple change)
- Adding new helper methods (additive)

### Medium Risk
- TextFormatter.Draw() with ArrayPool (complex control flow)
- Span-based API additions (need careful API design)

### Mitigation
- Comprehensive unit tests
- Gradual rollout (behind feature flag if needed)
- Extensive profiling before merge

---

## Alternative Approaches Considered

### 1. String Interning
**Pros:** Reduces string allocations  
**Cons:** Doesn't solve array allocations, memory pressure  
**Decision:** Not suitable for dynamic content

### 2. Custom Grapheme Iterator
**Pros:** Ultimate control, zero allocations  
**Cons:** Complex to implement, maintain  
**Decision:** ArrayPool is simpler, achieves same goal

### 3. Code Generation
**Pros:** Compile-time optimization  
**Cons:** Overkill for this problem  
**Decision:** Runtime optimization sufficient

---

## Conclusion

The optimization strategy is:

1. **Clear and Actionable** - Specific files, lines, and solutions
2. **Proven Patterns** - Uses existing patterns (GetCellMap) and standard tools (ArrayPool)
3. **Measurable** - Clear metrics and testing strategy
4. **Low Risk** - Gradual implementation with extensive testing
5. **High Impact** - 90-99% allocation reduction in critical paths

**Recommended First Step:** Fix LineCanvas.GetMap() as proof of concept (4-8 hours of work, massive impact)

---

## Questions or Feedback?

For implementation questions, consult:
- HEAP_ALLOCATION_ANALYSIS.md - Detailed problem analysis
- ALLOCATION_CALL_FLOW.md - Call flow and measurement details
- This document - Implementation roadmap

**Next Action:** Review with team and prioritize Phase 1 implementation.
