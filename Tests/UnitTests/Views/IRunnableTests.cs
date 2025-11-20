using Xunit;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public class IRunnableTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Toplevel_Implements_IRunnable ()
    {
        var top = new Toplevel ();
        Assert.IsAssignableFrom<IRunnable> (top);
    }

    [Fact]
    public void Dialog_Implements_IModalRunnable ()
    {
        var dialog = new Dialog ();
        Assert.IsAssignableFrom<IRunnable> (dialog);
        Assert.IsAssignableFrom<IModalRunnable<int?>> (dialog);
    }

    [Fact]
    public void Runnable_Base_Class_Works ()
    {
        var runnable = new Runnable ();
        Assert.IsAssignableFrom<IRunnable> (runnable);
        Assert.False (runnable.Running);
    }

    [Fact]
    [AutoInitShutdown]
    public void Dialog_Result_Property_Works ()
    {
        var dialog = new Dialog ();
        var btn1 = new Button { Text = "OK" };
        var btn2 = new Button { Text = "Cancel" };

        dialog.AddButton (btn1);
        dialog.AddButton (btn2);

        // Result should be null by default
        Assert.Null (dialog.Result);

        // Simulate clicking the first button by invoking Accept
        btn1.InvokeCommand (Command.Accept);

        // Result should now be 0 (first button)
        Assert.Equal (0, dialog.Result);

        // Reset
        dialog.Result = null;

        // Simulate clicking the second button
        btn2.InvokeCommand (Command.Accept);

        // Result should now be 1 (second button)
        Assert.Equal (1, dialog.Result);
    }

    [Fact]
    [AutoInitShutdown]
    public void Toplevel_Legacy_Events_Still_Work ()
    {
        // Verify that legacy Toplevel events still fire for backward compatibility
        var eventsRaised = new List<string> ();
        var top = new Toplevel ();

#pragma warning disable CS0618 // Type or member is obsolete
        top.Activate += (s, e) => eventsRaised.Add ("Activate");
#pragma warning restore CS0618

        var token = Application.Begin (top);

        // Legacy event should still fire
        Assert.Contains ("Activate", eventsRaised);

        Application.End (token);
    }

    [Fact]
    public void IRunnable_Stopping_Event_Works ()
    {
        // Test that the Stopping event can be subscribed to and fires
        var top = new Toplevel ();
        var stoppingFired = false;
        var stoppedFired = false;

        top.Stopping += (s, e) => { stoppingFired = true; };
        top.Stopped += (s, e) => { stoppedFired = true; };

        top.Running = true;
        top.RaiseStoppingEvent ();

        Assert.True (stoppingFired);
        Assert.True (stoppedFired);
        Assert.False (top.Running);
    }

    [Fact]
    public void IRunnable_Stopping_Can_Be_Canceled ()
    {
        // Test that the Stopping event can cancel the stop operation
        var top = new Toplevel ();
        var stoppingFired = false;
        var stoppedFired = false;

        top.Stopping += (s, e) =>
        {
            stoppingFired = true;
            e.Cancel = true; // Cancel the stop
        };

        top.Stopped += (s, e) => { stoppedFired = true; };

        top.Running = true;
        top.RaiseStoppingEvent ();

        Assert.True (stoppingFired);
        Assert.False (stoppedFired); // Should not fire because it was canceled
        Assert.True (top.Running); // Should still be running
    }

    [Fact]
    [AutoInitShutdown]
    public void IRunnable_Initialization_Events_Fire ()
    {
        // Test that Initializing and Initialized events fire
        var initializingFired = false;
        var initializedFired = false;
        var top = new Toplevel ();

        top.Initializing += (s, e) => { initializingFired = true; };
        top.Initialized += (s, e) => { initializedFired = true; };

        var token = Application.Begin (top);

        Assert.True (initializingFired);
        Assert.True (initializedFired);

        Application.End (token);
    }

    [Fact]
    public void IRunnable_Activating_And_Activated_Events_Can_Fire ()
    {
        // Test that manually calling the activation methods works
        var activatingFired = false;
        var activatedFired = false;
        var top = new Toplevel ();

        top.Activating += (s, e) => { activatingFired = true; };
        top.Activated += (s, e) => { activatedFired = true; };

        // Manually trigger activation
        bool canceled = top.RaiseActivatingEvent (null);

        Assert.False (canceled);
        Assert.True (activatingFired);
        Assert.True (activatedFired);
    }
}
