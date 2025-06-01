---
uid: Terminal.Gui.App
summary: The `App` namespace holds @Terminal.Gui.Application and associated classes.
---

@Terminal.Gui.Application provides the main entry point and core application logic for Terminal.Gui applications. This static singleton class is responsible for initializing and shutting down the Terminal.Gui environment, managing the main event loop, handling input and output, and providing access to global application state.

Typical usage involves calling `Application.Init()` to initialize the application, creating and running a `Window`, and then calling `Application.Shutdown()` to clean up resources. The class also provides methods for culture support, idle handlers, and rendering the application state as a string.

## Example Usage

```csharp
Application.Init();
var win = new Window()
{
    Title = $"Example App ({Application.QuitKey} to quit)"
};
Application.Run(win);
win.Dispose();
Application.Shutdown();
```

## See Also

- [Logging](~/docs/logging.md)
- [Multitasking](~/docs/multitasking.md)
