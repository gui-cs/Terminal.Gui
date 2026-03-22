---
name: No Console.WriteLine for debugging
description: Never use Console.Error.WriteLine or Console.WriteLine for debug tracing - use  `Terminal.Gui.App.Logging`, `Terminal.Gui.Tests.TestLogging` and `Terminal.Gui.Tracing.Tracing.Trace` instead
type: feedback
---

Do not use Console.Error.WriteLine or Console.WriteLine for debug output in Terminal.Gui code. Use project's Logging infrastructure instead: `Terminal.Gui.App.Logging`, `Terminal.Gui.Tests.TestLogging` and `Terminal.Gui.Tracing.Tracing.Trace`.

**Why:** User has explicitly corrected this twice. Console output interferes with the terminal UI framework.

**How to apply:** When adding temporary debug tracing, use `TestLogging` and `Tracing.Trace` and the project's logging system. 

`Tracing.Trace` is only available in DEBUG builds; do not use it to validate test results as all tests must pass in RELEASE builds.

```csharp
using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;

[Fact]
public void MyTest ()
{
    // Enable logging and tracing in one call
    using (TestLogging.Verbose (_output, TraceCategory.Command))
    {
        // Logs and traces appear in xUnit test output
        CheckBox checkbox = new () { Id = "test" };
        checkbox.InvokeCommand (Command.Activate);
    }
}
```

See `./docfx/docs/logging.md` for full details.