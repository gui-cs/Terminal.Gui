# Application Architecture

Terminal.Gui v2 uses an instance-based application architecture with the **IRunnable** interface pattern that decouples views from the global application state, improving testability, enabling multiple application contexts, and providing type-safe result handling.

## Key Features

- **Instance-Based**: Use `Application.Create()` to get an `IApplication` instance instead of static methods
- **IRunnable Interface**: Views implement `IRunnable<TResult>` to participate in session management without inheriting from `Runnable`
- **Fluent API**: Chain `Init()`, `Run()`, and `Shutdown()` for elegant, concise code  
- **IDisposable Pattern**: Proper resource cleanup with `Dispose()` or `using` statements
- **Automatic Disposal**: Framework-created runnables are automatically disposed
- **Type-Safe Results**: Generic `TResult` parameter provides compile-time type safety
- **CWP Compliance**: All lifecycle events follow the Cancellable Work Pattern

## View Hierarchy and Run Stack

```mermaid
graph TB
    subgraph ViewTree["View Hierarchy (SuperView/SubView)"]
        direction TB
        Top[app.Current<br/>Window]
        Menu[MenuBar]
        Status[StatusBar]
        Content[Content View]
        Button1[Button]
        Button2[Button]
        
        Top --> Menu
        Top --> Status
        Top --> Content
        Content --> Button1
        Content --> Button2
    end
    
    subgraph Stack["app.SessionStack"]
        direction TB
        S1[Window<br/>Currently Active]
        S2[Previous Runnable<br/>Waiting]
        S3[Base Runnable<br/>Waiting]
        
        S1 -.-> S2 -.-> S3
    end
    
    Top -.->|"same instance"| S1
    
    style Top fill:#ccffcc,stroke:#339933,stroke-width:3px
    style S1 fill:#ccffcc,stroke:#339933,stroke-width:3px
```

## Usage Example Flow

```mermaid
sequenceDiagram
    participant App as IApplication
    participant Main as Main Window
    participant Dialog as Dialog
    
    Note over App: Initially empty SessionStack
    
    App->>Main: Run(mainWindow)
    activate Main
    Note over App: SessionStack: [Main]<br/>Current: Main
    
    Main->>Dialog: Run(dialog)
    activate Dialog
    Note over App: SessionStack: [Dialog, Main]<br/>Current: Dialog
    
    Dialog->>App: RequestStop()
    deactivate Dialog
    Note over App: SessionStack: [Main]<br/>Current: Main
    
    Main->>App: RequestStop()
    deactivate Main
    Note over App: SessionStack: []<br/>Current: null
```

## Key Concepts

### Instance-Based vs Static

**Terminal.Gui v2** supports both static and instance-based patterns. The static `Application` class is marked obsolete but still functional for backward compatibility. The recommended pattern is to use `Application.Create()` to get an `IApplication` instance:

```csharp
// OLD (v1 / early v2 - still works but obsolete):
Application.Init();
var top = new Window();
top.Add(myView);
Application.Run(top);
top.Dispose();
Application.Shutdown();

// NEW (v2 recommended - instance-based):
using var app = Application.Create();
app.Init();
var top = new Window();
top.Add(myView);
app.Run(top);
top.Dispose();

// NEWEST (v2 with IRunnable and Fluent API):
using IApplication app = Application.Create();
Color? result = app.Run<ColorPickerDialog>().GetResult<Color>();

// SIMPLEST (v2 - automatic Init and Dispose):
IApplication app = Application.Create();
Color? result = app.Run<ColorPickerDialog>().GetResult<Color>();
app.Dispose();
```

**Note:** The static `Application` class delegates to `ApplicationImpl.Instance` (a singleton). `Application.Create()` creates a **new** `ApplicationImpl` instance, enabling multiple application contexts and better testability.

### View.App Property

Every view now has an `App` property that references its application context:

```csharp
public class View
{
    /// <summary>
    /// Gets the application context for this view.
    /// </summary>
    public IApplication? App { get; internal set; }
    
    /// <summary>
    /// Gets the application context, checking parent hierarchy if needed.
    /// Override to customize application resolution.
    /// </summary>
    public virtual IApplication? GetApp() => App ?? SuperView?.GetApp();
}
```

**Benefits:**
- Views can be tested without `Application.Init()`
- Multiple applications can coexist
- Clear ownership: views know their context
- Reduced global state dependencies

