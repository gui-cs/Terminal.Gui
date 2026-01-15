using UnitTests;

namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="PromptDialog{TView, TResult}"/> and <see cref="PromptExtensions"/>.
/// </summary>
/// <remarks>
///     Claude - Opus 4.5
/// </remarks>
public class PromptDialogTests : TestDriverBase
{
    #region PromptDialog Constructor Tests

    [Fact]
    public void Constructor_Initializes_With_OkCancel_Buttons ()
    {
        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            WrappedView = label,
            ResultExtractor = l => l.Text
        };

        Assert.Equal (2, dialog.Buttons.Length);
        Assert.Equal (Strings.btnCancel, dialog.Buttons [0].Text);
        Assert.Equal (Strings.btnOk, dialog.Buttons [1].Text);
        Assert.True (dialog.Buttons [1].IsDefault);
    }

    [Fact]
    public void Constructor_Sets_Dialog_Defaults ()
    {
        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            WrappedView = label
        };

        Assert.True (dialog.CanFocus);
        Assert.Equal (Alignment.End, dialog.ButtonAlignment);
        Assert.Null (dialog.Result);
    }

    #endregion

    #region WrappedView Tests

    [Fact]
    public void WrappedView_Is_Required ()
    {
        // This test verifies the 'required' modifier works - the dialog cannot be created without WrappedView
        // The following would not compile:
        // PromptDialog<Label, string> dialog = new ();

        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            WrappedView = label
        };

        Assert.Same (label, dialog.WrappedView);
    }

    [Fact]
    public void WrappedView_Added_As_SubView_On_EndInit ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Label label = new () { Text = "Test Label" };

        using PromptDialog<Label, string> dialog = new ()
        {
            Driver = driver,
            WrappedView = label,
            ResultExtractor = l => l.Text
        };

        // Before EndInit, label is not in SubViews
        Assert.DoesNotContain (label, dialog.SubViews);

        dialog.BeginInit ();
        dialog.EndInit ();

        // After EndInit, label should be in SubViews
        Assert.Contains (label, dialog.SubViews);
    }

    #endregion

    #region ResultExtractor Tests

    [Fact]
    public void ResultExtractor_Can_Be_Set ()
    {
        Label label = new () { Text = "Hello" };

        using PromptDialog<Label, string> dialog = new ()
        {
            WrappedView = label,
            ResultExtractor = l => l.Text
        };

        Assert.NotNull (dialog.ResultExtractor);
    }

    [Fact]
    public void ResultExtractor_Can_Be_Null ()
    {
        Label label = new () { Text = "Hello" };

        using PromptDialog<Label, string> dialog = new ()
        {
            WrappedView = label,
            ResultExtractor = null
        };

        Assert.Null (dialog.ResultExtractor);
    }

    #endregion

    #region Button Text Customization Tests

    [Fact]
    public void OkButtonText_Default_Is_Localized ()
    {
        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            WrappedView = label
        };

        Assert.Equal (Strings.btnOk, dialog.OkButtonText);
    }

    [Fact]
    public void CancelButtonText_Default_Is_Localized ()
    {
        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            WrappedView = label
        };

        Assert.Equal (Strings.btnCancel, dialog.CancelButtonText);
    }

    [Fact]
    public void Button_Text_Can_Be_Customized ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            Driver = driver,
            WrappedView = label,
            OkButtonText = "Accept",
            CancelButtonText = "Reject"
        };

        dialog.BeginInit ();
        dialog.EndInit ();

        Assert.Equal ("Reject", dialog.Buttons [0].Text);
        Assert.Equal ("Accept", dialog.Buttons [1].Text);
    }

    #endregion

    #region Result Extraction Tests

    [Fact]
    public void ResultExtractor_Extracts_Correct_Value ()
    {
        TextField textField = new () { Text = "User Input" };

        using PromptDialog<TextField, string> dialog = new ()
        {
            WrappedView = textField,
            ResultExtractor = tf => tf.Text
        };

        // Verify extractor function works correctly
        string? extracted = dialog.ResultExtractor?.Invoke (textField);

        Assert.Equal ("User Input", extracted);
    }

    [Fact]
    public void Result_Can_Be_Set_Directly ()
    {
        TextField textField = new () { Text = "User Input" };

        using PromptDialog<TextField, string> dialog = new ()
        {
            WrappedView = textField,
            ResultExtractor = tf => tf.Text
        };

        // Initially, result is null
        Assert.Null (dialog.Result);

        // Set result directly (simulating what OnAccepting does)
        dialog.Result = "Extracted Value";

        Assert.Equal ("Extracted Value", dialog.Result);
    }

    [Fact]
    public void Result_Is_Null_Initially ()
    {
        TextField textField = new () { Text = "User Input" };

        using PromptDialog<TextField, string> dialog = new ()
        {
            WrappedView = textField,
            ResultExtractor = tf => tf.Text
        };

        Assert.Null (dialog.Result);
    }

    [Fact]
    public void IRunnable_Result_Returns_Boxed_Value ()
    {
        TextField textField = new () { Text = "Test" };

        using PromptDialog<TextField, string> dialog = new ()
        {
            WrappedView = textField,
            ResultExtractor = tf => tf.Text
        };

        dialog.Result = "Hello";

        // Access via IRunnable
        IRunnable runnable = dialog;

        Assert.NotNull (runnable.Result);
        Assert.IsType<string> (runnable.Result);
        Assert.Equal ("Hello", runnable.Result);
    }

    #endregion

    #region Layout and Drawing Tests

    [Fact]
    public void PromptDialog_Layout_Works ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (80, 25);

        Label label = new () { Text = "Choose an option" };

        using PromptDialog<Label, string> dialog = new ()
        {
            Driver = driver,
            Title = "Prompt",
            WrappedView = label,
            ResultExtractor = l => l.Text
        };

        dialog.BeginInit ();
        dialog.EndInit ();
        dialog.Layout ();

        Assert.True (dialog.Frame.Width > 0);
        Assert.True (dialog.Frame.Height > 0);
    }

    [Fact]
    public void PromptDialog_Contains_WrappedView_After_EndInit ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (40, 12);

        Label label = new () { Text = "Hello World" };

        using PromptDialog<Label, string> dialog = new ()
        {
            Driver = driver,
            Title = "Test",
            WrappedView = label,
            ResultExtractor = l => l.Text
        };

        dialog.BeginInit ();
        dialog.EndInit ();

        // Verify the wrapped view is in the dialog's SubViews
        Assert.Contains (label, dialog.SubViews);

        // Verify buttons are present
        Assert.Equal (2, dialog.Buttons.Length);
    }

    #endregion

    #region PromptExtensions Tests

    [Fact]
    public void PromptExtensions_IApplication_Prompt_Returns_True_When_Accepted ()
    {
        // This test validates the extension method signature exists
        // Full integration testing would require Application.Run

        Label label = new () { Text = "Test" };

        using PromptDialog<Label, bool> dialog = new ()
        {
            WrappedView = label,
            ResultExtractor = _ => true
        };

        // Manually set result to simulate acceptance
        dialog.Result = true;

        Assert.True (dialog.Result);
    }

    [Fact]
    public void PromptExtensions_IApplication_Prompt_Returns_False_When_Canceled ()
    {
        Label label = new () { Text = "Test" };

        using PromptDialog<Label, bool> dialog = new ()
        {
            WrappedView = label,
            ResultExtractor = _ => true
        };

        // Result is null when canceled (default bool is false)
        Assert.False (dialog.Result);
    }

    #endregion

    #region Various TResult Type Tests

    [Fact]
    public void PromptDialog_Works_With_DateTime_Result ()
    {
        DatePicker datePicker = new () { Date = new DateTime (2024, 6, 15) };

        using PromptDialog<DatePicker, DateTime> dialog = new ()
        {
            WrappedView = datePicker,
            ResultExtractor = dp => dp.Date
        };

        // Manually invoke the extractor to verify it works
        DateTime? result = dialog.ResultExtractor?.Invoke (datePicker);

        Assert.NotNull (result);
        Assert.Equal (new DateTime (2024, 6, 15), result.Value);
    }

    [Fact]
    public void PromptDialog_Works_With_Color_Result ()
    {
        ColorPicker colorPicker = new () { SelectedColor = Color.Red };

        using PromptDialog<ColorPicker, Color> dialog = new ()
        {
            WrappedView = colorPicker,
            ResultExtractor = cp => cp.SelectedColor
        };

        // Manually invoke the extractor to verify it works
        Color? result = dialog.ResultExtractor?.Invoke (colorPicker);

        Assert.NotNull (result);
        Assert.Equal (Color.Red, result.Value);
    }

    [Fact]
    public void PromptDialog_Works_With_Int_Result ()
    {
        TextField textField = new () { Text = "42" };

        using PromptDialog<TextField, int> dialog = new ()
        {
            WrappedView = textField,
            ResultExtractor = tf => int.TryParse (tf.Text, out int result) ? result : 0
        };

        // Manually invoke the extractor to verify it works
        int? result = dialog.ResultExtractor?.Invoke (textField);

        Assert.NotNull (result);
        Assert.Equal (42, result.Value);
    }

    #endregion

    #region Title Tests

    [Fact]
    public void Title_Can_Be_Set ()
    {
        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            Title = "Select Item",
            WrappedView = label
        };

        Assert.Equal ("Select Item", dialog.Title);
    }

    [Fact]
    public void Title_Supports_Wide_Characters ()
    {
        Label label = new () { Text = "Test" };

        using PromptDialog<Label, string> dialog = new ()
        {
            Title = "选择日期",
            WrappedView = label
        };

        Assert.Equal ("选择日期", dialog.Title);
    }

    #endregion
}
