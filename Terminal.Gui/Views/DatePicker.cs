//
// DatePicker.cs: text entry for date
//
// Author: Maciej Winnik
//
// Licensed under the MIT license
//
using System;

namespace Terminal.Gui.Views;

/// <summary>
///   Simple Date editing <see cref="View"/>
/// </summary>
/// <remarks>
///   The <see cref="DateField"/> <see cref="View"/> provides date editing functionality with mouse support.
/// </remarks>
public class DatePicker : View {
	DateTime Date { get; set; }
	char SeparationChar { get; set; } = '/';
	public string Format { get; set; }

	void Initialize ()
	{
		var cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
		CanFocus = true;
	}

	int GetAmountOfDaysInMonth (int month, int year)
	{
		return DateTime.DaysInMonth (year, month);
	}

}
