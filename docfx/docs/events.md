# Terminal.Gui Events Deep Dive

This document provides practical guidance for implementing events in Terminal.Gui using the [Cancellable Work Pattern (CWP)](cancellable-work-pattern.md).

> [!TIP]
> **New to CWP?** Read the [Cancellable Work Pattern](cancellable-work-pattern.md) conceptual overview first.

## Quick Start: Which Pattern Do I Need?

Use this decision tree to choose the right pattern:

```
┌─────────────────────────────────────────────────────────────┐
│              Which Event Pattern Should I Use?              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Need to notify about something?                            │
│           │                                                 │
│           ▼                                                 │
│  ┌─────────────────┐                                        │
│  │ Can it be       │──► NO ──► Simple EventHandler          │
│  │ cancelled?      │          (no CWP needed)               │
│  └────────┬────────┘                                        │
│           │ YES                                             │
│           ▼                                                 │
│  ┌─────────────────┐                                        │
│  │ Property or     │──► PROPERTY ──► CWPPropertyHelper      │
│  │ Action/Workflow?│                 (Recipe 1)             │
│  └────────┬────────┘                                        │
│           │ ACTION                                          │
│           ▼                                                 │
│       Manual CWP or CWPWorkflowHelper                       │
│       (Recipe 2)                                            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

| Scenario | Pattern | Jump to |
|----------|---------|---------|
| Property change (cancellable) | `CWPPropertyHelper` | [Recipe 1](#recipe-1-cancellable-property-change) |
| Action/workflow (cancellable) | Manual CWP or `CWPWorkflowHelper` | [Recipe 2](#recipe-2-cancellable-action-workflow) |
| Simple notification (no cancel) | `EventHandler` | [Recipe 3](#recipe-3-simple-notification) |
| Property notification (MVVM) | `INotifyPropertyChanged` | [Recipe 4](#recipe-4-mvvm-property-notification) |

## See Also

* [Cancellable Work Pattern](cancellable-work-pattern.md) - Conceptual overview
* [Command Deep Dive](command.md) - Command system details
* [Lexicon & Taxonomy](lexicon.md)

## Lexicon and Taxonomy

[!INCLUDE [Events Lexicon](~/includes/events-lexicon.md)]

---

## Recipes: Implementing CWP in Terminal.Gui

### Recipe 1: Cancellable Property Change

**Use when:** A property change can be vetoed/cancelled by subclasses or subscribers.

#### Step 1: Define the Events and Virtual Methods

```csharp
public class MyDataView : View
{
    private object? _dataSource;

    // Pre-change event (cancellable)
    public event EventHandler<ValueChangingEventArgs<object?>>? DataSourceChanging;

    // Post-change event (notification)
    public event EventHandler<ValueChangedEventArgs<object?>>? DataSourceChanged;

    // Virtual method for subclasses (pre-change) - returns true to cancel
    protected virtual bool OnDataSourceChanging(ValueChangingEventArgs<object?> args) => false;

    // Virtual method for subclasses (post-change) - void, cannot cancel
    protected virtual void OnDataSourceChanged(ValueChangedEventArgs<object?> args) { }
}
```

#### Step 2: Implement the Property with CWPPropertyHelper

```csharp
public object? DataSource
{
    get => _dataSource;
    set
    {
        if (CWPPropertyHelper.ChangeProperty (
                sender: this,
                currentValue: ref _dataSource,
                newValue: value,
                onChanging: OnDataSourceChanging,
                changingEvent: DataSourceChanging,
                doWork: newDataSource =>
                {
                    // Additional work AFTER value changes but BEFORE Changed events
                    // e.g., refresh display, update selection
                    SetNeedsDraw ();
                },
                onChanged: OnDataSourceChanged,
                changedEvent: DataSourceChanged,
                out _))
        {
            // Property was changed (not cancelled)
        }
    }
}
```

#### Step 3: Consuming the Events

```csharp
// External subscriber (event)
myDataView.DataSourceChanging += (sender, args) =>
{
    if (args.NewValue is null)
    {
        args.Handled = true; // Prevent null assignment
    }
};

myDataView.DataSourceChanged += (sender, args) =>
{
    Console.WriteLine($"DataSource changed from {args.OldValue} to {args.NewValue}");
};

// Subclass (virtual method override)
public class MyCustomDataView : MyDataView
{
    protected override bool OnDataSourceChanging(ValueChangingEventArgs<object?> args)
    {
        // Validate new data source
        if (args.NewValue is ICollection { Count: 0 })
        {
            return true; // Cancel - don't allow empty collections
        }
        return false;
    }
}
```

---

### Recipe 2: Cancellable Action/Workflow

**Use when:** An action or operation can be cancelled by subclasses or subscribers.

**Example:** Custom view with an `Executing` event.

#### Option A: Manual CWP Implementation

```csharp
public class MyProcessor : View
{
    // Event for external subscribers
    public event EventHandler<CancelEventArgs>? Processing;

