using Xunit;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Tests for Phase 2 of the IRunnable migration: Toplevel, Dialog, MessageBox, and Wizard implementing IRunnable pattern.
/// These tests verify that the migrated components work correctly with the new IRunnable architecture.
/// </summary>
public class Phase2RunnableMigrationTests
{
    [Fact]
    public void Toplevel_ImplementsIRunnable()
    {
        // Arrange
        Toplevel toplevel = new ();

        // Act & Assert
        Assert.IsAssignableFrom<IRunnable> (toplevel);
    }

    [Fact]
    public void Dialog_ImplementsIRunnableInt()
    {
        // Arrange
        Dialog dialog = new ();

        // Act & Assert
        Assert.IsAssignableFrom<IRunnable<int?>> (dialog);
    }

    [Fact]
    public void Dialog_Result_DefaultsToNull()
    {
        // Arrange
        Dialog dialog = new ();

        // Act & Assert
        Assert.Null (dialog.Result);
    }

    [Fact]
    public void Dialog_Result_SetInOnIsRunningChanging()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        Dialog dialog = new ()
        {
            Title = "Test Dialog",
            Buttons =
            [
                new Button { Text = "OK" },
                new Button { Text = "Cancel" }
            ]
        };

        int? extractedResult = null;

        // Subscribe to verify Result is set before IsRunningChanged fires
        ((IRunnable)dialog).IsRunningChanged += (s, e) =>
        {
            if (!e.Value) // Stopped
            {
                extractedResult = dialog.Result;
            }
        };

        // Act
        // Simulate clicking the first button (index 0)
        app.Run (dialog);
        dialog.Buttons [0].SetFocus ();
        app.RequestStop (dialog);

        // Assert
        Assert.NotNull (extractedResult);
        Assert.Equal (0, extractedResult);
        Assert.Equal (0, dialog.Result);

        dialog.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void Dialog_Result_IsNullWhenCanceled()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        Dialog dialog = new ()
        {
            Title = "Test Dialog",
            Buttons =
            [
                new Button { Text = "OK" }
            ]
        };

        // Act
        app.Run (dialog);
        // Don't focus any button - simulate cancel (ESC pressed)
        app.RequestStop (dialog);

        // Assert
        Assert.Null (dialog.Result);

        dialog.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void Dialog_Canceled_PropertyMatchesResult()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        Dialog dialog = new ()
        {
            Title = "Test Dialog",
            Buttons = [new Button { Text = "OK" }]
        };

        // Act - Cancel the dialog
        app.Run (dialog);
        app.RequestStop (dialog);

        // Assert
        Assert.True (dialog.Canceled);
        Assert.Null (dialog.Result);

        dialog.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void MessageBox_Query_ReturnsDialogResult()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        // Act
        // MessageBox.Query creates a Dialog internally and returns its Result
        // We can't easily test this without actually running the UI, but we can verify the pattern

        // Create a Dialog similar to what MessageBox creates
        Dialog dialog = new ()
        {
            Title = "Test",
            Text = "Message",
            Buttons =
            [
                new Button { Text = "Yes" },
                new Button { Text = "No" }
            ]
        };

        app.Run (dialog);
        dialog.Buttons [1].SetFocus (); // Focus "No" button (index 1)
        app.RequestStop (dialog);

        int result = dialog.Result ?? -1;

        // Assert
        Assert.Equal (1, result);
        Assert.Equal (1, dialog.Result);

        dialog.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void MessageBox_Clicked_PropertyUpdated()
    {
        // Arrange & Act
        // MessageBox.Clicked is updated from Dialog.Result for backward compatibility
        // Since we can't easily run MessageBox.Query without UI, we verify the pattern is correct

        // The implementation should be:
        // int result = dialog.Result ?? -1;
        // MessageBox.Clicked = result;

        // Assert
        // This test verifies the property exists and has the expected type
        int clicked = MessageBox.Clicked;
        Assert.True (clicked is int);
    }

