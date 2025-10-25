# Application.Run Terminology - Industry Comparison

This document compares Terminal.Gui's terminology with other popular UI frameworks to provide context for the proposed changes.

## Framework Comparison Matrix

### Complete Lifecycle (Show/Run a Window)

| Framework | API | Notes |
|-----------|-----|-------|
| **Terminal.Gui (current)** | `Application.Run(toplevel)` | Modal execution |
| **Terminal.Gui (proposed)** | `Application.Run(toplevel)` | Keep same (high-level API) |
| **WPF** | `window.ShowDialog()` | Modal, returns DialogResult |
| **WPF** | `window.Show()` | Non-modal |
| **WinForms** | `form.ShowDialog()` | Modal |
| **WinForms** | `Application.Run(form)` | Main message loop |
| **Avalonia** | `window.ShowDialog()` | Modal, async |
| **GTK#** | `window.ShowAll()` + `Gtk.Application.Run()` | Combined |
| **Qt** | `QApplication::exec()` | Main event loop |
| **Electron** | `mainWindow.show()` | Non-modal |

### Session/Context Token

| Framework | Concept | Type | Purpose |
|-----------|---------|------|---------|
| **Terminal.Gui (current)** | `RunState` | Class | Token for Begin/End pairing |
| **Terminal.Gui (proposed)** | `ToplevelSession` | Class | Session token (clearer name) |
| **WPF** | N/A | - | Hidden by framework |
| **WinForms** | `ApplicationContext` | Class | Message loop context |
| **Avalonia** | N/A | - | Hidden by framework |
| **ASP.NET** | `HttpContext` | Class | Request context (analogous) |
| **Entity Framework** | `DbContext` | Class | Session context (analogous) |

**Analysis:** "Session" or "Context" are industry standards for bounded execution periods. "State" is misleading.

### Initialize/Start

| Framework | API | Purpose |
|-----------|-----|---------|
| **Terminal.Gui (current)** | `Application.Begin(toplevel)` | Prepare toplevel for execution |
| **Terminal.Gui (proposed)** | `Application.BeginSession(toplevel)` | Start an execution session |
| **WPF** | `window.Show()` / `window.ShowDialog()` | Combined with event loop |
| **WinForms** | `Application.Run(form)` | Combined initialization |
| **Node.js HTTP** | `server.listen()` | Start accepting requests |
| **ASP.NET** | `app.Run()` | Start web server |
| **Qt** | `widget->show()` | Show widget |

**Analysis:** Most frameworks combine initialization with execution. Terminal.Gui's separation is powerful for advanced scenarios.

### Event Loop Processing

| Framework | API | Purpose | Notes |
|-----------|-----|---------|-------|
| **Terminal.Gui (current)** | `Application.RunLoop(runState)` | Process events until stopped | Confusing "Run" + "Loop" |
| **Terminal.Gui (proposed)** | `Application.ProcessEvents(session)` | Process events until stopped | Clear action verb |
| **WPF** | `Dispatcher.Run()` | Run dispatcher loop | Hidden in most apps |
| **WinForms** | `Application.Run()` | Process message loop | Auto-started |
| **Node.js** | `eventLoop.run()` | Run event loop | Internal |
| **Qt** | `QApplication::exec()` | Execute event loop | "exec" = execute |
| **GTK** | `Gtk.Application.Run()` | Main loop | Standard GTK term |
| **Win32** | `GetMessage()` / `DispatchMessage()` | Message pump | Manual control |
| **X11** | `XNextEvent()` | Process events | Manual control |

**Analysis:** "ProcessEvents" or "EventLoop" are clearer than "RunLoop". The term "pump" is Windows-specific.

### Single Iteration

