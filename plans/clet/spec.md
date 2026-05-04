# `clet` Implementation Spec

**Status:** draft v0.1 · for review · companion to the PR/FAQ in [issue #5155](https://github.com/gui-cs/Terminal.Gui/issues/5155)
**Owner:** TBD · **Target:** v1.0 GA (matches Terminal.Gui v2 GA)

This is the implementation spec. It assumes the PR/FAQ is broadly accepted and covers what to build, where it lives, what changes in Terminal.Gui to support it, how it ships, and how it's tested.

No em-dashes. Parens or semicolons. Scope is v1.0; v2 (third-party plugin loading, password clet, additional viewer clets) is out of scope unless explicitly noted.

---

## 1. Scope and Non-Goals

### In scope (v1.0)

- New assembly `Terminal.Gui.Clets` in `gui-cs/Terminal.Gui` (separate NuGet, separate version cadence inside the repo, ships with TG releases).
- New repo `gui-cs/clet` containing the CLI binary, native installer manifests, release automation.
- Fourteen input clets and one viewer clet (`md`) statically registered in v1.0.
- Native installer channels: Homebrew (gui-cs tap), WinGet, .NET tool. NativeAOT for native channels.
- Auto-release workflow tied to TG releases. Version 1:1 with TG.
- JSON output contract (schemaVersion 1).
- Inline input rendering; alt-screen viewer rendering.
- Theming via TG's `ConfigurationManager`.

### Out of scope (deferred to v2 or later)

- Third-party clet runtime loading (`Assembly.LoadFrom` into the AOT'd CLI).
- `password` clet.
- Additional viewer clets (`json`, `log`, `diff`).
- Telemetry beyond a one-shot opt-in install ping.
- Embedded/inline-in-other-TG-app use of clets (the "host inside another running TG app" use case is dropped per the v0.5 PR/FAQ).

---

## 2. Architecture Overview

Two repos, three deliverables, one release cadence.

```
gui-cs/Terminal.Gui                          gui-cs/clet
├── Terminal.Gui/         (core, unchanged   ├── src/Clet/
│                          public surface)   │     Program.cs
├── Terminal.Gui.Clets/   (NEW assembly)     │     CommandLineRoot.cs
│     IClet, IViewerClet                     │     OutputFormatter.cs
│     ICletRegistry                          │     ExitCodes.cs
│     CletRunOptions, CletRunResult<T>       ├── packaging/
│     Built-in clets                         │     homebrew/clet.rb.template
│     JSON schema + serialization            │     winget/manifests.template
├── Tests/                                   │     dotnet-tool/
│     UnitTestsParallelizable/Clets/         ├── .github/workflows/
│     IntegrationTests/Clets/                │     release-on-tg-release.yml
└── docs/clets.md                            │     smoke-test.yml
                                             │     publish-homebrew.yml
                                             │     publish-winget.yml
                                             │     publish-nuget.yml
                                             └── tests/
                                                 SmokeTests/
                                                 ContractTests/
```

**Process model.** The `clet` binary is a thin shell. It:
1. Parses CLI args (System.CommandLine).
2. Looks up the alias in `ICletRegistry`.
3. Initializes a Terminal.Gui `IApplication`.
4. Calls `clet.RunAsync(...)`.
5. Serializes the result, emits to stdout, exits with the right code.

All of (3)+(4) is plain Terminal.Gui hosting. The clet itself is a Terminal.Gui View.

---

## 3. Terminal.Gui Changes Required

These are the changes (or audits-with-fixes) the TG core needs to make `clet` viable. Each is tracked as its own issue/PR off the main `develop` branch.

### 3.1 Inline rendering audit and hardening (P0)

**Goal:** When an `IApplication` is run with `AppModel.Inline`, a Prompt-shaped view (single-row TextField, etc.) renders at the cursor's current line, claims a known number of lines, and on exit restores the cursor and leaves only its result on screen (or nothing, if cancelled). No alt-screen toggle.

**Tasks:**
- Audit current `AppModel.Inline` behavior across the four target terminals (macOS Terminal, iTerm2, Windows Terminal, GNOME Terminal) using a minimal repro.
- Identify and fix any remaining alt-screen toggles in the inline path.
- Add a `View.PreferredInlineHeight` (or equivalent) so the host can budget lines correctly.
- Verify cursor save/restore on every supported terminal driver (`NetDriver`, `WindowsDriver`, `UnixDriver`/`V2`).
- Snapshot tests for the inline output (see §7).

**Risk:** This is the riskiest TG change; "inline" is one of the kill criteria in the PR/FAQ.

### 3.2 Cancellation token plumbing (P0)

**Goal:** `IApplication.RunAsync(Toplevel, CancellationToken)` overload that cancels the run loop cleanly when the token is cancelled, returning whatever state the View has accumulated (so `IValue<T>.Value` is still readable for partial-result inspection if relevant).

**Tasks:**
- Add `Task RunAsync(Toplevel toplevel, CancellationToken cancellationToken)`.
- Wire `CancellationToken.Register` to post a "stop" message into the run loop.
- Ensure idempotent shutdown if both Ctrl-C and the token fire.
- Tests in `UnitTestsParallelizable` (no `Application.Init`, just `IApplication`).

**Why TG, not clet:** Cancellation must work for any TG app, not just clets. The hook belongs in core.

### 3.3 AOT compatibility audit (P0)

**Goal:** `Terminal.Gui` and `Terminal.Gui.Clets` publish under NativeAOT with zero trim warnings and zero AOT analyzer warnings.

**Tasks:**
- Add `<IsAotCompatible>true</IsAotCompatible>` to both csprojs.
- Build under `dotnet publish -p:PublishAot=true` with `TrimMode=full` and resolve every warning.
- Replace any reflection-based config loading (`ConfigurationManager`) with source-generated `JsonSerializerContext`.
- Replace any `Activator.CreateInstance` for theme/scheme types with explicit factory delegates or `[DynamicallyAccessedMembers]`.
- Document the AOT contract in `docs/aot.md`.

**Existing state:** unknown; must be assessed first. Likely the largest TG-side workstream by line count.

### 3.4 ConfigurationManager: load from arbitrary path (P1)

**Goal:** A clet binary running outside any "app" can load the user's TG theme by pointing at the standard config locations (`$XDG_CONFIG_HOME/terminal-gui/config.json`, `%APPDATA%\terminal-gui\config.json`).

**Tasks:**
- Verify `ConfigurationManager` already supports loading from disk by path. If not, add it.
- Document the search order.
- Tests: env-var override, missing-file fallback, malformed-file diagnostic.

### 3.5 Markdown View confirmation (P1)

**Goal:** Confirm the v2 `Markdown` View can run as a viewer clet:
- Read-only (no caret in body).
- Dismissable on `q`, Esc, Ctrl-C.
- Honors current theme.
- No external network on link click (links are surfaced, not opened).
- AOT-clean.

**Tasks:**
- Code review for AOT-incompatible patterns (regex source generation, `JsonSerializerContext` for any embedded JSON, etc.).
- Add a `Markdown.LinkPolicy` enum (`SurfaceOnly`, `OpenWithDefault`) defaulting to `SurfaceOnly` for clet's use.
- Snapshot tests against a fixed corpus of Markdown documents (CommonMark, GFM tables, code blocks, nested lists, links).

### 3.6 FileDialog: programmatic configuration (P1)

**Goal:** `FileDialog` can be constructed and run from a clet without depending on `Application` static state, given a root path, an extension filter list, and a "show hidden" flag.

**Tasks:**
- Verify the existing `FileDialog` constructor surface accepts these without static fallback.
- Add any missing properties (e.g., `RootPath`, `ExtensionFilters`, `ShowHiddenFiles` if not already present).
- Tests: each option exercised in isolation.

### 3.7 Terminal driver: detect inline-capable host (P2)

**Goal:** At driver init, detect whether the host terminal supports cursor-save/restore reliably enough for inline mode. If not, fall back to alt-screen with a stderr warning.

**Tasks:**
- Per-driver capability probe.
- `--force-inline` and `--force-altscreen` overrides on the clet CLI for users on edge-case terminals.

---

## 4. `Terminal.Gui.Clets` Assembly

### 4.1 Project layout

```
Terminal.Gui.Clets/
├── Terminal.Gui.Clets.csproj    (TFM net10.0, IsAotCompatible=true)
├── Abstractions/
│     IClet.cs
│     IViewerClet.cs
│     ICletRegistry.cs
│     CletKind.cs                (Input | Viewer)
│     CletRunOptions.cs
│     CletRunResult.cs           (record + record<T>)
│     CletRunStatus.cs           (Ok | Cancelled | Error | NoResult)
│     IParsableInitial.cs        (optional; mostly we use IParsable<T>)
├── Registry/
│     CletRegistry.cs            (instance)
│     CletRegistration.cs
├── Json/
│     CletJsonContext.cs         ([JsonSerializable] for source-gen)
│     CletJsonOutput.cs
│     SchemaV1.cs                (constants and shapes)
├── BuiltIn/
│     Input/
│       TextClet.cs
│       IntClet.cs
│       DecimalClet.cs
│       SelectClet.cs
│       MultiSelectClet.cs
│       ConfirmClet.cs
│       PickFileClet.cs
│       PickDirectoryClet.cs
│       DateClet.cs
│       TimeClet.cs
│       DurationClet.cs
│       ColorClet.cs
│       AttributePickerClet.cs
│       RangeClet.cs
│     Viewer/
│       MarkdownClet.cs
└── Hosting/
      CletHost.cs                (utility for the gui-cs/clet binary)
```

### 4.2 Public API (sketch)

```csharp
namespace Terminal.Gui.Clets;

public enum CletKind { Input, Viewer }

public enum CletRunStatus { Ok, Cancelled, Error, NoResult }

public sealed record CletRunOptions
{
    public string? Title { get; init; }
    public bool JsonOutput { get; init; }
    public TimeSpan? Timeout { get; init; }
    public string? ThemeName { get; init; }
    public bool ForceInline { get; init; }
    public bool ForceAltScreen { get; init; }
    public IReadOnlyDictionary<string, string>? CletOptions { get; init; }
}

public readonly record struct CletRunResult
{
    public CletRunStatus Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public readonly record struct CletRunResult<T>
{
    public CletRunStatus Status { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public CletRunResult ToUntyped () => new () { Status = Status, ErrorCode = ErrorCode, ErrorMessage = ErrorMessage };
}

public interface IClet
{
    string PrimaryAlias { get; }
    IReadOnlyList<string> Aliases { get; }
    string Description { get; }
    CletKind Kind { get; }
    Type ResultType { get; }
    IReadOnlyList<CletOptionDescriptor> Options { get; }
}

public interface IClet<TValue> : IClet
{
    Task<CletRunResult<TValue>> RunAsync (
        IApplication app,
        string? initial,
        CletRunOptions options,
        CancellationToken cancellationToken);
}

public interface IViewerClet : IClet
{
    Task<CletRunResult> RunAsync (
        IApplication app,
        string? content,
        CletRunOptions options,
        CancellationToken cancellationToken);
}

public interface ICletRegistry
{
    void Register (IClet clet);
    bool TryResolve (string alias, out IClet clet);
    IReadOnlyCollection<IClet> All { get; }
}

public sealed record CletOptionDescriptor (
    string Name,
    string? ShortName,
    Type ValueType,
    string Description,
    bool Required,
    string? DefaultValue);
```

### 4.3 JSON schema (v1)

```json
{
  "$id": "https://gui-cs.github.io/clet/schema/v1.json",
  "type": "object",
  "required": ["schemaVersion", "status"],
  "properties": {
    "schemaVersion": { "const": 1 },
    "status": { "enum": ["ok", "cancelled", "error", "no-result"] },
    "type":    { "type": "string" },
    "value":   { },
    "code":    { "type": "string" },
    "message": { "type": "string" }
  },
  "allOf": [
    { "if": { "properties": { "status": { "const": "ok"    } } },
      "then": { "anyOf": [ { "required": ["type", "value"] }, { "not": { "required": ["type"] } } ] } },
    { "if": { "properties": { "status": { "const": "error" } } },
      "then": { "required": ["code", "message"] } }
  ]
}
```

Schema is published in the `gui-cs/clet` repo and pinned in `CletJsonContext`. Contract tests (§7.4) validate every emitted line against this schema.

### 4.4 Source-generated registration

For AOT compatibility and to avoid `typeof(...)` registration spam in `Program.Main`, use a small source generator:

```csharp
[Clet ("text", typeof (string))]
public sealed class TextClet : IClet<string> { ... }
```

The generator produces `BuiltInClets.RegisterAll(ICletRegistry registry)` which calls `registry.Register(new TextClet())` for each. Generator lives in `Terminal.Gui.Clets.SourceGen/` and is referenced as a build-time-only package.

---

## 5. `gui-cs/clet` Repo

### 5.1 Layout

```
gui-cs/clet/
├── README.md
├── LICENSE
├── src/
│   └── Clet/
│       ├── Clet.csproj          (Exe, PublishAot=true, TFM net10.0)
│       ├── Program.cs           (Main, async)
│       ├── CommandLineRoot.cs   (System.CommandLine root)
│       ├── AliasDispatcher.cs
│       ├── OutputFormatter.cs   (text vs JSON)
│       └── ExitCodes.cs
├── tests/
│   ├── Clet.SmokeTests/         (boot every clet, capture stdout/exit)
│   └── Clet.ContractTests/      (JSON schema validation)
├── packaging/
│   ├── homebrew/
│   │   └── clet.rb.template
│   ├── winget/
│   │   ├── gui-cs.clet.installer.yaml.template
│   │   ├── gui-cs.clet.locale.en-US.yaml.template
│   │   └── gui-cs.clet.yaml.template
│   └── dotnet-tool/
│       └── Clet.Tool.csproj
├── .github/
│   └── workflows/
│       ├── ci.yml                          (build, test on push)
│       ├── release-on-tg-release.yml       (triggered by TG release)
│       ├── smoke-test.yml                  (matrix per RID)
│       ├── publish-homebrew.yml
│       ├── publish-winget.yml
│       └── publish-nuget.yml
├── scripts/
│   ├── sign-macos.sh
│   ├── sign-windows.ps1
│   ├── notarize-macos.sh
│   └── update-homebrew-tap.sh
└── docs/
    ├── installing.md
    ├── json-schema.md
    └── exit-codes.md
```

### 5.2 `Program.Main` outline

```csharp
public static async Task<int> Main (string[] args)
{
    using CancellationTokenSource cts = new ();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel (); };

    ICletRegistry registry = new CletRegistry ();
    BuiltInClets.RegisterAll (registry);

    CommandLineRoot root = new (registry);
    return await root.InvokeAsync (args, cts.Token);
}
```

### 5.3 CLI surface

```
clet <alias> [initial] [--json] [--timeout 30s] [--theme <name>] [--inline] [--alt-screen] [clet-specific options]
clet list [--json]
clet help <alias>
clet --version
```

### 5.4 Exit code mapping

| Status                | Exit |
|-----------------------|-----:|
| Ok (input or viewer)  |    0 |
| NoResult              |    1 |
| Usage error           |    2 |
| Validation error      |   65 |
| I/O error             |   74 |
| Cancelled             |  130 |

### 5.5 NativeAOT publish settings

In `Clet.csproj`:
```xml
<PublishAot>true</PublishAot>
<InvariantGlobalization>false</InvariantGlobalization>  <!-- date/time clets need real culture -->
<StackTraceSupport>true</StackTraceSupport>             <!-- helpful diagnostics -->
<DebuggerSupport>false</DebuggerSupport>
<EventSourceSupport>false</EventSourceSupport>
<UseSystemResourceKeys>true</UseSystemResourceKeys>
```

Target binary size: ~8MB. Cold-start budget: <100ms on Apple Silicon, <150ms on Windows x64.

---

## 6. Release and Update Pipeline

### 6.1 Trigger

When `gui-cs/Terminal.Gui` cuts a release (`v2.X.Y` tag pushed), a `repository_dispatch` event is sent to `gui-cs/clet`. The `release-on-tg-release.yml` workflow consumes it.

```yaml
# gui-cs/Terminal.Gui/.github/workflows/notify-clet-on-release.yml (NEW)
on:
  release:
    types: [published]
jobs:
  notify-clet:
    runs-on: ubuntu-latest
    steps:
      - uses: peter-evans/repository-dispatch@v3
        with:
          token: ${{ secrets.CLET_DISPATCH_PAT }}
          repository: gui-cs/clet
          event-type: tg-released
          client-payload: '{"tg_version": "${{ github.event.release.tag_name }}"}'
```

### 6.2 Build matrix

```yaml
# gui-cs/clet/.github/workflows/release-on-tg-release.yml
on:
  repository_dispatch:
    types: [tg-released]
jobs:
  build:
    strategy:
      matrix:
        rid:
          - osx-arm64
          - osx-x64
          - linux-x64
          - linux-arm64
          - win-x64
          - win-arm64
        include:
          - rid: osx-arm64   ; runner: macos-14
          - rid: osx-x64     ; runner: macos-13
          - rid: linux-x64   ; runner: ubuntu-22.04
          - rid: linux-arm64 ; runner: ubuntu-22.04-arm
          - rid: win-x64     ; runner: windows-2022
          - rid: win-arm64   ; runner: windows-11-arm
    runs-on: ${{ matrix.runner }}
    steps:
      - uses: actions/checkout@v4
      - run: dotnet publish src/Clet -c Release -r ${{ matrix.rid }} --self-contained -p:PublishAot=true
      - name: Smoke test
        run: ./scripts/smoke-test.sh
      - name: Sign (macOS)
        if: startsWith(matrix.rid, 'osx-')
        run: ./scripts/sign-macos.sh
      - name: Sign (Windows)
        if: startsWith(matrix.rid, 'win-')
        run: ./scripts/sign-windows.ps1
      - uses: actions/upload-artifact@v4
```

### 6.3 Smoke test gate (P0; release fails closed)

Before any publish step, every built binary runs a smoke matrix:

1. `clet --version` returns the TG version.
2. `clet list --json` validates against the schema.
3. For each input clet: spawn with `--initial <stub> --json --timeout 1s`, send a fake "Enter" via stdin, verify exit 0 and JSON shape.
4. For `md`: spawn against a fixture markdown file, send "q", verify exit 0 and JSON shape.
5. Cancellation: spawn with `--timeout 100ms`, verify exit 130.

Any failure halts the publish workflow. The maintainer is paged via the release issue's auto-comment.

### 6.4 Publish steps

After all matrix jobs and smoke tests pass:

**Homebrew tap** (`gui-cs/homebrew-tap`):
- Generate `clet.rb` from `clet.rb.template` with new version + SHA256s for each bottle.
- PR (or push-with-token) to the tap repo.
- Verify with `brew install --build-bottle gui-cs/tap/clet` on a fresh runner.

**WinGet** (PR to `microsoft/winget-pkgs`):
- Generate manifests from templates with new version + installer URLs + SHA256s.
- Use `wingetcreate update` with the GitHub token.
- Manifest PR is auto-merged by Microsoft's bot if validation passes (otherwise paged).

**.NET tool** (NuGet):
- `dotnet pack` the `Clet.Tool` project.
- `dotnet nuget push` to nuget.org with the API key.

### 6.5 Failure handling

If any publish step fails:
- The workflow opens an issue in `gui-cs/clet` titled `Release v<TG_VERSION> failed at <step>`.
- Tags the maintainer team.
- Already-published channels are noted; rollback is manual (we don't auto-revert).
- The smoke-test step ensures broken binaries never hit a channel; failures here are most often manifest/signing problems, not runtime regressions.

### 6.6 Version 1:1 with TG

The `Clet.csproj` `Version` property is set at build time from the dispatch payload's `tg_version`. There is no version negotiation, no compatibility matrix, no "clet 1.5 supports TG 2.3+." The pair is locked.

---

## 7. Testing Plan

The user asked for thorough; this section is detailed accordingly. Five test layers, each with a clear "what does this catch" purpose.

### 7.1 Unit tests (Tests/UnitTestsParallelizable/Clets)

**What this catches:** Logic bugs in the registry, options, parsing, JSON serialization, exit code mapping.

**Coverage target:** 90%+ for `Terminal.Gui.Clets`.

**Cases:**
- `CletRegistry`:
  - Register/resolve by primary alias, by secondary alias.
  - Conflict on duplicate alias raises `InvalidOperationException`.
  - `All` is stable in iteration order.
- `CletRunOptions`:
  - Default values.
  - Mutually exclusive options (`ForceInline` + `ForceAltScreen`) rejected at construction.
- `IParsable<T>` integration:
  - String → int, decimal, DateTime, TimeSpan via reflection-free hooks.
  - Bad input → `CletRunResult { Status = Error, ErrorCode = "validation" }`.
- `CletJsonOutput`:
  - Round-trip every result variant.
  - Output matches `SchemaV1` byte-for-byte for canonical inputs (golden files).
  - No properties leak (`type` absent on viewer clets).
- Exit code mapping:
  - Each `CletRunStatus` and error code maps to the documented exit.
- Cancellation:
  - `CletRunResult.Cancelled` propagates through every layer.

**Per-clet behavior tests** (one fixture per clet; 15 total):
- Register, resolve, advertise correct `Kind` and `ResultType`.
- Default options round-trip.
- Initial-value parsing with valid input.
- Initial-value rejection with invalid input.
- (Where applicable) options: `--root`, `--filter`, `--multi`, etc., each tested in isolation.

**Patterns:** xUnit v3, `[Fact]` and `[Theory]`. No `Application.Init`. Each test file leads with `// Claude - Opus 4.7` per CLAUDE.md.

### 7.2 Integration tests (Tests/IntegrationTests/Clets)

**What this catches:** TG hosting bugs (init/teardown, cancellation, rendering) that unit tests can't see because they don't run a real run loop.

**Cases:**
- Run each clet end-to-end against a scripted input/output stream.
- Cancellation token cancels mid-run; verify final result and clean shutdown.
- Timeout fires; verify `Status = Cancelled` and exit 130.
- Theme override per invocation; verify View's effective scheme.
- Inline vs alt-screen mode; verify driver state transitions.

**Test harness:** `IApplication` instance per test, scripted input stream (a `TextReader` substitute for keyboard events), captured render output (snapshot to string).

### 7.3 CLI/process tests (gui-cs/clet/tests/Clet.SmokeTests)

**What this catches:** Bugs that only appear when `clet` runs as a real process (argument parsing, stdout/stderr wiring, exit codes, signal handling).

**Cases:** Identical to §6.3 smoke tests (every clet boots, returns valid JSON, exits with correct code). Run on every PR to `gui-cs/clet`, every TG-triggered release build, and nightly against the latest TG develop branch.

**Tooling:** `Process.Start`, capture stdout/stderr, assert exit code.

### 7.4 JSON contract tests (gui-cs/clet/tests/Clet.ContractTests)

**What this catches:** Schema drift; promises to AI agent consumers being broken silently.

**Cases:**
- Every line emitted by every clet across the full input matrix validates against `SchemaV1`.
- Schema additions in v1.x are confirmed additive only (a v1.0 consumer can still parse v1.x output).
- `clet list --json` validates against its own list schema.

**Tooling:** `JsonSchema.Net` for validation. The schema file is the source of truth; tests read it, not a copy.

### 7.5 Cross-terminal manual matrix

**What this catches:** Driver-specific rendering bugs that automated tests can't reproduce reliably (cursor save/restore, alt-screen toggles, mouse).

**Matrix:**

|                  | macOS Terminal | iTerm2 | Windows Terminal | GNOME Terminal |
|------------------|:--------------:|:------:|:----------------:|:--------------:|
| `clet text`      |       ☐        |   ☐    |        ☐         |       ☐        |
| `clet pick-file` |       ☐        |   ☐    |        ☐         |       ☐        |
| `clet md`        |       ☐        |   ☐    |        ☐         |       ☐        |
| Theme switch     |       ☐        |   ☐    |        ☐         |       ☐        |
| Mouse click      |       ☐        |   ☐    |        ☐         |       ☐        |
| Inline restore   |       ☐        |   ☐    |        ☐         |       ☐        |

Run before every minor release (v1.0, v1.1, ...). Captured in a release checklist issue. This is the v0.5 milestone gate.

### 7.6 AOT publish tests

**What this catches:** Trim warnings, runtime AOT failures, regressions in AOT-compatibility of TG core.

**Cases:**
- CI publishes the AOT binary on every PR to `gui-cs/clet` and to `gui-cs/Terminal.Gui`'s clet-touching paths.
- Zero trim warnings tolerated; warnings fail the build.
- Smoke tests (§7.3) run against the AOT binary, not just the JIT'd debug build.

### 7.7 Performance tests (gui-cs/clet/tests/Clet.PerfTests)

**What this catches:** Cold-start regressions that erode the "feels instant" property AI agents need.

**Cases:**
- `clet --version` cold start: <100ms macOS arm64, <150ms Windows x64.
- `clet list --json` cold start: same budgets.
- Tracked over time; regression alerts at +25% on a 7-day rolling baseline.

### 7.8 Markdown rendering golden-file tests

**What this catches:** `Markdown` View regressions visible to clet users specifically (where TG's own tests might pass but the rendered output for `clet md` looks wrong).

**Corpus:** CommonMark spec examples + GFM table samples + a curated "real READMEs" set (TG's own README, .NET runtime README, a few popular OSS projects).

**Method:** Render each to a fixed terminal size, capture as ANSI text, diff against golden. Updates require explicit reviewer approval.

### 7.9 Release pipeline dry-run tests

**What this catches:** Workflow regressions that would otherwise only surface during a real TG release (when the cost is high).

**Cases:**
- Weekly cron: simulate a `repository_dispatch` with a fake version. Build, smoke-test, generate manifests, but stop short of publish.
- Verify all template files render correctly, all artifact uploads succeed, all checksums match.

---

## 8. Milestones

| Milestone | Date target | Exit criteria |
|-----------|-------------|---------------|
| **v0.1 alpha** | T+4 weeks | `Terminal.Gui.Clets` skeleton; `IClet`, `ICletRegistry` defined; `text` and `confirm` clets working in tests. |
| **v0.3 alpha** | T+8 weeks | All 14 input clets functional in unit tests. JSON schema drafted. AOT audit complete. |
| **v0.5 beta** | T+14 weeks | Naming locked; JSON schema locked; exit-code table locked; inline rendering proven on the four-terminal matrix; v1.0 input and viewer lists locked; Markdown View integration verified end-to-end including link safety; threat model published; Homebrew tap and WinGet manifest in working draft form; the gui-cs/clet release workflow proven against a real TG release cut. |
| **v0.9 RC** | T+18 weeks | All §7 test layers passing in CI. One real release cycle exercised end-to-end. |
| **v1.0 GA** | T+20 weeks (matches TG v2 GA) | Brew, WinGet, NuGet channels live. Documentation published. Issue templates for clet bugs in place. |

---

## 9. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------:|-------:|------------|
| Inline rendering broken on one or more target terminals | Medium | High (kills v1.0 claim) | §3.1 audit early; explicit go/no-go at v0.5; ship without the inline claim if not solved. |
| AOT incompatibility deeper than expected in TG core | Medium | High (forces channel change) | §3.3 audit at v0.1; if blocking, fall back to self-contained single-file (~30MB) and document. |
| Native installer pipeline (Homebrew/WinGet) ops cost | Medium | Medium | §6.3 smoke gate + §7.9 dry-runs catch most issues pre-publish; explicit on-call rotation for release weeks. |
| Markdown View quality regression vs `glow` | Low | Medium | §7.8 golden-file corpus; quarterly comparison run. |
| Naming concerns about "clet" surfacing in support channels | Low | Low | Acknowledge in docs; outlast. |
| Cancellation tokens in TG core have unforeseen complexity | Medium | Medium | §3.2 spike at v0.1; if hard, ship clet with a polling-based cancellation as fallback. |

---

## 10. Open Questions

1. **Theme location on disk.** The PR/FAQ says `ConfigurationManager` themes apply, but the search path for a system-wide theme isn't documented. Confirm with the TG core team before v0.5.
2. **WinGet ARM64.** The `win-arm64` matrix entry assumes WinGet ARM64 publishing is supported; verify, and fall back to x64-only if not.
3. **Telemetry.** The PR/FAQ mentions an opt-in usage ping. Spec deliberately does not include this in v1.0 scope; revisit at v1.1 with a privacy review.
4. **Homebrew tap repo name.** `gui-cs/homebrew-tap` is assumed; confirm it exists or create.
5. **Code signing certs.** Apple Developer ID and Authenticode certs are operational dependencies; confirm ownership/renewal process before v0.9.
6. **`range` clet result type.** `(low, high)` tuple, named record, or two separate fields? Decide before locking the JSON schema at v0.5.
7. **`md` content source.** File argument (`clet md README.md`), stdin (`cat README.md | clet md -`), or both? Both is implied; confirm CLI shape.

---

## 11. Implementation Order

A suggested sequence (linear, not parallelizable until v0.3 except where noted):

1. TG audit: §3.1 inline, §3.2 cancellation, §3.3 AOT (§3.3 can run in parallel as a dedicated workstream).
2. `Terminal.Gui.Clets` skeleton: abstractions, registry, JSON, source generator.
3. First two clets (`text`, `confirm`) end-to-end in unit + integration tests.
4. `gui-cs/clet` repo bootstrapped: Program.Main, System.CommandLine, alias dispatch, output formatter.
5. Smoke test harness (§7.3) running on a single RID.
6. Remaining input clets, in order of complexity: `int`, `decimal`, `select`, `multi-select`, `range`, `date`, `time`, `duration`, `color`, `attribute-picker`, `pick-directory`, `pick-file`.
7. `md` viewer clet (after §3.5 confirmation).
8. Release pipeline: build matrix, signing, smoke gate.
9. Publish channels: Homebrew first (lowest ops friction), then WinGet, then NuGet tool.
10. v0.5 gate: four-terminal matrix run + threat model + locked schema.
11. RC and GA.

---

## Appendix A: Threat Model Summary

(Full document published with v0.5; sketch only here.)

- **Untrusted inputs:** `--initial`, env vars, stdin content, fixture file paths, `--title`, clet-specific options.
- **Sanitization:** All output to stdout/stderr passes through a terminal-escape filter (strip C0/C1 control sequences except those we generate). User-controlled display strings (`--title`, prompt labels) sanitized at the View boundary.
- **Markdown link policy:** Default `SurfaceOnly` (links shown, never auto-opened). `--allow-link-open` flag for the user to opt in; off by default for AI agent use.
- **File access:** `pick-file` and `pick-directory` honor the OS sandbox/permission model; no privilege escalation.
- **Plugin loading:** None in v1.0. (Closes the entire LoadFrom-based attack surface.)

## Appendix B: Cross-References

- PR/FAQ: [issue #5155](https://github.com/gui-cs/Terminal.Gui/issues/5155)
- TG core docs: `docfx/docs/application.md`, `docfx/docs/View.md`, `docfx/docs/cancellable-work-pattern.md`
- Contributor rules: `.claude/rules/`, `CLAUDE.md`
