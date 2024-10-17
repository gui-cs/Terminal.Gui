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

## `EventHandler` style event best-practices

* Implement a helper method for raising the event: `RaisexxxEvent`.
  * If the event is cancelable, the return type should be either `bool` or `bool?`.
  * Can be `private`, `internal`, or `public` depending on the situation. `internal` should only be used to enable unit tests.
* Raising an event involves FIRST calling the `protected virtual` method, THEN invoking the `EventHandler.

## `Action<T>` style callback best-practices

- tbd

## `INotifyPropertyChanged` style notification best practices

- tbd

## Common Patterns

The primary pattern for events is the `event/EventHandler` idiom. We use the `Action<T>` idiom sparingly. We support `INotifyPropertyChanged` in cases where data binding is relevant.



## Cancellable Event Pattern

A cancellable event is really two events and some activity that takes place between those events. The "pre-event" happens before the activity. The activity then takes place (or not). If the activity takes place, then the "post-event" is typically raised. So, to be precise, no event is being cancelled even though we say we have a cancellable event. Rather, the activity that takes place between the two events is what is cancelled — and likely prevented from starting at all.

### **Before** - If any pre-conditions are met raise the "pre-event", typically named in the form of "xxxChanging". e.g.

  - A `protected virtual` method is called. This method is named `OnxxxChanging` and the base implementation simply does `return false`.
  - If the `OnxxxChanging` method returns `true` it means a derived class canceled the event. Processing should stop.
  - Otherwise, the `xxxChanging` event is invoked via `xxxChanging?.Invoke(args)`. If `args.Cancel/Handled == true` it means a subscriber has cancelled the event. Processing should stop.


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

 

