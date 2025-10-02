# Terminal.Gui Event Deep Dive

This document provides a comprehensive overview of how events work in Terminal.Gui. For the conceptual overview of the Cancellable Work Pattern, see [Cancellable Work Pattern](cancellable-work-pattern.md).

## See Also

* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Command Deep Dive](command.md)
* [Lexicon & Taxonomy](lexicon.md)

## Lexicon and Taxonomy

[!INCLUDE [Events Lexicon](~/includes/events-lexicon.md)]

## Event Categories

Terminal.Gui uses several types of events:

1. **UI Interaction Events**: Events triggered by user input (keyboard, mouse)
2. **View Lifecycle Events**: Events related to view creation, activation, and disposal
3. **Property Change Events**: Events for property value changes
4. **Drawing Events**: Events related to view rendering
5. **Command Events**: Events for command execution and workflow control

## Event Patterns

### 1. Cancellable Work Pattern (CWP)

The [Cancellable Work Pattern (CWP)](cancellable-work-pattern.md) is a core pattern in Terminal.Gui that provides a consistent way to handle cancellable operations. An "event" has two components:

1. **Virtual Method**: `protected virtual OnMethod()` that can be overridden in a subclass so the subclass can participate
2. **Event**: `public event EventHandler<>` that allows external subscribers to participate

The virtual method is called first, letting subclasses have priority. Then the event is invoked.

Optional CWP Helper Classes are provided to provide consistency.

#### Manual CWP Implementation

The basic CWP pattern combines a protected virtual method with a public event:

```csharp
public class MyView : View
{
    // Public event
    public event EventHandler<MouseEventArgs>? MouseEvent;

    // Protected virtual method
    protected virtual bool OnMouseEvent(MouseEventArgs args)
    {
        // Return true to handle the event and stop propagation
        return false;
    }

    // Internal method to raise the event
    internal bool RaiseMouseEvent(MouseEventArgs args)
    {
        // Call virtual method first
        if (OnMouseEvent(args) || args.Handled)
        {
            return true;
        }

        // Then raise the event
        MouseEvent?.Invoke(this, args);
        return args.Handled;
    }
}
```

#### CWP with Helper Classes

Terminal.Gui provides static helper classes to implement CWP:

#### Property Changes

For property changes, use `CWPPropertyHelper.ChangeProperty`:

```csharp
public class MyView : View
{
    private string _text = string.Empty;
    public event EventHandler<ValueChangingEventArgs<string>>? TextChanging;
    public event EventHandler<ValueChangedEventArgs<string>>? TextChanged;

    public string Text
    {
        get => _text;
        set
        {
            if (CWPPropertyHelper.ChangeProperty(
                currentValue: _text,
                newValue: value,
                onChanging: args => OnTextChanging(args),
                changingEvent: TextChanging,
                onChanged: args => OnTextChanged(args),
                changedEvent: TextChanged,
                out string finalValue))
            {
                _text = finalValue;
            }
        }
    }

    // Virtual method called before the change
    protected virtual bool OnTextChanging(ValueChangingEventArgs<string> args)
    {
        // Return true to cancel the change
        return false;
    }

    // Virtual method called after the change
    protected virtual void OnTextChanged(ValueChangedEventArgs<string> args)
    {
        // React to the change
    }
}
```

#### Workflows

For general workflows, use `CWPWorkflowHelper`:

```csharp
public class MyView : View
{
    public bool? ExecuteWorkflow()
    {
        ResultEventArgs<bool> args = new();
        return CWPWorkflowHelper.Execute(
            onMethod: args => OnExecuting(args),
            eventHandler: Executing,
            args: args,
            defaultAction: () =>
            {
                // Main execution logic
                DoWork();
                args.Result = true;
            });
    }

    // Virtual method called before execution
    protected virtual bool OnExecuting(ResultEventArgs<bool> args)
    {
        // Return true to cancel execution
        return false;
    }

    public event EventHandler<ResultEventArgs<bool>>? Executing;
}
```

### 2. Action Callbacks

For simple callbacks without cancellation, use `Action`. For example, in `Shortcut`:

```csharp
public class Shortcut : View
{
    /// <summary>
    ///     Gets or sets the action to be invoked when the shortcut key is pressed or the shortcut is clicked on with the
    ///     mouse.
    /// </summary>
    /// <remarks>
    ///     Note, the <see cref="View.Accepting"/> event is fired first, and if cancelled, the event will not be invoked.
    /// </remarks>
    public Action? Action { get; set; }

    internal virtual bool? DispatchCommand(ICommandContext? commandContext)
    {
        bool cancel = base.DispatchCommand(commandContext) == true;

        if (cancel)
        {
            return true;
        }

        if (Action is { })
        {
            Logging.Debug($"{Title} ({commandContext?.Source?.Title}) - Invoke Action...");
            Action.Invoke();

            // Assume if there's a subscriber to Action, it's handled.
            cancel = true;
        }

        return cancel;
    }
}
```

### 3. Property Change Notifications

For property change notifications, implement `INotifyPropertyChanged`. For example, in `Aligner`:

```csharp
public class Aligner : INotifyPropertyChanged
{
    private Alignment _alignment;
    public event PropertyChangedEventHandler? PropertyChanged;

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            _alignment = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alignment)));
        }
    }
}
```

### 4. Event Propagation

Events in Terminal.Gui often propagate through the view hierarchy. For example, in `Button`, the `Selecting` and `Accepting` events are raised as part of the command handling process:

```csharp
private bool? HandleHotKeyCommand (ICommandContext commandContext)
{
    bool cachedIsDefault = IsDefault; // Supports "Swap Default" in Buttons scenario where IsDefault changes

    if (RaiseSelecting (commandContext) is true)
    {
        return true;
    }

    bool? handled = RaiseAccepting (commandContext);

    if (handled == true)
    {
        return true;
    }

    SetFocus ();

    // If Accept was not handled...
    if (cachedIsDefault && SuperView is { })
    {
        return SuperView.InvokeCommand (Command.Accept);
    }

    return false;
}
```

This example shows how `Button` first raises the `Selecting` event, and if not canceled, proceeds to raise the `Accepting` event. If `Accepting` is not handled and the button is the default, it invokes the `Accept` command on the `SuperView`, demonstrating event propagation up the view hierarchy.

## Event Context

### Event Arguments

Terminal.Gui provides rich context through event arguments. For example, `CommandEventArgs`:

```csharp
public class CommandEventArgs : EventArgs
{
    public ICommandContext? Context { get; set; }
    public bool Handled { get; set; }
    public bool Cancel { get; set; }
}
```

### Command Context

Command execution includes context through `ICommandContext`:

```csharp
public interface ICommandContext
{
    View Source { get; }
    object? Parameter { get; }
    IDictionary<string, object> State { get; }
}
```

## Best Practices

1. **Event Naming**:
   - Use past tense for completed events (e.g., `Clicked`, `Changed`)
   - Use present tense for ongoing events (e.g., `Clicking`, `Changing`)
   - Use "ing" suffix for cancellable events

2. **Event Handler Implementation**:
   - Keep handlers short and focused
   - Use async/await for long-running tasks
   - Unsubscribe from events in Dispose
   - Use weak event patterns for long-lived subscriptions

3. **Event Context**:
   - Provide rich context in event args
   - Include source view and binding details
   - Add view-specific state when needed

4. **Event Propagation**:
   - Use appropriate propagation mechanisms
   - Avoid unnecessary event bubbling
   - Consider using `PropagatedCommands` for hierarchical views

## Common Pitfalls

1. **Memory Leaks**:
   ```csharp
   // BAD: Potential memory leak
   view.Activating += OnActivating;
   
   // GOOD: Unsubscribe in Dispose
   protected override void Dispose(bool disposing)
   {
       if (disposing)
       {
           view.Activating -= OnActivating;
       }
       base.Dispose(disposing);
   }
   ```

2. **Incorrect Event Cancellation**:
   ```csharp
   // BAD: Using Cancel for event handling
   args.Cancel = true;  // Wrong for MouseEventArgs
   
   // GOOD: Using Handled for event handling
   args.Handled = true;  // Correct for MouseEventArgs
   
   // GOOD: Using Cancel for operation cancellation
   args.Cancel = true;  // Correct for CancelEventArgs
   ```

3. **Missing Context**:
   ```csharp
   // BAD: Missing context
   Activating?.Invoke(this, new CommandEventArgs());
   
   // GOOD: Including context
   Activating?.Invoke(this, new CommandEventArgs { Context = ctx });
   ```

## Useful External Documentation

* [.NET Naming Guidelines - Names of Events](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members?redirectedfrom=MSDN#names-of-events)
* [.NET Design for Extensibility - Events and Callbacks](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/events-and-callbacks)
* [C# Event Implementation Fundamentals, Best Practices and Conventions](https://www.codeproject.com/Articles/20550/C-Event-Implementation-Fundamentals-Best-Practices)

## Naming

TG follows the *naming* advice provided in [.NET Naming Guidelines - Names of Events](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members?redirectedfrom=MSDN#names-of-events).

## Known Issues

### Proposed Enhancement: Command Propagation

The *Cancellable Work Pattern* in `View.Command` currently supports local `Command.Activate` and propagating `Command.Accept`. To address hierarchical coordination needs (e.g., `MenuBarv2` popovers, `Dialog` closing), a `PropagatedCommands` property is proposed (Issue #4050):

- **Change**: Add `IReadOnlyList<Command> PropagatedCommands` to `View`, defaulting to `[Command.Accept]`. `Raise*` methods propagate if the command is in `SuperView?.PropagatedCommands` and `args.Handled` is `false`.
- **Example**:

  ```csharp
  public IReadOnlyList<Command> PropagatedCommands { get; set; } = new List<Command> { Command.Accept };
  protected bool? RaiseAccepting(ICommandContext? ctx)
  {
      CommandEventArgs args = new() { Context = ctx };
      if (OnAccepting(args) || args.Handled)
      {
          return true;
      }
      Accepting?.Invoke(this, args);
      if (!args.Handled && SuperView?.PropagatedCommands.Contains(Command.Accept) == true)
      {
          return SuperView.InvokeCommand(Command.Accept, ctx);
      }
      return Accepting is null ? null : args.Handled;
  }
  ```

- **Impact**: Enables `Command.Activate` propagation for `MenuBarv2` while preserving `Command.Accept` propagation, maintaining decoupling and avoiding noise from irrelevant commands.

### **Conflation in FlagSelector**:
   - **Issue**: `CheckBox.Activating` triggers `Accepting`, conflating state change and confirmation.
   - **Recommendation**: Refactor to separate `Activating` and `Accepting`:
     ```csharp
     checkbox.Activating += (sender, args) =>
     {
         if (RaiseAccepting(args.Context) is true)
         {
             args.Handled = true;
         }
     };
     ```

### **Propagation Limitations**:
   - **Issue**: Local `Command.Activate` restricts `MenuBarv2` coordination; `Command.Accept` uses hacks (#3925).
   - **Recommendation**: Adopt `PropagatedCommands` to enable targeted propagation, as proposed.

### **Complexity in Multi-Phase Workflows**:
   - **Issue**: `View.Draw`'s multi-phase workflow can be complex for developers to customize.
   - **Recommendation**: Provide clearer phase-specific documentation and examples.