### Accessing Application from Views

**Recommended pattern:**

```csharp
public class MyView : View
{
    public override void OnEnter(View view)
    {
        // Use View.App instead of static Application
        App?.Current?.SetNeedsDraw();
        
        // Access SessionStack
        if (App?.SessionStack.Count > 0)
        {
            // Work with sessions
        }
    }
}
```

**Alternative - dependency injection:**

```csharp
public class MyView : View
{
    private readonly IApplication _app;
    
    public MyView(IApplication app)
    {
        _app = app;
        // Now completely decoupled from static Application
    }
    
    public void DoWork()
    {
        _app.Current?.SetNeedsDraw();
    }
}
```

## IRunnable Architecture

Terminal.Gui v2 introduces the **IRunnable** interface pattern that decouples runnable behavior from the `Runnable` class hierarchy. Views can implement `IRunnable<TResult>` to participate in session management without inheritance constraints.

### Key Benefits

- **Interface-Based**: No forced inheritance from `Runnable`
- **Type-Safe Results**: Generic `TResult` parameter provides compile-time type safety
- **Fluent API**: Method chaining for elegant, concise code
- **Automatic Disposal**: Framework manages lifecycle of created runnables
- **CWP Compliance**: All lifecycle events follow the Cancellable Work Pattern

### Fluent API Pattern

The fluent API enables elegant method chaining with automatic resource management:

```csharp
// All-in-one: Create, initialize, run, shutdown, and extract result
Color? result = Application.Create()
                           .Init()
                           .Run<ColorPickerDialog>()
                           .Shutdown() as Color?;

if (result is { })
{
    ApplyColor(result);
}
```

**Key Methods:**

- `Init()` - Returns `IApplication` for chaining
- `Run<TRunnable>()` - Creates and runs runnable, returns `IApplication`
- `Shutdown()` - Disposes framework-owned runnables, returns `object?` result

### Disposal Semantics

**"Whoever creates it, owns it":**

| Method | Creator | Owner | Disposal |
|--------|---------|-------|----------|
| `Run<TRunnable>()` | Framework | Framework | Automatic in `Shutdown()` |
| `Run(IRunnable)` | Caller | Caller | Manual by caller |

```csharp
// Framework ownership - automatic disposal
var result = app.Run<MyDialog>().Shutdown();

// Caller ownership - manual disposal
var dialog = new MyDialog();
app.Run(dialog);
var result = dialog.Result;
dialog.Dispose();  // Caller must dispose
```

### Creating Runnable Views

Derive from `Runnable<TResult>` or implement `IRunnable<TResult>`:

```csharp
public class FileDialog : Runnable<string?>
{
    private TextField _pathField;
    
    public FileDialog()
    {
        Title = "Select File";
        
        _pathField = new TextField { X = 1, Y = 1, Width = Dim.Fill(1) };
        
        var okButton = new Button { Text = "OK", IsDefault = true };
        okButton.Accepting += (s, e) => {
            Result = _pathField.Text;
            Application.RequestStop();
        };
        
        Add(_pathField, okButton);
    }
    
    protected override bool OnIsRunningChanging(bool oldValue, bool newValue)
    {
        if (!newValue)  // Stopping - extract result before disposal
        {
            Result = _pathField?.Text;
        }
        return base.OnIsRunningChanging(oldValue, newValue);
    }
}
```

### Lifecycle Properties

- **`IsRunning`** - True when runnable is on `RunnableSessionStack`
- **`IsModal`** - True when runnable is at top of stack (capturing all input)
- **`Result`** - Typed result value set before stopping

### Lifecycle Events (CWP-Compliant)

All events follow Terminal.Gui's Cancellable Work Pattern:

| Event | Cancellable | When | Use Case |
|-------|-------------|------|----------|
| `IsRunningChanging` | ✓ | Before add/remove from stack | Extract result, prevent close |
| `IsRunningChanged` | ✗ | After stack change | Post-start/stop cleanup |
| `IsModalChanged` | ✗ | After modal state change | Update UI after focus change |

**Example - Result Extraction:**

