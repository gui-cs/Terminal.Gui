# Terminal.Gui Event Deep Dive

Terminal.Gui exposes and uses events in many places. This deep dive covers the patterns used, where they are used, and notes any exceptions.

## See Also

* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Command Deep Dive](command.md)

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


