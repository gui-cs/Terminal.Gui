//
// DatePicker.cs: DatePicker control
//
// Author: Maciej Winnik
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Terminal.Gui.Resources;

namespace Terminal.Gui.Views;

/// <summary>
/// The <see cref="DatePicker"/> <see cref="View"/> Date Picker.
/// </summary>
public class DatePicker : TextField {

	private DataTable _table;
	private TableView _calendar;
	private DateTime _date = DateTime.Now;
	private List<string> _months = new ();
	private List<string> years = new ();

	private readonly int comboBoxHeight = 4;
	private readonly int comboBoxWidth = 12;
	private readonly int buttonWidth = 12;
	private readonly int calendarWidth = 22;
	private readonly int dialogHeight = 14;
	private readonly int dialogWidth = 40;

	/// <summary>
	/// Format of date. The default is MM/dd/yyyy.
	/// </summary>
	public string Format { get; set; } = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

	/// <summary>
	/// Range of years in the calendar. The default is 1900..2100.
	/// </summary>
	public Range YearsRange { get; set; } = 1900..2100;

	/// <summary>
	/// Get or set the date.
	/// </summary>
	public DateTime Date {
		get => _date;
		set {
			_date = value;
			Text = _date.ToString (Format);
		}
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/>.
	/// </summary>
	public DatePicker () : base () => SetInitialProperties (_date);

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/> with the specified date.
	/// </summary>
	public DatePicker (DateTime date) : base ()
	{
		SetInitialProperties (date);
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/> with the specified date and format.
	/// </summary>
	public DatePicker (DateTime date, string format) : base ()
	{
		Format = format;
		SetInitialProperties (date);
	}

	void SetInitialProperties (DateTime date)
	{
		Date = date;
		_months = Enum.GetValues (typeof (Month)).Cast<Month> ().Select (x => x.ToString ()).ToList ();
	}

	private void ShowDatePickerDialog ()
	{
		years = Enumerable.Range (YearsRange.Start.Value, YearsRange.End.Value - YearsRange.Start.Value + 1)
			.Select (x => x.ToString ())
			.ToList ();

		if (_date.Year < YearsRange.Start.Value || _date.Year > YearsRange.End.Value) {
			MessageBox.ErrorQuery ("Error", "Date year is out of range", "Ok");
			return;
		}

		var dialog = new Dialog () {
			Title = Strings.dpTitle,
			X = Pos.Center (),
			Y = Pos.Center (),
			Height = dialogHeight,
			Width = dialogWidth,
		};

		_calendar = new TableView () {
			X = 0,
			Y = Pos.Center () + 1,
			Width = calendarWidth,
			Height = Dim.Fill (1),
			Style = new TableStyle {
				ShowHeaders = true,
				ShowHorizontalHeaderOverline = true,
				ShowHorizontalHeaderUnderline = true,
				ShowHorizontalBottomline = true,
				ShowVerticalCellLines = true,
				ExpandLastColumn = false,
			}
		};

		var yearsLabel = new Label ("Year:") {
			X = Pos.Right (_calendar) + 1,
			Y = 1,
			Height = 1,
		};
		var comboBoxYear = new ComboBox (years) {
			X = Pos.Right (_calendar) + 1,
			Y = Pos.Bottom (yearsLabel),
			Width = comboBoxWidth,
			Height = comboBoxHeight,
			SelectedItem = years.IndexOf (_date.Year.ToString ())
		};
		comboBoxYear.SelectedItemChanged += ChangeYearDate;

		var monthsLabel = new Label ("Month:") {
			X = Pos.Right (_calendar) + 1,
			Y = Pos.Bottom (comboBoxYear),
			Height = 1,
		};

		var comboBoxMonth = new ComboBox (_months) {
			X = Pos.Right (_calendar) + 1,
			Y = Pos.Bottom (monthsLabel),
			Width = comboBoxWidth,
			Height = comboBoxHeight,
			SelectedItem = _date.Month - 1
		};
		comboBoxMonth.SelectedItemChanged += ChangeMonthDate;

		var previousMonthButton = new Button ("Previous") {
			X = 0,
			Y = Pos.Bottom (_calendar),
			Width = buttonWidth,
			Height = 1,
		};

		previousMonthButton.Clicked += (sender, e) => {
			Date = _date.AddMonths (-1);
			CreateCalendar ();
			if (comboBoxMonth.SelectedItem == 0) {
				comboBoxYear.SelectedItem--;
				comboBoxMonth.SelectedItem = 11;
			} else {
				comboBoxMonth.SelectedItem = _date.Month - 1;
			}
		};

		var nextMonthButton = new Button ("Next") {
			X = Pos.Right (previousMonthButton),
			Y = Pos.Bottom (_calendar),
			Width = buttonWidth,
			Height = 1,
		};

		nextMonthButton.Clicked += (sender, e) => {
			Date = _date.AddMonths (1);
			CreateCalendar ();
			if (comboBoxMonth.SelectedItem == 11) {
				comboBoxYear.SelectedItem++;
				comboBoxMonth.SelectedItem = 0;
			} else {
				comboBoxMonth.SelectedItem = _date.Month - 1;
			}
		};

		dialog.Add (_calendar,
			yearsLabel, comboBoxYear,
			monthsLabel, comboBoxMonth,
			previousMonthButton, nextMonthButton);

		CreateCalendar ();
		SelectDayOnCalendar (_date.Day);

		_calendar.CellActivated += (sender, e) => {
			var dayValue = _table.Rows [e.Row] [e.Col];
			int day = int.Parse (dayValue.ToString ());
			ChangeDayDate (day);

			SelectDayOnCalendar (day);
			try {
				Text = _date.ToString (Format);
			} catch (Exception) {
				MessageBox.ErrorQuery ("Error", "Invalid date format", "Ok");
			}
			Application.RequestStop ();
		};

		Application.Run (dialog);
	}

	private void CreateCalendar ()
	{
		_calendar.Table = new DataTableSource (_table = CreateDataTable (_date.Month, _date.Year));
	}

	private void ChangeYearDate (object sender, ListViewItemEventArgs e)
	{
		int year = int.Parse (e.Value.ToString ());
		_date = new DateTime (year, _date.Month, _date.Day);
		CreateCalendar ();
	}

	private void ChangeMonthDate (object sender, ListViewItemEventArgs e)
	{
		int monthNumber = (int)Enum.Parse (typeof (Month), e.Value.ToString ());
		_date = new DateTime (_date.Year, monthNumber, _date.Day);
		CreateCalendar ();
	}

	private void ChangeDayDate (int day)
	{
		_date = new DateTime (_date.Year, _date.Month, day);
		CreateCalendar ();
	}

	private DataTable CreateDataTable (int month, int year)
	{
		_table = new DataTable ();
		_table.Columns.Add ("Su");
		_table.Columns.Add ("Mo");
		_table.Columns.Add ("Tu");
		_table.Columns.Add ("We");
		_table.Columns.Add ("Th");
		_table.Columns.Add ("Fr");
		_table.Columns.Add ("Sa");
		int amountOfDaysInMonth = GetAmountOfDaysInMonth (month, year);
		DateTime dateValue = new DateTime (year, month, 1);
		var dayOfWeek = dateValue.DayOfWeek;

		_table.Rows.Add (new object [6]);
		for (int i = 1; i <= amountOfDaysInMonth; i++) {
			_table.Rows [^1] [(int)dayOfWeek] = i;
			if (dayOfWeek == DayOfWeek.Saturday && i != amountOfDaysInMonth) {
				_table.Rows.Add (new object [7]);
			}
			dayOfWeek = dayOfWeek == DayOfWeek.Saturday ? DayOfWeek.Sunday : dayOfWeek + 1;
		}

		return _table;
	}

	private void SelectDayOnCalendar (int day)
	{
		for (int i = 0; i < _table.Rows.Count; i++) {
			for (int j = 0; j < _table.Columns.Count; j++) {
				if (_table.Rows [i] [j].ToString () == day.ToString ()) {
					_calendar.SetSelection (j, i, false);
					return;
				}
			}
		}
	}

	int GetAmountOfDaysInMonth (int month, int year)
	{
		return DateTime.DaysInMonth (year, month);
	}

	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		base.OnDrawContent (contentArea);
		Driver.SetAttribute (ColorScheme.Focus);
		Move (Bounds.Right - 1, 0);
		Driver.AddRune (Glyphs.BlackCircle);
	}

	///<inheritdoc/>
	public override bool MouseEvent (MouseEvent me)
	{
		if (me.X == Bounds.Right - 1 && me.Y == Bounds.Top && me.Flags == MouseFlags.Button1Pressed) {
			ShowDatePickerDialog ();
			return true;
		}

		return false;
	}

	enum Month {
		January = 1,
		February = 2,
		March = 3,
		April = 4,
		May = 5,
		June = 6,
		July = 7,
		August = 8,
		September = 9,
		October = 10,
		November = 11,
		December = 12
	}

}
