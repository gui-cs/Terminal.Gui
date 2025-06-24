using System.Globalization;

namespace Terminal.Gui.ViewsTests;

public class NumericUpDownTests
{
    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_int ()
    {
        NumericUpDown<int> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_long ()
    {
        NumericUpDown<long> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_float ()
    {
        NumericUpDown<float> numericUpDown = new ();

        Assert.Equal (0F, numericUpDown.Value);
        Assert.Equal (1.0F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_double ()
    {
        NumericUpDown<double> numericUpDown = new ();

        Assert.Equal (0F, numericUpDown.Value);
        Assert.Equal (1.0F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_decimal ()
    {
        NumericUpDown<decimal> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_int ()
    {
        NumericUpDown<int> numericUpDown = new()
        {
            Value = 10,
            Increment = 2
        };

        Assert.Equal (10, numericUpDown.Value);
        Assert.Equal (2, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_float ()
    {
        NumericUpDown<float> numericUpDown = new()
        {
            Value = 10.5F,
            Increment = 2.5F
        };

        Assert.Equal (10.5F, numericUpDown.Value);
        Assert.Equal (2.5F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_decimal ()
    {
        NumericUpDown<decimal> numericUpDown = new ()
        {
            Value = 10.5m,
            Increment = 2.5m
        };

        Assert.Equal (10.5m, numericUpDown.Value);
        Assert.Equal (2.5m, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithInvalidType_ShouldThrowInvalidOperationException ()
    {
        Assert.Throws<InvalidOperationException> (() => new NumericUpDown<string> ());
    }

    [Fact]
    public void WhenCreatedWithInvalidTypeObject_ShouldNotThrowInvalidOperationException ()
    {
        Exception exception = Record.Exception (() => new NumericUpDown<object> ());
        Assert.Null (exception);
    }

    [Fact]
    public void WhenCreatedWithValidNumberType_ShouldThrowInvalidOperationException_UnlessTheyAreRegisterAsValid ()
    {
        Exception exception = Record.Exception (() => new NumericUpDown<short> ());
        Assert.NotNull (exception);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_int ()
    {
        NumericUpDown<int> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_float ()
    {
        NumericUpDown<float> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_double ()
    {
        NumericUpDown<double> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_long ()
    {
        NumericUpDown<long> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_decimal ()
    {
        NumericUpDown<decimal> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_Text_Should_Be_Correct_int ()
    {
        NumericUpDown<int> numericUpDown = new ();

        Assert.Equal ("0", numericUpDown.Text);
    }

    [Fact]
    public void WhenCreated_Text_Should_Be_Correct_float ()
    {
        NumericUpDown<float> numericUpDown = new ();

        Assert.Equal ("0", numericUpDown.Text);
    }

    [Fact]
    public void Format_Default ()
    {
        NumericUpDown<float> numericUpDown = new ();

        Assert.Equal ("{0}", numericUpDown.Format);
    }

    [Theory]
    [InlineData (0F, "{0}", "0")]
    [InlineData (1.1F, "{0}", "1.1")]
    [InlineData (0F, "{0:0%}", "0%")]
    [InlineData (.75F, "{0:0%}", "75%")]
    public void Format_decimal (float value, string format, string expectedText)
    {
        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        NumericUpDown<float> numericUpDown = new ();

        numericUpDown.Format = format;
        numericUpDown.Value = value;

        Assert.Equal (expectedText, numericUpDown.Text);

        CultureInfo.CurrentCulture = currentCulture;
    }

    [Theory]
    [InlineData (0, "{0}", "0")]
    [InlineData (11, "{0}", "11")]
    [InlineData (-1, "{0}", "-1")]
    [InlineData (911, "{0:X}", "38F")]
    [InlineData (911, "0x{0:X04}", "0x038F")]
    public void Format_int (int value, string format, string expectedText)
    {
        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        NumericUpDown<int> numericUpDown = new ();

        numericUpDown.Format = format;
        numericUpDown.Value = value;

        Assert.Equal (expectedText, numericUpDown.Text);

        CultureInfo.CurrentCulture = currentCulture;
    }

    [Fact]
    public void KeyDown_CursorUp_Increments ()
    {
        NumericUpDown<int> numericUpDown = new ();

        numericUpDown.NewKeyDownEvent (Key.CursorUp);

        Assert.Equal (1, numericUpDown.Value);
    }

    [Fact]
    public void KeyDown_CursorDown_Decrements ()
    {
        NumericUpDown<int> numericUpDown = new ();

        numericUpDown.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal (-1, numericUpDown.Value);
    }
}
