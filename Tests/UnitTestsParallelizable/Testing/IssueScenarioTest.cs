namespace TestingTests;

/// <summary>
///     Test that validates the exact scenario from the GitHub issue.
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "InputInjection")]
public class IssueScenarioTest
{
    [Fact]
    public void Issue_MakeInjectingDoubleClickEasier_WorksAsExpected ()
    {
        // This test reproduces the exact scenario from the issue
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        CheckBox checkBox = new () { Text = "_Checkbox" };
        (runnable as View)?.Add (checkBox);

        CheckState initialState = checkBox.Value;

        // This is the simplified syntax requested in the issue
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (new Point (0, 0)));

        // After double-click, checkbox should have toggled twice (back to initial)
        Assert.Equal (initialState, checkBox.Value);

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void Issue_LeftButtonClick_WorksAsExpected ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new () { Text = "Click Me" };
        (runnable as View)?.Add (button);

        var acceptingCalled = false;
        button.Accepting += (s, e) => acceptingCalled = true;

        // Test the LeftButtonClick helper
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (0, 0)));

        Assert.True (acceptingCalled);
        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void Issue_RightButtonClick_WorksAsExpected ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        // Test the RightButtonClick helper
        app.InjectSequence (InputInjectionExtensions.RightButtonClick (new Point (5, 5)));

        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonPressed));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonReleased));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonClicked));
    }
}
