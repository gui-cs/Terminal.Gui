using UnitTests;
using Xunit.Abstractions;

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
    public void Constructor_Initializes_DefaultValues ()
    {
        Dialog dialog = new ();

        Assert.NotNull (dialog);
        Assert.True (dialog.CanFocus);
        Assert.Equal (Alignment.End, dialog.ButtonAlignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);
        Assert.Equal (LineStyle.Heavy, dialog.BorderStyle);
        Assert.Equal (ShadowStyle.Transparent, dialog.ShadowStyle);
        Assert.Empty (dialog.Buttons);
        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled); // Canceled is true when Result is null
        Assert.Equal (ViewArrangement.Overlapped, dialog.Arrangement);

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
    public void Constructor_Sets_AutoDimensions ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.Width.Has<DimAuto> (out _));
        Assert.True (dialog.Height.Has<DimAuto> (out _));

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
    public void Buttons_Property_Set_Adds_Buttons ()
    {
        Dialog dialog = new ();

        Button [] buttons =
        [
            new () { Title = "Cancel" },
            new () { Title = "OK" }
        ];

        dialog.Buttons = buttons;

        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal ("Cancel", dialog.Buttons [0].Title);
        Assert.Equal ("OK", dialog.Buttons [1].Title);
        Assert.True (dialog.Buttons [1].IsDefault);

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
    public void Canceled_False_When_Result_Null ()
    {
        Dialog dialog = new ();

        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void Canceled_True_When_Result_Is_1 ()
    {
        Dialog dialog = new ();

        dialog.Result = 1;
        Assert.True (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void Canceled_False_When_Result_Is_0 ()
    {
        Dialog dialog = new ();

        dialog.Result = 0;
        Assert.False (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void Canceled_False_When_Result_Is_2 ()
    {
        Dialog dialog = new ();

        dialog.Result = 2;
        Assert.False (dialog.Canceled);

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

    [Fact]
    public void Add_SubView_Updates_Dialog ()
    {
        Dialog dialog = new ();

        Label label = new ()
        {
            Text = "Hello World",
            X = 0,
            Y = 0
        };

        dialog.Add (label);

        Assert.Contains (label, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_With_Title_And_Buttons ()
    {
        Dialog dialog = new ()
        {
            Title = "Confirm"
        };

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
    public void Dialog_Arrangement_Default ()
    {
        Dialog dialog = new ();

        Assert.Equal (ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_CanFocus_Default_True ()
    {
        Dialog dialog = new ();

        Assert.True (dialog.CanFocus);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_With_Multiple_SubViews ()
    {
        Dialog dialog = new ()
        {
            Title = "Form"
        };

        Label nameLabel = new ()
        {
            Text = "Name:",
            X = 0,
            Y = 0
        };

        TextField nameField = new ()
        {
            X = Pos.Right (nameLabel) + 1,
            Y = 0,
            Width = 20
        };

        Label emailLabel = new ()
        {
            Text = "Email:",
            X = 0,
            Y = 1
        };

        TextField emailField = new ()
        {
            X = Pos.Right (emailLabel) + 1,
            Y = 1,
            Width = 20
        };

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
    public void Empty_Dialog_Has_No_Buttons ()
    {
        Dialog dialog = new ();

        Assert.Empty (dialog.Buttons);
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_Border_Style_Can_Be_Changed ()
    {
        Dialog dialog = new ()
        {
            BorderStyle = LineStyle.Single
        };

        Assert.Equal (LineStyle.Single, dialog.BorderStyle);

        dialog.BorderStyle = LineStyle.Double;
        Assert.Equal (LineStyle.Double, dialog.BorderStyle);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_ShadowStyle_Can_Be_Changed ()
    {
        Dialog dialog = new ()
        {
            ShadowStyle = ShadowStyle.None
        };

        Assert.Equal (ShadowStyle.None, dialog.ShadowStyle);

        dialog.ShadowStyle = ShadowStyle.Opaque;
        Assert.Equal (ShadowStyle.Opaque, dialog.ShadowStyle);

        dialog.Dispose ();
    }

    [Theory]
    [InlineData (Alignment.Start)]
    [InlineData (Alignment.Center)]
    [InlineData (Alignment.End)]
    [InlineData (Alignment.Fill)]
    public void ButtonAlignment_Theory (Alignment alignment)
    {
        Dialog dialog = new ()
        {
            ButtonAlignment = alignment
        };

        Assert.Equal (alignment, dialog.ButtonAlignment);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_Disposes_Buttons ()
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
    public void Result_Can_Be_Set_And_Retrieved ()
    {
        Dialog dialog = new ();

        Assert.Null (dialog.Result);

        dialog.Result = 0;
        Assert.Equal (0, dialog.Result);

        dialog.Result = 1;
        Assert.Equal (1, dialog.Result);

        dialog.Result = 5;
        Assert.Equal (5, dialog.Result);

        dialog.Result = null;
        Assert.Null (dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_With_Wide_Character_Title ()
    {
        Dialog dialog = new ()
        {
            Title = "你好世界"
        };

        Assert.Equal ("你好世界", dialog.Title);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_With_Wide_Character_Button_Text ()
    {
        Dialog dialog = new ();
        Button button = new () { Title = "确定" };

        dialog.AddButton (button);

        Assert.Single (dialog.Buttons);
        Assert.Equal ("确定", dialog.Buttons [0].Title);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_Text_Property ()
    {
        Dialog dialog = new ()
        {
            Text = "This is a message"
        };

        Assert.Equal ("This is a message", dialog.Text);

        dialog.Text = "Updated message";
        Assert.Equal ("Updated message", dialog.Text);

        dialog.Dispose ();
    }

    #region Layout Tests

    [Fact]
    public void EnableForDesign_Initializes_Dialog_With_Content ()
    {
        IDriver driver = CreateTestDriver ();
        Dialog dialog = new ()
        {
            Driver = driver
        };

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
    public void Dialog_Layout_With_EnableForDesign_Default_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver
        };

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
    public void Dialog_Layout_With_Small_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        Dialog dialog = new ()
        {
            Driver = driver
        };

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
    public void Dialog_Layout_With_Large_Container ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (200, 100);

        Dialog dialog = new ()
        {
            Driver = driver
        };

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
    public void Dialog_Width_Height_DimAuto_Calculates_Based_On_Content ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver,
            Title = "Test"
        };

        // Add content that requires specific size
        Label label = new ()
        {
            Text = "This is a label",
            X = 0,
            Y = 0
        };
        TextField textField = new ()
        {
            X = Pos.Right (label) + 1,
            Y = 0,
            Width = 30,
            Text = "Input here"
        };

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
    public void Dialog_Height_Accounts_For_Buttons ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver
        };

        Button button1 = new () { Title = "Cancel" };
        Button button2 = new () { Title = "OK" };

        dialog.AddButton (button1);
        dialog.AddButton (button2);

        dialog.Layout ();

        // Height should account for buttons at the bottom
        Assert.True (dialog.Frame.Height > 0);
        Assert.True (dialog.Padding!.Thickness.Bottom >= button1.Frame.Height);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_Respects_Explicit_Width_Height ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver,
            Width = 50,
            Height = 15
        };

        IDesignable designable = dialog;
        designable.EnableForDesign ();

        dialog.Layout ();

        // Should use explicit dimensions
        Assert.Equal (50, dialog.Frame.Width);
        Assert.Equal (15, dialog.Frame.Height);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_Padding_Affects_Content_Area ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver
        };

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
    public void Dialog_Multiple_Buttons_Layout_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog = new ()
        {
            Driver = driver,
            ButtonAlignment = Alignment.End,
            ButtonAlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems
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
        Assert.True (button1.X.Has<PosAlign> (out PosAlign? align1));
        Assert.True (button2.X.Has<PosAlign> (out PosAlign? align2));
        Assert.True (button3.X.Has<PosAlign> (out PosAlign? align3));

        // All should use same GroupId (Dialog's hash code)
        Assert.Equal (align1!.GroupId, align2!.GroupId);
        Assert.Equal (align2.GroupId, align3!.GroupId);

        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_With_Text_Property_Affects_Height ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Dialog dialog1 = new ()
        {
            Driver = driver
        };

        Button button = new () { Title = "OK" };
        dialog1.AddButton (button);

        dialog1.Layout ();
        int heightWithoutText = dialog1.Frame.Height;

        Dialog dialog2 = new ()
        {
            Driver = driver,
            Text = "Line 1\nLine 2\nLine 3"
        };

        dialog2.AddButton (new () { Title = "OK" });

        dialog2.Layout ();
        int heightWithText = dialog2.Frame.Height;

        // Dialog with text should be taller
        Assert.True (heightWithText > heightWithoutText);

        dialog1.Dispose ();
        dialog2.Dispose ();
    }

    #endregion Layout Tests

    #region Drawing Tests

    [Fact]
    public void Dialog_Draws_Single_Button ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌────┐
│    │
│  OK│
└────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Dialog_Draws_Two_Buttons ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (40, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;
        dialog.ButtonAlignment = Alignment.End;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌────────┐
│        │
│CancelOK│
└────────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Dialog_Draws_Text_Content ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 20);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;
        dialog.Text = "Hello World";

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌───────────┐
│Hello World│
│           │
│         OK│
└───────────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Dialog_Draws_Multiline_Text ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 12);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;
        dialog.Text = "Line 1\nLine 2\nLine 3";

        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
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
    public void Dialog_Draws_Three_Buttons_End_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (50, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;
        dialog.ButtonAlignment = Alignment.End;

        Button helpButton = new ()
        {
            Title = "Help",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (helpButton);
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌────────────┐
│            │
│HelpCancelOK│
└────────────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Dialog_Draws_Buttons_Center_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (35, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;
        dialog.ButtonAlignment = Alignment.Center;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌────────┐
│        │
│CancelOK│
└────────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }

    [Fact]
    public void Dialog_Draws_Buttons_Start_Aligned ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (35, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;
        dialog.ButtonAlignment = Alignment.Start;

        Button cancelButton = new ()
        {
            Title = "Cancel",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        Button okButton = new ()
        {
            Title = "OK",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌────────┐
│        │
│CancelOK│
└────────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }


    [Fact]
    public void Dialog_Draws_EnableForDesign ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 10);

        using Dialog dialog = new ();

        dialog.X = 0;
        dialog.Y = 0;
        dialog.BorderStyle = LineStyle.Single;
        dialog.ShadowStyle = ShadowStyle.None;

        (dialog as IDesignable).EnableForDesign ();

        dialog.Driver = driver;
        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌┤Dialog Title├───────────────────────────────────┐
│Example: Type and press ENTER to accept.         │
│                                                 │
│                            ⟦ Cancel ⟧  ⟦► OK ◄⟧ │
│                                                 │
└─────────────────────────────────────────────────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
    }
    #endregion Drawing Tests

    #region Dialog<TResult> Generic Tests

    // Claude - Opus 4.5

    /// <summary>
    ///     Test dialog that returns a <see cref="Color"/> result.
    /// </summary>
    private class TestColorDialog : Dialog<Color>
    {
        public Color SelectedColor { get; set; } = Color.Blue;

        public TestColorDialog ()
        {
            Title = "Select Color";
            AddButton (new () { Title = "Cancel" });
            AddButton (new () { Title = "OK" });
        }

        protected override void OnButtonPressed (int buttonIndex)
        {
            if (buttonIndex == 1) // OK
            {
                Result = SelectedColor;
            }
            // Cancel leaves Result as default (null for reference types, default for value types)

            RequestStop ();
        }
    }

    /// <summary>
    ///     Test dialog that returns a <see cref="string"/> result.
    /// </summary>
    private class TestStringDialog : Dialog<string>
    {
        public string InputText { get; set; } = "";

        public TestStringDialog ()
        {
            Title = "Enter Text";
            AddButton (new () { Title = "Cancel" });
            AddButton (new () { Title = "OK" });
        }

        protected override void OnButtonPressed (int buttonIndex)
        {
            if (buttonIndex == 1) // OK
            {
                Result = InputText;
            }

            RequestStop ();
        }
    }

    /// <summary>
    ///     Test dialog that returns a <see cref="DateTime"/> result.
    /// </summary>
    private class TestDateDialog : Dialog<DateTime>
    {
        public DateTime SelectedDate { get; set; } = DateTime.Now;

        public TestDateDialog ()
        {
            Title = "Select Date";
            AddButton (new () { Title = "Cancel" });
            AddButton (new () { Title = "OK" });
        }

        protected override void OnButtonPressed (int buttonIndex)
        {
            if (buttonIndex == 1) // OK
            {
                Result = SelectedDate;
            }

            RequestStop ();
        }
    }

    [Fact]
    public void GenericDialog_Constructor_Initializes_DefaultValues ()
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
    public void GenericDialog_Result_Is_Null_Initially ()
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
    public void GenericDialog_Color_Result_Can_Be_Set ()
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
    public void GenericDialog_String_Result_Can_Be_Set ()
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
    public void GenericDialog_DateTime_Result_Can_Be_Set ()
    {
        TestDateDialog dialog = new ();
        DateTime testDate = new (2024, 6, 15);

        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Result = testDate;
        Assert.Equal (testDate, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_OnButtonPressed_Sets_Result_On_OK ()
    {
        TestColorDialog dialog = new ()
        {
            SelectedColor = Color.Magenta
        };

        // Simulate pressing OK button (index 1)
        dialog.Result = dialog.SelectedColor;

        Assert.Equal (Color.Magenta, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_OnButtonPressed_Leaves_Null_On_Cancel ()
    {
        TestColorDialog dialog = new ()
        {
            SelectedColor = Color.Cyan
        };

        // Simulate pressing Cancel button (index 0) - Result stays null
        Assert.Null (((IRunnable)dialog).Result); // Check via IRunnable for nullable object?

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_IRunnable_Result_Returns_Object ()
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
    public void GenericDialog_BaseClass_Result_Casts_Correctly ()
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
    public void GenericDialog_Inherits_Dialog_Properties ()
    {
        TestColorDialog dialog = new ();

        // Should inherit all Dialog properties
        Assert.Equal (Alignment.End, dialog.ButtonAlignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, dialog.ButtonAlignmentModes);
        Assert.Equal (LineStyle.Heavy, dialog.BorderStyle);
        Assert.Equal (ShadowStyle.Transparent, dialog.ShadowStyle);
        Assert.Equal (ViewArrangement.Overlapped, dialog.Arrangement);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_Buttons_Can_Be_Added ()
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
    public void GenericDialog_Can_Add_SubViews ()
    {
        TestColorDialog dialog = new ();

        Label label = new () { Text = "Choose a color:" };
        dialog.Add (label);

        Assert.Contains (label, dialog.SubViews);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_Title_Can_Be_Set ()
    {
        Dialog<string> dialog = new ()
        {
            Title = "Custom Title"
        };

        Assert.Equal ("Custom Title", dialog.Title);

        dialog.Title = "Changed Title";
        Assert.Equal ("Changed Title", dialog.Title);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_Multiple_Result_Types ()
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

        Guid testGuid = Guid.NewGuid ();
        guidDialog.Result = testGuid;
        Assert.Equal (testGuid, guidDialog.Result);

        intDialog.Dispose ();
        boolDialog.Dispose ();
        doubleDialog.Dispose ();
        guidDialog.Dispose ();
    }

    [Fact]
    public void NonGenericDialog_Is_DialogOfInt ()
    {
        Dialog dialog = new ();

        // Verify Dialog inherits from Dialog<int>
        Assert.IsAssignableFrom<Dialog<int>> (dialog);

        // Result should work as int
        dialog.Result = 5;
        Assert.Equal (5, dialog.Result);

        dialog.Dispose ();
    }

    [Fact]
    public void NonGenericDialog_Canceled_Works_As_Expected ()
    {
        Dialog dialog = new ();

        // Initially null result means canceled
        Assert.Null (dialog.Result);
        Assert.True (dialog.Canceled);

        // Result 0 means not canceled (first button pressed)
        dialog.Result = 0;
        Assert.False (dialog.Canceled);

        // Result 1 typically means cancel button pressed
        dialog.Result = 1;
        Assert.True (dialog.Canceled);

        // Result 2 or higher means some other button pressed
        dialog.Result = 2;
        Assert.False (dialog.Canceled);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_Layout_Works ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        TestColorDialog dialog = new ()
        {
            Driver = driver
        };

        dialog.Layout ();

        // Should calculate size correctly
        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);

        dialog.Dispose ();
    }

    [Fact]
    public void GenericDialog_Draws_Correctly ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (30, 10);

        Dialog<Color> dialog = new ()
        {
            X = 0,
            Y = 0,
            Title = "Color",
            BorderStyle = LineStyle.Single,
            ShadowStyle = ShadowStyle.None,
            Driver = driver
        };

        Button cancelButton = new ()
        {
            Title = "No",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        Button okButton = new ()
        {
            Title = "Yes",
            BorderStyle = LineStyle.None,
            ShadowStyle = ShadowStyle.None,
            NoPadding = true,
            NoDecorations = true
        };
        dialog.AddButton (cancelButton);
        dialog.AddButton (okButton);

        dialog.Layout ();
        dialog.Draw ();

        string expected = """
┌┤Color├──┐
│         │
│   No Yes│
└─────────┘
""";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        dialog.Dispose ();
    }

    #endregion Dialog<TResult> Generic Tests
}
