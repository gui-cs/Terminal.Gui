#nullable disable
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Pure unit tests for <see cref="Button"/> that don't require Application static dependencies.
///     These tests can run in parallel without interference.
/// </summary>
public class ButtonTests : FakeDriverBase
{
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new Button ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.LeftBracket} Hello {Glyphs.RightBracket}", view.TextFormatter.Text);
        view.Dispose ();
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new Button ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.LeftBracket} Hello {Glyphs.RightBracket}", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);
        view.Dispose ();
    }

    [Theory]
    [InlineData ("01234", 0, 0, 0, 0)]
    [InlineData ("01234", 1, 0, 1, 0)]
    [InlineData ("01234", 0, 1, 0, 1)]
    [InlineData ("01234", 1, 1, 1, 1)]
    [InlineData ("01234", 10, 1, 10, 1)]
    [InlineData ("01234", 10, 3, 10, 3)]
    [InlineData ("0_1234", 0, 0, 0, 0)]
    [InlineData ("0_1234", 1, 0, 1, 0)]
    [InlineData ("0_1234", 0, 1, 0, 1)]
    [InlineData ("0_1234", 1, 1, 1, 1)]
    [InlineData ("0_1234", 10, 1, 10, 1)]
    [InlineData ("0_12你", 10, 3, 10, 3)]
    [InlineData ("0_12你", 0, 0, 0, 0)]
    [InlineData ("0_12你", 1, 0, 1, 0)]
    [InlineData ("0_12你", 0, 1, 0, 1)]
    [InlineData ("0_12你", 1, 1, 1, 1)]
    [InlineData ("0_12你", 10, 1, 10, 1)]
    public void Button_AbsoluteSize_Text (string text, int width, int height, int expectedWidth, int expectedHeight)
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button
        {
            Text = text,
            Width = width,
            Height = height
        };

        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), btn1.GetContentSize ());
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (1, 0, 1, 0)]
    [InlineData (0, 1, 0, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (10, 1, 10, 1)]
    [InlineData (10, 3, 10, 3)]
    public void Button_AbsoluteSize_DefaultText (int width, int height, int expectedWidth, int expectedHeight)
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button ();
        btn1.Width = width;
        btn1.Height = height;

        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    [Fact]
    public void Button_HotKeyChanged_EventFires ()
    {
        var btn = new Button { Text = "_Yar" };

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }

    [Fact]
    public void Button_HotKeyChanged_EventFires_WithNone ()
    {
        var btn = new Button ();

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Null, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }

    [Fact]
    public void HotKeyChange_Works ()
    {
        var clicked = false;
        var btn = new Button { Text = "_Test" };
        btn.Accepting += (_, _) => clicked = true;

        Assert.Equal (KeyCode.T, btn.HotKey);
        Assert.False (btn.NewKeyDownEvent (Key.T)); // Button processes, but does not handle
        Assert.True (clicked);

        clicked = false;
        Assert.False (btn.NewKeyDownEvent (Key.T.WithAlt)); // Button processes, but does not handle
        Assert.True (clicked);

        clicked = false;
        btn.HotKey = KeyCode.E;
        Assert.False (btn.NewKeyDownEvent (Key.E.WithAlt)); // Button processes, but does not handle
        Assert.True (clicked);
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Space_Fires_Accept (bool focused, int expected)
    {
        var superView = new View
        {
            CanFocus = true
        };

        Button button = new ();

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (_, _) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Space);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Enter_Fires_Accept (bool focused, int expected)
    {
        var superView = new View
        {
            CanFocus = true
        };

        Button button = new ();

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (_, _) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 1)]
    public void HotKey_Fires_Accept (bool focused, int expected)
    {
        var superView = new View
        {
            CanFocus = true
        };

        Button button = new ()
        {
            HotKey = Key.A
        };

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (_, _) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.A);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Accepts ()
    {
        var btn = new Button { Text = "_Test" };
        var accepted = false;
        btn.Accepting += (_, _) => accepted = true;

        Assert.Equal (KeyCode.T, btn.HotKey);
        btn.InvokeCommand (Command.HotKey);
        Assert.True (accepted);
    }

    [Fact]
    public void Accept_Event_Returns_True ()
    {
        var btn = new Button { Text = "Test" };
        var acceptInvoked = false;

        btn.Accepting += (_, e) =>
                         {
                             acceptInvoked = true;
                             e.Handled = true;
                         };

        Assert.True (btn.InvokeCommand (Command.Accept));
        Assert.True (acceptInvoked);
    }

    [Fact]
    public void Setting_Empty_Text_Sets_HoKey_To_KeyNull ()
    {
        var btn = new Button { Text = "_Test" };

        Assert.Equal (KeyCode.T, btn.HotKey);

        btn.Text = "";

        Assert.Equal (KeyCode.Null, btn.HotKey);
    }

    /// <summary>
    ///     Tests that Button's Accepting event fires correctly when using driver injection with timestamps.
    ///     Uses InjectMouseEventDirectly to bypass ANSI encoding which cannot preserve timestamps.
    /// </summary>
    [Fact]
    public void LeftButtonClicked_Accepts_Driver_Injection_With_Timestamps ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var button = new Button
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me"
        };

        var top = new Runnable { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        var activatingCount = 0;
        var acceptingCount = 0;

        button.Activating += (_, _) => activatingCount++;
        button.Accepting += (_, _) => acceptingCount++;

        // Act - Inject a complete click sequence (Press -> Release) with timestamps
        // Use InjectMouseEventDirectly to preserve timestamps through the pipeline
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (7, 6); // Inside button at screen coordinates

        // Inject Press and Release to generate a Clicked event
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (50)
        });

        // Assert - Button should receive Clicked event and fire both Activating and Accepting
        Assert.Equal (1, activatingCount);
        Assert.Equal (1, acceptingCount);

        // Act - Second click with timestamp spacing >500ms should be a new single click
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime.AddMilliseconds (600)
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (650)
        });

        // Assert - Should fire again (two independent single clicks, not a double-click)
        Assert.Equal (2, activatingCount);
        Assert.Equal (2, acceptingCount);

        // Cleanup
        app.End (token);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests that Button receives double-click events correctly when using driver injection with timestamps.
    ///     Uses InjectMouseEventDirectly to bypass ANSI encoding which cannot preserve timestamps.
    /// </summary>
    [Fact]
    public void LeftButtonDoubleClicked_Accepts_Driver_Injection_With_Timestamps ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var button = new Button
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me"
        };

        var top = new Runnable { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        List<MouseFlags> receivedFlags = [];

        button.MouseEvent += (_, e) =>
        {
            receivedFlags.Add (e.Flags);
        };

        // Act - Inject two clicks with <500ms spacing to generate a double-click
        // Use InjectMouseEventDirectly to preserve timestamps through the pipeline
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (7, 6); // Inside button at screen coordinates

        // First click
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (50)
        });

        // Second click within 500ms threshold
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime.AddMilliseconds (300)
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (350)
        });

        // Assert - Should receive a double-click event (timestamp spacing allows multi-click)
        // We don't assert exact count since View may emit additional events,
        // but we verify the double-click event was generated
        Assert.Contains (MouseFlags.LeftButtonDoubleClicked, receivedFlags);

        // Also verify we got the first single-click
        Assert.Contains (MouseFlags.LeftButtonClicked, receivedFlags);

        // Cleanup
        app.End (token);
        top.Dispose ();
    }

    [Fact]
    public void TestAssignTextToButton ()
    {
        var btn = new Button { Text = "_K Ok" };

        Assert.Equal ("_K Ok", btn.Text);

        btn.Text = "_N Btn";

        Assert.Equal ("_N Btn", btn.Text);
    }

    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var button = new Button ();
        var acceptInvoked = false;

        button.Accepting += ButtonAccept;

        bool? ret = button.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        button.Dispose ();

        return;

        void ButtonAccept (object sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void LeftButtonPressed_Activates ()
    {
        Button button = new () { Text = "_Button" };
        Assert.True (button.CanFocus);

        var activatingCount = 0;
        button.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;
        Assert.True (button.HasFocus);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // When ShouldAutoGrab is true (Button default: MouseHighlightStates = In | Pressed | PressedOutside),
        // Pressed events do NOT invoke commands - only Clicked events do.
        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, activatingCount); // No command invocation on Pressed when ShouldAutoGrab
        Assert.Equal (0, acceptingCount);

        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // When MouseHighlightStates = None, ShouldAutoGrab = false, so Pressed DOES invoke commands
        button.MouseHighlightStates = MouseState.None;
        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (1, activatingCount); // Now Pressed invokes Command.Activate
        Assert.Equal (0, acceptingCount);

        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (2, activatingCount);
        Assert.Equal (0, acceptingCount);
    }

    [Fact (Skip = "Broke in #4417")]
    public void LeftButtonClicked_Accepts ()
    {
        Button button = new () { Text = "_Button" };
        Assert.True (button.CanFocus);

        var activatingCount = 0;
        button.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;
        Assert.True (button.HasFocus);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (1, activatingCount);
        Assert.Equal (1, acceptingCount);

        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (2, activatingCount);
        Assert.Equal (2, acceptingCount);

        // Disable Mouse Highlighting to test that it does not interfere with Accepting event
        button.MouseHighlightStates = MouseState.None;
        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (3, activatingCount);
        Assert.Equal (3, acceptingCount);

        button.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (4, activatingCount);
        Assert.Equal (4, acceptingCount);
    }

    [Fact]
    public void LeftButtonClicked_Accepts_Driver_Injection ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new () { Text = "_Button", X = 0, Y = 0, Width = 10, Height = 3 };
        runnable.Add (button);
        runnable.Layout ();

        var activatingCount = 0;
        button.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;
        Assert.True (button.HasFocus);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // Use deterministic base time instead of DateTime.Now for predictable test behavior
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (2, 1); // Inside button at screen coordinates (accounting for Runnable border)

        // First click
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) });

        Assert.Equal (1, acceptingCount);
        Assert.Equal (1, activatingCount);

        // Second click - more than 500ms later to avoid double-click detection
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (650) });

        Assert.Equal (2, acceptingCount);
        Assert.Equal (2, activatingCount);
    }

    /// <summary>
    ///     Tests that Button's Accepting event fires correctly when MouseHighlightStates is disabled.
    ///     When MouseHighlightStates = None, ShouldAutoGrab = false, which changes the event handling behavior:
    ///     - Pressed events invoke Command.Activate (fires Activating only)
    ///     - Clicked events invoke Command.Accept (fires both Activating and Accepting)
    ///     Uses InjectMouseEventDirectly to bypass ANSI encoding which cannot preserve timestamps.
    /// </summary>
    [Fact]
    public void LeftButtonClicked_Accepts_Driver_Injection_With_MouseHighlightStates_None ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_Button",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3,
            MouseHighlightStates = MouseState.None // Disable auto-grab
        };
        runnable.Add (button);
        runnable.Layout ();

        var activatingCount = 0;
        button.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;
        Assert.True (button.HasFocus);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // Use deterministic base time for predictable test behavior
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (2, 1); // Inside button at screen coordinates (accounting for Runnable border)

        // First click - when MouseHighlightStates = None:
        // - Pressed event fires Activating (Command.Activate)
        // - Clicked event fires both Activating and Accepting (Command.Accept)
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) });

        // Activating: 1 from Pressed + 1 from Clicked = 2
        // Accepting: 1 from Clicked only = 1
        Assert.Equal (2, activatingCount);
        Assert.Equal (1, acceptingCount);

        // Second click - more than 500ms later to avoid double-click detection
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (650) });

        // Activating: previous 2 + 1 from Pressed + 1 from Clicked = 4
        // Accepting: previous 1 + 1 from Clicked = 2
        Assert.Equal (4, activatingCount);
        Assert.Equal (2, acceptingCount);

        // Third click - verify it continues to work
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (1200) });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (1250) });

        // Activating: previous 4 + 1 from Pressed + 1 from Clicked = 6
        // Accepting: previous 2 + 1 from Clicked = 3
        Assert.Equal (6, activatingCount);
        Assert.Equal (3, acceptingCount);

        // Fourth click - verify consistency
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (1800) });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (1850) });

        // Activating: previous 6 + 1 from Pressed + 1 from Clicked = 8
        // Accepting: previous 3 + 1 from Clicked = 4
        Assert.Equal (8, activatingCount);
        Assert.Equal (4, acceptingCount);
    }

    /// <summary>
    ///     Tests Button.MouseHoldRepeat = true behavior with quick single click.
    ///     Per spec: Quick click (press + immediate release within 100ms) should fire Accept once.
    ///     Uses InjectMouseEventDirectly to bypass ANSI encoding and control timing precisely.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_QuickSingleClick_FiresAcceptOnce ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_Button",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3,
            MouseHoldRepeat = true // Enable hold-repeat behavior
        };
        runnable.Add (button);
        runnable.Layout ();

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;

        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (2, 1);

        // Quick single click - press and release within 50ms (too fast for timer to start)
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) });

        // Should fire Accept once from the Clicked event
        Assert.Equal (1, acceptingCount);
    }

    /// <summary>
    ///     Tests that MouseHoldRepeat button fires Accept on quick double-click.
    ///     Per spec: When MouseHoldRepeat=true, Press/Release events are used (Click events ignored).
    ///     Each Press/Release cycle fires exactly one Accept.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_QuickDoubleClick_FiresAcceptTwice ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var button = new Button
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me",
            MouseHoldRepeat = true
        };

        var top = new Runnable { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        // Act - Quick double-click (both clicks within 500ms window)
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (7, 6); // Inside button at screen coordinates

        // First click
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (50)
        });

        // Second click within 500ms threshold (creates double-click, but we ignore Click events)
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime.AddMilliseconds (300)
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (350)
        });

        // Assert - Should fire Accept twice (one per Press/Release cycle)
        // MouseHoldRepeat uses Press/Release events only, ignoring Click/DoubleClick synthesized events
        Assert.Equal (2, acceptingCount);

        // Cleanup
        app.End (token);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests that MouseHoldRepeat button fires Accept on quick triple-click.
    ///     Per spec: When MouseHoldRepeat=true, Press/Release events are used (Click events ignored).
    ///     Each Press/Release cycle fires exactly one Accept.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_QuickTripleClick_FiresAcceptThreeTimes ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var button = new Button
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me",
            MouseHoldRepeat = true
        };

        var top = new Runnable { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        // Act - Quick triple-click (all clicks within multi-click threshold)
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (7, 6); // Inside button at screen coordinates

        // First click
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (50)
        });

        // Second click
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime.AddMilliseconds (200)
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (250)
        });

        // Third click
        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime.AddMilliseconds (400)
        });

        app.InjectMouseEventDirectly (new ()
        {
            ScreenPosition = clickPos,
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (450)
        });

        // Assert - Should fire Accept three times (one per Press/Release cycle)
        // MouseHoldRepeat uses Press/Release events only, ignoring Click/DoubleClick/TripleClick synthesized events
        Assert.Equal (3, acceptingCount);

        // Cleanup
        app.End (token);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests Button.MouseHoldRepeat = true with spaced clicks (not a multi-click sequence).
    ///     Per spec: Clicks spaced >500ms apart should each fire Accept once independently.
    ///     Uses InjectMouseEventDirectly to bypass ANSI encoding and control timing precisely.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_SpacedClicks_FiresAcceptForEach ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_Button",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3,
            MouseHoldRepeat = true
        };
        runnable.Add (button);
        runnable.Layout ();

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;

        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        var clickPos = new Point (2, 1);

        // First click
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) });

        Assert.Equal (1, acceptingCount);

        // Second click - more than 500ms later (not a double-click)
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (650) });

        Assert.Equal (2, acceptingCount);

        // Third click - more than 500ms after second
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (1200) });
        app.InjectMouseEventDirectly (new () { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (1250) });

        Assert.Equal (3, acceptingCount);
    }
}
