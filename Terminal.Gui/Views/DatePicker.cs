//
// DatePicker.cs: DatePicker control
//
// Author: Maciej Winnik
//

using System.Data;
using System.Globalization;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Views;

/// <summary>Lets the user pick a date from a visual calendar.</summary>
public class DatePicker : View, IValue<DateTime>
{
    private TableView? _calendar;
    private DateTime _date;
    private DateField? _dateField;
    private Label? _dateLabel;
    private Button? _nextMonthButton;
    private Button? _previousMonthButton;
    private DataTable? _table;

    /// <summary>Initializes a new instance of <see cref="DatePicker"/>.</summary>
    public DatePicker () { SetInitialProperties (DateTime.Now); }

    /// <summary>Initializes a new instance of <see cref="DatePicker"/> with the specified date.</summary>
    public DatePicker (DateTime date) { SetInitialProperties (date); }

    /// <summary>CultureInfo for date. The default is CultureInfo.CurrentCulture.</summary>
    public CultureInfo? Culture
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
            DateTime oldDate = _date;

            if (oldDate == value)
            {
                return;
            }

            if (RaiseDateValueChanging (oldDate, value))
            {
                return;
            }

            _date = value;
            RaiseDateValueChanged (oldDate, value);
        }
    }

    /// <inheritdoc />
    public override string Text
    {
        get => Date.ToString (Format);
        set
        {
            if (DateTime.TryParse (value, out DateTime result))
            {
                Date = result;
            }
        }
    }

    #region IValue<DateTime> Implementation

    /// <inheritdoc/>
    public DateTime Value
    {
        get => Date;
        set => Date = value;
    }

    /// <inheritdoc/>
    object? IValue.GetValue () => Date;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<DateTime>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<DateTime>>? ValueChanged;

    private bool RaiseDateValueChanging (DateTime currentValue, DateTime newValue)
    {
        ValueChangingEventArgs<DateTime> args = new (currentValue, newValue);
        ValueChanging?.Invoke (this, args);

        return args.Handled;
    }

    private void RaiseDateValueChanged (DateTime oldValue, DateTime newValue)
    {
        ValueChangedEventArgs<DateTime> args = new (oldValue, newValue);
        ValueChanged?.Invoke (this, args);
    }

    #endregion

    private string Format => StandardizeDateFormat (Culture?.DateTimeFormat.ShortDatePattern);

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        _dateLabel?.Dispose ();
        _calendar?.Dispose ();
        _dateField?.Dispose ();
        _table?.Dispose ();
        _previousMonthButton?.Dispose ();
        _nextMonthButton?.Dispose ();
        base.Dispose (disposing);
    }

    private void ChangeDayDate (int day)
    {
        Date = new (Date.Year, Date.Month, day);
        _dateField!.Date = Date;
        CreateCalendar ();
    }

    private void CreateCalendar () { _calendar!.Table = new DataTableSource (_table = CreateDataTable (Date.Month, Date.Year)); }

    private DataTable CreateDataTable (int month, int year)
    {
        _table = new ();
        GenerateCalendarLabels ();
        int amountOfDaysInMonth = DateTime.DaysInMonth (year, month);
        DateTime dateValue = new DateTime (year, month, 1);
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

    private void DateField_DateChanged (object? sender, EventArgs<DateTime> e)
    {
        Date = e.Value;

        if (e.Value.Date.Day != Date.Day)
        {
            SelectDayOnCalendar (e.Value.Day);
        }

        if (Date.Month == DateTime.MinValue.Month && Date.Year == DateTime.MinValue.Year)
        {
            _previousMonthButton!.Enabled = false;
        }
        else
        {
            _previousMonthButton!.Enabled = true;
        }

        if (Date.Month == DateTime.MaxValue.Month && Date.Year == DateTime.MaxValue.Year)
        {
            _nextMonthButton!.Enabled = false;
        }
        else
        {
            _nextMonthButton!.Enabled = true;
        }

        CreateCalendar ();
        SelectDayOnCalendar (Date.Day);
    }

    private void GenerateCalendarLabels ()
    {
        _calendar!.Style.ColumnStyles.Clear ();

        for (var i = 0; i < 7; i++)
        {
            string abbreviatedDayName =
                CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName ((DayOfWeek)i);

            _calendar.Style.ColumnStyles.Add (
                                              i,
                                              new ()
                                              {
                                                  MaxWidth = abbreviatedDayName.Length,
                                                  MinWidth = abbreviatedDayName.Length,
                                                  MinAcceptableWidth = abbreviatedDayName.Length
                                              }
                                             );
            _table!.Columns.Add (abbreviatedDayName);
        }

        // TODO: Get rid of the +7 which is hackish
        _calendar.Width = _calendar.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 7;
    }

    private static string GetBackButtonText () { return Glyphs.LeftArrow + Glyphs.LeftArrow.ToString (); }
    private static string GetForwardButtonText () { return Glyphs.RightArrow + Glyphs.RightArrow.ToString (); }

    private void SelectDayOnCalendar (int day)
    {
        for (var i = 0; i < _table!.Rows.Count; i++)
        {
            for (var j = 0; j < _table.Columns.Count; j++)
            {
                if (_table.Rows [i] [j].ToString () == day.ToString ())
                {
                    _calendar!.SetSelection (j, i, false);

                    return;
                }
            }
        }
    }

    private void SetInitialProperties (DateTime date)
    {
        Date = date;
        BorderStyle = LineStyle.Single;
        Date = date;
        _dateLabel = new () { X = 0, Y = 0, Text = "Date: " };
        CanFocus = true;

        _calendar = new ()
        {
            Id = "_calendar",
            X = 0,
            Y = Pos.Bottom (_dateLabel),
            Height = 11,
            Style = new ()
            {
                ShowHeaders = true,
                ShowHorizontalBottomline = true,
                ShowVerticalCellLines = true,
                ExpandLastColumn = true
            },
            MultiSelect = false
        };

        _dateField = new (DateTime.Now)
        {
            Id = "_dateField",
            X = Pos.Right (_dateLabel),
            Y = 0,
            Width = Dim.Width (_calendar) - Dim.Width (_dateLabel),
            Height = 1,
            Culture = Culture
        };

        _previousMonthButton = new ()
        {
            Id = "_previousMonthButton",
            X = Pos.Center () - 2,
            Y = Pos.Bottom (_calendar) - 1,
            Width = 2,
            Text = GetBackButtonText (),
            MouseHoldRepeat = MouseFlags.LeftButtonReleased,
            NoPadding = true,
            NoDecorations = true,
            ShadowStyle = ShadowStyle.None
        };
        _previousMonthButton.Accepting += (_, _) => AdjustMonth (-1);

        _nextMonthButton = new ()
        {
            Id = "_nextMonthButton",
            X = Pos.Right (_previousMonthButton) + 2,
            Y = Pos.Bottom (_calendar) - 1,
            Width = 2,
            Text = GetForwardButtonText (),
            MouseHoldRepeat = MouseFlags.LeftButtonReleased,
            NoPadding = true,
            NoDecorations = true,
            ShadowStyle = ShadowStyle.None
        };

        _nextMonthButton.Accepting += (_, _) => AdjustMonth (1);

        CreateCalendar ();
        SelectDayOnCalendar (Date.Day);

        _calendar.CellActivated += (_, e) =>
                                   {
                                       object dayValue = _table!.Rows [e.Row] [e.Col];

                                       bool isDay = int.TryParse (dayValue.ToString (), out int day);

                                       if (!isDay)
                                       {
                                           return;
                                       }

                                       ChangeDayDate (day);
                                       SelectDayOnCalendar (day);
                                       Text = Date.ToString (Format);
                                   };

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        _dateField.DateChanged += DateField_DateChanged;

        Add (_dateLabel, _dateField, _calendar, _previousMonthButton, _nextMonthButton);
    }

    private void AdjustMonth (int offset)
    {
        Date = Date.AddMonths (offset);
        CreateCalendar ();
        _dateField!.Date = Date;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingText () { return true; }

    private static string StandardizeDateFormat (string? format)
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
