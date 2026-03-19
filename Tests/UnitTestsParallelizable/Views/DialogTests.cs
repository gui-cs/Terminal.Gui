using UnitTests;

namespace ViewsTests;

/// <summary>
///     Pure unit tests for <see cref=\"Dialog\"/> that don't require Application static dependencies.
///     These tests can run in parallel without interference.
/// </summary>
/// <remarks>
///     CoPilot - GitHub Copilot v4
/// </remarks>
public class DialogTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Add_SubView_Updates_Dialog ()
    {
        Dialog dialog = new ();

        Label label = new () { Text = "Hello World", X = 0, Y = 0 };

        dialog.Add (label);

        Assert.Contains (label, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void AddButton_Adds_Button_To_Dialog ()
    {
        Dialog dialog = new ();
        Button button = new () { Title = "OK" };

        dialog.AddButton (button);

        Assert.Single (dialog.Buttons);
        Assert.Equal ("OK", dialog.Buttons [0].Title);
        Assert.True (button.IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void AddButton_Multiple_Buttons_Last_IsDefault ()
    {
        Dialog dialog = new ();
        Button button1 = new () { Title = "Cancel" };
        Button button2 = new () { Title = "OK" };
        Button button3 = new () { Title = "Apply" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);
        dialog.AddButton (button3);

        Assert.Equal (3, dialog.Buttons.Length);
        Assert.False (button1.IsDefault);
        Assert.False (button2.IsDefault);
        Assert.True (button3.IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void AddButton_Sets_Button_Position ()
    {
        Dialog dialog = new ();
        Button button = new () { Title = "OK" };

        dialog.AddButton (button);

        Assert.True (button.X.Has<PosAlign> (out _));
        Assert.Equal (1, button.Y);

        dialog.Dispose ();
    }

    [Fact]
    public void ButtonAlignment_Get_Set ()
    {
        Dialog dialog = new ();

        Assert.Equal (Alignment.End, dialog.ButtonAlignment);

        dialog.ButtonAlignment = Alignment.Start;
        Assert.Equal (Alignment.Start, dialog.ButtonAlignment);

        dialog.ButtonAlignment = Alignment.Center;
        Assert.Equal (Alignment.Center, dialog.ButtonAlignment);

        dialog.Dispose ();
    }

    [Theory]
    [InlineData (Alignment.Start)]
    [InlineData (Alignment.Center)]
    [InlineData (Alignment.End)]
    [InlineData (Alignment.Fill)]
    public void ButtonAlignment_Theory (Alignment alignment)
    {
        Dialog dialog = new () { ButtonAlignment = alignment };

        Assert.Equal (alignment, dialog.ButtonAlignment);

        dialog.Dispose ();
    }

    [Fact]
    public void ButtonAlignmentModes_Get_Set ()
    {
        Dialog dialog = new ();

        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);

        dialog.ButtonAlignmentModes = AlignmentModes.StartToEnd;
        Assert.Equal (AlignmentModes.StartToEnd, dialog.ButtonAlignmentModes);

        dialog.ButtonAlignmentModes = AlignmentModes.IgnoreFirstOrLast;
        Assert.Equal (AlignmentModes.IgnoreFirstOrLast, dialog.ButtonAlignmentModes);

        dialog.Dispose ();
    }

    [Fact]
    public void Buttons_Property_Set_Adds_Buttons ()
    {
        Dialog dialog = new ();

        Button [] buttons = [new () { Title = "Cancel" }, new () { Title = "OK" }];

        dialog.Buttons = buttons;

        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal ("Cancel", dialog.Buttons [0].Title);
        Assert.Equal ("OK", dialog.Buttons [1].Title);
        Assert.True (dialog.Buttons [1].IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void Canceled_False_When_Result_Null ()
    {
        Dialog dialog = new ();

        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void Constructor_Initializes_DefaultValues ()
    {
        Dialog dialog = new ();

        Assert.NotNull (dialog);
        Assert.True (dialog.CanFocus);
        Assert.Equal (Alignment.End, dialog.ButtonAlignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);
        Assert.Equal (LineStyle.Heavy, dialog.BorderStyle);
        Assert.Equal (ShadowStyles.Transparent, dialog.ShadowStyle);
        Assert.Empty (dialog.Buttons);
        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled); // Canceled is true when Result is null
        Assert.Equal (ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void Constructor_Sets_AutoDimensions ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.Width.Has<DimAuto> (out _));
        Assert.True (dialog.Height.Has<DimAuto> (out _));

        dialog.Dispose ();
    }

    [Fact]
    public void Constructor_Sets_Position_Center ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.X.Has<PosCenter> (out _));
        Assert.True (dialog.Y.Has<PosCenter> (out _));

        dialog.Dispose ();
    }

    [Fact]
    public void Arrangement_Default ()
    {
        Dialog dialog = new ();

        Assert.Equal (ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void Border_Style_Can_Be_Changed ()
    {
        Dialog dialog = new () { BorderStyle = LineStyle.Single };

        Assert.Equal (LineStyle.Single, dialog.BorderStyle);

        dialog.BorderStyle = LineStyle.Double;
        Assert.Equal (LineStyle.Double, dialog.BorderStyle);

        dialog.Dispose ();
    }

    [Fact]
    public void CanFocus_Default_True ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.CanFocus);

        dialog.Dispose ();
    }

    [Fact]
    public void Command_Accept_SetsResultAndStops ()
    {
        Dialog dialog = new () { Title = "Test" };
        Button button = new () { Text = "OK" };
        dialog.AddButton (button);

        var acceptingFired = false;

        dialog.Accepting += (_, e) =>
                            {
                                acceptingFired = true;
                                e.Handled = true;
                            };

        // Accept command on dialog should propagate through default button
        dialog.InvokeCommand (Command.Accept);

        // The accepting event on dialog fires
        Assert.True (acceptingFired);

        dialog.Dispose ();
    }

    [Fact]
    public void DialogButton_Accept_BubblesUp ()
    {
        Dialog dialog = new () { Title = "Test" };
        Button button = new () { Text = "OK" };
        dialog.AddButton (button);

        Assert.Equal (dialog.DefaultAcceptView, button);

        var buttonAcceptingFired = false;

        button.Accepting += (_, e) => { buttonAcceptingFired = true; };

        var dialogAcceptedFired = false;

        dialog.Accepted += (_, e) => { dialogAcceptedFired = true; };

        // Button's Accept should fire
        button.InvokeCommand (Command.Accept);

        Assert.True (buttonAcceptingFired);
        Assert.True (dialogAcceptedFired);

        dialog.Dispose ();
    }

    [Fact]
    public void Modal_DialogButton_Accept_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();
        dialog.Title = "Test";
        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (0, okAcceptedFired);
        Assert.Equal (1, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            okButton.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Modal_DialogButton_Cancel_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();
        dialog.Title = "Test";
        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (0, dialogAcceptedFired); // 0 because Cancel's OnAccepting handled it; RaiseAccepted is not called
        Assert.Equal (1, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (0, okAcceptingFired);
        Assert.Equal (0, okAcceptedFired);
        Assert.Equal (0, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            cancelButton.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Disposes_Buttons ()
    {
        Dialog dialog = new ();
        Button button1 = new () { Title = "OK" };
        Button button2 = new () { Title = "Cancel" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);

        Assert.Equal (2, dialog.Buttons.Length);

        dialog.Dispose ();

#if DEBUG_IDISPOSABLE

        // After disposal, buttons should be disposed through the dialog's disposal chain
        Assert.True (dialog.WasDisposed);
        Assert.True (button1.WasDisposed);
        Assert.True (button2.WasDisposed);
#endif
    }

    [Fact]
    public void ShadowStyle_Can_Be_Changed ()
    {
        Dialog dialog = new () { ShadowStyle = null };

        Assert.Null (dialog.ShadowStyle);

        dialog.ShadowStyle = ShadowStyles.Opaque;
        Assert.Equal (ShadowStyles.Opaque, dialog.ShadowStyle);

        dialog.Dispose ();
    }

    [Fact]
    public void Text_Property ()
    {
        Dialog dialog = new () { Text = "This is a message" };

        Assert.Equal ("This is a message", dialog.Text);

        dialog.Text = "Updated message";
        Assert.Equal ("Updated message", dialog.Text);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Multiple_SubViews ()
    {
        Dialog dialog = new () { Title = "Form" };

        Label nameLabel = new () { Text = "Name:", X = 0, Y = 0 };

        TextField nameField = new () { X = Pos.Right (nameLabel) + 1, Y = 0, Width = 20 };

        Label emailLabel = new () { Text = "Email:", X = 0, Y = 1 };

        TextField emailField = new () { X = Pos.Right (emailLabel) + 1, Y = 1, Width = 20 };

        dialog.Add (nameLabel, nameField, emailLabel, emailField);

        Button okButton = new () { Title = "OK" };
        dialog.AddButton (okButton);

        Assert.Equal (4, dialog.SubViews.Count);
        Assert.Single (dialog.Buttons);
        Assert.Contains (nameLabel, dialog.SubViews);
        Assert.Contains (nameField, dialog.SubViews);
        Assert.Contains (emailLabel, dialog.SubViews);
        Assert.Contains (emailField, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Title_And_Buttons ()
    {
        Dialog dialog = new () { Title = "Confirm" };

        Button cancelButton = new () { Title = "Cancel" };
        Button okButton = new () { Title = "OK" };

        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        Assert.Equal ("Confirm", dialog.Title);
        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal ("Cancel", dialog.Buttons [0].Title);
        Assert.Equal ("OK", dialog.Buttons [1].Title);
        Assert.False (cancelButton.IsDefault);
        Assert.True (okButton.IsDefault);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Wide_Character_Button_Text ()
    {
        Dialog dialog = new ();
        Button button = new () { Title = "确定" };

        dialog.AddButton (button);

        Assert.Single (dialog.Buttons);
        Assert.Equal ("确定", dialog.Buttons [0].Title);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Wide_Character_Title ()
    {
        Dialog dialog = new () { Title = "你好世界" };

        Assert.Equal ("你好世界", dialog.Title);

        dialog.Dispose ();
    }

    [Fact]
    public void Empty_Has_No_Buttons ()
    {
        Dialog dialog = new ();

        Assert.Empty (dialog.Buttons);
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Result_Can_Be_Set_And_Retrieved ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        Assert.Null (dialog.Result);

        dialog.Result = 0;
        Assert.Equal (0, dialog.Result);

        dialog.Result = 1;
        Assert.Equal (1, dialog.Result);

        dialog.Result = null;
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Title_Get_Set ()
    {
        Dialog dialog = new ();

        Assert.Equal (string.Empty, dialog.Title);

        dialog.Title = "Test Dialog";
        Assert.Equal ("Test Dialog", dialog.Title);

        dialog.Title = "你好";
        Assert.Equal ("你好", dialog.Title);

        dialog.Dispose ();
    }

    #region Layout Tests

    [Fact]
    public void EnableForDesign_Initializes_With_Content ()
    {
        IDriver driver = CreateTestDriver ();
        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.Equal ("Dialog Title", dialog.Title);
        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal (Strings.btnCancel, dialog.Buttons [0].Title);
        Assert.Equal (Strings.btnOk, dialog.Buttons [1].Title);
        Assert.True (dialog.Buttons [1].IsDefault);

        // Should have label and textfield
        Assert.True (dialog.SubViews.Count >= 2);

        dialog.Dispose ();
    }

    [Fact]
    public void Layout_With_EnableForDesign_Default_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Dialog should be centered with DimAuto
        Assert.True (dialog.X.Has<PosCenter> (out _));
        Assert.True (dialog.Y.Has<PosCenter> (out _));
        Assert.True (dialog.Width.Has<DimAuto> (out _));
        Assert.True (dialog.Height.Has<DimAuto> (out _));

        // Frame should be calculated based on content
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        // Should fit within screen
        Assert.True (dialog.Frame.Width <= 80);
        Assert.True (dialog.Frame.Height <= 25);

        dialog.Dispose ();
    }

    [Fact]
    public void Layout_With_Small_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Should calculate size based on content
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        // With small container, dialog may need more space than available
        // Just verify it laid out successfully
        Assert.NotEqual (Rectangle.Empty, dialog.Frame);

        dialog.Dispose ();
    }

    [Fact]
    public void Layout_With_Large_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (200, 100);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Should calculate size based on content, not fill entire container
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        // DimAuto with max 100% - 2 means won't exceed 198x98
        Assert.True (dialog.Frame.Width <= 198);
        Assert.True (dialog.Frame.Height <= 98);

        dialog.Dispose ();
    }

    [Fact]
    public void Width_Height_DimAuto_Calculates_Based_On_Content ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver, Title = "Test" };

        // Add content that requires specific size
        Label label = new () { Text = "This is a label", X = 0, Y = 0 };
        TextField textField = new () { X = Pos.Right (label) + 1, Y = 0, Width = 30, Text = "Input here" };

        dialog.Add (label, textField);

        Button okButton = new () { Title = "OK" };
        dialog.AddButton (okButton);

        dialog.Layout ();

        // Width should accommodate: label + space + textfield + padding + border
        int expectedMinWidth = label.Text.GetColumns () + 1 + 30;

        Assert.True (dialog.Frame.Width >= expectedMinWidth);

        dialog.Dispose ();
    }

    [Fact]
    public void Height_Accounts_For_Buttons ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver };

        Button button1 = new () { Title = "Cancel" };
        Button button2 = new () { Title = "OK" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);

        dialog.Layout ();

        // Height should account for buttons at the bottom
        Assert.True (dialog.Frame.Height > 0);
        Assert.True (dialog.Padding.Thickness.Bottom >= button1.Frame.Height);

        dialog.Dispose ();
    }

    [Fact]
    public void Respects_Explicit_Width_Height ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver, Width = 50, Height = 15 };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Should use explicit dimensions
        Assert.Equal (50, dialog.Frame.Width);
        Assert.Equal (15, dialog.Frame.Height);

        dialog.Dispose ();
    }

    [Fact]
    public void Padding_Affects_Content_Area ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new () { Driver = driver };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Padding should be set up correctly
        Assert.NotNull (dialog.Padding);

        // Buttons should be in a button container within padding
        Assert.True (dialog.Padding.SubViews.Count > 0);

        // Padding bottom should accommodate buttons
        Assert.True (dialog.Padding.Thickness.Bottom > 0);

        dialog.Dispose ();
    }

    [Fact]
    public void Multiple_Buttons_Layout_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver, ButtonAlignment = Alignment.End, ButtonAlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems
        };

        Button button1 = new () { Title = "Help" };
        Button button2 = new () { Title = "Cancel" };
        Button button3 = new () { Title = "OK" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);
        dialog.AddButton (button3);

        dialog.Layout ();

        // All buttons should be at same Y position (AnchorEnd)
        Assert.Equal (button1.Frame.Y, button2.Frame.Y);
        Assert.Equal (button2.Frame.Y, button3.Frame.Y);

        // Buttons should be aligned using PosAlign
        Assert.True (button1.X.Has (out PosAlign align1));
        Assert.True (button2.X.Has (out PosAlign align2));
        Assert.True (button3.X.Has (out PosAlign align3));

        // All should use same GroupId (Dialog's hash code)
        Assert.Equal (align1.GroupId, align2!.GroupId);
        Assert.Equal (align2.GroupId, align3!.GroupId);

        dialog.Dispose ();
    }

    [Fact]
    public void With_Text_Property_Affects_Height ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog1 = new () { Driver = driver };

        Button button = new () { Title = "OK" };
        dialog1.AddButton (button);

        dialog1.Layout ();
        int heightWithoutText = dialog1.Frame.Height;

        Dialog dialog2 = new () { Driver = driver, Text = "Line 1\nLine 2\nLine 3" };

        dialog2.AddButton (new Button { Title = "OK" });

        dialog2.Layout ();
        int heightWithText = dialog2.Frame.Height;

        // Dialog with text should be taller
        Assert.True (heightWithText > heightWithoutText);

        dialog1.Dispose ();
        dialog2.Dispose ();
    }

    /// <summary>
    ///     Regression test for https://github.com/gui-cs/Terminal.Gui/issues/4615
    ///     Dialog's size logic should not allow it to be taller or wider than the screen.
    /// </summary>
    [Fact]
    public void Frame_Clamped_To_Container ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        const int SCREEN_WIDTH = 20;
        const int SCREEN_HEIGHT = 20;
        app.Screen = new Rectangle (0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);

        using Dialog dialog = new ();
        dialog.AddButton (new Button ());

        View view = new () { Width = 10, Height = 50 };

        dialog.ShadowStyle = ShadowStyles.Transparent;
        dialog.Add (view);

        app.Begin (dialog);
        Assert.True (app.Screen.Contains (dialog.Frame));
        dialog.Remove (view);

        view.Width = SCREEN_WIDTH;
        view.Height = SCREEN_WIDTH;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));

        dialog.Remove (view);
        view.Width = SCREEN_WIDTH + 5;
        view.Height = SCREEN_WIDTH + 5;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));

        dialog.Remove (view);
        dialog.ShadowStyle = ShadowStyles.Transparent;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));

        dialog.Remove (view);
        view.Height = SCREEN_HEIGHT + 10;
        dialog.Add (view);
        dialog.Layout ();
        Assert.True (app.Screen.Contains (dialog.Frame));
    }

    /// <summary>
    ///     Regression test for https://github.com/gui-cs/Terminal.Gui/issues/4615
    ///     Dialog's size should be constrained by maximumContentDim.
    /// </summary>
    [Theory]
    [InlineData (20, 10)]
    [InlineData (40, 20)]
    [InlineData (80, 25)]
    [InlineData (100, 50)]
    public void Height_Never_Exceeds_Screen_Height (int screenWidth, int screenHeight)
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (screenWidth, screenHeight);

        // Create a container view that represents the screen (simulating TopRunnableView)
        View container = new ()
        {
            Driver = driver,
            X = 0,
            Y = 0,
            Width = screenWidth,
            Height = screenHeight
        };

        Dialog dialog = new ();

        // Add many subviews to force a large content area
        for (var i = 0; i < 100; i++)
        {
            dialog.Add (new Label { Text = $"Label {i}", X = 0, Y = i });
        }

        dialog.AddButton (new Button { Title = "OK" });
        container.Add (dialog);

        // Initialize and layout the container
        container.BeginInit ();
        container.EndInit ();
        container.SetRelativeLayout (new Size (screenWidth, screenHeight));

        // Debug: Check container size before layout
        output.WriteLine ($"Container Frame before layout: {container.Frame}");
        output.WriteLine ($"Container ContentSize: {container.GetContentSize ()}");

        container.LayoutSubViews ();

        // Debug: Check dialog size after layout
        output.WriteLine ($"Dialog Frame after layout: {dialog.Frame}");
        output.WriteLine ($"Dialog SuperView: {dialog.SuperView}");
        output.WriteLine ($"Dialog SuperView ContentSize: {dialog.SuperView?.GetContentSize ()}");

        // The dialog should never exceed the screen height
        Assert.True (dialog.Frame.Height <= screenHeight, $"Dialog height {dialog.Frame.Height} exceeded screen height {screenHeight}");

        // The dialog should never exceed the screen width
        Assert.True (dialog.Frame.Width <= screenWidth, $"Dialog width {dialog.Frame.Width} exceeded screen width {screenWidth}");

        container.Dispose ();
    }

    #endregion Layout Tests

    #region Drawing Tests

    [Fact]
    public void Draws_Single_Button ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────┐
                       │    │
                       │  OK│
                       └────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Two_Buttons ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (40, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.End;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────┐
                       │        │
                       │CancelOK│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Text_Content ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 20);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Text = "Hello World";

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌───────────┐
                       │Hello World│
                       │           │
                       │         OK│
                       └───────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Multiline_Text ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 12);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Text = "Line 1\nLine 2\nLine 3";

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌──────┐
                       │Line 1│
                       │Line 2│
                       │Line 3│
                       │      │
                       │    OK│
                       └──────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Three_Buttons_End_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (50, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.End;

        Button helpButton = new ()
        {
            Title = "Help",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (helpButton);
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────────┐
                       │            │
                       │HelpCancelOK│
                       └────────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Buttons_Center_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (35, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.Center;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────┐
                       │        │
                       │CancelOK│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_Buttons_Start_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (35, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.ButtonAlignment = Alignment.Start;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌────────┐
                       │        │
                       │CancelOK│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Draws_EnableForDesign ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;

        (dialog as IDesignable).EnableForDesign ();

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌┤Dialog Title├───────────────────────────────────┐
                       │Example: Type and press ENTER to accept.         │
                       │                                                 │
                       │                            ⟦ Cancel ⟧  ⟦► OK ◄⟧ │
                       │                                                 │
                       └─────────────────────────────────────────────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Theory]
    [MemberData (nameof (PosData))]
    public void Dialog_Draws_SubView_With_SubViews_WithDifferentPosTypes (Pos pos, string expected)
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (22, 9);

        using Dialog dialog = new ();

        dialog.Driver = driver;
        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Title = "Dialog";

        var container = new View
        {
            X = pos,
            Id = "container",
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Title = "container",
            BorderStyle = LineStyle.Single
        };
        var view1 = new View { Width = Dim.Auto (), Height = Dim.Auto (), Text = "view1" };
        var view2 = new View { Y = 1, Width = Dim.Auto (), Height = Dim.Auto (), Text = "view2" };
        container.Add (view1, view2);

        dialog.Add (container);

        dialog.Layout ();
        dialog.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    public static TheoryData<Pos, string> PosData () =>
        new ()
        {
            {
                Pos.Absolute (0), """
                                  ┌┤Dialog├──┐
                                  │┌┤con├┐   │
                                  ││view1│   │
                                  ││view2│   │
                                  │└─────┘   │
                                  └──────────┘
                                  """
            },
            {
                Pos.Absolute (2), """
                                  ┌┤Dialog├──┐
                                  │  ┌┤con├┐ │
                                  │  │view1│ │
                                  │  │view2│ │
                                  │  └─────┘ │
                                  └──────────┘
                                  """
            },
            {
                Pos.Center (), """
                               ┌┤Dialog├──┐
                               │ ┌┤con├┐  │
                               │ │view1│  │
                               │ │view2│  │
                               │ └─────┘  │
                               └──────────┘
                               """
            },
            {
                Pos.AnchorEnd (), """
                                  ┌┤Dialog├──┐
                                  │   ┌┤con├┐│
                                  │   │view1││
                                  │   │view2││
                                  │   └─────┘│
                                  └──────────┘
                                  """
            },
            {
                Pos.Align (Alignment.Start), """
                                             ┌┤Dialog├──┐
                                             │┌┤con├┐   │
                                             ││view1│   │
                                             ││view2│   │
                                             │└─────┘   │
                                             └──────────┘
                                             """
            },
            {
                Pos.Align (Alignment.Center), """
                                              ┌┤Dialog├──┐
                                              │ ┌┤con├┐  │
                                              │ │view1│  │
                                              │ │view2│  │
                                              │ └─────┘  │
                                              └──────────┘
                                              """
            },
            {
                Pos.Align (Alignment.End), """
                                           ┌┤Dialog├──┐
                                           │   ┌┤con├┐│
                                           │   │view1││
                                           │   │view2││
                                           │   └─────┘│
                                           └──────────┘
                                           """
            },
            {
                Pos.Align (Alignment.Fill), """
                                            ┌┤Dialog├──┐
                                            │┌┤con├┐   │
                                            ││view1│   │
                                            ││view2│   │
                                            │└─────┘   │
                                            └──────────┘
                                            """
            },
            {
                Pos.Percent (50), """
                                  ┌┤Dialog├──┐
                                  │     ┌┤con│
                                  │     │view│
                                  │     │view│
                                  │     └────│
                                  └──────────┘
                                  """
            },
            {
                Pos.Func (_ => 3), """
                                   ┌┤Dialog├──┐
                                   │   ┌┤con├┐│
                                   │   │view1││
                                   │   │view2││
                                   │   └─────┘│
                                   └──────────┘
                                   """
            }
        };

    [Theory]
    [MemberData (nameof (PosViewData))]
    public void Dialog_Draws_SubView_With_SubViews_WithDifferentPosViewTypes (Func<View, Pos> posFactory, Func<View> viewFactory, string expected)
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (23, 11);

        using Dialog dialog = new ();

        dialog.Driver = driver;
        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = null;
        dialog.Title = "Dialog";

        View view = viewFactory (); // Create fresh instance
        Pos pos = posFactory (view);

        var container = new View
        {
            X = pos,
            Y = pos,
            Id = "container",
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Title = "container",
            BorderStyle = LineStyle.Single
        };
        var view1 = new View { Width = Dim.Auto (), Height = Dim.Auto (), Text = "v" };
        container.Add (view1);

        dialog.Add (container, view);

        dialog.Layout ();
        dialog.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    public static TheoryData<Func<View, Pos>, Func<View>, string> PosViewData () =>
        new ()
        {
            {
                Pos.Bottom, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │   ┌─┐    │
                │   │v│    │
                │   └─┘    │
                └──────────┘
                """
            },
            {
                Pos.Left, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘
                """
            },
            {
                Pos.Right, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │          │
                │          │
                │          │
                │      ┌─┐ │
                │      │v│ │
                │      └─┘ │
                └──────────┘
                """
            },
            {
                Pos.Top, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘
                
                """
            },
            {
                Pos.X, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘
                """
            },
            {
                Pos.Y, () => new View
                {
                    X = 2,
                    Y = 2,
                    Width = Dim.Auto (),
                    Height = Dim.Auto (),
                    Text = "view"
                },
                """
                ┌┤Dialog├──┐
                │          │
                │          │
                │  view    │
                │  │v│     │
                │  └─┘     │
                └──────────┘
                """
            }
        };

    #endregion Drawing Tests

    #region Dialog<TResult> Generic Tests

    // Claude - Opus 4.5

    /// <summary>
    ///     Test dialog that returns a <see cref="Color"/> result.
    /// </summary>
    private class TestColorDialog : Dialog<Color>
    {
        public TestColorDialog ()
        {
            Title = "Select Color";
            AddButton (new Button { Title = "Cancel" });
            AddButton (new Button { Title = "OK" });
        }

        public Color SelectedColor { get; init; } = Color.Blue;

        protected override bool OnAccepting (CommandEventArgs args)
        {
            if (base.OnAccepting (args))
            {
                return true;
            }
            Result = SelectedColor;

            return false;
        }
    }

    /// <summary>
    ///     Test dialog that returns a <see cref="string"/> result.
    /// </summary>
    private class TestStringDialog : Dialog<string>
    {
        public TestStringDialog ()
        {
            Title = "Enter Text";
            AddButton (new Button { Title = "Cancel" });
            AddButton (new Button { Title = "OK" });
        }

        public string InputText { get; set; } = string.Empty;

        /// <inheritdoc/>
        protected override bool OnAccepting (CommandEventArgs args)
        {
            if (base.OnAccepting (args))
            {
                return true;
            }
            Result = InputText;

            return false;
        }
    }

    /// <summary>
    ///     Test dialog that returns a <see cref="DateTime"/> result.
    /// </summary>
    private class TestDateDialog : Dialog<DateTime?>
    {
        private readonly DatePicker _datePicker = new () { Value = new DateTime (1966, 9, 10) };

        public TestDateDialog ()
        {
            Title = "Select Date";
            AddButton (new Button { Title = "Cancel" });
            AddButton (new Button { Title = "OK" });

            Add (_datePicker);
        }

        protected override void OnAccepted (ICommandContext? ctx)
        {
            base.OnAccepted (ctx);
            Result = SelectedDate;
        }

        public DateTime SelectedDate
        {
            get => _datePicker.Value;
            set => _datePicker.Value = value;
        }
    }

    [Fact]
    public void Generic_DialogButton_Accept_BubblesUp ()
    {
        TestDateDialog dialog = new () { Title = "Test" };

        DateTime selectedDate = new (1966, 9, 10);

        var dialogAcceptedFired = false;

        dialog.Accepted += (_, _) => { dialogAcceptedFired = true; };

        dialog.Buttons [1].InvokeCommand (Command.Accept);

        Assert.True (dialogAcceptedFired);

        Assert.Equal (selectedDate, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Generic_Modal_DialogButton_Accept_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (0, okAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            okButton.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Generic_Modal_Dialog_Command_Accept_BubblesUp_TestStringDialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestStringDialog dialog = new ();
        dialog.Title = "Test";
        string newString = "new";

        dialog.InputText = newString;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (1, okAcceptedFired);
        Assert.Equal (newString, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            dialog.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Generic_Modal_Dialog_Command_Accept_BubblesUp_TestDateDialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (1, okAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            dialog.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;


            Assert.True (dialog.StopRequested);
        }
    }


    [Fact]
    public void Generic_Modal_Dialog_DatePicker_Accept_BubblesUp_TestDateDialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        DatePicker datePicker = dialog.SubViews.OfType<DatePicker> ().FirstOrDefault () ?? throw new InvalidOperationException ("DatePicker not found in dialog.");
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        Assert.Equal (1, okAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            datePicker.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            
            Assert.True (dialog.StopRequested);
        }
    }

    [Fact]
    public void Generic_Modal_DialogButton_Cancel_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptingFired = 0;
        dialog.Accepting += (_, e) => { dialogAcceptingFired++; };

        int dialogAcceptedFired = 0;
        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (0, dialogAcceptingFired);
        Assert.Equal (0, dialogAcceptedFired);
        Assert.Equal (0, okAcceptingFired);
        Assert.Equal (0, okAcceptedFired);
        Assert.Null (dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            cancelButton.InvokeCommand (Command.Accept);

            // Just in case
            app.Iteration -= AppOnIteration;
            Assert.True (dialog.StopRequested);
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Generic_Modal_Dialog_EnterKey_Accepts_Dialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestDateDialog dialog = new ();
        dialog.Title = "Test";
        DateTime newDateTime = dialog.SelectedDate.AddYears (1);

        dialog.SelectedDate = newDateTime;

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, _) => { dialogAcceptedFired++; };

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (newDateTime, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            app.Iteration -= AppOnIteration;

            // Simulate pressing Enter key via Application key processing
            app.Keyboard.RaiseKeyDownEvent (Key.Enter);

            if (!dialog.StopRequested)
            {
                // Enter didn't work - get debug info and force stop
                View? focused = dialog.Focused;
                View? deepFocused = dialog.MostFocused;

                dialog.RequestStop ();

                Assert.Fail ($"Enter key did not accept dialog. Focused={focused?.GetType ().Name ?? "null"} ({focused?.Id}), MostFocused={deepFocused?.GetType ().Name ?? "null"} ({deepFocused?.Id})");
            }
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void NonGeneric_Modal_Dialog_EnterKey_Accepts_Dialog ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Dialog dialog = new ();
        dialog.Title = "Test";

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        Label label = new () { Text = "Press Enter" };
        dialog.Add (label);

        app.Iteration += AppOnIteration;
        app.Run (dialog);
        app.Iteration -= AppOnIteration;

        // Enter on the focused button (Cancel, the first button) should stop the dialog
        Assert.True (dialog.StopRequested);

        // Cancel button (index 0) is focused by default, so pressing Enter accepts with Result=0
        Assert.Equal (0, dialog.Result);

        return;

        void AppOnIteration (object? sender, EventArgs<IApplication?> e)
        {
            app.Iteration -= AppOnIteration;

            // Simulate pressing Enter key via Application key processing
            app.Keyboard.RaiseKeyDownEvent (Key.Enter);

            if (!dialog.StopRequested)
            {
                dialog.RequestStop ();

                Assert.Fail ("Enter key did not accept dialog.");
            }
        }
    }

    [Fact]
    public void GenericConstructor_Initializes_DefaultValues ()
    {
        TestColorDialog dialog = new ();

        Assert.NotNull (dialog);
        Assert.True (dialog.CanFocus);
        Assert.Equal ("Select Color", dialog.Title);
        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Dispose ();
    }

    [Fact]
    public void GenericResult_Is_Null_Initially ()
    {
        TestColorDialog colorDialog = new ();
        TestStringDialog stringDialog = new ();
        TestDateDialog dateDialog = new ();

        // Check via IRunnable for nullable object?
        Assert.Null (((IRunnable)colorDialog).Result);
        Assert.Null (((IRunnable)stringDialog).Result);
        Assert.Null (((IRunnable)dateDialog).Result);

        colorDialog.Dispose ();
        stringDialog.Dispose ();
        dateDialog.Dispose ();
    }

    [Fact]
    public void GenericColor_Result_Can_Be_Set ()
    {
        TestColorDialog dialog = new ();

        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Result = Color.Red;
        Assert.Equal (Color.Red, dialog.Result);

        dialog.Result = Color.Green;
        Assert.Equal (Color.Green, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericString_Input_Can_Be_Set ()
    {
        TestStringDialog dialog = new () { InputText = "Initial Text" };

        Assert.Null (dialog.Result);

        dialog.InvokeCommand (Command.Accept);

        Assert.Equal (dialog.InputText, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericString_Result_Can_Be_Set ()
    {
        TestStringDialog dialog = new ();

        Assert.Null (dialog.Result);

        dialog.Result = "Hello";
        Assert.Equal ("Hello", dialog.Result);

        dialog.Result = "World";
        Assert.Equal ("World", dialog.Result);

        dialog.Result = null;
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }


    [Fact]
    public void GenericString_Command_Accept_BubblesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using TestStringDialog dialog = new ();
        dialog.Title = "Test";
        string newString = "new";

        dialog.InputText = newString;

        Button cancelButton = new () { Text = "Cancel" };
        dialog.AddButton (cancelButton);
        Button okButton = new () { Text = "OK" };
        dialog.AddButton (okButton);

        int dialogAcceptedFired = 0;

        dialog.Accepted += (_, e) => { dialogAcceptedFired++; };

        int cancelAcceptingFired = 0;
        cancelButton.Accepting += (_, e) => { cancelAcceptingFired++; };

        int cancelAcceptedFired = 0;
        cancelButton.Accepted += (_, e) => { cancelAcceptedFired++; };

        int okAcceptingFired = 0;
        okButton.Accepting += (_, e) => { okAcceptingFired++; };

        int okAcceptedFired = 0;
        okButton.Accepted += (_, e) => { okAcceptedFired++; };

        dialog.InvokeCommand (Command.Accept);

        Assert.Equal (newString, dialog.Result);
        Assert.Equal (1, dialogAcceptedFired);
        Assert.Equal (0, cancelAcceptingFired);
        Assert.Equal (0, cancelAcceptedFired);
        Assert.Equal (1, okAcceptingFired);
        //Assert.Equal (0, okAcceptedFired);
    }

    [Fact]
    public void GenericDateTime_Result_Can_Be_Set ()
    {
        TestDateDialog dialog = new ();
        DateTime testDate = new (2024, 6, 15);

        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Result = testDate;
        Assert.Equal (testDate, dialog.Result);

        dialog.Dispose ();
    }



    [Fact]
    public void Generic_Ok_Command_Accept_Sets_Result ()
    {
        TestColorDialog dialog = new () { SelectedColor = Color.Magenta };

        dialog.Buttons [1].InvokeCommand (Command.Accept);

        Assert.Equal (Color.Magenta, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Generic_Cancel_Command_Accept_Does_Not_Set_Result ()
    {
        TestColorDialog dialog = new () { SelectedColor = Color.Cyan };

        // Simulate pressing Cancel button (index 0) - Result stays null
        dialog.Buttons [0].InvokeCommand (Command.Accept);

        Assert.Null (((IRunnable)dialog).Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericIRunnable_Result_Returns_Object ()
    {
        TestColorDialog dialog = new ();

        IRunnable runnable = dialog;
        Assert.Null (runnable.Result);

        dialog.Result = Color.Yellow;

        // IRunnable.Result returns the boxed value
        Assert.NotNull (runnable.Result);
        Assert.IsType<Color> (runnable.Result);
        Assert.Equal (Color.Yellow, (Color)runnable.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericBaseClass_Result_Casts_Correctly ()
    {
        TestStringDialog dialog = new ();

        // Set via typed property
        dialog.Result = "Test Value";
        Assert.Equal ("Test Value", dialog.Result);

        // Access via IRunnable
        IRunnable runnable = dialog;
        Assert.Equal ("Test Value", runnable.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericInherits_Properties ()
    {
        TestColorDialog dialog = new ();

        // Should inherit all Dialog properties
        Assert.Equal (Alignment.End, dialog.ButtonAlignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);
        Assert.Equal (LineStyle.Heavy, dialog.BorderStyle);
        Assert.Equal (ShadowStyles.Transparent, dialog.ShadowStyle);
        Assert.Equal (ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericButtons_Can_Be_Added ()
    {
        Dialog<Color> dialog = new ();

        Assert.Empty (dialog.Buttons);

        Button button1 = new () { Title = "First" };
        Button button2 = new () { Title = "Second" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);

        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal ("First", dialog.Buttons [0].Title);
        Assert.Equal ("Second", dialog.Buttons [1].Title);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericCan_Add_SubViews ()
    {
        TestColorDialog dialog = new ();

        Label label = new () { Text = "Choose a color:" };
        dialog.Add (label);

        Assert.Contains (label, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericTitle_Can_Be_Set ()
    {
        Dialog<string> dialog = new () { Title = "Custom Title" };

        Assert.Equal ("Custom Title", dialog.Title);

        dialog.Title = "Changed Title";
        Assert.Equal ("Changed Title", dialog.Title);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericMultiple_Result_Types ()
    {
        // Test various result types work correctly
        Dialog<int> intDialog = new ();
        Dialog<bool> boolDialog = new ();
        Dialog<double> doubleDialog = new ();
        Dialog<Guid> guidDialog = new ();

        intDialog.Result = 42;
        Assert.Equal (42, intDialog.Result);

        boolDialog.Result = true;
        Assert.True (boolDialog.Result);

        doubleDialog.Result = 3.14159;
        Assert.Equal (3.14159, doubleDialog.Result);

        var testGuid = Guid.NewGuid ();
        guidDialog.Result = testGuid;
        Assert.Equal (testGuid, guidDialog.Result);

        intDialog.Dispose ();
        boolDialog.Dispose ();
        doubleDialog.Dispose ();
        guidDialog.Dispose ();
    }

    [Fact]
    public void NonGenericIs_DialogOfInt ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        // Verify Dialog inherits from Dialog<int>
        Assert.IsAssignableFrom<Dialog<int>> (dialog);

        // Result should work as int
        dialog.Result = 1;
        Assert.Equal (1, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void NonGenericCanceled_Works_As_Expected ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        // Initially null result means canceled
        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled);

        // Result 0 means Cancel button pressed
        dialog.Result = 0;
        Assert.True (dialog.Canceled);

        // Result 1 means OK button pressed
        dialog.Result = 1;
        Assert.False (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void NonGenericResult_Throws_With_Invalid_Value ()
    {
        Dialog dialog = new ();
        dialog.AddButton (new Button { Title = "Cancel" });
        dialog.AddButton (new Button { Title = "OK" });

        Assert.Throws<ArgumentOutOfRangeException> (() => dialog.Result = -1);
        Assert.Throws<ArgumentOutOfRangeException> (() => dialog.Result = 2);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericLayout_Works ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        TestColorDialog dialog = new () { Driver = driver };

        dialog.Layout ();

        // Should calculate size correctly
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDraws_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        Dialog<Color> dialog = new ()
        {
            X = 0,
            Y = 0,
            BorderStyle = LineStyle.Single,
            ShadowStyle = null,
            Driver = driver
        };

        Button cancelButton = new ()
        {
            Title = "No",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };

        Button okButton = new ()
        {
            Title = "Yes",
            BorderStyle = LineStyle.None,
            ShadowStyle = null,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Layout ();
        dialog.Draw ();

        var expected = """
                       ┌─────┐
                       │     │
                       │NoYes│
                       └─────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        dialog.Dispose ();
    }

    #endregion Dialog<TResult> Generic Tests
}
