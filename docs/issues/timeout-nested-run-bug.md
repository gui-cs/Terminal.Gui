# Bug: Timeouts Lost in Nested Application.Run() Calls

## Summary

Timeouts scheduled via `IApplication.AddTimeout()` do not fire correctly when a nested modal dialog is shown using `Application.Run()`. This causes demo keys (and other scheduled timeouts) to be lost when MessageBox or other dialogs are displayed.

## Environment

- **Terminal.Gui Version**: 2.0 (current main branch)
- **OS**: Windows/Linux/macOS (all platforms affected)
- **.NET Version**: .NET 8

## Steps to Reproduce

### Minimal Repro Code

```csharp
using Terminal.Gui;

var app = Application.Create();
app.Init("FakeDriver");

var mainWindow = new Window { Title = "Main Window" };
var dialog = new Dialog { Title = "Dialog", Buttons = [new Button { Text = "Ok" }] };

// Schedule timeout at 100ms to show dialog
app.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
{
    Console.WriteLine("Enter timeout - showing dialog");
    app.Run(dialog);  // This blocks in a nested run loop
    Console.WriteLine("Dialog closed");
    return false;
});

// Schedule timeout at 200ms to close dialog (should fire while dialog is running)
app.AddTimeout(TimeSpan.FromMilliseconds(200), () =>
{
    Console.WriteLine("ESC timeout - closing dialog");
    app.RequestStop(dialog);
    return false;
});

// Stop main window after dialog closes
app.AddTimeout(TimeSpan.FromMilliseconds(300), () =>
{
    app.RequestStop();
    return false;
});

app.Run(mainWindow);
app.Dispose();
```

### Expected Behavior

- At 100ms: First timeout fires, shows dialog
- At 200ms: Second timeout fires **while dialog is running**, closes dialog
- At 300ms: Third timeout fires, closes main window
- Application exits cleanly

### Actual Behavior

- At 100ms: First timeout fires, shows dialog
- At 200ms: **Second timeout NEVER fires** - dialog stays open indefinitely
- Application hangs waiting for dialog to close

## Root Cause

The bug is in `TimedEvents.RunTimersImpl()`:

```csharp
private void RunTimersImpl()
{
    long now = GetTimestampTicks();
    SortedList<long, Timeout> copy;

    lock (_timeoutsLockToken)
    {
        copy = _timeouts;           // ? Copy ALL timeouts
        _timeouts = new();          // ? Clear the queue
    }

    foreach ((long k, Timeout timeout) in copy)
    {
        if (k < now)
        {
            if (timeout.Callback!())  // ? This can block for a long time
            {
                AddTimeout(timeout.Span, timeout);
            }
        }
        else
        {
            lock (_timeoutsLockToken)
            {
                _timeouts.Add(NudgeToUniqueKey(k), timeout);
            }
        }
    }
}
```

### The Problem

1. **All timeouts are removed from the queue** at the start and copied to a local variable
2. **Callbacks are executed sequentially** in the foreach loop
3. **When a callback blocks** (e.g., `app.Run(dialog)`), the entire `RunTimersImpl()` method is paused
4. **Future timeouts are stuck** in the local `copy` variable, inaccessible to the nested run loop
5. The nested dialog's `RunTimers()` calls see an **empty timeout queue**
6. Timeouts scheduled before the nested run never fire during the nested run

### Why `now` is captured only once

Additionally, `now = GetTimestampTicks()` is captured once at the start. If a callback takes a long time, `now` becomes stale, and the time evaluation `k < now` uses outdated information.

## Impact

This bug affects:

1. **Example Demo Keys**: The `ExampleDemoKeyStrokesAttribute` feature doesn't work correctly when examples show MessageBox or dialogs. The ESC key to close dialogs is lost.

2. **Any automated testing** that uses timeouts to simulate user input with modal dialogs

3. **Application code** that schedules timeouts expecting them to fire during nested `Application.Run()` calls

## Real-World Example

The bug was discovered in `Examples/Example/Example.cs` which has this demo key sequence:

```csharp
[assembly: ExampleDemoKeyStrokes(
    KeyStrokes = ["a", "d", "m", "i", "n", "Tab", 
                  "p", "a", "s", "s", "w", "o", "r", "d", 
                  "Enter",  // ? Opens MessageBox
                  "Esc"],   // ? Should close MessageBox, but never fires
    Order = 1)]
```

When "Enter" is pressed, it triggers:
```csharp
btnLogin.Accepting += (s, e) =>
{
    if (userNameText.Text == "admin" && passwordText.Text == "password")
    {
        MessageBox.Query(App, "Logging In", "Login Successful", "Ok");
        // ? This blocks in a nested Application.Run() call
        // The ESC timeout scheduled for 1600ms never fires
    }
};
```

## Solution

Rewrite `TimedEvents.RunTimersImpl()` to process timeouts **one at a time** instead of batching them:

```csharp
private void RunTimersImpl()
{
    long now = GetTimestampTicks();

    // Process due timeouts one at a time, without blocking the entire queue
    while (true)
    {
        Timeout? timeoutToExecute = null;
        long scheduledTime = 0;

        // Find the next due timeout
        lock (_timeoutsLockToken)
        {
            if (_timeouts.Count == 0)
            {
                break; // No more timeouts
            }

            // Re-evaluate current time for each iteration
            now = GetTimestampTicks();
            
            // Check if the earliest timeout is due
            scheduledTime = _timeouts.Keys[0];
            
            if (scheduledTime >= now)
            {
                // Earliest timeout is not yet due, we're done
                break;
            }

            // This timeout is due - remove it from the queue
            timeoutToExecute = _timeouts.Values[0];
            _timeouts.RemoveAt(0);
        }

        // Execute the callback outside the lock
        // This allows nested Run() calls to access the timeout queue
        if (timeoutToExecute != null)
        {
            bool repeat = timeoutToExecute.Callback!();
            
            if (repeat)
            {
                AddTimeout(timeoutToExecute.Span, timeoutToExecute);
            }
        }
    }
}
```

### Key Changes

1. **Lock ? Check ? Remove ? Unlock ? Execute** pattern
2. Only removes **one timeout at a time** that is currently due
3. Executes callbacks **outside the lock**
4. Future timeouts **remain in the queue**, accessible to nested `Run()` calls
5. **Re-evaluates current time** on each iteration to handle long-running callbacks

## Verification

The fix can be verified with these unit tests (all pass after fix):

```csharp
[Fact]
public void Timeout_Fires_In_Nested_Run()
{
    // Tests that a timeout fires during a nested Application.Run() call
}

[Fact]
public void Timeout_Scheduled_Before_Nested_Run_Fires_During_Nested_Run()
{
    // Reproduces the exact ESC key issue scenario
}

[Fact]
public void Multiple_Timeouts_Fire_In_Correct_Order_With_Nested_Run()
{
    // Verifies timeout execution order with nested runs
}
```

See `Tests/UnitTestsParallelizable/Application/NestedRunTimeoutTests.cs` for complete test implementations.

## Files Changed

- `Terminal.Gui/App/Timeout/TimedEvents.cs` - Fixed `RunTimersImpl()` method
- `Tests/UnitTestsParallelizable/Application/NestedRunTimeoutTests.cs` - Added comprehensive tests

## Additional Notes

This is a **critical bug** for the Example infrastructure and any code that relies on timeouts working correctly with modal dialogs. The fix is **non-breaking** - all existing code continues to work, but nested run scenarios now work correctly.

## Related Issues

- Demo keys not working when MessageBox is shown
- Timeouts appearing to "disappear" in complex UI flows
- Automated tests hanging when simulating input with dialogs