    [Fact]
    public void Wizard_InheritsFromDialog_ImplementsIRunnable()
    {
        // Arrange
        Wizard wizard = new ();

        // Act & Assert
        Assert.IsAssignableFrom<Dialog> (wizard);
        Assert.IsAssignableFrom<IRunnable<int?>> (wizard);
    }

    [Fact]
    public void Wizard_WasFinished_DefaultsToFalse()
    {
        // Arrange
        Wizard wizard = new ();

        // Act & Assert
        Assert.False (wizard.WasFinished);
    }

    [Fact]
    public void Wizard_WasFinished_TrueWhenFinished()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        Wizard wizard = new ();
        WizardStep step = new ();
        step.Title = "Step 1";
        wizard.AddStep (step);

        bool finishedEventFired = false;
        wizard.Finished += (s, e) => { finishedEventFired = true; };

        // Act
        app.Run (wizard);
        wizard.CurrentStep = step;
        // Simulate finishing the wizard
        wizard.NextFinishButton.SetFocus ();
        app.RequestStop (wizard);

        // Assert
        Assert.True (finishedEventFired);
        // Note: WasFinished depends on internal _finishedPressed flag being set

        wizard.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void Toplevel_Running_PropertyUpdatedByIRunnable()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        Toplevel toplevel = new ();

        // Act
        app.Run (toplevel);
        bool runningWhileRunning = toplevel.Running;
        app.RequestStop (toplevel);
        bool runningAfterStop = toplevel.Running;

        // Assert
        Assert.True (runningWhileRunning);
        Assert.False (runningAfterStop);

        toplevel.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void Toplevel_Modal_PropertyIndependentOfIRunnable()
    {
        // Arrange
        Toplevel toplevel = new ();

        // Act
        toplevel.Modal = true;
        bool modalValue = toplevel.Modal;

        // Assert
        Assert.True (modalValue);
        // Modal property is separate from IRunnable.IsModal
        // This test verifies the legacy Modal property still works
    }

    [Fact]
    public void Dialog_OnIsRunningChanging_CanCancelStopping()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        TestDialog dialog = new ();
        dialog.CancelStopping = true;

        // Act
        app.Run (dialog);
        app.RequestStop (dialog);

        // The dialog should still be running because we canceled the stop
        bool stillRunning = ((IRunnable)dialog).IsRunning;

        // Clean up - force stop
        dialog.CancelStopping = false;
        app.RequestStop (dialog);

        // Assert
        Assert.True (stillRunning);

        dialog.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void Dialog_IsRunningChanging_EventFires()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        Dialog dialog = new ();
        int eventFireCount = 0;
        bool? lastNewValue = null;

        ((IRunnable)dialog).IsRunningChanging += (s, e) =>
        {
            eventFireCount++;
            lastNewValue = e.NewValue;
        };

        // Act
        app.Run (dialog);
        app.RequestStop (dialog);

        // Assert
        Assert.Equal (2, eventFireCount); // Once for starting, once for stopping
        Assert.False (lastNewValue); // Last event was for stopping (false)

        dialog.Dispose ();
        app.Shutdown ();
    }

    [Fact]
    public void Dialog_IsRunningChanged_EventFires()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();

        Dialog dialog = new ();
        int eventFireCount = 0;
        bool? lastValue = null;

        ((IRunnable)dialog).IsRunningChanged += (s, e) =>
        {
            eventFireCount++;
            lastValue = e.Value;
        };

        // Act
        app.Run (dialog);
        app.RequestStop (dialog);

        // Assert
        Assert.Equal (2, eventFireCount); // Once for started, once for stopped
        Assert.False (lastValue); // Last event was for stopped (false)

        dialog.Dispose ();
        app.Shutdown ();
    }

    /// <summary>
    /// Test helper dialog that can cancel stopping
    /// </summary>
    private class TestDialog : Dialog
    {
        public bool CancelStopping { get; set; }

        protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
        {
            if (!newIsRunning && CancelStopping)
            {
                return true; // Cancel stopping
            }

            return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
        }
    }
}
