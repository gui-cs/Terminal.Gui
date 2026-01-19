# macOS Test Hang Analysis Plan

## Overview

**Issue**: Intermittent test failure on `macos-latest` runner in GitHub Actions for `unit-tests.yml`
**Dump File**: `C:\Users\Tig\Downloads\dotnet_3981_20260115T162211_hangdump.dmp`
**Failure Source**: [GitHub Actions Run #21038205501](https://github.com/gui-cs/Terminal.Gui/actions/runs/21038205501/job/60492337248?pr=4571)
**Test Suite**: Parallel Unit Tests (macos-latest)
**Failure Type**: Hang dump (test exceeded `--blame-hang-timeout 120s`)

### Dump File Context

The filename `dotnet_3981_20260115T162211_hangdump.dmp` tells us:
- **Process**: dotnet test host
- **PID**: 3981
- **Timestamp**: 2026-01-15 at 16:22:11 UTC
- **Type**: `hangdump` - created because tests exceeded the 120-second hang timeout

---

## Phase 1: Environment Setup & Tool Preparation

### 1.1 Required Tools

| Tool | Purpose | Installation |
|------|---------|--------------|
| **WinDbg** (Windows) | Primary dump analysis | Microsoft Store or [Windows SDK](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/) |
| **dotnet-dump** | Cross-platform managed analysis | `dotnet tool install -g dotnet-dump` |
| **dotnet-sos** | SOS debugging extension | `dotnet tool install -g dotnet-sos` then `dotnet sos install` |
| **Visual Studio** | Alternative GUI-based analysis | VS 2022 with "Native development" workload |

### 1.2 Symbol Configuration

```cmd
:: Set symbol path for Microsoft symbols + local symbols
set _NT_SYMBOL_PATH=srv*C:\Symbols*https://msdl.microsoft.com/download/symbols;D:\s\inspect_TG_dump\Terminal.Gui\bin\Debug\net8.0

:: Or in WinDbg:
.sympath srv*C:\Symbols*https://msdl.microsoft.com/download/symbols;D:\s\inspect_TG_dump\Terminal.Gui\bin\Debug\net8.0
.reload
```

### 1.3 Load the Dump

**Option A: dotnet-dump (Recommended for managed code)**
```bash
dotnet dump analyze "C:\Users\Tig\Downloads\dotnet_3981_20260115T162211_hangdump.dmp"
```

**Option B: WinDbg**
```
File -> Open Crash Dump -> Select the .dmp file
```

**Option C: Visual Studio**
```
File -> Open -> File -> Select .dmp file -> "Debug with Managed Only"
```

---

## Phase 2: Initial Dump Analysis

### 2.1 Basic Dump Information

Run these commands in `dotnet-dump` or WinDbg with SOS loaded:

```
# Get CLR version and runtime info
clrversion

# List all threads
threads

# Get exception info (if any)
pe

# Check for deadlocks
syncblk
```

### 2.2 Thread Analysis (Critical for Hangs)

```
# List all managed threads with their states
clrthreads

# Show thread pool statistics
threadpool

# For each interesting thread, show stack:
setthread <thread-id>
clrstack

# Show full call stack with all frames
clrstack -all

# Show locals and parameters
clrstack -a
```

### 2.3 Identify the Hanging Thread(s)

Look for threads that are:
1. **Waiting on locks** - `syncblk` will show lock contention
2. **Blocked in I/O** - waiting on console/terminal operations
3. **Spinning** - high CPU threads stuck in loops
4. **Waiting on ManualResetEvent/AutoResetEvent** - synchronization primitives

```
# Check synchronization blocks for deadlocks
syncblk

# Dump all lock objects
dumpheap -type ReaderWriterLockSlim
dumpheap -type SemaphoreSlim
dumpheap -type Monitor
dumpheap -type ManualResetEvent
dumpheap -type AutoResetEvent
```

---

## Phase 3: Terminal.Gui-Specific Investigation

### 3.1 Key Classes to Investigate

Based on the codebase structure, focus on these areas:

| Component | Path | Why |
|-----------|------|-----|
| `ApplicationImpl` | `Terminal.Gui/App/ApplicationImpl.cs` | Main application loop, thread management |
| `MainLoopCoordinator` | `Terminal.Gui/App/MainLoop/MainLoopCoordinator.cs` | Coordinates main loop execution |
| `TimedEvents` | `Terminal.Gui/App/Timeout/TimedEvents.cs` | Timer/invoke scheduling (known issue area) |
| `NetInput` | `Terminal.Gui/Drivers/DotNetDriver/NetInput.cs` | Console input handling |
| `InputImpl` | `Terminal.Gui/Drivers/Input/InputImpl.cs` | Input processing |

### 3.2 Look for Known Patterns

**A. Application.Invoke Deadlock**
```
# Search for TimedEvents on the heap
dumpheap -type TimedEvents
dumpobj <address>

# Check the _timeouts collection
dumpobj <_timeouts_address>
```

**B. Main Loop Starvation**
```
# Find ApplicationMainLoop instances
dumpheap -type ApplicationMainLoop

# Check iteration state
dumpobj <address>
```

**C. Input Driver Blocking**
```
# Look for threads waiting on Console operations
clrthreads

# Search for NetInput or InputImpl
dumpheap -type NetInput
dumpheap -type InputImpl
```

### 3.3 Thread State Checklist

For each thread with a managed stack, determine:

- [ ] Is it the main UI thread? (Check for `Application.Run` in stack)
- [ ] Is it a test worker thread? (Check for xUnit in stack)
- [ ] Is it waiting on a lock? (SyncBlock analysis)
- [ ] Is it waiting on I/O? (Console.Read*, Stream operations)
- [ ] Is it in `TimedEvents.RunTimers`?
- [ ] Is it calling `Application.Invoke`?

---

## Phase 4: Specific Analysis Commands

### 4.1 Complete Thread Dump Script

Run this sequence in `dotnet-dump`:

```
# 1. Basic info
clrversion
threads
threadpool

# 2. Check for deadlocks immediately
syncblk

# 3. Dump all thread stacks
clrthreads
~* e !clrstack

# 4. Look for Terminal.Gui specific objects
dumpheap -stat -type Terminal.Gui

# 5. Find Application instances
dumpheap -type ApplicationImpl
dumpheap -type MainLoopCoordinator
dumpheap -type TimedEvents

# 6. Check for waiting threads
dumpheap -type ManualResetEventSlim
dumpheap -type SemaphoreSlim
```

### 4.2 WinDbg-Specific Commands

```
# Load SOS
.loadby sos coreclr
# or for .NET 5+:
.load C:\Users\<user>\.dotnet\sos\sos.dll

# Parallel stacks view
!pstacks

# Unique stacks (helpful for parallel tests)
!uniqstack

# GC roots for an object
!gcroot <address>
```

---

## Phase 5: Hypothesis Investigation

### 5.1 Hypothesis 1: Timer Resolution Issue (Similar to InvokeLeakTest)

The previous `InvokeLeakTest` failure was caused by `DateTime.UtcNow` resolution issues. Check if `TimedEvents` is involved:

```
# Find TimedEvents instances
dumpheap -type TimedEvents

# Dump the object
dumpobj <address>

# Check _timeouts collection
dumpobj <_timeouts_field_address>

# If there are pending timeouts, list them
dumparray <sorted_list_items_address>
```

**What to look for:**
- Large number of pending timeouts
- Timeouts with timestamps in the future that should have fired
- Multiple threads waiting to add timeouts

### 5.2 Hypothesis 2: Console Input Deadlock on macOS

macOS terminal handling differs from Windows/Linux. Check if threads are blocked on console operations:

```
# Look for threads in native Console operations
~* k

# Check for NetInput or terminal driver threads
clrthreads
# Look for threads with Console.Read* in their stacks
```

**What to look for:**
- Threads blocked in `System.Console` methods
- Threads waiting in `Terminal.Gui.Drivers.DotNetDriver.NetInput`
- Native frames showing terminal/tty operations

### 5.3 Hypothesis 3: Test Parallelism Race Condition

With `MaxParallelThreads=4` on macOS (capped in workflow), parallel test execution may cause race conditions:

```
# Count xUnit worker threads
clrthreads
# Look for multiple threads in test methods

# Check for contention
syncblk

# Look for multiple Application instances (should only be one)
dumpheap -type ApplicationImpl
```

**What to look for:**
- Multiple test methods executing simultaneously
- Shared state corruption
- Application not properly initialized/shutdown between tests

### 5.4 Hypothesis 4: Signal Handling on macOS

macOS uses different signal handling than Linux. A signal might interrupt operations:

```
# Check for exception records
pe

# Look at native stacks for signal frames
~* k
```

---

## Phase 6: Data Collection Checklist

Before drawing conclusions, collect:

- [ ] **All thread stacks** - both managed and native
- [ ] **SyncBlock table** - for lock analysis
- [ ] **ThreadPool statistics** - for resource exhaustion
- [ ] **Heap statistics** - for memory state
- [ ] **Exception records** - for any unhandled exceptions
- [ ] **TimedEvents state** - pending timeouts count and values
- [ ] **Application state** - initialized, running, shutdown status

---

## Phase 7: Documentation Template

When you find the root cause, document using this template:

```markdown
## Root Cause Analysis

### Summary
[One paragraph describing what caused the hang]

### Thread Analysis
- Main thread state: [state]
- Blocked threads: [count]
- Deadlock detected: [yes/no]

### Key Findings
1. [Finding 1]
2. [Finding 2]
3. [Finding 3]

### Stack Traces
[Relevant stack traces]

### Affected Code
- File: [path]
- Line: [number]
- Issue: [description]

### Recommended Fix
[Description of the fix]

### Reproduction
[Steps to reproduce, if known]
```

---

## Phase 8: Next Steps After Analysis

Based on findings, potential actions:

1. **If Timer/Invoke issue**: Apply similar fix as InvokeLeakTest (Stopwatch instead of DateTime.UtcNow)

2. **If Console/Input deadlock**:
   - Review macOS-specific terminal handling
   - Consider adding timeout to input operations
   - Check for blocking calls in input driver

3. **If Test Parallelism issue**:
   - Review test isolation
   - Consider reducing parallel threads further on macOS
   - Add proper test cleanup/setup

4. **If Signal handling issue**:
   - Review signal handlers in .NET runtime on macOS
   - Consider platform-specific workarounds

---

## Quick Reference: Common dotnet-dump Commands

```bash
# Start analysis
dotnet dump analyze <dumpfile>

# Thread commands
threads                  # List threads
clrthreads              # List CLR threads with details
threadpool              # Thread pool statistics
setthread <id>          # Switch to thread

# Stack commands
clrstack                # Managed stack
clrstack -a             # With arguments
clrstack -all           # All frames
dumpstack               # Full stack

# Object commands
dumpheap -stat          # Heap statistics
dumpheap -type <name>   # Find objects by type
dumpobj <addr>          # Dump object
dumparray <addr>        # Dump array

# Lock/sync commands
syncblk                 # Sync block table

# Memory commands
gcroot <addr>           # Find roots
dumpmt <addr>           # Dump method table

# Exit
exit
```

---

## Appendix A: Workflow Configuration Context

From `.github/workflows/unit-tests.yml`:

```yaml
# macOS-specific settings
if [ "${{ runner.os }}" == "macOS" ]; then
  MAX_THREADS=4               # Capped at 4 threads
  HANG_TIMEOUT="120s"         # 2-minute timeout
fi

--blame-hang                  # Enable hang detection
--blame-hang-timeout 120s     # Trigger dump after 120s
--blame-crash-collect-always  # Always collect crash dumps
```

---

## Appendix B: Related Past Issues

### InvokeLeakTest (FIXED)
- **Issue**: Test failing due to `DateTime.UtcNow` resolution
- **Cause**: Low timer resolution causing race conditions in `TimedEvents`
- **Fix**: Replaced with `Stopwatch.GetTimestamp()`
- **Relevance**: Similar timing issues may affect other code paths

See: `Tests/StressTests/InvokeLeakTest_Analysis.md`

---

## Appendix C: Analysis Progress Log

### Session 1: 2026-01-15

#### Environment
- **Analysis Machine**: Windows (attempting to analyze macOS ARM64 dump)
- **Dump Target**: macOS ARM64, .NET 10.0.2

#### Key Finding: Cross-Platform DAC Limitation

**Problem**: The dump is from macOS ARM64, but we're analyzing on Windows. The Data Access Component (DAC) required for managed debugging (`mscordaccore.dylib` for macOS) cannot execute on Windows.

**What Works**:
- Basic dump loading ✓
- Native thread enumeration ✓
- Module list ✓
- Runtime identification ✓

**What Doesn't Work**:
- `clrthreads` - requires DAC
- `syncblk` - requires DAC
- `clrstack` - requires DAC
- `dumpheap` - requires DAC
- Any managed code inspection

#### Runtime Information Extracted

```
Target OS: OSX Architecture: Arm64
Runtime: .NET Core runtime 10.0.225.61305
Runtime module: /Users/runner/.dotnet/shared/Microsoft.NETCore.App/10.0.2/libcoreclr.dylib
DAC index: 89e8fa3097673705b839e0494c040931
```

#### Thread Count

```
Total threads: 46

Thread IDs:
*0 0x49A5 (18853)   <- Current thread at dump time
 1 0x49A8 (18856)
 2 0x49AB (18859)
... (46 total threads)
```

#### Modules Loaded (Partial)

Key modules from the dump:
- `/Users/runner/.dotnet/dotnet` (main host)
- `/Users/runner/.dotnet/shared/Microsoft.NETCore.App/10.0.2/libcoreclr.dylib` (runtime)
- Various test assemblies

#### Symbol Download Status

Symbols downloaded successfully for:
- dotnet host
- libhostfxr.dylib
- libhostpolicy.dylib
- libcoreclr.dylib
- libSystem.Native.dylib
- Various framework assemblies

**DAC download failed** - 0 bytes received from symbol server

### Recommended Next Steps

1. **Option A: Analyze on macOS**
   - Copy dump to a macOS machine
   - Install `dotnet-dump` on macOS
   - Run analysis there (DAC will work natively)

2. **Option B: Use Visual Studio with Remote Debugging Symbols**
   - Try Visual Studio 2022's dump analysis
   - May have better cross-platform support

3. **Option C: Extract Native Information**
   - Use LLDB on macOS to analyze native stacks
   - Cross-reference with managed code knowledge

4. **Option D: Request Additional Artifacts from CI**
   - Modify workflow to also capture:
     - `dotnet-trace` output
     - Text-based stack dumps
     - Process memory maps

### Detailed Thread List (from dump)

46 total threads at time of hang:

```
*0 0x49A5 (18853)   <- Current/main thread
 1 0x49A8 (18856)
 2 0x49AB (18859)
 3 0x49AC (18860)
 4 0x49AE (18862)
 5 0x49AF (18863)
 6 0x49B0 (18864)
 7 0x49B1 (18865)
 8 0x49B2 (18866)
 9 0x49B3 (18867)
10 0x49B4 (18868)
11 0x49B5 (18869)
12 0x49B6 (18870)
13 0x49B7 (18871)
14 0x49B8 (18872)
15 0x49B9 (18873)
16 0x49BA (18874)
17 0x49BB (18875)
18 0x49BC (18876)
19 0x49BD (18877)
20 0x49BE (18878)
21 0x49BF (18879)
22 0x49C0 (18880)
23 0x49C1 (18881)
24 0x49C2 (18882)
25 0x49C3 (18883)
26 0x49C4 (18884)
27 0x49C5 (18885)
28 0x49C6 (18886)
29 0x49C7 (18887)
30 0x49C8 (18888)
31 0x49C9 (18889)
32 0x49CA (18890)
33 0x49CB (18891)
34 0x49CC (18892)
35 0x49CD (18893)
36 0x49CE (18894)
37 0x49CF (18895)
38 0x49D0 (18896)
39 0x49D1 (18897)
40 0x49D2 (18898)
41 0x49D3 (18899)
42 0x49D4 (18900)
43 0x49D5 (18901)
44 0x49D6 (18902)
45 0x49D7 (18903)
```

### Thread Register State (Thread 0)

```
cpsr = 0x80001000
x0 = 0x0000000141008E18
x1 = 0x0000000000000001
x2 = 0x0000000000000000
x3 = 0x000000016D47F1A0
fp = 0x000000016D47EED0
lr = 0x000000018DA86FCC
sp = 0x000000016D47EE80
pc = 0x000000018DA86FF4
```

The `pc` (program counter) `0x000000018DA86FF4` is in `libsystem_kernel.dylib` (`0x000000018DA49000`), which suggests the main thread was in a system call when the dump was taken - likely a wait/sleep operation.

### Key Observations

1. **46 threads** is substantial - indicates multiple test worker threads plus .NET runtime threads
2. **Main thread in kernel** - blocked on a system call (wait/sleep/poll)
3. **No managed assemblies visible** in module list - they're loaded by CLR, not as native modules
4. **DAC cross-platform limitation** - Cannot inspect managed code from Windows

### Timeline Analysis (from GitHub Actions API)

```
Failed Job: Parallel Unit Tests (macos-latest)
  Job ID: 60492337248

  Timeline:
  - 16:18:04 - Job started
  - 16:18:46 - Run UnitTestsParallelizable step started
  - 16:22:11 - Hang dump captured (from filename timestamp)
  - 16:23:05 - Step failed (after ~4 minutes 19 seconds)
  - 16:25:55 - Job completed (uploading logs)

  Hang timeout: 120 seconds
  Time from test start to hang dump: ~3 minutes 25 seconds

Comparison with other platforms (same test suite):
  - windows-latest: 16:19:40 to 16:22:08 (2 min 28 sec) ✓ PASSED
  - ubuntu-latest:  16:18:33 to 16:24:24 (5 min 51 sec) ✓ PASSED
  - macos-latest:   16:18:46 to 16:23:05 (4 min 19 sec) ✗ FAILED (hang)

Interesting observation:
  - macOS Non-Parallel tests PASSED (16:18:37 to 16:19:52, ~1 min 15 sec)
  - macOS Parallel tests FAILED
  - This suggests the issue is specific to PARALLEL test execution on macOS
```

### Critical Finding: Last Tests Before Hang

**Last completed test:**
```
16:20:10.4739490Z  Passed ViewsTests.WizardTests.NextFinishButton_Shows_Finish_On_Last_Step [1 s]
```

**Hang dump triggered:**
```
16:22:11.5012000Z  Blame: Dumping 3981 - dotnet
```

**Gap analysis:** ~2 minutes between last test completion and hang dump = 120s timeout was hit

### Console Output at Hang Time

The blame output captured ANSI escape sequences being written to the terminal at hang time:

```
[?1049h[?25l[?1003h[?1015h[?1006h  <- Alternate screen, mouse tracking enabled
dotnet                             <- Text "dotnet" in title bar
unix                               <- Text "unix" in title bar
Test                               <- Text "Test" visible
┌──────────────────────────────────
│                                   <- Box drawing UI visible
└──────────────────────────────────
[createdump] Gathering state...
```

**Key observation:** The process was actively writing Terminal.Gui ANSI output when the hang occurred. This indicates:
1. A test was running that uses the console driver
2. The test got stuck in the middle of rendering UI
3. This could be a console I/O blocking issue specific to macOS

### Tests Running at Hang Time (from testhost logs)

The testhost log file (`logs.host.26-01-15_16-18-47_49652_5.txt`) ends at line 65532 with this being the **last test to start**:

```
16:20:10.330  TestExecutionRecorder.RecordStart: Starting test:
              ApplicationTests.RunnableTests.ApplicationRunnableIntegrationTests.Begin_ThrowsOnNullRunnable
```

**No completion record was written for this test!** The test started and then the log stops. This is consistent with:
- Test started at 16:20:10.330
- Hang dump triggered at 16:22:11 (121 seconds later)
- The 120s hang timeout was exceeded

### Suspect Tests

Based on the evidence, these tests are likely involved in the hang:

1. **Primary Suspect**: `ApplicationTests.RunnableTests.ApplicationRunnableIntegrationTests.Begin_ThrowsOnNullRunnable`
   - Last test to start before hang
   - No completion logged

2. **Tests running concurrently** (4 parallel threads on macOS):
   - `ViewsTests.WizardTests.NextFinishButton_Shows_Finish_On_Last_Step` (1 second test, last to pass)
   - Other Application/Runnable integration tests

### Recommended Immediate Action

**Run these tests in isolation on macOS to reproduce:**

```bash
# Test the primary suspect
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~ApplicationRunnableIntegrationTests.Begin_ThrowsOnNullRunnable" --no-build

# Test all ApplicationRunnable tests
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~ApplicationRunnableIntegrationTests" --no-build

# Test the Wizard tests (may be a race condition with the above)
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~WizardTests" --no-build
```

---

## Appendix D: Practical Recommendations

Given the cross-platform DAC limitation, here are the recommended paths forward:

### NEW: Dedicated Analysis Workflow

A new workflow has been created at `.github/workflows/analyze-hang-dump.yml` that:

1. **Automatically triggers** when unit-tests.yml fails on macOS
2. **Can be manually triggered** with a specific run ID
3. **Runs on macOS** where the DAC works natively
4. **Extracts full diagnostics**: thread stacks, deadlock info, Terminal.Gui objects
5. **Uploads results** as `hang-dump-analysis` artifact

**Manual trigger:**
```
gh workflow run analyze-hang-dump.yml -f run_id=21038205501
```

**Or via GitHub UI:**
Actions → Analyze Hang Dump → Run workflow → Enter run ID

---

### Immediate Action: Analyze on macOS

The most direct solution is to analyze the dump on a macOS machine:

```bash
# On macOS (must be ARM64 for this dump)
dotnet tool install -g dotnet-dump
dotnet dump analyze ~/Downloads/dotnet_3981_20260115T162211_hangdump.dmp

# Then run these commands:
clrthreads
syncblk
~* e clrstack
```

### RECOMMENDED: Analyze Dump On-Runner

Add a workflow step that analyzes any hang dumps **on the macOS runner itself** where the DAC works natively. This extracts managed stack traces before uploading.

**Insert this step in `.github/workflows/unit-tests.yml` between "Run UnitTestsParallelizable" and "Upload UnitTestsParallelizable Logs":**

```yaml
    - name: Analyze Hang Dumps (macOS only)
      if: always() && runner.os == 'macOS'
      shell: bash
      run: |
        # Install dotnet-dump if not present
        dotnet tool install -g dotnet-dump 2>/dev/null || true
        export PATH="$PATH:$HOME/.dotnet/tools"

        # Find any hang dump files in the logs directory
        DUMP_DIR="logs/UnitTestsParallelizable/macOS"

        for DUMP_FILE in "$DUMP_DIR"/*_hangdump.dmp; do
          if [ -f "$DUMP_FILE" ]; then
            echo "============================================"
            echo "Analyzing hang dump: $DUMP_FILE"
            echo "============================================"
            ANALYSIS_FILE="${DUMP_FILE%.dmp}_analysis.txt"

            # Basic thread and sync info
            echo "=== CLR THREADS ===" > "$ANALYSIS_FILE"
            dotnet dump analyze "$DUMP_FILE" -c "clrthreads" -c "exit" >> "$ANALYSIS_FILE" 2>&1 || true

            echo "" >> "$ANALYSIS_FILE"
            echo "=== THREAD POOL ===" >> "$ANALYSIS_FILE"
            dotnet dump analyze "$DUMP_FILE" -c "threadpool" -c "exit" >> "$ANALYSIS_FILE" 2>&1 || true

            echo "" >> "$ANALYSIS_FILE"
            echo "=== SYNC BLOCKS (DEADLOCK CHECK) ===" >> "$ANALYSIS_FILE"
            dotnet dump analyze "$DUMP_FILE" -c "syncblk" -c "exit" >> "$ANALYSIS_FILE" 2>&1 || true

            # Get all managed thread stacks
            echo "" >> "$ANALYSIS_FILE"
            echo "=== ALL MANAGED THREAD STACKS ===" >> "$ANALYSIS_FILE"

            # Extract thread IDs and get stack for each
            THREAD_IDS=$(dotnet dump analyze "$DUMP_FILE" -c "clrthreads" -c "exit" 2>/dev/null | grep -oE "^\s*[0-9]+" | tr -d ' ')
            for TID in $THREAD_IDS; do
              echo "" >> "$ANALYSIS_FILE"
              echo "--- Thread $TID ---" >> "$ANALYSIS_FILE"
              dotnet dump analyze "$DUMP_FILE" -c "setthread $TID" -c "clrstack -a" -c "exit" >> "$ANALYSIS_FILE" 2>&1 || true
            done

            # Terminal.Gui specific objects
            echo "" >> "$ANALYSIS_FILE"
            echo "=== TERMINAL.GUI HEAP OBJECTS ===" >> "$ANALYSIS_FILE"
            dotnet dump analyze "$DUMP_FILE" -c "dumpheap -stat -type Terminal.Gui" -c "exit" >> "$ANALYSIS_FILE" 2>&1 || true

            echo "" >> "$ANALYSIS_FILE"
            echo "=== APPLICATION INSTANCES ===" >> "$ANALYSIS_FILE"
            dotnet dump analyze "$DUMP_FILE" -c "dumpheap -type ApplicationImpl" -c "exit" >> "$ANALYSIS_FILE" 2>&1 || true

            echo "Analysis saved to: $ANALYSIS_FILE"
            echo ""
            # Also print summary to console for quick viewing in logs
            echo "=== QUICK SUMMARY ==="
            head -100 "$ANALYSIS_FILE"
          fi
        done
```

**What this gives you:**
- Full managed thread stacks (what we couldn't get on Windows)
- Deadlock detection via `syncblk`
- Thread pool statistics
- Terminal.Gui object heap statistics
- All saved to a `*_analysis.txt` file that uploads with artifacts
- Quick summary printed to CI logs for immediate visibility

### Simpler Alternative: Single-Command Analysis

If the above is too complex, a minimal version that still gets the key info:

```yaml
    - name: Analyze Hang Dumps (macOS only)
      if: always() && runner.os == 'macOS'
      shell: bash
      run: |
        dotnet tool install -g dotnet-dump 2>/dev/null || true
        export PATH="$PATH:$HOME/.dotnet/tools"
        for f in logs/UnitTestsParallelizable/macOS/*_hangdump.dmp; do
          [ -f "$f" ] && dotnet dump analyze "$f" \
            -c "clrthreads" \
            -c "syncblk" \
            -c "threadpool" \
            -c "dumpheap -stat -type Terminal.Gui" \
            -c "exit" > "${f%.dmp}_analysis.txt" 2>&1
        done
```

### Alternative: Enhanced CI Logging

Modify `.github/workflows/unit-tests.yml` to capture more diagnostic data before the hang:

```yaml
# Add to the macOS test step
- name: Run Unit Tests (macOS)
  env:
    DOTNET_DiagnosticPorts: "${{ runner.temp }}/diag.sock"
    COMPlus_EnableDiagnostics: 1
  run: |
    # Start dotnet-trace in background
    dotnet tool install -g dotnet-trace
    dotnet trace collect --process-id $$ --output ${{ runner.temp }}/trace.nettrace &

    # Run tests
    dotnet test ...
```

### Alternative: Add Test Instrumentation

Add logging to identify which test hangs:

```csharp
// In test setup
[SetUp]
public void Setup()
{
    Console.WriteLine($"[TEST START] {TestContext.CurrentContext.Test.FullName} at {DateTime.UtcNow:O}");
}

[TearDown]
public void TearDown()
{
    Console.WriteLine($"[TEST END] {TestContext.CurrentContext.Test.FullName} at {DateTime.UtcNow:O}");
}
```

### Alternative: Bisect the Test Suite

Run tests in smaller batches to identify the problematic test(s):

```bash
# Run tests filtering by namespace/class
dotnet test --filter "FullyQualifiedName~SomeNamespace"
```

### Focus Areas Based on Known Issues

Based on the similar `InvokeLeakTest` issue and Terminal.Gui architecture:

1. **TimedEvents / Timer handling** - Check for DateTime.UtcNow resolution issues
2. **Console Input** - macOS terminal handling may block differently
3. **Thread synchronization** - Look for lock ordering issues with 46 threads
4. **Application.Init/Shutdown** - Test isolation may be failing
