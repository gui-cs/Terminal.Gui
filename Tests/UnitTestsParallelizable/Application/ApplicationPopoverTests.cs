using Moq;

namespace Terminal.Gui.ApplicationTests;

public class ApplicationPopoverTests
{
    [Fact]
    public void Register_AddsPopover ()
    {
        // Arrange
        var popover = new Mock<IPopover> ().Object;
        var popoverManager = new ApplicationPopover ();

        // Act
        popoverManager.Register (popover);

        // Assert
        Assert.Contains (popover, popoverManager.Popovers);
    }

    [Fact]
    public void DeRegister_RemovesPopover ()
    {
        // Arrange
        var popover = new Mock<IPopover> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);

        // Act
        var result = popoverManager.DeRegister (popover);

        // Assert
        Assert.True (result);
        Assert.DoesNotContain (popover, popoverManager.Popovers);
    }

    [Fact]
    public void Show_SetsActivePopover ()
    {
        // Arrange
        var popover = new Mock<IPopoverTestClass> ().Object;
        var popoverManager = new ApplicationPopover ();

        // Act
        popoverManager.Show (popover);

        // Assert
        Assert.Equal (popover, popoverManager.GetActivePopover ());
    }

    [Fact]
    public void Hide_ClearsActivePopover ()
    {
        // Arrange
        var popover = new Mock<IPopover> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Show (popover);

        // Act
        popoverManager.Hide (popover);

        // Assert
        Assert.Null (popoverManager.GetActivePopover ());
    }


    [Fact]
    public void DispatchKeyDown_ActivePopoverGetsKey ()
    {
        // Arrange
        var popover = new IPopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Show (popover);

        // Act
        popoverManager.DispatchKeyDown (Key.A);

        // Assert
        Assert.Contains (KeyCode.A, popover.HandledKeys);
    }


    [Fact]
    public void DispatchKeyDown_ActivePopoverGetsHotKey ()
    {
        // Arrange
        var popover = new IPopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Show (popover);

        // Act
        popoverManager.DispatchKeyDown (Key.N.WithCtrl);

        // Assert
        Assert.Equal(1, popover.NewCommandInvokeCount);
        Assert.Contains (Key.N.WithCtrl, popover.HandledKeys);
    }


    [Fact]
    public void DispatchKeyDown_InactivePopoverGetsHotKey ()
    {
        // Arrange
        var activePopover = new IPopoverTestClass () { Id = "activePopover" };
        var inactivePopover = new IPopoverTestClass () { Id = "inactivePopover" }; ;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Show (activePopover);
        popoverManager.Register (inactivePopover);

        // Act
        popoverManager.DispatchKeyDown (Key.N.WithCtrl);

        // Assert
        Assert.Equal (1, activePopover.NewCommandInvokeCount);
        Assert.Equal (1, inactivePopover.NewCommandInvokeCount);
        Assert.Contains (Key.N.WithCtrl, activePopover.HandledKeys);
        Assert.NotEmpty (inactivePopover.HandledKeys);
    }

    [Fact]
    public void DispatchKeyDown_InactivePopoverDoesGetKey ()
    {
        // Arrange
        var activePopover = new IPopoverTestClass ();
        var inactivePopover = new IPopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Show (activePopover);
        popoverManager.Register (inactivePopover);

        // Act
        popoverManager.DispatchKeyDown (Key.A);

        // Assert
        Assert.Contains (Key.A, activePopover.HandledKeys);
        Assert.NotEmpty (inactivePopover.HandledKeys);
    }
    
    public class IPopoverTestClass : View, IPopover
    {
        public List<Key> HandledKeys { get; } = new List<Key> ();
        public int NewCommandInvokeCount { get; private set; }

        public IPopoverTestClass ()
        {
            CanFocus = true;
            AddCommand(Command.New, NewCommandHandler );
            HotKeyBindings.Add (Key.N.WithCtrl, Command.New);

            bool? NewCommandHandler (ICommandContext ctx)
            {
                NewCommandInvokeCount++;

                return false;
            }
        }

        protected override bool OnKeyDown (Key key)
        {
            HandledKeys.Add (key);
            return false;
        }
    }
}
