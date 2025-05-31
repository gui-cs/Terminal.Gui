# Event Processing and the Application Main Loop

_See also [Cross-platform Driver Model](drivers.md)_

The method `Application.Run` will wait for events from either the keyboard or mouse and route those events to the proper view.

The job of waiting for events and dispatching them in the `Application` is implemented by an instance of the Main Loop.

Main loops are a common idiom in many user interface toolkits so many of the concepts will be familiar to you if you have used other toolkits before.

This class provides the following capabilities:

* Keyboard and mouse processing
* .NET Async support
* Timers processing
* Idle processing handlers
* Invoking UI code from a background thread

The `MainLoop` property in the the [`Application`](~/api/Terminal.Gui.App.Application.yml)
provides access to these functions.

When `Application.Run (Toplevel)` is called, the application will prepare the current
[`Toplevel`](~/api/Terminal.Gui.Views.Toplevel.yml) instance by redrawing the screen appropriately and then starting the main loop.

Configure the Mainloop before calling Application.Run, or  configure the MainLoop in response to events during the execution.

Keyboard input is dispatched by the Application class to the
current TopLevel window. This is covered in more detail in the
[Keyboard Event Processing](keyboard.md) document.

Async Execution
---------------

On startup, the `Application` class configures the .NET Asynchronous
machinery to allow the use of the `await` keyword to run tasks in the
background and have the execution of those tasks resume on the context
of the main thread running the main loop.

Timers Processing
-----------------

Timers can be set to be executed at specified intervals by calling the [`AddTimeout`]() method, like this:

```csharp
void UpdateTimer ()
{
	time.Text = DateTime.Now.ToString ();
}

var token = Application.MainLoop.AddTimeout (TimeSpan.FromSeconds (20), UpdateTimer);
```

The return value from AddTimeout is a token value that can be used to cancel the timer:

```csharup
Application.MainLoop.RemoveTimeout (token);
```

Idle Handlers
-------------

[`AddIdle`]() registers a function to be executed when the application is idling and there are no events to process. Idle functions should return `true` if they should be invoked again,
and `false` if the idle invocations should stop.

Like the timer APIs, the return value is a token that can be used to cancel the scheduled idle function from being executed.

Threading
---------

Like most UI toolkits, Terminal.Gui should be assumed to not be thread-safe. Avoid calling methods in the UI classes from a background thread as there is no guarantee they will not corrupt the state of the UI application. 

Instead, use C# async APIs (e.g. `await` and `System.Threading.Tasks.Task`). Only invoke
APIs in Terminal.Gui from the main thread by using the `Application.Invoke`
method to pass an `Action` that will be queued for execution on the main thread at an appropriate time.

For example, the following shows how to properly update a label from a background thread:

```cs
void BackgroundThreadUpdateProgress ()
{
	Application.Invoke (() => {
		progress.Text = $"Progress: {bytesDownloaded/totalBytes}";
        });
}
```

Low-Level Application APIs
----------------------------------------

It is possible to run the main loop in a cooperative way: Use the lower-level APIs in `Application`: the `Begin` method to prepare a toplevel for execution, followed by calls
to `MainLoop.EventsPending` to determine whether the events must be processed, and in that case, calling `RunLoop` method and finally completing the process  by calling `End`.

The method `Run` is implemented like this:

```cs
void Run (Toplevel top)
{
	var runToken = Begin (view);
	RunLoop (runToken);
	End (runToken);
}
```