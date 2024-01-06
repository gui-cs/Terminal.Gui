//
// DatePicker.cs: DatePicker control
//
// Author: Maciej Winnik
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui.Views;

/// <summary>
/// The <see cref="DatePicker"/> <see cref="View"/> Date Picker.
/// </summary>
public class DatePicker : TextField {

	private DataTable table;
	private TableView calendar;
	private DateTime date = DateTime.Now;
	private List<string> months = new ();
	private List<string> years = new ();

	/// <summary>
	/// Format of date.
	/// </summary>
	public string Format { get; set; } = "dd/MM/yyyy";

	/// <summary>
	/// Get or set the date.
	/// </summary>
	public DateTime Date {
		get => date;
		set {
			date = value;
			Text = date.ToString (Format);
		}
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/>.
	/// </summary>
	public DatePicker () : base () => Initialize ();

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/> with the specified date.
	/// </summary>
	public DatePicker (DateTime date) : base ()
	{
		this.date = date;
		Initialize ();
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DatePicker"/> with the specified date and format.
	/// </summary>
	public DatePicker (DateTime date, string format) : base ()
	{
		this.date = date;
		Format = format;
		Initialize ();
	}

	private void Initialize ()
	{
		months = Enum.GetValues (typeof (Month)).Cast<Month> ().Select (x => x.ToString ()).ToList ();
		years = Enumerable.Range (1900, 200).Select (x => x.ToString ()).ToList ();
	}

	private void ShowDatePickerDialog ()
	{
		var dialog = new Dialog () {
			Height = 14,
			Width = 40,
			Title = "Date Picker",
		};

		calendar = new TableView () {
			X = 0,
			Y = Pos.Center () + 1,
			Width = 22,
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
			X = Pos.Right (calendar) + 1,
			Y = 1,
			Height = 1,
		};
		var comboBoxYear = new ComboBox (years) {
			X = Pos.Right (calendar) + 1,
			Y = 2,
			Width = 12,
			Height = 4,
			SelectedItem = years.IndexOf (date.Year.ToString ())
		};
		comboBoxYear.SelectedItemChanged += ChangeYearDate;

		var monthsLabel = new Label ("Month:") {
			X = Pos.Right (calendar) + 1,
			Y = 6,
			Height = 1,
		};

		var comboBoxMonth = new ComboBox (months) {
			X = Pos.Right (calendar) + 1,
			Y = 7,
			Width = 12,
			Height = 4,
			SelectedItem = date.Month - 1
		};
		comboBoxMonth.SelectedItemChanged += ChangeMonthDate;

		dialog.Add (calendar, yearsLabel, comboBoxYear, monthsLabel, comboBoxMonth);

		CreateCalendar ();
		SelectDayOnCalendar (date.Day);

		calendar.CellActivated += (sender, e) => {
			var dayValue = table.Rows [e.Row] [e.Col];
			int day = int.Parse (dayValue.ToString ());
			ChangeDayDate (day);

			SelectDayOnCalendar (day);
			//calendar.SetSelection (e.Row, e.Col, false);
			try {
				Text = date.ToString (Format);
			} catch (Exception) {
				MessageBox.ErrorQuery ("Error", "Invalid date format", "Ok");
			}
			Application.RequestStop ();
		};
		Application.Run (dialog);
	}

	private void CreateCalendar ()
	{
		calendar.Table = new DataTableSource (table = CreateDataTable (date.Month, date.Year));
	}

	private void ChangeYearDate (object sender, ListViewItemEventArgs e)
	{
		int year = int.Parse (e.Value.ToString ());
		date = new DateTime (year, date.Month, date.Day);
		CreateCalendar ();
	}

	private void ChangeMonthDate (object sender, ListViewItemEventArgs e)
	{
		int monthNumber = (int)Enum.Parse (typeof (Month), e.Value.ToString ());
		date = new DateTime (date.Year, monthNumber, date.Day);
		CreateCalendar ();
	}


	private void ChangeDayDate (int day)
	{
		date = new DateTime (date.Year, date.Month, day);
		CreateCalendar ();
	}

	private DataTable CreateDataTable (int month, int year)
	{
		table = new DataTable ();
		table.Columns.Add ("Su");
		table.Columns.Add ("Mo");
		table.Columns.Add ("Tu");
		table.Columns.Add ("We");
		table.Columns.Add ("Th");
		table.Columns.Add ("Fr");
		table.Columns.Add ("Sa");
		int amountOfDaysInMonth = GetAmountOfDaysInMonth (month, year);
		DateTime dateValue = new DateTime (year, month, 1);
		var dayOfWeek = dateValue.DayOfWeek;

		table.Rows.Add (new object [6]);
		for (int i = 1; i <= amountOfDaysInMonth; i++) {
			table.Rows [^1] [(int)dayOfWeek] = i;
			if (dayOfWeek == DayOfWeek.Saturday && i != amountOfDaysInMonth) {
				table.Rows.Add (new object [7]);
			}
			dayOfWeek = dayOfWeek == DayOfWeek.Saturday ? DayOfWeek.Sunday : dayOfWeek + 1;
		}

		return table;
	}

	private void SelectDayOnCalendar (int day)
	{
		for (int i = 0; i < table.Rows.Count; i++) {
			for (int j = 0; j < table.Columns.Count; j++) {
				if (table.Rows [i] [j].ToString () == day.ToString ()) {
					calendar.SetSelection (j, i, false);
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
