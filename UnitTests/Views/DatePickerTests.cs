using System;
using System.Globalization;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.ViewsTests;

public class DatePickerTests {

	[Fact]
	public void DatePicker_SetFormat_ShouldChangeFormat ()
	{
		var datePicker = new DatePicker {
			Format = "dd/MM/yyyy"
		};
		Assert.Equal ("dd/MM/yyyy", datePicker.Format);
	}

	[Fact]
	public void DatePicker_Initialize_ShouldSetCurrentDate ()
	{
		var datePicker = new DatePicker ();
		var format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
		Assert.Equal (DateTime.Now.ToString (format), datePicker.Text);
	}

	[Fact]
	public void DatePicker_SetDate_ShouldChangeText ()
	{
		var datePicker = new DatePicker ();
		var newDate = new DateTime (2024, 1, 15);
		var format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

		datePicker.Date = newDate;
		Assert.Equal (newDate.ToString (format), datePicker.Text);
	}

	[Fact]
	public void DatePicker_ShowDatePickerDialog_ShouldChangeDate ()
	{
		var datePicker = new DatePicker ();
		var format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
		var originalDate = datePicker.Date;

		datePicker.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked, X = 4, Y = 1 });

		var newDate = new DateTime (2024, 2, 20);
		datePicker.Date = newDate;

		Assert.Equal (newDate.ToString (format), datePicker.Text);

		datePicker.Date = originalDate;
	}
}
