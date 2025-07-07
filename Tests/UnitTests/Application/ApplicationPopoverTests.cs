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
        Application.ResetState (true);
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
        Application.ResetState (true);
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
        Application.ResetState (true);
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
        Application.ResetState (true);
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
        Application.ResetState (true);
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
        Application.ResetState (true);
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

    [Fact]
    public void Register_SetsTopLevel ()
    {
        Application.ResetState (true);
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());
        Application.Top = new Toplevel ();
        PopoverTestClass popover = new ();

        // Act
        Application.Popover?.Register (popover);

        // Assert
        Assert.Equal (Application.Top, popover.Toplevel);

        Application.ResetState (true);
    }

    [Fact]
    public void Keyboard_Events_Go_Only_To_Popover_Associated_With_Toplevel ()
    {
        Application.ResetState (true);
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());
        Application.Top = new Toplevel () { Id = "initialTop" };
        PopoverTestClass popover = new ();
        int keyDownEvents = 0;
        popover.KeyDown += (s, e) =>
                           {
                               keyDownEvents++;
                               e.Handled = true;
                           }; // Ensure it handles the key

        Application.Popover?.Register (popover);

        // Act
        Application.RaiseKeyDownEvent (Key.A); // Goes to initialTop

        Application.Top = new Toplevel () { Id = "secondaryTop" };
        Application.RaiseKeyDownEvent (Key.A); // Goes to secondaryTop

        // Test
        Assert.Equal (1, keyDownEvents);


        popover.Dispose ();
        Assert.Equal (1, popover.DisposedCount);
        Application.ResetState (true);
    }

    // See: https://github.com/gui-cs/Terminal.Gui/issues/4122
    [Theory]
    [InlineData (0, 0, new [] { "top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (1, 1, new [] { "top", "view" })]
    [InlineData (5, 5, new [] { "top" })]
    [InlineData (6, 6, new [] { "popoverSubView" })]
    [InlineData (7, 7, new [] { "top" })]
    [InlineData (3, 3, new [] { "top" })]
    public void GetViewsUnderMouse_Supports_ActivePopover (int mouseX, int mouseY, string [] viewIdStrings)
    {
        Application.ResetState (true);
        // Arrange
        Assert.Null (Application.Popover);
        Application.Init (new FakeDriver ());
        Application.Top = new ()
        {
            Frame = new (0, 0, 10, 10),
            Id = "top"
        };

        View view = new ()
        {
            Id = "view",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
        }; // at 1,1 to 3,2 (screen)

        Application.Top.Add (view);

        PopoverTestClass popover = new ()
        {
            Id = "popover",
            X = 5,
            Y = 5,
            Width = 3,
            Height = 3,
        }; // at 5,5 to 8,8 (screen)

        View popoverSubView = new ()
        {
            Id = "popoverSubView",
            X = 1,
            Y = 1,
            Width = 1,
            Height = 1,
        }; // at 6,6 to 7,7 (screen)

        popover.Add (popoverSubView);

        Application.Popover?.Show (popover);

        List<View?> found = View.GetViewsUnderLocation (new (mouseX, mouseY), ViewportSettingsFlags.TransparentMouse);

        string [] foundIds = found.Select (v => v!.Id).ToArray ();

        Assert.Equal (viewIdStrings, foundIds);

        popover.Dispose ();
        Application.Top.Dispose ();
        Application.ResetState (true);
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
