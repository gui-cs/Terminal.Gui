using System.Globalization;
using UnitTests;

namespace Terminal.Gui.ViewsTests;

public class DatePickerTests
{
    [Fact]
    [AutoInitShutdown]
    public void DatePicker_ShouldNot_SetDateOutOfRange_UsingNextMonthButton ()
    {
        var date = new DateTime (9999, 11, 15);
        var datePicker = new DatePicker (date);

        var top = new Toplevel ();
        top.Add (datePicker);
        Application.Begin (top);

        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_dateField"), datePicker.Focused);

        // Set focus to next month button
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_calendar"), datePicker.Focused);
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_previousMonthButton"), datePicker.Focused);
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_nextMonthButton"), datePicker.Focused);

        // Change month to December
        Assert.False (Application.RaiseKeyDownEvent (Key.Enter));
        Assert.Equal (12, datePicker.Date.Month);

        // Next month button is disabled, so focus advanced to edit field
        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_previousMonthButton"), datePicker.Focused);

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

        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_dateField"), datePicker.Focused);

        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_calendar"), datePicker.Focused);
        datePicker.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_previousMonthButton"), datePicker.Focused);

        // Change month to January 
        Assert.False (datePicker.NewKeyDownEvent (Key.Enter));
        Assert.Equal (1, datePicker.Date.Month);

        // Next prev button is disabled, so focus advanced to edit button
        Assert.Equal (datePicker.SubViews.First (v => v.Id == "_calendar"), datePicker.Focused);

        top.Dispose ();
    }
}
