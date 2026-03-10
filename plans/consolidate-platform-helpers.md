# Plan: Consolidate Platform-Specific Code into WindowsHelpers / UnixHelpers

## Problem Statement

Platform-specific code is scattered across the driver tree. Some Unix-only code sits
at the Drivers root (`SuspendHelper.cs`), some Windows-only code lives in
`DotNetDriver/` (`NetWinVTConsole.cs`), and the "platform-agnostic" `AnsiDriver/`
files embed P/Invoke declarations for both platforms inline
(`AnsiTerminalHelper.cs`, `Driver.cs`).

The `WindowsHelpers/` and `UnixHelpers/` directories already exist and hold _some_
of the right code, but the consolidation is incomplete.

## Goals

1. **Every P/Invoke and every platform-specific helper** lives in the matching
   `*Helpers/` directory.
2. **`AnsiDriver/` files contain zero P/Invoke** — they call into the helpers.
3. **`DotNetDriver/` has no Windows-only files** — `NetWinVTConsole` moves (or is
   eliminated if redundant with `WindowsVTInputHelper` + `WindowsVTOutputHelper`).
4. **Drivers root has no platform-only files** — `SuspendHelper` moves to
   `UnixHelpers/`.
5. **`Driver.cs`** `IsAttachedToTerminal` P/Invokes move to helpers; the static
   method dispatches via `PlatformDetection`.

## Current State — Files to Move / Refactor

### Pure platform files in the wrong directory

| File | Current Location | Target | Action |
|------|-----------------|--------|--------|
| `SuspendHelper.cs` | `Drivers/` (root) | `Drivers/UnixHelpers/` | **Move** |
| `NetWinVTConsole.cs` | `Drivers/DotNetDriver/` | `Drivers/WindowsHelpers/` or **delete** | See note 1 |

> **Note 1 — `NetWinVTConsole` vs existing helpers:**
> `NetWinVTConsole` enables `ENABLE_VIRTUAL_TERMINAL_INPUT` +
> `ENABLE_VIRTUAL_TERMINAL_PROCESSING` and restores modes on cleanup.
> `WindowsVTInputHelper` already does the input half; `WindowsVTOutputHelper`
> already does the output half. `NetWinVTConsole` duplicates both in one class.
> **Recommendation:** Refactor `NetInput` to use `WindowsVTInputHelper` +
> `WindowsVTOutputHelper` (which already support `TryEnable` / `Dispose`), then
> **delete** `NetWinVTConsole.cs`. If there are subtle differences (e.g. flush
> behaviour), fold them into the existing helpers.

### Mixed files with inline P/Invoke

| File | Platform Code | Refactoring |
|------|--------------|-------------|
| `AnsiTerminalHelper.cs` | libc `tcdrain`/`fsync` + kernel32 `FlushFileBuffers`/`GetStdHandle` | Extract `FlushUnix` body → `UnixIOHelper.FlushStdout()`. Extract `FlushWindows` body → `WindowsVTOutputHelper.FlushStdout()`. `AnsiTerminalHelper.FlushNative` becomes a two-line dispatcher with no P/Invoke. |
| `Driver.cs` | libc `isatty` + kernel32 `GetStdHandle`/`GetConsoleMode` | Extract the Windows branch → `WindowsHelpers/WindowsConsoleHelper.IsAttachedToTerminal()`. Extract the Unix branch → `UnixHelpers/UnixIOHelper.IsTerminal(fd)` (or expose existing `isatty` wrapper). `Driver.IsAttachedToTerminal` becomes a dispatcher. |

### Mixed files with platform _branching_ (no P/Invoke — lower priority)

These files use `PlatformDetection` to select which helper to use. They are
_correctly structured_ — the platform code is in the helpers, the branching is in
the consumer. **No file moves needed**, but document the pattern as the convention.

