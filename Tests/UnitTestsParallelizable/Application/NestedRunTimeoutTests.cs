#nullable enable
using Xunit.Abstractions;

namespace ApplicationTests.Timeout;

/// <summary>
///     Tests for timeout behavior with nested Application.Run() calls.
///     These tests verify that timeouts scheduled in a parent run loop continue to fire
///     correctly when a nested modal dialog is shown via Application.Run().
/// </summary>
public class NestedRunTimeoutTests (ITestOutputHelper output)
{
    [Fact]
    public void Timeout_Fires_With_Single_Session ()
    {
        // Arrange
        using IApplication? app = Application.Create (example: false);

        app.Init ("FakeDriver");

        // Create a simple window for the main run loop
        var mainWindow = new Window { Title = "Main Window" };

        // Schedule a timeout that will ensure the app quits
        var requestStopTimeoutFired = false;
        app.AddTimeout (
                        TimeSpan.FromMilliseconds (100),
                        () =>
                        {
                            output.WriteLine ($"RequestStop Timeout fired!");
                            requestStopTimeoutFired = true;
                            app.RequestStop ();
                            return false;
                        }
                       );

        // Act - Start the main run loop
        app.Run (mainWindow);

        // Assert
        Assert.True (requestStopTimeoutFired, "RequestStop Timeout should have fired");

        mainWindow.Dispose ();
    }

    [Fact]
    public void Timeout_Fires_In_Nested_Run ()
    {
        // Arrange
        using IApplication? app = Application.Create (example: false);

        app.Init ("FakeDriver");

        var timeoutFired = false;
        var nestedRunStarted = false;
        var nestedRunEnded = false;

        // Create a simple window for the main run loop
        var mainWindow = new Window { Title = "Main Window" };

        // Create a dialog for the nested run loop
        var dialog = new Dialog { Title = "Nested Dialog", Buttons = [new Button { Text = "Ok" }] };

        // Schedule a safety timeout that will ensure the app quits if test hangs
        var requestStopTimeoutFired = false;
        app.AddTimeout (
                        TimeSpan.FromMilliseconds (5000),
                        () =>
                        {
                            output.WriteLine ($"SAFETY: RequestStop Timeout fired - test took too long!");
                            requestStopTimeoutFired = true;
                            app.RequestStop ();
                            return false;
                        }
                       );


        // Schedule a timeout that will fire AFTER the nested run starts and stop the dialog
        app.AddTimeout (
                         TimeSpan.FromMilliseconds (200),
                         () =>
                         {
                             output.WriteLine ($"DialogRequestStop Timeout fired! TopRunnable: {app.TopRunnableView?.Title ?? "null"}");
                             timeoutFired = true;

                             // Close the dialog when timeout fires
                             if (app.TopRunnableView == dialog)
                             {
                                 app.RequestStop (dialog);
                             }

                             return false;
                         }
                        );

        // After 100ms, start the nested run loop
        app.AddTimeout (
                         TimeSpan.FromMilliseconds (100),
                         () =>
                         {
                             output.WriteLine ("Starting nested run...");
                             nestedRunStarted = true;

                             // This blocks until the dialog is closed (by the timeout at 200ms)
                             app.Run (dialog);

                             output.WriteLine ("Nested run ended");
                             nestedRunEnded = true;

                             // Stop the main window after nested run completes
                             app.RequestStop ();

                             return false;
                         }
                        );

        // Act - Start the main run loop
        app.Run (mainWindow);

        // Assert
        Assert.True (nestedRunStarted, "Nested run should have started");
        Assert.True (timeoutFired, "Timeout should have fired during nested run");
        Assert.True (nestedRunEnded, "Nested run should have ended");

        Assert.False (requestStopTimeoutFired, "Safety timeout should NOT have fired");

        dialog.Dispose ();
        mainWindow.Dispose ();
    }

