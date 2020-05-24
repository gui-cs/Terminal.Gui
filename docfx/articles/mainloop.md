Event Processing and the Application Main Loop
==============================================

The method `Application.Run` that we covered before will wait for
events from either the keyboard or mouse and route those events to the
proper view.

The job of waiting for events and dispatching them in the
`Application` is implemented by an instance of the
[`MainLoop`]()
class.

Mainloops are a common idiom in many user interface toolkits so many
of the concepts will be familiar to you if you have used other
toolkits before.

This class provides the following capabilities:

* Keyboard and mouse processing
* .NET Async support
* Timers processing
* Invoking of UI code from a background thread
* Idle processing handlers
* Possibility of integration with other mainloops.
* On Unix systems, it can monitor file descriptors for readability or writability.

The `MainLoop` property in the the
[`Application`](../api/Terminal.Gui/Terminal.Gui.Application.html)
provides access to these functions.

When your code invokes `Application.Run (Toplevel)`, the application
will prepare the current
[`Toplevel`](../api/Terminal.Gui/Terminal.Gui.Toplevel.html) instance by
redrawing the screen appropriately and then calling the mainloop to
run.    

You can configure the Mainloop before calling Application.Run, or you
can configure the MainLoop in response to events during the execution.

The keyboard inputs is dispatched by the application class to the
current TopLevel window this is covered in more detail in the
[Keyboard Event Processing](keyboard.html) document.


Async Execution
---------------

On startup, the `Application` class configured the .NET Asynchronous
machinery to allow you to use the `await` keyword to run tasks in the
background and have the execution of those tasks resume on the context
of the main thread running the main loop.

Once you invoke `Application.Main` the async machinery will be ready
to use, and you can merely call methods using `await` from your main
thread, and the awaited code will resume execution on the main
thread. 

Timers Processing
-----------------

You can register timers to be executed at specified intervals by
calling the [`AddTimeout`]() method, like this:

```csharp
void UpdateTimer ()
{
	time.Text = DateTime.Now.ToString ();
}

var token = Application.MainLoop.AddTimeout (TimeSpan.FromSeconds (20), UpdateTimer);
```

The return value from AddTimeout is a token value that you can use if
you desire to cancel the timer before it runs:

```csharup
Application.MainLoop.RemoveTimeout (token);
```

Idle Handlers
-------------

You can register code to be executed when the application is idling
and there are no events to process by calling the
[`AddIdle`]()
method.  This method takes as a parameter a function that will be
invoked when the application is idling.  

Idle functions should return `true` if they should be invoked again,
and `false` if the idle invocations should stop.

Like the timer APIs, the return value is a token that can be used to
cancel the scheduled idle function from being executed.

Threading
---------

Like other UI toolkits, Terminal.Gui is generally not thread safe.
You should avoid calling methods in the UI classes from a background
thread as there is no guarantee that they will not corrupt the state
of the UI application.  

Generally, as there is not much state, you will get lucky, but the
application will not behave properly.

You will be served better off by using C# async machinery and the
various APIs in the `System.Threading.Tasks.Task` APIs.   But if you
absolutely must work with threads on your own you should only invoke
APIs in Terminal.Gui from the main thread.

To make this simple, you can use the `Application.MainLoop.Invoke`
method and pass an `Action`.  This action will be queued for execution
on the main thread at an appropriate time and will run your code
there.

For example, the following shows how to properly update a label from a
background thread:

```
void BackgroundThreadUpdateProgress ()
{
	Application.MainLoop.Invoke (() => {
		progress.Text = $"Progress: {bytesDownloaded/totalBytes}";
        });
}
```

Integration With Other Main Loop Drivers
----------------------------------------

It is possible to run the main loop in a way that it does not take
over control of your application, but rather in a cooperative way.

To do this, you must use the lower-level APIs in `Application`: the
`Begin` method to prepare a toplevel for execution, followed by calls
to `MainLoop.EventsPending` to determine whether the events must be
processed, and in that case, calling `RunLoop` method and finally
completing the process  by calling `End`.

The method `Run` is implemented like this:

```
void Run (Toplevel top)
{
	var runToken = Begin (view);
	RunLoop (runToken);
	End (runToken);
}
```

Unix File Descriptor Monitoring
-------------------------------

On Unix, it is possible to monitor file descriptors for input being
available, or for the file descriptor being available for data to be
written without blocking the application.

To do this, you on Unix, you can cast the `MainLoop` instance to a
[`UnixMainLoop`]()
and use the `AddWatch` method to register an interest on a particular
condition.
