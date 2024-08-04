using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class NumericUpDownTests (ITestOutputHelper _output)
{

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_int ()
    {
        NumericUpDown<int> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (int.MinValue, numericUpDown.Minimum);
        Assert.Equal (int.MaxValue, numericUpDown.Maximum);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_long ()
    {
        NumericUpDown<long> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (long.MinValue, numericUpDown.Minimum);
        Assert.Equal (long.MaxValue, numericUpDown.Maximum);
        Assert.Equal (1, numericUpDown.Increment);
    }
    
    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_float ()
    {
        NumericUpDown<float> numericUpDown = new ();

        Assert.Equal (0F, numericUpDown.Value);
        Assert.Equal (float.MinValue, numericUpDown.Minimum);
        Assert.Equal (float.MaxValue, numericUpDown.Maximum);
        Assert.Equal (1.0F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_double ()
    {
        NumericUpDown<double> numericUpDown = new ();

        Assert.Equal (0F, numericUpDown.Value);
        Assert.Equal (double.MinValue, numericUpDown.Minimum);
        Assert.Equal (double.MaxValue, numericUpDown.Maximum);
        Assert.Equal (1.0F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_decimal ()
    {
        NumericUpDown<decimal> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (decimal.MinValue, numericUpDown.Minimum);
        Assert.Equal (decimal.MaxValue, numericUpDown.Maximum);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_int ()
    {
        var numericUpDown = new NumericUpDown<int> ()
        {
            Value = 10,
            Minimum = 5,
            Maximum = 15,
            Increment = 2
        };

        Assert.Equal (10, numericUpDown.Value);
        Assert.Equal (5, numericUpDown.Minimum);
        Assert.Equal (15, numericUpDown.Maximum);
        Assert.Equal (2, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_float ()
    {
        var numericUpDown = new NumericUpDown<float> ()
        {
            Value = 10.5F,
            Minimum = 5.5F,
            Maximum = 15.5F,
            Increment = 2.5F
        };

        Assert.Equal (10.5F, numericUpDown.Value);
        Assert.Equal (5.5F, numericUpDown.Minimum);
        Assert.Equal (15.5F, numericUpDown.Maximum);
        Assert.Equal (2.5F, numericUpDown.Increment);
    }

    [Fact]
    public void NumericUpDown_WhenCreatedWithInvalidType_ShouldThrowInvalidOperationException ()
    {
        Assert.Throws<InvalidOperationException> (() => new NumericUpDown<string> ());
    }

    [Fact]
    public void NumericUpDown_WhenCreatedWithInvalidTypeObject_ShouldNotThrowInvalidOperationException ()
    {
        var numericUpDown = new NumericUpDown<object> ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (0, numericUpDown.Minimum);
        Assert.Equal (100, numericUpDown.Maximum);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void NumericUpDown_WhenCreatedWithInvalidTypeObjectAndCustomValues_ShouldHaveCustomValues ()
    {
        var numericUpDown = new NumericUpDown<object> ()
        {
            Value = 10,
            Minimum = 5,
            Maximum = 15,
            Increment = 2
        };

        Assert.Equal (10, numericUpDown.Value);
        Assert.Equal (5, numericUpDown.Minimum);
        Assert.Equal (15, numericUpDown.Maximum);
        Assert.Equal (2, numericUpDown.Increment);
    }

    [Fact]
    public void NumericUpDown_WhenCreated_ShouldHaveDefaultWidthAndHeight_int ()
    {
        var numericUpDown = new NumericUpDown<int> ();
        numericUpDown.SetRelativeLayout(Application.Screen.Size);

        Assert.Equal (5, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void NumericUpDown_WhenCreated_ShouldHaveDefaultWidthAndHeight_float ()
    {
        var numericUpDown = new NumericUpDown<float> ();
        numericUpDown.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (5, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void NumericUpDown_WhenCreated_ShouldHaveDefaultUpDownButtons ()
    {
        var numericUpDown = new NumericUpDown<int> ();

        //   Assert.Equal (1, numericUpDown
    }

}