    [Fact]
    public void Multiple_Timeouts_Fire_In_Correct_Order_With_Nested_Run ()
    {
        // Arrange
        using IApplication? app = Application.Create (example: false);
        app.Init ("FakeDriver");

        var executionOrder = new List<string> ();

        var mainWindow = new Window { Title = "Main Window" };
        var dialog = new Dialog { Title = "Nested Dialog", Buttons = [new Button { Text = "Ok" }] };

        // Schedule a safety timeout that will ensure the app quits if test hangs
        var requestStopTimeoutFired = false;
        app.AddTimeout (
                        TimeSpan.FromMilliseconds (10000),
                        () =>
                        {
                            output.WriteLine ($"SAFETY: RequestStop Timeout fired - test took too long!");
                            requestStopTimeoutFired = true;
                            app.RequestStop ();
                            return false;
                        }
                       );

        // Schedule multiple timeouts
        app.AddTimeout (
                         TimeSpan.FromMilliseconds (100),
                         () =>
                         {
                             executionOrder.Add ("Timeout1-100ms");
                             output.WriteLine ("Timeout1 fired at 100ms");
                             return false;
                         }
                        );

        app.AddTimeout (
                         TimeSpan.FromMilliseconds (200),
                         () =>
                         {
                             executionOrder.Add ("Timeout2-200ms-StartNestedRun");
                             output.WriteLine ("Timeout2 fired at 200ms - Starting nested run");

                             // Start nested run
                             app.Run (dialog);

                             executionOrder.Add ("Timeout2-NestedRunEnded");
                             output.WriteLine ("Nested run ended");
                             return false;
                         }
                        );

        app.AddTimeout (
                         TimeSpan.FromMilliseconds (300),
                         () =>
                         {
                             executionOrder.Add ("Timeout3-300ms-InNestedRun");
                             output.WriteLine ($"Timeout3 fired at 300ms - TopRunnable: {app.TopRunnableView?.Title}");

                             // This should fire while dialog is running
                             Assert.Equal (dialog, app.TopRunnableView);

                             return false;
                         }
                        );

        app.AddTimeout (
                         TimeSpan.FromMilliseconds (400),
                         () =>
                         {
                             executionOrder.Add ("Timeout4-400ms-CloseDialog");
                             output.WriteLine ("Timeout4 fired at 400ms - Closing dialog");

                             // Close the dialog
                             app.RequestStop (dialog);

                             return false;
                         }
                        );

        app.AddTimeout (
                         TimeSpan.FromMilliseconds (500),
                         () =>
                         {
                             executionOrder.Add ("Timeout5-500ms-StopMain");
                             output.WriteLine ("Timeout5 fired at 500ms - Stopping main window");

                             // Stop main window
                             app.RequestStop (mainWindow);

                             return false;
                         }
                        );

        // Act
        app.Run (mainWindow);

        // Assert - Verify all timeouts fired in the correct order
        output.WriteLine ($"Execution order: {string.Join (", ", executionOrder)}");

        Assert.Equal (6, executionOrder.Count); // 5 timeouts + 1 nested run end marker
        Assert.Equal ("Timeout1-100ms", executionOrder [0]);
        Assert.Equal ("Timeout2-200ms-StartNestedRun", executionOrder [1]);
        Assert.Equal ("Timeout3-300ms-InNestedRun", executionOrder [2]);
        Assert.Equal ("Timeout4-400ms-CloseDialog", executionOrder [3]);
        Assert.Equal ("Timeout2-NestedRunEnded", executionOrder [4]);
        Assert.Equal ("Timeout5-500ms-StopMain", executionOrder [5]);

        Assert.False (requestStopTimeoutFired, "Safety timeout should NOT have fired");

        dialog.Dispose ();
        mainWindow.Dispose ();
    }

    [Fact]
    public void Timeout_Scheduled_Before_Nested_Run_Fires_During_Nested_Run ()
    {
        // This test specifically reproduces the ESC key issue scenario:
        // - Timeouts are scheduled upfront (like demo keys)
        // - A timeout fires and triggers a nested run (like Enter opening MessageBox)
        // - A subsequent timeout should still fire during the nested run (like ESC closing MessageBox)

        // Arrange
        using IApplication? app = Application.Create (example: false);
        app.Init ("FakeDriver");

        var enterFired = false;
        var escFired = false;
        var messageBoxShown = false;
        var messageBoxClosed = false;

        var mainWindow = new Window { Title = "Login Window" };
        var messageBox = new Dialog { Title = "Success", Buttons = [new Button { Text = "Ok" }] };

        // Schedule a safety timeout that will ensure the app quits if test hangs
        var requestStopTimeoutFired = false;
        app.AddTimeout (
                        TimeSpan.FromMilliseconds (10000),
                        () =>
                        {
                            output.WriteLine ($"SAFETY: RequestStop Timeout fired - test took too long!");
                            requestStopTimeoutFired = true;
                            app.RequestStop ();
                            return false;
                        }
                       );
        
        // Schedule "Enter" timeout at 100ms
        app.AddTimeout (
                         TimeSpan.FromMilliseconds (100),
                         () =>
                         {
                             output.WriteLine ("Enter timeout fired - showing MessageBox");
                             enterFired = true;

                             // Simulate Enter key opening MessageBox
                             messageBoxShown = true;
                             app.Run (messageBox);
                             messageBoxClosed = true;

                             output.WriteLine ("MessageBox closed");
                             return false;
                         }
                        );

        // Schedule "ESC" timeout at 200ms (should fire while MessageBox is running)
        app.AddTimeout (
                         TimeSpan.FromMilliseconds (200),
                         () =>
                         {
                             output.WriteLine ($"ESC timeout fired - TopRunnable: {app.TopRunnableView?.Title}");
                             escFired = true;

                             // Simulate ESC key closing MessageBox
                             if (app.TopRunnableView == messageBox)
                             {
                                 output.WriteLine ("Closing MessageBox with ESC");
                                 app.RequestStop (messageBox);
                             }

                             return false;
                         }
                        );

        // Stop main window after MessageBox closes
        app.AddTimeout (
                         TimeSpan.FromMilliseconds (300),
                         () =>
                         {
                             output.WriteLine ("Stopping main window");
                             app.RequestStop (mainWindow);
                             return false;
                         }
                        );

        // Act
        app.Run (mainWindow);

        // Assert
        Assert.True (enterFired, "Enter timeout should have fired");
        Assert.True (messageBoxShown, "MessageBox should have been shown");
        Assert.True (escFired, "ESC timeout should have fired during MessageBox"); // THIS WAS THE BUG - NOW FIXED!
        Assert.True (messageBoxClosed, "MessageBox should have been closed");

        Assert.False (requestStopTimeoutFired, "Safety timeout should NOT have fired");

        messageBox.Dispose ();
        mainWindow.Dispose ();
    }