```csharp
protected override bool OnIsRunningChanging(bool oldValue, bool newValue)
{
    if (!newValue)  // Stopping
    {
        // Extract result before views are disposed
        Result = _colorPicker.SelectedColor;
        
        // Optionally cancel stop (e.g., unsaved changes)
        if (HasUnsavedChanges())
        {
            int response = MessageBox.Query("Save?", "Save changes?", "Yes", "No", "Cancel");
            if (response == 2) return true;  // Cancel stop
            if (response == 0) Save();
        }
    }
    
    return base.OnIsRunningChanging(oldValue, newValue);
}
```

### RunnableSessionStack

The `RunnableSessionStack` manages all running `IRunnable` sessions:

```csharp
public interface IApplication
{
    /// <summary>
    /// Stack of running IRunnable sessions.
    /// Each entry is a RunnableSessionToken wrapping an IRunnable.
    /// </summary>
    ConcurrentStack<RunnableSessionToken>? RunnableSessionStack { get; }
    
    /// <summary>
    /// The IRunnable at the top of RunnableSessionStack (currently modal).
    /// </summary>
    IRunnable? TopRunnable { get; }
}
```

**Stack Behavior:**

- Push: `Begin(IRunnable)` adds to top of stack
- Pop: `End(RunnableSessionToken)` removes from stack
- Peek: `TopRunnable` returns current modal runnable
- All: `RunnableSessionStack` enumerates all running sessions

## IApplication Interface

The `IApplication` interface defines the application contract with support for both legacy `Runnable` and modern `IRunnable` patterns:

```csharp
public interface IApplication
{
    // IRunnable support (primary)
    IRunnable? TopRunnable { get; }
    View? TopRunnableView { get; }
    ConcurrentStack<SessionToken>? SessionStack { get; }
    IRunnable? FrameworkOwnedRunnable { get; set; }
    
    // Driver and lifecycle
    IDriver? Driver { get; }
    IMainLoopCoordinator? Coordinator { get; }
    
    // Fluent API methods  
    IApplication Init(string? driverName = null);
    void Dispose(); // IDisposable
    
    // Runnable methods
    SessionToken? Begin(IRunnable runnable);
    object? Run(IRunnable runnable, Func<Exception, bool>? errorHandler = null);
    IApplication Run<TRunnable>(Func<Exception, bool>? errorHandler = null) where TRunnable : IRunnable, new();
    void RequestStop(IRunnable? runnable);
    void End(SessionToken sessionToken);
    
    // Result extraction
    object? GetResult();
    T? GetResult<T>() where T : class;
    
    // ... other members
}
```

## Terminology Changes

Terminal.Gui v2 modernized its terminology for clarity:

### Application.TopRunnable (formerly "Current", and before that "Top")

The `TopRunnable` property represents the `IRunnable` on the top of the session stack (the active runnable session):

```csharp
// Access the top runnable session
IRunnable? topRunnable = app.TopRunnable;

// From within a view  
IRunnable? topRunnable = App?.TopRunnable;

// Cast to View if needed
View? topView = app.TopRunnableView;
```

**Why "TopRunnable"?**
- Clearly indicates it's the top of the runnable session stack
- Aligns with the IRunnable architecture
- Distinguishes from other concepts like "Current" which could be ambiguous
- Works with any view that implements `IRunnable`, not just `Runnable`

### Application.SessionStack (formerly "Runnables")

The `SessionStack` property is the stack of running sessions:

```csharp
// Access all running sessions
foreach (var runnable in app.SessionStack)
{
    // Process each session
}

// From within a view
int sessionCount = App?.SessionStack.Count ?? 0;
```

**Why "SessionStack" instead of "Runnables"?**
- Describes both content (sessions) and structure (stack)
- Aligns with `SessionToken` terminology
- Follows .NET naming patterns (descriptive + collection type)

## Migration from Static Application

The static `Application` class delegates to `ApplicationImpl.Instance` (a singleton) and is marked obsolete. All static methods and properties are marked with `[Obsolete]` but remain functional for backward compatibility:

```csharp
public static partial class Application
{
    [Obsolete("The legacy static Application object is going away.")]
    public static View? TopRunnableView => ApplicationImpl.Instance.TopRunnableView;
    
    [Obsolete("The legacy static Application object is going away.")]
    public static IRunnable? TopRunnable => ApplicationImpl.Instance.TopRunnable;
    
    [Obsolete("The legacy static Application object is going away.")]
    public static ConcurrentStack<SessionToken>? SessionStack => ApplicationImpl.Instance.SessionStack;
    
    // ... other obsolete static members
}
```

