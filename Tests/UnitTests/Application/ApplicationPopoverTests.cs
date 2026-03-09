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
            Application.Init (DriverRegistry.Names.ANSI);

            // Act
            Assert.NotNull (Application.Popovers);
        }
        finally
        {
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Application_Shutdown_Resets_PopoverManager ()
    {
        try
        {
            // Arrange

            Application.Init (DriverRegistry.Names.ANSI);

            // Act
            Assert.NotNull (Application.Popovers);

            Application.Shutdown ();

            // Test
        }
        finally
        {
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Application_End_Does_Not_Reset_PopoverManager ()
    {
        Runnable? top = null;

        try
        {
            // Arrange
            Application.Init (DriverRegistry.Names.ANSI);
            Assert.NotNull (Application.Popovers);
            Application.StopAfterFirstIteration = true;

            top = new ();
            SessionToken rs = Application.Begin (top);

            // Act
            Application.End (rs);

            // Test
            Assert.NotNull (Application.Popovers);
        }
        finally
        {
            top?.Dispose ();
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Application_End_Hides_Active ()
    {
        Runnable? top = null;

        try
        {
            // Arrange
            Application.Init (DriverRegistry.Names.ANSI);
            Application.StopAfterFirstIteration = true;

            top = new ();
            SessionToken rs = Application.Begin (top);

            PopoverTestClass? popover = new ();

            Application.Popovers?.Register (popover);
            Application.Popovers?.Show (popover);
            Assert.True (popover.Visible);

            // Act
            Application.End (rs);

            // Test
            Assert.False (popover.Visible);
            Assert.NotNull (Application.Popovers);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            top?.Dispose ();
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Application_Shutdown_Disposes_Registered_Popovers ()
    {
        try
        {
            // Arrange

            Application.Init (DriverRegistry.Names.ANSI);

            PopoverTestClass? popover = new ();

            // Act
            Application.Popovers?.Register (popover);
            Application.Shutdown ();

            // Test
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Application_Shutdown_Does_Not_Dispose_DeRegistered_Popovers ()
    {
        try
        {
            // Arrange

            Application.Init (DriverRegistry.Names.ANSI);

            PopoverTestClass? popover = new ();

            Application.Popovers?.Register (popover);

            // Act
            Application.Popovers?.DeRegister (popover);
            Application.Shutdown ();

            // Test
            Assert.Equal (0, popover.DisposedCount);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Application_Shutdown_Does_Not_Dispose_ActiveNotRegistered_Popover ()
    {
        try
        {
            // Arrange

            Application.Init (DriverRegistry.Names.ANSI);

            PopoverTestClass? popover = new ();
            Application.Popovers?.Register (popover);
            Application.Popovers?.Show (popover);
            Application.Popovers?.DeRegister (popover);

            // Act
            Application.Shutdown ();

            // Test
            Assert.Equal (0, popover.DisposedCount);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Register_SetsRunnable ()
    {
        try
        {
            // Arrange

            Application.Init (DriverRegistry.Names.ANSI);
            Application.Begin (new Runnable ());
            PopoverTestClass? popover = new ();

            // Act
            Application.Popovers?.Register (popover);

            // Assert
            Assert.Equal (Application.TopRunnableView as IRunnable, popover.Owner);
        }
        finally
        {
            Application.TopRunnableView?.Dispose ();
            Application.Shutdown ();
        }
    }

    [Fact]
    public void Keyboard_Events_Go_Only_To_Popover_Associated_With_Runnable ()
    {
        try
        {
            // Arrange
            Application.Init (DriverRegistry.Names.ANSI);

            Runnable<bool>? initialRunnable = new () { Id = "initialRunnable" };
            Application.Begin (initialRunnable);
            PopoverTestClass? popover = new ();
            var keyDownEvents = 0;

            popover.KeyDown += (s, e) =>
                               {
                                   keyDownEvents++;
                                   e.Handled = true;
                               }; // Ensure it handles the key

            Application.Popovers?.Register (popover);

            // Act
            Application.RaiseKeyDownEvent (Key.A); // Goes to initialRunnable

            Runnable<bool>? secondaryRunnable = new () { Id = "secondaryRunnable" };
            Application.Begin (secondaryRunnable);

            Application.RaiseKeyDownEvent (Key.A); // Goes to secondaryRunnable

            // Test
            Assert.Equal (1, keyDownEvents);

            popover.Dispose ();
            Assert.Equal (1, popover.DisposedCount);
        }
        finally
        {
            Application.Shutdown ();
        }
    }

    // See: https://github.com/gui-cs/Terminal.Gui/issues/4122
    [Theory]
    [InlineData (0, 0, new [] { "runnable" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (1, 1, new [] { "runnable", "view" })]
    [InlineData (5, 5, new [] { "runnable" })]
    [InlineData (6, 6, new [] { "popoverSubView" })]
    [InlineData (7, 7, new [] { "runnable" })]
    [InlineData (3, 3, new [] { "runnable" })]
    public void GetViewsUnderMouse_Supports_ActivePopover (int mouseX, int mouseY, string [] viewIdStrings)
    {
        PopoverTestClass? popover = null;

        try
        {
            // Arrange
            Application.Init (DriverRegistry.Names.ANSI);

            Runnable<bool>? runnable = new ()
            {
                Frame = new (0, 0, 10, 10),
                Id = "runnable"
            };
            Application.Begin (runnable);

            View? view = new ()
            {
                Id = "view",
                X = 1,
                Y = 1,
                Width = 2,
                Height = 2
            };

            runnable.Add (view);

            popover = new ()
            {
                Id = "popover",
                X = 5,
                Y = 5,
                Width = 3,
                Height = 3
            }; // at 5,5 to 8,8 (screen)

            View? popoverSubView = new ()
            {
                Id = "popoverSubView",
                X = 1,
                Y = 1,
                Width = 1,
                Height = 1
            };

            popover.Add (popoverSubView);
            Application.Popovers?.Register (popover);

            Application.Popovers?.Show (popover);

            List<View?> found = view.GetViewsUnderLocation (new (mouseX, mouseY), ViewportSettingsFlags.TransparentMouse);

            string [] foundIds = found.Select (v => v!.Id).ToArray ();

            Assert.Equal (viewIdStrings, foundIds);
        }
        finally
        {
            popover?.Dispose ();
            Application.Shutdown ();
        }
    }

    public class PopoverTestClass : PopoverImpl
    {
        public List<Key> HandledKeys { get; } = [];
        public int NewCommandInvokeCount { get; private set; }

#if DEBUG_IDISPOSABLE
        // NOTE: Hides the base DisposedCount property
        public new int DisposedCount { get; private set; }
#else
        public int DisposedCount { get; private set; }
#endif
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
