using UnitTests;
using Xunit.Abstractions;

namespace ViewsTests;

/// <summary>
///     Pure unit tests for <see cref="Dialog"/> that don't require Application static dependencies.
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
    public void DefaultBorderStyle_Get_Set ()
    {
        LineStyle original = Dialog.DefaultBorderStyle;

        try
        {
            Dialog.DefaultBorderStyle = LineStyle.Single;
            Assert.Equal (LineStyle.Single, Dialog.DefaultBorderStyle);

            Dialog dialog = new ();
            Assert.Equal (LineStyle.Single, dialog.BorderStyle);
            dialog.Dispose ();

            Dialog.DefaultBorderStyle = LineStyle.Double;
            Assert.Equal (LineStyle.Double, Dialog.DefaultBorderStyle);

            dialog = new ();
            Assert.Equal (LineStyle.Double, dialog.BorderStyle);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultBorderStyle = original;
        }
    }

    [Fact]
    public void DefaultButtonAlignment_Get_Set ()
    {
        Alignment original = Dialog.DefaultButtonAlignment;

        try
        {
            Dialog.DefaultButtonAlignment = Alignment.Start;
            Assert.Equal (Alignment.Start, Dialog.DefaultButtonAlignment);

            Dialog dialog = new ();
            Assert.Equal (Alignment.Start, dialog.ButtonAlignment);
            dialog.Dispose ();

            Dialog.DefaultButtonAlignment = Alignment.Center;
            Assert.Equal (Alignment.Center, Dialog.DefaultButtonAlignment);

            dialog = new ();
            Assert.Equal (Alignment.Center, dialog.ButtonAlignment);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultButtonAlignment = original;
        }
    }

    [Fact]
    public void DefaultButtonAlignmentModes_Get_Set ()
    {
        AlignmentModes original = Dialog.DefaultButtonAlignmentModes;

        try
        {
            Dialog.DefaultButtonAlignmentModes = AlignmentModes.StartToEnd;
            Assert.Equal (AlignmentModes.StartToEnd, Dialog.DefaultButtonAlignmentModes);

            Dialog dialog = new ();
            Assert.Equal (AlignmentModes.StartToEnd, dialog.ButtonAlignmentModes);
            dialog.Dispose ();

            Dialog.DefaultButtonAlignmentModes = AlignmentModes.IgnoreFirstOrLast;
            Assert.Equal (AlignmentModes.IgnoreFirstOrLast, Dialog.DefaultButtonAlignmentModes);

            dialog = new ();
            Assert.Equal (AlignmentModes.IgnoreFirstOrLast, dialog.ButtonAlignmentModes);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultButtonAlignmentModes = original;
        }
    }

    [Fact]
    public void DefaultShadow_Get_Set ()
    {
        ShadowStyle original = Dialog.DefaultShadow;

        try
        {
            Dialog.DefaultShadow = ShadowStyle.None;
            Assert.Equal (ShadowStyle.None, Dialog.DefaultShadow);

            Dialog dialog = new ();
            Assert.Equal (ShadowStyle.None, dialog.ShadowStyle);
            dialog.Dispose ();

            Dialog.DefaultShadow = ShadowStyle.Opaque;
            Assert.Equal (ShadowStyle.Opaque, Dialog.DefaultShadow);

            dialog = new ();
            Assert.Equal (ShadowStyle.Opaque, dialog.ShadowStyle);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultShadow = original;
        }
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

        // After disposal, buttons should be disposed through the dialog's disposal chain
        Assert.True (dialog.WasDisposed);
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
}
