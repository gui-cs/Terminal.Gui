using System.Globalization;

namespace Terminal.Gui.ViewsTests;

public class DatePickerTests
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

    [Fact]
    [AutoInitShutdown]
    public void DatePicker_ShouldNot_SetDateOutOfRange_UsingNextMonthButton ()
    {
        var date = new DateTime (9999, 11, 15);
        var datePicker = new DatePicker (date);

        var top = new Toplevel ();
        top.Add (datePicker);
        Application.Begin (top);

        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_dateField"), datePicker.Focused);

        // Set focus to next month button
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_calendar"), datePicker.Focused);
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_previousMonthButton"), datePicker.Focused);
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_nextMonthButton"), datePicker.Focused);

        // Change month to December
        Assert.False (Application.RaiseKeyDownEvent (Key.Enter));
        Assert.Equal (12, datePicker.Date.Month);

        // Next month button is disabled, so focus advanced to edit field
        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_previousMonthButton"), datePicker.Focused);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DatePicker_ShouldNot_SetDateOutOfRange_UsingPreviousMonthButton ()
    {
        var date = new DateTime (1, 2, 15);
        var datePicker = new DatePicker (date);
        var top = new Toplevel ();

        // Move focus to previous month button
        top.Add (datePicker);
        Application.Begin (top);

        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_dateField"), datePicker.Focused);

        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_calendar"), datePicker.Focused);
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_previousMonthButton"), datePicker.Focused);

        // Change month to January 
        Assert.False (datePicker.NewKeyDownEvent (Key.Enter));
        Assert.Equal (1, datePicker.Date.Month);

        // Next prev button is disabled, so focus advanced to edit button
        Assert.Equal (datePicker.Subviews.First (v => v.Id == "_calendar"), datePicker.Focused);

        top.Dispose ();
    }
}