    // Virtual method for subclasses
    protected virtual bool OnProcessing(CancelEventArgs args)
    {
        return false; // Return true to cancel
    }

    // Internal method that implements CWP
    public bool Process()
    {
        CancelEventArgs args = new ();

        // Step 1: Call virtual method (subclass gets first chance)
        if (OnProcessing(args) || args.Cancel)
        {
            return false; // Cancelled
        }

        // Step 2: Raise event (external subscribers get a chance)
        Processing?.Invoke(this, args);
        if (args.Cancel)
        {
            return false; // Cancelled
        }

        // Step 3: Execute default behavior
        DoProcessing();
        return true;
    }

    private void DoProcessing()
    {
        // Default processing logic
    }
}
```

#### Option B: Using CWPWorkflowHelper

```csharp
public class MyProcessor : View
{
    public event EventHandler<ResultEventArgs<bool>>? Processing;

    protected virtual bool OnProcessing(ResultEventArgs<bool> args)
    {
        return false; // Return true to cancel
    }

    public bool? Process()
    {
        ResultEventArgs<bool> args = new ();

        return CWPWorkflowHelper.Execute(
            onMethod: OnProcessing,
            eventHandler: Processing,
            args: args,
            defaultAction: () =>
            {
                // Default processing logic
                DoProcessing();
                args.Result = true;
            });
    }

    private void DoProcessing()
    {
        // Processing logic
    }
}
```

---

### Recipe 3: Simple Notification

**Use when:** You just need to notify that something happened (no cancellation needed).

> [!IMPORTANT]
> The virtual method must be a **no-op by default**. It exists solely for subclasses to override.
> The event invocation happens in a separate `Raise*` method, NOT in the virtual method.

```csharp
public class MyView : View
{
    // Simple event - no cancellation
    public event EventHandler? SelectionMade;

    // Virtual method for subclasses - NO-OP by default
    protected virtual void OnSelectionMade()
    {
        // Does nothing by default.
        // Subclasses override this to react to the selection.
    }

    // Internal method that raises the notification
    private void RaiseSelectionMade()
    {
        // 1. Call virtual method first (subclasses get priority)
        OnSelectionMade();

        // 2. Raise event (external subscribers)
        SelectionMade?.Invoke(this, EventArgs.Empty);
    }

    private void HandleSelection()
    {
        // ... selection logic ...
        RaiseSelectionMade();
    }
}

// Subclass example
public class MyCustomView : MyView
{
    protected override void OnSelectionMade()
    {
        // React to selection in subclass
        UpdateStatusBar();
    }
}
```

---

### Recipe 4: MVVM Property Notification

**Use when:** You need data binding support via `INotifyPropertyChanged`.

```csharp
public class ViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }
}
```

---

## Event Categories in Terminal.Gui

Terminal.Gui uses several types of events:

| Category | Examples | Pattern |
|----------|----------|---------|
| **UI Interaction** | `KeyDown`, `MouseClick` | CWP with `Handled` |
| **View Lifecycle** | `Initialized`, `Disposed` | Simple notification |
| **Property Change** | `TextChanging`, `TextChanged` | CWP with `Handled` |
| **Drawing** | `DrawingContent`, `DrawComplete` | CWP with `Handled` |
| **Command** | `Accepting`, `Activating` | CWP with `Handled` |

## Event Context and Arguments

### Standard Event Arguments

Terminal.Gui provides these event argument types:

| Type | Use Case | Key Properties |
|------|----------|----------------|
| `ValueChangingEventArgs<T>` | Pre-property-change | `CurrentValue`, `NewValue`, `Handled` |
| `ValueChangedEventArgs<T>` | Post-property-change | `OldValue`, `NewValue` |
| `CommandEventArgs` | Command execution | `Context`, `Handled` |
| `CancelEventArgs` | Cancellable operations | `Cancel` |
| `MouseEventArgs` | Mouse input | `Position`, `Flags`, `Handled` |

### Command Context

When handling command events, rich context is available through `ICommandContext`:

```csharp
public interface ICommandContext
{
    Command Command { get; set; }      // The command being invoked
    View? Source { get; set; }         // The view that first invoked the command
    IInputBinding? Binding { get; }    // The binding that triggered the command
}
```

### Binding Types and Pattern Matching

Terminal.Gui provides three binding types. Use pattern matching to access binding-specific details:

```csharp
public override bool OnAccepting(object? sender, CommandEventArgs e)
{
    // Determine what triggered the command
    switch (e.Context?.Binding)
    {
        case KeyBinding kb:
            // Keyboard-triggered
            Key key = kb.Key;
            break;

        case MouseBinding mb:
            // Mouse-triggered
            Point position = mb.MouseEvent.Position;
            MouseFlags flags = mb.MouseEvent.Flags;
            break;

        case InputBinding ib:
            // Programmatic invocation
            object? data = ib.Data;
            break;
    }
    return false;
}

