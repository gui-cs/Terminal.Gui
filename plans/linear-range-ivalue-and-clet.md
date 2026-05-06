# LinearRange `IValue<T>` Refactor and `linear-range` Clet

Tracking: [Terminal.Gui#5202](https://github.com/gui-cs/Terminal.Gui/issues/5202)
Downstream consumer: [gui-cs/clet](https://github.com/gui-cs/clet)

## 1. Problem

`LinearRange<T>` does not implement `IValue<T>`. The Terminal.Gui v2 convention
(`SelectorBase`, `CheckBox`, `DatePicker`, `ScrollBar`, `Tabs`, `ListView<T>`,
`NumericUpDown<T>`, …) is for any view whose primary purpose is editing a value
to expose that value through `IValue<TValue>` (`Value` getter/setter,
`ValueChanging` / `ValueChanged` / `ValueChangedUntyped` events).

This matters now because the [clet](https://github.com/gui-cs/clet) project
wraps Terminal.Gui views as command-line tools. Today, clet ships a
hand-rolled `RangeView` + `RangeClet` that is **not** built on `LinearRange`;
that is duplicated work and leaves the real, feature-rich `LinearRange` (with
typed options, legends, abbreviation, range modes, drag, CWP events) inaccessible
from the CLI. Wiring `LinearRange` into clet requires a single canonical
typed `Value` surface — i.e. `IValue<T>`.

### The design tension

`LinearRange<T>`'s "value" is heterogeneous and depends on
`LinearRangeType`:

| Type           | What the user picked                                  | Natural shape       |
|----------------|-------------------------------------------------------|---------------------|
| `Single`       | 0 or 1 option                                         | `T?`                |
| `Multiple`     | 0..N options                                          | `IReadOnlyList<T>`  |
| `LeftRange`    | 1 cut point: "everything ≤ X"                         | `T` plus a kind tag |
| `RightRange`   | 1 cut point: "everything ≥ X"                         | `T` plus a kind tag |
| `Range`        | 2 cut points: closed interval                         | `(T start, T end)`  |

Today this is exposed as `_setOptions: List<int>` (indices) plus an
`OptionsChanged` event carrying `Dictionary<int, LinearRangeOption<T>>`.
There is no `Value` and no clean way to bind one. Forcing a single
`IValue<T>` over all five types either over-boxes (`IValue<object?>` —
useless for type-safety) or under-fits (`IValue<T?>` — drops Multiple/Range).

## 2. Recommendation: split the family, then implement `IValue`

Mirror what was done with `SelectorBase` → `OptionSelector<TEnum>` /
`FlagSelector<TFlagsEnum>`. Make value semantics part of the **type**
the consumer picks, so each subclass has a single, honest
`IValue<TValue>`.

### 2.1 New type family (in `Terminal.Gui/Views/LinearRange/`)

```
LinearRangeViewBase<TOption, TValue>   abstract base : View, IOrientation, IValue<TValue>
    │
    ├── LinearSelector<T>          : LinearRangeViewBase<T, T?>                   // was Type.Single
    │
    ├── LinearMultiSelector<T>     : LinearRangeViewBase<T, IReadOnlyList<T>>     // was Type.Multiple
    │
    └── LinearRange<T>             : LinearRangeViewBase<T, LinearRangeSpan<T>>   // was Range / LeftRange / RightRange
```

* `LinearRangeViewBase<TOption, TValue>` owns:
  the option list (`IReadOnlyList<LinearRangeOption<TOption>> Options`),
  `Orientation`, `LegendsOrientation`, `ShowLegends`, `ShowEndSpacing`,
  `MinimumInnerSpacing`, `UseMinimumSize`, `Style`, `AllowEmpty`,
  drawing, hit-testing, CWP events for those properties, key/mouse handling,
  and the protected `SetSelectedIndices(IReadOnlyList<int>)` plumbing
  shared by subclasses.

* Each subclass exposes its own `Value` via `CWPPropertyHelper.ChangeProperty`
  and translates `Value ↔ indices` internally.

* `LinearRangeType` enum is **deleted** as public surface.
  (Internally `LinearRangeViewBase` keeps a `RenderMode` analogue used
  only by drawing/hit-testing — `Single`, `Multiple`, `LeftSpan`, `RightSpan`,
  `Span` — set by each subclass in its constructor.)

* The non-generic `LinearRange : LinearRange<object>` shortcut goes away.
  Callers that wanted "any options" now pick `LinearSelector<object>`,
  `LinearMultiSelector<object>`, or `LinearRange<object>`.

### 2.2 `LinearRangeSpan<T>`

```csharp
public readonly record struct LinearRangeSpan<T>
{
    public LinearRangeSpan (LinearRangeSpanKind kind, T? start, T? end, int startIndex, int endIndex)
    { … }

    public LinearRangeSpanKind Kind        { get; }   // None | LeftBounded | RightBounded | Closed
    public T?                  Start       { get; }
    public T?                  End         { get; }
    public int                 StartIndex  { get; }   // -1 when not set
    public int                 EndIndex    { get; }   // -1 when not set

    public static LinearRangeSpan<T> Empty { get; } = new (LinearRangeSpanKind.None, default, default, -1, -1);
}

public enum LinearRangeSpanKind { None, LeftBounded, RightBounded, Closed }
```

This is one struct that can describe all three "range" sub-modes — kind
selects which fields are meaningful. Equality / `record struct`
gives free `EqualityComparer<>` for the CWP guard.

`LinearRange<T>` exposes `RangeKind { get; set; }` — `LeftBounded`,
`RightBounded`, `Closed` — defaulting to `Closed`. Setting it migrates
the current `Value` (e.g. dropping `End` when switching `Closed →
LeftBounded`).

### 2.3 Why this works for `IValue<T>`

| New view                 | `IValue<TValue>` satisfies #5202 because…                                    |
|--------------------------|------------------------------------------------------------------------------|
| `LinearSelector<T>`      | Drop-in for any "pick one of N typed things" — same shape as `Tabs`, `OptionSelector`. |
| `LinearMultiSelector<T>` | First-class multi-pick view; `Value` is an immutable list, easy to data-bind. |
| `LinearRange<T>`         | The honest "range" case; `Value` is a struct that already carries kind.        |

Each is a single concrete `IValue<TValue>` — no `T?` ambiguity, no
heterogeneous boxing.

## 3. Migration / breaking changes

`LinearRange` is alpha and #5202 explicitly trades breakage for a clean
shape. Concretely:

| Old                                          | New                                                   |
|----------------------------------------------|-------------------------------------------------------|
| `LinearRange<T>` (Type=Single)               | `LinearSelector<T>`                                   |
| `LinearRange<T>` (Type=Multiple)             | `LinearMultiSelector<T>`                              |
| `LinearRange<T>` (Type=Range / Left / Right) | `LinearRange<T>` with `RangeKind`                     |
| `LinearRange : LinearRange<object>`          | removed; pick the typed subclass                      |
| `Type` property                              | removed                                               |
| `SetOption`, `UnSetOption`, `GetSetOptions`  | removed; use `Value` setter                           |
| `OptionsChanged`                             | replaced by `ValueChanging` / `ValueChanged`          |
| `OptionFocused`                              | retained on the base (focus is independent of value)  |

UICatalog `LinearRanges.cs` and the existing tests
(`LinearRangeTests`, `LinearRangeFluentTests`,
`LinearRangeDefaultKeyBindingsTests`) get migrated as part of the same
PR; net coverage must not drop.

## 4. Downstream: the `linear-range` clet

clet currently has `RangeClet` + a custom `RangeView` for numeric
`low..high` input. That stays — it's the *numeric* range tool. The new
clet wraps the new Terminal.Gui views.

### 4.1 One clet, three modes

`clet linear-range` covers all three subclasses via `--mode`. This keeps
discovery simple (`clet list` shows one entry) and lets agents pick the
shape they want from a single command.

```
clet linear-range \
  --title <text> \
  --mode  single | multi | range \
  --options <CSV-or-spec> \
  [--initial <selection>] \
  [--orientation horizontal|vertical] \
  [--show-legends] [--no-end-spacing] [--allow-empty] \
  [--range-kind closed|left|right]   # only with --mode range
  [--json] [--timeout 30s]
```

#### Options spec

`--options` accepts two forms, picked by parsing:

1. **Labelled enumeration** — `"Free,Pro,Team,Enterprise"`.
   Each label becomes a `LinearRangeOption<string>` whose `Data == Legend`.
2. **Numeric range** — `"0..1000:50"` (start..end[:step]). Expands to
   `LinearRangeOption<double>` (or `<long>` if all components parse as
   integers) with `Legend = value.ToString()`.

`--initial` matches the same forms:

| Mode    | Initial syntax                          |
|---------|-----------------------------------------|
| single  | `"Pro"` or `"500"`                      |
| multi   | `"Pro,Team"` or `"100,300,500"`         |
| range   | `"100..500"`, `"..500"` (left), `"100.."` (right) |

### 4.2 JSON output

Schema version stays 1, status / cancelled / error envelopes stay as in
the existing clet contract.

```jsonc
// --mode single
{ "schemaVersion":1, "status":"ok", "mode":"single",
  "value":"Pro", "index":1 }

// --mode multi
{ "schemaVersion":1, "status":"ok", "mode":"multi",
  "values":["Pro","Team"], "indices":[1,2] }

// --mode range  (range-kind closed)
{ "schemaVersion":1, "status":"ok", "mode":"range", "kind":"closed",
  "start":100, "end":500, "startIndex":2, "endIndex":10 }

// --mode range  (range-kind left)
{ "schemaVersion":1, "status":"ok", "mode":"range", "kind":"left",
  "end":500, "endIndex":10 }
```

Cancellation / errors use the existing envelopes; exit codes unchanged
(0/1/2/130).

### 4.3 Mapping from Terminal.Gui to clet

```
RangeCletV2 (file: Clets/Input/LinearRangeClet.cs)
    --mode single → LinearSelector<TOption>     → result.Value
    --mode multi  → LinearMultiSelector<TOption>→ result.Values
    --mode range  → LinearRange<TOption>        → result.Value : LinearRangeSpan<TOption>
```

All three are `IValue<TValue>`, so the clet wraps each in
`RunnableWrapper`, awaits Accept/Cancel, and serialises `view.Value` with
a per-mode JSON shape. No special-cases beyond the mode switch.

## 5. Implementation order

1. **Add `LinearRangeSpan<T>` + `LinearRangeSpanKind`** under `LinearRange/`.
2. **Extract `LinearRangeViewBase<TOption, TValue>`** from today's
   `LinearRange.cs` — keep drawing, layout, options, key/mouse,
   focus, CWP for non-value properties; abstract out `SetSelectedIndices`
   and value translation.
3. **Add `LinearSelector<T>`** with `IValue<T?>`. Migrate single-select
   tests.
4. **Add `LinearMultiSelector<T>`** with `IValue<IReadOnlyList<T>>`.
   Use a defensive immutable copy in the setter; equality via
   `SequenceEqual` in the CWP guard.
5. **Replace `LinearRange<T>`** body with the range-only subclass using
   `IValue<LinearRangeSpan<T>>`; expose `RangeKind`.
6. **Delete** `LinearRangeType`, the non-generic `LinearRange`, and the
   index-centric public methods (`SetOption`, `UnSetOption`,
   `GetSetOptions`, `OptionsChanged`, `ChangeOption`).
7. **Migrate** `Examples/UICatalog/Scenarios/LinearRanges.cs` — three
   demo views, one per subclass.
8. **Update tests** — port to value-based assertions; new tests for
   `Value`/`ValueChanging`/`ValueChanged` on each subclass.
9. **(clet repo)** Add `LinearRangeClet : IClet<JsonObject?>` next to
   `RangeClet.cs`. Don't touch `RangeClet`.

## 6. Open questions for review

* **Naming of `LinearSelector<T>` / `LinearMultiSelector<T>`.**
  Alternatives: `LinearPicker<T>`, `LinearChoice<T>`,
  `RangeSelector<T>` (Multi). `LinearSelector` reads cleanly next to
  `OptionSelector` / `FlagSelector` and stays in the slider family.
* **Should the multi value be `ImmutableArray<T>` instead of
  `IReadOnlyList<T>`?** Better defensiveness, slightly heavier API.
  Lean toward `IReadOnlyList<T>` to match `IValue<T>` precedent
  (lightweight contract).
* **`LinearRangeOption<T>` keeps its `Set`/`UnSet`/`Changed` events?**
  These are useful per-option signals (e.g. "this tier was just
  selected"). Keep them on the base; they fire alongside the new
  `ValueChanged` event.
* **Single clet vs three clets in the CLI.** Single `linear-range`
  with `--mode` keeps `clet list` lean and matches the structural
  symmetry; the alternative would be `linear-select`, `linear-multi`,
  `linear-range`. Recommend single.
