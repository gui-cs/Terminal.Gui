using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (ProgressBar))]
public class ProgressBarCommandTests
{
    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    // NOTE: ProgressBar has CanFocus = false and does not handle commands
    [Fact]
    public void ProgressBar_CannotFocus_DoesNotHandleCommands ()
    {
        ProgressBar progressBar = new ();

        Assert.False (progressBar.CanFocus);

        // Commands should not be handled
        bool? activateResult = progressBar.InvokeCommand (Command.Activate);
        bool? acceptResult = progressBar.InvokeCommand (Command.Accept);

        // Results should indicate not handled or no handler
        Assert.NotEqual (true, activateResult);
        Assert.NotEqual (true, acceptResult);

        progressBar.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ProgressBar_DisplayOnly_NoKeyboardInput ()
    {
        ProgressBar progressBar = new () { Fraction = 0.5f };

        // ProgressBar is display-only, doesn't handle keyboard
        bool? result = progressBar.NewKeyDownEvent (Key.Space);

        // Should not handle the key
        Assert.False (result);

        progressBar.Dispose ();
    }
}