// Or use property patterns for concise access:
if (e.Context?.Binding is MouseBinding { MouseEvent: { } mouse })
{
    Point position = mouse.Position;
}
```

### Source Tracking During Propagation

Understanding the difference between sources is important during event propagation:

| Property | Description | Changes During Propagation? |
|----------|-------------|----------------------------|
| `ICommandContext.Source` | View that first invoked the command | No (constant) |
| `IInputBinding.Source` | View where binding was defined | No (constant) |
| `sender` (event parameter) | View currently raising the event | **Yes** |

```csharp
public override bool  OnAccepting(object? sender, CommandEventArgs e)
{
    // sender = current view raising the event (changes as it bubbles)
    // e.Context?.Source = original view that started the command (constant)

    View? currentView = sender as View;
    View? originalView = e.Context?.Source;
    ...
}
```

---

## Best Practices

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Pre-change event | `<Property>Changing` | `TextChanging`, `SourceChanging` |
| Post-change event | `<Property>Changed` | `TextChanged`, `SourceChanged` |
| Pre-change virtual | `On<Property>Changing` | `OnTextChanging` |
| Post-change virtual | `On<Property>Changed` | `OnTextChanged` |
| Handled property | `Handled` | `args.Handled = true` |

### Implementation Guidelines

1. **Virtual methods return `bool`** for cancellable operations (return `true` to cancel)
2. **Virtual methods return `void`** for post-change notifications (cannot cancel)
3. **Always call virtual method BEFORE raising the event** (subclasses get priority)
4. **Execute default behavior AFTER both checks pass**
5. **Unsubscribe in Dispose** to prevent memory leaks

```csharp
// ✅ CORRECT order: Virtual → Event → Default behavior
protected void DoSomething()
{
    SomeEventArgs args = new ();

    // 1. Virtual method first
    if (OnDoingSomething(args))
    {
        return; // Cancelled by subclass
    }

    // 2. Event second
    DoingSomething?.Invoke(this, args);
    if (args.Handled)
    {
        return; // Cancelled by subscriber
    }

    // 3. Default behavior 
    ExecuteDefaultBehavior();

    // 4. Post-change notification (if applicable)
    OnDidSomething(new DidSomethingEventArgs(...));
    DidSomething?.Invoke(this, new DidSomethingEventArgs(...));
}
```

---

## Common Pitfalls

### 1. Memory Leaks from Unsubscribed Events

```csharp
// ❌ BAD: Potential memory leak
view.Accepting += OnAccepting;

// ✅ GOOD: Unsubscribe in Dispose
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        view.Accepting -= OnAccepting;
    }
    base.Dispose(disposing);
}
```

### 2. Using Wrong Cancellation Property

```csharp
// ❌ WRONG: Using non-existent Cancel property
args.Cancel = true;  // ValueChangingEventArgs doesn't have Cancel!

// ✅ CORRECT: Use Handled for all CWP events
args.Handled = true;
```

### 3. Wrong Order of Virtual Method and Event

```csharp
// ❌ WRONG: Event raised before virtual method
DoingSomething?.Invoke(this, args);
if (OnDoingSomething(args)) { return; }  // Too late!

// ✅ CORRECT: Virtual method first, then event
if (OnDoingSomething(args)) { return; }
DoingSomething?.Invoke(this, args);
```

### 4. Forgetting to Check Both Cancellation Points

```csharp
// ❌ WRONG: Only checking virtual method
if (OnDoingSomething(args)) { return; }
DoingSomething?.Invoke(this, args);
ExecuteDefault(); // Bug: Event subscribers can't cancel!

// ✅ CORRECT: Check both virtual method AND event args
if (OnDoingSomething(args) || args.Handled) { return; }
DoingSomething?.Invoke(this, args);
if (args.Handled) { return; }
ExecuteDefault();
```

---

## External Resources

* [.NET Naming Guidelines - Names of Events](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members?redirectedfrom=MSDN#names-of-events)
* [.NET Design for Extensibility - Events and Callbacks](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/events-and-callbacks)
