#nullable enable
namespace UnitTests.ApplicationTests;

public class ApplicationPopoverTests
{
    [Fact]
    public void Application_Init_Initializes_PopoverManager ()
    {
        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");

            // Act
            Assert.NotNull (Application.Popover);
        }
        finally
        {
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Application_Shutdown_Resets_PopoverManager ()
    {
        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");

            // Act
            Assert.NotNull (Application.Popover);

            Application.Shutdown ();

            // Test
            Assert.Null (Application.Popover);
        }
        finally
        {
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Application_End_Does_Not_Reset_PopoverManager ()
    {
        Toplevel? top = null;

        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");
            Assert.NotNull (Application.Popover);
            Application.StopAfterFirstIteration = true;

            top = new Toplevel ();
            SessionToken rs = Application.Begin (top);

            // Act
            Application.End (rs);

            // Test
            Assert.NotNull (Application.Popover);
        }
        finally
        {
            top?.Dispose ();
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Application_End_Hides_Active ()
    {
        Toplevel? top = null;

        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");
            Application.StopAfterFirstIteration = true;

            top = new Toplevel ();
            SessionToken rs = Application.Begin (top);

            PopoverTestClass? popover = new ();

            Application.Popover?.Show (popover);
            Assert.True (popover.Visible);

            // Act
            Application.End (rs);

            // Test
            Assert.False (popover.Visible);
            Assert.NotNull (Application.Popover);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            top?.Dispose ();
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Application_Shutdown_Disposes_Registered_Popovers ()
    {
        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");

            PopoverTestClass? popover = new ();

            // Act
            Application.Popover?.Register (popover);
            Application.Shutdown ();

            // Test
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Application_Shutdown_Does_Not_Dispose_DeRegistered_Popovers ()
    {
        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");

            PopoverTestClass? popover = new ();

            Application.Popover?.Register (popover);

            // Act
            Application.Popover?.DeRegister (popover);
            Application.Shutdown ();

            // Test
            Assert.Equal (0, popover.DisposedCount);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Application_Shutdown_Does_Not_Dispose_ActiveNotRegistered_Popover ()
    {
        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");

            PopoverTestClass? popover = new ();

            Application.Popover?.Show (popover);
            Application.Popover?.DeRegister (popover);

            // Act
            Application.Shutdown ();

            // Test
            Assert.Equal (0, popover.DisposedCount);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Register_SetsTopLevel ()
    {
        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");
            Application.Current = new Toplevel ();
            PopoverTestClass? popover = new ();

            // Act
            Application.Popover?.Register (popover);

            // Assert
            Assert.Equal (Application.Current, popover.Toplevel);
        }
        finally
        {
            Application.ResetState (true);
        }
    }

    [Fact]
    public void Keyboard_Events_Go_Only_To_Popover_Associated_With_Toplevel ()
    {
        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");
            Application.Current = new Toplevel () { Id = "initialTop" };
            PopoverTestClass? popover = new ();
            int keyDownEvents = 0;
            popover.KeyDown += (s, e) =>
            {
                keyDownEvents++;
                e.Handled = true;
            }; // Ensure it handles the key

            Application.Popover?.Register (popover);

            // Act
            Application.RaiseKeyDownEvent (Key.A); // Goes to initialTop

            Application.Current = new Toplevel () { Id = "secondaryTop" };
            Application.RaiseKeyDownEvent (Key.A); // Goes to secondaryTop

            // Test
            Assert.Equal (1, keyDownEvents);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.ResetState (true);
        }
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
        PopoverTestClass? popover = null;

        try
        {
            // Arrange
            Assert.Null (Application.Popover);
            Application.Init (null, "fake");
            Application.Current = new ()
            {
                Frame = new (0, 0, 10, 10),
                Id = "top"
            };

            View? view = new ()
            {
                Id = "view",
                X = 1,
                Y = 1,
                Width = 2,
                Height = 2,
            };

            Application.Current.Add (view);

            popover = new ()
            {
                Id = "popover",
                X = 5,
                Y = 5,
                Width = 3,
                Height = 3,
            }; // at 5,5 to 8,8 (screen)

            View? popoverSubView = new ()
            {
                Id = "popoverSubView",
                X = 1,
                Y = 1,
                Width = 1,
                Height = 1,
            };

            popover.Add (popoverSubView);

            Application.Popover?.Show (popover);

            List<View?> found = View.GetViewsUnderLocation (new (mouseX, mouseY), ViewportSettingsFlags.TransparentMouse);

            string [] foundIds = found.Select (v => v!.Id).ToArray ();

            Assert.Equal (viewIdStrings, foundIds);
        }
        finally
        {
            popover?.Dispose ();
            Application.Current?.Dispose ();
            Application.ResetState (true);
        }
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
            AddCommand (Command.New, NewCommandHandler!);
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