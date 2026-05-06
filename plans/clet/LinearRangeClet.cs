// Drop-in for gui-cs/clet at: src/Clet/Clets/Input/LinearRangeClet.cs
//
// Targets the LinearRange family that landed in Terminal.Gui via #5204:
//   - LinearSelector<T>      : LinearRangeViewBase<T, T>                   IValue<T>
//   - LinearMultiSelector<T> : LinearRangeViewBase<T, IReadOnlyList<T>>    IValue<IReadOnlyList<T>>
//   - LinearRange<T>         : LinearRangeViewBase<T, LinearRangeSpan<T>>  IValue<LinearRangeSpan<T>>
//
// All options are surfaced as labels (T = string). The clet returns a JsonObject whose
// shape depends on --mode, so AI agents can branch on a single, predictable schema:
//
//   --mode single → { schemaVersion:1, status:"ok", mode:"single", value:"Pro",       index:1 }
//   --mode multi  → { schemaVersion:1, status:"ok", mode:"multi",  values:[ ... ],   indices:[ ... ] }
//   --mode range  → { schemaVersion:1, status:"ok", mode:"range",  kind:"closed",
//                     start:"...", end:"...", startIndex:N, endIndex:M }
//
// Cancel / error envelopes follow the existing clet contract; the JSON envelope itself
// is built by the host (Hosting/JSON layer), so RunAsync only returns the typed payload.

using System.Text.Json.Nodes;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Clet;

internal sealed class LinearRangeClet : IClet<JsonObject?>
{
    public string PrimaryAlias => "linear-range";
    public IReadOnlyList<string> Aliases => ["linear-range"];

    public string Description =>
        "Presents a LinearRange (single, multi, or bounded range) over a list of labelled options "
        + "and returns the selection.";

    public CletKind Kind => CletKind.Input;
    public Type ResultType => typeof (JsonObject);

    public IReadOnlyList<CletOptionDescriptor> Options =>
    [
        new ("mode", "m", typeof (string),
             "Selection shape: 'single' (default), 'multi', or 'range'.",
             false, "single"),
        new ("options", "o", typeof (string),
             "Comma-separated list of options to display.", true, null),
        new ("orientation", null, typeof (string),
             "'horizontal' (default) or 'vertical'.", false, "horizontal"),
        new ("range-kind", "k", typeof (string),
             "Range shape when --mode=range: 'closed' (default), 'left' (= ≤ end), or 'right' (= ≥ start).",
             false, "closed"),
        new ("allow-empty", null, typeof (bool),
             "Permit a no-selection result.", false, "false"),
        new ("hide-legends", null, typeof (bool),
             "Suppress per-option legend text under the slider.", false, "false"),
    ];

    public bool AcceptsPositionalArgs => true;

    public async Task<CletRunResult<JsonObject?>> RunAsync (
        IApplication app,
        string? initial,
        CletRunOptions options,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new () { Status = CletRunStatus.Cancelled };
        }

        // ----- Parse CLI options -----

        string mode = (GetOption (options, "mode") ?? "single").Trim ().ToLowerInvariant ();
        string orientationStr = (GetOption (options, "orientation") ?? "horizontal").Trim ().ToLowerInvariant ();
        string rangeKindStr = (GetOption (options, "range-kind") ?? "closed").Trim ().ToLowerInvariant ();
        bool allowEmpty = ParseBool (GetOption (options, "allow-empty"));
        bool hideLegends = ParseBool (GetOption (options, "hide-legends"));

        Orientation orientation = orientationStr switch
        {
            "vertical" or "v" => Orientation.Vertical,
            _ => Orientation.Horizontal
        };

        string [] labels = options.Arguments is { Count: > 0 }
                               ? LabelParser.Split (options.Arguments)
                               : options.CletOptions?.TryGetValue ("options", out string? optionsValue) == true
                                   ? LabelParser.Split (optionsValue)
                                   : [];

        if (labels.Length == 0)
        {
            return new ()
            {
                Status = CletRunStatus.Error,
                ErrorCode = "validation",
                ErrorMessage = "linear-range requires --options or positional arguments.",
            };
        }

        // Build typed options (T = string) once; reused by whichever view we instantiate.
        List<LinearRangeOption<string>> linearOptions = labels
            .Select (s => new LinearRangeOption<string> (s, (Rune)(s.Length > 0 ? s [0] : ' '), s))
            .ToList ();

