# Plan for drawing.md

- Document: `/home/runner/work/Terminal.Gui/Terminal.Gui/docfx/docs/drawing.md`
- Current size: 400 lines

## Current Signals
- Em-dashes: 27
- Horizontal separators (`---`): 0
- Mermaid blocks: 0
- xref markers: 54
- C# code blocks: 6

## Planned Actions
- [ ] Verify technical claims against source code in: `/home/runner/work/Terminal.Gui/Terminal.Gui/Terminal.Gui/Drawing`.
- [ ] Remove repetition and add missing advanced details only where they are not obvious from API names.
- [ ] Normalize heading structure, bullet style, and grammar to match other deep-dive docs.
- [ ] Keep document free of horizontal separators (`---`).
- [ ] Replace all em-dashes with semicolons or parentheses (27 found).
- [ ] Validate and correct all xrefs (54 markers detected); use `<xref:...>` where possible.
- [ ] Update all C# examples (6 block(s)) to repository style and C# 14 conventions.

## File-Specific Notes
- Update legacy example `SetScheme (new Scheme (Scheme)` to current style and validate behavior against Scheme APIs.

## Done Criteria
- [ ] Accuracy validated against source; no stale API names or lifecycle guidance.
- [ ] Completeness balanced; advanced behavior covered without documenting obvious API surface.
- [ ] Consistency and grammar aligned with the rest of `docfx/docs`.
- [ ] No horizontal separators and no em-dashes remain.
- [ ] Mermaid renders on GitHub; xrefs resolve correctly.