| Framework | API | Purpose |
|-----------|-----|---------|
| **Terminal.Gui (current)** | `Application.RunIteration()` | Process one event cycle |
| **Terminal.Gui (proposed)** | `Application.ProcessEventsIteration()` | Process one event cycle |
| **Game Engines (Unity)** | `Update()` / `Tick()` | One frame/tick |
| **Game Engines (Unreal)** | `Tick(DeltaTime)` | One frame |
| **WPF** | `Dispatcher.ProcessEvents()` | Process pending events |
| **WinForms** | `Application.DoEvents()` | Process pending events |
| **Node.js** | `setImmediate()` / `process.nextTick()` | Next event loop iteration |

**Analysis:** "Iteration", "Tick", or "DoEvents" are all clear. "ProcessEvents" aligns with WPF.

### Cleanup/End

| Framework | API | Purpose |
|-----------|-----|---------|
| **Terminal.Gui (current)** | `Application.End(runState)` | Clean up after execution |
| **Terminal.Gui (proposed)** | `Application.EndSession(session)` | End the execution session |
| **WPF** | `window.Close()` | Close window |
| **WinForms** | `form.Close()` | Close form |
| **ASP.NET** | Request ends automatically | Dispose context |
| **Entity Framework** | `context.Dispose()` | Dispose context |

**Analysis:** "EndSession" pairs clearly with "BeginSession". "Close" works for windows but not for execution context.

### Stop/Request Stop

| Framework | API | Purpose |
|-----------|-----|---------|
| **Terminal.Gui (current)** | `Application.RequestStop()` | Signal loop to stop |
| **Terminal.Gui (proposed)** | `Application.StopProcessingEvents()` | Stop event processing |
| **WPF** | `window.Close()` | Close window, stops its loop |
| **WinForms** | `Application.Exit()` | Exit application |
| **Node.js** | `server.close()` | Stop accepting connections |
| **CancellationToken** | `cancellationToken.Cancel()` | Request cancellation |

**Analysis:** "RequestStop" is already clear. "StopProcessingEvents" is more explicit about what stops.

## Terminology Patterns Across Industries

### Game Development

Game engines use clear, explicit terminology:

```csharp
// Unity pattern
void Start() { }      // Initialize
void Update() { }     // Per-frame update (tick)
void OnDestroy() { }  // Cleanup

// Typical game loop
while (running)
{
    ProcessInput();
    UpdateGameState();
    Render();
}
```

**Lesson:** Use explicit verbs that describe what happens each phase.

### Web Development

Web frameworks use session/context patterns:

```csharp
// ASP.NET Core
public void Configure(IApplicationBuilder app)
{
    app.Run(async context =>  // HttpContext = request session
    {
        await context.Response.WriteAsync("Hello");
    });
}

// Entity Framework
using (var context = new DbContext())  // Session
{
    // Work with data
}
```

**Lesson:** "Context" or "Session" for bounded execution periods is industry standard.

### GUI Frameworks

Desktop GUI frameworks separate showing from modal execution:

```csharp
// WPF pattern
window.Show();              // Non-modal
var result = window.ShowDialog();  // Modal (blocks)

// Terminal.Gui (current - confusing)
Application.Run(toplevel);  // Modal? Non-modal? Unclear

// Terminal.Gui (proposed - clearer)
Application.Run(toplevel);  // High-level: modal execution
// OR low-level:
var session = Application.BeginSession(toplevel);
Application.ProcessEvents(session);
Application.EndSession(session);
```

**Lesson:** Separate high-level convenience from low-level control.

## Key Insights

### 1. "Run" is Overloaded Everywhere

Many frameworks have "Run" methods, but they mean different things:
- **WPF**: `Application.Run()` - "run the entire application"
- **WinForms**: `Application.Run(form)` - "run with this form as main"
- **ASP.NET**: `app.Run()` - "start the web server"
- **Terminal.Gui**: `Application.Run(toplevel)` - "run this toplevel modally"

**Solution:** Keep high-level `Run()` for simplicity, but clarify low-level APIs.

### 2. Session/Context Pattern is Standard

