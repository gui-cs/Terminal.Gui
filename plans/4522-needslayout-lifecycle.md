# Issue #4522 — `NeedsLayout` is buggy — working plan

Branch: `kh/4522-needslayout-lifecycle`

## Scope recap (from the #4522 status comment)

This issue now scopes to the deterministic-`NeedsLayout`-lifecycle cleanup. Two items were split off:
- #5498 — Frame/layout-driven size changes bypass `Width/Height` change events.
- #5499 — Skip `SetNeedsLayout` when a `Dim.Auto` recompute yields an unchanged size.

## What I verified in the current code (post #4863, #5357/#5373, #5358, #5359)

- **Bug #2 (O(N²) propagation): FIXED.** `SetNeedsLayout` now splits into `MarkSubtreeNeedsLayout` (down) and `MarkAncestorsNeedLayout` (up); ancestors no longer re-descend into sibling subtrees.
- **duskfold stale `ContentSize`: primary fix landed (#4863).** `LayoutSubViews` re-reads `GetContentSize()` after the `OnSubViewLayout` virtual (`View.Layout.cs:812-813`). The re-read does **not** also happen after the `SubViewLayout` *event* (line 815).
- **Bug #5 (clear-before-events): current order is actually the safe one.** `LayoutSubViews` clears `NeedsLayout` (`:862`) *before* firing `SubViewsLaidOut` (`:864-865`). A handler that calls `SetNeedsLayout` re-marks *after* the clear, so the next iteration honors it. The issue's proposed "clear after events" would **regress** this (it would overwrite a handler's re-mark). Do NOT apply the issue's suggested fix.
- **`NeedsLayout` setter:** still `{ get; internal set; }`. The `internal set` has a **legitimate test-infra use**: `SetNeedsLayoutPropagationTests.ClearAllNeedsLayout` sets `root.NeedsLayout = false` to establish a clean baseline. Fully removing the setter requires a replacement testing seam.

## Why Bug #1 / #6 (SetFrame self-dirtying) cannot be safely fixed piecemeal

`SetFrame` (`:113`) calls `SetNeedsLayout()` unconditionally. During a layout pass this re-marks the view's **SuperView** via `MarkAncestorsNeedLayout`. For a non-`Dim.Auto` SuperView this is a false positive (the issue's complaint). **But** the engine relies on exactly this mark for `Dim.Auto` convergence:

- Layout is driven top-down: `SuperView.Layout()` resolves the SuperView's own `Frame` in `SetRelativeLayout` **before** `LayoutSubViews` lays out children.
- For a `Dim.Auto` SuperView, the final children sizes are only known *during* `LayoutSubViews` — after the SuperView's frame was computed.
- The "mark SuperView + re-layout next iteration" behavior is the convergence mechanism that lets `Dim.Auto` settle.

Naively guarding `SetFrame` against the upward mark (the issue's proposed `_isLayouting` flag) breaks `Dim.Auto` convergence. A correct fix is a **redesign of `Dim.Auto` multi-pass convergence**, not a one-line guard. This is why @tig called it "too gnarly" and deferred it post-2.0. It should be its own design-led PR with full visual verification, not bundled here.

The same reasoning applies to removing the ~9 direct `Layout()` "workaround" calls (Bar, Dialog, ToolTipHost, Markdown, Popover, ScrollBars, Arranger, View init): each exists because the iteration-driven layout doesn't converge for that case. They can only be removed *after* convergence is deterministic, and each needs individual visual verification.

## This branch delivers (safe, verifiable increment)

1. **Accurate `NeedsLayout` documentation.** Replaced the "we expose no setter … ONLY place it's changed is SetNeedsLayout" + BUGBUG block (`View.Layout.cs`) — which the issue explicitly calls "bogus" — with an accurate description of the real invariant and the internal write sites, and why the setter is `internal` (test seam) pending full removal.
2. **Completed the duskfold re-read symmetry.** `LayoutSubViews` already re-reads `GetContentSize()` after the `OnSubViewLayout` *virtual* (#4863); now it also re-reads after the `SubViewLayout` *event*, so a handler on that event that calls `SetContentSize` is honored (the value captured there is what every SubView is laid out against).
3. **Regression test** (parallelizable): `LayoutSubViews_Honors_SetContentSize_From_SubViewLayout_Event` in `StaleContentSizeCaptureTests`. Note: #4863's `OnSubViewLayout`-virtual, `SubViewsLaidOut`, Dialog, ListView and TableView scenarios are **already** covered by that file (7 tests) — this adds the missing event-companion case.

### Verification
- Core library + both test projects build clean (the only warning is the pre-existing CS0419 in `View.Drawing.cs`, a file not touched here).
- `UnitTestsParallelizable`: **17,376 passed, 0 failed** (run twice; one transient failure on the first run was the known-flaky `ProcessQueue_DoesNotReleaseEscape_BeforeTimeout`, green on re-run).
- `UnitTests.NonParallelizable`: **74 passed, 0 failed** (after a clean rebuild; an initial mass failure was a stale local `Markdig.dll` 1.3.0 vs. central 1.3.1, unrelated to this change).

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
- **`Dim.Auto` parents still converge correctly in one pass**, and the upward `NeedsLayout` mark from `SetFrame` is **load-bearing** for that.

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

- **All-views render diff:** captured a SHA fingerprint of `Driver.ToString()` for every concrete
  view type (61 views) with the `LayoutSubViews` change applied vs. reverted. **Byte-identical**
  (combined hash `CCF30A9EA3C45AB5` both ways) — the change is provably render- and layout-neutral
  across all views.
- **`AllViewsDrawTests`** (existing) passes — every view draws with exactly one layout pass, no extra.
- Full `UnitTestsParallelizable` (**17,382**) and `UnitTests.NonParallelizable` (**74**) green.
- tuirec PTY capture was not available in this environment (needs a Go install + non-allowlisted
  network); deterministic in-process `Driver.ToString()` snapshots were used instead.

## Deferred (recommend separate, design-led PRs — and now data-backed as low priority)

- Bug #1/#6 dependency-aware invalidation (remove the O(depth) ancestor re-layout) + removal of direct
  `Layout()` workarounds. **Not warranted by current measurements** — convergence is already single-pass.
- Full removal of the `NeedsLayout` setter (needs a test-only seam to replace `ClearAllNeedsLayout`).
