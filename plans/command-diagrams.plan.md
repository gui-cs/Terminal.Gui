# Plan for command-diagrams.md

- Document: `/home/runner/work/Terminal.Gui/Terminal.Gui/docfx/docs/command-diagrams.md`
- Current size: 133 lines

## Current Signals
- Em-dashes: 4
- Horizontal separators (`---`): 0
- Mermaid blocks: 3
- xref markers: 71
- C# code blocks: 0

## Planned Actions
- [ ] Verify technical claims against source code in: `/home/runner/work/Terminal.Gui/Terminal.Gui/Terminal.Gui/Input and /home/runner/work/Terminal.Gui/Terminal.Gui/Terminal.Gui/ViewBase`.
- [ ] Remove repetition and add missing advanced details only where they are not obvious from API names.
- [ ] Normalize heading structure, bullet style, and grammar to match other deep-dive docs.
- [ ] Keep document free of horizontal separators (`---`).
- [ ] Replace all em-dashes with semicolons or parentheses (4 found).
- [ ] Validate all Mermaid blocks for GitHub compatibility (3 block(s)); prefer `flowchart` syntax and avoid unsupported features.
- [ ] Validate and correct all xrefs (71 markers detected); use `<xref:...>` where possible.

## File-Specific Notes
- No additional file-specific issue found in automated checks; validate manually during implementation.

## Done Criteria
- [ ] Accuracy validated against source; no stale API names or lifecycle guidance.
- [ ] Completeness balanced; advanced behavior covered without documenting obvious API surface.
- [ ] Consistency and grammar aligned with the rest of `docfx/docs`.
- [ ] No horizontal separators and no em-dashes remain.
- [ ] Mermaid renders on GitHub; xrefs resolve correctly.
