# GitHub Issue: Reduce View class memory footprint

**Title**: Reduce View class memory footprint (currently ~4-5 KB per instance)

---

## Problem

The `View` class currently has a memory footprint of **~4-5 KB per instance** (minimum ~4.2 KB typical). For applications with hundreds or thousands of views, this results in significant memory overhead:
- 1,000 Views = ~4.3 MB
- 10,000 Views = ~43 MB

## Current Memory Breakdown

| Category | Size (bytes) | % of Total |
|----------|--------------|------------|
| Adornments (Margin, Border, Padding) | 1500-3000 | 35-60% |
| Direct fields & properties | 650-900 | 15-18% |
| TextFormatter objects (2x) | 400-600 | 9-12% |
| Event handler fields (40+) | 320+ | 7-8% |
| Pos/Dim layout objects (4x) | 160-240 | 4-5% |
| KeyBindings objects (2x) | 160-300 | 4-6% |
| LineCanvas | 200-500 | 5-10% |
| Command dictionary | 100-200 | 2-4% |
| Collections overhead | 50-100 | 1-2% |

## Root Causes

1. **Adornments always created** - Margin, Border, and Padding are instantiated in constructor even when unused (35-60% of footprint)
2. **40+ EventHandler fields** - Allocated regardless of whether events are subscribed
3. **Duplicate TextFormatters** - One for Title, one for Text, always created
4. **Always-created infrastructure** - KeyBindings, LineCanvas, command dictionary created upfront
5. **Empty string overhead** - Default empty strings still consume memory

## Proposed Solution

See detailed plan: [view-memory-reduction-plan.md](https://github.com/gui-cs/Terminal.Gui/blob/claude/reduce-viewbase-memory-S8nN6/plans/view-memory-reduction-plan.md)

### Tier 1: Non-Breaking Optimizations (40% reduction)

**Expected savings: ~1000-2000 bytes per instance**

1. **Lazy-load Adornments** ⭐ HIGHEST PRIORITY
   - Create Margin, Border, Padding only when accessed
   - Saves: ~1500-3000 bytes (35-60% of total!)
   - Breaking: No

2. **Lazy-load LineCanvas**
   - Create only when drawing needs it
   - Saves: ~200-500 bytes
   - Breaking: No

3. **Lazy-load KeyBindings**
   - Create when first key binding is added
   - Saves: ~160-300 bytes
   - Breaking: No

4. **String interning**
   - Use `string.Empty` for default empty strings
   - Saves: ~72 bytes
   - Breaking: No

5. **Lazy command dictionary**
   - Create when first command is added
   - Saves: ~100-200 bytes
   - Breaking: No

### Tier 2: Minor Breaking Changes (10% additional reduction)

**Expected savings: ~200-450 bytes per instance**

1. **Event consolidation**
   - Use event broker pattern or dictionary storage
   - Saves: ~200-250 bytes
   - Breaking: Minor (internal change, may affect event subscription syntax)

2. **Lazy TextFormatter for Title**
   - Create only when Title is set
   - Saves: ~200-300 bytes
   - Breaking: Minor (timing change)

3. **Optimize Pos/Dim defaults**
   - Use null until explicitly set
   - Saves: ~160-240 bytes
   - Breaking: Minor

### Tier 3: Moderate Breaking Changes (15% additional reduction)

**Expected savings: ~450-700 bytes per instance**

1. **Separate ViewBase/View classes**
   - Lightweight `ViewBase` with core features only
   - Full-featured `View` with advanced features
   - Breaking: Moderate

2. **Feature flags system**
   - Enable/disable features at instance creation
   - Breaking: Moderate

3. **Shared scheme storage**
   - Use reference counting for schemes
   - Breaking: Minor

### Tier 4: Major Architectural Changes

**Expected savings: ~800-1800 bytes per instance**

1. Component-based architecture (ECS-style)
2. Flyweight pattern for common configurations
3. Memory pooling
4. Separate rendering from view model

## Expected Results

### After Tier 1 Implementation (Non-Breaking):
- Footprint: ~4.3 KB → ~2.5-3 KB (**30-40% reduction**)
- For 1,000 Views: ~4.3 MB → ~2.5 MB (**saves ~1.8 MB**)

### After Tier 2 Implementation (Minor Breaking):
- Footprint: ~2.5 KB → ~2-2.5 KB (**50%+ total reduction**)
- For 1,000 Views: ~2.5 MB → ~2 MB (**saves ~2.3 MB total**)

## Implementation Priority

1. **Phase 1**: Lazy-load Adornments (highest impact, non-breaking)
2. **Phase 2**: Lazy-load other infrastructure (LineCanvas, KeyBindings, etc.)
3. **Phase 3**: Event consolidation and TextFormatter optimization
4. **Phase 4**: Consider architectural changes for v3

## Success Criteria

- Reduce typical footprint by 40-50% without breaking changes
- All existing tests pass
- No performance regression in rendering or layout
- Maintain API compatibility in Tiers 1-2

## Related

Branch: `claude/reduce-viewbase-memory-S8nN6`
Detailed Plan: `/plans/view-memory-reduction-plan.md`

https://claude.ai/code/session_015D3RaBfwEY19M2ChrBYYkN
