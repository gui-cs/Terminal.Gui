namespace ApplicationTests.Popover;

[Collection ("Application Tests")]
public class PopoverBaseImplTests
{
    // Minimal concrete implementation for testing
    private class TestPopover : PopoverBaseImpl
    { }

    [Fact]
    public void Constructor_SetsDefaults ()
    {
        var popover = new TestPopover ();

        Assert.Equal ("popoverBaseImpl", popover.Id);
        Assert.True (popover.CanFocus);
        Assert.Equal (Dim.Fill (), popover.Width);
        Assert.Equal (Dim.Fill (), popover.Height);
        Assert.True (popover.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent));
        Assert.True (popover.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse));
    }

    [Fact]
    public void Owner_Property_CanBeSetAndGet ()
    {
        var popover = new TestPopover ();
        var top = new Runnable ();
        popover.Owner = top;
        Assert.Same (top, popover.Owner);
    }

    [Fact]
    public void Show_ThrowsIfPopoverMissingRequiredFlags ()
    {
        var popover = new TestPopover ();

        // Popover missing Transparent flags
        popover.ViewportSettings = ViewportSettingsFlags.None; // Remove required flags

        var popoverManager = new ApplicationPopover ();

        // Test missing Transparent flags
        Assert.ThrowsAny<Exception> (() => popoverManager.Show (popover));
    }

    [Fact]
    public void Show_ThrowsIfPopoverMissingQuitCommand ()
    {
        var popover = new TestPopover ();

        // Popover missing Command.Quit binding
        popover.KeyBindings.Clear (); // Remove all key bindings

        var popoverManager = new ApplicationPopover ();
        Assert.ThrowsAny<Exception> (() => popoverManager.Show (popover));
    }

    [Fact]
    public void Show_Throw_If_Not_Registered ()
    {
        var popover = new TestPopover ();

        var popoverManager = new ApplicationPopover ();
        Assert.Throws<InvalidOperationException> (() => popoverManager.Show (popover));
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Default_Is_Null ()
    {
        TestPopover popover = new ();
        Assert.Null (popover.Target);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Get_Returns_Set_Value ()
    {
        TestPopover popover = new ();
        View target = new ();
        WeakReference<View> weakRef = new (target);

        popover.Target = weakRef;

        Assert.Same (weakRef, popover.Target);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Set_Creates_Bridge_Activated_Reaches_Target ()
    {
        TestPopover popover = new ();
        View target = new ();

        popover.Target = new WeakReference<View> (target);

        bool activatedFired = false;
        ICommandContext? capturedCtx = null;

        target.Activated += (_, args) =>
                            {
                                activatedFired = true;
                                capturedCtx = args.Value;
                            };

        popover.InvokeCommand (Command.Activate);

        Assert.True (activatedFired);
        Assert.NotNull (capturedCtx);
        Assert.Equal (CommandRouting.Bridged, capturedCtx!.Routing);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Set_Creates_Bridge_Accepted_Reaches_Target ()
    {
        TestPopover popover = new ();
        View target = new ();

        popover.Target = new WeakReference<View> (target);

        bool acceptedFired = false;
        ICommandContext? capturedCtx = null;

        target.Accepted += (_, args) =>
                           {
                               acceptedFired = true;
                               capturedCtx = args.Context;
                           };

        popover.InvokeCommand (Command.Accept);

        Assert.True (acceptedFired);
        Assert.NotNull (capturedCtx);
        Assert.Equal (CommandRouting.Bridged, capturedCtx!.Routing);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Activated_Bubbles_Through_Target_SuperView_Chain ()
    {
        TestPopover popover = new ();
        View target = new ();
        View superView = new () { CommandsToBubbleUp = [Command.Activate] };
        superView.Add (target);

        popover.Target = new WeakReference<View> (target);

        bool superViewActivatedFired = false;

        superView.Activated += (_, _) =>
                               {
                                   superViewActivatedFired = true;
                               };

        popover.InvokeCommand (Command.Activate);

        Assert.True (superViewActivatedFired);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Changed_Disposes_Old_Bridge_Uses_New ()
    {
        TestPopover popover = new ();
        View viewA = new ();
        View viewB = new ();

        popover.Target = new WeakReference<View> (viewA);

        // Change target to viewB
        popover.Target = new WeakReference<View> (viewB);

        bool viewAActivated = false;
        bool viewBActivated = false;

        viewA.Activated += (_, _) => { viewAActivated = true; };
        viewB.Activated += (_, _) => { viewBActivated = true; };

        popover.InvokeCommand (Command.Activate);

        Assert.False (viewAActivated);
        Assert.True (viewBActivated);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Set_Null_Disposes_Bridge ()
    {
        TestPopover popover = new ();
        View target = new ();

        popover.Target = new WeakReference<View> (target);

        // Now set target to null
        popover.Target = null;

        bool activatedFired = false;
        target.Activated += (_, _) => { activatedFired = true; };

        popover.InvokeCommand (Command.Activate);

        Assert.False (activatedFired);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_Cleans_Up_Target_Bridge ()
    {
        TestPopover popover = new ();
        View target = new ();

        popover.Target = new WeakReference<View> (target);

        popover.Dispose ();

        bool activatedFired = false;
        target.Activated += (_, _) => { activatedFired = true; };

        // The popover is disposed, so invoking a command on it should not bridge
        popover.InvokeCommand (Command.Activate);

        Assert.False (activatedFired);
    }
}
