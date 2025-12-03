# Heap Allocation Analysis for Terminal.Gui

## Executive Summary

This document provides a comprehensive analysis of intermediate heap allocations in Terminal.Gui, focusing on the `TextFormatter` and `LineCanvas` classes as reported in the issue.

## Severity Assessment: **HIGH IMPACT**

The allocation issues identified are significant performance concerns that affect:
- Every frame redraw in UI scenarios
- Any time-based updates (progress bars, timers, clocks)
- Text rendering operations
- Border and line drawing operations

## Key Findings

### 1. TextFormatter Class (`Terminal.Gui/Text/TextFormatter.cs`)

#### Critical Allocation Hotspots

**Location: Line 126 (in `Draw` method)**
```csharp
string[] graphemes = GraphemeHelper.GetGraphemes (strings).ToArray ();
```
- **Frequency**: Every time Draw is called (potentially 60+ times per second during animations)
- **Impact**: Allocates a new string array for every line being drawn
- **Called from**: View.Drawing.cs, Border.cs, TextField.cs, and other UI components

**Location: Line 934 (in `GetDrawRegion` method)**
```csharp
string [] graphemes = GraphemeHelper.GetGraphemes (strings).ToArray ();
```
- **Frequency**: Every time region calculation is needed
- **Impact**: Similar allocation for grapheme arrays

**Additional Allocation Points:**
- Line 1336: `List<string> graphemes = GraphemeHelper.GetGraphemes (text).ToList ();` in `SplitNewLine`
- Line 1407: `string [] graphemes = GraphemeHelper.GetGraphemes (text).ToArray ();` in `ClipOrPad`
- Line 1460: `List<string> graphemes = GraphemeHelper.GetGraphemes (StripCRLF (text)).ToList ();` in `WordWrapText`
- Line 1726: `List<string> graphemes = GraphemeHelper.GetGraphemes (text).ToList ();` in `ClipAndJustify`
- Line 2191: `string [] graphemes = GraphemeHelper.GetGraphemes (text).ToArray ();` in `GetSumMaxCharWidth`
- Line 2300: `string [] graphemes = GraphemeHelper.GetGraphemes (lines [lineIdx]).ToArray ();` in `GetMaxColsForWidth`

**Total Count**: 9 distinct allocation points in TextFormatter alone

#### Why This Matters

The `Draw` method is called:
1. On every frame update for animated content
2. When any view needs to redraw its text
3. During progress bar updates (the example mentioned in the issue)
4. For real-time displays (clocks, status bars)

With a typical progress bar updating at 10-30 times per second, and potentially multiple text elements on screen, this can result in **hundreds to thousands of allocations per second**.

### 2. LineCanvas Class (`Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs`)

#### Critical Allocation Hotspot

**Location: Lines 219-222 (in `GetMap(Rectangle inArea)` method)**
```csharp
IntersectionDefinition [] intersects = _lines
    .Select (l => l.Intersects (x, y))
    .OfType<IntersectionDefinition> ()
    .ToArray ();
```

- **Frequency**: **Once per pixel in the area** (nested loop over x and y)
- **Impact**: EXTREMELY HIGH - allocates array for every single pixel being evaluated
- **Example**: A 80x24 terminal window border = 1,920 allocations per redraw
- **Example**: A 120x40 dialog with borders = 4,800 allocations per redraw

#### Good News

The `GetCellMap()` method (line 162) was already optimized:
```csharp
List<IntersectionDefinition> intersectionsBufferList = [];
// ... reuses list with Clear() ...
ReadOnlySpan<IntersectionDefinition> intersects = CollectionsMarshal.AsSpan(intersectionsBufferList);
```

This is the **correct pattern** - reusing a buffer and using spans to avoid allocations.

### 3. Cell Class (`Terminal.Gui/Drawing/Cell.cs`)

**Location: Line 30**
```csharp
if (GraphemeHelper.GetGraphemes(value).ToArray().Length > 1)
```

- **Frequency**: Every time Grapheme property is set
- **Impact**: Moderate - validation code

### 4. GraphemeHelper Pattern

The core issue is that `GraphemeHelper.GetGraphemes()` returns an `IEnumerable<string>`, which is then immediately materialized to arrays or lists. This pattern appears throughout the codebase.

## Root Cause Analysis

### TextFormatter Allocations

The fundamental issue is the design pattern:
1. `GetGraphemes()` returns `IEnumerable<string>` (lazy enumeration)
2. Code immediately calls `.ToArray()` or `.ToList()` to materialize it
3. This happens on every draw call, creating garbage

### LineCanvas Allocations

The `GetMap(Rectangle inArea)` method has a particularly problematic nested loop structure:
- Outer loop: Y coordinates
- Inner loop: X coordinates
- **Inside inner loop**: LINQ query with `.ToArray()` allocation

This is a classic O(n²) allocation problem where the allocation count grows quadratically with area size.

