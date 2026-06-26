# Issue #4522 — `NeedsLayout` is buggy — working plan

Branch: `kh/4522-needslayout-lifecycle`

## Scope recap (from the #4522 status comment)

This issue now scopes to the deterministic-`NeedsLayout`-lifecycle cleanup. Two items were split off:
- #5498 — Frame/layout-driven size changes bypass `Width/Height` change events.
- #5499 — Skip `SetNeedsLayout` when a `Dim.Auto` recompute yields an unchanged size.

## What I verified in the current code (post #4863, #5357/#5373, #5358, #5359)

- **Bug #2 (O(N²) propagation): FIXED.** `SetNeedsLayout` now splits into `MarkSubtreeNeedsLayout` (down) and `MarkAncestorsNeedLayout` (up); ancestors no longer re-descend into sibling subtrees.
- **duskfold stale `ContentSize`: primary fix landed (#4863).** `LayoutSubViews` re-reads `GetContentSize()` after the `OnSubViewLayout` virtual (`View.Layout.cs:812-813`). This branch adds the matching re-read after the `SubViewLayout` *event* (`View.Layout.cs:817-819`), so event handlers that call `SetContentSize` are honored before SubViews are laid out.
- **Bug #5 (clear-before-events): current order is actually the safe one.** `LayoutSubViews` clears `NeedsLayout` (`:866`) *before* firing `SubViewsLaidOut` (`:868-869`). A handler that calls `SetNeedsLayout` re-marks *after* the clear, so the next iteration honors it. The issue's proposed "clear after events" would **regress** this (it would overwrite a handler's re-mark). Do NOT apply the issue's suggested fix.
- **`NeedsLayout` setter:** still `{ get; internal set; }`. The `internal set` has a **legitimate test-infra use**: `SetNeedsLayoutPropagationTests.ClearAllNeedsLayout` sets `root.NeedsLayout = false` to establish a clean baseline. Fully removing the setter requires a replacement testing seam.

## Why Bug #1 / #6 (SetFrame self-dirtying) is not worth fixing piecemeal

`SetFrame` (`:113`) calls `SetNeedsLayout()` unconditionally. `SetNeedsLayout` only propagates upward via `MarkAncestorsNeedLayout` when `SuperView.NeedsLayout` is **already false**; if the SuperView is already marked, the call is a no-op for the ancestor chain. During an active layout pass the SuperView's `NeedsLayout` is true (otherwise `LayoutSubViews` would have returned early), so `SetFrame`'s upward mark is effectively a no-op during the pass itself.

`DimAuto` resolves in-place: `DimAuto.Calculate()` calls `SetRelativeLayout()` directly on its subviews, so the SuperView's frame is already correct after that single call — no "mark + re-layout next iteration" mechanism is involved. The measurements (above) confirm everything is single-pass.

The concern with the ad-hoc `_isLayouting` guard is not that it would break `Dim.Auto` convergence (it is likely safe for that specific path), but that:

- `SetFrame` is called from many non-layout contexts (user code, direct frame assignment). The guard would need to be robust across all call sites.
- The propagation to a **false** SuperView (the only case where it fires) might matter in scenarios not covered by synthetic tests.
- The O(depth) traversal it produces is already single-pass and dominates no measured profile.

**Recommendation: do not attempt the ad-hoc `SetFrame` guard.** The risk/reward is poor: high complexity for a change that saves O(depth) work that is already single-pass and not measurably expensive. Any attempt must be a design-led PR with full visual verification — not bundled into a lifecycle cleanup.

The same reasoning applies to removing the ~9 direct `Layout()` "workaround" calls (Bar, Dialog, ToolTipHost, Markdown, Popover, ScrollBars, Arranger, View init): each exists because the iteration-driven layout doesn't converge for that case. They can only be removed *after* convergence is deterministic, and each needs individual visual verification.

## This branch delivers (safe, verifiable increment)

1. **Accurate `NeedsLayout` documentation.** Replaced the "we expose no setter … ONLY place it's changed is SetNeedsLayout" + BUGBUG block (`View.Layout.cs`) — which the issue explicitly calls "bogus" — with an accurate description of the real invariant and the internal write sites, and why the setter is `internal` (test seam) pending full removal.
2. **Completed the duskfold re-read symmetry.** `LayoutSubViews` already re-reads `GetContentSize()` after the `OnSubViewLayout` *virtual* (#4863); now it also re-reads after the `SubViewLayout` *event*, so a handler on that event that calls `SetContentSize` is honored (the value captured there is what every SubView is laid out against).
3. **Regression test** (parallelizable): `LayoutSubViews_Honors_SetContentSize_From_SubViewLayout_Event` in `StaleContentSizeCaptureTests`. Note: #4863's `OnSubViewLayout`-virtual, `SubViewsLaidOut`, Dialog, ListView and TableView scenarios are **already** covered by that file (7 tests) — this adds the missing event-companion case.

### Verification
- Focused layout/content-size regression tests pass: `StaleContentSizeCaptureTests` (8 tests) and `LayoutConvergenceTests` (5 tests).
- The new event-companion test was mutation-checked: removing the post-`SubViewLayout` re-read makes `LayoutSubViews_Honors_SetContentSize_From_SubViewLayout_Event` fail with `Expected: 50, Actual: 20`; restoring the re-read makes it pass.
- `AllViewsRenderFingerprintTests` passes locally after classifying FileDialog-family filesystem permission failures as environment limitations (`|ENV:`) rather than layout/draw exceptions (`|EX:`).
- Builds during the focused test runs still show the pre-existing CS0419 warning in `View.Drawing.cs`, a file not touched here.

## Measurement: is the Bug #1/#6 convergence redesign actually warranted?

Before touching convergence code, I measured the *current* fan-out of a single property change
(`LayoutConvergenceTests`, counting `SubViewsLaidOut` + `FrameChanged` per change). Post
#5357/#5358/#5359 the numbers are healthy:

| Tree (depth × breadth) | total views | application passes | views that ran LayoutSubViews | FrameChanged |
|---|---|---|---|---|
| 3 × 3 | 10 | **1** | 5 | **1** |
| 6 × 3 | 19 | **1** | 8 | **1** |
| 8 × 4 | 33 | **1** | 10 | **1** |

Conclusions:
- **Convergence is already single-pass** — no multi-iteration thrashing (Bug #6 symptom gone).
- **Only the changed view's frame changes** (`FrameChanged == 1`) — Bug #1's spurious-frame-churn symptom is gone.
- **No sibling/subtree fan-out** — work stays on the affected ancestor chain (`SubViewsLaidOut ≈ depth+2`), far below the total view count. (#5357.)
- **`Dim.Auto` parents still converge correctly in one pass.** Note: in the synthetic tests here, the user-initiated `child.Width = N` setter already propagates upward via `SetNeedsLayout → MarkAncestorsNeedLayout`, so the additional mark that `SetFrame` emits during the layout pass is redundant for those cases. Whether the `SetFrame` upward mark is **load-bearing** for some production scenario (e.g. a multi-pass cascade where no user setter triggered the initial propagation) is not disproved by these tests, but not proven either. The ad-hoc guard is still **not recommended** (see below).

The only residual is **O(depth)** ancestor re-layout that produces no frame change. Removing it
requires **dependency-aware invalidation** (marking only ancestors/siblings that actually reference
the changed view) — high risk to `Dim.Auto` correctness for a gain that is already single-pass.
**Recommendation: do not attempt the ad-hoc `SetFrame` guard.** The catastrophic behaviors the issue
described were fixed by the #4973-era PRs; what remains is a measured, low-value optimization that
should only be pursued as a dedicated dependency-analysis project if profiling shows it matters.

## What this branch adds beyond the safe increment

- **`LayoutConvergenceTests`** (5 tests) — permanent regression guards that lock in the deterministic
  properties above: single-pass convergence, no spurious `FrameChanged`, bounded fan-out, `Dim.Auto`
  growth correctness, and sibling-reference (`Pos.Right`) repositioning — each asserted single-pass.
- **`AllViewsRenderFingerprintTests`** — an all-views smoke guard (every concrete `View` lays out and
  draws in design mode without throwing) and the harness used to prove render-neutrality (below).

## Visual / rendering verification

- **All-views render smoke:** the harness lays out and draws every concrete, non-environment-dependent
  view type and fails on `|EX:` layout/draw exceptions. FileDialog-family views may touch restricted
  filesystem paths during design-mode initialization/layout; those permission failures are reported as
  `|ENV:` so the smoke test remains portable without hiding real layout/draw exceptions from other views.
- The focused verification above passes. Re-run the full parallelizable and non-parallelizable suites
  before merge to refresh branch-wide pass counts.
- tuirec PTY capture was not available in this environment (needs a Go install + non-allowlisted
  network); deterministic in-process `Driver.ToString()` snapshots were used instead.

## Deferred (recommend separate, design-led PRs — and now data-backed as low priority)

- Bug #1/#6 dependency-aware invalidation (remove the O(depth) ancestor re-layout) + removal of direct
  `Layout()` workarounds. **Not warranted by current measurements** — convergence is already single-pass.
- Full removal of the `NeedsLayout` setter (needs a test-only seam to replace `ClearAllNeedsLayout`).
