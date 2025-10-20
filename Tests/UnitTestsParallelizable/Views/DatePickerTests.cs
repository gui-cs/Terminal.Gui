using System.Globalization;

namespace Terminal.Gui.ViewsTests;

/// <summary>
/// Pure unit tests for <see cref="DatePicker"/> that don't require Application.Driver or View context.
/// These tests can run in parallel without interference.
/// </summary>
public class DatePickerTests : UnitTests.Parallelizable.ParallelizableBase
{
    [Fact]
    public void DatePicker_ChangingCultureChangesFormat ()
    {
        var date = new DateTime (2000, 7, 23);
        var datePicker = new DatePicker (date);

        datePicker.Culture = CultureInfo.GetCultureInfo ("en-GB");
        Assert.Equal ("23/07/2000", datePicker.Text);

        datePicker.Culture = CultureInfo.GetCultureInfo ("pl-PL");
        Assert.Equal ("23.07.2000", datePicker.Text);

        // Deafult date format for en-US is M/d/yyyy but we are using StandardizeDateFormat method
        // to convert it to the format that has 2 digits for month and day.
        datePicker.Culture = CultureInfo.GetCultureInfo ("en-US");
        Assert.Equal ("07/23/2000", datePicker.Text);
    }

    [Fact]
    public void DatePicker_Default_Constructor_ShouldSetCurrenDate ()
    {
        var datePicker = new DatePicker ();
        Assert.Equal (DateTime.Now.Date.Day, datePicker.Date.Day);
        Assert.Equal (DateTime.Now.Date.Month, datePicker.Date.Month);
        Assert.Equal (DateTime.Now.Date.Year, datePicker.Date.Year);
    }

    [Fact]
    public void DatePicker_Constrctor_Now_ShouldSetCurrenDate ()
    {
        var datePicker = new DatePicker (DateTime.Now);
        Assert.Equal (DateTime.Now.Date.Day, datePicker.Date.Day);
        Assert.Equal (DateTime.Now.Date.Month, datePicker.Date.Month);
        Assert.Equal (DateTime.Now.Date.Year, datePicker.Date.Year);
    }

    [Fact]
    public void DatePicker_X_Y_Init ()
    {
        var datePicker = new DatePicker { Y = Pos.Center (), X = Pos.Center () };
        Assert.Equal (DateTime.Now.Date.Day, datePicker.Date.Day);
        Assert.Equal (DateTime.Now.Date.Month, datePicker.Date.Month);
        Assert.Equal (DateTime.Now.Date.Year, datePicker.Date.Year);
    }

    [Fact]
    public void DatePicker_SetDate_ShouldChangeText ()
    {
        var datePicker = new DatePicker { Culture = CultureInfo.GetCultureInfo ("en-GB") };
        var newDate = new DateTime (2024, 1, 15);
        string format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

        datePicker.Date = newDate;
        Assert.Equal (newDate.ToString (format), datePicker.Text);
    }
}
