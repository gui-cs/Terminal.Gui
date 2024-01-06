using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Date Picker", Description: "Demonstrates how to use DatePicker class")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class DatePickers : Scenario {
	private DataTable table;
	private TableView calendar;
	private DateTime date = DateTime.Now;
	public override void Setup ()
	{
		List<string> months = Enum.GetValues (typeof (Month)).Cast<Month> ().Select (x => x.ToString ()).ToList ();
		List<string> years = Enumerable.Range (1900, 200).Select (x => x.ToString ()).ToList ();
		var comboBoxYear = new ComboBox (years) {
			X = Pos.Center (),
			Y = 1,
			Width = Dim.Percent (25),
		};

		comboBoxYear.SelectedItem = years.IndexOf (date.Year.ToString ());
		comboBoxYear.SelectedItemChanged += ChangeYearDate;
		Win.Add (comboBoxYear);

		var comboBoxMonth = new ComboBox (months) {
			X = Pos.Right (comboBoxYear),
			Y = 1,
			Width = Dim.Percent (25),
		};

		comboBoxMonth.SelectedItem = date.Month;
		comboBoxMonth.SelectedItemChanged += ChangeMonthDate;

		Win.Add (comboBoxMonth);

		var button = new Button ("Open date picker") {
			X = Pos.Center (),
			Y = Pos.Bottom (comboBoxMonth),
		};

		button.Clicked += (s, e) => {
			var dialog = new Dialog ();
			Application.Run (dialog);
		};

		calendar = new TableView () {
			X = 0,
			Y = Pos.Bottom (button),
			Width = Dim.Fill (),
			Height = Dim.Fill (1),
			Style = new TableStyle {
				ShowHeaders = true,
				ShowHorizontalHeaderOverline = false,
				ShowHorizontalHeaderUnderline = false,
				ShowHorizontalBottomline = false,
				ExpandLastColumn = false,
			}
		};

		var days = Enumerable.Range (1, 31).ToArray ();
		calendar.Table = new ListTableSource (days, calendar);

		CreateCalendar ();
		calendar.CellActivated += SelectDate;

		Win.Add (calendar);
		Win.Add (button);
	}

	public void CreateCalendar ()
	{
		calendar.Table = new DataTableSource (this.table = CreateDataTable (date.Month, date.Year));
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

	private void SelectDate (object sender, CellActivatedEventArgs e)
	{
		var o = table.Rows [e.Row] [e.Col];
		MessageBox.Query ("Selected date", date.ToString (), "Ok");
	}



	public DataTable CreateDataTable (int month, int year)
	{
		// calculate amount of days in month
		int days = GetAmountOfDaysInMonth (month, year);
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

		table.Rows.Add (new object [7]);
		for (int i = 1; i <= amountOfDaysInMonth; i++) {
			table.Rows [^1] [(int)dayOfWeek] = i;
			if (dayOfWeek == DayOfWeek.Saturday) {
				table.Rows.Add (new object [7]);
			}
			dayOfWeek = dayOfWeek == DayOfWeek.Saturday ? DayOfWeek.Sunday : dayOfWeek + 1;
		}

		return table;
	}

	int GetAmountOfDaysInMonth (int month, int year)
	{
		return DateTime.DaysInMonth (year, month);
	}

	int GetNumberOfMonth (string nameOfMonth) =>
		nameOfMonth.ToLower () switch {
			"january" => 1,
			"february" => 2,
			"march" => 3,
			"april" => 4,
			"may" => 5,
			"june" => 6,
			"july" => 7,
			"august" => 8,
			"september" => 9,
			"october" => 10,
			"november" => 11,
			"december" => 12,
			_ => -1
		};

	public enum Month {
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

// Repopulate on choice