**Important:** The static `Application` class uses a singleton (`ApplicationImpl.Instance`), while `Application.Create()` creates new instances. For new code, prefer the instance-based pattern using `Application.Create()`.

### Migration Strategies

**Strategy 1: Use View.App**

```csharp
// OLD:
void MyMethod()
{
    Application.TopRunnable?.SetNeedsDraw();
}

// NEW:
void MyMethod(View view)
{
    view.App?.TopRunnableView?.SetNeedsDraw();
}
```

**Strategy 2: Pass IApplication**

```csharp
// OLD:
void ProcessSessions()
{
    foreach (var runnable in Application.SessionStack)
    {
        // Process
    }
}

// NEW:
void ProcessSessions(IApplication app)
{
    foreach (var runnable in app.SessionStack)
    {
        // Process
    }
}
```

**Strategy 3: Store IApplication Reference**

```csharp
public class MyService
{
    private readonly IApplication _app;
    
    public MyService(IApplication app)
    {
        _app = app;
    }
    
    public void DoWork()
    {
        _app.Current?.Title = "Processing...";
    }
}
```

## Resource Management and Disposal

Terminal.Gui v2 implements the `IDisposable` pattern for proper resource cleanup. Applications must be disposed after use to:
- Stop the input thread cleanly
- Release driver resources
- Prevent thread leaks in tests
- Free unmanaged resources

### Using the `using` Statement (Recommended)

```csharp
// Automatic disposal with using statement
using (var app = Application.Create().Init())
{
    app.Run<MyDialog>();
    // app.Dispose() automatically called when scope exits
}
```

### Manual Disposal

```csharp
// Manual disposal
var app = Application.Create();
try
{
    app.Init();
    app.Run<MyDialog>();
}
finally
{
    app.Dispose(); // Ensure cleanup even if exception occurs
}
```

### Shutdown() vs Dispose()

- **`Shutdown()`** - Obsolete method that calls `Dispose()` and returns the result
- **`Dispose()`** - Recommended IDisposable pattern for resource cleanup
- **`GetResult()`** / **`GetResult<T>()`** - Retrieve results after `Dispose()`

```csharp
// OLD (Shutdown returns result):
var result = app.Run<MyDialog>().Shutdown() as MyResult;

// NEW (Dispose + GetResult):
using (var app = Application.Create().Init())
{
    app.Run<MyDialog>();
    var result = app.GetResult<MyResult>();
}
```

### Input Thread Lifecycle

When you call `Init()`, Terminal.Gui starts a dedicated input thread that continuously polls for console input. This thread must be stopped properly:

```csharp
var app = Application.Create();
app.Init("fake"); // Input thread starts here

// Input thread runs in background at ~50 polls/second (20ms throttle)

app.Dispose(); // Cancels input thread and waits for it to exit
```

**Important for Tests**: Always dispose applications in tests to prevent thread leaks:

```csharp
[Fact]
public void My_Test()
{
    using var app = Application.Create();
    app.Init("fake");
    
    // Test code here
    
    // app.Dispose() called automatically
}
```

### Singleton Re-initialization

The legacy static `Application` singleton can be re-initialized after disposal (for backward compatibility with old tests):

```csharp
// Test 1
Application.Init();
Application.Shutdown();

// Test 2 - singleton resets and can be re-initialized
Application.Init(); // ✅ Works!
Application.Shutdown();
```

However, instance-based applications follow standard `IDisposable` semantics and cannot be reused after disposal:

```csharp
var app = Application.Create();
app.Init();
app.Dispose();

app.Init(); // ❌ Throws ObjectDisposedException
```

## Session Management

### Begin and End

Applications manage sessions through `Begin()` and `End()`:

```csharp
using var app = Application.Create ();
app.Init();

var window = new Window();

// Begin a new session - pushes to SessionStack
SessionToken? token = app.Begin(window);

// TopRunnable now points to this window
Debug.Assert(app.TopRunnable == window);

// End the session - pops from SessionStack
if (token != null)
{
    app.End(token);
}

// TopRunnable restored to previous runnable (if any)
```

### Nested Sessions

Multiple sessions can run nested:

