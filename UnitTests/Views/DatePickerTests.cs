using System;
using Terminal.Gui.Views;
using Xunit;

namespace Terminal.Gui.ViewsTests;

public class DatePickerTests {
	[Fact]
	public void DatePicker_DefaultFormat_ShouldBe_dd_MM_yyyy ()
	{
		var datePicker = new DatePicker ();
		Assert.Equal ("MM/dd/yyyy", datePicker.Format);
	}

	[Fact]
	public void DatePicker_SetFormat_ShouldChangeFormat ()
	{
		var datePicker = new DatePicker {
			Format = "dd/MM/yyyy"
		};
		Assert.Equal ("dd/MM/yyyy", datePicker.Format);
	}

	[Fact]
	public void DatePicker_SetYearsRange_ShouldChangeYearsRange ()
	{
		var datePicker = new DatePicker {
			YearsRange = 2000..2030
		};
		Assert.Equal (2000..2030, datePicker.YearsRange);
	}

	[Fact]
	public void DatePicker_Initialize_ShouldSetCurrentDate ()
	{
		var datePicker = new DatePicker ();
		Assert.Equal (DateTime.Now.ToString ("MM/dd/yyyy"), datePicker.Text);
	}

	[Fact]
	public void DatePicker_SetDate_ShouldChangeText ()
	{
		var datePicker = new DatePicker ();
		var newDate = new DateTime (2024, 1, 15);
		datePicker.Date = newDate;
		Assert.Equal (newDate.ToString ("MM/dd/yyyy"), datePicker.Text);
	}

	[Fact]
	public void DatePicker_ShowDatePickerDialog_ShouldChangeDate ()
	{
		var datePicker = new DatePicker ();
		var originalDate = datePicker.Date;

		datePicker.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked, X = 4, Y = 1 });

		var newDate = new DateTime (2024, 2, 20);
		datePicker.Date = newDate;

		Assert.Equal (newDate.ToString ("MM/dd/yyyy"), datePicker.Text);

		datePicker.Date = originalDate;
	}
}