| File | What It Does |
|------|-------------|
| `AnsiInput.cs` | Branches on `PlatformDetection.IsWindows()` → `WindowsVTInputHelper`, `.IsUnixLike()` → `UnixRawModeHelper`/`UnixIOHelper` |
| `AnsiOutput.cs` | Branches on `PlatformDetection.IsWindows()` → `WindowsVTOutputHelper`, `.IsUnixLike()` → `UnixIOHelper` |
| `AnsiComponentFactory.cs` | Branches on `RuntimeInformation.IsOSPlatform(Windows)` for `CreateNativeSizeQuery` |
| `DriverImpl.cs` | Branches on platform for `CreateClipboard` (uses helpers from both dirs) |
| `DriverRegistry.cs` | Platform-based default driver selection |
| `NetInput.cs` | Windows branch creates `NetWinVTConsole` (will change per Note 1) |
| `NetOutput.cs` | `_isWinPlatform` flag for cursor positioning; `SuspendHelper` call |

## Target Directory Structure

```
Drivers/
├── AnsiDriver/
│   ├── AnsiComponentFactory.cs    (no P/Invoke — dispatches to helpers)
│   ├── AnsiInput.cs               (no P/Invoke — uses WindowsVTInputHelper / UnixIOHelper)
│   ├── AnsiInputProcessor.cs      (pure ANSI — unchanged)
│   ├── AnsiOutput.cs              (no P/Invoke — uses WindowsVTOutputHelper / UnixIOHelper)
│   ├── AnsiPlatform.cs            (enum — unchanged)
│   ├── AnsiSizeMonitor.cs         (pure ANSI — unchanged)
│   ├── AnsiTerminalHelper.cs      (dispatcher only — no P/Invoke)
│   └── FakeClipboard.cs           (test stub — unchanged)
│
├── DotNetDriver/
│   ├── INetInput.cs               (unchanged)
│   ├── NetComponentFactory.cs     (unchanged)
│   ├── NetInput.cs                (refactored: uses WindowsVTInputHelper + WindowsVTOutputHelper)
│   ├── NetInputProcessor.cs       (unchanged)
│   ├── NetKeyConverter.cs         (unchanged)
│   └── NetOutput.cs               (unchanged — branching only, no P/Invoke)
│   # NetWinVTConsole.cs DELETED (consolidated into existing helpers)
│
├── WindowsDriver/                 (unchanged — self-contained Windows driver)
│   └── (all 11 files stay)
│
├── WindowsHelpers/
│   ├── WindowsVTInputHelper.cs    (already here)
│   ├── WindowsVTOutputHelper.cs   (already here + gains FlushStdout)
│   └── WindowsConsoleHelper.cs    (NEW — IsAttachedToTerminal extracted from Driver.cs)
│
├── UnixHelpers/
│   ├── UnixClipboard.cs           (already here)
│   ├── UnixIOHelper.cs            (already here + gains FlushStdout, IsTerminal)
│   ├── UnixRawModeHelper.cs       (already here)
│   ├── UnixTerminalHelper.cs      (already here)
│   └── SuspendHelper.cs           (MOVED from Drivers root)
│
├── (root — platform-agnostic only)
│   ├── ComponentFactoryImpl.cs
│   ├── Cursor.cs / CursorStyle.cs
│   ├── Driver.cs                  (no P/Invoke — dispatches to helpers)
│   ├── DriverImpl.cs
│   ├── DriverRegistry.cs
│   ├── IComponentFactory.cs / IDriver.cs / ISizeMonitor.cs
│   ├── PlatformDetection.cs
│   ├── SizeDetectionMode.cs
│   └── SizeMonitorImpl.cs
│
├── AnsiHandling/                  (unchanged — pure ANSI parsing)
├── Input/                         (unchanged)
├── Keyboard/                      (VK.cs stays — consumed cross-platform)
├── Mouse/                         (unchanged)
├── Output/                        (unchanged)
└── TerminalEnvironment/           (unchanged)
```

## Implementation Steps

### Phase 1 — Simple moves (no code changes beyond namespace update)

