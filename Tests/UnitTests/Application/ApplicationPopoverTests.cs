namespace Terminal.Gui.ApplicationTests;

public class ApplicationPopoverTests
{
    [Fact]
    public void Application_Init_Initializes_PopoverManager ()
    {
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());

        // Act
        Assert.NotNull (Application.Popover);

        Application.ResetState (true);
    }

    [Fact]
    public void Application_Shutdown_Resets_PopoverManager ()
    {
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());

        // Act
        Assert.NotNull (Application.Popover);

        Application.Shutdown ();

        // Test
        Assert.Null (Application.Popover);
    }

    [Fact]
    public void Application_End_Does_Not_Reset_PopoverManager ()
    {
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());
        Assert.NotNull (Application.Popover);
        Application.Iteration += (s, a) => Application.RequestStop ();

        var top = new Toplevel ();
        RunState rs = Application.Begin (top);

        // Act
        Application.End (rs);

        // Test
        Assert.NotNull (Application.Popover);

        top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void Application_End_Hides_Active ()
    {
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());
        Application.Iteration += (s, a) => Application.RequestStop ();

        var top = new Toplevel ();
        RunState rs = Application.Begin (top);

        PopoverTestClass popover = new ();

        Application.Popover?.Show (popover);
        Assert.True (popover.Visible);

        // Act
        Application.End (rs);
        top.Dispose ();

        // Test
        Assert.False (popover.Visible);
        Assert.NotNull (Application.Popover);

        Application.Shutdown ();
        Assert.Equal (1, popover.DisposedCount);
    }

    [Fact]
    public void Application_Shutdown_Disposes_Registered_Popovers ()
    {
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());

        PopoverTestClass popover = new ();

        // Act
        Application.Popover?.Register (popover);
        Application.Shutdown ();

        // Test
        Assert.Equal (1, popover.DisposedCount);
    }

    [Fact]
    public void Application_Shutdown_Does_Not_Dispose_DeRegistered_Popovers ()
    {
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());

        PopoverTestClass popover = new ();

        Application.Popover?.Register (popover);

        // Act
        Application.Popover?.DeRegister (popover);
        Application.Shutdown ();

        // Test
        Assert.Equal (0, popover.DisposedCount);

        popover.Dispose ();
        Assert.Equal (1, popover.DisposedCount);
    }

    [Fact]
    public void Application_Shutdown_Does_Not_Dispose_ActiveNotRegistered_Popover ()
    {
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());

        PopoverTestClass popover = new ();

        Application.Popover?.Show (popover);
        Application.Popover?.DeRegister (popover);

        // Act
        Application.Shutdown ();

        // Test
        Assert.Equal (0, popover.DisposedCount);

        popover.Dispose ();
        Assert.Equal (1, popover.DisposedCount);
    }

    public class PopoverTestClass : PopoverBaseImpl
    {
        public List<Key> HandledKeys { get; } = [];
        public int NewCommandInvokeCount { get; private set; }

        // NOTE: Hides the base DisposedCount property
        public new int DisposedCount { get; private set; }

        public PopoverTestClass ()
        {
            CanFocus = true;
            AddCommand (Command.New, NewCommandHandler);
            HotKeyBindings.Add (Key.N.WithCtrl, Command.New);

            return;

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

        /// <inheritdoc/>
        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            DisposedCount++;
        }
    }
}
