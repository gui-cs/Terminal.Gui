# Move BorderView TabTitleView Configuration Out of Draw

## Problem
`BorderView.DrawTabBorder()` performs persistent configuration (ViewportSettings, TabTitleView creation, Frame/Size/Thickness, Text, BorderStyle, TextDirection) on every draw pass. These should be set once when settings/thickness/layout changes.

## Approach
Split TabTitleView configuration into setup (property-change-driven) and layout (geometry) phases. Keep only LineCanvas line-drawing and gradient in the draw path.

## Todos

### 1. `border-settings-notify` — Add change notification to Border properties
- `Border.Settings` setter: fire a new `SettingsChanged` event
- `Border.TabSide`, `TabOffset`, `TabLength` setters: add change-guard + `SetNeedsLayout` + event

### 2. `configure-tab-mode` — Create `ConfigureForTabMode()` in BorderView
Called when `Border.Settings` or thickness changes:
- If Tab enabled: set `ViewportSettings |= Transparent | TransparentMouse`, call `EnsureTabTitleView()`, set static props (OwnerView, CanFocus, TabStop, SuperViewRendersLineCanvas, Border.Settings, HotKeySpecifier, BorderStyle, TextDirection)
- If Tab disabled: clear transparent flags, hide TabTitleView

### 3. `tab-layout` — Create `UpdateTabTitleViewLayout()` in BorderView
Called during `SubViewLayout`:
- Compute header rect via `GetTabBorderBounds` + `ComputeHeaderRect`
- Set TabTitleView Frame/Width/Height
- Set `Border.Thickness` based on focus + depth
- Sync Text from parent
- Set Visible based on clipping

### 4. `hook-events` — Wire up events
- Subscribe to `Border.SettingsChanged` → `ConfigureForTabMode()`
- Subscribe to `SubViewLayout` → `UpdateTabTitleViewLayout()`
- In `OnThicknessChanged` → call `ConfigureForTabMode()`
- In `BeginInit` → call `ConfigureForTabMode()`
- Hook parent `HasFocusChanged` → update tab thickness

### 5. `simplify-draw` — Strip setup code from DrawTabBorder
Remove from `DrawTabBorder`:
- `ViewportSettings` assignment (line 660)
- `EnsureTabTitleView()` call + all label property assignments (lines 391-414)
- Keep: LineCanvas operations, gradient, LastTitleRect, visibility toggle

### 6. `test-validate` — Build + run tests
