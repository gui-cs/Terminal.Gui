# Plan: Fix OutputBufferImpl Race Condition (#5130)

**PR**: [#5131](https://github.com/gui-cs/Terminal.Gui/pull/5131)  
**Branch**: `fix/5130-output-buffer-race-condition`

## Problem Statement

`OutputBufferImpl` has a race condition where `ClearContents()` replaces the `Contents` array
reference **outside** any lock, then acquires a lock on the **new** object. Concurrently,
`AddGrapheme()` acquires a lock on whatever `Contents` happens to point to. When both run at
the same time:

1. `AddGrapheme` reads `Contents` (old reference), passes the null check
2. `ClearContents` swaps `Contents` to a new array
3. `AddGrapheme` locks on `Contents` — now the **new** object — same object `ClearContents` locks on
4. Both threads can proceed through the lock without mutual exclusion on the **old** array
5. The old array's `Cell` data gets partially overwritten, corrupting `Rune` values
6. Native `Wcwidth` crashes with `AccessViolationException` on the corrupted data

The `FillRect(Rectangle, Rune)` overload has the identical `lock (Contents!)` anti-pattern.

Additionally, `OutputBase.WriteToScreen()` reads `Contents` without any lock during screen
flush, creating a third concurrent access point.

## Root Cause

Locking on `Contents` itself is fundamentally broken because `ClearContents` **replaces** the
reference. A `lock(obj)` only provides mutual exclusion when all threads lock on the **same**
object instance.

## Approach

Introduce a **stable, `readonly` dedicated lock object** (`_contentsLock`) that is never
replaced. All code that reads or writes `Contents`, `DirtyLines`, `Clip`, or `_urlMap` must
do so inside `lock (_contentsLock)`.

### Design Considerations

1. **Lock granularity**: A single lock object is simplest and correct. The critical sections
   are short (cell writes, array swaps). Performance impact should be negligible since
   drawing is already serialized on the main thread — the lock primarily protects against
   the driver resize path which runs on a different thread.

2. **`Rows`/`Cols` setters**: Both call `ClearContents()` which will now acquire the lock.
   The `SetSize()` method sets `Cols` then `Rows`, triggering two `ClearContents` calls. We
   should consider having `SetSize` set backing fields directly and call `ClearContents`
   once. However, this is an optimization — the lock is re-entrant safe... actually, 
   `Monitor.Enter` (what `lock` uses) is **re-entrant** in .NET, so the double call won't
   deadlock, but it's wasteful. We should fix `SetSize` to avoid the double clear.

3. **`OutputBase.WriteToScreen()`**: This reads `Contents` without any synchronization. The
   fix should expose the lock or provide a synchronized accessor. However, since
   `WriteToScreen` runs on the main thread during `Refresh()`, and `ClearContents` is the
   only cross-thread mutator, the primary fix in `OutputBufferImpl` should suffice. We can
   address `OutputBase` as a follow-up if needed.

4. **`Move()`**: Sets `Col` and `Row` without the lock. Since `Move` + `AddGrapheme` are
   always called sequentially from the same thread, and the lock protects `AddGrapheme`
   internals, `Move` doesn't need the lock. However, `Col`/`Row` are read inside the lock
   in `AddGrapheme` — we should verify no cross-thread `Move` calls exist.

5. **The `FillRect(Rectangle, char)` overload** (line 452): This calls `Move` + `AddRune`
   in a loop without a lock. It's protected indirectly because `AddGrapheme` acquires the
   lock. No change needed for this overload.

## Implementation Steps

### Step 1: Add `_contentsLock` field

Add a `private readonly object _contentsLock = new ();` field near the top of the class,
next to the existing `_cols` / `_rows` fields.

### Step 2: Fix `ClearContents(bool)`

Move **all** mutations (`Contents`, `Clip`, `DirtyLines`, `_urlMap`) inside
`lock (_contentsLock)`. The entire method body goes inside the lock.

```csharp
public void ClearContents (bool initiallyDirty)
{
    lock (_contentsLock)
    {
        Contents = new Cell [Rows, Cols];
        Clip = new Region (Screen);
        DirtyLines = new bool [Rows];
        _urlMap?.Clear ();

        for (var row = 0; row < Rows; row++)
        {
            for (var c = 0; c < Cols; c++)
            {
                Contents [row, c] = new Cell
                {
                    Grapheme = " ",
                    Attribute = new Attribute (Color.White, Color.Black),
                    IsDirty = initiallyDirty
                };
            }

            DirtyLines [row] = initiallyDirty;
        }
    }
}
```

### Step 3: Fix `AddGrapheme(string)`

Replace `lock (Contents)` with `lock (_contentsLock)`. Add a double-checked null guard
inside the lock. Move `Clip` access inside the lock since `ClearContents` now replaces
`Clip` under the lock.

```csharp
private void AddGrapheme (string grapheme)
{
    lock (_contentsLock)
    {
        if (Contents is null)
        {
            return;
        }

        Clip ??= new Region (Screen);
        Rectangle clipRect = Clip!.GetBounds ();
        int printableGraphemeWidth = -1;

        if (IsValidLocation (grapheme, Col, Row))
        {
            SetAttributeAndDirty (Col, Row);
            InvalidateOverlappedWideGlyph (Col, Row);

            string printableGrapheme = grapheme.MakePrintable ();
            printableGraphemeWidth = printableGrapheme.GetColumns ();
            WriteGraphemeByWidth (Col, Row, printableGrapheme, printableGraphemeWidth, clipRect);

            DirtyLines [Row] = true;
        }

        Col++;

        if (printableGraphemeWidth <= 1)
        {
            return;
        }

        if (Clip.Contains (Col, Row))
        {
            if (Contents [Row, Col].Attribute != CurrentAttribute)
            {
                Contents [Row, Col].Attribute = CurrentAttribute;
                Contents [Row, Col].IsDirty = true;
            }
        }

        Col++;
    }
}
```

### Step 4: Fix `FillRect(Rectangle, Rune)`

Replace `lock (Contents!)` with `lock (_contentsLock)`. Add null guard inside.

### Step 5: Fix `SetSize` to avoid double `ClearContents`

Currently `SetSize` sets `Cols` then `Rows`, each triggering `ClearContents()` via the
property setters. Refactor to set backing fields directly and call `ClearContents` once:

```csharp
public void SetSize (int cols, int rows)
{
    _cols = cols;
    _rows = rows;
    ClearContents ();
}
```

### Step 6: Verify the test passes

Run the existing concurrency test:

```bash
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-method "*AddStr_And_ClearContents_Concurrent_DoesNotThrow"
```

### Step 7: Run full test suite

Ensure no regressions:

```bash
dotnet test --project Tests/UnitTestsParallelizable --no-build
dotnet test --project Tests/UnitTests.NonParallelizable --no-build
```

### Step 8: Consider additional test coverage

- Test that `FillRect` + `ClearContents` concurrent access doesn't throw
- Test that `Move` + `AddStr` + `ClearContents` produces consistent state

## Files to Change

| File | Change |
|------|--------|
| `Terminal.Gui/Drivers/Output/OutputBufferImpl.cs` | Add `_contentsLock`; fix `ClearContents`, `AddGrapheme`, `FillRect`, `SetSize` |

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Lock contention slows drawing | Lock is only held for cell-level operations; main thread drawing is already serialized. Measure if concerned. |
| Deadlock from re-entrant lock in `SetSize` | .NET `Monitor` (used by `lock`) is re-entrant. Plus Step 5 eliminates the double call. |
| `OutputBase.WriteToScreen` still reads without lock | Runs on main thread during `Refresh()`; cross-thread mutation is now synchronized. Document as future improvement. |
| `Col`/`Row` modified outside lock by `Move()` | `Move()` is always called from the same thread as `AddGrapheme`. No cross-thread `Move` calls observed. |

## Benchmark Baseline (Before Fix)

Captured with `--job short` on Intel Core i9-14900K, .NET 10.0.6:

| Method              | Cols | Rows | Mean        | Allocated  |
|-------------------- |----- |----- |------------:|-----------:|
| AddStr_FullRow      | 80   | 25   |    31.48 μs |   63.77 KB |
| AddStr_AllRows      | 80   | 25   |   776.68 μs | 1590.02 KB |
| FillRect_FullScreen | 80   | 25   |   666.49 μs | 1634.18 KB |
| ClearContents       | 80   | 25   |    75.76 μs |   63.09 KB |
| SetSize             | 80   | 25   |   225.99 μs |  189.26 KB |
| TypicalDrawCycle    | 80   | 25   |   859.06 μs | 1653.11 KB |
| AddStr_FullRow      | 200  | 50   |    78.34 μs |  159.63 KB |
| AddStr_AllRows      | 200  | 50   | 4,205.18 μs | 7961.35 KB |
| FillRect_FullScreen | 200  | 50   | 3,799.05 μs | 8190.23 KB |
| ClearContents       | 200  | 50   |   444.25 μs |  313.27 KB |
| SetSize             | 200  | 50   | 1,310.77 μs |  939.82 KB |
| TypicalDrawCycle    | 200  | 50   | 5,512.82 μs | 8274.62 KB |

The fix must not regress these numbers significantly. Re-run after implementing
and compare.

## Out of Scope

- Making `OutputBase.WriteToScreen` acquire the lock (would require exposing the lock or adding a synchronized read API — separate PR)
- Thread-safety for `Clip` property getter/setter beyond what's protected by `_contentsLock`
- General thread-safety audit of the driver layer
