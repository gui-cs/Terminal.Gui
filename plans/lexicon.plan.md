# Plan for lexicon.md

- Document: `/home/runner/work/Terminal.Gui/Terminal.Gui/docfx/docs/lexicon.md`
- Current size: 32 lines

## Current Signals
- Em-dashes: 0
- Horizontal separators (`---`): 0
- Mermaid blocks: 0
- xref markers: 0
- C# code blocks: 0

## Planned Actions
- [ ] Verify technical claims against source code in: `/home/runner/work/Terminal.Gui/Terminal.Gui/docfx/includes`.
- [ ] Remove repetition and add missing advanced details only where they are not obvious from API names.
- [ ] Normalize heading structure, bullet style, and grammar to match other deep-dive docs.
- [ ] Keep document free of horizontal separators (`---`).
- [ ] Confirm no em-dashes are introduced.
- [ ] Add xrefs for core public APIs mentioned in prose and examples (none currently detected).

## File-Specific Notes
- Normalize include paths to a single style (`~/includes/...`) and remove mixed relative forms.

## Done Criteria
- [ ] Accuracy validated against source; no stale API names or lifecycle guidance.
- [ ] Completeness balanced; advanced behavior covered without documenting obvious API surface.
- [ ] Consistency and grammar aligned with the rest of `docfx/docs`.
- [ ] No horizontal separators and no em-dashes remain.
- [ ] Mermaid renders on GitHub; xrefs resolve correctly.
