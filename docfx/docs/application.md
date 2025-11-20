# Application Architecture

Terminal.Gui v2 uses an instance-based application architecture that decouples views from the global application state, improving testability and enabling multiple application contexts.

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
        S2[Previous Toplevel<br/>Waiting]
        S3[Base Toplevel<br/>Waiting]
        
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
var top = new Toplevel();
top.Add(myView);
Application.Run(top);
top.Dispose();
Application.Shutdown();

// NEW (v2 recommended - instance-based):
var app = Application.Create();
app.Init();
var top = new Toplevel();
top.Add(myView);
app.Run(top);
top.Dispose();
app.Shutdown();
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

## IApplication Interface

The `IApplication` interface defines the application contract:

```csharp
public interface IApplication
{
    /// <summary>
    /// Gets the currently running Toplevel (the "current session").
    /// Renamed from "Top" for clarity.
    /// </summary>
    Toplevel? Current { get; }
    
    /// <summary>
    /// Gets the stack of running sessions.
    /// Renamed from "TopLevels" to align with SessionToken terminology.
    /// </summary>
    ConcurrentStack<Toplevel> SessionStack { get; }
    
    IDriver? Driver { get; }
    IMainLoopCoordinator? MainLoop { get; }
    
    void Init(string? driverName = null);
    void Shutdown();
    SessionToken? Begin(Toplevel toplevel);
    void End(SessionToken sessionToken);
    // ... other members
}
```

## Terminology Changes

Terminal.Gui v2 modernized its terminology for clarity:

### Application.Current (formerly "Top")

The `Current` property represents the currently running Toplevel (the active session):

```csharp
// Access the current session
Toplevel? current = app.Current;

// From within a view
Toplevel? current = App?.Current;
```

**Why "Current" instead of "Top"?**
- Follows .NET patterns (`Thread.CurrentThread`, `HttpContext.Current`)
- Self-documenting: immediately clear it's the "current" active view
- Less confusing than "Top" which could mean "topmost in Z-order"

### Application.SessionStack (formerly "TopLevels")

The `SessionStack` property is the stack of running sessions:

```csharp
// Access all running sessions
foreach (var toplevel in app.SessionStack)
{
    // Process each session
}

// From within a view
int sessionCount = App?.SessionStack.Count ?? 0;
```

**Why "SessionStack" instead of "TopLevels"?**
- Describes both content (sessions) and structure (stack)
- Aligns with `SessionToken` terminology
- Follows .NET naming patterns (descriptive + collection type)

## Migration from Static Application

The static `Application` class delegates to `ApplicationImpl.Instance` (a singleton) and is marked obsolete. All static methods and properties are marked with `[Obsolete]` but remain functional for backward compatibility:

```csharp
public static partial class Application
{
    [Obsolete("The legacy static Application object is going away.")]
    public static Toplevel? Current => ApplicationImpl.Instance.Current;
    
    [Obsolete("The legacy static Application object is going away.")]
    public static ConcurrentStack<Toplevel> SessionStack => ApplicationImpl.Instance.SessionStack;
    
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
    Application.Current?.SetNeedsDraw();
}

// NEW:
void MyMethod(View view)
{
    view.App?.Current?.SetNeedsDraw();
}
```

**Strategy 2: Pass IApplication**

```csharp
// OLD:
void ProcessSessions()
{
    foreach (var toplevel in Application.SessionStack)
    {
        // Process
    }
}

// NEW:
void ProcessSessions(IApplication app)
{
    foreach (var toplevel in app.SessionStack)
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

## Session Management

### Begin and End

Applications manage sessions through `Begin()` and `End()`:

```csharp
var app = Application.Create ();
app.Init();

var toplevel = new Toplevel();

// Begin a new session - pushes to SessionStack
SessionToken? token = app.Begin(toplevel);

// Current now points to this toplevel
Debug.Assert(app.Current == toplevel);

// End the session - pops from SessionStack
if (token != null)
{
    app.End(token);
}

// Current restored to previous toplevel (if any)
```

### Nested Sessions

Multiple sessions can run nested:

```csharp
var app = Application.Create ();
app.Init();

// Session 1
var main = new Toplevel { Title = "Main" };
var token1 = app.Begin(main);
// app.Current == main, SessionStack.Count == 1

// Session 2 (nested)
var dialog = new Dialog { Title = "Dialog" };
var token2 = app.Begin(dialog);
// app.Current == dialog, SessionStack.Count == 2

// End dialog
app.End(token2);
// app.Current == main, SessionStack.Count == 1

// End main
app.End(token1);
// app.Current == null, SessionStack.Count == 0
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
    mockApp.Setup(a => a.Current).Returns(new Toplevel());
    
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
    var app = Application.Create ();
    try
    {
        app.Init(new FakeDriver());
        
        var view = new MyView();
        var top = new Toplevel();
        top.Add(view);
        
        app.Begin(top);
        
        // View.App automatically set
        Assert.NotNull(view.App);
        Assert.Same(app, view.App);
        
        // Test view behavior
        view.DoSomething();
        
    }
    finally
    {
        app.Shutdown();
    }
}
```

## Best Practices

### DO: Use View.App

```csharp
✅ GOOD:
public void Refresh()
{
    App?.Current?.SetNeedsDraw();
}
```

### DON'T: Use Static Application

```csharp
❌ AVOID:
public void Refresh()
{
    Application.Current?.SetNeedsDraw(); // Obsolete!
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
    Application.Current?.SetNeedsDraw(); // Obsolete static access
}

✅ PREFERRED:
public void Refresh()
{
    App?.Current?.SetNeedsDraw(); // Use View.App property
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
var app1 = Application.Create ();
app1.Init(new WindowsDriver());
var top1 = new Toplevel { Title = "App 1" };
// ... configure top1

// Application 2 (different driver!)
var app2 = Application.Create ();
app2.Init(new CursesDriver());
var top2 = new Toplevel { Title = "App 2" };
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
