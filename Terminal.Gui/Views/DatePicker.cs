//
// DatePicker.cs: DatePicker control
//
// Author: Maciej Winnik
//

using System.Data;
using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Lets the user pick a date from a visual calendar.</summary>
public class DatePicker : View
{
    private TableView _calendar;
    private DateTime _date;
    private DateField _dateField;
    private Label _dateLabel;
    private Button _nextMonthButton;
    private Button _previousMonthButton;
    private DataTable _table;

    /// <summary>Initializes a new instance of <see cref="DatePicker"/>.</summary>
    public DatePicker () { SetInitialProperties (DateTime.Now); }

    /// <summary>Initializes a new instance of <see cref="DatePicker"/> with the specified date.</summary>
    public DatePicker (DateTime date) { SetInitialProperties (date); }

    /// <summary>CultureInfo for date. The default is CultureInfo.CurrentCulture.</summary>
    public CultureInfo Culture
    {
        get => CultureInfo.CurrentCulture;
        set
        {
            if (value is { })
            {
                CultureInfo.CurrentCulture = value;
                Text = Date.ToString (Format);
            }
        }
    }

    /// <summary>Get or set the date.</summary>
    public DateTime Date
    {
        get => _date;
        set
        {
            _date = value;
            Text = _date.ToString (Format);
        }
    }

    private string Format => StandardizeDateFormat (Culture.DateTimeFormat.ShortDatePattern);

    /// <inheritdoc/>
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

    private void ChangeDayDate (int day)
    {
        _date = new DateTime (_date.Year, _date.Month, day);
        _dateField.Date = _date;
        CreateCalendar ();
    }

    private void CreateCalendar () { _calendar.Table = new DataTableSource (_table = CreateDataTable (_date.Month, _date.Year)); }

    private DataTable CreateDataTable (int month, int year)
    {
        _table = new DataTable ();
        GenerateCalendarLabels ();
        int amountOfDaysInMonth = DateTime.DaysInMonth (year, month);
        var dateValue = new DateTime (year, month, 1);
        DayOfWeek dayOfWeek = dateValue.DayOfWeek;

        _table.Rows.Add (new object [6]);

        for (var i = 1; i <= amountOfDaysInMonth; i++)
        {
            _table.Rows [^1] [(int)dayOfWeek] = i;

            if (dayOfWeek == DayOfWeek.Saturday && i != amountOfDaysInMonth)
            {
                _table.Rows.Add (new object [7]);
            }

            dayOfWeek = dayOfWeek == DayOfWeek.Saturday ? DayOfWeek.Sunday : dayOfWeek + 1;
        }

        int missingRows = 6 - _table.Rows.Count;

        for (var i = 0; i < missingRows; i++)
        {
            _table.Rows.Add (new object [7]);
        }

        return _table;
    }

    private void DateField_DateChanged (object sender, DateTimeEventArgs<DateTime> e)
    {
        Date = e.NewValue;

        if (e.NewValue.Date.Day != _date.Day)
        {
            SelectDayOnCalendar (e.NewValue.Day);
        }

        if (_date.Month == DateTime.MinValue.Month && _date.Year == DateTime.MinValue.Year)
        {
            _previousMonthButton.Enabled = false;
        }
        else
        {
            _previousMonthButton.Enabled = true;
        }

        if (_date.Month == DateTime.MaxValue.Month && _date.Year == DateTime.MaxValue.Year)
        {
            _nextMonthButton.Enabled = false;
        }
        else
        {
            _nextMonthButton.Enabled = true;
        }

        CreateCalendar ();
        SelectDayOnCalendar (_date.Day);
    }

