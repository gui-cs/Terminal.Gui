# Plan for layout.md

- Document: `/home/runner/work/Terminal.Gui/Terminal.Gui/docfx/docs/layout.md`
- Current size: 254 lines

## Current Signals
- Em-dashes: 8
- Horizontal separators (`---`): 1
- Mermaid blocks: 1
- xref markers: 20
- C# code blocks: 0

## Planned Actions
- [ ] Verify technical claims against source code in: `/home/runner/work/Terminal.Gui/Terminal.Gui/Terminal.Gui/ViewBase/Layout`.
- [ ] Remove repetition and add missing advanced details only where they are not obvious from API names.
- [ ] Normalize heading structure, bullet style, and grammar to match other deep-dive docs.
- [ ] Remove all horizontal separators (`---`) in this document (1 found).
- [ ] Replace all em-dashes with semicolons or parentheses (8 found).
- [ ] Validate all Mermaid blocks for GitHub compatibility (1 block(s)); prefer `flowchart` syntax and avoid unsupported features.
- [ ] Validate and correct all xrefs (20 markers detected); use `<xref:...>` where possible.

## File-Specific Notes
- Fix `AnchorEnd (10)` example to `Pos.AnchorEnd (10)` and re-check all positioning examples against current APIs.

## Done Criteria
- [ ] Accuracy validated against source; no stale API names or lifecycle guidance.
- [ ] Completeness balanced; advanced behavior covered without documenting obvious API surface.
- [ ] Consistency and grammar aligned with the rest of `docfx/docs`.
- [ ] No horizontal separators and no em-dashes remain.
- [ ] Mermaid renders on GitHub; xrefs resolve correctly.
