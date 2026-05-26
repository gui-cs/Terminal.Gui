# Spec: Remove Legacy ConfigurationManager (Phase 4–6 of CM → MEC)

> **Status:** Draft — follow-up to PR #5411 (`copilot/replace-cm-with-mec`).
> **Tracking Issue:** [#4943](https://github.com/gui-cs/Terminal.Gui/issues/4943)
> **PR:** [#5416 — `tig/remove-cm-followup`](https://github.com/gui-cs/Terminal.Gui/pull/5416) (stacked on `copilot/replace-cm-with-mec`)
> **Predecessor Spec:** [`specs/replace-cm-with-mec.md`](./replace-cm-with-mec.md)

---

## 1. Purpose

PR #5411 completed the **functional** CM → MEC migration:

- Per‑component Settings POCOs with static `Defaults` facades.
- `TuiConfigurationBuilder` + `TuiConfigurationExtensions` providing the MEC-based source chain.
- `IThemeManager` / `ISchemeManager` services (currently thin wrappers over legacy static managers).
- All `[ConfigurationProperty(...)]` attributes removed from production view/option properties (only the attribute *class* still exists).
- `ConfigurationManager`, `SourcesManager`, `ConfigProperty`, `Scope<T>`, `DeepCloner`, `ScopeJsonConverter` marked `[Obsolete]` and only referenced internally during the transition.

This PR finishes the migration by **deleting** every legacy CM type, the embedded flat-key `Resources/config.json` format, the residual CM-backed wiring inside the MEC managers, and all CM-specific tests. The result is the AOT size reduction and parallel-test unlock that motivated #4943 in the first place.

This corresponds to **Phases 4, 5, and 6** of the predecessor spec, executed as a single stacked PR.

---

## 2. Goals

1. **Delete every legacy CM type and attribute** (no `[Obsolete]` shim period — they were already shimmed in #5411).
2. **Replace CM-backed `ThemeManager` / `SchemeManager` internals** with POCO + `IOptionsMonitor<ThemeSettings>` storage so `MecThemeManager` / `MecSchemeManager` are no longer thin wrappers over the static legacy types.
3. **Eliminate CM-related anti-trim patterns**: `ConfigPropertyHostTypes.GetTypes()`, `[DynamicDependency]` on 29 host types, `Assembly.GetTypes()` reflection scan, `DeepCloner` reflection, `MakeGenericType` usage.
4. **Drop the `ConfigurationManager.Applied` event subscription pattern** used by `Menu`, `MenuBar`, `StatusBar`, `LineCanvas` and replace with `IOptionsMonitor<T>.OnChange` (or direct theme-manager events).
5. **Migrate the embedded `Terminal.Gui/Resources/config.json`** from the legacy flat-key `Themes: [ { Name: { "Class.Prop": ... } } ]` layout to the MEC-native nested `Themes:{ Name:{ Class:{ Prop:... } } }` layout (per D-02 Option 3 ⇒ now Option 2 since flat keys are no longer parsed).
6. **Delete all CM-only tests** (`ConfigurationMangerTests`, `ConfigPropertyAssemblyScanTests`, `ConfigPropertyTests`, `ConfigPropertyHostTypesTests`, `DeepClonerTests`, `ScopeJsonConverterTests`, `ScopeTests`, `SettingsScopeTests`, `ThemeScopeTests`, `ConfigurationPropertyAttributeTests`, legacy `SourcesManagerTests`). Keep MEC equivalents.
7. **Measure and record the NativeAOT binary-size delta** in PR #5416 so the stated motivation in #4943 is verifiable.
8. **Update `docfx/docs/config.md`** so the documented contract matches the shipped contract (Constitution: *Documentation Is the Spec*).

### Non‑goals

- Generic Host / `IHostBuilder` adoption (deferred, Q‑02).
- View constructor injection of `IOptionsMonitor<T>` (deferred — static `Defaults` facade remains per D‑01).
- Multi-instance `IApplication` scoping of themes (D‑03).
- Lazy driver registration / assembly splitting / TextMate conditional compilation (out of scope per #10 of predecessor spec).

---

## 3. Constitution Alignment

| Tenet | How this PR aligns |
|-------|--------------------|
| ✅ **Testability First** | Removing CM static state moves every theme/scheme test into `UnitTestsParallelizable`. |
| ✅ **Performance Is a Feature** | Removes the `Assembly.GetTypes()` scan + DeepCloner reflection at module init. Hot path (`Rune` read on a glyph, `ShadowStyles` read on Button) becomes a direct field read on a POCO. |
| ✅ **Users Have Final Control** | All configurable surfaces stay configurable. Only the *internal mechanism* changes; the documented public configuration model is MEC. |
| ✅ **Separation of Concerns** | Views no longer reach into a process-wide static (`ConfigurationManager.Settings["Foo"]`). They observe `IOptionsMonitor<T>` (via the static `Defaults` facade for now). |
| ✅ **Respect What Came Before** | The 9‑level precedence semantics are preserved through the MEC provider chain assembled in `TuiConfigurationExtensions`. The user-facing JSON keeps the same *shape* aside from the flat→nested key migration. |
| 🔴 **Documentation Is the Spec** | `docfx/docs/config.md` is rewritten in this PR; no out-of-date references to `ConfigurationManager.Enable`, `RuntimeConfig`, `Applied` event, etc. |

---

## 4. Current Residual Surface (post‑#5411)

The exhaustive inventory of what still has to go. Discovered by `grep`ping the worktree against `copilot/replace-cm-with-mec` HEAD.

### 4.1 Types to **delete** entirely (`Terminal.Gui/Configuration/`)

| File | Notes |
|------|-------|
| `ConfigurationManager.cs` (~33 KB) | Static facade; obsolete |
| `ConfigurationManagerEventArgs.cs` | `Applied`/`Updated` event args |
| `ConfigurationManagerNotEnabledException.cs` | No "enabled" concept in MEC |
| `ConfigurationPropertyAttribute.cs` | Attribute already unused on production properties |
| `ConfigProperty.cs` (~28 KB) | Reflection-discovered config property descriptor |
| `ConfigPropertyHostTypes.cs` | `[DynamicDependency]` rooting of 29 host types |
| `ConfigLocations.cs` | Replaced by MEC provider ordering in `TuiConfigurationExtensions` |
| `DeepCloner.cs` (~19 KB) | Reflection-based deep clone; not needed once POCOs replace `Scope<T>` |
| `Scope.cs`, `SettingsScope.cs`, `AppSettingsScope.cs`, `ThemeScope.cs` | Scope abstractions superseded by typed `IOptions<T>` |
| `ScopeJsonConverter.cs` | Flat-key scope deserializer |
| `DictionaryJsonConverter.cs`, `ConcurrentDictionaryJsonConverter.cs` | Only needed by the flat-key scope format |
| `KeyArrayJsonConverter.cs`, `KeyCodeJsonConverter.cs`, `MouseFlagsArrayJsonConverter.cs` | Audit — only delete if unused after migration; otherwise keep and register on the MEC-side `JsonSerializerOptions` |

### 4.2 Types to **keep** (already MEC-shaped) and harden

| File | Action |
|------|--------|
| `SourceGenerationContext.cs` | Keep. Re-audit `[JsonSerializable]` entries — remove any that referenced `SettingsScope` / `ThemeScope` / `AppSettingsScope`. Add `ThemeSettings` and the per-component settings POCOs. |
| `AttributeJsonConverter.cs`, `SchemeJsonConverter.cs`, `RuneJsonConverter.cs`, `KeyJsonConverter.cs`, `ColorJsonConverter.cs`, `TraceCategoryJsonConverter.cs` | Keep. Replace internal references to `ConfigurationManager.SerializerContext` with `SourceGenerationContext.Default` (the source‑generated context). Remove the `#pragma warning disable CS0618` blocks. |
| `Settings/*Settings.cs` (all 30+) | Keep. These are the POCOs that became the source of truth in #5411. |
| `Settings/TuiConfigurationBuilder.cs` | Keep. Becomes the **only** configuration entry point. |
| `Settings/TuiConfigurationExtensions.cs` | Keep. |
| `Settings/IThemeManager.cs`, `Settings/IThemeManager.cs`, `Settings/MecThemeManager.cs`, `Settings/MecSchemeManager.cs` | Keep but rewrite internals (see §5.2). |

### 4.3 Types to **rename** (drop the `Mec` prefix once the legacy peers are gone)

| Old (post‑#5411) | New |
|------------------|-----|
| `MecSchemeManager` | `SchemeManager` (replaces the deleted legacy static class) |
| `MecThemeManager` | `ThemeManager` (replaces the deleted legacy static class) |
| `Tests/UnitTestsParallelizable/Configuration/MecSettingsTests.cs` | `SettingsTests.cs` |
| `Tests/UnitTestsParallelizable/Configuration/MecThemeTests.cs` | `ThemeTests.cs` |
| `Tests/UnitTestsParallelizable/Configuration/MecAppSettingsTests.cs` | `AppSettingsTests.cs` |

The legacy `SchemeManager` / `ThemeManager` are deleted in §4.1; the rename is a single namespace move and does not collide.

### 4.4 Callers to **rewire** (production source outside `Configuration/`)

| File | Current dependency | Replacement |
|------|--------------------|-------------|
| `ModuleInitializers.cs` | Calls `ConfigurationManager.Initialize ()` then `mecBuilder.ApplyToStaticFacades ()` | Single call to `TuiConfigurationBuilder.ApplyToStaticFacades ()`. Delete the dual-init suppression. |
| `App/ApplicationImpl.Lifecycle.cs` | `ConfigurationManager.PrintJsonErrors ()` | Delete the call. MEC throws on parse error; STJ errors go through `Logging`. |
| `App/Application.cs` | Several `ConfigurationManager.*` references in xmldoc/method bodies | Replace with `TuiConfigurationBuilder` references or delete. |
| `App/Tracing/Trace.cs` | One CM reference | Replace with `TraceSettings.Defaults.EnabledCategories`. |
| `Views/Menu/Menu.cs`, `Views/Menu/MenuBar.cs`, `Views/StatusBar.cs`, `Drawing/LineCanvas/LineCanvas.cs` | Subscribe/unsubscribe to `ConfigurationManager.Applied` to re-read theme-driven defaults | Subscribe to `IOptionsMonitor<ThemeSettings>.OnChange` (exposed via a small `ThemeChanges.Observed` helper, or via `IThemeManager.ThemeChanged`). Drop the `#pragma warning disable CS0618` suppressions. |
| `Drawing/Scheme.cs` | xmldoc references CM in a comment | Update wording. |
| `Drawing/Glyphs.cs` | (None — already POCO-backed via `GlyphSettings.Defaults`.) | No change. Confirm via grep after removal. |
| `Input/CommandBindingsBase.cs` | Comment refers to `ConfigurationManager.Apply` / `DeepMemberWiseCopy` | Update comment; the dictionary‑copy workaround stays because the underlying race is now in `IOptionsMonitor` callbacks. |
| `Views/Menu/PopoverMenu.cs`, `Views/Selectors/SelectorBase.cs`, `Views/Window.cs`, `Views/Dialog.cs`, `Views/FrameView.cs`, `Views/MessageBox.cs`, `Views/HexView.cs`, `Views/CharMap/CharMap.cs`, `Views/CheckBox.cs`, `Views/Button.cs`, `Views/FileDialogs/FileDialog.cs`, `Views/FileDialogs/FileDialogStyle.cs`, `Views/LinearRange/LinearRangeDefaults.cs`, `Views/LinearRange/LinearRangeViewBase.cs`, `Views/TextInput/TextField/TextField.cs`, `Views/TextInput/TextView/TextView.cs`, `Views/TreeView/TreeViewT.cs`, `ViewBase/View.Keyboard.cs`, `ViewBase/Mouse/View.Mouse.cs`, `ViewBase/Adornment/BorderView.Arrangement.cs`, `Text/NerdFonts.cs`, `FileServices/FileSystemIconProvider.cs`, `Drawing/Color/Color.cs`, `Input/Keyboard/Key.cs`, `Drivers/Driver.cs`, `App/Legacy/Application.Mouse.cs` | Each contains one or two stale doc-comment references to `ConfigurationManager`, `SettingsScope`, or `ThemeScope` (no live code dependency confirmed by grep `[ConfigurationProperty(` returning only the attribute file). | Sweep comments only. |

### 4.5 Resource files

| File | Action |
|------|--------|
| `Terminal.Gui/Resources/config.json` (52 KB, ~1,500 lines) | **Rewrite** in nested MEC format. Keys like `"Button.DefaultShadow"` become `Button: { DefaultShadow: ... }`. The `"Themes": [ { "Default": { ... } } ]` array-of-single-key-objects becomes `Themes: { Default: { ... }, Dark: { ... } }`. Schemes stay as a nested map. The `$schema` URL stays; the hosted schema must be updated in tandem (Q‑03 — file a tracking issue). |
| `Examples/Config/example_config.json` | Migrate alongside library `config.json`. |
| `Tests/.../*config.json` (if any) | Audit and migrate. |

### 4.6 Tests to **delete** (`Tests/`)

| File | Reason |
|------|--------|
| `UnitTests.NonParallelizable/Configuration/ConfigurationMangerTests.cs` | Tests deleted facade. |
| `UnitTests.NonParallelizable/Configuration/ConfigPropertyAssemblyScanTests.cs` | Tests deleted reflection scan. |
| `UnitTests.NonParallelizable/Configuration/SourcesManagerLoadNullJsonTests.cs` | Replaced by `SourcesManagerTests` in `UnitTestsParallelizable` (which already test the MEC builder). |
| `UnitTests.NonParallelizable/Configuration/GlyphTests.cs` | Move to parallelizable if it doesn't depend on static state; otherwise replace with MEC equivalent. |
| `UnitTestsParallelizable/Configuration/ConfigPropertyTests.cs` | Tests deleted type. |
| `UnitTestsParallelizable/Configuration/ConfigPropertyHostTypesTests.cs` | Tests deleted type. |
| `UnitTestsParallelizable/Configuration/ConfigurationPropertyAttributeTests.cs` | Tests deleted attribute. |
| `UnitTestsParallelizable/Configuration/DeepClonerTests.cs` | Tests deleted utility. |
| `UnitTestsParallelizable/Configuration/ScopeJsonConverterTests.cs` | Tests deleted converter. |
| `UnitTestsParallelizable/Configuration/ScopeTests.cs` | Tests deleted base class. |
| `UnitTestsParallelizable/Configuration/SettingsScopeTests.cs` | Tests deleted scope. |
| `UnitTestsParallelizable/Configuration/ThemeScopeTests.cs` | Tests deleted scope. |
| `Benchmarks/Configuration/ConfigurationManagerLoadBenchmark.cs` | Replace with `TuiConfigurationBuilderBuildBenchmark`. |
| `Benchmarks/Configuration/ThemeSwitchBenchmark.cs` | Rewrite against `IThemeManager.SwitchTheme`. |

### 4.7 Tests to **keep** (and audit)

- `UnitTestsParallelizable/Configuration/MecAppSettingsTests.cs`
- `UnitTestsParallelizable/Configuration/MecSettingsTests.cs`
- `UnitTestsParallelizable/Configuration/MecThemeTests.cs`
- `UnitTestsParallelizable/Configuration/SourcesManagerTests.cs` (MEC version — confirm not the legacy one)
- `UnitTestsParallelizable/Configuration/KeyJsonConverterTests.cs`
- `UnitTestsParallelizable/Configuration/RuneJsonConverterTests.cs`
- `UnitTestsParallelizable/Configuration/SchemeJsonConverterTests.cs`
- All `*DefaultKeyBindingsTests.cs` (these read `View.DefaultKeyBindings`; after CM removal they read it via the bound POCO)

### 4.8 Examples to **rewrite**

| File | Action |
|------|--------|
| `Examples/UICatalog/Runner.cs` | Replace `ConfigurationManager.RuntimeConfig = "..."` with `TuiConfigurationBuilder.Runtime(...)` |
| `Examples/UICatalog/UICatalog.cs` and `UICatalogRunnable.cs` | Replace `ConfigurationManager.Enable/Apply/Applied` with the new builder + `IOptionsMonitor<ThemeSettings>.OnChange` |
| `Examples/UICatalog/Scenarios/ConfigurationEditor.cs` | Heavy CM user. Rewrite to enumerate the `Settings/*Settings.cs` POCOs and serialize via `SourceGenerationContext`. |
| `Examples/UICatalog/Scenarios/Themes.cs`, `ThemeFallback.cs` | Use `IThemeManager.ThemeChanged` and `IThemeManager.SwitchTheme`. |
| `Examples/NativeAot/Program.cs`, `Examples/SelfContained/Program.cs`, `Examples/ReactiveExample/Program.cs`, `Examples/CommunityToolkitExample/Program.cs`, `Examples/ScenarioRunner/Program.cs`, `Examples/ShortcutTest/ShortcutTest.cs`, `Examples/PromptExample/Program.cs`, `Examples/Example/Example.cs` | Drop `ConfigurationManager.Enable (...)`; replace with `new TuiConfigurationBuilder ().ApplyToStaticFacades ()` (or remove entirely if defaults are sufficient). |
| `Examples/Config/README.md`, `Examples/NativeAot/README.md`, `Examples/SelfContained/README.md`, `Examples/ReactiveExample/README.md`, `Examples/CommunityToolkitExample/README.md` | Update documentation to reference MEC. |
| All `Examples/UICatalog/Scenarios/*.cs` files listed in §4 of the discovery output | Most contain only stale `using Terminal.Gui.Configuration;` plus an occasional `ConfigurationManager.Applied` subscription. Sweep with a single mechanical pass. |

---

## 5. Detailed Design

### 5.1 New `IOptionsMonitor<ThemeSettings>` flow

```text
TuiConfigurationBuilder.Build()
  └─ IConfigurationBuilder
       ├─ AddTuiLibraryDefaults()       (embedded Terminal.Gui.Resources.config.json)
       ├─ AddTuiAppDefaults(appName)    (entry assembly config.json)
       ├─ AddTuiUserFiles(appName)      (~/.tui/*.json, ./.tui/*.json)
       ├─ AddTuiEnvironmentVariable()   (TUI_CONFIG)
       └─ AddTuiRuntimeConfig(json)     (in-memory string)
  └─ IConfiguration root
  └─ services.Configure<ThemeSettings>(root.GetSection("Themes"))
  └─ services.Configure<ApplicationSettings>(root.GetSection("Application"))
  └─ services.Configure<ButtonSettings>(root.GetSection("Button"))
  └─ ... one Configure<T> per POCO

TuiConfigurationBuilder.ApplyToStaticFacades()
  └─ ButtonSettings.Defaults  = options.Get<ButtonSettings>()
  └─ DialogSettings.Defaults  = options.Get<DialogSettings>()
  └─ ... per POCO
  └─ Hook IOptionsMonitor<T>.OnChange(newValue => XxxSettings.Defaults = newValue)
```

`ButtonSettings.Defaults` (and peers) remain the **static facade** used by all views per D‑01. The only change versus #5411 is that the facade is now updated *only* by the MEC monitor — never by `ConfigurationManager.Apply`.

### 5.2 ThemeManager / SchemeManager: from wrapper to owner

`MecThemeManager` and `MecSchemeManager` currently delegate to the legacy static `ThemeManager` / `SchemeManager`. Once the legacy types are deleted, they become the owners:

```csharp
public sealed class ThemeManager : IThemeManager
{
    private readonly IOptionsMonitor<ThemeSettings> _monitor;
    private readonly object _switchLock = new ();

    public ThemeManager (IOptionsMonitor<ThemeSettings> monitor)
    {
        _monitor = monitor;
        _monitor.OnChange (HandleChanged);
    }

    public string ActiveTheme
    {
        get => _monitor.CurrentValue.ActiveTheme;
        set => SwitchTheme (value);
    }

    public IReadOnlyList<string> ThemeNames => _monitor.CurrentValue.Themes.Keys.ToImmutableList ();

    public event EventHandler<string>? ThemeChanged;

    public void SwitchTheme (string name) { /* update + raise */ }

    private void HandleChanged (ThemeSettings settings, string? name) => ThemeChanged?.Invoke (this, settings.ActiveTheme);
}
```

`SchemeManager` becomes an instance class fronting `ThemeSettings.Schemes`. The static `SchemeManager.GetScheme(string)` API used in many views is kept as a **static convenience facade** that forwards to the registered service (via a static `Application.Services` accessor) or to a process-wide fallback when no app has been created — same shape as `XxxSettings.Defaults`.

### 5.3 Event replacement table

| Old subscription | New subscription | File(s) affected |
|------------------|------------------|------------------|
| `ConfigurationManager.Applied += handler` (to re-read theme glyphs/border styles) | `ThemeChanges.Observed += handler` (small static event raised once after MEC apply finishes), OR `IThemeManager.ThemeChanged` | `Menu.cs`, `MenuBar.cs`, `StatusBar.cs`, `LineCanvas.cs`, `UICatalogRunnable.cs` |
| `ConfigurationManager.Updated += handler` | Delete — there is no MEC "loaded but not applied" state. |
| `ThemeManager.ThemeChanged` (legacy) | `IThemeManager.ThemeChanged` |

### 5.4 JSON config: flat → nested key migration

**Library `config.json`** is rewritten. Example snippet:

Before (flat):
```json
{
  "Themes": [
    {
      "Default": {
        "Button.DefaultShadow": "Opaque",
        "Glyphs.CheckStateChecked": "☑",
        "Schemes": [
          { "Base": { "Normal": { "Foreground": "White", "Background": "Black" } } }
        ]
      }
    }
  ]
}
```

After (nested, MEC-native):
```json
{
  "Themes": {
    "Default": {
      "Button": { "DefaultShadow": "Opaque" },
      "Glyphs": { "CheckStateChecked": "☑" },
      "Schemes": {
        "Base": { "Normal": { "Foreground": "White", "Background": "Black" } }
      }
    }
  }
}
```

**Breaking change for users with hand-authored `~/.tui/config.json` files.** A small migration helper (`TuiConfigMigrator.MigrateFlatToNested(string json)`) is shipped in `Terminal.Gui` for one release; the UICatalog `ConfigurationEditor` scenario gains a "Migrate" button. After one major version the helper is removed.

(This supersedes predecessor spec D‑02 Option 3. Continuing to accept flat keys requires keeping `ScopeJsonConverter`, which is the largest AOT-hostile blob in the Configuration namespace. The cost/benefit no longer favors dual format support after #5411 makes the nested format viable.)

### 5.5 Source generation context cleanup

`SourceGenerationContext` currently lists `SettingsScope`, `ThemeScope`, `AppSettingsScope`, and various dictionary closures. Replace with the per-component POCOs:

```csharp
[JsonSerializable (typeof (ThemeSettings))]
[JsonSerializable (typeof (ApplicationSettings))]
[JsonSerializable (typeof (ButtonSettings))]
[JsonSerializable (typeof (DialogSettings))]
[JsonSerializable (typeof (GlyphSettings))]
// ... one entry per Settings/*Settings.cs POCO
[JsonSerializable (typeof (Scheme))]
[JsonSerializable (typeof (Attribute))]
[JsonSerializable (typeof (Color))]
[JsonSerializable (typeof (Dictionary<string, Scheme>))]
[JsonSerializable (typeof (Dictionary<string, ThemeDefinition>))]
[JsonSourceGenerationOptions (Converters = new[] {
    typeof (RuneJsonConverter), typeof (KeyJsonConverter),
    typeof (ColorJsonConverter), typeof (AttributeJsonConverter),
    typeof (SchemeJsonConverter), typeof (TraceCategoryJsonConverter)
})]
internal partial class SourceGenerationContext : JsonSerializerContext { }
```

This is what unlocks the AOT-friendly path: every type STJ touches is statically known, no `MakeGenericType`, no `Assembly.GetTypes()`.

### 5.6 ModuleInitializer simplification

```csharp
[ModuleInitializer]
internal static void InitializeTuiConfiguration ()
{
    TuiConfigurationBuilder builder = new ();
    builder.ApplyToStaticFacades ();
}
```

Single call. No `ConfigurationManager.Initialize ()`, no `#pragma warning disable CS0618`.

---

## 6. Implementation Phases (within this PR)

Each phase is one or more commits. Tests must build and pass at every commit.

### Phase A — Make `MecThemeManager` / `MecSchemeManager` self-sufficient
- Move theme/scheme storage from the legacy static `ThemeManager` / `SchemeManager` into `ThemeSettings` (already exists; populate it from `IOptionsMonitor<ThemeSettings>`).
- Update `MecThemeManager` / `MecSchemeManager` to read/write the POCO directly.
- Add tests in `MecThemeTests` for switch / add / remove scheme paths that previously delegated to the legacy code.

### Phase B — Replace `ConfigurationManager.Applied` subscribers
- Introduce `Terminal.Gui.Configuration.ThemeChanges` (or expose `IThemeManager.ThemeChanged` via a static convenience accessor).
- Rewire `Menu`, `MenuBar`, `StatusBar`, `LineCanvas` to the new event.
- Delete the `#pragma warning disable CS0618` suppressions in those files.
- Update tests that assert the subscription chain.

### Phase C — JSON converter / `SourceGenerationContext` decoupling
- Replace all `ConfigurationManager.SerializerContext` references in `AttributeJsonConverter`, `SchemeJsonConverter`, `Concurrent/DictionaryJsonConverter`, `DeepCloner` (the last one is being deleted anyway) with `SourceGenerationContext.Default`.
- Audit `SourceGenerationContext` entries and replace scope types with the POCOs (§5.5).

### Phase D — Library `config.json` rewrite + migration helper
- Rewrite `Terminal.Gui/Resources/config.json` in nested format.
- Add `TuiConfigMigrator.MigrateFlatToNested(string)` and tests.
- Update `Examples/Config/example_config.json`.
- Update the JSON schema if hosted (file follow-up issue).

### Phase E — Delete legacy CM types + tests
- Delete every file listed in §4.1 and every test listed in §4.6.
- Delete `ModuleInitializers.cs`'s `ConfigurationManager.Initialize ()` call (§5.6).
- Delete `ConfigurationManager.PrintJsonErrors ()` from `ApplicationImpl.Lifecycle.cs`.
- Sweep stale doc-comment references (§4.4 last row).

### Phase F — Rename `MecThemeManager`/`MecSchemeManager` → `ThemeManager`/`SchemeManager`
- Now that the legacy types are gone, the `Mec` prefix is redundant.
- Pure rename — no behavior change.

### Phase G — Update examples (Runner, UICatalog, NativeAot, etc.)
- §4.8 sweep.

### Phase H — Update `docfx/docs/config.md`
- Rewrite to reflect the MEC contract.
- Remove every reference to `ConfigurationManager.Enable`, `ConfigLocations`, `ConfigurationProperty`, `SettingsScope`, `ThemeScope`, `AppSettingsScope`.
- Document `TuiConfigurationBuilder`, `IThemeManager`, `ISchemeManager`, the nested JSON schema, the migration helper.

### Phase I — Verification
- Run full test matrix (`UnitTestsParallelizable`, `UnitTests.NonParallelizable`, `IntegrationTests`).
- Confirm zero new warnings (the `CS0618` suppressions are gone — if anything tries to use a deleted obsolete API the build fails by design).
- Build `Examples/NativeAot` with `PublishAot=true` and record `before` / `after` binary size in the PR description.
- Run `Benchmarks/Configuration/*` and record delta.

---

## 7. Breaking Changes (Public API)

The following public surface is **removed**. Source-incompatible for any consumer that touched these (the predecessor #5411 marked them all `[Obsolete]`).

- `Terminal.Gui.Configuration.ConfigurationManager` (class)
- `Terminal.Gui.Configuration.ConfigurationManagerNotEnabledException`
- `Terminal.Gui.Configuration.ConfigurationPropertyAttribute`
- `Terminal.Gui.Configuration.ConfigLocations`
- `Terminal.Gui.Configuration.SettingsScope`, `ThemeScope`, `AppSettingsScope`, `Scope<T>`
- `Terminal.Gui.Configuration.ConfigProperty`
- `Terminal.Gui.Configuration.ConfigPropertyHostTypes`
- `Terminal.Gui.Configuration.SourcesManager` (replaced by `TuiConfigurationBuilder`)
- `Terminal.Gui.Configuration.DeepCloner`
- `Terminal.Gui.Configuration.ScopeJsonConverter<T>`
- Static `Terminal.Gui.Configuration.ThemeManager` — replaced by instance `IThemeManager`
- Static `Terminal.Gui.Configuration.SchemeManager` static class — replaced by instance `ISchemeManager` (with a static convenience facade for `GetScheme(string)` only)
- The `Terminal.Gui.Configuration.ConfigurationManager.Applied` / `Updated` events

### JSON file breaking change

User config files in the legacy flat-key format (`"Button.DefaultShadow": "..."`) must be migrated. A migration helper ships for one release.

### Migration guide

A separate `docfx/docs/migrate-cm-to-mec.md` is added in Phase H with a side-by-side cheatsheet. The PR description links to it.

---

## 8. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Hidden third-party consumers still call obsolete CM API | They received an `[Obsolete]` warning in #5411 with the message pointing at `TuiConfigurationBuilder`. One release window has elapsed by the time this PR ships. |
| Nested JSON breaks every existing user config file | Ship `TuiConfigMigrator` + UICatalog "Migrate" button + clear release-note + `migrate-cm-to-mec.md` guide. |
| Renaming `MecThemeManager` → `ThemeManager` clashes with the deleted legacy `ThemeManager` if a partial revert lands | Phase F runs only after Phase E commits; if reverted, the rename is reverted with it. |
| AOT size delta is smaller than predicted | Acceptable. The parallel-test and decoupling wins still justify the change. Record actual delta in the PR. |
| `IOptionsMonitor<T>.OnChange` is not invoked in NativeAOT due to missing trim roots | Verified in #5411. Re-verify with a smoke test in `Examples/NativeAot` as part of Phase I. |

---

## 9. Out of Scope

Carried forward from the predecessor spec (§10), unchanged:

1. Generic Host / `IHostBuilder` adoption.
2. Constructor injection mandate for all Views.
3. View-level DI.
4. Multiple concurrent `IApplication` instances.
5. TextMate / Markdig conditional compilation.
6. Lazy driver registration.
7. Assembly splitting.

---

*Authors: @copilot. Companion to [`specs/replace-cm-with-mec.md`](./replace-cm-with-mec.md). Tracking PR: #5416.*