The pattern of a token representing a bounded execution period is common:
- `HttpContext` - one HTTP request
- `DbContext` - one database session
- `ExecutionContext` - one execution scope
- `CancellationToken` - one cancellation scope

**Terminal.Gui's `RunState` fits this pattern** - it should be named accordingly.

### 3. Begin/End vs Start/Stop vs Open/Close

Different frameworks use different pairs:
- **Begin/End** - .NET (BeginInvoke/EndInvoke, BeginInit/EndInit)
- **Start/Stop** - Common (StartService/StopService)
- **Open/Close** - Resources (OpenFile/CloseFile, OpenConnection/CloseConnection)

Terminal.Gui currently uses **Begin/End**, which is fine, but it needs a noun:
- ✅ `BeginSession/EndSession` - Clear
- ❓ `Begin/End` - Begin what?

### 4. Event Processing Terminology

Most frameworks use one of:
- **ProcessEvents** - Explicit action (WPF, WinForms)
- **EventLoop** - Noun describing the construct
- **Pump/PumpMessages** - Windows-specific
- **Dispatch** - Action of dispatching events

**Terminal.Gui's "RunLoop"** is ambiguous - it could be a verb (run the loop) or a noun (the RunLoop object).

## Recommendations Based on Industry Analysis

### Primary Recommendation: Session-Based (Option 1)

```
Application.Run(toplevel)                              // Keep - familiar, simple
  ├─ Application.BeginSession(toplevel) → ToplevelSession
  ├─ Application.ProcessEvents(session)
  └─ Application.EndSession(session)
```

**Aligns with:**
- .NET patterns (BeginInvoke/EndInvoke, HttpContext sessions)
- Industry standard "session" terminology
- Explicit "ProcessEvents" from WPF/WinForms

**Why it wins:**
1. "Session" is universally understood as bounded execution
2. "ProcessEvents" is explicit about what happens
3. Begin/End is already .NET standard
4. Minimal disruption to existing mental model

### Alternative: Lifecycle-Based (Option 3)

```
Application.Run(toplevel)
  ├─ Application.Start(toplevel) → ExecutionContext
  ├─ Application.Execute(context)
  └─ Application.Stop(context)
```

**Aligns with:**
- Service patterns (StartService/StopService)
- Game patterns (Start/Update/Stop)
- Simpler verbs

**Trade-offs:**
- ⚠️ "Start/Stop" breaks existing Begin/End pattern
- ✅ More intuitive for newcomers
- ⚠️ "Execute" is less explicit than "ProcessEvents"

### Why Not Modal/Show (Option 2)

```
Application.ShowModal(toplevel)
  ├─ Application.Activate(toplevel)
  └─ Application.Deactivate(toplevel)
```

**Issues:**
- Terminal.Gui doesn't distinguish modal/non-modal the way WPF does
- "Activate/Deactivate" implies window state, not execution
- Bigger departure from current API

## Conclusion

**Industry analysis supports Option 1 (Session-Based):**

1. ✅ "Session" is industry standard for bounded execution
2. ✅ "ProcessEvents" is clear and matches WPF/WinForms
3. ✅ Begin/End is established .NET pattern
4. ✅ Minimal disruption to existing API
5. ✅ Clear improvement over current "Run*" terminology

The proposed terminology brings Terminal.Gui in line with industry patterns while respecting its unique architecture that exposes low-level event loop control.

## References

- [WPF Application Model](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/app-development/application-management-overview)
- [WinForms Application Class](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.application)
- [Avalonia Application Lifetime](https://docs.avaloniaui.net/docs/concepts/application-lifetimes)
- [GTK Application](https://docs.gtk.org/gtk4/class.Application.html)
- [Qt Application](https://doc.qt.io/qt-6/qapplication.html)
- [.NET HttpContext](https://docs.microsoft.com/en-us/dotnet/api/system.web.httpcontext)
- [Entity Framework DbContext](https://docs.microsoft.com/en-us/ef/core/dbcontext-configuration/)