        // Dispatch on --mode.
        return mode switch
        {
            "multi" => await RunMulti (app, initial, options, labels, linearOptions, orientation,
                                       allowEmpty, hideLegends, cancellationToken),
            "range" => await RunRange (app, initial, options, labels, linearOptions, orientation,
                                       rangeKindStr, allowEmpty, hideLegends, cancellationToken),
            _ => await RunSingle (app, initial, options, labels, linearOptions, orientation,
                                  allowEmpty, hideLegends, cancellationToken),
        };
    }

    // -----------------------------------------------------------------------------
    // Single
    // -----------------------------------------------------------------------------

    private static async Task<CletRunResult<JsonObject?>> RunSingle (
        IApplication app,
        string? initial,
        CletRunOptions options,
        string [] labels,
        List<LinearRangeOption<string>> linearOptions,
        Orientation orientation,
        bool allowEmpty,
        bool hideLegends,
        CancellationToken cancellationToken)
    {
        LinearSelector<string> selector = new ()
        {
            Options = linearOptions,
            Orientation = orientation,
            AllowEmpty = allowEmpty,
            ShowLegends = !hideLegends,
            AssignHotKeys = true,
        };

        if (initial is { Length: > 0 })
        {
            int idx = FindLabelIndex (labels, initial);

            if (idx >= 0)
            {
                selector.SelectedIndex = idx;
            }
        }

        RunnableWrapper<LinearSelector<string>, string?> wrapper = new (selector)
        {
            Title = options.Title ?? "Pick one (Enter to accept, Esc to cancel)",
            Width = Dim.Fill (),
            BorderStyle = LineStyle.Rounded,
            SchemeName = CletStyling.BaseSchemeName,
        };
        wrapper.Border.Thickness = new Thickness (0, 1, 0, 0);

        try
        {
            await app.RunAsync (wrapper, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new () { Status = CletRunStatus.Cancelled };
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new () { Status = CletRunStatus.Cancelled };
        }

        string? value = wrapper.Result;
        int index = value is null ? -1 : FindLabelIndex (labels, value);

        JsonObject json = new ()
        {
            ["mode"] = "single",
            ["value"] = value,
            ["index"] = index,
        };

        return new () { Status = CletRunStatus.Ok, Value = json };
    }

    // -----------------------------------------------------------------------------
    // Multi
    // -----------------------------------------------------------------------------

    private static async Task<CletRunResult<JsonObject?>> RunMulti (
        IApplication app,
        string? initial,
        CletRunOptions options,
        string [] labels,
        List<LinearRangeOption<string>> linearOptions,
        Orientation orientation,
        bool allowEmpty,
        bool hideLegends,
        CancellationToken cancellationToken)
    {
        LinearMultiSelector<string> selector = new ()
        {
            Options = linearOptions,
            Orientation = orientation,
            AllowEmpty = allowEmpty,
            ShowLegends = !hideLegends,
            AssignHotKeys = true,
        };

        if (initial is { Length: > 0 })
        {
            string [] initialLabels = initial.Split (',');
            List<string> seeded = [];

            foreach (string lbl in labels)
            {
                if (Array.Exists (initialLabels, l => string.Equals (l.Trim (), lbl, StringComparison.OrdinalIgnoreCase)))
                {
                    seeded.Add (lbl);
                }
            }

            selector.Value = seeded;
        }

        RunnableWrapper<LinearMultiSelector<string>, IReadOnlyList<string>?> wrapper = new (selector)
        {
            Title = options.Title ?? "Pick one or more (Space to toggle, Enter to accept, Esc to cancel)",
            Width = Dim.Fill (),
            BorderStyle = LineStyle.Rounded,
            SchemeName = CletStyling.BaseSchemeName,
        };
        wrapper.Border.Thickness = new Thickness (0, 1, 0, 0);

        try
        {
            await app.RunAsync (wrapper, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new () { Status = CletRunStatus.Cancelled };
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new () { Status = CletRunStatus.Cancelled };
        }

        IReadOnlyList<string> result = wrapper.Result ?? [];

        JsonArray values = [];
        JsonArray indices = [];

        foreach (string v in result)
        {
            values.Add (JsonValue.Create (v));
            indices.Add (JsonValue.Create (FindLabelIndex (labels, v)));
        }

        JsonObject json = new ()
        {
            ["mode"] = "multi",
            ["values"] = values,
            ["indices"] = indices,
        };

        return new () { Status = CletRunStatus.Ok, Value = json };
    }

    // -----------------------------------------------------------------------------
    // Range
    // -----------------------------------------------------------------------------

    private static async Task<CletRunResult<JsonObject?>> RunRange (
        IApplication app,
        string? initial,
        CletRunOptions options,
        string [] labels,
        List<LinearRangeOption<string>> linearOptions,
        Orientation orientation,
        string rangeKindStr,
        bool allowEmpty,
        bool hideLegends,
        CancellationToken cancellationToken)
    {
        LinearRangeSpanKind kind = rangeKindStr switch
        {
            "left" or "left-bounded" or "l" => LinearRangeSpanKind.LeftBounded,
            "right" or "right-bounded" or "r" => LinearRangeSpanKind.RightBounded,
            _ => LinearRangeSpanKind.Closed,
        };

        LinearRange<string> view = new ()
        {
            Options = linearOptions,
            Orientation = orientation,
            AllowEmpty = allowEmpty,
            ShowLegends = !hideLegends,
            AssignHotKeys = true,
            RangeKind = kind,
        };

        if (initial is { Length: > 0 })
        {
            LinearRangeSpan<string> seed = ParseInitialSpan (initial, labels, kind);

            if (seed.Kind != LinearRangeSpanKind.None)
            {
                view.Value = seed;
            }
        }

        RunnableWrapper<LinearRange<string>, LinearRangeSpan<string>> wrapper = new (view)
        {
            Title = options.Title ?? "Pick a range (Ctrl+Left/Right to extend, Enter to accept, Esc to cancel)",
            Width = Dim.Fill (),
            BorderStyle = LineStyle.Rounded,
            SchemeName = CletStyling.BaseSchemeName,
        };
        wrapper.Border.Thickness = new Thickness (0, 1, 0, 0);

        try
        {
            await app.RunAsync (wrapper, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new () { Status = CletRunStatus.Cancelled };
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new () { Status = CletRunStatus.Cancelled };
        }

        LinearRangeSpan<string> span = wrapper.Result;

        JsonObject json = new ()
        {
            ["mode"] = "range",
            ["kind"] = SpanKindToString (span.Kind),
        };

        // Only emit fields that are meaningful for the kind.
        switch (span.Kind)
        {
            case LinearRangeSpanKind.None:
                json ["start"] = null;
                json ["end"] = null;
                json ["startIndex"] = -1;
                json ["endIndex"] = -1;

                break;

            case LinearRangeSpanKind.LeftBounded:
                json ["end"] = span.End;
                json ["endIndex"] = span.EndIndex;

                break;

            case LinearRangeSpanKind.RightBounded:
                json ["start"] = span.Start;
                json ["startIndex"] = span.StartIndex;

                break;

            case LinearRangeSpanKind.Closed:
                json ["start"] = span.Start;
                json ["end"] = span.End;
                json ["startIndex"] = span.StartIndex;
                json ["endIndex"] = span.EndIndex;

                break;
        }

        return new () { Status = CletRunStatus.Ok, Value = json };
    }

    // -----------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------

    private static string? GetOption (CletRunOptions options, string name)
    {
        if (options.CletOptions is null)
        {
            return null;
        }

        return options.CletOptions.TryGetValue (name, out string? v) ? v : null;
    }

    private static bool ParseBool (string? raw)
    {
        if (string.IsNullOrWhiteSpace (raw))
        {
            return false;
        }

        return raw.Trim ().ToLowerInvariant () is "1" or "true" or "yes" or "on";
    }

    private static int FindLabelIndex (string [] labels, string value)
    {
        return Array.FindIndex (labels, l => string.Equals (l, value, StringComparison.OrdinalIgnoreCase));
    }

    private static string SpanKindToString (LinearRangeSpanKind kind) => kind switch
    {
        LinearRangeSpanKind.LeftBounded => "left",
        LinearRangeSpanKind.RightBounded => "right",
        LinearRangeSpanKind.Closed => "closed",
        _ => "none",
    };

    // Parses initial range strings:
    //   "Pro..Team"  → Closed
    //   "..Team"     → LeftBounded (overrides explicit kind only when explicit kind is Closed)
    //   "Pro.."      → RightBounded
    //   "Pro"        → single point, mapped per requestedKind
    // Returns LinearRangeSpan<string>.Empty if nothing valid parses.
    private static LinearRangeSpan<string> ParseInitialSpan (string initial, string [] labels, LinearRangeSpanKind requestedKind)
    {
        string trimmed = initial.Trim ();

        if (trimmed.Length == 0)
        {
            return LinearRangeSpan<string>.Empty;
        }

        int sep = trimmed.IndexOf ("..", StringComparison.Ordinal);

        if (sep < 0)
        {
            // Single label — interpret per requested kind.
            int idx = FindLabelIndex (labels, trimmed);

            if (idx < 0)
            {
                return LinearRangeSpan<string>.Empty;
            }

            return requestedKind switch
            {
                LinearRangeSpanKind.LeftBounded => new (LinearRangeSpanKind.LeftBounded, default, labels [idx], -1, idx),
                LinearRangeSpanKind.RightBounded => new (LinearRangeSpanKind.RightBounded, labels [idx], default, idx, -1),
                _ => new (LinearRangeSpanKind.Closed, labels [idx], labels [idx], idx, idx),
            };
        }

        string left = trimmed [..sep].Trim ();
        string right = trimmed [(sep + 2)..].Trim ();

        bool hasLeft = left.Length > 0;
        bool hasRight = right.Length > 0;

        int leftIdx = hasLeft ? FindLabelIndex (labels, left) : -1;
        int rightIdx = hasRight ? FindLabelIndex (labels, right) : -1;

        if (hasLeft && !hasRight && leftIdx >= 0)
        {
            return new (LinearRangeSpanKind.RightBounded, labels [leftIdx], default, leftIdx, -1);
        }

        if (!hasLeft && hasRight && rightIdx >= 0)
        {
            return new (LinearRangeSpanKind.LeftBounded, default, labels [rightIdx], -1, rightIdx);
        }

        if (hasLeft && hasRight && leftIdx >= 0 && rightIdx >= 0)
        {
            // Normalise so Start ≤ End by index.
            int lo = Math.Min (leftIdx, rightIdx);
            int hi = Math.Max (leftIdx, rightIdx);

            return new (LinearRangeSpanKind.Closed, labels [lo], labels [hi], lo, hi);
        }

        return LinearRangeSpan<string>.Empty;
    }
}
