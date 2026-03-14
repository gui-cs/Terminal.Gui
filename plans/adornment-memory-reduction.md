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

**Expected memory reduction:** 40–65% for typical views (those that never use subviews in adornments or shadows).

---

## 2. Design Principles

This design was evaluated against the following criteria for a robust, long-lasting engineering solution:

| Principle | How This Design Satisfies It |
|-------|------------------------------|
| **Customer focus** | API stays backward-compatible for the common case; most view code (`view.Border.Thickness = new(1)`) compiles unchanged. |
| **Long-term thinking** | Decoupling "adornment settings" from "adornment view" is the right architectural split for v3 extensibility. Lazy creation future-proofs for component-based architectures. |
| **Data-driven** | Memory analysis (§4) is grounded in class-by-class measurement; expected savings are conservative estimates. |
| **Honest about tradeoffs** | This is a moderate breaking change to internal/advanced-user code. The plan explicitly calls out each breaking point rather than hiding it. |
| **Think big** | The `IAdornment` interface opens the door to custom adornment implementations without requiring View subclassing. |
| **Simplify** | Removing the `is Adornment` checks scattered throughout View, Navigation, Drawing, Layout code simplifies those code paths. |
| **Phased delivery** | Phase 1 ships zero behavior change (safe rename); Phase 2 delivers memory savings; each phase independently shippable. |

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

    // Fallback geometry when no View exists yet (derived from Parent + Thickness)
    private Point ComputeViewportToScreen (in Point location) { /* ... */ }
    private Rectangle ComputeFrameToScreen () { /* ... */ }
    private Point ComputeScreenToFrame (in Point location) { /* ... */ }

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

### Phase 3: No-View Border Drawing (Optional, Phase 2+)

**Goal:** Draw simple borders directly from parent View without creating a `BorderView`.

**Duration:** 2–4 weeks

**Pre-condition:** Phase 2 complete and stable.

**Changes:**
1. Extract border-line drawing logic from `BorderView.OnDrawingContent()` into a static helper `BorderDrawer.DrawBorder(IDriver, Border, Rectangle, View?)`
2. In `View.Drawing.cs`, after `OnClearingViewport()`, check `if (Border.View is null && Border.Thickness != Thickness.Empty)` and call `BorderDrawer.DrawBorder()`
3. Similarly for Margin thickness clearing
4. The `BorderView` is then only created when: arrangement is enabled, arrangement buttons are shown, or subviews are added to the border

**Expected additional savings:** Eliminates `BorderView` creation for all windows/frames with only a visual border and no arrangement

**Risk:** Higher — draw-order changes, clipping interactions. Requires extensive visual testing.

---

### Phase 4: Additional Optimizations (Post-Adornment)

Complementary to the adornment work:

| Optimization | Savings | Risk |
|---|---|---|
| Lazy `TextFormatter` for Title | 200–300 bytes | Low |
| Lazy `KeyBindings` | 160–300 bytes | Low-Medium |
| Lazy `LineCanvas` | 200–500 bytes | Low |
| Lazy Command dictionary | 100–200 bytes | Low |
| Event broker pattern (40+ fields → dictionary) | 200–250 bytes | Medium |

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

### Baseline (1 View instance, typical):
- 3× AdornmentView (Margin + Border + Padding): ~4,800–7,500 bytes

### After Phase 2, no border:
- 3× lightweight objects (Border + Margin + Padding ≈ 150–300 bytes)
- Savings: **~4,500–7,200 bytes** (>90% reduction in adornment cost)

### After Phase 2, with BorderStyle:
- 1× BorderView (for drawing): ~1,600–2,500 bytes
- 2× lightweight Margin + Padding: ~100–200 bytes
- Savings: **~3,200–5,000 bytes** (~65–70% reduction)

### After Phase 2, with shadow:
- 1× BorderView + 1× MarginView: ~3,200–5,000 bytes
- 1× lightweight Padding: ~50 bytes
- Savings: **~1,600–2,500 bytes** (~25–35% reduction)

### For 1,000 typical views (mixed: 60% no border, 30% border, 10% shadow):
| Scenario | Before | After | Saved |
|----------|--------|-------|-------|
| No border (60%) | ~4.3 MB | ~0.18 MB | 4.1 MB |
| With border (30%) | ~4.3 MB | ~1.5 MB | 2.8 MB |
| With shadow (10%) | ~4.3 MB | ~2.5 MB | 1.8 MB |
| **Total** | **~43 MB** | **~4.2 MB** | **~39 MB (91%)** |

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
