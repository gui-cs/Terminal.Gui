//
// DatePicker.cs: DatePicker control
//
// Author: Maciej Winnik
//

using System.Data;
using System.Globalization;

namespace Terminal.Gui;

/// <summary>
///         The <see cref="DatePicker" /> <see cref="View" /> Date Picker.
/// </summary>
public class DatePicker : View {
	TableView _calendar;

	DateTime _date = DateTime.Now;

	DateField _dateField;
	Label _dateLabel;
	Button _nextMonthButton;
	Button _previousMonthButton;
	DataTable _table;

	/// <summary>
	///         Initializes a new instance of <see cref="DatePicker" />.
	/// </summary>
	public DatePicker () => SetInitialProperties (_date);

	/// <summary>
	///         Initializes a new instance of <see cref="DatePicker" /> with the specified date.
	/// </summary>
	public DatePicker (DateTime date) => SetInitialProperties (date);


	/// <summary>
	///         CultureInfo for date. The default is CultureInfo.CurrentCulture.
	/// </summary>
	public CultureInfo Culture {
		get => CultureInfo.CurrentCulture;
		set {
			if (value is not null) {
				CultureInfo.CurrentCulture = value;
				Text = Date.ToString (Format);
			}
		}
	}

	string Format => StandardizeDateFormat (Culture.DateTimeFormat.ShortDatePattern);

	/// <summary>
	///         Get or set the date.
	/// </summary>
	public DateTime Date {
		get => _date;
		set {
			_date = value;
			Text = _date.ToString (Format);
		}
	}

	void SetInitialProperties (DateTime date)
	{
		Title = "Date Picker";
		BorderStyle = LineStyle.Single;
		Date = date;
		_dateLabel = new Label {
			X = 0,
			Y = 0,
			Height = 1,
			Text = "Date: "
		};

		_dateField = new DateField (DateTime.Now) {
			X = Pos.Right (_dateLabel),
			Y = 0,
			Width = Dim.Fill (1),
			Height = 1,
			Culture = Culture
		};

		_calendar = new TableView {
			X = 0,
			Y = Pos.Bottom (_dateLabel),
			Height = 11,
			Style = new TableStyle {
				ShowHeaders = true,
				ShowHorizontalBottomline = true,
				ShowVerticalCellLines = true,
				ExpandLastColumn = true
			}
		};

		_previousMonthButton = new Button {
			X = Pos.Center () - 4,
			Y = Pos.Bottom (_calendar) - 1,
			Height = 1,
			Width = CalculateCalendarWidth () / 2,
			Text = GetBackButtonText ()
		};

		_previousMonthButton.Clicked += (sender, e) => {
			Date = _date.AddMonths (-1);
			CreateCalendar ();
			_dateField.Date = Date;
		};

		_nextMonthButton = new Button {
			X = Pos.Right (_previousMonthButton) + 2,
			Y = Pos.Bottom (_calendar) - 1,
			Height = 1,
			Width = CalculateCalendarWidth () / 2,
			Text = GetBackButtonText ()
		};

		_nextMonthButton.Clicked += (sender, e) => {
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

			var isDay = int.TryParse (dayValue.ToString (), out var day);
			if (!isDay) {
				return;
			}

			ChangeDayDate (day);
			SelectDayOnCalendar (day);
			Text = _date.ToString (Format);
		};

		Width = CalculateCalendarWidth () + 2;
		Height = _calendar.Height + 3;

		_dateField.DateChanged += DateField_DateChanged;

		Add (_dateLabel, _dateField, _calendar, _previousMonthButton, _nextMonthButton);
	}

	void DateField_DateChanged (object sender, DateTimeEventArgs<DateTime> e)
	{
		Date = e.NewValue;
		if (e.NewValue.Date.Day != _date.Day) {
			SelectDayOnCalendar (e.NewValue.Day);
		}

		if (_date.Month == DateTime.MinValue.Month && _date.Year == DateTime.MinValue.Year) {
			_previousMonthButton.Enabled = false;
		} else {
			_previousMonthButton.Enabled = true;
		}

		if (_date.Month == DateTime.MaxValue.Month && _date.Year == DateTime.MaxValue.Year) {
			_nextMonthButton.Enabled = false;
		} else {
			_nextMonthButton.Enabled = true;
		}

		CreateCalendar ();
		SelectDayOnCalendar (_date.Day);
	}

	void CreateCalendar () =>
		_calendar.Table = new DataTableSource (_table = CreateDataTable (_date.Month, _date.Year));