```csharp
using var app = Application.Create ();
app.Init();

// Session 1
var main = new Window { Title = "Main" };
var token1 = app.Begin(main);
// app.TopRunnable == main, SessionStack.Count == 1

// Session 2 (nested)
var dialog = new Dialog { Title = "Dialog" };
var token2 = app.Begin(dialog);
// app.TopRunnable == dialog, SessionStack.Count == 2

// End dialog
app.End(token2);
// app.TopRunnable == main, SessionStack.Count == 1

// End main
app.End(token1);
// app.TopRunnable == null, SessionStack.Count == 0
```

## View.Driver Property

Similar to `View.App`, views now have a `Driver` property:

```csharp
public class View
{
    /// <summary>
    /// Gets the driver for this view.
    /// </summary>
    public IDriver? Driver => GetDriver();
    
    /// <summary>
    /// Gets the driver, checking application context if needed.
    /// Override to customize driver resolution.
    /// </summary>
    public virtual IDriver? GetDriver() => App?.Driver;
}
```

**Usage:**

```csharp
public override void OnDrawContent(Rectangle viewport)
{
    // Use view's driver instead of Application.Driver
    Driver?.Move(0, 0);
    Driver?.AddStr("Hello");
}
```

## Testing with the New Architecture

The instance-based architecture dramatically improves testability:

### Testing Views in Isolation

```csharp
[Fact]
public void MyView_DisplaysCorrectly()
{
    // Create mock application
    var mockApp = new Mock<IApplication>();
    mockApp.Setup(a => a.Current).Returns(new Runnable());
    
    // Create view with mock app
    var view = new MyView { App = mockApp.Object };
    
    // Test without Application.Init()!
    view.SetNeedsDraw();
    Assert.True(view.NeedsDraw);
    
    // No Application.Shutdown() needed!
}
```

### Testing with Real ApplicationImpl

```csharp
[Fact]
public void MyView_WorksWithRealApplication()
{
    using var app = Application.Create ();
    app.Init("fake");
    
    var view = new MyView();
    var top = new Window();
    top.Add(view);
    
    app.Begin(top);
    
    // View.App automatically set
    Assert.NotNull(view.App);
    Assert.Same(app, view.App);
    
    // Test view behavior
    view.DoSomething();
}
```

## Best Practices

### DO: Use View.App

```csharp
✅ GOOD:
public void Refresh()
{
    App?.TopRunnableView?.SetNeedsDraw();
}
```

### DON'T: Use Static Application

```csharp
❌ AVOID:
public void Refresh()
{
    Application.TopRunnableView?.SetNeedsDraw(); // Obsolete!
}
```

### DO: Pass IApplication as Dependency

```csharp
✅ GOOD:
public class Service
{
    public Service(IApplication app) { }
}
```

### DON'T: Use Static Application in New Code

```csharp
❌ AVOID (obsolete pattern):
public void Refresh()
{
    Application.TopRunnableView?.SetNeedsDraw(); // Obsolete static access
}

✅ PREFERRED:
public void Refresh()
{
    App?.TopRunnableView?.SetNeedsDraw(); // Use View.App property
}
```

### DO: Override GetApp() for Custom Resolution

```csharp
✅ GOOD:
public class SpecialView : View
{
    private IApplication? _customApp;
    
    public override IApplication? GetApp()
    {
        return _customApp ?? base.GetApp();
    }
}
```

## Advanced Scenarios

### Multiple Applications

The instance-based architecture enables multiple applications:

```csharp
// Application 1
using var app1 = Application.Create ();
app1.Init("windows");
var top1 = new Window { Title = "App 1" };
// ... configure top1

// Application 2 (different driver!)
using var app2 = Application.Create ();
app2.Init("unix");
var top2 = new Window { Title = "App 2" };
// ... configure top2

// Views in top1 use app1
// Views in top2 use app2
```

### Application-Agnostic Views

Create views that work with any application:

```csharp
public class UniversalView : View
{
    public void ShowMessage(string message)
    {
        // Works regardless of which application context
        var app = GetApp();
        if (app != null)
        {
            var msg = new MessageBox(message);
            app.Begin(msg);
        }
    }
}
```

## See Also

- [Navigation](navigation.md) - Navigation with the instance-based architecture
- [Keyboard](keyboard.md) - Keyboard handling through View.App
- [Mouse](mouse.md) - Mouse handling through View.App  
- [Drivers](drivers.md) - Driver access through View.Driver
- [Multitasking](multitasking.md) - Session management with SessionStack
