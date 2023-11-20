# Threading
It is common for a developer to run one or more background tasks on a separate
`Thread` (or `Task`). For example to periodically fetch data. If you want to
make changes to any `View` from this background task, you must issue the change
on the main Terminal.Gui UI thread.

**Updates to the user interface should only be performed on the main UI thread.**

This is required to prevent errors, for example if Terminal.Gui is trying to
render a `View` while its contents are actively being changed from another Thread.

## How to issue call to main thread
To run an operation on the main thread from a background task use:

```csharp
Application.MainLoop.Invoke(()=>{/* Your Code Here*/});
```

For example:

```csharp
using Terminal.Gui;

Application.Init();

var w = new Window();

// Start a background task
Task.Run(() =>
{
    while(true)
    {
        // Do some computation
        Task.Delay(30).Wait();

        // Issue a call to the main thread
        Application.MainLoop.Invoke(() =>
        {
            //This code resolves in the main UI thread
            w.RemoveAll();
            w.Add(new Label("Time is:" + DateTime.Now));

            w.SetNeedsDisplay();
        });
    }
});

Application.Run(w);

Application.Shutdown();
```