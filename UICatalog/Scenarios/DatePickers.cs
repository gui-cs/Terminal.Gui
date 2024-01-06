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

		var button = new Button ("Open date picker") {
			X = Pos.Center (),
			Y = 0,
		};

		button.Clicked += (s, e) => {
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
			calendar.CellActivated += SelectDate;
			Application.Run (dialog);
		};


		Win.Add (button);
	}

	public void CreateCalendar ()
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

	private void SelectDate (object sender, CellActivatedEventArgs e)
	{
		var dayValue = table.Rows [e.Row] [e.Col];
		int day = int.Parse (dayValue.ToString ());
		ChangeDayDate (day);
		MessageBox.Query ("Selected date", date.ToString (), "Ok");
	}

	public DataTable CreateDataTable (int month, int year)
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