	void ChangeDayDate (int day)
	{
		_date = new DateTime (_date.Year, _date.Month, day);
		_dateField.Date = _date;
		CreateCalendar ();
	}

	DataTable CreateDataTable (int month, int year)
	{
		_table = new DataTable ();
		GenerateCalendarLabels ();
		var amountOfDaysInMonth = DateTime.DaysInMonth (year, month);
		var dateValue = new DateTime (year, month, 1);
		var dayOfWeek = dateValue.DayOfWeek;

		_table.Rows.Add (new object [6]);
		for (var i = 1; i <= amountOfDaysInMonth; i++) {
			_table.Rows [^1] [(int)dayOfWeek] = i;
			if (dayOfWeek == DayOfWeek.Saturday && i != amountOfDaysInMonth) {
				_table.Rows.Add (new object [7]);
			}

			dayOfWeek = dayOfWeek == DayOfWeek.Saturday ? DayOfWeek.Sunday : dayOfWeek + 1;
		}

		var missingRows = 6 - _table.Rows.Count;
		for (var i = 0; i < missingRows; i++) {
			_table.Rows.Add (new object [7]);
		}

		return _table;
	}

	void GenerateCalendarLabels ()
	{
		_calendar.Style.ColumnStyles.Clear ();
		for (var i = 0; i < 7; i++) {
			var abbreviatedDayName =
				CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName ((DayOfWeek)i);
			_calendar.Style.ColumnStyles.Add (i, new ColumnStyle {
				MaxWidth = abbreviatedDayName.Length,
				MinWidth = abbreviatedDayName.Length,
				MinAcceptableWidth = abbreviatedDayName.Length
			});
			_table.Columns.Add (abbreviatedDayName);
		}

		_calendar.Width = CalculateCalendarWidth ();
	}

	int CalculateCalendarWidth () => _calendar.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 7;

	void SelectDayOnCalendar (int day)
	{
		for (var i = 0; i < _table.Rows.Count; i++) {
			for (var j = 0; j < _table.Columns.Count; j++) {
				if (_table.Rows [i] [j].ToString () == day.ToString ()) {
					_calendar.SetSelection (j, i, false);
					return;
				}
			}
		}
	}

	string GetForwardButtonText () => Glyphs.RightArrow + Glyphs.RightArrow.ToString ();

	string GetBackButtonText () => Glyphs.LeftArrow + Glyphs.LeftArrow.ToString ();

	static string StandardizeDateFormat (string format) =>
		format switch {
			"MM/dd/yyyy" => "MM/dd/yyyy",
			"yyyy-MM-dd" => "yyyy-MM-dd",
			"yyyy/MM/dd" => "yyyy/MM/dd",
			"dd/MM/yyyy" => "dd/MM/yyyy",
			"d?/M?/yyyy" => "dd/MM/yyyy",
			"dd.MM.yyyy" => "dd.MM.yyyy",
			"dd-MM-yyyy" => "dd-MM-yyyy",
			"dd/MM yyyy" => "dd/MM/yyyy",
			"d. M. yyyy" => "dd.MM.yyyy",
			"yyyy.MM.dd" => "yyyy.MM.dd",
			"g yyyy/M/d" => "yyyy/MM/dd",
			"d/M/yyyy" => "dd/MM/yyyy",
			"d?/M?/yyyy g" => "dd/MM/yyyy",
			"d-M-yyyy" => "dd-MM-yyyy",
			"d.MM.yyyy" => "dd.MM.yyyy",
			"d.MM.yyyy '?'." => "dd.MM.yyyy",
			"M/d/yyyy" => "MM/dd/yyyy",
			"d. M. yyyy." => "dd.MM.yyyy",
			"d.M.yyyy." => "dd.MM.yyyy",
			"g yyyy-MM-dd" => "yyyy-MM-dd",
			"d.M.yyyy" => "dd.MM.yyyy",
			"d/MM/yyyy" => "dd/MM/yyyy",
			"yyyy/M/d" => "yyyy/MM/dd",
			"dd. MM. yyyy." => "dd.MM.yyyy",
			"yyyy. MM. dd." => "yyyy.MM.dd",
			"yyyy. M. d." => "yyyy.MM.dd",
			"d. MM. yyyy" => "dd.MM.yyyy",
			_ => "dd/MM/yyyy"
		};

	/// <inheritdoc />
	protected override void Dispose (bool disposing)
	{
		_dateLabel.Dispose ();
		_calendar.Dispose ();
		_dateField.Dispose ();
		_table.Dispose ();
		_previousMonthButton.Dispose ();
		_nextMonthButton.Dispose ();
		base.Dispose (disposing);
	}
}