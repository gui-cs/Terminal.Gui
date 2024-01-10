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

	private ComboBox _comboBoxYear;
	private ComboBox _comboBoxMonth;

	private List<string> _months = new ();
	private List<string> _years = new ();

	private readonly int comboBoxHeight = 4;

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
		_months = GetMonthNames ();
	}

	private void ShowDatePickerDialog ()
	{
		_years = Enumerable.Range (YearsRange.Start.Value, YearsRange.End.Value - YearsRange.Start.Value + 1)
			.Select (x => x.ToString ())
			.ToList ();

		var dialog = new Dialog () {
			Title = Strings.dpTitle,
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = Dim.Percent (50),
			Height = Dim.Percent (50),
		};

		_calendar = new TableView () {
			X = 0,
			Y = 0,
			Width = Dim.Fill (1),
			Height = Dim.Fill (1),
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

		_comboBoxYear = new ComboBox (_years) {
			X = Pos.Right (_calendar) + 1,
			Y = Pos.Bottom (yearsLabel),
			Width = Dim.Fill (),
			Height = comboBoxHeight,
			SelectedItem = _years.IndexOf (_date.Year.ToString ())
		};

		if (_comboBoxYear.SelectedItem == -1) {
			_comboBoxYear.SearchText = Date.Year.ToString ();
		}

		_comboBoxYear.SelectedItemChanged += ChangeYearDate;

		var monthsLabel = new Label ("Month:") {
			X = Pos.Right (_calendar) + 1,
			Y = Pos.Bottom (_comboBoxYear),
			Height = 1,
		};

		_comboBoxMonth = new ComboBox (_months) {
			X = Pos.Right (_calendar) + 1,
			Y = Pos.Bottom (monthsLabel),
			Width = Dim.Fill (),
			Height = comboBoxHeight,
			SelectedItem = _date.Month - 1
		};
		_comboBoxMonth.SelectedItemChanged += ChangeMonthDate;

		var previousMonthButton = new Button ("Previous") {
			X = 0,
			Y = Pos.Bottom (_calendar),
			Height = 1,
		};

		previousMonthButton.Clicked += (sender, e) => {
			Date = _date.AddMonths (-1);
			CreateCalendar ();
			if (_comboBoxMonth.SelectedItem == 0) {
				if (_comboBoxYear.SelectedItem == 0) {
					_comboBoxYear.SearchText = Date.Year.ToString ();
				} else {
					_comboBoxYear.SelectedItem--;
				}
				_comboBoxMonth.SelectedItem = 11;
			} else {
				_comboBoxMonth.SelectedItem = _date.Month - 1;
			}
		};

		var nextMonthButton = new Button ("Next") {
			X = Pos.Right (previousMonthButton),
			Y = Pos.Bottom (_calendar),
			Height = 1,
		};

		nextMonthButton.Clicked += (sender, e) => {
			Date = _date.AddMonths (1);
			CreateCalendar ();
			if (_comboBoxMonth.SelectedItem == 11) {
				if (_comboBoxYear.SelectedItem == _years.Count - 1) {
					_comboBoxYear.SearchText = Date.Year.ToString ();
				} else {
					_comboBoxYear.SelectedItem++;
				}
				_comboBoxMonth.SelectedItem = 0;
			} else {
				_comboBoxMonth.SelectedItem = _date.Month - 1;
			}
		};


		dialog.Add (_calendar,
			yearsLabel, _comboBoxYear,
			monthsLabel, _comboBoxMonth);

		dialog.AddButton (previousMonthButton);
		dialog.AddButton (nextMonthButton);

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
		if (e.Value is not null && int.TryParse (e.Value.ToString (), out int year)) {
			_date = new DateTime (year, _date.Month, _date.Day);
			CreateCalendar ();
		} else {
			_comboBoxYear.SelectedItem = _years.IndexOf (_date.Year.ToString ());
		}
	}

	private void ChangeMonthDate (object sender, ListViewItemEventArgs e)
	{
		bool isValueNotNull = e.Value is not null;
		bool isParsingSuccessful = DateTime.TryParseExact (
		    e.Value.ToString (),
		    "MMMM",
		    CultureInfo.CurrentCulture,
		    DateTimeStyles.None,
		    out DateTime parsedMonth
		);

		if (isValueNotNull && isParsingSuccessful) {
			_date = new DateTime (_date.Year, parsedMonth.Month, _date.Day);
			CreateCalendar ();
		} else {
			_comboBoxMonth.SelectedItem = _date.Month - 1;
		}
	}

	private void ChangeDayDate (int day)
	{
		_date = new DateTime (_date.Year, _date.Month, day);
		CreateCalendar ();
	}

	private DataTable CreateDataTable (int month, int year)
	{
		_table = new DataTable ();
		GenerateCalendarLabels ();
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

	static List<string> GetMonthNames ()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		var monthNames = Enumerable.Range (1, 12)
					   .Select (month => new DateTime (1, month, 1).ToString ("MMMM", culture))
					   .ToList ();

		return monthNames;
	}
}
