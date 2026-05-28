# Issue #5433 — Make `View.Draw` self-draw context flow explicit

**Branch:** `fix-5433-explicit-self-draw-context` (based on `develop` — NOT stacked on #5435).

**Parent issue:** [#4973](https://github.com/gui-cs/Terminal.Gui/issues/4973)
**Follow-up to:** [#5431](https://github.com/gui-cs/Terminal.Gui/pull/5431) (#5358)

## Why not stacked

#5433 refactors the `_localDrawContext` / `needsDrawSelf` self-draw flow in `View.Drawing.cs`
(`Draw()` lines ~96–198 and `DoDrawComplete` ~845). That code was introduced by #5358 and is
already on `develop`. PR #5435 (#5359) only touches `View.Drawing.cs` at lines 241–310
(`CanNarrowClearToNeedsDrawRect`). No overlap → no stacking required. The two will merge cleanly.

## Goal (behavior-preserving)

Make the `_localDrawContext` lifecycle explicit and defensive instead of relying on three
separate `if (needsDrawSelf)` conditionals that must stay manually aligned. No rendering,
ordering, or invalidation behavior changes.

## Current shape (the problem)

In `Draw()`, the self-draw work is spread across three `if (needsDrawSelf)` blocks:

1. `_localDrawContext = new ()` — recreate per-view context.
2. `SetAttributeForRole(...)` + `DoClearViewport(context)` — clear self background.
3. (after `DoDrawSubViews`) `DoDrawText` + `DoDrawContent` into `_localDrawContext`, then merge into shared `context`.

`_localDrawContext` is then consumed in `DoDrawComplete` (`CachedDrawnRegion = _localDrawContext?.GetDrawnRegion()`).

The subtle invariants — recreate only on self-redraw, preserve across child-only passes, consume in
`DoDrawComplete` — are split across distant blocks, so a future edit can easily reset or split the
lifecycle at the wrong point and reintroduce the #5431 review regression
(`TransparentMouseParent_ChildOnlyDirty_PreservesCachedDrawnRegion`).

## Key observation enabling the cleanup

`DoClearViewport` uses the **shared** `context`, not `_localDrawContext`. The local context is
only written by `DoDrawText`/`DoDrawContent` and merged afterward. So its creation does **not** need
to sit in the clear block — it can move into the self-content step, making
create → populate → merge a single contiguous unit. This is behavior-preserving:

- self-redraw pass: created before first use, exactly as today.
- child-only pass: never created (block skipped), prior context preserved, exactly as today.

## Changes (`Terminal.Gui/ViewBase/View.Drawing.cs`)

1. Remove the standalone `if (needsDrawSelf) { _localDrawContext = new (); }` block.
2. Keep the clear block: `if (needsDrawSelf) { SetAttributeForRole(...); DoClearViewport(context); }`.
   Document that the self-draw is intentionally split around `DoDrawSubViews` to honor the
   `Clear → SubViews → Text → Content` order (AC4).
3. Replace the third block with a single call: `if (needsDrawSelf) { DrawSelfContent(context); }`.
4. Add private helper `DrawSelfContent(DrawContext sharedContext)` that owns the full local-context
   lifecycle in one place: create `_localDrawContext`, draw Text + Content into it, merge its region
   into the shared context. Trace calls and `SetAttributeForRole` preserved.
5. Consolidate the authoritative `_localDrawContext` contract comment at the field declaration
   (recreated only on self-redraw; preserved on child-only passes; consumed in `DoDrawComplete`
   for `TransparentMouse` hit-testing), and reference it from `DrawSelfContent`.

Draw order is unchanged: clear (self) → SubViews → flush `_lastClearedViewport` → self Text/Content.

## Verification

- `dotnet build` — no new warnings.
- Regression / contract tests must stay green:
  - `SubViewOnlyRedrawTests` (incl. `TransparentMouseParent_ChildOnlyDirty_PreservesCachedDrawnRegion`)
  - `RegionAwareClearViewportTests`
  - `NeedsDrawTests`
  - full `UnitTestsParallelizable` suite.

## Acceptance criteria mapping

| AC | Coverage |
|----|----------|
| 1. self-draw as one explicit unit | `DrawSelfContent` helper owns the local-context lifecycle; clear stays a single documented block whose split from content is explained. |
| 2. recreate `_localDrawContext` only on self-redraw | creation lives inside `DrawSelfContent`, only called when `needsDrawSelf`. |
| 3. preserve prior context on child-only passes | `DrawSelfContent` not called when `needsDrawSelf` is false; field untouched. |
| 4. draw order unchanged | Clear → SubViews → Text → Content preserved. |