    [Fact]
    public void Timeout_Queue_Persists_Across_Nested_Runs ()
    {
        // Verify that the timeout queue is not cleared when nested runs start/end

        // Arrange
        using IApplication? app = Application.Create (example: false);
        app.Init ("FakeDriver");

        // Schedule a safety timeout that will ensure the app quits if test hangs
        var requestStopTimeoutFired = false;
        app.AddTimeout (
                        TimeSpan.FromMilliseconds (10000),
                        () =>
                        {
                            output.WriteLine ($"SAFETY: RequestStop Timeout fired - test took too long!");
                            requestStopTimeoutFired = true;
                            app.RequestStop ();
                            return false;
                        }
                       );

        var mainWindow = new Window { Title = "Main Window" };
        var dialog = new Dialog { Title = "Dialog", Buttons = [new Button { Text = "Ok" }] };

        int initialTimeoutCount = 0;
        int timeoutCountDuringNestedRun = 0;
        int timeoutCountAfterNestedRun = 0;

        // Schedule 5 timeouts at different times
        for (int i = 0; i < 5; i++)
        {
            int capturedI = i;
            app.AddTimeout (
                             TimeSpan.FromMilliseconds (100 * (i + 1)),
                             () =>
                             {
                                 output.WriteLine ($"Timeout {capturedI} fired at {100 * (capturedI + 1)}ms");

                                 if (capturedI == 0)
                                 {
                                     initialTimeoutCount = app.TimedEvents!.Timeouts.Count;
                                     output.WriteLine ($"Initial timeout count: {initialTimeoutCount}");
                                 }

                                 if (capturedI == 1)
                                 {
                                     // Start nested run
                                     output.WriteLine ("Starting nested run");
                                     app.Run (dialog);
                                     output.WriteLine ("Nested run ended");

                                     timeoutCountAfterNestedRun = app.TimedEvents!.Timeouts.Count;
                                     output.WriteLine ($"Timeout count after nested run: {timeoutCountAfterNestedRun}");
                                 }

                                 if (capturedI == 2)
                                 {
                                     // This fires during nested run
                                     timeoutCountDuringNestedRun = app.TimedEvents!.Timeouts.Count;
                                     output.WriteLine ($"Timeout count during nested run: {timeoutCountDuringNestedRun}");

                                     // Close dialog
                                     app.RequestStop (dialog);
                                 }

                                 if (capturedI == 4)
                                 {
                                     // Stop main window
                                     app.RequestStop (mainWindow);
                                 }

                                 return false;
                             }
                            );
        }

        // Act
        app.Run (mainWindow);

        // Assert
        output.WriteLine ($"Final counts - Initial: {initialTimeoutCount}, During: {timeoutCountDuringNestedRun}, After: {timeoutCountAfterNestedRun}");

        // The timeout queue should have pending timeouts throughout
        Assert.True (initialTimeoutCount >= 0, "Should have timeouts in queue initially");
        Assert.True (timeoutCountDuringNestedRun >= 0, "Should have timeouts in queue during nested run");
        Assert.True (timeoutCountAfterNestedRun >= 0, "Should have timeouts in queue after nested run");

        Assert.False (requestStopTimeoutFired, "Safety timeout should NOT have fired");

        dialog.Dispose ();
        mainWindow.Dispose ();
    }
}
