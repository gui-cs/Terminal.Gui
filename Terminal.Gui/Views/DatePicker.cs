//
// DatePicker.cs: DatePicker control
//
// Author: Maciej Winnik
//

using System.Data;
using System.Globalization;

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
    public DatePicker () => SetInitialProperties (DateTime.Now);

    /// <summary>Initializes a new instance of <see cref="DatePicker"/> with the specified date.</summary>
    public DatePicker (DateTime date) => SetInitialProperties (date);

    /// <summary>CultureInfo for date. The default is CultureInfo.CurrentCulture.</summary>
    public CultureInfo? Culture
    {
        get => CultureInfo.CurrentCulture;
        set
        {
            if (value is { })
            {
                CultureInfo.CurrentCulture = value;
                Text = Value.ToString (Format);
            }
        }
    }

    /// <inheritdoc/>
    public override string Text
    {
        get => Value.ToString (Format);
        set
        {
            if (DateTime.TryParse (value, out DateTime result))
            {
                Value = result;
            }
        }
    }

    #region IValue<DateTime> Implementation

    /// <summary>Gets or sets the date value of the <see cref="DatePicker"/>.</summary>
    public DateTime Value
    {
        get => _date;
        set
        {
            DateTime oldValue = _date;

            if (oldValue == value)
            {
                return;
            }

            ValueChangingEventArgs<DateTime> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _date = value;

            ValueChangedEventArgs<DateTime> changedArgs = new (oldValue, _date);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _date));
        }
    }

    /// <inheritdoc/>
    object? IValue.GetValue () => _date;

    /// <inheritdoc />
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called when the <see cref="DatePicker"/> <see cref="Value"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<DateTime> args) => false;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<DateTime>>? ValueChanging;

    /// <summary>
    ///     Called when the <see cref="DatePicker"/> <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<DateTime> args) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<DateTime>>? ValueChanged;

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
        Value = new DateTime (Value.Year, Value.Month, day);
        _dateField!.Value = Value;
        CreateCalendar ();
    }

    private void CreateCalendar () => _calendar!.Table = new DataTableSource (_table = CreateDataTable (Value.Month, Value.Year));

    private DataTable CreateDataTable (int month, int year)
    {
        _table = new DataTable ();
        GenerateCalendarLabels ();
        int amountOfDaysInMonth = DateTime.DaysInMonth (year, month);
        DateTime dateValue = new (year, month, 1);
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

    private void DateField_ValueChanged (object? sender, ValueChangedEventArgs<DateTime?> e)
    {
        if (!e.NewValue.HasValue)
        {
            return;
        }

        Value = e.NewValue.Value;

        if (e.NewValue.Value.Day != Value.Day)
        {
            SelectDayOnCalendar (e.NewValue.Value.Day);
        }

        if (Value.Month == DateTime.MinValue.Month && Value.Year == DateTime.MinValue.Year)
        {
            _previousMonthButton!.Enabled = false;
        }
        else
        {
            _previousMonthButton!.Enabled = true;
        }

        if (Value.Month == DateTime.MaxValue.Month && Value.Year == DateTime.MaxValue.Year)
        {
            _nextMonthButton!.Enabled = false;
        }
        else
        {
            _nextMonthButton!.Enabled = true;
        }

        CreateCalendar ();
        SelectDayOnCalendar (Value.Day);
    }

    private void GenerateCalendarLabels ()
    {
        _calendar!.Style.ColumnStyles.Clear ();

        for (var i = 0; i < 7; i++)
        {
            string abbreviatedDayName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName ((DayOfWeek)i);

            _calendar.Style.ColumnStyles.Add (i,
                                              new ColumnStyle
                                              {
                                                  MaxWidth = abbreviatedDayName.Length,
                                                  MinWidth = abbreviatedDayName.Length,
                                                  MinAcceptableWidth = abbreviatedDayName.Length
                                              });
            _table!.Columns.Add (abbreviatedDayName);
        }

        // TODO: Get rid of the +7 which is hackish
        _calendar.Width = _calendar.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 7;
    }

    private static string GetBackButtonText () => Glyphs.LeftArrow + Glyphs.LeftArrow.ToString ();
    private static string GetForwardButtonText () => Glyphs.RightArrow + Glyphs.RightArrow.ToString ();

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
        Value = date;
        BorderStyle = LineStyle.Single;
        Value = date;
        _dateLabel = new Label { X = 0, Y = 0, Text = "Date: " };
        CanFocus = true;

        _calendar = new TableView
        {
            Id = "_calendar",
            X = 0,
            Y = Pos.Bottom (_dateLabel),
            Height = 11,
            Style = new TableStyle { ShowHeaders = true, ShowHorizontalBottomline = true, ShowVerticalCellLines = true, ExpandLastColumn = true },
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
            MouseHoldRepeat = MouseFlags.LeftButtonReleased,
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
            MouseHoldRepeat = MouseFlags.LeftButtonReleased,
            NoPadding = true,
            NoDecorations = true,
            ShadowStyle = ShadowStyle.None
        };

        _nextMonthButton.Accepting += (_, _) => AdjustMonth (1);

        CreateCalendar ();
        SelectDayOnCalendar (Value.Day);

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
                                       Text = Value.ToString (Format);
                                   };

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        _dateField.ValueChanged += DateField_ValueChanged;

        Add (_dateLabel, _dateField, _calendar, _previousMonthButton, _nextMonthButton);
    }

    private void AdjustMonth (int offset)
    {
        Value = Value.AddMonths (offset);
        CreateCalendar ();
        _dateField!.Value = Value;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingText () => true;

    private static string StandardizeDateFormat (string? format) =>
        format switch
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
