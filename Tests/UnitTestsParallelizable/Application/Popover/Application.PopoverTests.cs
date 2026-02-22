using Moq;

namespace ApplicationTests.Popover;

[Collection ("Application Tests")]
public class ApplicationPopoverTests
{
    [Fact]
    public void Register_AddsPopover ()
    {
        // Arrange
        IPopover popover = new Mock<IPopover> ().Object;
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
        IPopover popover = new Mock<IPopover> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);

        // Act
        bool result = popoverManager.DeRegister (popover);

        // Assert
        Assert.True (result);
        Assert.DoesNotContain (popover, popoverManager.Popovers);
    }

    [Fact]
    public void Show_SetsActivePopover ()
    {
        // Arrange
        PopoverTestClass popover = new Mock<PopoverTestClass> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);

        // Act
        popoverManager.Show (popover);

        // Assert
        Assert.Equal (popover, popoverManager.GetActivePopover ());
    }

    [Fact]
    public void Hide_ClearsActivePopover ()
    {
        // Arrange
        IPopover popover = new Mock<IPopover> ().Object;
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);
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
        var popover = new PopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);

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
        var popover = new PopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (popover);
        popoverManager.Show (popover);

        // Act
        popoverManager.DispatchKeyDown (Key.N.WithCtrl);

        // Assert
        Assert.Equal (1, popover.NewCommandInvokeCount);
        Assert.Contains (Key.N.WithCtrl, popover.HandledKeys);
    }

    [Fact]
    public void DispatchKeyDown_InactivePopoverGetsHotKey ()
    {
        // Arrange
        var activePopover = new PopoverTestClass { Id = "activePopover" };
        var inactivePopover = new PopoverTestClass { Id = "inactivePopover" };

        var popoverManager = new ApplicationPopover ();

        popoverManager.Register (activePopover);
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
        var activePopover = new PopoverTestClass ();
        var inactivePopover = new PopoverTestClass ();
        var popoverManager = new ApplicationPopover ();
        popoverManager.Register (activePopover);

        popoverManager.Show (activePopover);
        popoverManager.Register (inactivePopover);

        // Act
        popoverManager.DispatchKeyDown (Key.A);

        // Assert
        Assert.Contains (Key.A, activePopover.HandledKeys);
        Assert.NotEmpty (inactivePopover.HandledKeys);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Issue 2: DispatchKeyDown crashes if popover is deregistered during iteration.
    ///     Calling DeRegister during DispatchKeyDown iteration should not throw.
    /// </summary>
    [Fact]
    public void DispatchKeyDown_DeRegisterDuringIteration_DoesNotThrow ()
    {
        // Arrange
        ApplicationPopover popoverManager = new ();
        PopoverTestClass first = new () { Id = "first" };
        PopoverTestClass second = new () { Id = "second" };

        popoverManager.Register (first);
        popoverManager.Register (second);

        // Make the first popover deregister the second when it receives a key
        first.OnKeyDownCallback = _ =>
                                  {
                                      popoverManager.DeRegister (second);

                                      return false;
                                  };

        // Act & Assert — should not throw InvalidOperationException
        Exception? ex = Record.Exception (() => popoverManager.DispatchKeyDown (Key.A));
        Assert.Null (ex);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Issue 6: Dispose() doesn't clear _activePopover.
    ///     After Dispose, GetActivePopover should return null.
    /// </summary>
    [Fact]
    public void Dispose_ClearsActivePopover ()
    {
        // Arrange
        PopoverTestClass popover = new ();
        ApplicationPopover popoverManager = new ();
        popoverManager.Register (popover);
        popoverManager.Show (popover);
        Assert.NotNull (popoverManager.GetActivePopover ());

        // Act
        popoverManager.Dispose ();

        // Assert
        Assert.Null (popoverManager.GetActivePopover ());
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Issue 9: Hide() silently no-ops for non-active popovers.
    ///     This is a coverage test documenting existing behavior.
    /// </summary>
    [Fact]
    public void Hide_NonActivePopover_DoesNotAffectActivePopover ()
    {
        // Arrange
        PopoverTestClass popover1 = new () { Id = "popover1" };
        PopoverTestClass popover2 = new () { Id = "popover2" };
        ApplicationPopover popoverManager = new ();
        popoverManager.Register (popover1);
        popoverManager.Register (popover2);
        popoverManager.Show (popover1);

        // Remember popover2's initial visible state (Views default to Visible = true)
        bool initialVisibleState = popover2.Visible;

        // popover2 is registered but not active
        // Act
        popoverManager.Hide (popover2);

        // Assert — popover1 is still active
        Assert.Equal (popover1, popoverManager.GetActivePopover ());

        // popover2 was never shown via Show(), so its Visible state should be unchanged
        // (Hide only affects the active popover, which popover2 is not)
        Assert.Equal (initialVisibleState, popover2.Visible);
    }

    public class PopoverTestClass : View, IPopover
    {
        public List<Key> HandledKeys { get; } = [];
        public int NewCommandInvokeCount { get; private set; }

        /// <summary>
        ///     Optional callback invoked during OnKeyDown. Can be used to test
        ///     modification of popover state during key dispatch.
        /// </summary>
        public Func<Key, bool>? OnKeyDownCallback { get; set; }

        public PopoverTestClass ()
        {
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;
            CanFocus = true;
            AddCommand (Command.New, NewCommandHandler!);
            HotKeyBindings.Add (Key.N.WithCtrl, Command.New);

            AddCommand (Command.Quit, Quit);
            KeyBindings.Add (Application.QuitKey, Command.Quit);

            return;

            bool? Quit (ICommandContext? ctx)
            {
                if (!Visible)
                {
                    return false;
                }

                Visible = false;

                return true;
            }

            bool? NewCommandHandler (ICommandContext ctx)
            {
                NewCommandInvokeCount++;

                return false;
            }
        }

        protected override bool OnKeyDown (Key key)
        {
            HandledKeys.Add (key);

            // Call the callback if set, allowing tests to modify state during key dispatch
            if (OnKeyDownCallback is { })
            {
                return OnKeyDownCallback (key);
            }

            return false;
        }

        /// <inheritdoc/>
        public IRunnable? Current { get; set; }
    }
}
