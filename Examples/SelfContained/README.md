# Terminal.Gui C# SelfContained

This project aims to test the `Terminal.Gui` library to create a simple `self-contained` `single-file` GUI application in C#, ensuring that all its features are available.

## Modern Terminal.Gui API

This example uses the modern Terminal.Gui application model:

```csharp
ConfigurationManager.Enable (ConfigLocations.All);

IApplication app = Application.Create ();
app.Init ();

using ExampleWindow exampleWindow = new ();
string? userName = app.Run (exampleWindow) as string;

app.Dispose ();

Console.WriteLine ($@"Username: {userName}");
```

Key aspects of the modern model:
- Use `Application.Create()` to create an `IApplication` instance
- Call `app.Init()` to initialize the application
- Use `app.Run(view)` to run views with proper resource management
- Call `app.Dispose()` to clean up resources and restore the terminal
- Event handling uses `Accepting` event instead of legacy `Accept` event
- Set `e.Handled = true` in event handlers to prevent further processing

With `Debug` the `.csproj` is used and with `Release` the latest `nuget package` is used, either in `Solution Configurations` or in `Profile Publish`.

To publish the self-contained single file in `Debug` or `Release` mode, it is not necessary to select it in the `Solution Configurations`, just choose the `Debug` or `Release` configuration in the `Publish Profile`.

When executing the file directly from the self-contained single file and needing to debug it, it will be necessary to attach it to the debugger, just like any other standalone application. However, when trying to attach the file running on `Linux` or `macOS` to the debugger, it will issue the error "`Failed to attach to process: Unknown Error: 0x80131c3c`". This issue has already been reported on [Developer Community](https://developercommunity.visualstudio.com/t/Failed-to-attach-to-process:-Unknown-Err/10694351). Maybe it would be a good idea to vote in favor of this fix because I think `Visual Studio for macOS` is going to be discontinued and we need this fix to remotely attach a process running on `Linux` or `macOS` to `Windows 11`.