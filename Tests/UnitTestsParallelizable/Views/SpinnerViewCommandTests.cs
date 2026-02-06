using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (SpinnerView))]
public class SpinnerViewCommandTests
{
    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    // NOTE: SpinnerView has CanFocus = false and does not handle commands
    [Fact]
    public void SpinnerView_CannotFocus_DoesNotHandleCommands ()
    {
        SpinnerView spinnerView = new ();

        Assert.False (spinnerView.CanFocus);

        // Commands should not be handled
        bool? activateResult = spinnerView.InvokeCommand (Command.Activate);
        bool? acceptResult = spinnerView.InvokeCommand (Command.Accept);

        // Results should indicate not handled
        Assert.NotEqual (true, activateResult);
        Assert.NotEqual (true, acceptResult);

        spinnerView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void SpinnerView_DisplayOnly_NoKeyboardInput ()
    {
        SpinnerView spinnerView = new () { Style = new SpinnerStyle.Dots () };

        // SpinnerView is display-only, doesn't handle keyboard
        bool? result = spinnerView.NewKeyDownEvent (Key.Space);

        // Should not handle the key
        Assert.False (result);

        spinnerView.Dispose ();
    }
}