## Performance Impact Estimation

### TextFormatter in Progress Demo

Assuming:
- Progress bar updates 20 times/second
- Each update redraws the bar (1 line) and percentage text (1 line)
- Each line calls `Draw()` which allocates an array

**Result**: 40 array allocations per second, just for the progress bar

Add a clock display updating once per second, status messages, etc., and we easily reach **hundreds of allocations per second** in a moderately complex UI.

### LineCanvas in Border Drawing

A typical dialog window:
- 100x30 character area
- Border needs to evaluate 2×(100+30) = 260 pixels for the border
- Each pixel: 1 array allocation

**Result**: 260 allocations per border redraw

If the dialog is redrawn 10 times per second (e.g., with animated content inside), that's **2,600 allocations per second** just for one border.

## Comparison to v2_develop Branch

The issue mentions that allocations "increased drastically" on the v2_develop branch, particularly from LineCanvas. This is consistent with the findings:

1. **GetMap(Rectangle)** method allocates per-pixel
2. If border drawing or line canvas usage increased in v2, this would multiply the allocation impact

## Memory Allocation Types

The allocations fall into several categories:

1. **String Arrays**: `string[]` from `.ToArray()`
2. **String Lists**: `List<string>` from `.ToList()`
3. **LINQ Enumerable Objects**: Intermediate enumerables in LINQ chains
4. **Dictionary/Collection Allocations**: Less critical but still present

## GC Impact

With Gen0 collections potentially happening multiple times per second due to these allocations:

1. **Pause times**: GC pauses affect UI responsiveness
2. **CPU overhead**: GC work consumes CPU that could render content
3. **Memory pressure**: Constant allocation/collection cycle
4. **Cache pollution**: Reduces cache effectiveness

## Recommended Solutions (High-Level)

### For TextFormatter

1. **Use ArrayPool<string>**: Rent arrays from pool instead of allocating
2. **Use Span<T>**: Work with spans instead of materializing arrays
3. **Cache grapheme arrays**: If text doesn't change, cache the split
4. **Lazy evaluation**: Only materialize when truly needed

### For LineCanvas

1. **Apply GetCellMap pattern to GetMap**: Reuse buffer list, use spans
2. **Pool IntersectionDefinition arrays**: Similar to GetCellMap optimization
3. **Consider pixel-level caching**: Cache intersection results for static lines

### For GraphemeHelper

1. **Add GetGraphemesAsSpan**: Return `ReadOnlySpan<string>` variant where possible
2. **Add TryGetGraphemeCount**: Count without allocation for validation
3. **Consider string pooling**: Pool common grapheme strings

## Measurement Recommendations

To quantify the impact:

1. **Add BenchmarkDotNet tests**: Measure allocations for typical scenarios
2. **Profile with dotnet-trace**: Capture allocation profiles during Progress demo
3. **Memory profiler**: Use Visual Studio or JetBrains dotMemory

## Severity by Scenario

| Scenario | Severity | Reason |
|----------|----------|--------|
| Static UI (no updates) | LOW | Allocations only on initial render |
| Progress bars / animations | **CRITICAL** | Continuous allocations 10-60 Hz |
| Text-heavy UI | **HIGH** | Many text elements = many allocations |
| Border-heavy UI | **HIGH** | Per-pixel allocations in LineCanvas |
| Simple forms | MEDIUM | Periodic allocations on interaction |

## Conclusion

The heap allocation issue is **real and significant**, particularly for:

1. **Any time-based updates** (progress bars, clocks, animations)
2. **Border/line-heavy UIs** due to LineCanvas per-pixel allocations
3. **Text-heavy interfaces** with frequent redraws

The good news is that the patterns for fixing this are well-established:
- ArrayPool usage
- Span<T> adoption  
- Buffer reuse (as demonstrated in GetCellMap)

The LineCanvas.GetMap() issue is particularly straightforward to fix by applying the same pattern already used in GetCellMap().

## Files Requiring Changes

Priority order:

1. **Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs** (GetMap method) - CRITICAL
2. **Terminal.Gui/Text/TextFormatter.cs** (Draw method) - CRITICAL
3. **Terminal.Gui/Text/TextFormatter.cs** (other allocation points) - HIGH
4. **Terminal.Gui/Drawing/Cell.cs** (validation) - MEDIUM
5. **Terminal.Gui/Drawing/GraphemeHelper.cs** (add span-based APIs) - MEDIUM

## Next Steps

Based on this analysis, the recommendation is to:

1. ✅ **Acknowledge the issue is real and significant**
2. Fix the most critical issue: LineCanvas.GetMap() per-pixel allocations
3. Fix TextFormatter.Draw() allocations  
4. Add benchmarks to measure improvement
5. Consider broader architectural changes for grapheme handling

---

**Analysis Date**: 2025-12-03  
**Analyzed By**: GitHub Copilot  
**Codebase**: Terminal.Gui (v2_develop branch)
