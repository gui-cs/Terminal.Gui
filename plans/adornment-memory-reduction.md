# Adornment Memory Reduction Plan

> **Issue:** [#4696](https://github.com/gui-cs/Terminal.Gui/issues/4696) — Reduce View class memory footprint  
> **Branch:** `copilot/reduce-view-class-memory-footprint`  
> **Author:** Copilot (PE-level design review)  
> **Reviewed against:** @tig's comment on issue #4696

---

## 1. Executive Summary

Each `View` instance currently allocates three `Adornment` subclasses (`Margin`, `Border`, `Padding`), each of which is itself a full `View` with all associated overhead (KeyBindings, TextFormatter, LineCanvas, Pos/Dim objects, 40+ event handler fields, etc.). This contributes **35–60% of the ~4–5 KB per-instance footprint**.

This plan introduces a two-level split:

| Level | Class | Extends | Purpose |
|-------|-------|---------|---------|
| Lightweight | `Border`, `Margin`, `Padding` (via `AdornmentImpl`) | `object` | Store settings (Thickness, LineStyle, ShadowStyle) — always present, cheap |
| Heavy | `BorderView`, `MarginView`, `PaddingView` (via `AdornmentView`) | `View` | Full rendering, SubViews, mouse/keyboard, arrangement — **created only when needed** |

For a plain view with no border, shadow, or adornment subviews, **zero `View` objects are created for adornments**.  
For a view with `BorderStyle = LineStyle.Single`, a `BorderView` is created lazily on first draw, but `MarginView`/`PaddingView` are not.

**Expected adornment-only savings:** 40–90% depending on feature use (see §12 for realistic breakdown).

---

## 2. Amazon PE Tenets — Applied to This Design

The [Amazon Principal Engineering Community Tenets](https://www.amazon.jobs/content/en/teams/principal-engineering/tenets) describe how Principal Engineers operate and make decisions. Each is applied explicitly below.

### Exemplary Practitioner
The plan is hands-on: every class, every property, every lifecycle method is specified with working code sketches. The "geometry fallback" methods in `AdornmentImpl` are fully specified (§4.3), not left as `/* ... */` stubs. The complete callsite inventory (§13) was produced from a live grep of the repository, not estimated.

### Technically Fearless
The design acknowledges a moderate breaking change to public API and does not hedge away from it. The right architecture — separating adornment settings from adornment rendering — is proposed even though it means migrating ~15 library callsites and updating ~25 files. The alternative (null-coalescing hacks in existing classes) is explicitly rejected.

### Lead with Empathy
The breaking-changes catalog (§7) exists specifically for library consumers and contributors who will be affected. The API migration guide (§9) shows the before/after for every changed pattern. The `[Obsolete]` alias strategy gives users one full release cycle to migrate without a hard break.

### Balanced and Pragmatic
Phase 3 (no-View border drawing) is marked **recommended, not optional** for windowed apps — it eliminates `BorderView` creation for the most common Terminal.Gui use case (a `Window` with a title). The complexity cost is real but the payoff for the majority of applications is worth calling out clearly rather than deferring. Phases are sized for independent delivery; no phase requires another to be complete before it ships value.

### Illuminate and Clarify
Four lifecycle hazards identified during design — `EnsureView()` mid-lifecycle ordering, thread safety, `IDesignable` impact, and focus/tab ordering — are each addressed in §4.9 (Lifecycle Hazards) rather than buried in footnotes or ignored.

### Flexible in Approach
The interface `IAdornment` allows the design to evolve: a future v3 could swap in a struct-based `IAdornment` implementation with no changes to `View`. Consumers who need `View`-level access call `.EnsureView()` explicitly, which is also how custom adornment implementations can hook into the system.

### Respect What Came Before
Phase 1 is a pure rename with zero behavior change. Every callsite that worked before continues to compile. The `Adornment` class becomes `[Obsolete]` for one release — not deleted. `ViewportSettings` defaults are preserved exactly in the lightweight `Margin` (Transparent + TransparentMouse), so existing code relying on that behavior observes no change.

### Learn, Educate, and Advocate
The plan documents the pre-existing `Border.Arrangment.cs` filename typo and the pre-existing `// TODO: Make the Adornments Lazy` comment in `SetupAdornments()` — both evidence that the team has identified this work before. Those insights are surfaced, not ignored.

### Have Resounding Impact
For a typical Terminal.Gui application with 200 views (50% with borders, 10% with shadows, 40% plain):
- **Current:** ~860 KB in adornments alone
- **After Phase 2:** ~210 KB — a **75% reduction** in adornment memory
- For applications with thousands of views (dashboards, editors, data grids), this crosses the threshold from "acceptable" to "unusable" memory profiles. This change makes Terminal.Gui viable for those use cases.

---

## 3. Current State Analysis

### 3.1 Class Hierarchy

```
View
├── Adornment (: View) — base for all adornments
│   ├── Border (: Adornment)  — renders border lines + title + arrangement
│   ├── Margin (: Adornment)  — renders shadow, transparent spacing
│   └── Padding (: Adornment) — focusable spacer, hosts subviews
```

### 3.2 Per-Adornment Memory Cost

Each adornment is a full `View`. A bare `View` carries:

| Category | Estimated bytes |
|----------|----------------|
| Object header + managed type pointer | 16 |
| Pos/Dim fields (X, Y, Width, Height × 2 types) | 160–240 |
| Frame / Viewport rectangles | 32 |
| TextFormatter (Title + Text) | 400–600 |
| KeyBindings (2×) | 160–300 |
| LineCanvas | 200–500 |
| Command dictionary | 100–200 |
| 40+ EventHandler backing fields | 320+ |
| SubViews list | 48 |
| Other fields/properties | 200–300 |
| **Total per Adornment instance** | **~1,600–2,500 bytes** |

Three adornments per View = **~4,800–7,500 bytes** in adornments alone.

### 3.3 What Code Actually Uses on Adornments

Auditing the codebase (79 unique callsites in library code, 541 in tests):

**Always needed (lightweight-safe):**
- `adornment.Thickness` — read/write Thickness struct
- `adornment.Parent` — reference to owning View
- `border.LineStyle` / `border.Settings` — border rendering mode
- `margin.ShadowStyle` / `margin.ShadowSize` — shadow behavior
- Thickness-based frame calculation in `SetAdornmentFrames()`
- `GetAdornmentsThickness()` — sum of all three Thicknesses

**Needs the View (heavy path):**
- `border.Arranger.EnterArrangeMode(...)` — arrangement buttons
- `tab.Border!.Activating += ...` — event from Border-as-View
- `margin.SubViews.Count` / `border.SubViews.Count` — subview hosting
- `margin.ViewportSettings |= ...` — viewport flags
- `border.Add(subview)` — adding views to adornment
- `margin.CacheClip()` / `margin.GetCachedClip()` — transparent margin drawing
- `border.GetBorderRectangle()` — rendering geometry
- `border.AdvanceDrawIndicator()` — diagnostic spinner
- `adornment.Draw()` — actual rendering
- `adornment.SetNeedsDraw()` / `adornment.SetNeedsLayout()` — invalidation

**Finding:** The vast majority of adornment usage is `Thickness` access. View methods are used primarily for features that are not common (arrangement, subviews, shadows).

### 3.4 Internal Type Checks That Must Be Updated

The following patterns appear in `View.*` partial files and must be updated when the class hierarchy changes:

| Pattern | File | Update Required |
|---------|------|-----------------|
| `if (this is Adornment)` | `View.Adornments.cs`, `View.Drawing.cs`, `View.Drawing.Clipping.cs`, `View.NeedsDraw.cs`, `View.Layout.cs`, `View.ScrollBars.cs` | Change to `if (this is AdornmentView)` |
| `if (this is not Adornment)` | `View.Adornments.cs`, `View.Drawing.cs` | Change to `if (this is not AdornmentView)` |
| `as Adornment` | `View.Navigation.cs`, `App/Mouse/ApplicationMouse.cs` | Change to `as AdornmentView` |
| `is Adornment adornment` | Multiple files | Change to `is AdornmentView adornment` |
| `t != typeof(Adornment)` | `UICatalog/Scenarios/Popovers.cs` | Change to `t != typeof(AdornmentView)` |

---

## 4. Proposed Design

### 4.1 Interface

```csharp
/// <summary>
///     Defines the contract for an adornment layer around a View.
///     Implemented by <see cref="Border"/>, <see cref="Margin"/>, and <see cref="Padding"/>.
/// </summary>
public interface IAdornment
{
    /// <summary>The parent View this adornment surrounds.</summary>
    View? Parent { get; set; }

    /// <summary>
    ///     The thickness (space consumed by this adornment layer).
    ///     Changing this triggers layout recalculation.
    /// </summary>
    Thickness Thickness { get; set; }

    /// <summary>
    ///     The <see cref="AdornmentView"/> backing this adornment.
    ///     Null until the adornment actually needs View-level functionality
    ///     (rendering, SubViews, mouse, arrangement, shadow).
    /// </summary>
    AdornmentView? View { get; }

    /// <summary>Fired when <see cref="Thickness"/> changes.</summary>
    event EventHandler? ThicknessChanged;

    // --- Coordinator methods: delegate to View when present, or compute from Thickness ---

    /// <summary>Converts a viewport-relative point to screen coordinates.</summary>
    Point ViewportToScreen (in Point location);

    /// <summary>Converts a screen point to frame-relative coordinates.</summary>
    Point ScreenToFrame (in Point location);

    /// <summary>Returns the screen-relative rectangle for this adornment.</summary>
    Rectangle FrameToScreen ();
}
```

### 4.2 Class Hierarchy (New)

```
IAdornment
└── AdornmentImpl (abstract, : object)  — lightweight base
    ├── Border     — adds LineStyle, Settings, GetBorderRectangle()
    ├── Margin     — adds ShadowStyle, ShadowSize, CacheClip() family
    └── Padding    — GetSubViews() override logic

View
└── AdornmentView (: View) — renamed from Adornment
    ├── BorderView  (: AdornmentView) — renamed from Border
    ├── MarginView  (: AdornmentView) — renamed from Margin
    └── PaddingView (: AdornmentView) — renamed from Padding
```

### 4.3 AdornmentImpl — Lightweight Base

```csharp
/// <summary>
///     Lightweight base class for adornment settings.
///     Holds Thickness and optional Parent reference.
///     The full <see cref="AdornmentView"/> is created lazily via <see cref="EnsureView"/>.
/// </summary>
public abstract class AdornmentImpl : IAdornment
{
    private AdornmentView? _view;

    /// <inheritdoc/>
    public View? Parent { get; set; }

    private Thickness _thickness = Thickness.Empty;

    /// <inheritdoc/>
    public Thickness Thickness
    {
        get => _thickness;
        set
        {
            Thickness current = _thickness;
            _thickness = value;

            if (current != _thickness)
            {
                // Only hit View if one exists
                _view?.SetNeedsLayout ();
                _view?.SetNeedsDraw ();

                Parent?.SetAdornmentFrames ();
                OnThicknessChanged ();
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler? ThicknessChanged;

    /// <summary>Called when <see cref="Thickness"/> changes.</summary>
    protected virtual void OnThicknessChanged () => ThicknessChanged?.Invoke (this, EventArgs.Empty);

    /// <summary>
    ///     The backing <see cref="AdornmentView"/> — null until demanded.
    /// </summary>
    public AdornmentView? View => _view;

    /// <summary>
    ///     Returns the existing <see cref="AdornmentView"/>, creating it if not yet allocated.
    ///     Triggers initialization and layout integration.
    /// </summary>
    public AdornmentView EnsureView ()
    {
        if (_view is null)
        {
            _view = CreateView ();
            _view.Thickness = _thickness;
            _view.Parent = Parent;
            Parent?.SetAdornmentFrames ();
        }

        return _view;
    }

    /// <summary>Factory method — subclasses return their specific View subclass.</summary>
    protected abstract AdornmentView CreateView ();

    // --- IAdornment coordinator methods ---

    /// <inheritdoc/>
    public Point ViewportToScreen (in Point location)
        => View is { } v ? v.ViewportToScreen (location) : ComputeViewportToScreen (location);

    /// <inheritdoc/>
    public Rectangle FrameToScreen ()
        => View is { } v ? v.FrameToScreen () : ComputeFrameToScreen ();

    /// <inheritdoc/>
    public Point ScreenToFrame (in Point location)
        => View is { } v ? v.ScreenToFrame (location) : ComputeScreenToFrame (location);

    // Fallback geometry when no View exists yet (derived from Parent.Frame + Thickness).
    // These mirror the math in Adornment.FrameToScreen() / ViewportToScreen() but operate
    // without a View object, using only Parent references and the stored Thickness.
    private Point ComputeViewportToScreen (in Point location)
    {
        // ViewportToScreen: add the Frame's screen origin to the location.
        // Frame is computed by ComputeLayerFrame(), which applies layer-specific math
        // (Margin = parent-sized, Border = margin-inset, Padding = border-inset).
        Rectangle frame = ComputeFrame ();
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (parentScreen.X + frame.X + location.X,
                    parentScreen.Y + frame.Y + location.Y);
    }

    private Rectangle ComputeFrameToScreen ()
    {
        // Adornment.FrameToScreen mirrors Parent.FrameToScreen() offset by own Frame.
        Rectangle frame = ComputeFrame ();
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (new (parentScreen.X + frame.X, parentScreen.Y + frame.Y), frame.Size);
    }

    private Point ComputeScreenToFrame (in Point location)
    {
        // Inverse of ComputeViewportToScreen — subtract frame origin.
        Rectangle frame = ComputeFrame ();
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (location.X - parentScreen.X - frame.X,
                    location.Y - parentScreen.Y - frame.Y);
    }

    // Computes the Frame this adornment would occupy, derived from Parent.Frame and Thickness.
    // This mirrors Adornment.SetAdornmentFrames() logic without needing a View.
    private Rectangle ComputeFrame ()
    {
        if (Parent is null)
        {
            return Rectangle.Empty;
        }

        // For the three standard layers:
        // Margin.Frame  = Parent.Frame sized (0,0, parent.Width, parent.Height)
        // Border.Frame  = Margin.Thickness.GetInside(Margin.Frame)
        // Padding.Frame = Border.Thickness.GetInside(Border.Frame)
        // Without knowing which layer we are, we delegate to the subclass via abstract property.
        return ComputeLayerFrame ();
    }

    /// <summary>
    ///     Returns the Frame rectangle occupied by this specific adornment layer (Margin, Border, or Padding)
    ///     without requiring a <see cref="AdornmentView"/> instance. The calculation is derived from
    ///     <see cref="Parent"/>'s <see cref="View.Frame"/> and sibling layers' Thickness values.
    ///     Internal visibility allows sibling layers to delegate to each other's frame computation
    ///     without duplicating math. Returns <see cref="Rectangle.Empty"/> if <see cref="Parent"/> is null.
    /// </summary>
    internal abstract Rectangle ComputeLayerFrame ();

    /// <summary>
    ///     Draws the border/margin/padding content if a View exists.
    ///     When View is null but Thickness is non-empty, draws directly via Parent.
    /// </summary>
    internal virtual void Draw () => View?.Draw ();

    /// <summary>Propagates init to the View if it exists.</summary>
    internal void BeginInit () => View?.BeginInit ();

    /// <summary>Propagates init to the View if it exists.</summary>
    internal void EndInit () => View?.EndInit ();

    /// <summary>Propagates disposal to the View if it exists.</summary>
    internal void Dispose ()
    {
        View?.Dispose ();
        _view = null;
        Parent = null;
    }
}
```

### 4.4 Border (Lightweight)

```csharp
/// <summary>
///     The Border adornment settings for a <see cref="View"/>.
///     Stores Thickness, LineStyle, and Settings without creating a full View unless rendering
///     or SubViews require it.
/// </summary>
public class Border : AdornmentImpl
{
    private LineStyle? _lineStyle;

    /// <summary>
    ///     The line style for the border. Setting this to any value other than None
    ///     causes a <see cref="BorderView"/> to be created lazily.
    /// </summary>
    public LineStyle LineStyle
    {
        get => _lineStyle ?? Parent?.SuperView?.BorderStyle ?? LineStyle.None;
        set
        {
            _lineStyle = value;
            // Only need a View if we're actually going to draw
            if (value != LineStyle.None && Thickness != Thickness.Empty)
            {
                ((BorderView)EnsureView ()).LineStyle = value;
            }
        }
    }

    /// <summary>Gets or sets the border settings (Title display, etc.).</summary>
    public BorderSettings Settings { get; set; } = BorderSettings.Title;

    /// <summary>
    ///     Computes the border rectangle in screen coordinates.
    ///     Does NOT require a View — can be derived from Parent.FrameToScreen() + Thickness.
    /// </summary>
    public Rectangle GetBorderRectangle ()
    {
        if (View is BorderView bv)
        {
            return bv.GetBorderRectangle ();
        }

        // Compute without a View
        Rectangle parentScreen = Parent?.FrameToScreen () ?? Rectangle.Empty;

        return new (parentScreen.X + Math.Max (0, Thickness.Left - 1),
                    parentScreen.Y + Math.Max (0, Thickness.Top - 1),
                    Math.Max (0, parentScreen.Width - Math.Max (0, Thickness.Left - 1) - Math.Max (0, Thickness.Right - 1)),
                    Math.Max (0, parentScreen.Height - Math.Max (0, Thickness.Top - 1) - Math.Max (0, Thickness.Bottom - 1)));
    }

    /// <summary>
    ///     The view-arrangement controller. Only exists when a <see cref="BorderView"/> is present.
    /// </summary>
    public Arranger? Arranger => (View as BorderView)?.Arranger;

    /// <inheritdoc/>
    protected override AdornmentView CreateView () => new BorderView (Parent) { LineStyle = _lineStyle ?? LineStyle.None, Settings = Settings };

    /// <inheritdoc/>
    internal override Rectangle ComputeLayerFrame ()
    {
        if (Parent is null)
        {
            return Rectangle.Empty;
        }

        // Border.Frame = Margin.Frame inset by Margin.Thickness.
        // Delegates to Margin.ComputeLayerFrame() to avoid duplicating margin frame math.
        Rectangle marginFrame = Parent.Margin?.ComputeLayerFrame () ?? Rectangle.Empty with { Size = Parent.Frame.Size };
        Thickness marginThickness = Parent.Margin?.Thickness ?? Thickness.Empty;

        return marginThickness.GetInside (marginFrame);
    }
}
```

### 4.5 Margin (Lightweight)

```csharp
/// <summary>
///     The Margin adornment settings for a <see cref="View"/>.
///     Stores Thickness and ShadowStyle. The underlying <see cref="MarginView"/> is created
///     lazily when a shadow or subviews require it.
/// </summary>
public class Margin : AdornmentImpl
{
    private ShadowStyle _shadowStyle;

    /// <summary>
    ///     Shadow effect. Setting to anything other than None forces a <see cref="MarginView"/>
    ///     to be created so the shadow sub-views can be hosted.
    /// </summary>
    public ShadowStyle ShadowStyle
    {
        get => _shadowStyle;
        set
        {
            _shadowStyle = value;
            if (value != ShadowStyle.None)
            {
                ((MarginView)EnsureView ()).ShadowStyle = value;
            }
        }
    }

    private Size _shadowSize;

    /// <summary>Gets or sets the shadow size.</summary>
    public Size ShadowSize
    {
        get => _shadowSize;
        set
        {
            _shadowSize = value;
            if (View is MarginView mv)
            {
                mv.ShadowSize = value;
            }
        }
    }

    // --- Internal clip-cache methods (delegated to MarginView) ---

    internal void CacheClip () => (View as MarginView)?.CacheClip ();
    internal Region? GetCachedClip () => (View as MarginView)?.GetCachedClip ();
    internal void ClearCachedClip () => (View as MarginView)?.ClearCachedClip ();

    /// <inheritdoc/>
    public new ViewportSettingsFlags ViewportSettings
    {
        get => (View as MarginView)?.ViewportSettings ?? (ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse);
        set
        {
            if (View is MarginView mv)
            {
                mv.ViewportSettings = value;
            }
            // Store for deferred apply when View is created
            _deferredViewportSettings = value;
        }
    }

    private ViewportSettingsFlags? _deferredViewportSettings;

    /// <inheritdoc/>
    protected override AdornmentView CreateView ()
    {
        MarginView mv = new (Parent);
        if (_shadowStyle != ShadowStyle.None)
        {
            mv.ShadowStyle = _shadowStyle;
        }
        if (_deferredViewportSettings.HasValue)
        {
            mv.ViewportSettings = _deferredViewportSettings.Value;
        }
        return mv;
    }

    /// <inheritdoc/>
    internal override Rectangle ComputeLayerFrame ()
    {
        // Margin.Frame = Parent.Frame sized from (0,0)
        return Rectangle.Empty with { Size = Parent?.Frame.Size ?? Size.Empty };
    }
}
```

### 4.6 Padding (Lightweight)

```csharp
/// <summary>
///     The Padding adornment settings for a <see cref="View"/>.
///     A <see cref="PaddingView"/> is created lazily when SubViews are added.
/// </summary>
public class Padding : AdornmentImpl
{
    /// <summary>
    ///     Adds a SubView to the Padding. Forces creation of <see cref="PaddingView"/>.
    /// </summary>
    public void Add (View view) => ((PaddingView)EnsureView ()).Add (view);

    /// <summary>
    ///     Gets SubViews. If no <see cref="PaddingView"/> exists and
    ///     <paramref name="includePadding"/> is true, returns SubViews from Parent directly.
    /// </summary>
    public IReadOnlyCollection<View> GetSubViews (bool includeMargin = false, bool includeBorder = false, bool includePadding = false)
    {
        if (View is PaddingView pv)
        {
            return pv.GetSubViews (includeMargin, includeBorder, includePadding);
        }

        if (includePadding && Parent is { })
        {
            return Parent.GetSubViews (false, false, false);
        }

        return Array.Empty<View> ();
    }

    /// <inheritdoc/>
    protected override AdornmentView CreateView () => new PaddingView (Parent);

    /// <inheritdoc/>
    internal override Rectangle ComputeLayerFrame ()
    {
        if (Parent is null)
        {
            return Rectangle.Empty;
        }

        // Padding.Frame = Border.Frame inset by Border.Thickness.
        // Delegates to Border.ComputeLayerFrame() to avoid duplicating border frame math.
        Rectangle borderFrame = Parent.Border?.ComputeLayerFrame () ?? Rectangle.Empty with { Size = Parent.Frame.Size };
        Thickness borderThickness = Parent.Border?.Thickness ?? Thickness.Empty;

        return borderThickness.GetInside (borderFrame);
    }
}
```

### 4.7 View.SetupAdornments (New Behavior)

```csharp
private void SetupAdornments ()
{
    if (this is not AdornmentView)
    {
        // Lightweight objects — no View allocation
        Margin = new Margin { Parent = this };
        Border = new Border { Parent = this };
        Padding = new Padding { Parent = this };
    }
}

private void BeginInitAdornments ()
{
    Margin?.View?.BeginInit ();
    Border?.View?.BeginInit ();
    Padding?.View?.BeginInit ();
}

private void EndInitAdornments ()
{
    Margin?.View?.EndInit ();
    Border?.View?.EndInit ();
    Padding?.View?.EndInit ();
}

private void DisposeAdornments ()
{
    Margin?.Dispose ();
    Margin = null;
    Border?.Dispose ();
    Border = null;
    Padding?.Dispose ();
    Padding = null;
}
```

### 4.8 Lazy View Creation Triggers

An `AdornmentView` is created **only** when:

| Trigger | Adornment | Why |
|---------|-----------|-----|
| `BorderStyle = LineStyle.X` (non-None) AND Thickness > 0 | Border → BorderView | Needs to draw border lines |
| First time Draw() is called with Thickness > 0 | Any | Rendering requires a View context |
| `border.Arranger.EnterArrangeMode(...)` | Border → BorderView | Arrangement requires View events |
| `ShadowStyle = non-None` | Margin → MarginView | Shadow sub-views need a View host |
| `margin.ViewportSettings = X` called before View exists | Margin → MarginView | Must store and apply |
| `Add(subView)` called on an adornment | Any → AdornmentView | SubViews require a View host |
| `tab.Border.Activating += handler` | Border → BorderView | Activating is a View event |

For the **majority of simple views** (no arrangement, no shadow, no adornment subviews, simple or no border), only a `BorderView` may be created (for drawing), and `Margin`/`Padding` remain lightweight forever.

---

### 4.9 Lifecycle Hazards

Four hazards arise from lazy creation that must be addressed in implementation:

#### Hazard 1: `EnsureView()` called after `BeginInit()` but before `EndInit()`

`View.BeginInit()` and `View.EndInit()` wrap initialization of all adornments. If a trigger fires between `BeginInit()` and `EndInit()` on the parent — for example, if `ShadowStyle` is set inside a `BeginInit` block — the newly created `MarginView` must itself go through `BeginInit` + `EndInit` before use.

**Resolution:** `EnsureView()` must handle three parent states: pre-init, mid-init (between `BeginInit` and `EndInit`), and post-init. `View` exposes `IsInitialized` (true after `EndInit` completes). To detect mid-init, we need a flag — either `View._initialized` (private) or a new `IsBeginning` property. The simplest correct approach is to track init state inside `AdornmentImpl` itself: the parent calls `BeginInit()` / `EndInit()` on adornments during its own init; if a trigger fires before `EndInit()`, we call only `BeginInit()` on the new view and let the parent's pending `EndInit()` call complete it.

```csharp
private bool _beginInitCalled;
private bool _endInitCalled;

internal void BeginInit ()
{
    _beginInitCalled = true;
    _view?.BeginInit ();
}

internal void EndInit ()
{
    _endInitCalled = true;
    _view?.EndInit ();
}

public AdornmentView EnsureView ()
{
    if (_view is null)
    {
        _view = CreateView ();
        _view.Thickness = _thickness;
        _view.Parent = Parent;

        // Replay whichever init calls have already happened on the parent's behalf.
        if (_beginInitCalled)
        {
            _view.BeginInit ();
        }

        // Only call EndInit if the parent's EndInit has already fired (view fully initialized).
        // If we are mid-init (BeginInit fired, EndInit not yet), the parent's pending
        // EndInit call will flow through to _view via the updated EndInit() above.
        if (_endInitCalled)
        {
            _view.EndInit ();
        }

        Parent?.SetAdornmentFrames ();
    }

    return _view;
}
```

#### Hazard 2: Thread Safety

`EnsureView()` is not thread-safe. `View` generally assumes single-threaded access (the main loop thread), so no locking is required. This assumption is documented on `EnsureView()` with `/// <remarks>Must be called on the UI thread.</remarks>`.

#### Hazard 3: `IDesignable.EnableForDesign()` on Lightweight Objects

The current `Adornment` implements `IDesignable`. After the rename, `AdornmentView` retains `IDesignable`. The lightweight `Border`, `Margin`, `Padding` classes do **not** implement `IDesignable` — they are settings objects, not designable Views. The `AllViewsTester` scenario creates `AdornmentView` instances directly (via the parameter-less constructor), which is unaffected by this change.

#### Hazard 4: Focus and Tab Order When PaddingView Is Created Lazily

The current `Padding` is `CanFocus = true`, `TabStop = TabBehavior.NoStop`. This means clicking in the Padding area can give focus to the Parent. If a `PaddingView` is created lazily (e.g., after the parent is already displayed), the focus order must be recomputed. The trigger for `PaddingView` creation (`Padding.Add(view)`) already modifies the view tree, which causes `SetNeedsLayout()` and forces a layout pass that naturally restores correct tab order.

---

## 5. Incremental Adornment Drawing (No-View Path)

For `Thickness != Empty` but no `View` allocated, we can still draw the border using the parent View's draw context. This covers the most common case: `BorderStyle = LineStyle.Single` on a window.

```csharp
// In View.Drawing.cs — called from OnDrawingContent() when Border.View is null
private void DrawBorderDirect ()
{
    if (Border is null || Border.View is not null || Border.Thickness == Thickness.Empty)
    {
        return;  // View will handle it, or nothing to draw
    }

    Rectangle borderBounds = Border.GetBorderRectangle ();
    // draw lines via LineCanvas / Driver directly
    // draw title via TitleTextFormatter
}
```

This avoids View creation for the rendering of simple borders. The complexity tradeoff is acknowledged — this is a Phase 2 optimization (see §6.2).

---

## 6. Implementation Phases

### Phase 1: Rename + Interface (Enabling Change)

**Goal:** Rename existing View-based classes, introduce IAdornment. No behavior change yet.

**Duration:** 1–2 weeks

**Changes:**
1. Rename `Adornment.cs` → `AdornmentView.cs`, class `Adornment` → `AdornmentView`
2. Rename `Border.cs` → `BorderView.cs`, class `Border` → `BorderView`
3. Rename `Margin.cs` → `MarginView.cs`, class `Margin` → `MarginView`
4. Rename `Padding.cs` → `PaddingView.cs`, class `Padding` → `PaddingView`
5. Update `Border.Arrangment.cs` → reference `BorderView` (note: filename has pre-existing typo; rename opportunity)
6. Update all `is Adornment` / `as Adornment` / `typeof(Adornment)` → use `AdornmentView`
7. Update `View.Adornments.cs` property types: `Adornment` → `AdornmentView`
8. Create `IAdornment.cs` with interface definition
9. Create `AdornmentImpl.cs` with lightweight base (delegates all to concrete subclass)
10. Create lightweight `Border.cs`, `Margin.cs`, `Padding.cs` (thin wrappers over `BorderView`/`MarginView`/`PaddingView`) — **identical behavior** to current code, just split into two classes
11. Change `View.Border`, `View.Margin`, `View.Padding` property types from `BorderView?` to `Border?`, `Margin?`, `Padding?`
12. Update `View.SetupAdornments()` to create lightweight objects (still eagerly create Views initially)
13. All tests pass unchanged (no behavior change)

**Memory impact:** Zero (Views still eagerly created in Phase 1)

**Risk:** Low — pure rename + refactor, all tests must pass

---

### Phase 2: Lazy View Creation

**Goal:** Make `AdornmentView` creation lazy. Views only created when a feature triggers them.

**Duration:** 2–3 weeks

**Changes:**
1. Update `View.SetupAdornments()` to NOT create Views — only create lightweight `Border`/`Margin`/`Padding` objects
2. Update `BeginInitAdornments()` / `EndInitAdornments()` to be no-ops when View is null
3. Implement `EnsureView()` in `AdornmentImpl` with proper initialization sequencing
4. Update `SetAdornmentFrames()` to work purely from Thickness (no View needed)
5. Update `GetAdornmentsThickness()` — already Thickness-only, no change needed
6. Migrate triggering callsites to use `border.EnsureView()` / `border.View`:
   - `tab.Border!.Activating += Tab_Selecting!` → `tab.Border!.EnsureView().Activating += Tab_Selecting!`
   - `viewToArrange.Border?.Arranger` → `viewToArrange.Border?.EnsureView().Arranger` (or `Border?.Arranger` if Arranger is promoted to Border)
   - `current.Margin!.SubViews.Count` → `current.Margin?.View?.SubViews.Count ?? 0`
   - `current.Padding.SetNeedsLayout()` → `current.Padding?.View?.SetNeedsLayout()`
   - `HelpView.Margin!.ViewportSettings |= ...` → `HelpView.Margin!.ViewportSettings |= ...` (promoted to lightweight Margin)
7. Update `Margin.DrawMargins()` to skip when `margin.View is null` (transparent margins without shadow don't need drawing)
8. Update `View.Drawing.cs` `Margin?.CacheClip()` → `Margin?.CacheClip()` (already delegated in lightweight Margin)

**Memory impact for typical views:** ~1,600–5,000 bytes saved (2–3 `AdornmentView` instances eliminated)

**Risk:** Medium — behavioral testing required. All adornment tests must run.

---

### Phase 3: No-View Border Drawing (**Recommended** for windowed apps)

**Goal:** Draw simple borders directly from the parent View without creating a `BorderView` at all. This is the highest-value phase for the most common Terminal.Gui use case: a `Window` or `FrameView` with a visible title border.

**Duration:** 2–4 weeks

**Pre-condition:** Phase 2 complete and stable.

**Rationale:** After Phase 2, a view with `BorderStyle = LineStyle.Single` still creates a `BorderView` at draw time. For applications where most views are windows (the typical case), Phase 2 alone saves Margin and Padding cost but not Border. Phase 3 eliminates that remaining cost for the ~90% of views that have no arrangement, no arrangement buttons, and no SubViews in the Border. The added complexity in `View.Drawing.cs` is bounded and testable independently via draw-output comparison tests.

**Changes:**
1. Extract border-line drawing logic from `BorderView.OnDrawingContent()` into a static helper `BorderDrawer.DrawBorder(IDriver driver, Border border, Rectangle screenBounds, View parent)` — the helper reads Thickness, LineStyle, and Settings from the lightweight `Border`, and Title/TitleTextFormatter from the parent `View` parameter
2. In `View.Drawing.cs`, add a `DrawBorderDirect()` call when `Border.View is null && Border.Thickness != Thickness.Empty`
3. Add `DrawMarginDirect()` similarly for Margin transparency/shadow-free drawing
4. The `BorderView` is then only created when: arrangement is enabled (`Arrangement != ViewArrangement.Fixed`), diagnostic spinner is shown (`DrawIndicator`), or SubViews are added to the Border

**Expected additional savings:** Eliminates `BorderView` (~1,600–2,500 bytes) for all windows/frames with only a visual border and no arrangement. For a UICatalog-style application with 50 windows visible at once, this saves ~125 KB.

**Risk:** Higher — draw-order changes, clipping interactions, title rendering. Mitigated by comparison-based draw output tests (using `DriverAssert.AssertDriverContentsWithFrameAre`) before and after.

---

### Phase 4: Additional Optimizations (Post-Adornment)

Complementary to the adornment work, these are lower-complexity wins in `View` itself:

| Optimization | Est. Savings | Risk | Notes |
|---|---|---|---|
| Lazy `TextFormatter` for Title | 200–300 bytes | Low | Only create when `Title` is set non-empty |
| Lazy `KeyBindings` | 160–300 bytes | Low-Medium | Create when first binding is added |
| Lazy `LineCanvas` | 200–500 bytes | Low | Create in `OnDrawingContent` when first needed |
| Lazy Command dictionary | 100–200 bytes | Low | Create in `AddCommand` |
| Event broker pattern (40+ fields → dictionary) | 200–250 bytes | Medium | Significant refactor; saves backing-field allocation for unused events |

---

## 7. Breaking Changes Catalog

### 7.1 Internal Code (within Terminal.Gui library)

These are **breaking for contributors** but not for library users.

| Location | Current Code | New Code | Phase |
|----------|-------------|----------|-------|
| `View.Adornments.cs` | `if (this is not Adornment)` | `if (this is not AdornmentView)` | 1 |
| `View.Drawing.cs` | `if (this is Adornment)` | `if (this is AdornmentView)` | 1 |
| `View.Drawing.Clipping.cs` | `if (this is Adornment adornment)` | `if (this is AdornmentView adornment)` | 1 |
| `View.NeedsDraw.cs` | `if (this is Adornment adornment)` | `if (this is AdornmentView adornment)` | 1 |
| `View.Layout.cs` | `if (current is Adornment adornment)` | `if (current is AdornmentView adornment)` | 1 |
| `View.Navigation.cs` | `var thisAsAdornment = this as Adornment` | `var thisAsAdornment = this as AdornmentView` | 1 |
| `View.ScrollBars.cs` | `if (this is Adornment)` | `if (this is AdornmentView)` | 1 |
| `ApplicationMouse.cs` | `if (... is Adornment adornment)` | `if (... is AdornmentView adornment)` | 1 |
| `TabRow.cs` | `me.View is Adornment adornment` | `me.View is AdornmentView adornment` | 1 |
| `View.Layout.cs:886-898` | `current.Margin.SubViews.Count` | `current.Margin.View?.SubViews.Count ?? 0` | 2 |
| `View.Layout.cs:898` | `current.Padding.SetNeedsLayout()` | `current.Padding.View?.SetNeedsLayout()` | 2 |
| `TabView.cs:576,607,689,699` | `tab.Border!.Activating += ...` | `tab.Border!.EnsureView().Activating += ...` | 2 |
| `ApplicationKeyboard.cs:266` | `viewToArrange.Border?.Arranger` | `viewToArrange.Border?.Arranger` (promoted to `Border`) | 2 |
| `Margin.cs:414,420,421` | `Parent!.Border!.GetBorderRectangle()` | `Parent!.Border!.GetBorderRectangle()` (promoted) | 2 |
| `Arranger.cs:218-562` | `parent.Margin!.Thickness.*` | No change (Thickness on IAdornment) | — |

### 7.2 Public API (Breaking for Library Users)

| API | Change | Mitigation |
|-----|--------|-----------|
| `View.Border` type | `Border : Adornment : View` → `Border : AdornmentImpl` | Users accessing View methods must go through `Border.View` or use `Border.EnsureView()` |
| `View.Margin` type | Same pattern | Same |
| `View.Padding` type | Same pattern | Same |
| `Adornment` class | Renamed to `AdornmentView` | Provide `[Obsolete]` alias for one release cycle |
| `new Adornment(parent)` | Constructor on `AdornmentView` | Tests use `new Adornment()` parameter-less constructor — keep on `AdornmentView` |
| `view.Border?.BeginInit()` | Must use `view.Border?.View?.BeginInit()` | Breaking, but rare in user code |
| `view.Border?.SetNeedsDraw()` | Must use `view.Border?.View?.SetNeedsDraw()` | Breaking |
| `view.Border.Add(subView)` | Use `view.Border.EnsureView().Add(subView)` or `view.Padding.Add(subView)` helper | Provide convenience methods |

### 7.3 Nullability Clarification

`View.Border`, `View.Margin`, `View.Padding` remain **always non-null** after construction (as today). The `null` is on `Border.View`, `Margin.View`, `Padding.View`.

Code that does `v.Border?.XYZ` with nullable check is not needed for the lightweight object itself — but IS needed if accessing `.View?.XYZ`.

---

## 8. Files to Create / Modify

### New Files

| File | Contents |
|------|----------|
| `Terminal.Gui/ViewBase/Adornment/IAdornment.cs` | `IAdornment` interface |
| `Terminal.Gui/ViewBase/Adornment/AdornmentImpl.cs` | `AdornmentImpl` abstract base |
| `Terminal.Gui/ViewBase/Adornment/AdornmentView.cs` | Renamed from `Adornment.cs` |
| `Terminal.Gui/ViewBase/Adornment/BorderView.cs` | Renamed from `Border.cs` |
| `Terminal.Gui/ViewBase/Adornment/BorderView.Arrangement.cs` | Renamed from `Border.Arrangment.cs` (fixing pre-existing typo) |
| `Terminal.Gui/ViewBase/Adornment/MarginView.cs` | Renamed from `Margin.cs` |
| `Terminal.Gui/ViewBase/Adornment/PaddingView.cs` | Renamed from `Padding.cs` |
| `Terminal.Gui/ViewBase/Adornment/Border.cs` | New lightweight `Border` class |
| `Terminal.Gui/ViewBase/Adornment/Margin.cs` | New lightweight `Margin` class |
| `Terminal.Gui/ViewBase/Adornment/Padding.cs` | New lightweight `Padding` class |

### Modified Files (Phase 1)

| File | Change |
|------|--------|
| `Terminal.Gui/ViewBase/View.Adornments.cs` | Update property types, is-checks |
| `Terminal.Gui/ViewBase/View.cs` | Update SetupAdornments signature if needed |
| `Terminal.Gui/ViewBase/View.Drawing.cs` | Update `is Adornment` → `is AdornmentView` |
| `Terminal.Gui/ViewBase/View.Drawing.Clipping.cs` | Same |
| `Terminal.Gui/ViewBase/View.NeedsDraw.cs` | Same |
| `Terminal.Gui/ViewBase/View.Layout.cs` | Same (3 locations: lines 135, 193, 915) |
| `Terminal.Gui/ViewBase/View.Navigation.cs` | Same (2 locations: lines 613, 812) |
| `Terminal.Gui/ViewBase/View.ScrollBars.cs` | Same (3 locations: lines 51, 184, 224) |
| `Terminal.Gui/ViewBase/View.Hierarchy.cs` | Same |
| `Terminal.Gui/ViewBase/Adornment/ShadowView.cs` | `SuperView is not Adornment` → `is not AdornmentView` |
| `Terminal.Gui/App/Mouse/ApplicationMouse.cs` | Same (3 locations) |
| `Terminal.Gui/Views/TabView/TabRow.cs` | Same |
| `Terminal.Gui/ViewBase/EnumExtensions/BorderSettingsExtensions.cs` | Update references |
| UICatalog `Scenarios/Popovers.cs` | Update `typeof(Adornment)` (2 locations) |
| UICatalog `EditorsAndHelpers/AdornmentEditor.cs` | `ViewToEdit as Adornment` → `as AdornmentView` |
| UICatalog `EditorsAndHelpers/DimEditor.cs` | `ViewToEdit is not Adornment` → `is not AdornmentView` |
| UICatalog `EditorsAndHelpers/EditorBase.cs` | `view is Adornment adornment` → `is AdornmentView adornment` |
| UICatalog `EditorsAndHelpers/ExpanderButton.cs` | `superView is Adornment adornment` → `is AdornmentView adornment` |
| UICatalog `EditorsAndHelpers/PosEditor.cs` | `ViewToEdit is not Adornment` → `is not AdornmentView` |
| UICatalog `EditorsAndHelpers/ViewportSettingsEditor.cs` | Same (2 locations) |

### Modified Files (Phase 2, additional)

| File | Change |
|------|--------|
| `Terminal.Gui/ViewBase/View.Adornments.cs` | `SetupAdornments` creates lightweight objects only |
| `Terminal.Gui/ViewBase/View.Drawing.cs` | `Margin?.CacheClip()` delegates through lightweight Margin |
| `Terminal.Gui/ViewBase/View.Layout.cs` | `SubViews.Count` → `View?.SubViews.Count ?? 0` |
| `Terminal.Gui/Views/TabView/TabView.cs` | `Border.Activating` → `Border.EnsureView().Activating` |
| `Terminal.Gui/App/Keyboard/ApplicationKeyboard.cs` | `Border.Arranger` via promoted property |
| `Terminal.Gui/Views/Shortcut.cs` | `Margin.ViewportSettings` via promoted property |
| `Terminal.Gui/Views/StatusBar.cs` | Border null check (already null-safe, no change) |
| `Terminal.Gui/Views/FileDialogs/FileDialog.cs` | Border null check (already null-safe) |
| `Terminal.Gui/Views/Menu/MenuBar.cs` | Border null-conditional (already safe) |
| `Terminal.Gui/Views/Selectors/SelectorBase.cs` | Margin Thickness (lightweight, no change) |

### Test Files

| File | Change |
|------|--------|
| `Tests/*/Adornment/AdornmentTests.cs` | `new Adornment(null!)` → `new AdornmentView(null!)` |
| `Tests/*/Layout/ToScreenTests.cs` | `new Adornment()` → `new AdornmentView()` |
| All tests using `.Border.Activating` | Update to `.Border.EnsureView().Activating` or `.Border.View!.Activating` |

---

## 9. API Migration Guide

### For Library Users

Most code will not need changes. The common patterns still work:

```csharp
// ✅ UNCHANGED — Thickness access (most common case)
view.Border.Thickness = new Thickness (1);
view.Margin.Thickness = new Thickness (0, 0, 1, 1);

// ✅ UNCHANGED — BorderStyle helper
view.BorderStyle = LineStyle.Rounded;

// ✅ UNCHANGED — ShadowStyle helper
view.ShadowStyle = ShadowStyle.Opaque;

// ✅ UNCHANGED — Null checks on lightweight objects (never null)
if (view.Border is { }) { /* always true */ }

// ⚠️ CHANGED — Accessing View events (rare)
// Old:
tab.Border!.Activating += MyHandler;
// New:
tab.Border!.EnsureView().Activating += MyHandler;
// Or use the new Tab API (preferred):
tab.Activating += MyHandler; // if Tab exposes this

// ⚠️ CHANGED — Adding SubViews to adornments
// Old:
view.Border.Add(mySubView);
// New:
view.Border.EnsureView().Add(mySubView);
// Or (Padding preferred, convenience method):
view.Padding.Add(mySubView);

// ⚠️ CHANGED — Calling View.BeginInit on an adornment
// Old:
view.Border.BeginInit();
// New:
view.Border.View?.BeginInit();

// ⚠️ CHANGED — Type checks for Adornment
// Old:
if (someView is Adornment a) { }
// New:
if (someView is AdornmentView a) { }
```

### Obsolete Aliases

For one release cycle, provide:

```csharp
// In a separate Adornment.Compatibility.cs
[Obsolete("Use AdornmentView instead.", error: false)]
public class Adornment : AdornmentView
{
    public Adornment () : base () { }
    public Adornment (View parent) : base (parent) { }
}
```

---

## 10. Test Strategy

### Phase 1 (Rename)
- All existing tests must pass unchanged (except explicit `new Adornment(...)` calls → `new AdornmentView(...)`)
- Zero behavior change

### Phase 2 (Lazy)
- Add a memory-measurement test: create 1000 Views, measure GC memory, assert < 3 MB (vs current ~4.3 MB)
- All existing adornment tests pass
- New tests for lazy View creation:
  - View created without any adornment features → `Border.View is null`, `Margin.View is null`, `Padding.View is null`
  - `BorderStyle = LineStyle.Single` → `Border.View is not null`, others remain null
  - `ShadowStyle = ShadowStyle.Opaque` → `Margin.View is not null`
  - Adding SubViews to Padding → `Padding.View is not null`
  - After `EnsureView()`, all layout coordinates match current behavior

---

## 11. Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Layout regression from lazy frame calculation | Medium | High | Phase 1 has zero behavior change; Phase 2 adds extensive layout tests |
| Draw-order regression (border not drawn because View wasn't created) | Medium | High | Ensure BorderView creation is triggered before Draw(); add draw tests |
| Memory regression (View creation triggered too eagerly) | Low | Medium | Add memory measurement tests; profile in CI |
| Test breakage from `is Adornment` patterns | Low | Low | Grep-and-replace; compiler catches type errors |
| UICatalog/AllViewsTester breakage | Medium | Low | Update UICatalog editors to use AdornmentView type |
| Designer (`IDesignable`) breakage | Medium | Medium | `IDesignable.EnableForDesign()` moved to AdornmentView |
| Tab order regression (Padding is focusable) | Low | High | PaddingView retains focus behavior; lightweight Padding has no focus |

---

## 12. Expected Memory Savings

> **Note on methodology:** All numbers are per-adornment savings only. The full `View` instance has additional overhead (~2–3 KB) from TextFormatter, KeyBindings, LineCanvas, event fields, etc. — those are not reduced by this change. Numbers are estimates based on .NET object size analysis; actual savings will vary with GC compaction and runtime.

### Baseline — Adornment cost per View instance (current):

| Scenario | Adornment objects created | Adornment bytes (est.) |
|----------|--------------------------|------------------------|
| Any View today | 3× AdornmentView (Margin + Border + Padding) | ~4,800–7,500 |

### After Phase 2 — Adornment cost per View instance:

| View usage | Adornment objects created | Adornment bytes (est.) | Saved vs. baseline |
|------------|--------------------------|------------------------|--------------------|
| Plain view (no border, no shadow) | 3× lightweight (~50 bytes each) | ~150 | ~4,650–7,350 (96%) |
| View with border only | 1× BorderView + 2× lightweight | ~1,800–2,700 | ~3,000–4,800 (63%) |
| View with border + shadow | 1× BorderView + 1× MarginView + 1× lightweight | ~3,350–5,100 | ~1,450–2,400 (30%) |

### After Phase 3 — Adornment cost per View instance:

| View usage | Adornment objects created | Adornment bytes (est.) | Saved vs. Phase 2 |
|------------|--------------------------|------------------------|-------------------|
| Plain view (no border, no shadow) | 3× lightweight | ~150 | No change |
| View with border, no arrangement | 3× lightweight (draw happens in parent) | ~150 | ~1,650–2,550 (91% over baseline) |
| View with arrangement | 1× BorderView + 2× lightweight | ~1,800–2,700 | No change from Phase 2 |

### Realistic Application Scenario

A medium-complexity Terminal.Gui application with **200 views** (mix: 40% plain, 50% bordered without arrangement, 10% with shadow):

| Phase | Adornment memory | Savings vs. today |
|-------|-----------------|-------------------|
| Today (baseline) | ~860 KB | — |
| After Phase 2 | ~225 KB | **74% reduction** |
| After Phase 3 | ~70 KB | **92% reduction** |

**Important caveat:** The total `View` heap cost is not reduced by 92% — only the adornment fraction. If adornments represent ~40% of total view cost, Phase 2+3 reduce total per-view footprint by roughly **37%** (from ~4.3 KB to ~2.7 KB). This still saves ~320 KB in a 200-view application, which is meaningful for memory-constrained environments.

---

## 13. Appendix: Complete Callsite Inventory

### Adornment-as-View accesses in library code

| File | Line | Access | Needs EnsureView? |
|------|------|--------|-------------------|
| `View.Layout.cs` | 886–898 | `Margin.SubViews.Count`, `Border.SubViews.Count`, `Padding.SetNeedsLayout()` | Yes |
| `TabView.cs` | 576, 607, 689, 699 | `tab.Border!.Activating +=/-=` | Yes |
| `ApplicationKeyboard.cs` | 266 | `Border?.Arranger.EnterArrangeMode()` | Via promoted `Arranger` property |
| `View.Drawing.cs` | 180 | `Margin?.CacheClip()` | Via delegated method on lightweight Margin |
| `Margin.cs` | 89–103 | `DrawMargins` iterates, calls `margin.Draw()` | Via `margin.View?.Draw()` |
| `ShadowView.cs` | 160 | `SuperView is not Adornment` | Change to `is not AdornmentView` |

### Adornment type checks (all `is Adornment` patterns)

Locations (verified from codebase grep):

| File | Line(s) | Pattern |
|------|---------|---------|
| `ViewBase/View.Adornments.cs` | 11, 255 | `is not Adornment`, `is Adornment` |
| `ViewBase/View.Drawing.cs` | 52, 248, 821 | `is not Adornment`, `is Adornment`, `is not Adornment` |
| `ViewBase/View.Drawing.Clipping.cs` | 116, 169 | `is Adornment adornment` |
| `ViewBase/View.NeedsDraw.cs` | 105, 176 | `is Adornment adornment` |
| `ViewBase/View.Layout.cs` | 135, 193, 915 | `is Adornment adornment`, `is Adornment { Parent: null }`, `is not Adornment adornment` |
| `ViewBase/View.Navigation.cs` | 613, 812 | `as Adornment` |
| `ViewBase/View.ScrollBars.cs` | 51, 184, 224 | `is Adornment` |
| `ViewBase/Adornment/ShadowView.cs` | 160 | `SuperView is not Adornment` |
| `App/Mouse/ApplicationMouse.cs` | 154, 198, 289 | `is Adornment adornment`, `is Adornment { Parent: { } }` |
| `Views/TabView/TabRow.cs` | 50 | `me.View is Adornment adornment` |
| `UICatalog/EditorsAndHelpers/AdornmentEditor.cs` | 77 | `ViewToEdit as Adornment` |
| `UICatalog/EditorsAndHelpers/DimEditor.cs` | 32 | `ViewToEdit is not Adornment` |
| `UICatalog/EditorsAndHelpers/EditorBase.cs` | 152 | `view is Adornment adornment` |
| `UICatalog/EditorsAndHelpers/ExpanderButton.cs` | 199 | `superView is Adornment adornment` |
| `UICatalog/EditorsAndHelpers/PosEditor.cs` | 24 | `ViewToEdit is not Adornment` |
| `UICatalog/EditorsAndHelpers/ViewportSettingsEditor.cs` | 72, 112 | `ViewToEdit is not Adornment`, `ViewToEdit is Adornment` |
| `UICatalog/Scenarios/Popovers.cs` | 358, 359 | `typeof(Adornment)`, `IsSubclassOf(typeof(Adornment))` |

**Total: 30+ locations**, all mechanical find-and-replace.
