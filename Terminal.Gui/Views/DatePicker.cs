//
// DatePicker.cs: DatePicker control
//
// Author: Maciej Winnik
//
using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Terminal.Gui.Views;

/// <summary>
/// The <see cref="DatePicker"/> <see cref="View"/> Date Picker.
/// </summary>
public class DatePicker : View {

	private DataTable _table;
	private TableView _calendar;
	private DateTime _date = DateTime.Now;

	private DateField _dateField;

	/// <summary>
	/// Format of date. The default is MM/dd/yyyy.
	/// </summary>
	public string Format { get; set; } = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

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
	public DatePicker () => SetInitialProperties (_date);

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/> with the specified date.
	/// </summary>
	public DatePicker (DateTime date)
	{
		SetInitialProperties (date);
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/> with the specified date and format.
	/// </summary>
	public DatePicker (DateTime date, string format)
	{
		Format = format;
		SetInitialProperties (date);
	}

	private void SetInitialProperties (DateTime date)
	{
		Title = "Date Picker";
		BorderStyle = LineStyle.Single;
		Date = date;
		var dateLabel = new Label ("Date: ") {
			X = 0,
			Y = 0,
			Height = 1,
		};

		_dateField = new DateField (DateTime.Now) {
			X = Pos.Right (dateLabel),
			Y = 0,
			Width = Dim.Fill (1),
			Height = 1,
			IsShortFormat = false
		};

		_calendar = new TableView () {
			X = 0,
			Y = Pos.Bottom (dateLabel),
			Height = 11,
			Style = new TableStyle {
				ShowHeaders = true,
				ShowHorizontalBottomline = true,
				ShowVerticalCellLines = true,
				ExpandLastColumn = true,
			}
		};

		var yearsLabel = new Label ("Year:") {
			X = Pos.Right (_calendar) + 1,
			Y = 1,
			Height = 1,
		};

		var previousMonthButton = new Button (GetBackButtonText ()) {
			X = Pos.Center () - 4,
			Y = Pos.Bottom (_calendar) - 1,
			Height = 1,
			Width = CalculateCalendarWidth () / 2
		};

		previousMonthButton.Clicked += (sender, e) => {
			Date = _date.AddMonths (-1);
			CreateCalendar ();
			_dateField.Date = Date;
		};

		var nextMonthButton = new Button (GetForwardButtonText ()) {
			X = Pos.Right (previousMonthButton) + 2,
			Y = Pos.Bottom (_calendar) - 1,
			Height = 1,
			Width = CalculateCalendarWidth () / 2
		};

		nextMonthButton.Clicked += (sender, e) => {
			Date = _date.AddMonths (1);
			CreateCalendar ();
			_dateField.Date = Date;
		};

		CreateCalendar ();
		SelectDayOnCalendar (_date.Day);

		_calendar.CellActivated += (sender, e) => {
			var dayValue = _table.Rows [e.Row] [e.Col];
			if (dayValue is null) {
				return;
			}
			int day = int.Parse (dayValue.ToString ());
			ChangeDayDate (day);
			SelectDayOnCalendar (day);
			Text = _date.ToString (Format);

		};

		Width = CalculateCalendarWidth () + 2;
		Height = _calendar.Height + 3;

		_dateField.DateChanged += DateField_DateChanged;

		Add (dateLabel, _dateField, _calendar, previousMonthButton, nextMonthButton);

	}

	private void DateField_DateChanged (object sender, DateTimeEventArgs<DateTime> e)
	{
		if (e.NewValue.Date.Day != _date.Day) {
			SelectDayOnCalendar (e.NewValue.Day);
		}
		Date = e.NewValue;
		CreateCalendar ();
		SelectDayOnCalendar (_date.Day);
	}

	private void CreateCalendar ()
	{
		_calendar.Table = new DataTableSource (_table = CreateDataTable (_date.Month, _date.Year));
	}

	private void ChangeDayDate (int day)
	{
		_date = new DateTime (_date.Year, _date.Month, day);
		_dateField.Date = _date;
		CreateCalendar ();
	}

	private DataTable CreateDataTable (int month, int year)
	{
		_table = new DataTable ();
		GenerateCalendarLabels ();
		int amountOfDaysInMonth = DateTime.DaysInMonth (year, month);
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

	private void GenerateCalendarLabels ()
	{
		_calendar.Style.ColumnStyles.Clear ();
		for (int i = 0; i < 7; i++) {
			var abbreviatedDayName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName ((DayOfWeek)i);
			_calendar.Style.ColumnStyles.Add (i, new ColumnStyle () {
				MaxWidth = abbreviatedDayName.Length,
				MinWidth = abbreviatedDayName.Length,
				MinAcceptableWidth = abbreviatedDayName.Length
			});
			_table.Columns.Add (abbreviatedDayName);
		}
		_calendar.Width = CalculateCalendarWidth ();
	}

	private int CalculateCalendarWidth ()
	{
		return _calendar.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 7;
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

	private string GetForwardButtonText () => Glyphs.RightArrow.ToString () + Glyphs.RightArrow.ToString ();

	private string GetBackButtonText () => Glyphs.LeftArrow.ToString () + Glyphs.LeftArrow.ToString ();
}