    private void GenerateCalendarLabels ()
    {
        _calendar.Style.ColumnStyles.Clear ();

        for (var i = 0; i < 7; i++)
        {
            string abbreviatedDayName =
                CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName ((DayOfWeek)i);

            _calendar.Style.ColumnStyles.Add (
                                              i,
                                              new ColumnStyle
                                              {
                                                  MaxWidth = abbreviatedDayName.Length,
                                                  MinWidth = abbreviatedDayName.Length,
                                                  MinAcceptableWidth = abbreviatedDayName.Length
                                              }
                                             );
            _table.Columns.Add (abbreviatedDayName);
        }

        // TODO: Get rid of the +7 which is hackish
        _calendar.Width = _calendar.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 7;
    }

    private string GetBackButtonText () { return Glyphs.LeftArrow + Glyphs.LeftArrow.ToString (); }
    private string GetForwardButtonText () { return Glyphs.RightArrow + Glyphs.RightArrow.ToString (); }

    private void SelectDayOnCalendar (int day)
    {
        for (var i = 0; i < _table.Rows.Count; i++)
        {
            for (var j = 0; j < _table.Columns.Count; j++)
            {
                if (_table.Rows [i] [j].ToString () == day.ToString ())
                {
                    _calendar.SetSelection (j, i, false);

                    return;
                }
            }
        }
    }

    private void SetInitialProperties (DateTime date)
    {
        _date = date;
        BorderStyle = LineStyle.Single;
        Date = date;
        _dateLabel = new Label { X = 0, Y = 0, Text = "Date: " };
        CanFocus = true;

        _calendar = new TableView
        {
            Id = "_calendar",
            X = 0,
            Y = Pos.Bottom (_dateLabel),
            Height = 11,
            Style = new TableStyle
            {
                ShowHeaders = true,
                ShowHorizontalBottomline = true,
                ShowVerticalCellLines = true,
                ExpandLastColumn = true,
            },
            MultiSelect = false
        };

        _dateField = new DateField (DateTime.Now)
        {
            Id = "_dateField",
            X = Pos.Right (_dateLabel),
            Y = 0,
            Width = Dim.Width (_calendar) - Dim.Width (_dateLabel),
            Height = 1,
            Culture = Culture
        };

        _previousMonthButton = new Button
        {
            Id = "_previousMonthButton",
            X = Pos.Center () - 2,
            Y = Pos.Bottom (_calendar) - 1,
            Width = 2,
            Text = GetBackButtonText (),
            WantContinuousButtonPressed = true,
            NoPadding = true,
            NoDecorations = true,
            ShadowStyle = ShadowStyle.None
        };
        _previousMonthButton.Accepting += (_, _) => AdjustMonth (-1);

        _nextMonthButton = new Button
        {
            Id = "_nextMonthButton",
            X = Pos.Right (_previousMonthButton) + 2,
            Y = Pos.Bottom (_calendar) - 1,
            Width = 2,
            Text = GetForwardButtonText (),
            WantContinuousButtonPressed = true,
            NoPadding = true,
            NoDecorations = true,
            ShadowStyle = ShadowStyle.None
        };

        _nextMonthButton.Accepting += (_, _) => AdjustMonth (1);

        CreateCalendar ();
        SelectDayOnCalendar (_date.Day);

        _calendar.CellActivated += (sender, e) =>
                                   {
                                       object dayValue = _table.Rows [e.Row] [e.Col];

                                       if (dayValue is null)
                                       {
                                           return;
                                       }

                                       bool isDay = int.TryParse (dayValue.ToString (), out int day);

                                       if (!isDay)
                                       {
                                           return;
                                       }

                                       ChangeDayDate (day);
                                       SelectDayOnCalendar (day);
                                       Text = _date.ToString (Format);
                                   };

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        _dateField.DateChanged += DateField_DateChanged;

        Add (_dateLabel, _dateField, _calendar, _previousMonthButton, _nextMonthButton);
    }

    private void AdjustMonth (int offset)
    {
        Date = _date.AddMonths (offset);
        CreateCalendar ();
        _dateField.Date = Date;
    }

    /// <inheritdoc />
    protected override bool OnDrawingText () { return true; }

    private static string StandardizeDateFormat (string format)
    {
        return format switch
        {
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
    }
}
