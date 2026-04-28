# Plan for config.md

- Document: `/home/runner/work/Terminal.Gui/Terminal.Gui/docfx/docs/config.md`
- Current size: 1150 lines

## Current Signals
- Em-dashes: 2
- Horizontal separators (`---`): 14
- Mermaid blocks: 2
- xref markers: 7
- C# code blocks: 34

## Planned Actions
- [ ] Verify technical claims against source code in: `/home/runner/work/Terminal.Gui/Terminal.Gui/Terminal.Gui/Configuration`.
- [ ] Remove repetition and add missing advanced details only where they are not obvious from API names.
- [ ] Normalize heading structure, bullet style, and grammar to match other deep-dive docs.
- [ ] Remove all horizontal separators (`---`) in this document (14 found).
- [ ] Replace all em-dashes with semicolons or parentheses (2 found).
- [ ] Validate all Mermaid blocks for GitHub compatibility (2 block(s)); prefer `flowchart` syntax and avoid unsupported features.
- [ ] Validate and correct all xrefs (7 markers detected); use `<xref:...>` where possible.
- [ ] Update all C# examples (34 block(s)) to repository style and C# 14 conventions.

## File-Specific Notes
- Convert Mermaid `graph TD` blocks to `flowchart TD` for GitHub compatibility consistency.

## Done Criteria
- [ ] Accuracy validated against source; no stale API names or lifecycle guidance.
- [ ] Completeness balanced; advanced behavior covered without documenting obvious API surface.
- [ ] Consistency and grammar aligned with the rest of `docfx/docs`.
- [ ] No horizontal separators and no em-dashes remain.
- [ ] Mermaid renders on GitHub; xrefs resolve correctly.
