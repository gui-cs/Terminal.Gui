# Terminal.Gui Event Deep Dive

Terminal.Gui exposes and uses events in many places. This deep dive covers the patterns used, where they are used, and notes any exceptions.

## Tenets for Terminal.Gui Events (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **UI Interaction and Live Data Are Different Beasts** - TG distinguishes between events used for human interaction and events for live data. We don't believe in a one-size-fits-all eventing model. For UI interactions we use `EventHandler`. For data binding we think `INotifyPropertyChanged` is groovy. For some callbacks we use `Action<T>`.

## Lexicon and Taxonomy

* *Action*
* *Event*
* *Command*
* *Invoke*
* *Raise*
* *Listen*
* *Handle/Handling/Handled* - Applies to scenarios where an event can either be handled by an event listener (or override) vs not handled. Events that originate from a user action like mouse moves and key presses are examples. 
* *Cancel/Cancelling/Cancelled* - Applies to scenarios where something can be cancelled. Changing the `Orientation` of a `Slider` is cancelable.

## Useful External Documentation

* [.NET Naming Guidelines - Names of Events](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members?redirectedfrom=MSDN#names-of-events)
* [.NET Design for Extensibility - Events and Callbacks](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/events-and-callbacks)
* [C# Event Implementation Fundamentals, Best Practices and Conventions](https://www.codeproject.com/Articles/20550/C-Event-Implementation-Fundamentals-Best-Practices)

## Naming

TG follows the *naming* advice provided in [.NET Naming Guidelines - Names of Events](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members?redirectedfrom=MSDN#names-of-events).

## Common Event Patterns

### OnEvent/Event

The primary pattern for events is the `OnEvent/Event` idiom. 

* Implement a helper method for raising the event: `RaisexxxEvent`.
  * If the event is cancelable, the return type should be either `bool` or `bool?`.
  * Can be `private`, `internal`, or `public` depending on the situation. `internal` should only be used to enable unit tests.
* Raising an event involves FIRST calling the `protected virtual` method, THEN invoking 
the `EventHandler`.

### Action

We use the `Action<T>` idiom sparingly. 

### INotifyPropertyChanged

We support `INotifyPropertyChanged` in cases where data binding is relevant.

## Cancellable Work Pattern

Often there is a need for a class to do some work that can either

- proceed in the default manner.
- proceed in the default manner with some modification.
- be cancelled.

Frameworks often use inheritance and the overriding of `virtual` methods for this, but doing so requires overrides to know implementation details of the base classes in order to correctly call `base`. Instead, Terminal.Gui defines and uses the *Cancellable Work Pattern* as the primary means of supporting this.

The *Cancellable Work Pattern* flows as follows: 

- A method is called indicating work needs to be done, e.g. `view.DoClearViewport`.
- This method raises a **Cancelable Event** (see above). 
- If the cancelable event was cancelled, processing stops and the method returns.
- Otherwise, the work happens (in this case, the Viewport is cleared).
- A non-cancelable event is raised.

("Event" means an `OnEvent/Event`-style event as described above).

To proceed in the default manner, a consumer does nothing.

```cs
view.Draw ();
```

To proceed in the default manner with some modification to the default work, a consumer would subscribe to both events to do additional work before and after the default work, as appropriate.

```cs
view.ClearingViewport += (s, e) =>
            {
                if (s is View sender)
                {
                    sender.FillRect (sender.Viewport, Glyphs.Stipple);
                }
                e.Cancel = true;
            };
view.Draw ();
```

To cancel, a consumer would subscribe to just the cancelable event and cancel it.

```cs
view.ClearingViewport += (s, e) => { e.Cancel = true; };
view.Draw ();
```

### **Event Raising Method** - Standard pattern for code that raises events

The standard pattern for the implementation within a Terminal.Gui class that can raise an event is to implement a `Raisexxx ()` method (where `xxx` is the name of the event).or Depending on the use-case, this event can be `private`, `protected`, or `public`.

A typical implementation looks like this:

```cs
protected bool? RaiseCommandNotBound (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };

    // Best practice is to invoke the virtual method first.
    // This allows derived classes to handle the event and potentially cancel it.
    // For robustness=-sake, even if the virtual method returns true, if the args 
    // indicate the event should be cancelled, we honor that.
    if (OnCommandNotBound (args) || args.Cancel)
    {
        return true;
    }

    // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
    CommandNotBound?.Invoke (this, args);

    return CommandNotBound is null ? null : args.Cancel;
}
```


### **Before** - If any pre-conditions are met raise the "pre-event", typically named in the form of "xxxChanging". e.g.

  - A `protected virtual` method is called. These method is named `OnxxxChanging` and the base implementation simply does `return false`.
  - If the `OnxxxChanging` method returns `true` it means a derived class canceled the event. Processing should stop.
  - Otherwise, the `xxxChanging` event is invoked via `xxxChanging?.Invoke(args)`. If `args.Cancel/Handled == true` it means a subscriber has cancelled the event. Processing should stop.

The **Before** event, by definition is cancellable. 

Generally, the terms "Canceled" or "Handled" both mean the same thing: **Do not proceed**. In some cases, Canceled makes sense (e.g. if the "work" is a change to the state of an object, like whether a View is oriented horizontally or vertically). In other cases, Handled makes more sense (e.g. if the "work" is to process some user input, like a key press).


### **During** - Do work.

### **After** - Raise the "post-event", typically named in the form of "xxxChanged"

  - A `protected virtual` method is called. This method is named `OnxxxChanged` has a return type of `void`.
  - The `xxxChanged` event is invoked via `xxxChanging?.Invoke(args)`. 

The `OrientationHelper` class supporting `IOrientation` and a `View` having an `Orientation` property illustrates the preferred TG pattern for cancelable events.

```cs
   /// <summary>
   ///     Gets or sets the orientation of the View.
   /// </summary>
   public Orientation Orientation
   {
       get => _orientation;
       set
       {
           if (_orientation == value)
           {
               return;
           }

           // Best practice is to call the virtual method first.
           // This allows derived classes to handle the event and potentially cancel it.
           if (_owner?.OnOrientationChanging (value, _orientation) ?? false)
           {
               return;
           }

           // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
           CancelEventArgs<Orientation> args = new (in _orientation, ref value);
           OrientationChanging?.Invoke (_owner, args);

           if (args.Cancel)
           {
               return;
           }

           // If the event is not canceled, update the value.
           Orientation old = _orientation;

           if (_orientation != value)
           {
               _orientation = value;

               if (_owner is { })
               {
                   _owner.Orientation = value;
               }
           }

           // Best practice is to call the virtual method first, then raise the event.
           _owner?.OnOrientationChanged (_orientation);
           OrientationChanged?.Invoke (_owner, new (in _orientation));
       }
   }
```

## `bool` or `bool?` 

## The `Selecting` and `Accepting` Events

@Terminal.Gui.View.Selecting and @Terminal.Gui.View.Accepting provide an opinionated mechanism for the most common interactions: Selecting the state and accepting the state.

For a View like @Terminal.Gui.Button, where there is no state to select, only `Accepting` is relevant. For a View like @Terminal.Gui.ListView, where the user can change which list items are selected independently from indicating "do something based on which items are selected" both concepts are relevant: `Selecting` is raised when the `ListView`, and `Accepting` is raised when the user double-clicks or presses `Key.Enter` to accept the selection.



 
## Command Routing

@Terminal.Gui.Command defines a set of standard things @Terminal.Gui.View objects can do. Views declare which commands they support by calling @Terminal.Gui.View.AddCommmand and implementing command handlers.

For example, @Terminal.Gui.HexView supports moving the cursor to the top of the current page by defining a command handler for `Command.StartOfPage`, which is bound to `Ctrl+Up`:

```cs
AddCommand (Command.StartOfPage, () => MoveUp (BytesPerLine * ((int)(Address - Viewport.Y) / BytesPerLine)));
KeyBindings.Add (Key.CursorUp.WithCtrl, Command.StartOfPage);
```

Commands are cancellable. The command handler should `null` if no command was found; command routing should continue. `false` if the command was invoked and was not handled (or cancelled); command routing should continue. `true` if the command was invoked the command was handled (or cancelled); command handling should stop.

In most cases, how commands are routed to Views is simple: @Terminal.Gui.View.InvokeCommand is called, which calls the command handler. If there is no command handler, then @Terminal.Gui.View.RaiseCommandNotBound is called, which raises the @Terminal.Gui.View.CommandNotBound event.

@Terminal.Gui.Command.Select and @Terminal.Gui.Command.Accept support additional routing logic, provided by @Terminal.Gui.View.RaiseSelecting and @Terminal.Gui.View.RaiseAccepting respectively. Each of these methods raises the respective events on the View and if the View's command handler returns either `null` or `false` routes the command to the View's @Terminal.Gui.View.SuperView.

> `Command.Accept` has one additional behavior: If any peer-View is a @Terminal.Gui.Button with `@Terminal.Gui.Button.IsDefault == true` the Command will be routed to that View before routing up the SuperView hierarchy. This enables default button handling.