- [ ] **Move `SuspendHelper.cs`** from `Drivers/` to `Drivers/UnixHelpers/`
  - Update callers (`NetOutput.cs`, `AnsiOutput.cs` → `UnixTerminalHelper.Suspend`)
    to use the new location (namespace stays `Terminal.Gui.Drivers` so callers
    won't change unless we adopt sub-namespaces).

### Phase 2 — Extract P/Invoke from `AnsiTerminalHelper.cs`

- [ ] **Add `UnixIOHelper.FlushStdout()`** — move the `tcdrain`/`fsync` logic from
  `AnsiTerminalHelper.FlushUnix()` into `UnixIOHelper`. The P/Invoke declarations
  for `tcdrain` and `fsync` move too (or reuse existing ones if `UnixIOHelper`
  already imports them).
- [ ] **Add `WindowsVTOutputHelper.FlushStdout()`** — move the
  `GetStdHandle`/`FlushFileBuffers` logic from `AnsiTerminalHelper.FlushWindows()`.
- [ ] **Simplify `AnsiTerminalHelper.FlushNative()`** — becomes:
  ```csharp
  switch (platform)
  {
      case AnsiPlatform.UnixRaw:    UnixIOHelper.FlushStdout (); break;
      case AnsiPlatform.WindowsVT:  WindowsVTOutputHelper.FlushStdout (); break;
  }
  ```
  No P/Invoke declarations remain in `AnsiTerminalHelper.cs`.

### Phase 3 — Extract P/Invoke from `Driver.cs`

- [ ] **Create `WindowsHelpers/WindowsConsoleHelper.cs`** with:
  ```csharp
  internal static bool IsAttachedToTerminal (out bool input, out bool output)
  ```
  Move the `GetStdHandle`/`GetConsoleMode` P/Invoke + logic from `Driver.cs`.
- [ ] **Add `UnixIOHelper.IsTerminal(int fd)`** — expose the libc `isatty` call.
  `UnixIOHelper` may already import `isatty`; if not, add it.
- [ ] **Simplify `Driver.IsAttachedToTerminal()`** — becomes:
  ```csharp
  if (PlatformDetection.IsWindows ())
      return WindowsConsoleHelper.IsAttachedToTerminal (out inputAttached, out outputAttached);
  inputAttached = UnixIOHelper.IsTerminal (0);
  outputAttached = UnixIOHelper.IsTerminal (1);
  return inputAttached && outputAttached;
  ```

### Phase 4 — Eliminate `NetWinVTConsole.cs`

- [ ] **Audit `NetWinVTConsole`** vs `WindowsVTInputHelper` + `WindowsVTOutputHelper`
  to confirm functional equivalence (same console mode flags, same restore logic).
- [ ] **Refactor `NetInput.cs`** to use `WindowsVTInputHelper` +
  `WindowsVTOutputHelper` instead of `NetWinVTConsole`.
- [ ] **Delete `DotNetDriver/NetWinVTConsole.cs`**.

### Phase 5 — Verify

- [ ] `dotnet build --no-restore` — zero new warnings.
- [ ] `dotnet test --project Tests/UnitTestsParallelizable --no-build` — all pass.
- [ ] `dotnet test --project Tests/UnitTests --no-build` — all pass.
- [ ] Grep for `DllImport` / `LibraryImport` in `AnsiDriver/` and `DotNetDriver/`
  and `Drivers/*.cs` (root) — expect zero hits (all in `*Helpers/`, `WindowsDriver/`,
  or `Keyboard/VK.cs`).

## Conventions Established

After this refactoring, the rule is:

> **P/Invoke and OS-specific API calls live exclusively in `WindowsDriver/`,
> `WindowsHelpers/`, or `UnixHelpers/`.** Everything else uses `PlatformDetection`
> to dispatch into those directories. No `DllImport` appears in `AnsiDriver/`,
> `DotNetDriver/`, or the Drivers root.

## Out of Scope

- **`WindowsDriver/`** — Already self-contained. No changes needed.
- **`Keyboard/VK.cs`** — Windows virtual key codes consumed cross-platform. Stays.
- **`TerminalEnvironment/`** — Reads env vars (`TERM`, etc.) — no P/Invoke.
- **Sub-namespace changes** — All driver code currently uses `Terminal.Gui.Drivers`.
  Introducing sub-namespaces (e.g. `Terminal.Gui.Drivers.WindowsHelpers`) would be
  a larger change and is not proposed here.
