# `clet` Implementation Spec

**Status:** draft v0.3 · for review · companion to the PR/FAQ in [issue #5155](https://github.com/gui-cs/Terminal.Gui/issues/5155)
**Owner:** TBD · **Target:** v1.0 GA (matches Terminal.Gui v2 GA)

This is the implementation spec. It assumes the PR/FAQ is broadly accepted and covers what to build, where it lives, what changes in Terminal.Gui to support it, how it ships, and how it's tested.

No em-dashes. Parens or semicolons. Scope is v1.0; v2 (third-party plugin loading, password clet, additional viewer clets) is out of scope unless explicitly noted.

**v0.3 change vs v0.2:** five §3 TG-side workstreams dropped (inline rendering audit, AOT pre-emptive audit, `ConfigurationManager` path loading, `Markdown` View confirmation, terminal-driver inline detection) because each is already done or already tracked. §3 now lists two items: cancellation token plumbing (P0) and a `FileDialog` typed-result refactor (P1; change the base from `Dialog<int>` to `Dialog<List<string>?>`). AOT issues are discovered via §6.6 publish tests, not a separate audit. CLI surface trimmed: `--theme` removed (`ConfigurationManager` already exposes several ways to set themes); `--alt-screen` removed; `--inline` is the default and `--fullscreen` is the opt-out; `clet --help` and `clet help <alias>` render via Terminal.Gui's `Markdown` View, the same path `clet md` uses.

**v0.2 change vs v0.1:** the `Terminal.Gui.Clets` library assembly is dropped. All clet abstractions, registry, JSON, source generator, and built-in clet implementations live in `gui-cs/clet` alongside the CLI binary. Terminal.Gui core gains no clet-specific types; its only clet-related artifact is a release-notification GitHub workflow (§5.1).

---

## 1. Scope and Non-Goals

### In scope (v1.0)

- New repo `gui-cs/clet` containing all clet code: abstractions, registry, JSON, source generator, built-in clets, CLI binary, native installer manifests, release automation.
- Targeted changes to `gui-cs/Terminal.Gui` core (§3) that benefit TG generally and unblock clet specifically.
- Fourteen input clets and one viewer clet (`md`) statically registered in v1.0.
- Native installer channels: Homebrew (gui-cs tap), WinGet, .NET tool. NativeAOT for native channels.
- Auto-release workflow tied to TG releases. Version 1:1 with TG.
- JSON output contract (schemaVersion 1).
- Inline input rendering; alt-screen viewer rendering.
- Theming via TG's `ConfigurationManager`.

### Out of scope (deferred to v2 or later)

- Extracting clet abstractions into a published NuGet for third-party consumption (`Clet.Abstractions`). Today, `IClet` is internal-to-the-binary; v2 may publish it.
- Third-party clet runtime loading (`Assembly.LoadFrom` into the AOT'd CLI).
- `password` clet.
- Additional viewer clets (`json`, `log`, `diff`).
- Telemetry beyond a one-shot opt-in install ping.
- Embedded/inline-in-other-TG-app use of clets.

---

## 2. Architecture Overview

Two repos. One assembly that matters (the CLI exe). One release cadence.

```
gui-cs/Terminal.Gui                           gui-cs/clet
├── Terminal.Gui/                             ├── src/
│     (core; §3 tweaks land here,             │   ├── Clet/
│      no clet-specific types)                │   │     Abstractions/  (IClet, ICletRegistry, ...)
├── Tests/                                    │   │     Registry/
│     (TG core tests only;                    │   │     Json/          (CletJsonContext, schema)
│      clet tests live in gui-cs/clet)        │   │     Clets/Input/   (Text, PickFile, ...)
└── .github/workflows/                        │   │     Clets/Viewer/  (Markdown)
      notify-clet-on-release.yml (NEW)        │   │     Hosting/       (Program.cs, CLI)
                                              │   └── Clet.SourceGen/  (build-time analyzer)
                                              ├── tests/
                                              │     Clet.UnitTests/
                                              │     Clet.IntegrationTests/
                                              │     Clet.SmokeTests/
                                              │     Clet.ContractTests/
                                              │     Clet.PerfTests/
                                              ├── packaging/
                                              │     homebrew/clet.rb.template
                                              │     winget/manifests.template
                                              │     dotnet-tool/Clet.Tool.csproj
                                              ├── .github/workflows/
                                              │     ci.yml
                                              │     release-on-tg-release.yml
                                              │     publish-homebrew.yml
                                              │     publish-winget.yml
                                              │     publish-nuget.yml
                                              └── scripts/
                                                    sign-macos.sh
                                                    sign-windows.ps1
                                                    notarize-macos.sh
                                                    update-homebrew-tap.sh
```

**Process model.** The `clet` binary is a thin shell:
1. Parses CLI args (System.CommandLine).
2. Looks up the alias in its in-process `ICletRegistry`.
3. Initializes a Terminal.Gui `IApplication`.
4. Calls `clet.RunAsync(...)`.
5. Serializes the result, emits to stdout, exits with the right code.

All of (3)+(4) is plain Terminal.Gui hosting against TG's public API. The clet itself is a Terminal.Gui View. Nothing in TG core knows about clets; nothing in clets requires private TG API.

---

## 3. Terminal.Gui Changes Required

Most of what an early draft of this spec assumed would need to change in TG is already done or already tracked, including:

- **Inline rendering** is shipping today and exercised by `md`, the inline examples, and `gui-cs/ai`.
- **AOT compatibility** is tracked in TG core; remaining issues surface most efficiently by building `clet` and running it. The §6.6 publish tests are the discovery mechanism.
- **`ConfigurationManager`** path-based loading is broadly used and tested.
- **`Markdown` View** is vetted for the read-only, dismissable, themed shape clet needs.
- **Terminal-driver inline-capable detection** is already in place.

Two items remain. Each is a general TG improvement, not a clet-specific one.

### 3.1 Cancellation token plumbing (P0)

**Goal:** `IApplication.RunAsync(Toplevel, CancellationToken)` overload that cancels the run loop cleanly when the token is cancelled, returning whatever state the View has accumulated (so `IValue<T>.Value` is still readable for partial-result inspection if relevant).

**Tasks:**
- Add `Task RunAsync(Toplevel toplevel, CancellationToken cancellationToken)`.
- Wire `CancellationToken.Register` to post a "stop" message into the run loop.
- Ensure idempotent shutdown if both Ctrl-C and the token fire.
- Tests in `UnitTestsParallelizable` (no `Application.Init`, just `IApplication`).

**Why TG, not clet:** Cancellation must work for any TG app, not just clets. The hook belongs in core.

### 3.2 FileDialog: typed result refactor (P1)

**Background:** `FileDialog` currently inherits from `Dialog`, which inherits from `Dialog<int>`. The `int` result is an OK/Cancel sentinel; the actual selected paths are read off the dialog instance after the run completes. That shape doesn't compose with `IValue<T>` cleanly for clet's typed-result contract.

**Goal:** Refactor `FileDialog` to inherit from `Dialog<List<string>?>` (or equivalent; TG core team's call on the exact result type), so the typed result *is* the selection, with `null` representing cancel. This unblocks `pick-file` and `pick-directory` clets without per-clet glue code, and improves `FileDialog` for any TG app.

**Tasks:**
- Change the base type. Resulting `IValue<List<string>?>` (or `IValue<FileInfo?>` for single-select; team's call) is what clets bind to.
- Audit all in-tree callers of `FileDialog` for the result-shape change. Update callers (UICatalog scenarios, examples, tests).
- Migration notes for downstream users; this is a public API change, breaking for any v2 caller relying on the old `int` shape.
- Tests: existing `FileDialog` tests updated to consume the typed result.

**Why TG, not clet:** This is a TG public-API improvement that benefits any app using `FileDialog`. Forking the dialog just for clet would be wrong.

---

## 4. `gui-cs/clet` Repo

This repo holds everything: abstractions, registry, JSON, source generator, built-in clets, the CLI binary, packaging, and release automation. One assembly is published; everything else is build-time only or test-only.

### 4.1 Project layout

```
gui-cs/clet/
├── README.md
├── LICENSE
├── Clet.sln
├── src/
│   ├── Clet/                              (single Exe project; PublishAot=true; net10.0)
│   │   ├── Clet.csproj
│   │   ├── Abstractions/
│   │   │     IClet.cs
│   │   │     IViewerClet.cs
│   │   │     ICletRegistry.cs
│   │   │     CletKind.cs                  (Input | Viewer)
│   │   │     CletRunOptions.cs
│   │   │     CletRunResult.cs             (record + record<T>)
│   │   │     CletRunStatus.cs             (Ok | Cancelled | Error | NoResult)
│   │   │     CletOptionDescriptor.cs
│   │   ├── Registry/
│   │   │     CletRegistry.cs
│   │   │     CletRegistration.cs
│   │   ├── Json/
│   │   │     CletJsonContext.cs           ([JsonSerializable] for source-gen)
│   │   │     CletJsonOutput.cs
│   │   │     SchemaV1.cs
│   │   ├── Clets/
│   │   │   ├── Input/
│   │   │   │     TextClet.cs
│   │   │   │     IntClet.cs
│   │   │   │     DecimalClet.cs
│   │   │   │     SelectClet.cs
│   │   │   │     MultiSelectClet.cs
│   │   │   │     ConfirmClet.cs
│   │   │   │     PickFileClet.cs
│   │   │   │     PickDirectoryClet.cs
│   │   │   │     DateClet.cs
│   │   │   │     TimeClet.cs
│   │   │   │     DurationClet.cs
│   │   │   │     ColorClet.cs
│   │   │   │     AttributePickerClet.cs
│   │   │   │     RangeClet.cs
│   │   │   └── Viewer/
│   │   │         MarkdownClet.cs
│   │   └── Hosting/
│   │         Program.cs                   (Main, async)
│   │         CommandLineRoot.cs           (System.CommandLine root)
│   │         AliasDispatcher.cs
│   │         OutputFormatter.cs
│   │         ExitCodes.cs
│   └── Clet.SourceGen/                    (build-time analyzer; not shipped)
│         Clet.SourceGen.csproj
│         CletAttribute.cs
│         RegistrationGenerator.cs
├── tests/
│   ├── Clet.UnitTests/                    (parallelizable; pure logic)
│   ├── Clet.IntegrationTests/             (TG hosting + run-loop)
│   ├── Clet.SmokeTests/                   (process-level: spawn the exe)
│   ├── Clet.ContractTests/                (JSON schema validation)
│   └── Clet.PerfTests/                    (cold-start budgets)
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

**One src project (`Clet`).** Abstractions, registry, JSON, built-in clets, and `Program.Main` all compile into one assembly. No internal NuGet packaging. The source generator is a separate project because Roslyn analyzers must be (build-time only, never referenced at runtime).

### 4.2 Public types (sketch)

These are `internal` to the `Clet` assembly in v1.0. v2 may extract them to `Clet.Abstractions` and publish, when third-party plugin loading lands.

```csharp
namespace Clet;

internal enum CletKind { Input, Viewer }

internal enum CletRunStatus { Ok, Cancelled, Error, NoResult }

internal sealed record CletRunOptions
{
    public string? Title { get; init; }
    public bool JsonOutput { get; init; }
    public TimeSpan? Timeout { get; init; }
    public bool Fullscreen { get; init; }
    public IReadOnlyDictionary<string, string>? CletOptions { get; init; }
}

internal readonly record struct CletRunResult
{
    public CletRunStatus Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

internal readonly record struct CletRunResult<T>
{
    public CletRunStatus Status { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public CletRunResult ToUntyped () => new () { Status = Status, ErrorCode = ErrorCode, ErrorMessage = ErrorMessage };
}

internal interface IClet
{
    string PrimaryAlias { get; }
    IReadOnlyList<string> Aliases { get; }
    string Description { get; }
    CletKind Kind { get; }
    Type ResultType { get; }
    IReadOnlyList<CletOptionDescriptor> Options { get; }
}

internal interface IClet<TValue> : IClet
{
    Task<CletRunResult<TValue>> RunAsync (
        IApplication app,
        string? initial,
        CletRunOptions options,
        CancellationToken cancellationToken);
}

internal interface IViewerClet : IClet
{
    Task<CletRunResult> RunAsync (
        IApplication app,
        string? content,
        CletRunOptions options,
        CancellationToken cancellationToken);
}

internal interface ICletRegistry
{
    void Register (IClet clet);
    bool TryResolve (string alias, out IClet clet);
    IReadOnlyCollection<IClet> All { get; }
}

internal sealed record CletOptionDescriptor (
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

Schema is published in this repo under `docs/json-schema.md` and pinned in `Json/SchemaV1.cs`. Contract tests (§6.4) validate every emitted line against this schema.

### 4.4 Source-generated registration

For AOT compatibility and to avoid `typeof(...)` registration spam in `Program.Main`:

```csharp
[Clet ("text", typeof (string))]
internal sealed class TextClet : IClet<string> { ... }
```

The generator produces `BuiltInClets.RegisterAll(ICletRegistry registry)` which calls `registry.Register(new TextClet())` for each.

### 4.5 `Program.Main` outline

```csharp
internal static class Program
{
    public static async Task<int> Main (string[] args)
    {
        using CancellationTokenSource cts = new ();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel (); };

        ICletRegistry registry = new CletRegistry ();
        BuiltInClets.RegisterAll (registry);

        CommandLineRoot root = new (registry);

        return await root.InvokeAsync (args, cts.Token);
    }
}
```

### 4.6 CLI surface

```
clet <alias> [initial] [--json] [--timeout 30s] [--fullscreen] [clet-specific options]
clet list [--json]
clet help <alias>
clet --help
clet --version
```

**Defaults.** Input clets render inline. Viewer clets (`md`) render fullscreen. `--fullscreen` forces fullscreen for input clets; it's a no-op for viewers.

**Theming.** No `--theme` flag. Theme selection goes through `ConfigurationManager`'s existing mechanisms (config files, env var, system theme); `clet` honors whatever it resolves.

**Help rendering.** `clet --help` and `clet help <alias>` render their content via Terminal.Gui's `Markdown` View, the same code path `clet md` uses. Help content is authored as Markdown under `src/Clet/Help/` (one file per alias plus a top-level overview), embedded as resources, and surfaced through the same dismissable, themed, scrollable viewer experience as `mdv` and the help system in `Examples/UICatalog`. This means help is browsable with the keys the user already knows.

### 4.7 Exit code mapping

| Status                | Exit |
|-----------------------|-----:|
| Ok (input or viewer)  |    0 |
| NoResult              |    1 |
| Usage error           |    2 |
| Validation error      |   65 |
| I/O error             |   74 |
| Cancelled             |  130 |

### 4.8 NativeAOT publish settings

In `Clet.csproj`:
```xml
<PublishAot>true</PublishAot>
<InvariantGlobalization>false</InvariantGlobalization>
<StackTraceSupport>true</StackTraceSupport>
<DebuggerSupport>false</DebuggerSupport>
<EventSourceSupport>false</EventSourceSupport>
<UseSystemResourceKeys>true</UseSystemResourceKeys>
```

Target binary size: ~8MB. Cold-start budget: <100ms on Apple Silicon, <150ms on Windows x64.

---

## 5. Release and Update Pipeline

### 5.1 Trigger

When `gui-cs/Terminal.Gui` cuts a release (`v2.X.Y` tag pushed), a `repository_dispatch` event is sent to `gui-cs/clet`. The `release-on-tg-release.yml` workflow consumes it.

```yaml
# gui-cs/Terminal.Gui/.github/workflows/notify-clet-on-release.yml (NEW; only TG-side clet artifact)
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

### 5.2 Build matrix

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

### 5.3 Smoke test gate (P0; release fails closed)

Before any publish step, every built binary runs a smoke matrix:

1. `clet --version` returns the TG version.
2. `clet list --json` validates against the schema.
3. For each input clet: spawn with `--initial <stub> --json --timeout 1s`, send a fake "Enter" via stdin, verify exit 0 and JSON shape.
4. For `md`: spawn against a fixture markdown file, send "q", verify exit 0 and JSON shape.
5. Cancellation: spawn with `--timeout 100ms`, verify exit 130.

Any failure halts the publish workflow. The maintainer is paged via the release issue's auto-comment.

### 5.4 Publish steps

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
- `dotnet pack` the `Clet.Tool` project (which references `Clet` and packages the build output as a global tool).
- `dotnet nuget push` to nuget.org with the API key.

### 5.5 Failure handling

If any publish step fails:
- The workflow opens an issue in `gui-cs/clet` titled `Release v<TG_VERSION> failed at <step>`.
- Tags the maintainer team.
- Already-published channels are noted; rollback is manual (we don't auto-revert).
- The smoke-test step ensures broken binaries never hit a channel; failures here are most often manifest/signing problems, not runtime regressions.

### 5.6 Version 1:1 with TG

The `Clet.csproj` `Version` property is set at build time from the dispatch payload's `tg_version`. There is no version negotiation, no compatibility matrix, no "clet 1.5 supports TG 2.3+." The pair is locked.

---

## 6. Testing Plan

The user asked for thorough; this section is detailed accordingly. Eight test layers, each with a clear "what does this catch" purpose. All tests live in `gui-cs/clet/tests/` (Terminal.Gui core's test suite is unaffected by clet).

### 6.1 Unit tests (`tests/Clet.UnitTests`)

**What this catches:** Logic bugs in the registry, options, parsing, JSON serialization, exit code mapping.

**Coverage target:** 90%+ for `src/Clet/Abstractions`, `src/Clet/Registry`, `src/Clet/Json`.

**Cases:**
- `CletRegistry`:
  - Register/resolve by primary alias, by secondary alias.
  - Conflict on duplicate alias raises `InvalidOperationException`.
  - `All` is stable in iteration order.
- `CletRunOptions`:
  - Default values.
  - `Fullscreen` flag round-trips correctly.
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

### 6.2 Integration tests (`tests/Clet.IntegrationTests`)

**What this catches:** TG hosting bugs (init/teardown, cancellation, rendering) that unit tests can't see because they don't run a real run loop.

**Cases:**
- Run each clet end-to-end against a scripted input/output stream.
- Cancellation token cancels mid-run; verify final result and clean shutdown.
- Timeout fires; verify `Status = Cancelled` and exit 130.
- Theme override per invocation; verify View's effective scheme.
- Inline vs alt-screen mode; verify driver state transitions.

**Test harness:** `IApplication` instance per test, scripted input stream (a `TextReader` substitute for keyboard events), captured render output (snapshot to string).

### 6.3 Process/smoke tests (`tests/Clet.SmokeTests`)

**What this catches:** Bugs that only appear when `clet` runs as a real process (argument parsing, stdout/stderr wiring, exit codes, signal handling).

**Cases:** Identical to §5.3 release-pipeline smoke tests (every clet boots, returns valid JSON, exits with correct code). Run on every PR to `gui-cs/clet`, every TG-triggered release build, and nightly against the latest TG develop branch.

**Tooling:** `Process.Start`, capture stdout/stderr, assert exit code.

### 6.4 JSON contract tests (`tests/Clet.ContractTests`)

**What this catches:** Schema drift; promises to AI agent consumers being broken silently.

**Cases:**
- Every line emitted by every clet across the full input matrix validates against `SchemaV1`.
- Schema additions in v1.x are confirmed additive only (a v1.0 consumer can still parse v1.x output).
- `clet list --json` validates against its own list schema.

**Tooling:** `JsonSchema.Net` for validation. The schema file is the source of truth; tests read it, not a copy.

### 6.5 Cross-terminal manual matrix

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

### 6.6 AOT publish tests

**What this catches:** Trim warnings, runtime AOT failures, regressions in AOT-compatibility of TG core. With no separate AOT audit (the original §3 entry was dropped because TG core already tracks AOT work), these tests are the primary discovery mechanism for AOT issues; failures here are filed as issues against `gui-cs/Terminal.Gui` with a minimal repro.

**Cases:**
- CI publishes the AOT binary on every PR to `gui-cs/clet` and on the nightly TG-develop run.
- Zero trim warnings tolerated; warnings fail the build.
- Smoke tests (§6.3) run against the AOT binary, not just the JIT'd debug build.
- AOT failures discovered during `gui-cs/clet` builds are filed against `gui-cs/Terminal.Gui` with a minimal repro.

### 6.7 Performance tests (`tests/Clet.PerfTests`)

**What this catches:** Cold-start regressions that erode the "feels instant" property AI agents need.

**Cases:**
- `clet --version` cold start: <100ms macOS arm64, <150ms Windows x64.
- `clet list --json` cold start: same budgets.
- Tracked over time; regression alerts at +25% on a 7-day rolling baseline.

### 6.8 Markdown rendering golden-file tests

**What this catches:** `Markdown` View regressions visible to clet users specifically (where TG's own tests might pass but the rendered output for `clet md` looks wrong).

**Corpus:** CommonMark spec examples + GFM table samples + a curated "real READMEs" set (TG's own README, .NET runtime README, a few popular OSS projects).

**Method:** Render each to a fixed terminal size, capture as ANSI text, diff against golden. Updates require explicit reviewer approval.

### 6.9 Release pipeline dry-run tests

**What this catches:** Workflow regressions that would otherwise only surface during a real TG release (when the cost is high).

**Cases:**
- Weekly cron: simulate a `repository_dispatch` with a fake version. Build, smoke-test, generate manifests, but stop short of publish.
- Verify all template files render correctly, all artifact uploads succeed, all checksums match.

---

## 7. Milestones

| Milestone | Date target | Exit criteria |
|-----------|-------------|---------------|
| **v0.1 alpha** | T+4 weeks | `gui-cs/clet` repo bootstrapped; abstractions, registry, JSON in place; `text` and `confirm` clets working in unit + integration tests. |
| **v0.3 alpha** | T+8 weeks | All 14 input clets functional. JSON schema drafted. AOT publish (§6.6) green on `gui-cs/clet` CI. |
| **v0.5 beta** | T+14 weeks | Naming locked; JSON schema locked; exit-code table locked; inline rendering proven on the four-terminal matrix; v1.0 input and viewer lists locked; Markdown View integration verified end-to-end including link safety; threat model published; Homebrew tap and WinGet manifest in working draft form; the gui-cs/clet release workflow proven against a real TG release cut. |
| **v0.9 RC** | T+18 weeks | All §6 test layers passing in CI. One real release cycle exercised end-to-end. |
| **v1.0 GA** | T+20 weeks (matches TG v2 GA) | Brew, WinGet, NuGet channels live. Documentation published. Issue templates for clet bugs in place. |

---

## 8. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------:|-------:|------------|
| AOT issue surfaces during `gui-cs/clet` build or smoke test | Medium | Medium | §6.6 catches before publish; file against TG core; if blocking on a release, fall back to self-contained single-file (~30MB) and document. |
| `FileDialog` typed-result refactor (§3.2) breaks downstream callers | Low | Medium | Coordinate with TG core team; flag as breaking in release notes; fix in-tree callers as part of the PR. |
| Native installer pipeline (Homebrew/WinGet) ops cost | Medium | Medium | §5.3 smoke gate + §6.9 dry-runs catch most issues pre-publish; explicit on-call rotation for release weeks. |
| Markdown View quality regression vs `glow` | Low | Medium | §6.8 golden-file corpus; quarterly comparison run. |
| Cancellation tokens in TG core have unforeseen complexity | Medium | Medium | §3.1 spike at v0.1; if hard, ship clet with polling-based cancellation as fallback. |
| Naming concerns about "clet" surfacing in support channels | Low | Low | Acknowledge in docs; outlast. |

---

## 9. Open Questions

1. **Theme location on disk.** The PR/FAQ says `ConfigurationManager` themes apply, but the search path for a system-wide theme isn't documented. Confirm with the TG core team before v0.5.
2. **WinGet ARM64.** The `win-arm64` matrix entry assumes WinGet ARM64 publishing is supported; verify, and fall back to x64-only if not.
3. **Telemetry.** The PR/FAQ mentions an opt-in usage ping. Spec deliberately does not include this in v1.0 scope; revisit at v1.1 with a privacy review.
4. **Homebrew tap repo name.** `gui-cs/homebrew-tap` is assumed; confirm it exists or create.
5. **Code signing certs.** Apple Developer ID and Authenticode certs are operational dependencies; confirm ownership/renewal process before v0.9.
6. **`range` clet result type.** `(low, high)` tuple, named record, or two separate fields? Decide before locking the JSON schema at v0.5.
7. **`md` content source.** File argument (`clet md README.md`), stdin (`cat README.md | clet md -`), or both? Both is implied; confirm CLI shape.
8. **PR/FAQ update.** Issue #5155's PR/FAQ still references `Terminal.Gui.Clets` as a separate assembly (Tig's quote, the strategic FAQ). Update issue body to match this spec before v0.5.

---

## 10. Implementation Order

A suggested sequence (linear, not parallelizable until v0.3 except where noted):

1. TG: §3.1 cancellation token PR against `develop`. §3.2 `FileDialog` typed-result refactor (coordinate with TG team; breaking change against any v2 caller of the old `int` shape).
2. `gui-cs/clet` repo bootstrapped: solution layout, abstractions, registry, JSON, source generator.
3. First two clets (`text`, `confirm`) end-to-end in unit + integration tests.
4. CLI host: Program.Main, System.CommandLine, alias dispatch, output formatter.
5. Smoke test harness (§6.3) running on a single RID.
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
