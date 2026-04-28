# Plan for Popovers.md

- Document: `/home/runner/work/Terminal.Gui/Terminal.Gui/docfx/docs/Popovers.md`
- Current size: 508 lines

## Current Signals
- Em-dashes: 31
- Horizontal separators (`---`): 0
- Mermaid blocks: 0
- xref markers: 17
- C# code blocks: 16

## Planned Actions
- [ ] Verify technical claims against source code in: `/home/runner/work/Terminal.Gui/Terminal.Gui/Terminal.Gui/App/ApplicationPopover.cs and /home/runner/work/Terminal.Gui/Terminal.Gui/Terminal.Gui/App/IApplication.cs`.
- [ ] Remove repetition and add missing advanced details only where they are not obvious from API names.
- [ ] Normalize heading structure, bullet style, and grammar to match other deep-dive docs.
- [ ] Keep document free of horizontal separators (`---`).
- [ ] Replace all em-dashes with semicolons or parentheses (31 found).
- [ ] Validate and correct all xrefs (17 markers detected); use `<xref:...>` where possible.
- [ ] Update all C# examples (16 block(s)) to repository style and C# 14 conventions.

## File-Specific Notes
- Replace all `Application.Popover` references with `Application.Popovers` to match `IApplication.Popovers`.

## Done Criteria
- [ ] Accuracy validated against source; no stale API names or lifecycle guidance.
- [ ] Completeness balanced; advanced behavior covered without documenting obvious API surface.
- [ ] Consistency and grammar aligned with the rest of `docfx/docs`.
- [ ] No horizontal separators and no em-dashes remain.
- [ ] Mermaid renders on GitHub; xrefs resolve correctly.
