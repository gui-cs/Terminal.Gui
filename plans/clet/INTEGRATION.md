# Wiring `linear-range` into clet

This folder is a drop kit for the [gui-cs/clet](https://github.com/gui-cs/clet)
repository. Terminal.Gui scope is restricted to `gui-cs/terminal.gui`, so
the file is delivered here for a maintainer to copy across.

Target Terminal.Gui version: develop (post-#5204), shipped as the
LinearRange `IValue<T>` family:

- `LinearSelector<T>      : LinearRangeViewBase<T, T>`                  — `IValue<T>`
- `LinearMultiSelector<T> : LinearRangeViewBase<T, IReadOnlyList<T>>`   — `IValue<IReadOnlyList<T>>`
- `LinearRange<T>         : LinearRangeViewBase<T, LinearRangeSpan<T>>` — `IValue<LinearRangeSpan<T>>`

## Files

| File | Destination |
|------|-------------|
| `LinearRangeClet.cs` | `src/Clet/Clets/Input/LinearRangeClet.cs` |

## Steps to land in clet

1. **Bump Terminal.Gui** to the develop NuGet that contains #5204.
2. **Drop `LinearRangeClet.cs`** under `src/Clet/Clets/Input/`.
3. **Register the clet** in the registry. clet auto-discovers via
   reflection over `IClet` implementations in
   `src/Clet/Registry/`; if discovery is manual, add an entry alongside
   `SelectClet` and `MultiSelectClet`.
4. **Help text / `clet list --json`** picks up the new clet automatically
   because `Description`, `Aliases`, and `Options` are reflected.
5. **Add a smoke test** mirroring the shape of `SelectClet`'s test:
   - non-interactive run with `--initial`, assert exit 0, JSON `mode == "single"`,
     `index` matches.
   - `--mode multi --initial "Pro,Team"`, assert `indices` set.
   - `--mode range --initial "Pro..Team"`, assert `kind == "closed"`,
     `startIndex < endIndex`.
6. **README** — add a row under "Input clets":
   ```
   | linear-range | LinearRange (single, multi, or bounded range) |
   ```
   And a usage block:
   ```bash
   clet linear-range --mode single --options "Free,Pro,Team,Enterprise" --initial "Pro" --json
   clet linear-range --mode multi  --options "Mon,Tue,Wed,Thu,Fri,Sat,Sun" --initial "Mon,Tue,Wed,Thu,Fri" --json
   clet linear-range --mode range  --range-kind closed --options "8,9,10,11,12,13,14,15,16,17,18" --initial "9..17" --json
   ```

## Distinct from `range`

`RangeClet` (the existing `range` command) takes a numeric `low..high`
with a `--step` and renders a custom numeric `RangeView`. That stays
unchanged. `linear-range` is the labelled-options companion that exposes
the full `LinearRange` family — different surface, different use case,
no overlap.

## JSON contract (re-stated for reviewers)

```jsonc
// --mode single
{ "schemaVersion":1, "status":"ok", "mode":"single",
  "value":"Pro", "index":1 }

// --mode multi
{ "schemaVersion":1, "status":"ok", "mode":"multi",
  "values":["Mon","Tue"], "indices":[0,1] }

// --mode range, --range-kind closed
{ "schemaVersion":1, "status":"ok", "mode":"range", "kind":"closed",
  "start":"9", "end":"17", "startIndex":1, "endIndex":9 }

// --mode range, --range-kind left
{ "schemaVersion":1, "status":"ok", "mode":"range", "kind":"left",
  "end":"17", "endIndex":9 }

// --mode range, --range-kind right
{ "schemaVersion":1, "status":"ok", "mode":"range", "kind":"right",
  "start":"9", "startIndex":1 }
```

The `schemaVersion` / `status` envelope is added by the clet host; the
clet itself returns the inner `JsonObject`.
